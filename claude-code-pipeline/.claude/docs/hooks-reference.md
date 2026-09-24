# Active Hooks

The hooks this kit actually ships. `.claude/settings.json` wires every one except the two
track-prompt hooks, which `/track-prompt-duration` adds to `settings.local.json` on request.
Every command goes through `${CLAUDE_PROJECT_DIR}`, so it works whatever the current directory.

| Hook | Event (matcher) | Does | Outcome |
|---|---|---|---|
| `session-start.sh` | SessionStart | Branch, last 5 commits, new assets under `Assets/`/`Packages/` with no `.meta`, a preview of `docs/_session/active.md`, and a warning when no Python is available for the guard | stdout becomes session context |
| `guard.sh pre-edit` → `unity_guard.py` | PreToolUse (`Edit\|Write`) | Refuses text edits to `.meta`, `.unity`, `.prefab`, and serialised `.asset` files | **blocks** (exit 2) |
| `guard.sh post-edit` → `unity_guard.py` | PostToolUse (`Edit\|Write`) | C# lint on the edited `.cs`: GetComponent/Find near `Update`, LINQ in gameplay, bare `Debug.Log`, public fields, `{ get; private set; }`, a `[SerializeField]` renamed without `FormerlySerializedAs`, file/class name mismatch | warnings reach Claude (exit 2; the edit stands) |
| `guard.sh pre-bash` → `unity_guard.py` | PreToolUse (`Bash`) | Refuses deleting/renaming `.meta`, wiping `Library/`, touching `ProjectSettings/` or the package manifest, force push, switching branches, rebase, `unity build/run/install`, and asset deletion inside `unity eval` | **blocks** (exit 2) |
| `guard.sh pre-eval` → `unity_guard.py` | PreToolUse (bridge `eval` / `run_script` / `Unity_RunCommand`) | Refuses `AssetDatabase.DeleteAsset`, `File.Delete` and the like inside eval code, which would skip the reference scan | **blocks** (exit 2) |
| `validate-commit.sh` | PreToolUse (`Bash`) | Only for `git commit`: a new asset staged without its `.meta` is refused; the default branch, `ProjectSettings/` or a third-party SDK folder makes the developer confirm | **blocks** / **asks** |
| `track-prompt-start.sh` / `track-prompt-stop.sh` | UserPromptSubmit / Stop | Prompt timing (opt-in, `/track-prompt-duration`) | Stop prints `{"systemMessage": …}`: the developer sees the duration at the end of that same turn |

## Rules for writing a hook here

- **Only two things reach Claude:** stderr on **exit 2**, or a JSON decision on stdout
  (`hookSpecificOutput.permissionDecision` for PreToolUse). Anything printed with exit 0 on a
  tool hook is lost; any other non-zero code counts as "carry on". A hook that only warns must
  therefore exit 2 on PostToolUse, or ask/deny on PreToolUse. [INFERRED: Claude Code hooks docs]
- **To show the developer a line without involving Claude**, print a JSON object with
  `systemMessage` (exit 0). A `Stop` hook's `systemMessage` appears in the transcript; its
  plain stdout does not. [VERIFIED: code.claude.com/docs/en/hooks, Stop decision control]
- **Fail open, but visibly.** An internal error must never block a session, and a hook that
  cannot run (no Python) must say so once at session start instead of vanishing.
- **Never call `python` directly.** On Windows it is often the Microsoft Store stub (exit 49,
  which counts as "carry on"), on macOS often absent. Go through `guard.sh`, which probes.
- **Parse JSON without `jq`** (it is not installed everywhere): `grep -E`, never `grep -P`.
- **A PreToolUse `Bash` hook runs on every shell call.** Leave in the first lines when the
  command is not the one it cares about.
- Kill switches for `unity_guard.py`: `DISABLE_UNITY_HOOKS=1`, `UNITY_HOOK_MODE=warn`.

Input schemas and exit codes: `hooks-reference/hook-input-schemas.md`.
