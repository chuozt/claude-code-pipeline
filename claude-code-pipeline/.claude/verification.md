# Verification-Driven — Proving The Code Works

**No evidence, no "done".**
This is the most commonly skipped section, and the most expensive one to skip.

---

## 1. Evidence By Kind Of Work

| Kind of work | Required evidence | Level |
|---|---|---|
| **Logic** (formulas, state machines, algorithms) | EditMode unit test **PASSING** | **BLOCKING** |
| **Multi-system interaction** | Integration test **or** a recorded play-through scenario | **BLOCKING** |
| **Levels / balance** | Bot runs over N attempts + a results table | **BLOCKING** |
| **Performance** | Profiler numbers before/after, naming the device | **BLOCKING** |
| **UI / screens** | Screenshots or a step-by-step walkthrough | Recommended |
| **Feel** (VFX, animation, audio) | Video/images + developer sign-off | Recommended |
| **Config / tuning** | Smoke test covering the main flow | Recommended |

**BLOCKING** = without it, the work does not count as done.

---

## 2. Rules For Automated Tests

- **Naming:** file `<System>_<Feature>Tests.cs`; method `<Situation>_<ExpectedResult>()`
  → `Fire_WithZeroAmmo_DoesNotConsumeSlot()`
- **Deterministic:** the same input gives the same result on every run. No unseeded
  randomness, no dependency on wall-clock time.
- **Independent:** every test builds and tears down its own state. No dependency on run order.
- **No scattered magic numbers:** use constants or a factory. Exception: boundary tests,
  where the number itself is the thing under test.
- **No external I/O:** no API calls, no file reads, no databases. Substitute via DI.
- EditMode for pure logic; PlayMode for MonoBehaviour lifecycles.

---

## 3. What NOT To Automate

Do not waste effort writing tests for:
- Visual fidelity (shader output, VFX shape, animation curves)
- "Feel" (input responsiveness, weight, timing)
- Platform-specific rendering — needs a real device, cannot run headless
- A whole play session — that is playtesting, not automation

These need **a human to look**, not an assert.

---

## 4. How To Prove It, By Project Stage

**Level tool**
- Feed it deliberately broken levels → the validator must catch 100% of them
- The simulator runs N attempts per level in under X seconds
- A designer can build a level from blank to playable without asking a developer

**Core game**
- Runs start → Win, and start → Lose **for every listed lose condition**
- A bot plays 100 matches with no exceptions and no hangs
- The same seed gives the same result
- `grep UnityEngine` in the model folder returns nothing

**Mechanic**
- Fully enabled/disabled from level data, with no core line changed
- All **5 touch points** covered (logic · config · level format · validator · simulator) — list them
- Every edge case in the doc has a test or a sample level proving it

**UI**
- Every screen flow completes without errors
- Correct at the narrowest and the widest supported aspect ratio
- No `Find()` left, no hardcoded text left

**Polish**
- Holds the target FPS on the reference device across 10 minutes of continuous play
- No frame spike when the largest effect fires
- GC allocation ≈ 0 B/frame in the gameplay loop
- **No logic file modified** — prove it with `git diff`

**Level review**
- Every level measured by bot, with a win-rate table and a pressure curve
- No colour pair below the colour-blindness threshold
- Difficulty labels match the measurements

---

## 5. Honesty About Error Margins — Mandatory

When reporting any number:

- Numbers produced by a **bot**, not a human → the ranking between levels is trustworthy,
  the absolute values are not
- State the **variance between two runs**. Real example: 100 attempts on a hard level can
  differ by up to **±8 percentage points**
- A metric that correlates **weakly** with real outcomes is a **diagnostic** tool,
  not a **predictive** one — call it that
- Which device, which build, which date
- The measurement conditions (boosters/revives allowed? which bot level?)

**Never present an estimate as if it were a measurement.**

---

## 6. Questions The Developer Should Ask When The AI Says "Done"

1. *"How do you prove it works?"*
2. *"Does Unity compile cleanly?"*
3. *"Is any part of the request still unimplemented?"*
4. *"Which parts do you know for sure, and which are you guessing at?"*
5. *"When this breaks, how does it break?"*
