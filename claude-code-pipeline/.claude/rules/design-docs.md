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

## Which files are system GDDs

A **system GDD** is a `dev-*.md` under `design/dev-system/` — flat (`dev-<system>.md`) or, when
the developer docs follow the designer's tabs, inside a tab folder
(`design/dev-system/Mechanic Overview/dev-<system>.md`). These are **not** system GDDs and are
skipped by every skill that scans GDDs: `dev-map-systems.md`, `dev-architecture*.md`,
`dev-control-manifest.md`, `dev-tr-registry.yaml`, `dev-requirements-traceability.md`,
`dev-sim-conformance.md`, `dev-open-questions-*.md`, `dev-auto-verification-*.md`, and anything under `reviews/` or `adr/`.
