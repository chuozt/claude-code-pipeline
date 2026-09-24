# Quick Start

From an empty Unity project to generated, measured levels.

---

## 0. Install (once per project)

```
1. Copy CLAUDE.md and .claude/ into the Unity project root
2. Delete .claude/settings.local.json if it came along, and gitignore it
3. Open Claude in the project and type:  /project-overview
4. Fill the <...> placeholders in CLAUDE.md, project_setup.md and rules.md from that
   snapshot — by hand, or by asking Claude to draft them and confirming before it writes
```

`/project-overview` is **read-only**: it scans the project and reports the detected stack,
structure and pipeline progress — it asks nothing, touches no Unity Editor bridge, and
writes nothing. The working folder scaffold (`design/`, `production/`, `docs/`) is not
created by it either — each folder is created the first time a skill actually needs it.

---

## 1. Where am I?

| Situation | Start with |
|---|---|
| Brand new idea, nothing written | `/start` |
| A GDD or concept doc already exists | `/map-systems` |
| Code exists but no documentation | `/reverse-document` |
| Design done, want to generate levels | `/gd-workflow-help` |
| Not sure | `/gd-workflow-help` — it detects which outputs exist and marks the next step |

---

## 2. The phases — 1A/1B/2/3/4

```
Phase 1A idea → architecture      map-systems · design-system · create-architecture
         (developer track)        (start · brainstorm are standalone, not tracked steps here)

Phase 1B define difficulty        gd-mode · gd-map-flow · gd-core-difficulty
         (designer track)         gd-mechanic-difficulty · gd-mechanic-object-mix
                                  gd-mechanic-mix · gd-level-definition · gd-level-intent

         ⭐ 1A and 1B run in parallel — neither reads the other's output.

Phase 2  teach a bot (optional)   gd-bot-playstyle · gd-prototype-sim
                                  needs BOTH 1A and 1B finished first

Phase 3  generate and measure     gd-level-gen · gd-level-audit

Phase 4  calibrate                gd-calibrate → loop back to phase 3
```

Full table with expected output per command: `/gd-workflow-help`.

**The four ordering rules that matter:**

1. 1A and 1B are independent — run them at the same time, by different people if there are two.
2. `/gd-map-flow` comes first within 1B — without the model, no later skill in that track has
   a shared vocabulary.
3. Both mix matrices before `/gd-level-definition` — object scale and level scale are
   different questions.
4. Phase 2 gates phase 3 — no bot means no measurement, and a number from an ignorant bot is
   worse than no number.

Everything else can be reordered or skipped.

---

## 3. What gets written where

| Path | Written by | Holds |
|---|---|---|
| `design/gdd/` | `brainstorm`, `map-systems`, `design-system` | the concept, the systems index, per-system GDDs |
| `design/pipeline/` | the `gd-*` skills | flow map, difficulty model, mechanic files, mix matrices, level definition |
| `docs/architecture/` | `create-architecture`, `architecture-decision` | the architecture blueprint and ADRs |
| `docs/features/` | developers | living per-feature documentation next to the code |
| `docs/_session/active.md` | every session | the session state file — read this first after any interruption |
| `production/` | production skills | stage, review mode, QA evidence |

Each folder above is created the first time a skill actually needs it — `/project-overview`
only reports whether the tree already exists.

---

## 4. Everyday commands

| Need | Command |
|---|---|
| Talk in designer language, not implementation | `/gd-mode` |
| Write or review a mechanic doc | `/gd-mechanic-doc` |
| Add juice without touching logic | `/gd-feel` |
| Find performance bottlenecks | `/gd-perf` |
| Debug something | `/gd-bug` |
| Review before committing | `/gd-review` |
| Commit | `/commit` |
| Enable the Unity Editor bridge (CLI/MCP) for one prompt | `/use-mcp this` |

Full list: `.claude/docs/skills-reference.md`.

---

## 5. Two things that trip people up

**The Unity Editor bridge is OFF by default.** Every `mcp__unity-editor-mcp__*` and
`mcp__unity-mcp__Unity_*` tool and every Bash `unity command` / `unity eval` is forbidden until you type `/use-mcp on` (whole
session) or `/use-mcp this` (one prompt). With it off, Claude still checks compilation with
`dotnet build` but cannot run tests or read the console — it will say so plainly rather than guess.

**Claude asks before writing.** Every task goes gather → propose options with trade-offs →
you decide → blueprint → approval → code → verification. If it starts writing files without
asking, something is wrong with the rule loading — check that `CLAUDE.md` is at the project root.
