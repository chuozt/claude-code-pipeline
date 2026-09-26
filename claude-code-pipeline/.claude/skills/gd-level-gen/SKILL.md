---
name: gd-level-gen
model: claude-opus-5-5
effort: medium
description: "Generate levels from the defined formula — produce candidates, filter in two passes (static DSL, then bots), rank them and present them to the designer for approval. Requires a working simulation + solver API. Use when typing /gd-level-gen or saying 'generate levels', 'make new levels', 'add more hard levels'."
argument-hint: "<tier> [count] — e.g. hard 5"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Edit, Bash, AskUserQuestion
---

> **Coding rule — mandatory.** Every line of C# this skill writes, reviews, or proposes must follow `.claude/coding_convention.md` (Allman braces, §9 script layout, field order and naming, `GameDebug` instead of `Debug.Log`, no `{ get; private set; }`). Where any sample or advice below disagrees with that file, the convention wins.

Run the generation formula, filter in two passes, present the best candidates for approval.
**A human is the final gate — this skill never puts a level into the build.**

**In**: `level-definition.md` + a working simulation/solver · **Out**: level payloads + a
measurement report

## 0. Check the preconditions — stop immediately if any is missing

1. Read `.claude/reference/resource-flow-difficulty-framework.md` **sections 8, 9.2, 10**.
2. Read `design/pipeline/level-definition.md`. Missing → stop, point at `/gd-level-definition`.
2b. Read `design/pipeline/level-intent.md` **if it exists** (optional — from
   `/gd-level-intent`): any level present in that table has its tier profile **overridden**;
   absent levels use the default. Check the source hash in its header — if the designer says
   the intent sheet just changed but the hash is stale → tell them to re-run
   `/gd-level-intent` first. In the top-k report, mark which candidates are running on their
   own intent rather than the tier default.
3. **The simulation + solver API must run.** Verify with a compile gate and one test match,
   reporting the real result. Not running → stop, point at `/gd-prototype-sim`.
4. Read `design/pipeline/mechanics/*.md` — list any mechanic still **`unscored`** (no bot
   rule branch yet).

⚠️ **Hard stop**: if the requested tier uses an `unscored` mechanic → stop.
> "Mechanic <X> has no bot rule branch. Generating levels with it now would score them
> wrongly — the bot plays badly because the bot is ignorant, not because the level is hard."

## 1. Generate candidates

Per `level-definition.md`: keep the **identity** part fixed, permute the **free** part
(hidden INPUT × OUTPUT order), following the agreed strategy and a **recorded seed**.

Record the seed in the report — without it, a candidate you saw cannot be reproduced.

## 2. Filter pass 1 — static DSL (cheap)

Compute the DSL profile from the data alone, **without playing a match**. Reject immediately:
- candidates violating a constraint / the conservation invariant
- candidates that obviously deadlock (infinite DSL at the moment the MID fills)
- candidates far from the tier's target DSL profile

Report the numbers: how many generated, how many rejected, and for which reasons.

## 3. Filter pass 2 — bots (expensive)

For the survivors, run `Solver.Play` over many seeds for **all three Tier A bots**.
Keep candidates that satisfy:
- `GreedyBot` win rate within the tier's target band
- a `pressure` curve of the tier's shape (frame section 8)
- a `failPoint` that is not too early — losing at 20% of the match is broken design, not difficulty

## 4. Rank and present

Present the top-k (count from level-definition) as a comparison table:

| # | seed | win% Greedy | win% Random | peak pressure | peak position | failPoint | margin |
|---|---|---|---|---|---|---|---|

With a short note per candidate: which **factor** makes it hard, and where the tension sits.

**Spawn `game-designer`** (Task) for a **pacing** opinion. A machine can score whether the
curve hits the numeric thresholds; whether it is *elegant or forced* is a pacing judgement —
flow and pacing are this agent's frameworks.

Include in the prompt: an excerpt of **section 8** of
`resource-flow-difficulty-framework.md`, `level-definition.md`, and the top-k table above.
The request: *"rank by pacing quality and justify; point out candidates that meet the numeric
thresholds but whose curve feels forced. **Return analysis, DO NOT write files**"*.

Present the agent's opinion **alongside** the numbers, never instead of them — the designer
sees both and decides.

`AskUserQuestion`: "Which candidates do you approve?" → allow multiple, or
`Generate another batch`, or `Loosen/tighten the profile and regenerate`.

## 5. Write the approved levels

Ask permission before writing. Each level carries **traceability metadata**: seed, tier,
mechanics used, the bot numbers at approval time, date, approver.

That metadata is what later allows comparing **bot prediction vs real-player win rate** —
without it the calibration loop has nothing to compare against.

## 6. Mandatory closing reminders

State all three, never skip them:

1. **These numbers are bot predictions, not real difficulty.** Only after a calibration round
   against real-player data (frame section 9) does it become difficulty.
2. **Keep a holdout set**: during calibration, a portion of levels must be held out of the
   fitting process (frame section 9.2).
3. If any `unscored` mechanic was excluded in section 0 → restate what is still missing.

## Next

- `/gd-level-audit` — measure the whole level set and report on it
- Then `/gd-calibrate` (phase 4) once designer scoring or real-player data exists

Full pipeline: `/gd-workflow-help`
