---
name: gd-bot-playstyle
description: "Interview the designer to turn 'what playing this game well means' into three machine-executable playstyle rules, with an observation budget for each. Run immediately BEFORE /gd-prototype-sim, once levels can be generated. Use when typing /gd-bot-playstyle or saying 'define how the bot plays', 'what does playing well mean', 'we are about to build the sim'."
argument-hint: "[empty]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Edit, AskUserQuestion
---

Turn "what playing this game well means" into **rules a machine can run**. Run **once per
game**, immediately before `/gd-prototype-sim`.

**In**: `flow-map.md` + `difficulty-model.md` · **Out**: `design/pipeline/bot-playstyle.md`

> **Why this skill runs late.** Generating levels needs no bot — the layout, the colours, the order
> pieces arrive in and the difficulty points never ask what a bot thinks. A bot is only needed once
> **measurement** starts. So after `/gd-core-difficulty` the designer goes straight to
> `/gd-level-definition` and gets real levels in hand; this skill waits until the simulation
> is about to be built.
>
> This is frame section 5.1: **a bot's playstyle is a design document, not a code detail.** Writing
> it down makes the argument happen once, with the designer, at the design layer — instead of
> happening silently in code, forever, decided by whoever types fastest.

## 0. Load context

1. Read `.claude/reference/resource-flow-difficulty-framework.md` **sections 5.1 and 6.1**.
2. Read `design/pipeline/flow-map.md`. **Missing → stop**:
   > "There is no flow map. Run `/gd-map-flow` first."
3. Read `design/pipeline/difficulty-model.md`. **Missing → stop**:
   > "There is no difficulty model. Run `/gd-core-difficulty` first — without knowing which
   > factor is High, there is no way to know where the bot must be careful."
4. Read `design/pipeline/mechanics/*.md` if present — every mechanic needs a **branch** in these rules.
5. Read the gameplay GDD for concrete situations to use in the questions.

## 1. The root question

Ask in prose, **not** with `AskUserQuestion`:

> **"What does playing this game well mean? Explain it as if teaching a beginner, in 1–3 sentences."**

If the GDD already contains a sentence describing the AI/bot (many do, in the level-authoring
guidance), **show that sentence first** and ask the designer to confirm or amend it — faster
than asking from scratch, and it avoids creating a second version that contradicts the GDD.

## 2. Dig until a machine could execute it ⭐

The first answer is almost always incomplete. Dig with **concrete situations**, one at a time,
using `AskUserQuestion` where the options are finite:

| Situation to ask about | Why |
|---|---|
| **Several moves are legal** — which one? | without this rule the bot picks arbitrarily, and every measurement takes the shape of "arbitrary" |
| **Stuck** — no move serves an open OUTPUT | this is where DSL bites; how the bot handles it drives win rate more than anything else |
| **Side information** — does the game have a reveal mechanic (X-ray, peek, preview), and do good players use it to choose moves? | designers routinely forget to mention it; yet it is often exactly what separates good from decent |

**How to know it is enough**: read the rule back to the designer and ask *"in situation X,
which move does this rule pick?"* — if they have to think about it, the rule is missing a
branch. Repeat until they answer instantly.

## 3. Observation budget ⭐

If the flow map's *"mismatches"* section records a **free observation action** (rotating the
camera, X-ray, preview) then this section must be asked — otherwise the bot sees the whole
board for free and **always finds levels easier than a human does**.

`AskUserQuestion` with three approaches:

- **Weak bots are limited, strong bots see everything** *(usually the best choice)* — the
  win-rate gap between the two bots on the same level becomes a **measurement of the value of
  information**, i.e. a free Hiddenness metric.
- All bots see everything — simple, accepts an optimistic bot, compensated during calibration.
- All bots pay to observe — closest to a human, but adds a coefficient that must be tuned from day one.

## 4. Restate as three playstyle rules

| Bot | Derived from |
|---|---|
| `GreedyBot` | the designer's settled rule, with every branch from section 2 |
| `RandomBot` | uniformly random among legal moves *(nothing to ask)* — the floor |
| `LookaheadBot(k)` | GreedyBot + k-move lookahead — ask the designer for `k`, and **suggest a k from the game's structure** (e.g. the number of moves to the lose threshold, or the player's lookahead horizon from the flow map) |

Include the **perceptual premise** — list exactly what the bot is allowed to know, so the
simulation grants that and no more: the visible move set per the visibility rule
(`difficulty-model` section 1b), the revealed information from section 2, and the OUTPUT
horizon per the flow map.

## 5. `pressure(t)` — confirm

Take the proxy declared in the flow map, confirm it still holds, and record **how to read
it**: with a coarse quantum (one action pushing the MID up several steps) the curve is a
**sawtooth** — read it by step, not by slope.

## 6. Write the file

Present the draft, then ask:
> "Write this to `design/pipeline/bot-playstyle.md`?"

Structure: the designer's original sentence · the three playstyle rules in words · the perceptual
premise · the observation budget table · `pressure(t)` · a branch per mechanic (or `UNDEFINED`)
· date + who was interviewed.

`UNDEFINED` for everything the designer has not decided. **Never fill it in.**

⚠️ **A mechanic with no rule branch → levels containing it are `unscored`** and must not be
scored. List the mechanics still missing a branch inside the file.

## Next

`/gd-prototype-sim` — build the simulation + 3 bots from this document (needs a developer).
This file is the **contract**: the simulation must grant exactly the knowledge declared in
section 4, and no more.

Full pipeline: `/gd-workflow-help`
