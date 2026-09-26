# Unity AI Pipeline Kit

A portable rule set, knowledge base, and skill library for running Claude on a
Unity mobile game project. Copy `CLAUDE.md` + `.claude/` into the root of a new
Unity project and run `/project-overview`.

---

## First Run

```
1. Copy CLAUDE.md and .claude/ into the Unity project root
2. Add .claude/settings.local.json to .gitignore   (see "Never Share" below)
3. Open Claude in the project and type:  /project-overview
4. Fill the <...> placeholders in CLAUDE.md, project_setup.md and rules.md
   from that snapshot — by hand, or by asking Claude to draft them and confirming
```

`/project-overview` is **read-only**: it scans the project and reports what it found —
detected stack, structure, pipeline docs progress — but asks nothing, touches no Unity
Editor bridge, and writes nothing. Filling the placeholders is a separate, explicit step.

---

## Load Order

Claude reads these automatically or on demand, in this order of authority:

| Order | File | Role |
|---|---|---|
| 1 | `CLAUDE.md` | Project identity + the non-negotiable rules. Loaded every session. |
| 2 | `.claude/rules.md` | General rules, multi-developer file ownership, git rules. |
| 3 | `.claude/rules/*.md` | Path-scoped rules, applied automatically when a matching file is edited. |
| 4 | `.claude/coding_convention.md` | **The single source of truth for C# standards.** |
| 5 | `.claude/*.md` (rest) | Workflow, architecture, verification, context, anti-patterns. |
| 6 | `.claude/kb/INDEX.md` | Knowledge base index — open individual documents only when needed. |

**Rule of thumb:** if two documents disagree, the one higher in this table wins.
If you find a real contradiction, fix the lower document rather than adding a third.

---

## Directory Map

| Path | Contents |
|---|---|
| `.claude/skills/` | Slash commands. `gd-*` are the game-design pipeline; the rest are general. |
| `.claude/agents/` | Specialist agent role definitions. |
| `.claude/rules/` | Path-scoped rules (gameplay, UI, editor tools, data, tests, docs). |
| `.claude/kb/` | Unity 6 + middleware reference. `kb/unity/` is the main set, `kb/goc/` the studio's own notes. |
| `.claude/docs/` | Phase guides, templates, hook reference, workflow catalogue. |
| `.claude/templates/` | Short project-local templates (bug report, mechanic GDD, session state). |
| `.claude/hooks/` | Safety hooks wired in `settings.json` (`docs/hooks-reference.md`). All hooks are plain bash — no Python or `jq` needed. |
| `.claude/tools/` | Tools the kit ships with (currently the xlsx→markdown parser). |
| `.claude/reference/` | Theory the skills treat as law — the resource-flow difficulty framework. |

---

## The Pipeline — 1A/1B/2/3/4

Run `/gd-workflow-help` for the full table with expected outputs and a "you are here" marker.
**Who does what** — developer vs game designer, hand-overs, the weekly level loop — is in
`docs/team-workflow.md` (Vietnamese: `team-workflow.vi.md`). The short version:

```
Phase 1A  idea → architecture       (developer track)
  map-systems · design-system · create-architecture
  (start · brainstorm exist as standalone commands, not tracked steps of this pipeline)

Phase 1B  define difficulty          (game designer track — no bot, no code)
  gd-mode · gd-map-flow · gd-core-difficulty · gd-mechanic-difficulty
  gd-mechanic-object-mix · gd-mechanic-mix · gd-level-definition · gd-level-intent

  ⭐ 1A and 1B are independent — neither reads the other's output. Run them at the
  same time, by different people if there are two, once a game concept exists.

Phase 2  teach a bot to play        (optional — only needed to MEASURE)
  gd-bot-playstyle · gd-prototype-sim
  needs BOTH 1A (a Model/View split to simulate) and 1B (a difficulty model to simulate against)

Phase 3  generate and measure
  gd-level-gen · gd-level-audit

Phase 4  calibrate against humans
  gd-calibrate  → loop back to phase 3 with the calibrated ruler
```

Everything from phase 2 onward needs the Model/View split from `CLAUDE.md` §7.
Without a headless model there is no bot, and without a bot there is no measurement —
but phase 1B alone already produces real, generatable levels.

---

## What The Kit Expects Outside `.claude/`

The kit is **not fully self-contained**. Skills read and write a folder tree in the
project root, creating each folder the first time it is actually needed.
`/project-overview` only reports whether the tree already exists — it does not create it.

| Path | Written by | Holds |
|---|---|---|
| `design/` | `gd-*`, `design-system`, `ux-design` | GDDs, pipeline specs, entity registry, UX |
| `production/` | `playtest-report`, QA agents; `stage.txt` by hand | project stage, playtest reports, QA evidence |
| `docs/` | `create-architecture`, developers | architecture, ADRs, research, engine reference |
| `docs/features/` | developers | living feature docs next to the code |
| `docs/_session/active.md` | every multi-step skill and agent, developers | **the one session-state file** — read by the session-start hook and after every compaction (`context.md` §1) |

Everything else a skill touches is either standard Unity (`Assets/`,
`Packages/manifest.json`, `ProjectSettings/`) or lives inside `.claude/`.

Two things the kit ships with, both deliberately inside `.claude/` so copying it keeps them working:

- `.claude/tools/gdd-sync/` — an xlsx→markdown parser used by `gd-level-intent`
- `.claude/reference/resource-flow-difficulty-framework.md` — **the law all 11 `gd-*` skills
  read before doing anything.** Without it the whole designer pipeline stops at step 0.

---

## Never Share

These are machine- or project-specific and must **not** travel with the kit:

- `.claude/settings.local.json` — records the local machine's permission grants,
  including absolute paths and personal folder names. Gitignored; delete it before
  copying the folder anywhere.
- `.claude/agent-memory/` — session memory, meaningless in another project.

`.claude/settings.json` **is** shared: it holds the permission allow/deny policy that
should apply to every project using this kit.

---

## Keeping The Kit Clean

When adding to the kit, check these first — they are the failure modes this kit has
already hit once:

- **No duplicate documents.** A rule that exists in two files will drift, and the AI
  will read the stale one. There is exactly one coding standard: `coding_convention.md`.
- **No project names or absolute paths** outside `CLAUDE.md` §1. Everything else uses
  `<placeholders>` the developer fills in (`/project-overview` gives them a snapshot to
  fill from; it does not write the values itself).
- **Examples from real projects are welcome** — they carry real numbers — but label them
  as examples so they are not mistaken for the current project's data.
- **English only.** The kit is shared across projects and teams.
