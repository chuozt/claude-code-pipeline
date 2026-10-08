---
name: gd-level-gen
model: claude-opus-5-5
effort: medium
description: "Generate levels from the defined formula — produce candidates, filter in two passes (static DIG, then bots), rank them, write them as candidate files and report where they are; the designer reviews and confirms them in the project's level tool, never in the chat. Without a usable simulation it runs static-only (pass 1 only, every level marked unmeasured) instead of stopping. Use when typing /gd-level-gen or saying 'generate levels', 'make new levels', 'add more hard levels'."
argument-hint: "<tier> [count] — e.g. hard 5  |  block <from>-<to> — e.g. block 21-30"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Edit, Bash, AskUserQuestion
---

> **Coding rule — mandatory.** Every line of C# this skill writes, reviews, or proposes must follow `.claude/coding_convention.md` (Allman braces, §9 script layout, field order and naming, `GameDebug` instead of `Debug.Log`, no `{ get; private set; }`). Where any sample or advice below disagrees with that file, the convention wins.

Run the generation formula, filter in two passes, write the best candidates to their folder,
report the files; the designer reviews them in the project's level tool and confirms there.
**A human is the final gate — this skill never puts a level into the build.**

**In**: `gd-level-definition.md` + a simulation/solver (optional — see §0c) · **Out**: candidate
level files + a `READ-ME-FIRST.md` with the measurements

## 0. Check the preconditions

1. Read `.claude/reference/resource-flow-difficulty-framework.md` **sections 8, 9.2, 10**.
2. Read `design/gdd/gd-level-definition.md`. Missing → stop, point at `/gd-level-definition`.
2a. Read `design/gdd/gd-level-roadmap.md` **if it exists** (levels in staged blocks). For the
   requested level or block, take **role**, **mechanics allowed** and the block's **boundary
   contract** from it. Hard stops:
   - the request needs a mechanic the roadmap has not introduced by that level → stop, point
     at the lead (the roadmap is theirs; never widen "mechanics allowed" yourself);
   - the request contradicts the block's entry/exit tier or win-rate band → say so and ask
     the owner whether to change the request or ask the lead to change the contract.
   Nothing else is needed from other blocks: **do not read other blocks' levels** and do not
   use them as examples — the generator works from the formula, the roadmap and the bots only.
2b. Read `design/gdd/gd-level-intent.md` — or every `gd-level-intent-L*.md` shard — **if it
   exists** (optional — from
   `/gd-level-intent`): any level present in that table has its tier profile **overridden**;
   absent levels use the default. Check the source hash in its header — if the designer says
   the intent sheet just changed but the hash is stale → tell them to re-run
   `/gd-level-intent` first. In the top-k table, mark which candidates are running on their
   own intent rather than the tier default.
3. **Is the simulation + solver API usable?** Verify with a compile gate and one test match,
   reporting the real result. It is usable only if `/gd-prototype-sim` §3 also recorded the
   **parity test passed** against the real game and a **measured throughput** from
   `BatchRunner` (matches per second) — numbers from a simulation nobody has compared with the
   game are fiction. Usable → run all bot matches through `BatchRunner` (parallel), not one by
   one. **Not usable (missing, not compiling, no parity, no throughput) → do not stop: switch
   to static-only mode (§0c)** and say so in the first line of the reply, naming what is missing.
4. Read `design/gdd/gd-mechanics/*.md` — list any mechanic still **`unscored`** (no bot
   rule branch yet).

⚠️ **Hard stop** (simulation usable only): if the requested tier uses an `unscored` mechanic → stop.
> "Mechanic <X> has no bot rule branch. Generating levels with it now would score them
> wrongly — the bot plays badly because the bot is ignorant, not because the level is hard."

In static-only mode this stop does not apply: no level is scored, so no mechanic is scored
wrongly. List the `unscored` mechanics anyway, for when the simulation exists.

## 0b. Whole-block mode — `/gd-level-gen block 21-30`

Only when `gd-level-roadmap.md` has a block rhythm (the designer's string of tiers, e.g.
`N N N N H N N N N S`). Expand the range into one request per level: tier from its
position, mechanics from the roadmap row. Run sections 1–4 for **each level in turn**, with
its own tier, and write the result as one table per block in the block's `READ-ME-FIRST.md`:

| Level | Position | Tier | Role | Candidates (top-k) |
|---|---|---|---|---|

The designer reviews per level, not per block. A level whose tier needs an `unscored`
mechanic is skipped with the reason and listed at the end; the rest of the block still runs
(in static-only mode, §0c, no level is skipped for that reason — every level is `unmeasured`).
Never use another block's levels as input. One block per run.

## 0c. Static-only mode — no usable simulation yet

Entered automatically when §0 step 3 finds the simulation not usable. `/gd-prototype-sim` is
skipped, not faked:

| Section | In static-only mode |
|---|---|
| §1 Generate | unchanged — recorded seed, identity fixed, free part permuted |
| §2 Static DIG | unchanged — the only filter: constraints, conservation, obvious deadlock, distance from the tier's target DIG profile |
| §3 Bots | **skipped** — no `Solver.Play`, no win rate, no difficulty curve, no `failPoint` |
| §4 Rank | by distance from the tier's target DIG profile; the table shows the static numbers only and writes `unmeasured` in every bot column; `game-designer` still gives its pacing opinion, told that the numbers are static |
| §5 Write | metadata carries `measured: false` and the reason (`no simulation`), in place of bot numbers |
| §6 Remind | add the fourth reminder below |

Never estimate a win rate, a difficulty curve or a tier fit "by eye" to fill a bot column: an
estimate presented next to measurements reads as one. Every candidate stays `unmeasured` until
`/gd-level-audit` measures it with a usable simulation.

## 1. Generate candidates

Per `gd-level-definition.md`: keep the **identity** part fixed, permute the **free** part
(hidden GIVEN × GOAL order), following the agreed strategy and a **recorded seed**.

Record the seed in the read-me — without it, a candidate you saw cannot be reproduced.

Candidates go to **`design/levels/candidates/`**, one folder per request — per block when the
roadmap has blocks (`design/levels/candidates/L21-30/`), otherwise per level
(`design/levels/candidates/L5/`) — each with its own `READ-ME-FIRST.md`. A run only ever
touches the folder of the owner who asked for it. Candidate file names are
`Level_<N>_c<K>.json` (or the project's level file format with the same `_c<K>` suffix).

## 2. Filter pass 1 — static DIG (cheap)

Compute the DIG profile from the data alone, **without playing a match**. Reject immediately:
- candidates violating a constraint / the conservation invariant
- candidates that obviously deadlock (infinite DIG at the moment the BUFFER fills)
- candidates far from the tier's target DIG profile

Report the numbers: how many generated, how many rejected, and for which reasons.

## 3. Filter pass 2 — bots (expensive)

For the survivors, run `Solver.Play` over many seeds for **all three Tier A bots**.
Keep candidates that satisfy:
- `GreedyBot` win rate within the tier's target band
- a **difficulty curve** of the tier's shape (frame section 8; definition in
  `.claude/tools/level-audit/README.md` — the same one `/gd-level-audit` reports)
- a `failPoint` that is not too early — losing at 20% of the match is broken design, not difficulty

## 4. Rank

Rank the top-k (count from level-definition) in a comparison table:

| # | seed | win% Greedy | win% Random | peak pressure | peak position | stuck peak | failPoint | margin |
|---|---|---|---|---|---|---|---|---|

With a short note per candidate: which **factor** makes it hard, and where the tension sits.

**Spawn `game-designer`** (Task) for a **pacing** opinion. A machine can score whether the
curve hits the numeric thresholds; whether it is *elegant or forced* is a pacing judgement —
flow and pacing are this agent's frameworks.

Include in the prompt: an excerpt of **section 8** of
`resource-flow-difficulty-framework.md`, `gd-level-definition.md`, and the top-k table above.
The request: *"rank by pacing quality and justify; point out candidates that meet the numeric
thresholds but whose curve feels forced. **Return analysis, DO NOT write files**"*.

The table and the agent's opinion go **side by side into the folder's `READ-ME-FIRST.md`** —
the opinion never replaces the numbers. Nothing is presented for approval in the chat: see §4b.

## 4b. After writing — report, do not ask

The designer has not seen the candidates when the run ends, so **never ask which candidate to
approve** (no `AskUserQuestion`, no "which do you pick?"). The designer opens them in the
project's level tool (or the Editor) and confirms there; confirming is what copies a candidate
into the live levels folder, and it is never done by this skill. The reply says only:

1. that generation finished, for which level or block, and in which mode (bots / static-only);
2. each file's name and folder (e.g. `design/levels/candidates/L5/Level_5_c2.json`);
3. which existing candidate files were **overwritten** (or "none"), and that the live levels
   folder was not touched.

The table and the `game-designer` opinion stay in `READ-ME-FIRST.md`, not in the reply; the §6
reminders shrink to one line at the end of the reply.

## 4c. Overwriting a candidate voids its measurements

A candidate file that is overwritten is a **different level** under the same name, so
everything measured about the old one no longer applies. Whenever a run overwrites an existing
candidate (`Level_<N>_c<K>.json` already present):

1. In the folder's `READ-ME-FIRST.md`, reset that candidate's row: every bot column (win rates,
   difficulty curve, stuck curve, `failPoint`) back to `unmeasured`, "Confirmed by" back to `—`,
   generation date and seed to the new run's.
2. Never present the old numbers again — not in the reply, not in the table. The reply says
   "numbers of <names> reset: not measured yet".
3. The audit data is keyed by file content: each level has one audit file,
   `design/levels/audit-data/audit-level-<N>.json`, whose entries carry the `sourceHash` of
   the level file they measured (`.claude/tools/level-audit/README.md`). A new file has a new
   hash, so its old entries are stale by definition and `/gd-level-audit` reads them as
   `unmeasured`. Never edit an audit file by hand, and never touch the run logs in
   `audit-data/runs/` — they are history.
4. A candidate that had been confirmed into the game keeps that line in the read-me as
   history, with a note that the file is no longer the confirmed one.

## 5. Write the candidates

Ask once for the whole batch — *"May I write <k> candidates to `design/levels/candidates/<folder>/`?"* —
never per file, and never for the live levels folder, which this skill does not write. Each
candidate carries **traceability metadata**: seed, tier, mechanics used, the bot numbers at
generation time (or `measured: false` + reason, §0c), date, generator run id. The read-me's
"Confirmed by" column is filled by the level tool when the designer confirms, not here.

That metadata is what later allows comparing **bot prediction vs real-player win rate** —
without it the calibration loop has nothing to compare against.

## 6. Mandatory closing reminders

State them, never skip them — after a run, as one short line (§4b):

1. **These numbers are bot predictions, not real difficulty.** Only after a calibration round
   against real-player data (frame section 9) does it become difficulty.
2. **Keep a holdout set**: during calibration, a portion of levels must be held out of the
   fitting process (frame section 9.2).
3. If any `unscored` mechanic was excluded in section 0 → restate what is still missing.
4. **Static-only run** (§0c): "These levels are unmeasured. Their difficulty is not known until
   `/gd-level-audit` measures them with a working simulation (`/gd-prototype-sim`)."

## Next

- `/gd-level-audit` — measure the candidates (and the whole level set) and report on it
- Then `/gd-calibrate` (phase 4) once designer scoring or real-player data exists

Full pipeline: `/gd-workflow-help`
