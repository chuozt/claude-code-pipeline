---
name: gd-level-audit
model: claude-opus-5-5
effort: medium
description: Measure the difficulty and colour pairing of an entire level set with headless bots, never by eye. Produces win-rate tables, pressure curves, dangerous colour pairs, and a list of levels needing fixes with their root causes. Use when the developer types /gd-level-audit or says "measure level difficulty", "this level is too hard", "review the levels", "check the colour pairing".
---

> **Coding rule — mandatory.** Every line of C# this skill writes, reviews, or proposes must follow `.claude/coding_convention.md` (Allman braces, §9 script layout, field order and naming, `GameDebug` instead of `Debug.Log`, no `{ get; private set; }`). Where any sample or advice below disagrees with that file, the convention wins.

# /gd-level-audit — reviewing levels with numbers

**First principle: measure, do not look.** Humans are extremely good at seeing what they expect to see.

---

## SAFETY RULE — read first

**Never overwrite, edit, or delete an existing level.** Proposed levels are exported to a
**separate folder**. The developer/designer decides what replaces what. If the game is live on
a store, live levels are especially untouchable.

---

## The warning to state before starting

> **A clean validator does NOT mean the levels are playable.**
> On a previous project the validator reported **0 errors and 0 warnings across all 40 levels**,
> but bot measurement found **14/40 levels with a 0–3% win rate**, including one where only
> **2% of the content** could be cleared before hard-locking.
>
> The validator checks *whether the data contradicts itself*. The simulator checks *whether a
> winning path exists and how hard it is*. Entirely different questions.

If the developer says *"but the validator is clean"* → repeat exactly this passage.

---

## Phase 1 — Ask before measuring

1. What skill level does the bot simulate? Does it use boosters? Revives? What misclick rate?
   *(Suggestion: an average-skill bot, ~25% misclicks, **no revives** — so the numbers reflect
   the level and not the assistance systems.)*
2. How many attempts per level is trustworthy? *(Suggestion: 100. Fewer and noise exceeds signal.)*
3. What win-rate band is wanted per difficulty label?
   *(Reference: Normal 85–100% · Hard 35–55% · Super hard 20–35%.)*
4. Which levels may be changed, and which are live and untouchable?

---

## Phase 2 — Measure difficulty

For **each** level, run N attempts and record 4 metrics:

| Metric | Meaning |
|---|---|
| **Win rate** | **How** hard the level is |
| **Pressure curve** | **Where** it is hard — average free resource remaining at each 5% progress mark → 20 numbers |
| **% of content cleared on a loss** | A level that hard-locks early shows up here immediately |
| **Dominant loss type** | Points at which mechanic is killing the player |

**The pressure curve matters more than the win rate.** A win rate is one number; the pressure
curve shows the level's *shape*. Good levels usually have one clear bottleneck and then open
up, rather than being uniformly tight from start to finish.

Reading a pressure curve (average free slots, 20 marks):
```
L10  5 4 3 3 3 3 3 3 3 2 2 2 1 1 1 2 3 3 4 4   easy → tight at 50-70% → opens up   ✅
L29  3 2 1 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0   hard-locked from 15%                ❌
```

The efficient way to run this: write **one static method** in the project's editor code that
returns JSON, then — **once the developer has typed `/use-mcp`** (`CLAUDE.md` §2.1; without it,
ask) — call it in one line through `eval` (`unity command eval --code "return X.RunToJson();"`),
writing the output to a file. Do not send long snippets over the bridge repeatedly — it burns tokens.

---

## Phase 3 — Colour pairing across the play session

**Never judge the level's overall palette.** The player does not see the whole level at once.

Slide a **window** along play progress (sized to the number of elements visible on screen at
once). At each window position, consider only the colours present **simultaneously**, and compute:

1. **The most similar pair** — CIEDE2000 distance
2. **Recompute after simulating red–green colour blindness** (protan + deutan)
3. Are two colours of the **same hue family** adjacent (check every neighbouring direction)?
4. Does it violate the designer's **forbidden colour cluster** rules?

**Thresholds:** ΔE2000 ≥ **18** for normal vision · ≥ **12** after colour-blindness simulation.

> **Why the colour-blindness simulation is mandatory:** a Blue + Cyan pair on a previous
> project measured ΔE **43.7** to normal vision — perfectly fine, and no manual review session
> ever caught it — but only **2.2** after simulation. For roughly 8% of male players that is
> effectively one colour. It appeared in **17/40** levels.
>
> Other pairs caught the same way: Mint+Pink colour-blind ΔE **0.4** · Blue+Purple **1.8**
> (10 levels) · Yellow+Green **2.3** (12 levels).

---

## Phase 4 — Find the root cause, not the symptom

A bad level is rarely bad because "there are too many mechanics". On a previous project, the
real cause of most dead levels was the **supply order**: resources needed early were placed
behind resources needed late. Judged by eye, the conclusion was "too cluttered", mechanics were
removed — and the levels stayed broken.

Four suspects, in order of frequency:
1. **Resource ordering** — what is needed first is buried behind what is needed later
2. **Resources only usable once exposed** — sitting dead and occupying space for the whole match
3. **A mechanic's simultaneity condition** — several things must be true at once; missing one deadlocks permanently
4. **Mechanic stuffing** (*fake difficulty*) — confusing rather than hard, and it breaks the teaching rhythm

---

## Phase 5 — Report

1. **An N-row table:** level · designer label · win rate · difficulty verdict · worst colour pair · rule violations
2. **The list of levels needing fixes + their ROOT CAUSE** — not "too hard" but
   *"missing the right resource at the 40–60% stretch"*
3. **Proposed levels, exported to a separate folder**, with a read-me-first file
4. **Suggested validator additions** so the same problem is caught earlier next time

---

## Honesty about error margins — MANDATORY in the report

- These numbers come from a **bot**, not real players. The difficulty ranking between levels
  is trustworthy; **the absolute values are not**.
- Two runs of 100 attempts on the same Hard level can differ by up to **±8 percentage points**.
- A metric correlating weakly with real outcomes → call it a **diagnostic** tool, not a
  **predictive** one. *(Real example: a supply-skew metric explained the badly broken levels
  well but correlated only r = −0.20 with win rate across the fixed level set — unusable for estimation.)*
- The tool's "solvable" column is usually a **conservative** check (running a single greedy
  path). *(One level was reported "unsolvable" while the bot won 100% of the time.)*
  Do not read it as "impossible".
- Colours are computed from the swatches in the data, which may differ from what actually
  renders on a device.

---

## What NOT to do

- Decide on the designer's behalf which levels get replaced. Measure and propose; **the designer decides**.
- Change a difficulty label in the data without asking.
- Present an estimate as if it were a measurement.

---

## Next

- `/gd-calibrate` — phase 4: turn these bot predictions into real difficulty, using designer
  scoring or live player data
- Levels needing fixes → `/gd-level-gen` again for those tiers

Full pipeline: `/gd-workflow-help`
