---
name: init-unity-cli
model: claude-opus-5-5
effort: medium
description: First-time setup of the Unity Editor bridge on this machine and project — install the Unity CLI, install com.unity.pipeline into the project, register the CLI's MCP server for Claude Code (unity mcp configure claude-code), retire the old in-Editor MCP (unity-mcp / relay_win.exe), optionally remove the com.unity.ai.assistant package (asked every run, default keep), verify, then update CLAUDE.md, settings.json and the /use-mcp, /mcp-check and related rule files so the agent uses the Unity CLI + pipeline. Guides the developer and asks before every change. Use when the developer types /init-unity-cli or says "set up the Unity CLI", "install unity pipeline", "configure unity mcp", "replace the Unity MCP server", "unity command not found".
---

# /init-unity-cli — set up the Unity CLI bridge

Moves this machine and project from the deprecated in-Editor MCP of `com.unity.ai.assistant`
(`unity-mcp` → `relay_win.exe --mcp`, tools `mcp__unity-mcp__Unity_*`) to the **Unity CLI**
(`unity`) + **`com.unity.pipeline`**, following Unity's migration guide
https://docs.unity.com/en-us/unity-cli/replace-mcp-server-unity-cli. Afterwards the agent drives
the Editor with Bash `unity command` (preferred) or the `mcp__unity-editor-mcp__*` tools, under
the rules of `CLAUDE.md` §2.1 and `.claude/mcp_unity.md`.

## Self-contained — drop it into any project

This folder carries everything it needs; nothing else has to exist in the target project:

```
.claude/skills/init-unity-cli/
  SKILL.md                         this procedure
  references/agent-rules.md        detection + text blocks A–I for CLAUDE.md, settings.json, …
  templates/use-mcp.SKILL.md.tmpl  full /use-mcp skill (CLI-only)
  templates/mcp-check.SKILL.md.tmpl full /mcp-check skill (CLI-only)
  templates/mcp_unity.md.tmpl      minimal bridge handbook, when the project has none
```

For a project still on the old in-Editor MCP: copy this folder into its `.claude/skills/`, start
a new Claude Code session there (so the skill is picked up), and type `/init-unity-cli`. One run
sets up the machine (Steps 1–7) **and** rewrites the project's agent rules (Step 8) — `CLAUDE.md`,
`settings.json`, `/use-mcp`, `/mcp-check`, `mcp_unity.md`, `rules.md`, `project_setup.md`,
`project-overview`, the docs and the session-start hook — creating `/use-mcp`, `/mcp-check` and
`mcp_unity.md` from the templates when the project lacks them.

**The old project's rules forbid every `unity` call** (they predate this skill). The developer
typing `/init-unity-cli` is the authorization for exactly the commands this skill lists — the
read-only checks and the setup steps, each change still confirmed — nothing else. It does not
turn the bridge on: `/use-mcp` keeps gating every Editor action.

## Ground rules for this skill

- **Setup, not use.** This skill never drives the Editor: no `eval`, `recompile`, `menu`, Play
  mode, scene/asset commands. Only the read-only commands `CLAUDE.md` §2.1 always allows
  (`unity --version`, `unity status`, `unity pipeline list`, `unity mcp configure --list`,
  `unity command editor_status` / `console_status`) plus the setup steps below.
- **Every step that changes something is confirmed first**, one at a time, by showing the exact
  command and what it touches: the package manifest, the user's `~/.claude.json`, the user's
  `~/.claude/skills/`, the project's rule files (Step 8 — one approval for its whole change
  set). A "yes" covers that step only.
- **The AI never installs the CLI itself** (downloading and running an installer is the
  developer's action). It prints the command for the developer to run in their own terminal.
- **Never** `unity upgrade` / `self-update` / `self-uninstall`, never edit `ProjectSettings/`,
  never remove `com.unity.ai.assistant` without the explicit "remove" answer of Step 6 (the
  Assistant window may still be used).
- The guard hook blocks Bash command *text* that contains some CLI verbs — run each command on
  its own, never inside a heredoc that quotes docs.
- Report each step as `✅ done` / `⏭ already OK` / `⛔ needs the developer` — never "probably".

## Mode — first setup or teammate?

Decide before Step 1, from files only (Grep, no bridge):

- **Teammate mode** — `Packages/manifest.json` already has `"com.unity.pipeline"` **and**
  `references/agent-rules.md` *Detection* passes. Someone set the project up and pushed it; this
  machine only needs its own pieces (CLI binary, MCP registration — neither travels with git).
  Say so in one line: *"Project already set up for the Unity CLI — only this machine needs
  setting up."* Then run Steps 1, 2, 4, 5, 7; Step 3 is `⏭` (Unity imports the package on its
  own after the pull — if `pipeline list` shows it not loaded yet, ask the developer to focus
  the Editor and wait). Step 6 and Step 8 follow their teammate rules below.
- **First setup** — anything else. Run every step.

Work through the steps in order; skip a step whose check already passes.
`/init-unity-cli rules` runs **Step 8 only**: it touches no machine and needs no Editor, for
bringing a project's rules over (e.g. a freshly copied kit) on a machine that is already set up.

## Step 1 — Unity CLI installed?

```bash
unity --version --no-banner
```

- Prints a version → `⏭`, record it (e.g. `1.0.0-beta.13`).
- `command not found` → print for the developer (pick their OS), then wait:

  | OS | Command (run by the developer) |
  |---|---|
  | Windows | `winget install Unity.CLI` |
  | macOS / Linux | `curl -fsSL https://unity.com/install.sh \| bash` |
  | macOS / Linux (Homebrew) | `brew install --cask unity-cli` |

  `[VERIFIED 2026-10-20: docs.unity.com/en-us/unity-cli/use-unity-cli]`. Then **reopen the
  terminal / restart Claude Code** so the new `PATH` is seen, and run `unity --version` again.
  On Windows the per-user install lives in `%LOCALAPPDATA%\Unity\bin` — if the shell still
  cannot find it, call it by full path. Updating later is `winget upgrade Unity.CLI` (or
  `brew upgrade unity-cli`), run by the developer.

## Step 2 — Editor open on this project?

```bash
unity status --no-banner
unity pipeline list --no-banner
```

Unity must be open on **this** checkout (the session's working directory) with the version
pinned in `CLAUDE.md` §1. Not open → ask the developer to open it, do not loop. `pipeline list`
shows whether the Pipeline package is loaded per Editor; `Safe Mode` → compile errors, fix
those first (`mcp_unity.md` §1). Step 3 can still run with the Editor closed (it edits the
manifest), but Steps 6–7 need it open.

## Step 3 — `com.unity.pipeline` in the project?

Check `Packages/manifest.json` for `"com.unity.pipeline"` (Grep, no bridge needed).

- Present → `⏭`, record its version.
- Missing → show and ask:

  ```bash
  unity pipeline install --project-path "<this session's working directory>" --no-banner
  ```

  It adds the package to `Packages/manifest.json` (latest version by default; the developer may
  ask for `--package-version <v>` — `unity pipeline list-versions` lists them, e.g. to match
  another project). After it runs: `git diff -- Packages/` must show only the
  `com.unity.pipeline` line in `manifest.json` (and its `packages-lock.json` entry once Unity
  resolves). Anything else changed → stop and report. Unity resolves the package when it next
  gets focus; ask the developer to switch to the Editor and wait for the import.
- Also check `com.unity.ai.assistant`: if present it must be **≥ 2.13** (older versions conflict
  with the CLI). Lower → report it and ask whether to update; never update it silently.

## Step 4 — register the CLI's MCP server for Claude Code

```bash
unity mcp configure --list --no-banner
unity mcp configure claude-code --dry-run --no-banner
```

The client name for Claude Code is **`claude-code`** (`claude` is Claude Desktop). The dry run
prints what it will do — on this CLI version
`claude mcp add --scope user --transport stdio unity-editor-mcp unity mcp`. Show it, ask, then
run without `--dry-run` (add `--yes` only if an entry already exists and the developer agreed to
replace it).

**If the `claude` command is not on `PATH`** (common with the Claude desktop app), the delegation
fails. Fallback, with the developer's OK: back up `~/.claude.json` to the session scratchpad,
then add under its top-level `"mcpServers"` (a JSON edit that keeps every other key):

```json
"unity-editor-mcp": { "type": "stdio", "command": "unity", "args": ["mcp"], "env": {} }
```

Use the full path to the `unity` executable as `command` if `unity` is not on the `PATH` Claude
Code starts with. Report the backup path.

Optional: `unity skill install claude-code` copies Unity's CLI skill into
`~/.claude/skills/unity-cli/`. **Skip it when** `.claude/settings.json` enables the plugin
`unity@claude-plugins-official` — that plugin already ships `unity:unity-cli`, and two copies
just compete. Mention it, do not run it by default.

## Step 5 — retire the old in-Editor MCP

Look for an `mcpServers` entry running `relay_win.exe --mcp` (usually named `unity-mcp`) in the
user's `~/.claude.json` and the project's `.mcp.json`.

- None → `⏭`.
- Found in `~/.claude.json` → it is **user scope, so every project on this machine sees it**.
  Say so, then ask: `claude mcp remove unity-mcp -s user` (or, without the `claude` command,
  remove that one key from `mcpServers` after backing the file up). Projects that still use it
  would lose it — the developer decides.
- Either way, this project's `.claude/settings.json` already denies `mcp__unity-mcp`, so the
  retired tools cannot be called here even if the entry stays.
- Optionally the developer can switch off the MCP server in the Editor (Assistant / AI
  settings). That writes `ProjectSettings/` — the developer does it by hand, never the AI.

## Step 6 — remove `com.unity.ai.assistant`? (optional, default: KEEP)

Skip silently when `Packages/manifest.json` has no `"com.unity.ai.assistant"` (`⏭ not installed`).
Otherwise **always ask** — every run, never assume a previous answer — with **Keep** as the
default/first option. No answer, an unclear answer, or "skip" = keep.

**Teammate mode:** the project was already set up and the package was deliberately kept, so
removing it is a **team decision**, not this machine's. Ask with that stated up front
(*"This changes the shared manifest for everyone — has the team agreed?"*); proceed only on
an explicit "remove" **plus** a confirmation that it was agreed. Otherwise keep.

Put the trade-off in the question:

| | Keep (default) | Remove |
|---|---|---|
| Assistant window / Unity AI features in the Editor | still work | gone for everyone on the project |
| Retired MCP door | already denied in `settings.json`, harmless | cannot come back at all |
| CLI conflict | none if ≥ 2.13 (Step 3) | none |
| Team impact | none | a shared `Packages/manifest.json` change — every teammate's Editor drops it on pull |

**Only on an explicit "remove":**

1. **Reference scan first** (Grep, no bridge): `Unity.AI.Assistant`, `Unity.AI.` and
   `com.unity.ai.assistant` in `Assets/**/*.cs`, `Assets/**/*.asmdef`, `Packages/` (embedded /
   local packages), and every other package's `dependencies` in `Packages/packages-lock.json`.
   Do **not** confuse it with `com.unity.ai.navigation` (NavMesh) — that one stays. Any hit →
   `⛔`, list the hits, keep the package.
2. **Remove the single line** `"com.unity.ai.assistant": "<version>",` from
   `Packages/manifest.json` with the `Edit` tool (mind the trailing comma of the line above when
   it was the last entry). Never rewrite, truncate or regenerate the whole manifest (the guard
   hook blocks that), and never hand-edit `packages-lock.json` — Unity rewrites it. The developer
   may prefer to do it themselves: *Window → Package Manager → In Project → AI Assistant →
   Remove*; that is equally fine. `unity command package_remove` is a bridge mutation — only if
   the developer has `/use-mcp` on.
3. Ask the developer to focus the Editor so the Package Manager re-resolves. Then
   `unity command console_status --caller plugin --skill init-unity-cli --no-banner` →
   `compilationFailed: false`, and `git diff -- Packages/` must show only the removed manifest
   line and the matching `packages-lock.json` entry. Compile errors or other changes → restore
   the line with `Edit`, report, stop.
4. Leftovers such as `ProjectSettings/AI.Assistant/` (if present) are **not** deleted by the AI
   (`CLAUDE.md` §10) — mention them; the developer decides.
5. With the package gone, the old relay MCP of Step 5 has nothing to talk to in this project —
   if it was kept there, remind the developer it is now dead weight here.

## Step 7 — verify

```bash
unity status --no-banner
unity command editor_status --caller plugin --skill init-unity-cli --no-banner
unity mcp configure --list --no-banner
```

Expected: one `ready` Editor whose `Project` is this checkout and whose version matches
`CLAUDE.md` §1; `editor_status` answers (`compiling: false`, `playMode: stopped`);
`claude-code` listed as configured. The `mcp__unity-editor-mcp__*` tools only appear in a
**new** Claude Code session — tell the developer to restart it; this session keeps going with
Bash `unity command` meanwhile.

## Step 8 — teach the agent: update `CLAUDE.md` and the `/use-mcp` family

The machine is set up; now the project's own rules must tell every future agent to use the
Unity CLI + pipeline and never the retired door. The canonical text and the exact targets are in
[`references/agent-rules.md`](references/agent-rules.md) — open it now.

1. **Detect** with its *Detection* checks. All pass → `⏭ rules already migrated`; go to 4.
2. **Plan**: list every file and passage the failing checks need (blocks A–I), with
   `<project>`, `<date>` and `<assistant-sentence>` filled in from Steps 3 and 6. Show the plan
   as one change set and ask **once** for the whole set (`CLAUDE.md` §4 — "May I write these
   changes to …?"). No → `⛔ rules not updated`, the result tells the developer the agent will
   keep following the old rules.
3. **Apply** with `Edit` — merge into the existing text, never overwrite whole project files;
   keep the project's own offline compile gate, exceptions and numbering (see the reference's
   *Merge, don't overwrite*). A passage or file that is missing is inserted or created as the
   reference's *Built to be dropped into an old project* table says — `/use-mcp` and
   `/mcp-check` come from `templates/` (old copies replaced after carrying their
   project-specific parts over). Then run its *After applying* checks: the `unity-mcp` grep, and
   `.claude/settings.json` still parses.
4. **Record versions** (part of the same approved set, or asked separately if 1 skipped
   straight here): `.claude/project_setup.md` row *Editor bridge* — `unity` CLI version,
   `com.unity.pipeline` version, and either "`com.unity.ai.assistant` <version>: MCP retired,
   package kept for the Assistant window" or "`com.unity.ai.assistant` removed <date>" — plus
   the matching line of `.claude/mcp_unity.md` "This project". If the package was removed in
   Step 6, make sure `CLAUDE.md` §2.1 and `mcp_unity.md` no longer say it "may stay installed".

**Teammate mode:** the rules are migrated (1 is `⏭`) and the versions were recorded by whoever
set the project up. Do **not** rewrite them with this machine's versions. If this machine's CLI
or the resolved pipeline version differs from what `project_setup.md` records, report the
difference and ask; write only on a yes. Nothing to change → nothing to commit from this run.

Never write a machine-specific absolute path into a committed file. `settings.json` changes,
the `CLAUDE.md` text and the replaced `/use-mcp` / `/mcp-check` take effect in the **next** Claude
Code session — until then, do not use the bridge in this session beyond what this skill lists. Committing is a separate
request (`/commit`): one commit carrying `Packages/manifest.json`, `Packages/packages-lock.json`,
`CLAUDE.md` and the `.claude/` changes together.

## Result format

```
Unity CLI bridge — <project>, branch <branch> — mode: first setup | teammate
  1 CLI ............ ✅/⏭/⛔ <version>
  2 Editor ......... ✅/⏭/⛔ <ready on this checkout | what is wrong>
  3 Pipeline pkg ... ✅/⏭/⛔ <version> (ai.assistant <version>)
  4 MCP (claude-code) ✅/⏭/⛔ unity-editor-mcp
  5 Old relay MCP .. ✅ removed / ⏭ none / kept (denied in this project)
  6 AI Assistant pkg  kept (default) / ✅ removed / ⏭ not installed / ⛔ <references found>
  7 Verify ......... ✅/⛔
  8 Agent rules .... ✅ migrated (<n> files) / ⏭ already migrated / ⛔ declined — versions recorded ✅/⛔
Next: restart Claude Code (new rules + mcp__unity-editor-mcp__*) · the bridge stays OFF until /use-mcp
```
