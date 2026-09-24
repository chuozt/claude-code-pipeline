---
name: gd-core-difficulty
description: "Interview the designer to define what makes the core gameplay hard — rank the 5 factors, settle the visibility rule, define the perceptual proxy, set weights and target win-rate bands. Does NOT define the bot (that is /gd-bot-playstyle). Use when typing /gd-core-difficulty or saying 'define difficulty', 'what makes this game hard', 'what does easy or hard mean for a level'."
argument-hint: "[empty]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Edit, AskUserQuestion
---

Turn "difficulty" from a feeling into something measurable. Run **once per game**, right
after `gd-map-flow`.

**In**: `design/pipeline/flow-map.md` · **Out**: `design/pipeline/difficulty-model.md`

> **Scope — only the "what makes it hard" half.** The other half, *"what does playing well
> mean"* → the bot's playstyle rules, which belong to `/gd-bot-playstyle` and run later, right before
> `/gd-prototype-sim`.
>
> They are separated because **generating levels needs no bot**: the layout, the colours, the
> order pieces arrive in, difficulty points — none of them ask what a bot thinks. A bot is only needed once
> **measurement** starts. That lets the designer go straight from here to
> `/gd-level-definition` and produce real levels before a single line of simulation exists.
>
> **One exception must be settled here and cannot be deferred: the visibility rule
> (section 1b).** That is a game rule, not a bot rule — if a piece is never sufficiently
> exposed, the generated level is unwinnable no matter what the colours and the arrival order are.

## 0. Load context

1. Read `.claude/reference/resource-flow-difficulty-framework.md` **sections 4, 5.1, 6, 8**.
2. Read `design/pipeline/flow-map.md`. **Missing → stop**:
   > "There is no flow map. Run `/gd-map-flow` first — without the model there is no way to
   > know which factors apply."
3. Read the gameplay GDDs in `design/gdd/` for concrete examples to use in the questions.

## 1. Rank the 5 factors

For **each factor**, explain what it means **using an example from this game** (taken from the
GDD), then `AskUserQuestion` for a level: `High` / `Medium` / `Low` / `Not applicable`.

| Factor | Opening question |
|---|---|
| 1 · DSL | "Does the player often end up *knowing what they need but unable to reach it*?" |
| 2 · MID slack | *(skip if the flow map says N/A)* "Is queue capacity the main source of pressure?" |
| 3 · Hiddenness | "How much does the player have to guess because they cannot see it?" |
| 4 · Commitment | "Are there moves that cannot be taken back once made?" |
| 5 · Perceptual | "Does the player ever *know what to do* and still struggle because it is hard to see or find?" |

Warn the designer about factor 5: this is the factor **a bot cannot measure**, and if it is High,
every later bot number is missing a component — a calibration round with real humans becomes mandatory.

## 1b. The visibility rule ⭐ *(mandatory — hard-blocks level generation)*

Skip if the flow map says INPUT is **never** occluded (every move is always visible).

Ask the designer in **experience** terms, not numbers:

> "The player sees an [INPUT unit] peeking out a sliver from behind another, and taps it —
> does the game accept it?"

Then translate the answer into the two numbers the developer needs, and **present the
two-column table back for confirmation** (the designer reads the left column, the developer
the right):

| Decision (designer reads) | Number (developer reads) |
|---|---|
| generous tapping / must be clearly exposed | **exposure threshold** (e.g. ~3/4 of projected area) |
| does the player often hit "I can see it but the game refuses"? | **number of discrete directions** in the visibility set |
| does the camera rotate on one axis or two? | the direction set lies on a **circle** or a **sphere** |

Three things to tell the designer:

1. **The tighter the threshold, the fewer directions are needed** — a piece that is clearly
   exposed stays clearly exposed under small rotations, and the marginal cases have already
   been excluded from the legal set. A generous threshold brings the edge cases back and
   requires a denser direction set.
2. **A tight threshold raises factor 5 (perceptual)** — the player must both find the piece and
   rotate until it is fully exposed. If factor 5 was just ranked in section 1, revisit that level.
3. **Reversing this decision later requires re-examining the direction count.** Write that
   warning into the file.

This is the `coveredBy(unit, direction)` data that both the level tool and the simulation
read — without it, no trustworthy level can be generated.

## 2. PerceptualProxy

Only if factor 5 ≠ "Not applicable". Ask: "Specifically, what tires the eye?"

**Spawn `systems-designer`** (Task) to propose a formula — the designer knows *what* causes
eye strain but usually cannot write it as something countable.

The prompt must include — without these, the agent answers with generic game balance and
misses the frame entirely:
- excerpts from **sections 4 and 6** of `resource-flow-difficulty-framework.md`
- `flow-map.md` (this game's model)
- the designer's answer about what tires the eye
- the request: *"propose 2–3 candidate PerceptualProxy formulas, each with a variable table +
  value ranges; **return analysis, DO NOT write files**"*

Present the candidates for the designer to pick with `AskUserQuestion`. The designer decides;
the session owner writes.

Leave coefficients as `UNDEFINED` if the designer cannot estimate them — calibration will find
them. What matters is settling **what gets counted**.

## 3. `pressure(t)`

If the flow map has a MID → default to `MID occupancy ÷ capacity`, and confirm.
If not → take the proxy declared in the flow map and confirm it still holds.

## 4. Starting weights and win-rate bands

- `w₁..w₅` follow the levels ranked in section 1 (High/Medium/Low → the designer assigns
  numbers, or leave `UNDEFINED` and record the verbal level).
- **Target `GreedyBot` win-rate bands** for the three tiers Normal / Hard / SuperHard.
  If the designer has no view: spawn `systems-designer` (same prompt rules as section 2 —
  include the frame excerpt, the genre and the session length) to propose candidate bands,
  then `AskUserQuestion` for the designer to choose.
  Say clearly: this is a **starting point**, not truth — the calibration round (frame
  section 9) settles the real numbers.

## 5. Write the file

Present the draft, then ask:
> "Write this to `design/pipeline/difficulty-model.md`?"

Structure: the 5-factor table + levels · **the visibility rule (two-column table + the
reversal warning)** · PerceptualProxy · pressure proxy · starting `w` · the three tier
win-rate bands · date + who was interviewed.

`UNDEFINED` for everything the designer has not decided. **Never fill it in.**

## Next

- `/gd-mechanic-difficulty` — repeat per mechanic *(also only the "where it gets hard" half;
  the mechanic bot branch waits for `/gd-bot-playstyle` in phase 2)*
- Then `/gd-mechanic-object-mix` → `/gd-mechanic-mix` → `/gd-level-definition`

Full pipeline: `/gd-workflow-help`
