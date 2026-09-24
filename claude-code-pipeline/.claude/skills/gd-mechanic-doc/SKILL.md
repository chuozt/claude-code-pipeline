---
name: gd-mechanic-doc
description: Generate a mechanic design document skeleton with all 8 required sections, plus prompting questions for the designer to answer. Also used to review an existing doc for missing sections. Use when the developer types /gd-mechanic-doc or says "write a mechanic doc", "is this doc complete", "the designer sent an incomplete doc".
---

# /gd-mechanic-doc — mechanic document skeleton

## Modes

- `/gd-mechanic-doc <mechanic name>` → generate a new skeleton
- `/gd-mechanic-doc review <path>` → review an existing doc and point out what is missing

## The most important rule

**Never invent design content.** For each section, write the **prompting questions** the
designer needs to answer. Anything they have not decided stays as `UNDEFINED`.

## The 8 required sections

1. **Overview** — what is this mechanic, in one paragraph?
2. **Player Fantasy** — what must the player *feel*? Where is the satisfying moment?
3. **Detailed Rules** — exact, unambiguous rules. Who triggers it? Under what conditions? With what result?
4. **Formulas** — every formula, each variable defined, with units and valid ranges.
5. **Edge Cases** — see the checklist below.
6. **Dependencies** — which systems does it depend on? Which existing mechanics collide with it?
7. **Tuning Knobs** — which values are tunable, and in which config asset?
8. **Acceptance Criteria** — acceptance conditions **verifiable by machine or by number**.

## Edge case checklist — forgotten in every kind of mechanic

- Permanent deadlock: can this mechanic make a match unwinnable? What is the safety condition?
- Interaction: what happens with **each** existing mechanic? Build an **NxN matrix**; anything
  unclear goes in as `UNDEFINED` — that is a question for the designer, not a place for the AI to guess.
- End-of-level state: if a win/lose happens mid-flight, what does the mechanic leave behind?
- Quit and return mid-level: how is it restored?
- Limits: how many can exist at once? What happens beyond that?
- Unlocking: at which level? How is the player taught?

## After the doc exists

Remind the developer: this mechanic must touch all **5 touch points** —
logic · config SO · level format · validator rules · simulator branch.
Missing one produces silent bugs.

Full template: `.claude/templates/GDD_Mechanic.md`
