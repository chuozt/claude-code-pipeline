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
| `design/gdd/gd-game-concept.md`, source `.xlsx` workbooks | Designer | Both | comment, never edit |
| `design/dev-system/dev-<system>.md` (GDDs) | Developer, alone, with `/design-system` — design values quoted from the designer's docs | Both | fixes `[PLACEHOLDER]`s via Open Questions |
| `design/gdd/gd-*.md` (flow map, difficulty model, mix matrices, level definition, level intent) | Designer, through the `gd-*` skills | Developer, the simulation, the generator | read only |
| `design/levels/level-curves.md` | Designed row: `/gd-level-intent` · Measured row: `/gd-level-audit` | Both | only their own rows |
| `design/dev-system/`, ADRs, code under `Assets/` | Developer | Designer only through the developer | never touch |
| `Assets/_Tools/LevelSim/` (the level simulation: bots, solver, parallel runner) | Developer | Developer only; the designer sees only its results (`/gd-level-gen` tables, `level-audit-*.md`). Editor-only, never in the player build; runs on the game's own Model and the same phase-1A docs and configs as the gameplay, thousands of matches in parallel | never touch — `gd-mode` forbids it and `validate-commit.sh` rejects it on `gd` branches |
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
| Base project: stack filled in `project_setup.md` §1, asmdefs, `GameDebug` | Concept: `design/gdd/gd-game-concept.md` — by hand or with `/brainstorm` |
| Code already exists → `/reverse-document` to rebuild the docs from it | Starts the source `.xlsx` workbook |

**Hand-over:** `gd-game-concept.md` approved by both. Neither side starts phase 1 before it.

### Phase 1A ∥ 1B — In parallel, nobody waits

| 1A — Developer | 1B — Designer (in `/gd-mode`) |
|---|---|
| `/map-systems` → `dev-map-systems.md` | `/gd-map-flow` → `gd-flow-map.md` (**first**, the vocabulary for the rest) |
| `/design-system` → one GDD per system, **developer only**. Rules, numbers and ranges are quoted from the designer's docs (1B files, `.xlsx`) and confirmed by the developer; anything the docs do not settle is a working value marked `[PLACEHOLDER]` (or `UNDEFINED`) with an Open Question for the designer. No taste questions, no agents unless asked | `/gd-core-difficulty` → `gd-difficulty-model.md` |
| `/create-architecture` → Model/View split, ADR list | `/gd-mechanic-difficulty` × each mechanic |
| `/architecture-decision` × each required ADR | `/gd-mechanic-object-mix` + `/gd-mechanic-mix` (both, before the next step) |
| | `/gd-level-definition` → tier profiles |

The designer is not in the 1A session. They clear the GDDs' Open Questions asynchronously —
confirm or replace each `[PLACEHOLDER]` in the GDD and its config field — ideally once 1B has
produced the difficulty files, since those settle most of the numbers.

**Hand-over — the sync meeting.** Checklist before phase 2:
- [ ] The model folder has no `UnityEngine` reference (`architect.md` §1)
- [ ] `gd-difficulty-model.md` and `gd-level-definition.md` exist and the designer signs them off
- [ ] Every mechanic in the level definition has a `design/gdd/gd-mechanics/<name>.md`

### Phase 2 — Teach a bot to play (optional — only needed to measure)

| Order | Who | Command |
|---|---|---|
| 1 | Designer alone — needs only 1B, no simulation; can start as soon as `/gd-core-difficulty` is done | `/gd-bot-playstyle` → `bot-playstyle.md` |
| 2 | Developer — needs 1A and `bot-playstyle.md` | `/gd-prototype-sim` → the pure-C# simulation, bots, solver API, tests |

A mechanic without a bot rule makes its levels `unscored` — generatable, not measurable.

### Phase 4 — Calibrate against real players (after soft launch)

Designer supplies the data (analytics export); developer runs `/gd-calibrate` with the
mandatory 20% holdout. New weights → back to phase 3.

---

## 5. The Weekly Level Loop (phase 3)

| When | Who | What | Output |
|---|---|---|---|
| Start of week | Designer | Updates the intent sheet → `/gd-level-intent` → **approves the designed curves** (●) | `gd-level-intent.md`, Designed rows in `level-curves.md` |
| Once, before the first audit | Designer | Fills the **Tolerances** table in `level-curves.md` | no verdicts are possible without it |
| Mid-week, fixed slot | Developer | `/use-mcp on` → `/gd-level-gen` → `/gd-level-audit` | candidates exported to a separate folder; Measured rows (○) + verdicts |
| End of week | Designer | Reads `level-curves.md`: keep / revise / replace each level | decisions recorded next to each level |
| Daily | Both | Merge `gd` and `feature/*` into the integration branch | — |

The designer cannot run generation or audit alone — both need the Editor bridge, which only
the developer enables. That is why the developer's run is a **fixed weekly slot**, not a
request.

Levels are never overwritten: generated and fixed levels go to a separate folder, and the
designer decides what replaces what (`anti-patterns.md` §5).

### 5b. Several designers, levels in blocks

When the levels come in blocks (for example 50 levels: 1–10 core only, then a new mechanic at
11, 21, 31, 41, mixed with the older ones afterwards), split the work **by block, one owner
per block**, plus a **lead designer** who owns the shared files. Splitting by mechanic makes
two people edit the same mixed level, so do not.

| File | Single writer | Others |
|---|---|---|
| `gd-flow-map`, `gd-difficulty-model`, both mix matrices, `gd-level-definition`, **`gd-level-roadmap`**, the Tolerances table | lead | read, raise Open Questions |
| `gd-mechanics/<name>.md` | the owner of the block that introduces it | read |
| `gd-level-intent-L<a>-<b>.md`, `level-curves-L<a>-<b>.md`, the block's levels | that block's owner | read only |

**Nobody waits for another block's levels.** The generator does not learn from existing
levels; a block needs only (1) the roadmap row for each of its levels — role, mechanics
allowed, tier suggestion — (2) its **boundary contract**: entry and exit tier and win-rate
band, so level 20 → 21 is checked by numbers, and (3) the mechanics it uses to exist in the
simulation with a bot rule branch (a developer job, done **before** block 3 starts: it is the
real critical path). Changing a contract or the roadmap is the lead's decision, announced to
the affected owners.

**The block rhythm is the designers' decision.** The lead asks the designers what one block
of levels should feel like, as a string of tiers (`N` Normal · `H` Hard · `S` SuperHard).
`N N N N H N N N N S` is a common starting suggestion, not a rule — whatever the designers
settle goes into `gd-level-roadmap.md`, and every level's tier then follows from its position.
`/gd-level-gen block 21-30` expands the rhythm into one request per level.

If no simulation exists yet, nothing can be generated: levels (at least the anchors) are
built by hand until phase 2 is done. Optionally each owner finishes 1–2 **anchor levels**
first (the introducing level + one practice level) so neighbours can see the new mechanic
early; this is a courtesy, not a dependency.

Weekly rhythm with several owners: each owner updates their intent sheet and runs
`/gd-level-intent <file> <range>` → the developer's slot generates **one block per run** into
`<candidates>/L<a>-<b>/` and audits the whole set → each owner approves their block → the
lead reads the audit's **block boundaries** section and fixes contracts if a jump was not
planned. Branches: one per designer (`gd-<name>`; not `gd/<name>`, which git refuses while a `gd` branch exists), merged daily.

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
