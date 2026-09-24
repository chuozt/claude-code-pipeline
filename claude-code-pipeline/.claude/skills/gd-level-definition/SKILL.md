---
name: gd-level-definition
description: "Interview the designer to define HOW levels are generated — what is human-owned identity, what is a free variable for the generator, which constraints are mandatory, and the target profile per tier. Use when typing /gd-level-definition or saying 'define how levels are generated', 'what is the machine allowed to invent', 'the level generation formula'."
argument-hint: "[empty]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Edit, AskUserQuestion
---

Define the **generation space** before generating. Without this step the generator either
produces nonsense or destroys the game's artistic identity.

**In**: flow-map · difficulty-model · mechanic-mix · mechanic-object-mix ·
**Out**: `design/pipeline/level-definition.md`

## 0. Load context

1. Read `.claude/reference/resource-flow-difficulty-framework.md` **section 10** (the generation
   engine) and **section 2** (the conservation invariant).
2. Read `design/pipeline/flow-map.md`, `difficulty-model.md`, `mechanic-mix.md`,
   `mechanic-object-mix.md`. Any missing → stop and point at the matching skill.
3. Read the level/tool GDD if it exists, and any existing level table (if the designer has
   hand-built a few levels — they are valuable examples for asking "was this deliberate or
   incidental?").

## 1. Separate Identity from Difficulty ⭐

This is the central question. Explain the principle before asking:

> Expensive artistic content (models, silhouettes, themes) — **human-owned**.
> Whatever decides difficulty (ordering, hidden-layer colouring, OUTPUT order) —
> **machine-generated**.
> From the same content, the machine produces the whole Normal → SuperHard range with no
> change to the art.

Then ask two questions:

1. **"Which part of a level is identity — changing it breaks the artistic intent?"**
   (example: the shell layer's colours must match the concept — a daisy has white petals
   and a green stem)
2. **"Which part is invisible at the start, or carries no aesthetic meaning — where the
   machine may decide freely?"**
   (example: the colours of the hidden layer, the order pieces arrive in)

Settle these into two explicit lists. Anything the designer is unsure about → leave
`UNDEFINED`, and the generator **will not touch it** until there is a decision.

## 2. Mandatory constraints

List them and get each confirmed — these are what the validator will guard:

- **The conservation invariant** — from the flow map (`=` or `≥`)
- The table of valid types/colours and their maximum counts
- Capacity constraints (if there is a MID)
- The LEVEL-scale forbidden-combination matrix — from `mechanic-mix.md`
- The OBJECT-scale forbidden-combination matrix — from `mechanic-object-mix.md`
  (the level tool's validator blocks it at attachment time)
- Any game-specific constraint the designer adds

Also ask: **"When a level violates a constraint, should the generator skip it or raise an error?"**
Recommendation: skip the candidate during generation, raise an error when validating a
hand-authored level.

## 3. Target profile per tier

For each tier (Normal / Hard / SuperHard), assemble one profile:

| Component | Source |
|---|---|
| `GreedyBot` win-rate band | `difficulty-model.md` |
| `pressure` curve shape | frame section 8 |
| Number and kinds of mechanics | `mechanic-mix.md` section 4 |
| Target DSL profile | **asked here** |

The DSL question: *"At this tier, how deep should the player have to dig to reach what they
need — almost always available, or regularly peeling through several layers?"*
Turn the answer into something quantifiable (e.g. "average DSL ≤ 1 layer" / "2–3 layers" /
"≥ 3 layers with stretches that force discarding").

## 4. Search strategy

`AskUserQuestion`: "How should the generator search?"
- `Hill-climb toward the target DSL` *(recommended — directed and cheap)*
- `Random generation then filtering` *(simplest, much more expensive)*
- `Exhaustive enumeration` *(only feasible when the space is small)*

And: **"How many candidates per run should be presented to the designer?"** (suggest 3–5).

State clearly: **a human is always the final gate** — the generator never puts a level into
the build by itself.

## 5. Write the file

Present the draft, then ask:
> "Write this to `design/pipeline/level-definition.md`?"

Structure: the two identity/free lists · constraints + violation behaviour · the three tier
profiles · search strategy + candidate count · date + who signed off.

## Next

- `/gd-level-intent` — **optional**, only if the designer has a per-level intent sheet
- Then phase 2 (`/gd-bot-playstyle` → `/gd-prototype-sim`) before any generation can be
  measured. Without it, `/gd-level-gen` has nothing to filter or score with.

Full pipeline: `/gd-workflow-help`
