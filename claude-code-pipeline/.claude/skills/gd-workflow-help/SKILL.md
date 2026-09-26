---
name: gd-workflow-help
description: "Show the game designer's end-to-end workflow — every phase, every command in order, with one line each on what it does and what it produces. Use when typing /gd-workflow-help or asking 'what do I run next', 'show me the pipeline', 'which command do I use for this', 'where am I in the process'."
argument-hint: "[empty | 1A|1B|2|3|4 to expand one phase]"
user-invocable: true
allowed-tools: Read, Glob, Bash
---

The map of the whole pipeline. Print it whenever the designer is unsure what comes next.

## Before printing: locate where they are

Check which output files already exist, and mark each command **✅ done** / **▶ next** /
`—` not started. Do not just print a static list — the point is telling them where they are.

```bash
ls design/gdd/game-concept.md design/gdd/systems-index.md docs/architecture/architecture.md \
   design/pipeline/flow-map.md design/pipeline/difficulty-model.md \
   design/pipeline/mechanic-object-mix.md design/pipeline/mechanic-mix.md \
   design/pipeline/level-definition.md design/pipeline/level-intent.md \
   design/pipeline/bot-playstyle.md 2>/dev/null
ls design/pipeline/mechanics/*.md design/pipeline/calibration/*.md 2>/dev/null
```

`▶ next` is the first command whose output is missing and whose inputs all exist. Phase 1A
and 1B each start counting from their own first command — see the note below the two tables.

---

## Phase 1A — From idea to architecture *(developer track)*

Assumes a game concept already exists (`design/gdd/game-concept.md` — written however the
team produces one; `/brainstorm` and `/start` can still be run standalone if useful, they are
just not part of this pipeline's tracked sequence).

| Command | What it does | Produces |
|---|---|---|
| `/map-systems` | Splits the concept into systems, maps dependencies, sets design order | `design/gdd/systems-index.md` |
| `/design-system` | Writes every undesigned system's GDD, section by section, back-to-back in one run. `/design-system <name>` targets just one. **Developer only**: design values are taken from the designer's docs, anything missing is marked `[PLACEHOLDER]` and listed as an Open Question for the designer | `design/gdd/<system>.md` per system |
| `/create-architecture` | Turns the GDDs into a technical blueprint + the required ADR list | `docs/architecture/architecture.md` |

---

## Phase 1B — Define difficulty and the generation space *(game designer track)*

Nothing here needs a bot, a single line of code, or anything from Phase 1A.

| Command | What it does | Produces |
|---|---|---|
| `/gd-mode` | Switches to designer language and designer-only sources — docs, browser-opened docs, `.xlsx` workbooks, the designer's own recorded thinking; **never reads code, even read-only** | nothing — a mode, exit with `/gd-mode off` |
| `/gd-map-flow` | Maps the game onto the INPUT/MID/OUTPUT model. **The gateway to everything below** | `design/pipeline/flow-map.md` |
| `/gd-core-difficulty` | Ranks the 5 difficulty factors, settles the visibility rule and target win-rate bands | `design/pipeline/difficulty-model.md` |
| `/gd-mechanic-difficulty` | Where one mechanic creates difficulty + which factor it loads. **Run per mechanic** | `design/pipeline/mechanics/<name>.md` |
| `/gd-mechanic-object-mix` | Which mechanics one object may carry at once (deadlock / slot / state checks) | `design/pipeline/mechanic-object-mix.md` |
| `/gd-mechanic-mix` | Which mechanics may appear in one level, derived from their factors | `design/pipeline/mechanic-mix.md` |
| `/gd-level-definition` | Separates human-owned identity from machine-generated variables; sets the tier profiles | `design/pipeline/level-definition.md` |
| `/gd-level-intent` | Translates a per-level intent sheet into machine profiles. **Optional** | `design/pipeline/level-intent.md` + designed curves in `level-curves.md` |

> **Both mix matrices are needed before `/gd-level-definition`** — object scale and level scale
> answer different questions and neither substitutes for the other.

---

## ⭐ Phase 1A and 1B are independent — run them in parallel

Neither track reads the other's output. **1A** (architecture: systems → GDD sections →
technical blueprint) and **1B** (difficulty: flow model → factors → mix matrices → level
definition) can both start the moment a game concept exists, staffed by different people at
the same time. Nothing in 1B's five files (`flow-map.md`, `difficulty-model.md`,
`mechanic-object-mix.md`, `mechanic-mix.md`, `level-definition.md`) is produced by or
required by anything in 1A, and vice versa.

They only start to matter to each other once code is written: the architecture from 1A
defines *how* the game is built (Model/View split, systems, data flow), and the difficulty
model from 1B defines *what* the generator and simulation (phase 2 onward) must honour. Both
are needed before phase 2, but neither is an input to the other.

---

## Phase 2 — Teach a bot to play *(optional, unproven)*

Only needed to **measure**. Skip it and levels can still be generated, just not scored.
Needs both **1A** (a Model/View split to simulate) and **1B** (a difficulty model to
simulate against) finished first.

| Command | What it does | Produces |
|---|---|---|
| `/gd-bot-playstyle` | Turns "what playing well means" into three executable playstyle rules | `design/pipeline/bot-playstyle.md` |
| `/gd-prototype-sim` | Builds the pure-C# simulation + bots + solver API. **Needs a developer** | a `<Game>.Sim` assembly + tests |

> Any mechanic without a rule branch makes its levels `unscored` — generatable, but not
> scorable. That is the honest state, not a failure.

---

## Phase 3 — Generate levels and measure

| Command | What it does | Produces |
|---|---|---|
| `/gd-level-gen` | Generates candidates, filters by static DSL then by bots, presents the top-k for approval | approved level payloads + measurements |
| `/gd-level-audit` | Measures the whole level set with bots: win rates, pressure curves, colour pairing | an audit report + proposed fixes with root causes; measured curves in `level-curves.md` |

> Phase 3 needs phase 2. Without a simulation there is nothing to filter or measure with.

---

## Phase 4 — Calibrate against humans

| Command | What it does | Produces |
|---|---|---|
| `/gd-calibrate` | Fits the difficulty weights to designer scoring or real-player data, with a mandatory holdout | calibrated `w` + `design/pipeline/calibration/<date>.md` |

> This is the only step entitled to say "this level is hard" without an asterisk. Then loop
> back to `/gd-level-gen` with the calibrated ruler.

---

## Support commands, usable at any time

| Command | What it does |
|---|---|
| `/gd-mechanic-doc` | Generates or reviews a mechanic design doc against the 8 required sections |
| `/gd-feel` | Adds juice to gameplay that already works, without touching logic files |
| `/gd-perf` | Finds and ranks performance bottlenecks; analysis only |
| `/gd-bug` | The mandatory 4-step debugging procedure |
| `/gd-review` | Pre-commit code review against the convention and anti-patterns |

---

## The four ordering rules that actually matter

1. **1A and 1B are independent — run them at the same time, by different people if there are
   two.** Neither is the other's input; do not make the designer or the developer wait.
2. **`/gd-map-flow` comes first within 1B.** Without the model, no later skill in that track
   has a shared vocabulary.
3. **Both mix matrices before `/gd-level-definition`.** Object scale and level scale are
   different questions.
4. **Phase 2 gates phase 3.** No bot means no measurement — and a number from an ignorant bot
   is worse than no number at all. Phase 2 itself needs **both** 1A (the Model/View split to
   simulate) and 1B (the difficulty model to simulate against) done first.

Everything else can be reordered or skipped. Say so when the designer asks whether they can
skip a step.

## If asked to expand one phase

`/gd-workflow-help 1B` → print only that phase's table, then read each command's `SKILL.md`
header to show its full `description` rather than the one-line summary. Phase numbers are
`1A`, `1B`, `2`, `3`, `4` — there is no bare `1`, and no `5`.
