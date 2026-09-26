# Team Workflow — Developer + Game Designer

**Who** does **what**, **when**, and **where they hand over**. The command order itself lives in
`/gd-workflow-help` (it also marks "you are here"); where the two disagree, that skill wins.
The one-page visual summary is `pipeline-field-guide.pdf` at the repo root (source:
`pipeline-field-guide.html`). Vietnamese translation: `team-workflow.vi.md` — this file is the
original; edit it first, then re-translate.

---

## 1. The Principle — Files Are The Interface

The developer and the designer never hand work over in chat. They hand it over as files:

| Folder / file | Written by | Read by | The other side may… |
|---|---|---|---|
| `design/gdd/game-concept.md`, source `.xlsx` workbooks | Designer | Both | comment, never edit |
| `design/gdd/<system>.md` (GDDs) | Developer, alone, with `/design-system` — design values quoted from the designer's docs | Both | fixes `[PLACEHOLDER]`s via Open Questions |
| `design/pipeline/*.md` (flow map, difficulty model, mix matrices, level definition, level intent) | Designer, through the `gd-*` skills | Developer, the simulation, the generator | read only |
| `design/pipeline/level-curves.md` | Designed row: `/gd-level-intent` · Measured row: `/gd-level-audit` | Both | only their own rows |
| `docs/architecture/`, ADRs, code under `Assets/` | Developer | Designer only through the developer | never touch |
| `docs/_session/active.md` | Whoever runs the session | That person, after a compaction | — |

Two rules follow from this and are already project law:

- **The developer never decides difficulty, balance, pricing or palettes** (`CLAUDE.md` §10).
  A blank design decision is written `UNDEFINED` (or a clearly marked `[PLACEHOLDER]`) and sent to the designer — never passed off as settled.
- **The designer never reads or edits code** — `/gd-mode` enforces it. A question about what
  the game *currently does* goes to the developer.

---

## 2. Set Up Once (lead developer)

1. Copy `CLAUDE.md` + `.claude/` into the project, run `/project-overview`, fill the `<...>`
   placeholders (`project_setup.md` §6).
2. Create the code-root `.asmdef`, the `GameDebug` wrapper and the `ENABLE_LOGS` define.
3. Branches (`rules.md` §4): `<integration branch>` owned by the lead; `feature/<name>` per
   developer task; `gd` for the designer — **no `.cs` files ever**. Merge into the integration
   branch at least once a day.
4. Agree the file ownership in §1 with the designer before either writes anything.

---

## 3. Session Habits

| | Developer | Designer |
|---|---|---|
| Start of every session | `/project-overview`, then state the branch | `/gd-mode` |
| Reply style | `/dev-brief on` — answer decisions with `1A 2B`, `ok`, `skip N` | `/dev-brief on` also works inside `/gd-mode` |
| Unity Editor | `/use-mcp once` for one check, `on` for measurement runs — off otherwise | never |
| Scope | one session = one stage = one branch (`context.md` §6) | one session per design topic |
| Source of truth | code + `docs/features/*.md` | the `.xlsx` workbook, synced through `gdd-sync` |

Each person runs their own Claude session — never share one (`rules.md` §4).

---

## 4. The Phases — Who Does What

```
Phase 0      Dev: base project           ∥   Designer: concept docs
                 ↓ both done
Phase 1A/1B  Dev: architecture (1A)      ∥   Designer: difficulty model (1B)
                 ↓ both done — SYNC MEETING
Phase 2      Designer: bot playstyle     →   Dev: simulation
Phase 3      weekly level loop (§5)
Phase 4      after soft launch: calibrate, back to phase 3
```

### Phase 0 — Kick-off (in parallel)

| Developer | Designer |
|---|---|
| Base project: stack filled in `project_setup.md` §1, asmdefs, `GameDebug` | Concept: `design/gdd/game-concept.md` — by hand or with `/brainstorm` |
| Code already exists → `/reverse-document` to rebuild the docs from it | Starts the source `.xlsx` workbook |

**Hand-over:** `game-concept.md` approved by both. Neither side starts phase 1 before it.

### Phase 1A ∥ 1B — In parallel, nobody waits

| 1A — Developer | 1B — Designer (in `/gd-mode`) |
|---|---|
| `/map-systems` → `systems-index.md` | `/gd-map-flow` → `flow-map.md` (**first**, the vocabulary for the rest) |
| `/design-system` → one GDD per system, **developer only**. Rules, numbers and ranges are quoted from the designer's docs (1B files, `.xlsx`) and confirmed by the developer; anything the docs do not settle is a working value marked `[PLACEHOLDER]` (or `UNDEFINED`) with an Open Question for the designer. No taste questions, no agents unless asked | `/gd-core-difficulty` → `difficulty-model.md` |
| `/create-architecture` → Model/View split, ADR list | `/gd-mechanic-difficulty` × each mechanic |
| `/architecture-decision` × each required ADR | `/gd-mechanic-object-mix` + `/gd-mechanic-mix` (both, before the next step) |
| | `/gd-level-definition` → tier profiles |

The designer is not in the 1A session. They clear the GDDs' Open Questions asynchronously —
confirm or replace each `[PLACEHOLDER]` in the GDD and its config field — ideally once 1B has
produced the difficulty files, since those settle most of the numbers.

**Hand-over — the sync meeting.** Checklist before phase 2:
- [ ] The model folder has no `UnityEngine` reference (`architect.md` §1)
- [ ] `difficulty-model.md` and `level-definition.md` exist and the designer signs them off
- [ ] Every mechanic in the level definition has a `design/pipeline/mechanics/<name>.md`

### Phase 2 — Teach a bot to play (optional — only needed to measure)

| Order | Who | Command |
|---|---|---|
| 1 | Designer is interviewed | `/gd-bot-playstyle` → `bot-playstyle.md` |
| 2 | Developer builds | `/gd-prototype-sim` → the pure-C# simulation, bots, solver API, tests |

A mechanic without a bot rule makes its levels `unscored` — generatable, not measurable.

### Phase 4 — Calibrate against real players (after soft launch)

Designer supplies the data (analytics export); developer runs `/gd-calibrate` with the
mandatory 20% holdout. New weights → back to phase 3.

---

## 5. The Weekly Level Loop (phase 3)

| When | Who | What | Output |
|---|---|---|---|
| Start of week | Designer | Updates the intent sheet → `/gd-level-intent` → **approves the designed curves** (●) | `level-intent.md`, Designed rows in `level-curves.md` |
| Once, before the first audit | Designer | Fills the **Tolerances** table in `level-curves.md` | no verdicts are possible without it |
| Mid-week, fixed slot | Developer | `/use-mcp on` → `/gd-level-gen` → `/gd-level-audit` | candidates exported to a separate folder; Measured rows (○) + verdicts |
| End of week | Designer | Reads `level-curves.md`: keep / revise / replace each level | decisions recorded next to each level |
| Daily | Both | Merge `gd` and `feature/*` into the integration branch | — |

The designer cannot run generation or audit alone — both need the Editor bridge, which only
the developer enables. That is why the developer's run is a **fixed weekly slot**, not a
request.

Levels are never overwritten: generated and fixed levels go to a separate folder, and the
designer decides what replaces what (`anti-patterns.md` §5).

---

## 6. Decisions And Questions

- Every open question is written down with its **owner** (designer / developer / art), in the
  relevant doc's Open Questions or in `docs/_session/active.md` — never left in a chat.
- The closing block of `/dev-brief` (❓ decision · 🔒 approval · 🛠 action) is the format for
  passing a question to the other person: title, one option per line, the 🧭 current state.
- A decision is final only once it is **in a file**. The conversation is not the record.

---

## 7. Known Limits

- The `gd-*` pipeline fits the **resource-flow puzzle** family (sort, jam, tray, water sort…).
  Another genre stops at `/gd-map-flow`; use phases 0 and 1A only.
- Phase 2 is marked **unproven** in `/gd-workflow-help` — plan time for the developer to
  harden the simulation before trusting its numbers.
- Bot numbers rank levels reliably; their **absolute** values are not player truth until
  phase 4 (`verification.md` §5).
