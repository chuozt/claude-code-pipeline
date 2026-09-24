---
paths:
  - "**/docs/features/**"
  - "**/design/**"
---

# Rules For Design Documents

Every mechanic document **must have all 8 sections**:
Overview · Player Fantasy · Detailed Rules · Formulas · Edge Cases · Dependencies ·
Tuning Knobs · Acceptance Criteria

- **Formulas:** define every variable, its unit, and its valid range.
- **Edge Cases:** at minimum answer — when can a permanent deadlock happen? how does
  it interact with other mechanics? what is the state at level end? what if the
  player quits mid-level?
- **Acceptance Criteria** must be **verifiable**. "Feels satisfying" is not a
  criterion; "60fps on the reference device with 50 simultaneous effects" is.
- **Tuning Knobs** must name the exact config asset that holds each one.
- Anything the designer has not decided → write `UNDEFINED`. **The AI must not fill it in.**
- Doc and code disagree → fix the doc in the same change.
