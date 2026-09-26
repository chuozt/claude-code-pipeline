#!/bin/bash
# Unity safety hooks — one script, four modes, wired in settings.json:
#
#   bash .claude/hooks/guard.sh pre-edit    PreToolUse  Edit|Write    -> may BLOCK (exit 2)
#   bash .claude/hooks/guard.sh post-edit   PostToolUse Edit|Write    -> warnings reach Claude (exit 2)
#   bash .claude/hooks/guard.sh pre-bash    PreToolUse  Bash          -> may BLOCK (exit 2)
#   bash .claude/hooks/guard.sh pre-eval    PreToolUse  bridge eval   -> may BLOCK (exit 2)
#
# Pure bash on purpose. The previous version was Python behind a launcher that failed open when
# no interpreter existed — and on a stock Windows dev machine `python` is only the Microsoft
# Store stub, so the guard let everything through while looking installed. Bash is guaranteed:
# every hook in settings.json already runs through it. No jq either.
#
# Portability: bash 3.2 (macOS) and Git Bash. Regexes are POSIX ERE with [[:classes:]] only —
# no \b \s \w, which macOS's regex library does not know. "Word boundary" is spelled
# (^|[^[:alnum:]_]) in front and ([^[:alnum:]_]|$) behind.
#
# Exit codes: Claude Code shows stderr to Claude only on exit 2. On PostToolUse exit 2 undoes
# nothing (the edit already happened); it is the only way the warnings get read. Any other
# failure inside this script exits 0 — a broken guard must not stop every edit.
#
# Kill switches: DISABLE_UNITY_HOOKS=1 bypasses everything; UNITY_HOOK_MODE=warn downgrades
# every block to a warning.

export LC_ALL=C   # byte-wise matching: a stray non-UTF-8 byte must not make a regex fail

[ "$DISABLE_UNITY_HOOKS" = "1" ] && exit 0
MODE=$1
[ -z "$MODE" ] && exit 0

INPUT=$(cat)

# ---------------------------------------------------------------------------------------------
# JSON helpers
# ---------------------------------------------------------------------------------------------

# Raw, still JSON-escaped value of the first string field named $1. tool_input precedes
# tool_response in the payload, so the first hit is the tool_input one.
# The escape is spelled `[\\].`, not `\\.`: Git for Windows' GNU grep 3.0 never matches `\\.`,
# so every string holding a \" or \n came back empty and the guard passed it unchecked.
json_raw() {
    printf '%s' "$INPUT" \
        | grep -oE "\"$1\"[[:space:]]*:[[:space:]]*\"([^\"\\\\]|[\\\\].)*\"" \
        | head -1 \
        | sed -E "s/^\"$1\"[[:space:]]*:[[:space:]]*\"//; s/\"\$//"
}

# \" first (a raw \" is always an escaped quote inside a JSON string), then printf %b handles
# \\ \n \t \r \uXXXX left to right, so an escaped backslash never starts a second escape.
json_unescape() {
    local s=$1
    s=${s//\\\"/\"}
    printf '%b' "$s"
}

json_str() {
    json_unescape "$(json_raw "$1")"
}

block() {
    if [ "$UNITY_HOOK_MODE" = "warn" ]; then
        printf 'WARNING (downgraded from BLOCKED): %s\n' "$1" >&2
        exit 0
    fi
    printf 'BLOCKED: %s\n' "$1" >&2
    exit 2
}

# ---------------------------------------------------------------------------------------------
# pre-edit: Unity YAML and .meta must never be text-edited (CLAUDE.md s.6, anti-patterns s.5)
# ---------------------------------------------------------------------------------------------

pre_edit() {
    local path
    path=$(json_str file_path)
    path=${path//\\//}
    [ -z "$path" ] && return

    case "$path" in
        *.meta)
            block "$path
  .meta files hold the GUIDs every reference depends on. Unity manages them; never edit by hand (CLAUDE.md s.6)." ;;
        *.unity|*.prefab)
            block "$path
  Scenes and prefabs are Unity-serialised YAML; a text edit corrupts references. Change them in the Editor, or through the bridge after /use-mcp (get_scene_hierarchy / set_serialized_field / batch)." ;;
        *.asset)
            [[ $path =~ /(Scripts|Editor|Plugins)/ ]] && return
            block "$path
  .asset is serialised data (ScriptableObject / settings). Change it in the Inspector or through the bridge (set_serialized_field) after /use-mcp. ProjectSettings/*.asset is off-limits entirely (CLAUDE.md s.10)." ;;
    esac
}

# ---------------------------------------------------------------------------------------------
# post-edit: C# pitfalls the kit forbids (anti-patterns s.3/s.4, coding_convention s.6/s.8)
# ---------------------------------------------------------------------------------------------

B='(^|[^[:alnum:]_])'   # word boundary, leading
E='([^[:alnum:]_]|$)'   # word boundary, trailing
R_UNITY_BASE=":[[:space:]]*(MonoBehaviour|ScriptableObject)$E"
R_SERIALIZED='\[SerializeField\][^;=]*[[:space:]][[:alnum:]_]+[[:space:]]*[;=]'

WARNINGS=""
warn() {
    WARNINGS="$WARNINGS
  - $1"
}

# Names of [SerializeField] fields declared in $1, one per line.
serialized_fields() {
    printf '%s\n' "$1" \
        | grep -oE "$R_SERIALIZED" \
        | sed -E 's/.*[[:space:]]([[:alnum:]_]+)[[:space:]]*[;=]$/\1/' \
        | sort -u
}

check_renamed_fields() {
    local old=$1 new=$2 name
    [ -z "$old" ] || [ -z "$new" ] && return
    [[ $old =~ \[SerializeField\] ]] || return

    local new_fields
    new_fields=$(serialized_fields "$new")
    while IFS= read -r name; do
        [ -z "$name" ] && continue
        printf '%s\n' "$new_fields" | grep -qxF "$name" && continue
        [[ $new =~ FormerlySerializedAs\([[:space:]]*\"$name\" ]] && continue
        warn "[SerializeField] '$name' renamed without [FormerlySerializedAs(\"$name\")] - every scene/prefab value silently resets."
    done <<< "$(serialized_fields "$old")"
}

check_filename() {
    local path=$1 content=$2 name
    name=${path##*/}
    name=${name%.cs}
    case "$name" in *Tests|*Test|*.*) return ;; esac
    [[ $content =~ $R_UNITY_BASE ]] || return
    [[ $content =~ (class|struct)[[:space:]]+$name$E ]] && return
    [[ $content =~ (class|struct)[[:space:]]+([[:alnum:]_]+) ]] || return
    [ "${BASH_REMATCH[2]}" = "$name" ] && return
    warn "File '$name.cs' does not contain class '$name' (found '${BASH_REMATCH[2]}') - Unity cannot attach it (coding_convention s.4)."
}

check_public_field() {
    local content=$1
    [[ $content =~ $R_UNITY_BASE ]] || return
    # `=[^>]` so an expression-bodied property (`public int X => x;`) is not taken for a field.
    printf '%s\n' "$content" \
        | grep -oE "${B}public[[:space:]]+([[:alnum:]_<>,[]|])+[[:space:]]+[[:alnum:]_]+[[:space:]]*(;|=[^>])" \
        | grep -vqE "public[[:space:]]+(static|const|readonly|event|override|abstract|virtual|new)$E" \
        && warn "Public field on a MonoBehaviour/ScriptableObject - use [SerializeField] private (CLAUDE.md s.6)."
}

post_edit() {
    local path content old
    path=$(json_str file_path)
    path=${path//\\//}
    case "$path" in *.cs) ;; *) return ;; esac

    content=$(json_str new_string)
    [ -z "$content" ] && content=$(json_str content)
    [ -z "$content" ] && return

    local is_editor=0 is_test=0
    [[ $path =~ /[Ee]ditor/ ]] && is_editor=1
    [[ $path =~ Tests?[/.] ]] && is_test=1

    if [ $is_editor = 0 ]; then
        [[ $content =~ void[[:space:]]+(Update|FixedUpdate|LateUpdate)[[:space:]]*\( ]] \
            && [[ $content =~ ${B}(GetComponent|TryGetComponent|FindObjectOfType|FindObjectsOfType|GameObject\.Find|Camera\.main)$E ]] \
            && warn "GetComponent / Find / Camera.main next to an Update method - cache it in Awake/Start (CLAUDE.md s.6)."
        [ $is_test = 0 ] \
            && [[ $content =~ \.(Where|Select|Any|All|First|Last|OrderBy|GroupBy|Aggregate|ToList|ToArray|ToDictionary)[[:space:]]*\( ]] \
            && warn "LINQ in gameplay code allocates every call - use a plain loop (anti-patterns s.4)."
        [[ $content =~ new[[:space:]]+WaitForSeconds[[:space:]]*\( ]] \
            && warn "new WaitForSeconds() allocates each call - cache it, or use UniTask.Delay (the project's async standard)."
        [[ $content =~ ${B}GC\.Collect[[:space:]]*\( ]] \
            && warn "GC.Collect() by hand is FORBIDDEN - it causes the hitch it claims to fix (anti-patterns s.4)."
    fi
    [[ $content =~ ${B}Debug\.(Log|LogWarning|LogError)[[:space:]]*\( ]] \
        && ! [[ $content =~ \[Conditional[[:space:]]*\( ]] \
        && warn "Bare Debug.Log - use the GameDebug wrapper (coding_convention s.8)."
    [[ $content =~ \.tag[[:space:]]*==[[:space:]]*\" ]] \
        && warn "\`.tag == \"x\"\` allocates and hardcodes a tag string - use CompareTag with a constant (anti-patterns s.1)."
    [[ $content =~ \?\.(enabled|transform|gameObject|name|tag|GetComponent)$E ]] \
        && warn "Null-conditional on a UnityEngine.Object skips the fake-null check (coding_convention s.6)."
    [[ $content =~ \{[[:space:]]*get\;[[:space:]]*private[[:space:]]+set\;[[:space:]]*\} ]] \
        && warn "Auto-property with private setter is forbidden - backing field + expression body (coding_convention s.3)."
    check_public_field "$content"

    old=$(json_str old_string)
    check_renamed_fields "$old" "$content"
    check_filename "$path" "$content"

    [ -z "$WARNINGS" ] && return
    printf '\nUNITY-GUARD: %s%s\n' "$path" "$WARNINGS" >&2
    exit 2   # the edit stands; exit 2 is what makes Claude read the warnings
}

# ---------------------------------------------------------------------------------------------
# pre-bash / pre-eval: destructive commands and the Editor-bridge rule
# ---------------------------------------------------------------------------------------------

# A deny rule on `rm` is about the intent, not the spelling: deleting an asset through the
# bridge (AssetDatabase.DeleteAsset inside an eval) skips the same reference scan. Seen in
# practice from a subagent that had `rm` denied.
EVAL_DELETE="Deleting assets or files through eval bypasses the reference scan (rules.md s.5). Scan AssetDatabase.GetDependencies first, then use the delete_asset tool, which asks the developer for permission."
R_EVAL_DELETE="${B}(AssetDatabase\.(DeleteAssets?|MoveAssetsToTrash|MoveAssetToTrash)|File\.Delete|Directory\.Delete|FileUtil\.Delete[[:alnum:]_]*)[[:space:]]*\("

# [^|;&]* keeps a match inside one command segment, so a read-only
# `find . -name '*.meta' | grep -v x` passes while `find ... -name '*.meta' -delete` does not.
DANGER_RE=(
    "${B}rm[[:space:]]+(-[^[:space:]]+[[:space:]]+)*[^|;&]*(^|[[:space:]/\"'])(Library|Temp|Logs|obj|Build|Builds)(/|[[:space:]\"']|$)"
    "${B}(rm|mv|rename|unlink)[[:space:]][^|;&]*\.meta$E"
    "${B}find[[:space:]][^|;&]*\.meta$E[^|;&]*-(delete|exec)$E"
    "${B}(rm|mv|cp)[[:space:]]+(-[^[:space:]]+[[:space:]]+)*[^[:space:]]*ProjectSettings/|>[[:space:]]*[^[:space:]]*ProjectSettings/|${B}sed[[:space:]][^|;&]*-i[^|;&]*ProjectSettings/"
    "(${B}(rm|truncate)[[:space:]][^|;&]*|>[[:space:]]*[^[:space:]]*)Packages/(manifest|packages-lock)\.json"
    "${B}git[[:space:]]+push([[:space:]][^|;&]*)?[[:space:]](-[[:alpha:]]*f[[:alpha:]]*|--force)([^[:alnum:]_]|$)"
    "${B}git[[:space:]]+rebase$E"
    "${B}unity[[:space:]]+(build|run|install|uninstall|self-uninstall|upgrade)$E"
    "${B}unity[[:space:]]+(command[[:space:]]+)?eval$E.*$R_EVAL_DELETE"
)
DANGER_MSG=(
    "Deleting Library/Temp/obj/Build forces a full reimport and can corrupt GUIDs while the Editor is open."
    ".meta deletion/rename breaks every reference to the asset (anti-patterns s.5)."
    ".meta deletion breaks every reference to the asset (anti-patterns s.5)."
    "ProjectSettings/ is off-limits (CLAUDE.md s.10)."
    "Rewriting the package manifest drops packages silently."
    "Force push rewrites shared history - the developer does that, never the AI."
    "Rebasing rewrites history - the developer does that. Merges and commits are allowed when asked (CLAUDE.md s.4)."
    "unity build/run/install/uninstall are never run by the AI (mcp_unity.md s.0)."
    "$EVAL_DELETE"
)

# settings.json denies `rm -rf *`, but a permission rule matches the spelling: `rm -r -f`,
# `rm -fR`, `rm --recursive --force` all got past it. Block recursive+force in any spelling.
check_rm_recursive_force() {
    local rest=$1 flags
    while [[ $rest =~ ${B}rm(([[:space:]]+-[^[:space:]]*)+) ]]; do
        flags=${BASH_REMATCH[2]}
        rest=${rest#*"${BASH_REMATCH[0]}"}
        [[ $flags =~ (-[[:alpha:]]*[rR]|--recursive) ]] || continue
        [[ $flags =~ (-[[:alpha:]]*f|--force) ]] || continue
        block "$1
  Recursive force delete (rm -rf in any spelling) is denied by settings.json; a different spelling is the same intent (CLAUDE.md s.10). Ask the developer."
    done
}

# `git checkout -- <file>` restores a file; `--ours` / `--theirs` / `--merge` / `--conflict`
# pick a side during conflict resolution, which is the AI's job once the developer asks for a
# merge. Anything else switches or creates a branch.
check_branch_switch() {
    local rest=$1 arg
    while [[ $rest =~ ${B}git[[:space:]]+(checkout|switch)[[:space:]]+([^[:space:]]+) ]]; do
        arg=${BASH_REMATCH[3]}
        rest=${rest#*"${BASH_REMATCH[0]}"}
        case "$arg" in
            --|--ours|--theirs|--merge|--conflict|--conflict=*) continue ;;
        esac
        block "$1
  Switching branches is the developer's job (CLAUDE.md s.10)."
    done
}

pre_bash() {
    local cmd i
    cmd=$(json_str command)
    [ -z "$cmd" ] && return

    for i in "${!DANGER_RE[@]}"; do
        [[ $cmd =~ ${DANGER_RE[$i]} ]] && block "$cmd
  ${DANGER_MSG[$i]}"
    done
    check_rm_recursive_force "$cmd"
    check_branch_switch "$cmd"
}

pre_eval() {
    # The field that holds the code differs between the two bridges; scan the whole input.
    [[ $INPUT =~ $R_EVAL_DELETE ]] && block "$EVAL_DELETE"
}

case "$MODE" in
    pre-edit)  pre_edit ;;
    post-edit) post_edit ;;
    pre-bash)  pre_bash ;;
    pre-eval)  pre_eval ;;
esac
exit 0
