---
name: gd-map-flow
description: "Map a puzzle game onto the INPUT/MID/OUTPUT model of the resource-flow frame — the mandatory gateway before any difficulty or level-generation work. Use when the developer/designer types /gd-map-flow or says 'map the game onto the frame', 'what is this game's INPUT', 'start the level pipeline'."
argument-hint: "[game name, or leave empty to use the current game]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Edit, AskUserQuestion
---

The gateway to the whole level pipeline. Run **once per game**. Without this file the later
skills have no shared vocabulary to talk in.

**In**: the existing GDDs (`design/gdd/`) · **Out**: `design/pipeline/flow-map.md`

## 0. Load context

1. Read `.claude/reference/resource-flow-difficulty-framework.md` **section 2** — that is the
   model definition and the law for this skill.
2. Read `design/gdd/game-concept.md` and `design/gdd/systems-index.md` if they exist.
3. If `design/pipeline/flow-map.md` already exists → read it, ask "update or rewrite?".

If there is no GDD at all: warn that mapping from memory will drift, suggest running
`/design-system` first — but allow continuing if the designer wants to.

## 1. Interview — the five components

Ask in order, **one question at a time**, using `AskUserQuestion` where the answer set is
finite and prose where free description is needed.

1. **INPUT** — "Which resource does the player **freely choose** to solve the puzzle with?"
   Dig further: is it ordered? is part of it hidden?
2. **MID** — `AskUserQuestion`: "Is there an intermediate queue between choosing and
   consuming?" → `Yes, finite capacity` / `Yes, unbounded` / `No MID`.
3. **OUTPUT** — "What must the player consume/solve to reach the goal?"
   Remind the designer: OUTPUT is **passive** — if the player can pick it directly, it is
   an INPUT, so re-map.
4. **ACTION** — "What is a move? What does the player tap or drag?"
5. **VISIBILITY** — "Which part of the INPUT and OUTPUT does the player see in advance?"

## 2. Win / Lose

- **Win**: defaults to "every OUTPUT is satisfied" — confirm with the designer.
- **Lose**: if there **is a MID** → propose the standard form *"MID is full ∧ no unit in the
  MID matches an open OUTPUT"*, and confirm.
  If there is **no MID** → **you must ask**: how does the player lose? (out of moves / out of
  turns / out of time / other). It cannot be left blank.

## 3. Conservation invariant

`AskUserQuestion`: "For each type (colour/kind), may total supply **exceed** total demand?"
→ `No, must be equal` (`Σ INPUT(c) = Σ OUTPUT(c)`) / `Yes, surplus allowed`
(`Σ INPUT(c) ≥ Σ OUTPUT(c)`).

This is the rule the level validator will later guard — get it wrong and every generated
level is wrong. Explain the consequence to the designer before asking.

## 4. The three consequences of MID — declare them now

Fill this table into the file; the values follow from question 2:

| | Value for this game |
|---|---|
| Lose condition | *(from section 2)* |
| Factor 2 *MID slack* | applies / **N/A** |
| `pressure(t)` implementation | `MID occupancy ÷ capacity` / *(designer's chosen proxy)* |

If there is **no MID**: ask for the `pressure(t)` proxy immediately — suggest
`1 − (legal moves remaining ÷ at match start)` or `turns used ÷ turns allowed`.

## 5. Mismatches

Ask: "Is there any game mechanic this model **cannot describe**?"
Common examples: real-time elements, opposition, between-match randomness.

Write it straight into the file. **Never hide it** — this is where the frame needs extending
or the game needs its own proxy, and later skills read this section to know where the
numbers cannot be trusted.

## 6. Write the file

Present the full draft in conversation, then ask:
> "Write this to `design/pipeline/flow-map.md`?"

File structure: the 5-component model · win/lose · invariant · the three-MID-consequences
table · mismatches · date + who was interviewed.

Anything the designer has not decided → write `UNDEFINED`. **Never fill it in.**

## Next

`/gd-core-difficulty` — define what makes it hard and how the core is played.

Full pipeline: `/gd-workflow-help`
