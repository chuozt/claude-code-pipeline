---
name: init-unity-cli
model: claude-opus-5-5
effort: medium
description: First-time setup of the Unity Editor bridge on this machine and project — install the Unity CLI, install com.unity.pipeline into the project, register the CLI's MCP server for the AI clients the developer picks (Claude Desktop by default, plus Claude Code, Codex or others — multi-select; unity mcp configure <client>), leave the in-Editor MCP (unity-mcp / relay_win.exe) and the com.unity.ai.assistant package untouched — this skill never removes either — verify, then update CLAUDE.md, settings.json and the /use-mcp, /mcp-check and related rule files so the agent prefers the Unity CLI + pipeline and keeps the in-Editor MCP as a secondary door. Guides the developer and asks before every change. Use when the developer types /init-unity-cli or says "set up the Unity CLI", "install unity pipeline", "configure unity mcp", "add the Unity CLI to this project", "unity command not found".
---

# /init-unity-cli — set up the Unity CLI bridge

Sets this machine and project up for the **Unity CLI** (`unity`) + **`com.unity.pipeline`**, Unity's
replacement for the in-Editor MCP of `com.unity.ai.assistant`
(https://docs.unity.com/en-us/unity-cli/replace-mcp-server-unity-cli). **It adds; it never
removes.** The in-Editor MCP (`unity-mcp` → `relay_win.exe --mcp`, tools
`mcp__unity-mcp__Unity_*`) and the `com.unity.ai.assistant` package stay exactly as they are:
the CLI is the **preferred** door, the in-Editor MCP the secondary one for what the CLI lacks.
Afterwards the agent drives the Editor with Bash `unity command` (preferred) or the
`mcp__unity-editor-mcp__*` tools, under the rules of `CLAUDE.md` §2.1 and `.claude/mcp_unity.md`.

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
- **Never** `unity upgrade` / `self-update` / `self-uninstall`, never edit `ProjectSettings/`.
- **Never remove or disable the old door**: no edit of any client's MCP registration for
  `unity-mcp` / `relay_win.exe`, no removal of `com.unity.ai.assistant`, no `deny` entry for
  `mcp__unity-mcp` — the team uses both doors. If the developer asks for a removal, it is a
  separate request, done by hand or outside this skill.
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
  the Editor and wait). Step 8 follows its teammate rule below.
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

## Step 4 — register the CLI's MCP server for the developer's AI clients

**Ask first which clients to set up — multi-select** (AskUserQuestion with `multiSelect: true`),
every run, also in teammate mode (registrations are per machine):

| Option | Client id | Default |
|---|---|---|
| Claude Desktop | `claude` | **pre-selected / recommended** — listed first |
| Claude Code | `claude-code` | off — select it to give Claude Code sessions the `mcp__unity-editor-mcp__*` tools |
| Codex (OpenAI Codex CLI / app) | `codex` | off |
| Other… | any id from `unity mcp configure --list` (cursor, vscode, windsurf, …) | off |

No answer → Claude Desktop only. Put one line under the question: Claude Code sessions (the
CLI, the IDE extensions and the desktop app's Code tab) read `~/.claude.json`, not
`claude_desktop_config.json` — without `claude-code` selected they still drive the Editor
through Bash `unity command` (the preferred form anyway), only the `mcp__unity-editor-mcp__*`
tools are missing. Before asking, run the read-only listing so the question can say
which ones are already configured:

```bash
unity mcp configure --list --no-banner
```

Then, **for each selected client, one at a time**: dry run → show the output → ask → run without
`--dry-run` (add `--yes` only when an entry exists and the developer agreed to replace it).

```bash
unity mcp configure <client-id> --dry-run --no-banner
```

What each one writes `[VERIFIED 2026-10-20: dry runs with unity 1.0.0-beta.13 on Windows]`:

| Client | Where | Server name → tool prefix | How |
|---|---|---|---|
| `claude-code` | user's `~/.claude.json` (user scope) | `unity-editor-mcp` → `mcp__unity-editor-mcp__*` | delegates to `claude mcp add --scope user --transport stdio unity-editor-mcp unity mcp` |
| `claude` (Desktop) | `%APPDATA%\Claude\claude_desktop_config.json` (macOS: `~/Library/Application Support/Claude/…`) | `unity` | writes the JSON file directly, `command` = full path of `unity.exe`, `args ["mcp"]` |
| `codex` | `~/.codex/config.toml` | `[mcp_servers.unity]` | writes the TOML directly, same command/args |

The client name for Claude Code is **`claude-code`**; `claude` is Claude **Desktop** — never mix
them up in the question or the commands.

**`claude-code` without the `claude` command on `PATH`** (common with the Claude desktop app):
the delegation fails. Fallback, with the developer's OK: back up `~/.claude.json` to the session
scratchpad, then add under its top-level `"mcpServers"` (a JSON edit that keeps every other key):

```json
"unity-editor-mcp": { "type": "stdio", "command": "unity", "args": ["mcp"], "env": {} }
```

Use the full path to the `unity` executable as `command` if `unity` is not on the `PATH` Claude
Code starts with. Report the backup path.

**Desktop and Codex are outside this project's rules.** `CLAUDE.md`, `.claude/settings.json`
(the read-only allow-list, the hook guards) and `/use-mcp` govern **Claude Code**
only. Say so when either is selected: Claude Desktop has no project rules at all, and Codex reads
`AGENTS.md`, not `CLAUDE.md` — if the team drives this project from Codex, an `AGENTS.md` carrying
the same bridge rules is a separate task to propose, not something this skill writes.

Each client only sees the new server after **it** is restarted — list which ones in the result.

**Optional — Unity's CLI skill per client** (`unity skill install <id>`; ids: `claude-code`,
`claude-desktop`, `codex`, … — `unity skill install --list` shows what is installed). Offer it only
for the selected clients and only where it is not installed yet. **Skip `claude-code` when**
`.claude/settings.json` enables the plugin `unity@claude-plugins-official` — that plugin already
ships `unity:unity-cli`, and two copies just compete. Never run it by default.

## Step 5 — the old in-Editor MCP stays (report only)

Look for an entry running `relay_win.exe --mcp` (or `relay_mac` / `relay_linux` + `--mcp`) in
**every client selected in Step 4, plus Claude Code always**, and the project's `.mcp.json`
(match on the command, not on the name — usually `unity-mcp`):

| Client | Where |
|---|---|
| Claude Code | `~/.claude.json` → `mcpServers` |
| Claude Desktop | `claude_desktop_config.json` → `mcpServers` |
| Codex | `~/.codex/config.toml` |

**Read only — change nothing.** The result just says which clients have it (`present` / `none`).
It is the secondary door (`CLAUDE.md` §2.1): same `/use-mcp` gate as the CLI, used for what the
CLI lacks. Two registrations side by side (`unity-mcp` and `unity-editor-mcp`) are expected.

## Step 6 — `com.unity.ai.assistant` stays (version check only)

`Packages/manifest.json` has no `"com.unity.ai.assistant"` → `⏭ not installed`. Present → keep it
(the Assistant window and the in-Editor MCP depend on it); Step 3 already checked it is **≥ 2.13**.
This skill never removes it, never edits `ProjectSettings/AI.Assistant/`, and does not ask. Wanting
it gone is the team's separate decision, made in the Package Manager.


```bash
unity status --no-banner
unity command editor_status --caller plugin --skill init-unity-cli --no-banner
unity mcp configure --list --no-banner
```

Expected: one `ready` Editor whose `Project` is this checkout and whose version matches
`CLAUDE.md` §1; `editor_status` answers (`compiling: false`, `playMode: stopped`);
every client chosen in Step 4 listed as `✓ configured`; any old relay entry still there, untouched. New servers only appear after each client restarts:
the `mcp__unity-editor-mcp__*` tools in a **new** Claude Code session, the `unity` server in a
restarted Claude Desktop / Codex. Tell the developer which to restart; this session keeps going
with Bash `unity command` meanwhile.

## Step 8 — teach the agent: update `CLAUDE.md` and the `/use-mcp` family

The machine is set up; now the project's own rules must tell every future agent to use the
Unity CLI + pipeline first, with the in-Editor MCP as the secondary door. The canonical text and the exact targets are in
[`references/agent-rules.md`](references/agent-rules.md) — open it now.

1. **Detect** with its *Detection* checks. All pass → `⏭ rules already migrated`; go to 4.
2. **Plan**: list every file and passage the failing checks need (blocks A–I), with
   `<project>` and `<date>` filled in. Show the plan
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
   `com.unity.pipeline` version, and "`com.unity.ai.assistant` <version>: kept — in-Editor MCP is
   the secondary door" (or "not installed") — plus the matching line of `.claude/mcp_unity.md`
   "This project".

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
  4 MCP clients .... claude (Desktop) ✅/⏭/⛔ · claude-code ✅/⏭/— · codex ✅/⏭/— · <other> …
                     (— = not selected)
  5 Old relay MCP .. per client: present (untouched) / none
  6 AI Assistant pkg  ⏭ not installed / kept <version> (never removed)
  7 Verify ......... ✅/⛔
  8 Agent rules .... ✅ migrated (<n> files) / ⏭ already migrated / ⛔ declined — versions recorded ✅/⛔
Next: restart every client changed in 4, and Claude Code (new rules; + mcp__unity-editor-mcp__*
      if claude-code was selected) · the bridge stays OFF until /use-mcp (Claude Code)
```
