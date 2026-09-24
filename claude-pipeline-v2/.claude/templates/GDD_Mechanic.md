# GDD — Mechanic: <NAME>

> Status: Draft | In review | Approved
> Author: <designer>   ·   Updated: <date>   ·   Unlocks at level: <N>
> Related: <other mechanics/systems>

---

## 1. Overview
<One paragraph. What this mechanic is, what the player does with it.>

## 2. Player Fantasy
<What must the player FEEL? Where is the satisfying moment? Why is it fun?>

## 3. Detailed Rules
<Exact, unambiguous rules.>
- Triggers when: 
- Preconditions: 
- Result: 
- Ends when: 

## 4. Formulas
| Symbol | Meaning | Unit | Valid range |
|---|---|---|---|
|  |  |  |  |

<The full formulas. Every variable must appear in the table above.>

## 5. Edge Cases

| Situation | Handling |
|---|---|
| Can it make a match **unwinnable**? What is the safety condition? |  |
| State at **level end** (win/lose while this is mid-flight) |  |
| Player **quits mid-level** and returns |  |
| **Limits** — how many can exist at once? What happens beyond that? |  |
| Conflict with **booster / revive / hint** |  |

### Interaction matrix with existing mechanics
<Anything undecided goes in as UNDEFINED — that is a question for the designer, not a
place for the AI to guess.>

|  | Mechanic A | Mechanic B | Mechanic C |
|---|---|---|---|
| This mechanic |  |  |  |

## 6. Dependencies
- Systems depended on: 
- Data to add to the level format: 
- Mechanics that must exist first: 

## 7. Tuning Knobs
| Parameter | Default | Range | Config asset it lives in |
|---|---|---|---|
|  |  |  |  |

## 8. Acceptance Criteria
<Must be VERIFIABLE by machine or by number. "Feels satisfying" is not a criterion.>
- [ ] 
- [ ] 
- [ ] 

---

## 9. Teaching The Player
- Introduction level: <N>  · Tutorial: <yes/no, what kind>
- At the introduction level, is this the **only** mechanic present? <yes/no>

## 10. Five Touch Points Checklist (filled in by the developer)
- [ ] Logic — the mechanic class implements the interface, with no central controller change
- [ ] Config — its own ScriptableObject for tuning knobs
- [ ] Level format — the new data field + its place in the level tool
- [ ] Validator — validity rules, **and a check for the case where it should be present but is missing**
- [ ] Simulator — the bot understands this mechanic
