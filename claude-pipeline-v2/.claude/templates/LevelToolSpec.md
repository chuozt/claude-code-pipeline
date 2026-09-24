# Level Tool Specification — <GAME NAME>

## 1. Data structure of one level
| Field | Type | Meaning | Limits |
|---|---|---|---|
|  |  |  |  |

## 2. INVARIANTS — what makes a level valid
<The most important part of the whole tool. Every invariant must be machine-checkable.>

| # | Invariant | Why | Consequence if violated |
|---|---|---|---|
| 1 | <e.g. total resources of each colour = number of cells needing that colour> | | Level is unwinnable, and the eye cannot see it |
| 2 | | | |

## 3. Limits
- Maximum size: 
- Maximum number of colours: 
- Maximum mechanics per difficulty label: 

## 4. Mechanics placeable in the tool
| Mechanic | In v1? | Unlock level | Placement constraints |
|---|---|---|---|
|  |  |  |  |

## 5. Six things v1 must have
- [ ] **Validator** — checks every invariant in §2, reports errors with an exact location, has a check-everything button
- [ ] **Headless simulator** — a bot plays N attempts without Play mode, returning win rate + pressure curve
- [ ] **Colour check** — CIEDE2000 + red–green colour-blindness simulation (thresholds 18 / 12)
- [ ] **Undo/Redo** through `Undo.RecordObject`
- [ ] **JSON import/export** + a build step for the release format
- [ ] **A "play this now" button** that loads the open level straight into the gameplay scene

## 6. Export formats
| Environment | Format | Location | Loaded by |
|---|---|---|---|
| Development | JSON | | |
| Release | encrypted `.bytes` | | |

## 7. Data safety
- [ ] Never overwrite an existing level without confirmation
- [ ] Has a mode that exports to a separate folder for comparison before replacing
- [ ] Generated assets ship with their `.meta`

## 8. Static methods callable by AI / CI
| Method | Returns | Used for |
|---|---|---|
| `ValidateAllToJson()` | JSON list of errors | AI, CI |
| `SimulateLevelToJson(id, runs)` | JSON win rate + pressure curve | AI, level review |

## 9. Acceptance
- [ ] A designer builds a level from blank to playable in ≤ 10 minutes without asking a developer
- [ ] The validator catches 100% of errors in a deliberately broken level set
- [ ] The simulator runs 100 attempts per level in under <X> seconds per level
- [ ] No balance value lives inside the tool's code
