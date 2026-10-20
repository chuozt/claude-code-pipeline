# Agent rules for the Unity CLI bridge — reference blocks for /init-unity-cli Step 8

Canonical text that makes a project's agent rules say "one bridge: Unity CLI + `com.unity.pipeline`,
the in-Editor MCP of `com.unity.ai.assistant` is retired". Reference implementations:
e92-buzzybus (2026-10-20, keeps the offline `dotnet build` gate) and e88-busin (2026-10-19).

Placeholders: `<project>` = repository folder name · `<date>` = today (YYYY-MM-DD) ·
`<assistant-sentence>` = see block A. Never put a machine-specific absolute path in any block.

**Merge, don't overwrite.** Each block replaces only the passage named in its *Target*. Keep
everything project-specific around it — especially an offline compile gate (`dotnet build`,
Roslyn `csc`) and its baseline table, project-specific exceptions, and the file's own
section numbering (adjust `§` references if the project numbers differently).

**Built to be dropped into an old project.** The usual case: a project still on the old
in-Editor MCP, which never had this skill; the developer copies only the
`.claude/skills/init-unity-cli/` folder in and runs it once. So a missing passage is **not** a
reason to skip the rule it carries:

| Missing | Do |
|---|---|
| `CLAUDE.md` has no bridge / MCP section | insert block B as a new section right after its "start of session" section (or near the top), and block A's line into its session-start list if it has one |
| no `CLAUDE.md` at all | ask whether to create a minimal one holding blocks A + B; never invent project identity |
| `.claude/skills/use-mcp/` or `.claude/skills/mcp-check/` | create it from `templates/` (blocks F, G) |
| `.claude/mcp_unity.md` | create it from `templates/mcp_unity.md.tmpl` + block D |
| `.claude/settings.json` | create it with only the `permissions` of block E (the `hooks` part only if the hook scripts exist) |
| `rules.md`, `project_setup.md`, `quick-start.md`, `skills-reference.md`, `project-overview`, the session-start hook | these are optional kit files — skip and say so |

Fill `<compile-row>` / `<compile-warning>` in the use-mcp template from what the project
actually has: an offline gate in `CLAUDE.md` (e.g. `dotnet build …`) → "**Possible without the
bridge** — `<that command>`. Proves compilation only: say *"compiles, not tested"*" and "Run the
offline gate rather than hedging; report *"compiles, not tested"*, or *"not compiled"* with a
reason". No gate → "`unity command console_status` (read-only) — meaningful only once the Editor
has recompiled your edit; this project has no offline compile gate" and "Report *"compiles, not
tested"* only when `console_status` was read after the Editor recompiled the change; otherwise
*"not compiled, not tested"*".

---

## Detection — is the project already migrated?

Already migrated when **all** hold (then Step 8 only refreshes versions):

- `CLAUDE.md` contains `/init-unity-cli` and the word `retired` next to `mcp__unity-mcp`
- `.claude/settings.json` `permissions.deny` contains `"mcp__unity-mcp"` and `allow` contains
  `"Bash(unity command editor_status*)"`
- `.claude/skills/use-mcp/SKILL.md` description says the in-Editor MCP is retired **and** its
  `on` steps contain step `0. **Is this machine set up?**`
- `.claude/skills/mcp-check/SKILL.md` and `.claude/mcp_unity.md` exist and mention `/init-unity-cli`
- the session-start hook, if the project has one, contains `Unity CLI bridge readiness`

Each failing check names the blocks to apply. A project that passes only some checks (migrated
by an older version of this skill) gets just the missing blocks.

---

## A. `CLAUDE.md` — §2 "Start Of Every Session", item 1

*Target:* the item saying "Do not touch Unity MCP" (or similar).

```markdown
1. **Do not drive the Unity Editor.** The Unity CLI bridge is OFF by default except for the
   read-only status commands — see §2.1. The old in-Editor MCP (`mcp__unity-mcp__*`) is
   retired: never call it.
```

`<assistant-sentence>` used in block B:
- package kept → `` `com.unity.ai.assistant` (≥ 2.13, so it does not conflict with the CLI) may stay installed for the Assistant window only. ``
- package removed in Step 6 → `` `com.unity.ai.assistant` was removed on <date>. ``
- never installed → drop the sentence.

## B. `CLAUDE.md` — §2.1 bridge section

*Target:* from the `## 2.1` heading down to (not including) the offline compile-gate paragraph
("Compilation IS verifiable…") if the project has one; otherwise down to the closing
"See something worth checking through Unity → say so and stop" paragraph, which stays.
If the project had no compile gate, keep its existing "not compiled, not tested" wording.

```markdown
## 2.1 ⛔ Unity Editor bridge (Unity CLI) — OFF BY DEFAULT

This project has **one** bridge: the **Unity CLI** (`unity`) talking to the open Editor through
the `com.unity.pipeline` package. Two ways to call it, same gate, same rules:

- **Bash `unity command <name> --<param> <value>` — preferred** (cheaper on context). Pass
  `--caller plugin --skill <skill>` and `--no-banner` on every call (`--format json` when
  parsing the result).
- **MCP tools `mcp__unity-editor-mcp__<name>`** — the CLI's own MCP server (`unity mcp`),
  registered per machine by `unity mcp configure claude-code`. Equivalent to the Bash form;
  use it when the Bash form is unavailable. Never drive the same scene through both in one task.

Not set up on this machine yet → the developer types `/init-unity-cli`. **`unity` not found**
(common for a teammate who pulled a project someone else set up — the CLI and the MCP
registration are per machine, not in git) → even the read-only commands below are unavailable:
ask for pasted logs or read `mcp__terminal__read_terminal`, suggest `/init-unity-cli` **once**,
and never retry the CLI in a loop or ask the developer to open the Editor for it. The full CLI reference
is the `unity:unity-cli` skill; the project rules on top of it are in `.claude/mcp_unity.md`.

The old in-Editor MCP of `com.unity.ai.assistant` (`mcp__unity-mcp__Unity_*`, server
`unity-mcp` → `relay_win.exe --mcp`) is **retired** (switched <date>; Unity deprecated it in
favour of the CLI — https://docs.unity.com/en-us/unity-cli/replace-mcp-server-unity-cli).
**Never call it**, `/use-mcp` or not; `settings.json` denies it. <assistant-sentence>

**Always allowed** (read-only, they change nothing in the Editor):

- `unity status`, `unity doctor`, `unity --version`, `unity pipeline list`,
  `unity mcp configure --list`, bare `unity command` / `unity command --tag <tag>` (the listing)
- `unity command editor_status`, `console_status`, `console` (always with `--tail` / `--level`),
  `recompile_status`, `test_status`, `list_tests`, `list_open_scenes` — and their
  `mcp__unity-editor-mcp__*` twins
- the offline API lookup `mcp__unity-api__*`, if installed

**Everything else is forbidden until the developer types `/use-mcp on`** (whole session) or
**`/use-mcp once`** (exactly one prompt, then off again): `eval` / `eval_file` / `run_script`,
`recompile`, `run_tests`, `menu`, Play mode, scene / GameObject / asset / prefab commands,
captures, `get_scene_hierarchy` and the other inspection commands that flood the context. That
includes "just a quick check", bug reports, and wanting to confirm a build.

**Never, even with the bridge on:** `unity build` / `run` / `install` / `uninstall` /
`upgrade` / `self-uninstall` (the guard hook blocks them), every `set_*_settings` command
(`ProjectSettings/`, §10), `package_add` / `package_remove` unasked, `delete_asset` or file
deletion through `eval` without the reference scan.

**Right Editor, right checkout.** `unity status` must show this session's own working directory
in its `Project` column (never hard-code a path; two Editors listed → every call passes
`--project-path "<path>"`). A session in a git worktree never uses the bridge.

**Editor in Safe Mode** (compile errors): `unity status` / `unity command` cannot connect.
Confirm with `unity pipeline list`, fix the C# on disk, and ask the developer to restart Unity.

Without the bridge: edit code with `Read`/`Edit`/`Write`, search with `Grep`/`Glob`,
read data by parsing files.
```

## C. `CLAUDE.md` — small edits elsewhere

| Where | Replace with |
|---|---|
| §3 table row for `mcp_unity.md` | `` \| `.claude/mcp_unity.md` \| Unity Editor bridge (Unity CLI) handbook — **read only after `/use-mcp`** · first-time machine setup: `/init-unity-cli` \| `` (keep a leading `@` if the project's table uses one) |
| §4 bullet "Bug reported → demand real logs…" | `- Bug reported → demand **real logs**, NO GUESSING. Read the Unity console with` / `` `unity command console --level error --tail 50` (always allowed, §2.1), ask for pasted logs, `` / `` or read `mcp__terminal__read_terminal`; if reproducing it needs Play mode or `eval`, `` / `**ask the developer to type `/use-mcp`** rather than doing it.` |
| §5 "a one-line `eval` probe" | "a one-line `unity command eval` probe" |

## D. `.claude/mcp_unity.md`

No such file → create it from `templates/mcp_unity.md.tmpl` (fill `<project>`, `<date>`) and put
the section below where its HTML comment says; steps 2–3 are then already done.

1. Insert after the intro blockquote, before `## 0.` (replace an existing "This project" section;
   mention `/init-unity-cli` in it — the Detection check looks for it):

```markdown
## This project (<project>) — read first

- **One bridge: the Unity CLI** through `com.unity.pipeline`. Switched <date> from the
  in-Editor MCP of `com.unity.ai.assistant` (`mcp__unity-mcp__Unity_*`, server `unity-mcp` →
  `relay_win.exe --mcp`), which Unity deprecated: that door is **retired — never call it**;
  `settings.json` denies `mcp__unity-mcp`. <assistant-sentence>
- **Machine setup**: the developer types `/init-unity-cli`. Versions in use are recorded in
  `project_setup.md` (row *Editor bridge*); if a command listed here is missing, check the bare
  `unity command` listing before assuming it exists.
- **Preferred form: Bash `unity command`** with `--caller plugin --skill <skill> --no-banner`
  (`--format json` when parsing). The `mcp__unity-editor-mcp__*` tools are the same commands
  behind the same gate — use them only when Bash is unavailable, never both on the same scene.
- **Off by default**, on only after `/use-mcp on` / `once` — except the read-only commands listed
  in `CLAUDE.md` §2.1.
- **Never hard-code the project path**: compare the `Project` column of `unity status` with the
  working directory this session opened. A session in a git worktree never uses the bridge. Two
  Editors listed → every call passes `--project-path "<path>"` (quoted).
- **Before every Editor action:** `unity command editor_status` → `status: ready`,
  `compiling: false`, `domainReloadInProgress: false`, `playMode: stopped`, `projectPath` = this
  checkout. Never act during the developer's Play session.
- **Compile:** offline gate first if `CLAUDE.md` §2.1 has one. With the bridge on:
  `unity command recompile`, poll `recompile_status` until `completed` / `up_to_date`, then
  `console_status` → `groundTruth.compilationFailed: false`.
- **Assets** are changed through `SerializedObject` in a command or an Editor script, never as
  YAML text. After `AssetDatabase.SaveAssets()` read `git diff` and leave out what is not yours.
```

2. §0 door table: the "MCP tools (in-Editor)" row's last cell →
   `**retired in <project> (<date>) — never call, even with `/use-mcp`**`. Make the read-only
   diagnostics row list `unity pipeline list` and `--tag`, and add the row
   `| Bash CLI, read-only Editor state | `unity command editor_status`, `console_status`, `console` (`--tail`/`--level`), `recompile_status`, `test_status`, `list_tests`, `list_open_scenes` (and their `mcp__unity-editor-mcp__*` twins) | no — read state only, small output |`
3. Under the "in-Editor door" heading, add:
   `> **Retired in <project> on <date>** — kept as history. The Play-mode notes below still apply to CLI `eval`; the `Unity_*` tool quirks do not.`

## E. `.claude/settings.json` (JSON edit — keep every other key, validate after)

- `hooks.PreToolUse`: the matcher `"Bash"` → `"Bash|PowerShell"` (only the entry that runs
  `guard.sh pre-bash`); the eval matcher →
  `"mcp__unity-editor-mcp__eval|mcp__unity-editor-mcp__eval_file|mcp__unity-editor-mcp__run_script"`
  (drop `mcp__unity-mcp__Unity_RunCommand`).
- `permissions.allow` — add the missing ones:
  `Bash(unity --version*)`, `Bash(unity status*)`, `Bash(unity doctor*)`,
  `Bash(unity mcp configure --list*)`, `Bash(unity pipeline list*)`, `Bash(unity command)`,
  `Bash(unity command --no-banner)`, `Bash(unity command --tag *)`,
  `Bash(unity command editor_status*)`, `Bash(unity command console_status*)`,
  `Bash(unity command console --*)`, `Bash(unity command recompile_status*)`,
  `Bash(unity command test_status*)`, `Bash(unity command list_tests*)`,
  `Bash(unity command list_open_scenes*)`, `mcp__unity-editor-mcp__editor_status`,
  `mcp__unity-editor-mcp__console_status`, `mcp__unity-editor-mcp__recompile_status`,
  `mcp__unity-editor-mcp__test_status`, `mcp__unity-editor-mcp__list_tests`,
  `mcp__unity-editor-mcp__list_open_scenes`
- `permissions.deny` — replace every `mcp__unity-mcp__…` entry with the single `"mcp__unity-mcp"`;
  make sure `Bash(unity build*)`, `Bash(unity run*)`, `Bash(unity install*)`,
  `Bash(unity uninstall*)`, `Bash(unity self-uninstall*)`, `Bash(unity upgrade*)`, the
  `Bash(unity command set_*_settings*)` family and `Edit(**/Packages/com.unity.pipeline/**)` are there.
- `permissions.ask` — `Bash(unity command delete_asset*)`, `mcp__unity-editor-mcp__delete_asset`.

## F. `.claude/skills/use-mcp/SKILL.md` — install from template

Source: `templates/use-mcp.SKILL.md.tmpl`. Fill `<project>`, `<date>`, `<compile-row>`,
`<compile-warning>` (see *Built to be dropped into an old project*), write it as
`.claude/skills/use-mcp/SKILL.md`.

`use-mcp` is a kit file, so the template **replaces** the old one — but first diff the old file
against the template and carry over anything project-specific it had (an extra mode, a project
exception, a different coding-rule file, section references like `project_setup.md §3`). Show
what was carried over in the plan. The old file still describing `mcp__unity-mcp__Unity_*` as a
usable door, or lacking step 0, is exactly what gets replaced — that is not "project-specific".

## G. `.claude/skills/mcp-check/SKILL.md` — install from template

Source: `templates/mcp-check.SKILL.md.tmpl`; same procedure as F (diff, carry project-specific
additions over, replace). If the project's `CLAUDE.md` numbers its bridge section differently
from §2.1, fix the `§` reference while filling it in.

## H. Other files (optional kit files — only where the file exists)

| File | Change |
|---|---|
| `.claude/rules.md` "MCP is OFF by default" paragraph | "Bash `unity command|eval|test|open` calls and their `mcp__unity-editor-mcp__*` twins are forbidden until `/use-mcp`, except the read-only status commands `CLAUDE.md` §2.1 lists. The only bridge is the Unity CLI + `com.unity.pipeline`; the in-Editor MCP (`mcp__unity-mcp__Unity_*`) is **retired — never call it**. Machine setup: `/init-unity-cli`." |
| `.claude/project_setup.md` row *Editor bridge* | `**Unity CLI** + `com.unity.pipeline` (decided <date>)` + versions (Step 8 record) |
| `.claude/project_setup.md` new-project order, the "install Unity CLI…" item | "run `/init-unity-cli` (install Unity CLI, `unity pipeline install`, `unity mcp configure claude-code`, retire the old in-Editor MCP), and confirm with `unity status`…" |
| `.claude/docs/quick-start.md` | add row `Set up Unity CLI + pipeline + MCP on this machine (first time)` → `/init-unity-cli`; rewrite "bridge is OFF by default" paragraph to the CLI-only wording |
| `.claude/docs/skills-reference.md` | add `/init-unity-cli` row; `/mcp-check` and `/use-mcp` rows say "Unity CLI bridge" instead of "Unity MCP" |
| `.claude/skills/project-overview/SKILL.md` | "No `mcp__unity-editor-mcp__*` tool (the old `mcp__unity-mcp__Unity_*` door is retired anyway)" |

## I. `.claude/hooks/session-start.sh` (only if the project has this hook)

Insert before the "previous session state" part (or before the closing `echo "===…"` line).
Shell file: keep LF endings (`.claude/.gitattributes`); edit with `Edit`.

```bash
# Unity CLI bridge readiness on THIS machine. The project's rules and com.unity.pipeline come
# with git; the CLI binary and the MCP registration do not — a teammate who pulled a project
# someone else set up has neither. Prints only when something is missing.
if [ -f Packages/manifest.json ] && grep -q '"com.unity.pipeline"' Packages/manifest.json; then
    if ! command -v unity >/dev/null 2>&1 \
        && [ ! -x "${LOCALAPPDATA:-/nonexistent}/Unity/bin/unity.exe" ] \
        && [ ! -x "$HOME/.local/bin/unity" ]; then
        echo ""
        echo "!! Unity CLI not installed on this machine — the Editor bridge is unavailable"
        echo "   (read-only console/status commands too). The project already uses it:"
        echo "   suggest /init-unity-cli to the developer; until then ask for pasted logs."
    fi
elif [ -f Packages/manifest.json ] && grep -q '/init-unity-cli' CLAUDE.md 2>/dev/null; then
    echo ""
    echo "!! CLAUDE.md expects the Unity CLI bridge but com.unity.pipeline is not in"
    echo "   Packages/manifest.json — suggest /init-unity-cli to the developer."
fi
```

Detection for this block: the hook file contains `Unity CLI bridge readiness`.

## After applying

- `grep -rn "unity-mcp" CLAUDE.md .claude --include=*.md --include=*.json` (skip `.claude/kb/`):
  every remaining hit must be marked retired / denied / history.
- `.claude/settings.json` must still parse as JSON.
- Edit the files with the `Edit` tool, not a Bash heredoc or script: the guard hook blocks Bash
  command text that quotes the forbidden CLI verbs, and this file quotes them.
