# Active Hooks

The hooks this kit actually ships. `.claude/settings.json` wires every one except the two
track-prompt hooks, which `/track-prompt-duration` adds to `settings.local.json` on request.
Every command goes through `${CLAUDE_PROJECT_DIR}`, so it works whatever the current directory.

| Hook | Event (matcher) | Does | Outcome |
|---|---|---|---|
| `session-start.sh` | SessionStart | Branch, last 5 commits, new assets under `Assets/`/`Packages/` with no `.meta`, and a preview of `docs/_session/active.md` | stdout becomes session context |
| `guard.sh pre-edit` | PreToolUse (`Edit\|Write`) | Refuses text edits to `.meta`, `.unity`, `.prefab`, and serialised `.asset` files | **blocks** (exit 2) |
| `guard.sh post-edit` | PostToolUse (`Edit\|Write`) | C# lint on the edited `.cs`: GetComponent/Find near `Update`, LINQ in gameplay, bare `Debug.Log`, public fields, `{ get; private set; }`, a `[SerializeField]` renamed without `FormerlySerializedAs`, file/class name mismatch | warnings reach Claude (exit 2; the edit stands) |
| `guard.sh pre-bash` | PreToolUse (`Bash`) | Refuses deleting/renaming `.meta`, wiping `Library/`, touching `ProjectSettings/` or the package manifest, `rm -rf` in any spelling (`rm -r -f`, `-fR`, `--recursive --force`), force push, switching branches, rebase, `unity build/run/install`, and asset deletion inside `unity eval` | **blocks** (exit 2) |
| `guard.sh pre-eval` | PreToolUse (bridge `eval` / `run_script` / `Unity_RunCommand`) | Refuses `AssetDatabase.DeleteAsset`, `File.Delete` and the like inside eval code, which would skip the reference scan | **blocks** (exit 2) |
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
- **Fail open only on the guard's own bugs.** An internal error must never block a session.
- **Bash only — no Python, no Node.** On Windows `python` is often the Microsoft Store stub
  (exit 49, which counts as "carry on"), on macOS often absent. The guard used to be Python
  behind a probe and sat silently inactive on exactly those machines.
- **Parse JSON without `jq`** (it is not installed everywhere): `grep -E`, never `grep -P`.
  Spell a JSON escape `[\\].`, never `\\.` — Git for Windows' GNU grep 3.0 never matches the
  latter, so any string holding `\"` or `\n` extracts as empty.
- **Portable regex:** bash 3.2 (macOS) + POSIX ERE `[[:classes:]]` only — no `\b` `\s` `\w`.
- **After editing a hook, feed it fake payloads** for every mode, both a case it must block
  and one it must let through, including a Windows path and a string with `\"` and `\n`.
- **A PreToolUse `Bash` hook runs on every shell call.** Leave in the first lines when the
  command is not the one it cares about.
- Kill switches for `guard.sh`: `DISABLE_UNITY_HOOKS=1`, `UNITY_HOOK_MODE=warn`.

Input schemas and exit codes: `hooks-reference/hook-input-schemas.md`.
