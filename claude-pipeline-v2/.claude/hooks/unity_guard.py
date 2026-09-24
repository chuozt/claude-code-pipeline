#!/usr/bin/env python3
"""Unity safety hooks - Python port of the everything-claude-unity hooks (MIT), rewritten
so they need no `jq`. One script, four modes, launched by guard.sh from settings.json:

  bash .claude/hooks/guard.sh pre-edit    PreToolUse  Edit|Write    -> may BLOCK (exit 2)
  bash .claude/hooks/guard.sh post-edit   PostToolUse Edit|Write    -> warnings reach Claude (exit 2)
  bash .claude/hooks/guard.sh pre-bash    PreToolUse  Bash          -> may BLOCK (exit 2)
  bash .claude/hooks/guard.sh pre-eval    PreToolUse  bridge eval   -> may BLOCK (exit 2)

Exit codes matter: Claude Code shows a hook's stderr to Claude only on exit 2. On PostToolUse
exit 2 undoes nothing (the edit already happened); it is the only way the warnings get read.
Any other non-zero code counts as "carry on" and the output is lost.

Kill switches: DISABLE_UNITY_HOOKS=1 bypasses everything; UNITY_HOOK_MODE=warn downgrades
every block to a warning. Never exits non-zero for any other reason: a broken hook would
interrupt every session.
"""
import json
import os
import re
import sys

try:
    sys.stderr.reconfigure(encoding="utf-8")
except Exception:
    pass


def _read_input():
    try:
        return json.load(sys.stdin)
    except Exception:
        return {}


def _block(message):
    if os.environ.get("UNITY_HOOK_MODE") == "warn":
        sys.stderr.write("WARNING (downgraded from BLOCKED): " + message + "\n")
        sys.exit(0)
    sys.stderr.write("BLOCKED: " + message + "\n")
    sys.exit(2)


def _norm(path):
    return path.replace("\\", "/")


# --- pre-edit: Unity YAML and .meta must never be text-edited (CLAUDE.md s.6, anti-patterns s.5) ---
def pre_edit(data):
    path = _norm(str(data.get("tool_input", {}).get("file_path", "")))
    if not path:
        return
    if path.endswith(".meta"):
        _block(f"{path}\n  .meta files hold the GUIDs every reference depends on. Unity manages "
               "them; never edit by hand (CLAUDE.md s.6).")
    if path.endswith((".unity", ".prefab")):
        _block(f"{path}\n  Scenes and prefabs are Unity-serialised YAML; a text edit corrupts "
               "references. Change them in the Editor, or through the bridge after /use-mcp "
               "(get_scene_hierarchy / set_serialized_field / batch).")
    if path.endswith(".asset") and not re.search(r"/(Scripts|Editor|Plugins)/", path):
        _block(f"{path}\n  .asset is serialised data (ScriptableObject / settings). Change it in "
               "the Inspector or through the bridge (set_serialized_field) after /use-mcp. "
               "ProjectSettings/*.asset is off-limits entirely (CLAUDE.md s.10).")


# --- post-edit: C# pitfalls the kit forbids (anti-patterns s.3/s.4, coding_convention s.6/s.8) ---
def _content(data):
    ti = data.get("tool_input", {})
    return str(ti.get("new_string") or ti.get("content") or "")


def post_edit(data):
    ti = data.get("tool_input", {})
    path = _norm(str(ti.get("file_path", "")))
    if not path.endswith(".cs"):
        return
    content = _content(data)
    if not content:
        return
    warnings = []
    is_editor = "/Editor/" in path or "/editor/" in path
    is_test = re.search(r"Tests?[/.]", path) is not None

    if not is_editor:
        if re.search(r"void\s+(Update|FixedUpdate|LateUpdate)\s*\(", content) \
                and re.search(r"\b(GetComponent|TryGetComponent|FindObjectOfType|FindObjectsOfType|GameObject\.Find|Camera\.main)\b", content):
            warnings.append("GetComponent / Find / Camera.main next to an Update method - cache it in Awake/Start (CLAUDE.md s.6).")
        if not is_test and re.search(r"\.(Where|Select|Any|All|First|Last|OrderBy|GroupBy|Aggregate|ToList|ToArray|ToDictionary)\s*\(", content):
            warnings.append("LINQ in gameplay code allocates every call - use a plain loop (anti-patterns s.4).")
        if re.search(r"new\s+WaitForSeconds\s*\(", content):
            warnings.append("new WaitForSeconds() allocates each call - cache it, or use UniTask.Delay (the project's async standard).")
        if re.search(r"\bGC\.Collect\s*\(", content):
            warnings.append("GC.Collect() by hand is FORBIDDEN - it causes the hitch it claims to fix (anti-patterns s.4).")
    if re.search(r"\bDebug\.(Log|LogWarning|LogError)\s*\(", content) and not re.search(r"\[Conditional\s*\(", content):
        warnings.append("Bare Debug.Log - use the GameDebug wrapper (coding_convention s.8).")
    if re.search(r"\.tag\s*==\s*\"", content):
        warnings.append("`.tag == \"x\"` allocates and hardcodes a tag string - use CompareTag with a constant (anti-patterns s.1).")
    if re.search(r"\?\.(enabled|transform|gameObject|name|tag|GetComponent)\b", content):
        warnings.append("Null-conditional on a UnityEngine.Object skips the fake-null check (coding_convention s.6).")
    if re.search(r"\{\s*get;\s*private\s+set;\s*\}", content):
        warnings.append("Auto-property with private setter is forbidden - backing field + expression body (coding_convention s.3).")
    if re.search(r"\bpublic\s+(?!static\b|const\b|readonly\b)[\w<>\[\],]+\s+\w+\s*(=|;)", content) \
            and re.search(r":\s*(MonoBehaviour|ScriptableObject)\b", content):
        warnings.append("Public field on a MonoBehaviour/ScriptableObject - use [SerializeField] private (CLAUDE.md s.6).")

    warnings += _renamed_serialized_fields(ti)
    warnings += _filename_mismatch(path, content)

    if warnings:
        sys.stderr.write(f"\nUNITY-GUARD: {path}\n")
        for w in warnings:
            sys.stderr.write("  - " + w + "\n")
        sys.exit(2)  # the edit stands; exit 2 is what makes Claude read the warnings


def _renamed_serialized_fields(ti):
    old, new = str(ti.get("old_string") or ""), str(ti.get("new_string") or "")
    if not old or not new:
        return []
    pattern = re.compile(r"\[SerializeField\][^;=\n]*?\s(\w+)\s*[;=]")
    old_fields, new_fields = set(pattern.findall(old)), set(pattern.findall(new))
    out = []
    for name in old_fields - new_fields:
        if not re.search(r"FormerlySerializedAs\(\s*\"" + re.escape(name) + r"\"", new):
            out.append(f"[SerializeField] '{name}' renamed without [FormerlySerializedAs(\"{name}\")] - every scene/prefab value silently resets.")
    return out


def _filename_mismatch(path, content):
    name = os.path.basename(path)[:-3]
    if name.endswith(("Tests", "Test")) or "." in name:
        return []
    if not re.search(r":\s*(MonoBehaviour|ScriptableObject)\b", content):
        return []
    if re.search(r"\b(class|struct)\s+" + re.escape(name) + r"\b", content):
        return []
    m = re.search(r"\b(class|struct)\s+(\w+)", content)
    if m and m.group(2) != name:
        return [f"File '{name}.cs' does not contain class '{name}' (found '{m.group(2)}') - Unity cannot attach it (coding_convention s.4)."]
    return []


# --- pre-bash / pre-eval: destructive commands and the Editor-bridge rule ---
# A deny rule on `rm` is about the intent, not the spelling: deleting an asset through the
# bridge (AssetDatabase.DeleteAsset inside an eval) skips the same reference scan. Seen in
# practice from a subagent that had `rm` denied.
EVAL_DELETE = ("Deleting assets or files through eval bypasses the reference scan (rules.md s.5). "
               "Scan AssetDatabase.GetDependencies first, then use the delete_asset tool, which "
               "asks the developer for permission.")
EVAL_DELETE_RE = (r"\b(AssetDatabase\.(DeleteAssets?|MoveAssetsToTrash|MoveAssetToTrash)"
                  r"|File\.Delete|Directory\.Delete|FileUtil\.Delete\w*)\s*\(")

DANGER = [
    (r"rm\s+-[rRf]+\s+\S*(Library|Temp|Logs|obj|Build|Builds)/", "Deleting Library/Temp/obj/Build forces a full reimport and can corrupt GUIDs while the Editor is open."),
    # One command segment at a time ([^|;&]*), so a read-only `find ... | grep -v .meta` passes.
    (r"\b(rm|mv|rename|unlink)\s[^|;&]*\.meta\b", ".meta deletion/rename breaks every reference to the asset (anti-patterns s.5)."),
    (r"\bfind\s[^|;&]*\.meta\b[^|;&]*-(delete|exec)\b", ".meta deletion breaks every reference to the asset (anti-patterns s.5)."),
    (r"\b(rm|mv|cp)\s+\S*ProjectSettings/\w+\.asset|>\s*\S*ProjectSettings/", "ProjectSettings/ is off-limits (CLAUDE.md s.10)."),
    (r"(rm|>|truncate)\s*\S*Packages/(manifest|packages-lock)\.json", "Rewriting the package manifest drops packages silently."),
    (r"git\s+push\s+.*(--force|-f)\b", "Force push rewrites shared history - the developer does that, never the AI."),
    # `git checkout -- <file>` restores a file; `--ours` / `--theirs` / `--merge` / `--conflict`
    # pick a side during conflict resolution, which is the AI's job once the developer asks for a merge.
    (r"git\s+(checkout|switch)\s+(?!--(\s|ours\b|theirs\b|merge\b|conflict\b))\S", "Switching branches is the developer's job (CLAUDE.md s.10). `git checkout -- <file>` to restore is also destructive."),
    (r"git\s+rebase\b", "Rebasing rewrites history - the developer does that. Merges and commits are allowed when asked (CLAUDE.md s.4)."),
    (r"\bunity\s+(build|run|install|uninstall|self-uninstall|upgrade)\b", "unity build/run/install/uninstall are never run by the AI (mcp_unity.md s.0)."),
    (r"\bunity\s+(command\s+)?eval\b.*" + EVAL_DELETE_RE, EVAL_DELETE),
]


def pre_bash(data):
    cmd = str(data.get("tool_input", {}).get("command", ""))
    if not cmd:
        return
    for pattern, msg in DANGER:
        if re.search(pattern, cmd):
            _block(f"{cmd}\n  {msg}")


def pre_eval(data):
    # The field that holds the code differs between the two bridges; scan the whole input.
    if re.search(EVAL_DELETE_RE, json.dumps(data.get("tool_input", {}))):
        _block(EVAL_DELETE)


def main():
    if os.environ.get("DISABLE_UNITY_HOOKS") == "1" or len(sys.argv) < 2:
        return
    data = _read_input()
    mode = sys.argv[1]
    try:
        if mode == "pre-edit":
            pre_edit(data)
        elif mode == "post-edit":
            post_edit(data)
        elif mode == "pre-bash":
            pre_bash(data)
        elif mode == "pre-eval":
            pre_eval(data)
    except SystemExit:
        raise
    except Exception as e:  # never break the session because of the guard itself
        sys.stderr.write("unity_guard: internal error ignored: " + str(e) + "\n")


if __name__ == "__main__":
    main()
