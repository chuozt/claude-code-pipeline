#!/bin/bash
# PreToolUse (Bash) — checks a `git commit` before it runs. Every other command exits at once.
#
#   a NEW asset under Assets/ staged without its .meta  -> BLOCK (exit 2; Claude reads why)
#   default branch · ProjectSettings/ · a third-party   -> ASK   (the developer confirms in
#   SDK folder                                                    the permission prompt)
#
# Only exit 2 (with stderr) or a JSON decision on stdout reaches Claude or the developer;
# anything printed with exit 0 is lost. Bash only, like every hook here: no Python, no jq needed.

INPUT=$(cat)
case "$INPUT" in *commit*) ;; *) exit 0 ;; esac   # fast path: most Bash calls stop here

if command -v jq >/dev/null 2>&1; then
    COMMAND=$(printf '%s' "$INPUT" | jq -r '.tool_input.command // empty')
else
    COMMAND=$(printf '%s' "$INPUT" | grep -oE '"command"[[:space:]]*:[[:space:]]*"([^"\\]|[\\].)*"' \
        | sed 's/.*"command"[[:space:]]*:[[:space:]]*"//;s/"$//')
fi

# `git commit`, also after `cd x &&`, `;`, or with `git -C <dir>`.
printf '%s' "$COMMAND" \
    | grep -qE '(^|[;&|(][[:space:]]*)git([[:space:]]+-C[[:space:]]+[^[:space:]]+)?[[:space:]]+commit([[:space:]]|$)' \
    || exit 0

STAGED=$(git diff --cached --name-only 2>/dev/null)
[ -z "$STAGED" ] && exit 0

# Designer branches (`gd`, `gd-<name>`) carry levels and data, never code. The level simulation
# (Assets/_Tools/LevelSim/) is the developer's tooling. A hook cannot see whether gd-mode is on,
# so this guard is by branch name and holds either way (rules.md s.4, gd-mode s.3.0).
case "$(git rev-parse --abbrev-ref HEAD 2>/dev/null)" in
    gd|gd-*|gd/*)
        FORBIDDEN=$(printf '%s\n' "$STAGED" \
            | grep -E '\.(cs|asmdef)(\.meta)?$|^Assets/_Tools/(LevelSim|LevelPlayer)/' | head -10)
        if [ -n "$FORBIDDEN" ]; then
            printf 'BLOCKED: designer branch commits code or the level simulation (designers push levels and data only):\n%s\nUnstage these files. Code and Assets/_Tools/LevelSim/ and LevelPlayer/ belong to a feature/* branch, committed by the developer.\n' \
                "$(printf '%s\n' "$FORBIDDEN" | sed 's/^/  /')" >&2
            exit 2
        fi ;;
esac

# New assets missing their .meta. Only ADDED files: a modified asset already has its .meta
# in the repo and does not need it staged again.
MISSING=$(git diff --cached --name-only --diff-filter=A 2>/dev/null \
    | grep -E '^Assets/' | grep -vE '\.meta$' \
    | while IFS= read -r f; do
        printf '%s\n' "$STAGED" | grep -qxF "${f}.meta" \
            || git ls-files --error-unmatch "${f}.meta" >/dev/null 2>&1 \
            || echo "  $f"
      done)
if [ -n "$MISSING" ]; then
    printf 'BLOCKED: new assets staged without their .meta (another machine would import them with a new GUID and break every reference):\n%s\nStage the .meta files, or unstage these assets.\n' "$MISSING" >&2
    exit 2
fi

REASONS=""
BRANCH=$(git rev-parse --abbrev-ref HEAD 2>/dev/null)
if [ "$BRANCH" = "main" ] || [ "$BRANCH" = "master" ]; then
    REASONS="$REASONS Committing straight to the default branch '$BRANCH' (CLAUDE.md s.10)."
fi
printf '%s\n' "$STAGED" | grep -q '^ProjectSettings/' \
    && REASONS="$REASONS The commit touches ProjectSettings/ - check TagManager.asset for lost layer names."
printf '%s\n' "$STAGED" | grep -qiE '^Assets/(Plugins|ThirdParty|ThirdParties)/' \
    && REASONS="$REASONS The commit touches a third-party SDK folder."

if [ -n "$REASONS" ]; then
    # Fixed text plus a branch name: escaping quotes is all the JSON needs.
    REASONS=$(printf '%s' "$REASONS" | sed 's/^ //; s/"/\\"/g')
    printf '{"hookSpecificOutput":{"hookEventName":"PreToolUse","permissionDecision":"ask","permissionDecisionReason":"%s"}}\n' "$REASONS"
fi
exit 0
