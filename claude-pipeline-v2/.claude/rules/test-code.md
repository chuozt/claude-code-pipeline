---
paths:
  - "**/Tests/**"
  - "**/*Tests.cs"
---

# Rules For Tests

- **Naming:** file `<System>_<Feature>Tests.cs`; method `<Situation>_<ExpectedResult>()`
- **Deterministic:** same input → same result every time. No unseeded randomness, no wall-clock dependency.
- **Independent:** each test sets itself up and tears itself down. No dependency on run order.
- **No external I/O:** no APIs, no files, no databases. Use DI to substitute.
- EditMode for pure logic; PlayMode for MonoBehaviour lifecycles.
- Tests live in their own `.asmdef` referencing the runtime asmdef.
- **Every fixed bug gets a regression test** that would catch it coming back.
- **Never disable or skip a failing test to make CI green.** Fix the cause.

## Do not write tests for
Visuals, feel, timing, platform-specific rendering, or a whole play session.
Those need a human to look at them.
