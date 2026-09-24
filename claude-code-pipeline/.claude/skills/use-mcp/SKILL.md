---
name: use-mcp
model: claude-opus-5-5
effort: medium
description: Controls access to the Unity Editor bridge (Unity CLI + MCP — the mcp__unity-editor-mcp__* tools and Bash `unity command` / `unity eval`). OFF by default for the whole project. /use-mcp on = enabled for the whole session (large, important tasks) · /use-mcp off = disable · /use-mcp this|once|1 = this one prompt only, then off again. Use when the developer types /use-mcp or says "enable MCP", "use the Unity CLI", "use MCP just for this", "turn MCP off".
---

> **Coding rule — mandatory.** Every line of C# this skill writes, reviews, or proposes must follow `.claude/coding_convention.md` (Allman braces, §9 script layout, field order and naming, `GameDebug` instead of `Debug.Log`, no `{ get; private set; }`). Where any sample or advice below disagrees with that file, the convention wins.

Controls permission to reach the running Unity Editor **in the running session**, through
either door (`mcp_unity.md` §0):

- MCP tools, CLI bridge: `mcp__unity-editor-mcp__*` (`eval`, `console`, `recompile`, `run_tests`, …)
- MCP tools, in-Editor bridge: `mcp__unity-mcp__Unity_*` (`Unity_RunCommand`, `Unity_ApplyTextEdits`, …)
- Bash CLI: `unity command …`, `unity eval …`, `unity test …`, `unity build …`,
  `unity run …`, `unity open …`

Read-only diagnostics (`unity status`, `unity doctor`, `unity --version`, bare
`unity command` listing, `unity mcp configure --list`) and the offline API lookup
`mcp__unity-api__*` never need this skill.

## Base Rule: OFF BY DEFAULT ⭐

Until it is enabled, **every call through either door is forbidden** — including "just a
quick check", including when a bug is reported, including when confirming a compile seems
necessary.

**Why**: an Editor round-trip is cheap (~200–600 ms) but its *output* is not — `eval`
results, console dumps and hierarchy trees flood the context, and a mutating command
changes scenes/assets the developer did not ask to change. The developer decides when to
pay that and when the Editor may be touched.

This is **not** a suggestion — it is a constraint. If something "should" be checked
through Unity and the bridge is not enabled, **say so and stop**. Never enable it yourself.

## Three Modes

| Command | Scope | Use when |
|---|---|---|
| `/use-mcp on` | **whole session**, no per-call asking | large, important tasks: playtests, measurements, batch level generation |
| `/use-mcp off` | off immediately | done, or saving context |
| `/use-mcp this` · `once` · `1` | **the current prompt only** | one quick check, one screenshot, one probe |

No argument → **enables nothing**, just reports the current state and lists the three commands.

### `on` — session-wide

1. Verify the bridge is attached to the **right project**: run `unity status --no-banner`
   (or `mcp__unity-editor-mcp__editor_status`).
2. Compare its `Project` column against the current project's own root (the working
   directory this session opened, not a value copied between projects) and its `Version`
   against the Unity version pinned in `CLAUDE.md` §1. If either does not match →
   **STOP and tell the developer.** The bridge follows the **open Editor**, not the chat's
   working directory — the wrong project open means editing the wrong files.
   Two Editors listed → every later call passes `--project-path`.
3. Match → print `✅ Bridge ON (whole session) — <projectPath> | Unity <version> | port <port>` and proceed.
4. No `ready` instance → ask the developer to open the Unity Editor, **do not retry in a
   loop**. If Unity *is* open, suspect Safe Mode (`unity pipeline list`, `mcp_unity.md` §1).

### `this` / `once` / `1` — single use

Same verification as `on`, but print `✅ Bridge ON (this prompt only)`. **Permission ends with
the prompt**: the next prompt is treated as `off`, even mid-task. If the work turns out to
need several Editor round trips, **tell the developer** so they can type `on` — do not extend it yourself.

"End of prompt" = the final answer of that turn. A message the developer sends mid-turn
does **not** count as a new prompt.

### `off`

Stop calling the bridge immediately and fall back to the table below. Nothing to clean up.
If the developer says "stop running Unity" / "don't use MCP" / "don't use the CLI" mid-task,
treat it as `off` right away without asking again.

## Working With The Bridge Off

| Task | How |
|---|---|
| Edit code | `Read` / `Edit` / `Write` directly on disk |
| Find code | `Grep` / `Glob` — **not** `find_assets` / `search` / `Unity_Grep` |
| Check a Unity API signature | `mcp__unity-api__get_method_signature` — offline, always allowed |
| Check compilation | **Possible without the bridge** — `dotnet build Assembly-CSharp.csproj --no-restore` with the Editor open (plus `Assembly-CSharp-Editor` and each game `.asmdef`'s `.csproj`, listed in `CLAUDE.md` §2.1). Proves compilation only: say *"compiles, not tested"* |
| Read error logs | ask the developer to paste them, or use `mcp__terminal__read_terminal` |
| Read level/asset data | parse the JSON/`.asset` file with `node`/`Read` |
| Run tests | not possible — ask the developer to open the Test Runner (or to type `/use-mcp` for `run_tests`) |
| Screenshots | not possible — ask the developer |

⚠️ **Never infer "it probably compiles".** Not run means not known — and the `dotnet build`
gate above runs without the bridge, so run it rather than hedging. Report *"compiles, not
tested"*, or *"not compiled"* with a reason if the gate genuinely could not run. Never soften
either into a guess.

## Even With The Bridge On — still forbidden

- `unity build`, `unity run`, `unity install`, `unity uninstall`, `unity self-uninstall`
  (`project_setup.md` §3; machine-level changes)
- `set_player_settings`, `set_quality_settings`, `set_tags_layers`, and every other
  `set_*_settings` — they write `ProjectSettings/` (`CLAUDE.md` §10)
- `delete_asset` without the `rules.md` §5 dependency scan; `package_add` / `package_remove`
  without an explicit request
- `eval` used to delete files, edit `ProjectSettings/`, or touch third-party SDK folders

## State Lives In The Conversation, Not On Disk

This skill **writes no config file**. Permission exists only in the session context.
A new session **defaults back to off**.

⚠️ Do not create a flag file (`.claude/.mcp-enabled` or similar). This was considered and
rejected: a flag on disk survives sessions, so enabling once becomes enabling forever —
exactly what the developer wants to avoid. Do not use a hook either: this is a rule the AI
follows on its own.

## Traps

- **Context compaction loses the state.** After a compaction, if there is no trace of
  `/use-mcp on` left in context, treat it as **off** and ask again. Better to ask twice than
  to grant yourself permission. `once` has definitely expired by then.
- **`/mcp-check` is a different skill** — it diagnoses a broken bridge and only reads state,
  so it runs without enabling.
- **Editing `.cs` files while the bridge is off**: Unity reloads them on its own when the
  developer returns to the Editor. There is no need to call the bridge to "kick" a recompile.
- Currently `on` and the developer types `once` → they are **narrowing**: go back to `off`
  after this prompt. Currently `once` and they type `on` → widen to the whole session.
- **Plugin skills (`unity:*`) call `unity command` too.** They do not bypass this rule:
  running a `unity:*` skill that drives the Editor still requires `/use-mcp` first.
