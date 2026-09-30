---
name: gd-map-flow
description: "Map a puzzle game onto the GIVEN/BUFFER/GOAL model of the resource-flow frame — the mandatory gateway before any difficulty or level-generation work. Use when the developer/designer types /gd-map-flow or says 'map the game onto the frame', 'what is this game's GIVEN', 'start the level pipeline'."
argument-hint: "[game name, or leave empty to use the current game]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Edit, AskUserQuestion
---

The gateway to the whole level pipeline. Run **once per game**. Without this file the later
skills have no shared vocabulary to talk in.

**In**: the existing GDDs (`design/gdd/`) · **Out**: `design/pipeline/flow-map.md`

> **Question style — `gd-mode` §1, §3.1, §3.2, §5, §7 apply to every question below, mode on
> or off.** The words GIVEN / BUFFER / GOAL / invariant / `pressure(t)` are for the **file**.
> The designer hears a moment of play, in the game's own nouns, one question at a time. Each
> step below gives the question in that form; the framework word next to it is what you write.

## 0. Load context

1. Read `.claude/reference/resource-flow-difficulty-framework.md` **section 2** — that is the
   model definition and the law for this skill.
2. Read `design/gdd/game-concept.md` and `design/gdd/systems-index.md` if they exist. **Pull
   the game's own nouns from them** (what the player taps, where things wait, what gets
   filled) — every question below is asked in those nouns, never in the tray/tube examples.
3. If `design/pipeline/flow-map.md` already exists → read it, ask "update or rewrite?".

If there is no GDD at all: warn that mapping from memory will drift, suggest running
`/design-system` first — but allow continuing if the designer wants to.

## 1. Interview — the five components

Ask in order, **one question at a time**. Open with what the GDD already says, in the form
*"as I read it, the player …  — right?"*, so the designer confirms or corrects rather than
explains from zero.

| # | Writes to file | Ask the designer *(examples from a tray-sorting game — rebuild in this game's nouns)* |
|---|---|---|
| 1 | **GIVEN** | "What does the player actually pick up or tap to make progress — the pieces on the board, right? Are they in a fixed order, or can any of them be taken? And can the player see all of them from the start, or are some covered up?" |
| 2 | **BUFFER** (`AskUserQuestion`) | "When the player taps a piece and there is no tray for it yet, where does it go?" → `It waits in a spot with a fixed number of slots` · `It waits somewhere with no limit` · `Nothing waits — a piece can only be taken when there is a place for it` |
| 3 | **GOAL** | "And what has to be filled up or cleared for the level to be won — the trays? Does the player ever pick *which* tray to fill, or does the piece just go where it fits?" — if the player chooses it directly, it is not GOAL but GIVEN: say so plainly (*"so the trays are something the player picks too — let me redo the picture"*) and re-map. |
| 4 | **ACTION** | "So one move is: tap a piece, and it flies to its tray. Anything else that counts as a move — rotating the view, swapping, holding?" |
| 5 | **VISIBILITY** | "Before making a move, what can the player already see coming — the next trays, the pieces underneath, the colour of what is hidden? And what do they have to guess?" |

**Do not say** "resource", "freely choose", "intermediate queue", "consume", "passive". If
the designer uses a framework word first, keep it.

## 2. Win / Lose

- **Win**: "The level is won when every tray is full — nothing else? No score target, no
  timer?" → default `every GOAL satisfied`, confirm.
- **Lose**, if there **is a BUFFER**: "And the player loses when the waiting slots are all full
  and nothing in them fits any open tray — that is the moment, yes?" → standard form
  *`BUFFER full ∧ no unit in the BUFFER matches an open GOAL`*, confirm.
- **Lose**, if there is **no BUFFER** → **you must ask**: "How does a player lose this game —
  they run out of moves, out of turns, out of time, or something else?" It cannot be left
  blank.

## 3. Conservation invariant

`AskUserQuestion`, in these words:

> "Count one colour in a level — say blue. Do the blue pieces always come out **exactly
> even** with the blue tray spaces, or can a level have **more** blue pieces than spaces, so a
> few are left over at the end?"
> → `Always exactly even` · `Leftovers are allowed`

Say why it matters before asking, in one line: *"this is the one rule every generated level
gets checked against — if we get it wrong, every level is wrong."*

Writes to file: `Σ GIVEN(c) = Σ GOAL(c)` or `Σ GIVEN(c) ≥ Σ GOAL(c)`.

## 4. The three consequences of BUFFER — declare them now

Fill this table into the file; the values follow from question 2. **Nothing here is asked
of the designer** — it is derived, and the `pressure` row is a developer choice:

| | Value for this game |
|---|---|
| Lose condition | *(from section 2)* |
| Factor 2 *Buffer Room* | applies / **N/A** |
| `pressure(t)` implementation | `BUFFER occupancy ÷ capacity` / `[DEV DEFAULT]` *(see below)* |

If there is **no BUFFER**: write `pressure(t) = 1 − (legal moves remaining ÷ at match start)`
tagged `[DEV DEFAULT]` (alternative: `turns used ÷ turns allowed`), and tell the designer in
one line: *"the developer will confirm how 'how close to losing' gets counted in a game with
no waiting slots — nothing for you to decide here."* (`gd-mode` §3.2.)

## 5. Mismatches

Ask: "Is there anything in this game that the picture we just drew **does not cover**?
Things like: a clock running, an opponent, dice or random draws between levels, a booster
that breaks the rules."

Write it straight into the file. **Never hide it** — this is where the frame needs extending
or the game needs its own proxy, and later skills read this section to know where the
numbers cannot be trusted.

## 6. Write the file — two columns

Present the full draft in conversation, then ask:
> "Write this to `design/pipeline/flow-map.md`?"

The five components and the invariant are written as a **two-column table** (`gd-mode` §8):

| Component | In the designer's words | Framework value |
|---|---|---|
| GIVEN | pieces on the board, any order, some hidden under others | ordered: no · partially hidden: yes |
| BUFFER | 7 waiting slots under the board | capacity 7 |
| … | | |

File structure: the 5-component table · win/lose · invariant · the three-BUFFER-consequences
table · mismatches · date + who was interviewed.

Anything the designer has not decided → write `UNDEFINED`. **Never fill it in.**

## Next

`/gd-core-difficulty` — define what makes it hard and how the core is played.

Full pipeline: `/gd-workflow-help`
