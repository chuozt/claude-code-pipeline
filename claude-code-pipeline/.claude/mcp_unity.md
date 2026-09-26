# Unity Editor Bridge Handbook — Unity CLI + MCP

Operating manual for the AI ↔ Unity Editor bridge. Read before calling any
`mcp__unity-editor-mcp__*` tool or running `unity command` / `unity eval` in Bash.

> **What bridge(s) this project uses — fill in per project, do not assume the example below.**
> The usual setup is **Unity CLI** (`unity`) talking to the Editor through the
> `com.unity.pipeline` package (in `Packages/manifest.json`). The MCP server is `unity mcp`,
> registered in the user's `~/.claude.json` as `unity-editor-mcp`; its tools appear as
> `mcp__unity-editor-mcp__<command>`. Docs: https://docs.unity.com/en-us/unity-cli/replace-mcp-server-unity-cli
>
> Some projects also keep the in-Editor MCP server of `com.unity.ai.assistant` enabled
> (tools named `mcp__unity-mcp__Unity_*`: `Unity_RunCommand`, `Unity_ApplyTextEdits`,
> `Unity_GetConsoleLogs`…) alongside the CLI bridge, even though Unity deprecated it in favour
> of the CLI — confirm with the developer whether this project does. If both are present:
> same `/use-mcp` gate for both; prefer the CLI bridge (cheaper, Undo-aware commands,
> `[CliCommand]` extensibility); use `Unity_*` only for what the CLI lacks —
> `Unity_GetUserGuidelines`, `Unity_ScriptApplyEdits` structure-aware edits, the
> `Unity_AssetGeneration_*` family. `Unity_RunCommand` needs the `internal class CommandScript
> : IRunCommand` wrapper and echoes the whole script back; `eval` does not. Never drive the
> same scene through both bridges in one task.
>
> The official Unity plugin (`unity@claude-plugins-official`) ships the `unity:unity-cli`
> skill with the full CLI reference. Open it for CLI details; this file holds the
> **project rules** that sit on top.
>
> **Versions.** The bridge is beta: record in `project_setup.md` the `unity` CLI and
> `com.unity.pipeline` versions this project pins, and re-verify the behaviour described
> below on each new project and after every upgrade. Both doors can run side by side.

---

## 0. Two doors, one rule

There are two ways to reach the Editor, and **both are covered by "MCP OFF by default"**
(`CLAUDE.md` §2.1, `skills/use-mcp`):

| Door | Looks like | Needs `/use-mcp` |
|---|---|---|
| MCP tools (CLI bridge) | `mcp__unity-editor-mcp__eval`, `…__console`, `…__run_tests` | **yes** |
| MCP tools (in-Editor) | `mcp__unity-mcp__Unity_RunCommand`, `…Unity_ApplyTextEdits`, `…Unity_GetConsoleLogs` | **yes** |
| API reference MCP | `mcp__unity-api__search_unity_api`, `…get_method_signature`, `…get_deprecation_warnings` | no — offline SQLite lookup of the 6000.3 API docs, touches nothing. **Use it before proposing any Unity API** (`CLAUDE.md` §5) |
| Bash CLI | `unity command …`, `unity eval …`, `unity test …`, `unity build …`, `unity run …`, `unity open …` | **yes** |
| Bash CLI, read-only diagnostics | `unity status`, `unity doctor`, `unity --version`, `unity command` (bare list), `unity mcp configure --list` | no — harmless, no Editor mutation |

### The in-Editor door (`mcp__unity-mcp__Unity_*`) — what it can and cannot do `[VERIFIED 2026-09-15]`

The developer may ask for this door **only** (the Assistant window shows every call, so they
can follow the work). Its `Unity_RunCommand` compiles a snippet as `internal class
CommandScript : IRunCommand` (the wrapper adds a namespace; a second top-level class — a
`MonoBehaviour` — in the same snippet is fine, a class **nested** in CommandScript is not).
Rules learned the hard way:

- `System.Reflection` is **refused** ("unauthorized namespace"), fully-qualified use included.
  Read private state through a public API or a debug method instead.
- `System.IO` in `Execute` is refused as "User interactions are not supported" — but a
  `MonoBehaviour` the command spawns may write files later (`File.WriteAllText`,
  `ScreenCapture.CaptureScreenshot`); that is how a Play-mode run reports back.
- `Unity_GetConsoleLogs` came back empty every time; do not rely on it. Have the spawned
  behaviour write `Reports/<name>.txt` and read that with Bash (`until [ -f … ]; do sleep 2; done`).
- A command that logs a **warning** (e.g., on one project, an asset-loader's own "no atlas
  found" warning) is reported as `UNEXPECTED_ERROR: executed partially` even though it ran to
  the end — read the logs in the message before retrying anything.
- `Unity_Camera_Capture` failed on the Game camera ("Failed to render scene preview");
  `ScreenCapture.CaptureScreenshot` from a coroutine at chosen times is the working capture.
- To recompile after editing files: `AssetDatabase.Refresh(); UnityEditor.Compilation.
  CompilationPipeline.RequestScriptCompilation();` in a command, then ~20 s, then a command
  that `typeof()`s the new types proves they compiled (`EditorApplication.isCompiling` false).
- EditMode tests: if the project has a menu item that runs tests and writes a report file
  (worth building — see `Unity_GetConsoleLogs` note above), call that through
  `Unity_ManageMenuItem` and read the report file it writes; its assembly list must name
  every test asmdef.
- Play mode: `Unity_ManageEditor` Play/Stop with `WaitForCompletion`. Time inside a command
  is one frame: anything timed goes into a spawned `MonoBehaviour` coroutine.
- The project's Bash hook blocks any command whose *text* mentions the forbidden CLI verbs —
  a heredoc quoting this file trips it; edit such docs with the Edit tool.
- **Driving the real input path in Play mode**: `InputSystem.AddDevice<Mouse>()` in the
  spawned behaviour, then per frame `InputSystem.QueueStateEvent(mouse, new MouseState {
  position = cam.WorldToScreenPoint(world) }.WithButton(MouseButton.Left, down))` — the
  queued state is processed before the next frame's `Update`, so `Pointer.current` is that
  mouse and `BoardPointerInput` sees `wasPressedThisFrame` / `isPressed` /
  `wasReleasedThisFrame` exactly as with a finger. Focus the Game view first
  (`EditorApplication.ExecuteMenuItem("Window/General/Game")`): with the default
  `PointersAndKeyboardsRespectGameViewFocus` an unfocused Game view drops pointer events.
  `RemoveDevice` it when done. Sampled numbers at 30 fps and 60 fps differ (the editor's
  frame rate drifts) — log `Time.deltaTime` next to every sample. **Disable the developer's
  own pointers first** (`InputSystem.DisableDevice` on every enabled `Pointer`, re-enable in
  the probe's Finish/OnDestroy): a physical mouse that moves during the probe becomes
  `Pointer.current` and the dragged card follows *it* — one run measured the card parked in
  the screen corner while every sent position was right.
- `Unity_GetSha` / `Unity_ApplyTextEdits` refuse a **dotted** script name
  (`PlayerView.Movement.cs` → "Invalid script name"); partial-class files are edited on disk only.
- A `cd` into a project subfolder (even one done by a Bash command) moves the session's cwd,
  and the Bash/Edit/Write hooks then look for `.claude/hooks/guard.sh` under that folder
  and fail — every file tool is dead until the turn ends. Use absolute paths, never `cd`.

`unity build`, `unity run`, `unity install`, `unity uninstall`, `unity self-uninstall`
are **never** run by the AI (`project_setup.md` §3: no plain CLI builds; the rest change
the machine). If one seems needed, say so and stop.

---

## 1. Golden rule #1 — the bridge follows the running Editor

`unity mcp` and `unity command` **discover the running Editor themselves**; they do not
follow the directory you are chatting in. With several projects open, or the wrong one
open, every command runs against the wrong project **silently**. Classic symptom:
`CS0246: type or namespace not found` for types that certainly exist.

**First thing after `/use-mcp` is granted:**

```bash
unity status --no-banner
```

Expected: one line per connected Editor, whose `Project` column matches this project's own
root (the directory this session is working in) and whose `Version` matches the Unity
version pinned in `CLAUDE.md` §1, e.g.:

```
Port  State  Project                  Version      PID
7800  ready  <this project's path>    <CLAUDE.md's pinned version>  <pid>
```

Or through MCP: `mcp__unity-editor-mcp__editor_status` → `projectPath`, `unityVersion`,
`compiling`, `playMode`. **Project path or version mismatch → STOP, tell the developer,
do nothing else.** No `ready` instance → ask the developer to open the Editor; do not retry
in a loop. Two projects listed → pass `--project-path <this project's path>`.

### Related trap: Safe Mode

With C# compile errors the Editor boots in **Safe Mode**, where `com.unity.pipeline` does
not load — so `unity status` shows nothing and every tool reports "cannot connect", even
though the Editor is open. **"Can't connect" is not "no Editor, edit blindly."**
Confirm with `unity pipeline list`, then read the compile errors from
`Logs/Editor.log` (grep `error CS`), fix them on disk, run the `dotnet build` gate, and
ask the developer to restart Unity. `[VERIFIED: plugin skill unity-cli, integration-advanced.md]`

---

## 2. Three Editor conditions before editing files

| Condition | Why | How to check |
|---|---|---|
| Not in **Play mode** | Asset edits made during Play mode are lost on exit | `editor_status.playMode == "stopped"` |
| Not **compiling** | Commands run against the old assembly and mislead | `editor_status.compiling == false`, `recompile_status` |
| No **modal popup** open | The main thread is blocked and the command hangs | the developer confirms; a hanging call is the symptom |

`recompile` works while the Editor is unfocused, so after editing `.cs` on disk:
`recompile` → poll `recompile_status` until `completed` / `up_to_date` → read `errors[]`.

### Known beta traps (Vindler review 07/2026 + Unity Discussions thread) `[INFERRED: articles read 2026-09-11]`

- **Play Mode ⇒ domain reload ⇒ new bearer token.** An MCP client that cached the token gets
  `401` until restarted (patched, but if every call suddenly fails after `editor_play`, that
  is why — tell the developer to reload the MCP server rather than retrying).
- **Asset refresh may need Editor focus.** A new `.cs` file is not imported (no `.meta`, not
  in the `.csproj`) until Unity is focused or `recompile --focus` runs. Do not report
  "compiles" from a stale `recompile_status`.
- **Modal dialogs block everything** (unsaved-scene prompt, import warnings). A call that
  hangs ⇒ ask the developer to look at the Editor.
- **~0.5–0.8 s per call.** Batch with `batch`, promote repeated work to `[CliCommand]` (§7).

---

## 3. Commonly used tools

Tool names below are the MCP form; the Bash form is `unity command <name> --<param> <value>`.
Full list with parameters: bare `unity command` (read-only). [VERIFIED 2026-09-11 against the live list]

| Group | Tool | Use when |
|---|---|---|
| **State** | `editor_status` | **Call first.** Project path, version, compiling, play mode |
| | `recompile` · `recompile_status` | Recompile after disk edits and read the compile result |
| **Logs** | `console` (`--tail`, `--level`, `--since`) · `get_console_logs` (`--severity`, `--limit`) | **Mandatory when a bug is reported.** Never guess. Filter; never dump |
| | `clear_console` | Before reproducing a bug, so the next read is clean |
| **Running code** | `eval` (`--code`, `--timeout`) | Run a C# snippet in the Editor. See §4 |
| | `eval_file` · `run_script` | Longer snippets from a `.cs` file; `run_script` calls a named static entry point |
| **Tests** | `list_tests` · `run_tests` (`--mode`, `--filter`) · `test_status` · `cancel_tests` | EditMode / PlayMode tests — the verification gate of `verification.md` |
| **Search** | `find_assets` · `find_gameobjects` · `search` | Locate assets / scene objects. For code, prefer `Grep`/`Glob` on disk |
| | `read_text_file` | Read a file under the authoring root (`Assets`) — prefer `Read` on disk |
| **Scenes & objects** | `get_scene_hierarchy` · `get_component_properties` · `get_serialized_fields` | Inspect without opening the Editor UI |
| | `create_gameobject` · `set_component_properties` · `set_serialized_field` · `save_scene` | Mutations — only with the developer's explicit request |
| **Assets** | `create_asset` · `move_asset` · `rename_asset` · `delete_asset` (`--confirm`) | Prefer `--dry_run` first; `delete_asset` only after `rules.md` §5 dependency scan |
| **Screenshots** | `capture_game_view` · `capture_scene_view` · `screenshot` | Verify UI / scenes. Use `--save_path`, not inline base64 (token cost) |
| **Packages** | `package_list` · `package_add` · `package_remove` (`--confirm`) | Inspect / install packages — install only on request |
| **Settings** | `get_player_settings` · `get_quality_settings` · `get_tags_layers` … | Read-only project settings. The `set_*` twins touch `ProjectSettings/` → **forbidden** unless the developer asks (`CLAUDE.md` §10) |
| **Batch** | `batch` (`--operations`, `--transactional`) | Several mutations as one Undo step |

Not in this project's toolset: `Unity_ApplyTextEdits` / `Unity_ScriptApplyEdits`. **Script
editing is done on disk with `Edit`**, then `recompile`.

---

## 4. `eval` — how to use it without wasting tokens

`eval` compiles a C# snippet with Roslyn inside the Editor. No class wrapper, no
`CommandScript`, no `result` object: a statement list, `return <value>;` to get a value back.

```bash
unity command eval --code "return UnityEngine.Application.unityVersion;"
```

Rules:
1. **Fully-qualify every type** — assume nothing is imported, and a `using X;` line at the
   top does **not** work: the snippet is a statement list, so Roslyn reads it as a `using`
   *statement* and fails with CS1001/CS0210 [VERIFIED 2026-09-15]. Local functions and
   `$"…"` strings are fine.
2. **Short snippets only.** Heavy logic lives in the project as a static method; the snippet
   calls it in one line. This is cheaper, reusable from tests and CI, and cannot have a syntax
   slip: `return Game.LevelTools.LevelValidator.ValidateAllToJson();`
   The MCP tool's `timeout` defaults to **5000 ms** ("Main thread operation timed out") — pass
   a longer one for a store-wide job [VERIFIED 2026-09-15].
   **Capturing an EditorWindow** (no tool does it): `window.Focus(); window.Repaint();` then in
   `EditorApplication.delayCall` read `InternalEditorUtility.ReadScreenPixel(new Vector2(
   window.position.x, window.position.y), (int)width, (int)height)` into a `Texture2D` and
   `EncodeToPNG` it. The pixels are host-view ones (tab strip included) — the same frame
   `EditorWindow.SendEvent` coordinates use.
3. **Repeated runs → results to a file**, not to the console. Read the file afterwards.
4. **Mutations through `eval` bypass Undo.** For scene/asset changes prefer the dedicated
   commands (`create_gameobject`, `set_serialized_field`, `batch`), which register Undo.
5. `eval` is arbitrary code execution. Use it only for what the developer asked; never for
   `AssetDatabase.DeleteAsset`, `File.Delete`, or anything under `ProjectSettings/`
   (the `pre-eval` hook refuses the deletion calls).
6. **Editing a serialised list or array through `SerializedObject`: count before and after.**
   `InsertArrayElementAtIndex` duplicates the neighbouring element, and an element whose
   reference is already broken (a missing clip or sprite GUID) can drop out of the list on
   `ApplyModifiedProperties` with no error. Prefer `arraySize++` and filling the new last
   element field by field. Return `before → after` counts from the snippet and compare them
   with what was intended before reporting success.

### Checking compilation

- MCP off: `dotnet build Assembly-CSharp.csproj --no-restore` (`CLAUDE.md` §2.1).
- MCP on: `recompile` → `recompile_status` → `failed == false && errors == []`.
  Either way report *"compiles, not tested"*.

---

## 5. Running tests through the bridge

```bash
unity command list_tests --mode EditMode
unity command run_tests --mode EditMode --filter Game.Model.Tests
unity command test_status
```

- `run_tests` is async; poll `test_status`. Quote the pass/fail counts and the failing
  test names, not "tests pass".
- EditMode for the pure-C# model (`architect.md` §1); PlayMode only for MonoBehaviour lifecycles.
- With MCP off, tests cannot be run by the AI — ask the developer to open the Test Runner.

---

## 6. Context discipline when using the bridge

- **Never** "read the whole Scripts folder". Name the files — and read them from disk.
- `console` / `get_console_logs`: always `--tail` / `--limit` / `--level error` first.
- `get_scene_hierarchy` on a big scene is thousands of lines — pass `--path` and read once.
- Screenshots: `--save_path` into the scratchpad, then `Read` the image.
- While debugging → ask for "file + line number + cause only, do not reprint the code".
- Long reports/measurements → write them to a file rather than printing to chat.

---

## 7. Extending — making the project's own tools callable by AI

The most valuable step: if the project has a level validator or a playtest simulator, let
the AI call it directly instead of sending a script through `eval` every time.

**Cheapest approach, works today:** a static method in the project's editor code that returns
a compact JSON string, called through a one-line `eval`:

```bash
unity command eval --code "return Game.LevelTools.LevelSimulator.RunAllToJson(100);"
```

**Native approach.** `com.unity.pipeline` discovers commands by the `[CliCommand]` attribute;
a static method tagged that way shows up as `unity command <name>` and as an MCP tool after
the next recompile.

> **A useful set, once a validator and a simulator exist** (prefix the names with the game's
> own, tag them e.g. `<game>/levels`): `<game>_check_levels --trials` (batch report: every
> level validated + simulated), `<game>_check_level --level --trials` (one level: errors,
> warnings, issues, win rate), `<game>_validate_level --level` (validator only),
> `<game>_simulate_level --level --trials` (bot only: wins, deadlocks, moves left, first
> failing seed), `<game>_write_level_report --trials` (writes a markdown + JSON report,
> returns the path).

Authoring rules (`Packages/com.unity.pipeline/Documentation~/creating-commands.md`): static
method, `[CliCommand(name, description, Tags = …)]`, parameters tagged `[CliArg]`, return any
serialisable object; the asmdef must reference `Unity.Pipeline`; append new optional parameters
at the end (argument order is wire API).

> `Packages/com.unity.pipeline/` is a third-party package folder: **never edit it.** Its own
> `CLAUDE.md` (naming `m_PascalCase`, "use the unity-pipeline skill") applies to that package
> only, not to game code — `coding_convention.md` still rules under `Assets/`.

---

## 8. Checklist before asking the AI to do heavy work through the bridge

- [ ] `/use-mcp on` (or `once`) was typed by the developer this session
- [ ] `unity status` / `editor_status` shows this project's own path, the version pinned in `CLAUDE.md` §1, and `ready`
- [ ] Not in Play mode, not compiling, no popup
- [ ] The current git branch and the editable file scope have been stated
- [ ] Repeated work is wrapped in a static method inside the project, not sent as a long snippet
- [ ] Mutating commands go through Undo-aware commands or `batch`, with `--dry_run` tried first
