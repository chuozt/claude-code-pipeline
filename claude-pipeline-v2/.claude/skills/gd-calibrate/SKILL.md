---
name: gd-calibrate
description: "Close the difficulty-model calibration loop — designer hand-scoring before release, or comparing bot predictions against real-player data after soft launch, with a mandatory holdout. Use when typing /gd-calibrate or saying 'calibrate difficulty', 'the designer re-scored the levels', 'compare bot numbers with real players', 'the real win rate differs from the prediction'."
argument-hint: "[mode1 | mode2 | mode3] — defaults to choosing by the data available"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Edit, Bash, AskUserQuestion, Task
---

Turn bot numbers from a **relative prediction** into **absolute difficulty**. This is the only
step in the set entitled to say "this level is hard" without an asterisk.

**In**: real analytics + traceability metadata · **Out**: calibrated `w` (and/or DNA) +
**a holdout error report**

## 0. Load context and check preconditions

1. Read `.claude/reference/resource-flow-difficulty-framework.md` **all of section 9**.
2. Read `design/pipeline/difficulty-model.md` (the current `w` set), `level-definition.md`,
   and **every** `design/pipeline/mechanics/*.md`.
3. Find real-player data: ask the developer for the source (Firebase export / dashboard / CSV).
   **None available** → do not stop: run **mode 3** (designer hand-scoring) — that mode needs
   no data at all, only the simulation and a set of generated levels.
4. Read each level's traceability metadata (seed, tier, mechanics, bot numbers at approval) —
   written by `/gd-level-gen`. Missing metadata → warn: predictions cannot be matched against
   reality, only a crude fit is possible.

## 1. Choose the mode

Check the telemetry granularity available, then `AskUserQuestion`:

| | Mode 3 — designer scoring | Mode 1 — regress `w` | Mode 2 — evolve DNA with a GA |
|---|---|---|---|
| Needs | no data — the designer plays and scores | win rate per level | `action → state → result` **per move** |
| Produces | starting labels for `w` | calibrated relative difficulty score | **absolute** win rate / moves / failPoint / quit |

Unreleased, no real-player data → **mode 3** (designer scoring, frame sections 9.1 and
13.7-M3). Warn the designer about expert blindness: these labels are a starting point and will
be overwritten by mode 1 once real data exists.

Only per-level win rate → **mode 1**, and state its limits to the developer.
Wanting mode 2 without fine-grained telemetry → stop and report:
> "Telemetry is currently only at level granularity, which is not enough for a GA. **It cannot
> be retrofitted** — the logging has to be added and then a release cycle awaited. Run mode 1
> in the meantime?"

## 1b. Mode 3 — the designer scoring procedure

Runnable **as soon as the simulation exists**, with no player data.

1. Confirm the simulation has **variance enabled during self-play** (the `Mistake` trait — a
   perfect bot resembles nobody). **The variance must come from a seed**; otherwise the
   reproducibility of contract rule 1 is lost and nobody can debug a losing match.
2. Take the sample level set already generated, plus their bot numbers.
3. **The designer replays each level** and scores it easy/hard. Record labels on the same
   scale as the tiers (Normal / Hard / SuperHard) so they compare against predictions.
4. For each mismatched level, the designer states the direction: needs to be **easier** or **harder**.
5. Fit a crude `w` on those labels, then iterate.

> ⚠️ **Expert blindness — say this to the designer before starting.** They are the best player
> of this game; their sense of difficulty is **systematically** skewed relative to a newcomer.
> These labels are a **starting point**, will be overwritten by mode 1 once real data exists —
> and **must never be used to override real data** later.

The holdout (section 4) still applies: keep some levels out of the designer-scored set for checking.

## 2. Prepare the dataset

1. **Exclude every level containing an `unscored` mechanic** from the fitting set. List what was
   excluded and why — the bot cannot play that mechanic, so including it teaches the model from garbage.
2. Exclude levels with too few players (threshold set by the developer — ask; suggest ≥ 200
   attempts per level for the win rate to mean anything).
3. **Split 80 / 20**: a fitting set and a **holdout** set. Split randomly but **keep the seed**,
   and balance the tiers — do not let the holdout be all Normal.
4. Present a table: total levels, how many excluded and why, how many remain, how they were split.

## 3. Run the calibration

**Spawn `analytics-engineer`** (Task). The prompt must include — without it the agent returns
generic data analysis and misses the frame:
- excerpts from **sections 4, 6, 9** of `resource-flow-difficulty-framework.md`
- `difficulty-model.md` (the current `w` set and what each factor means)
- the data table: per level → 5 factor values + bot prediction + real win rate
- the request, per mode:
  - **Mode 1**: *"regress to find `w` such that DifficultyScore matches the real win rate on
    the FITTING SET; report the coefficients, the goodness of fit, and which factor contributes most"*
  - **Mode 2**: *"design the fitness function and GA parameters to evolve a Player DNA matching
    the action patterns; the fitness measures HUMAN-LIKENESS, not optimal play"*
- **"Return analysis, DO NOT write files."**

## 4. Holdout report — mandatory

Apply the new `w` (or DNA) to the **holdout set**, compare predictions against real win rates,
and report the error. Present a per-level holdout table: predicted / actual / difference.

**Never skip this step.** Without it the model may simply have memorised the fitting set, and
any claim that "the frame predicts accurately" is unfounded.

If the holdout error is **much larger** than the fitting error → say plainly that it is
overfitted, and propose fewer parameters or more data. Do not present the new `w` as usable.

## 5. Reading the results — three kinds of deviation

When a group of levels deviates from prediction, identify the cause before turning any dial:

| Signal | Cause | Action |
|---|---|---|
| Deviation concentrated on levels with mechanic X | the bot is ignorant about X | go back to `/gd-mechanic-difficulty`, **do not** adjust `w` |
| A whole batch deviates in the same direction | `w` is wrong | exactly this skill's job |
| Humans find levels with many types/clutter harder than predicted | **factor 5, perceptual**, is underweighted | raise `w₅`; if PerceptualProxy is still `UNDEFINED`, go back to `/gd-core-difficulty` |

The third kind is the most common, because factor 5 is the one a bot cannot measure.

## 6. Write the results

Ask permission before writing:
- update `w` (and/or DNA) in `design/pipeline/difficulty-model.md`, **keeping the old values in
  a history section** — never delete them, so the next round has something to compare against
- write `design/pipeline/calibration/<date>.md`: the mode, the fitting/holdout split, `w`
  before and after, the holdout error, the deviations found, and the excluded levels

## 7. Closing reminders

1. **The new `w` becomes `/gd-level-gen`'s objective function** — from now on generated levels
   are scored with the calibrated ruler.
2. **Re-run this skill when a new mechanic is added** — a new mechanic can shift the weights,
   and it only enters the fitting set once it has a bot rule branch.
3. If on mode 1 and wanting mode 2: decide the telemetry granularity **before the next build**;
   it cannot be retrofitted.

## Next

- `/gd-level-gen` with the new `w`
- `/gd-level-audit` re-run across the whole set to see how the ranking changed

Full pipeline: `/gd-workflow-help`
