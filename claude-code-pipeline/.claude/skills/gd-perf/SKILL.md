---
name: gd-perf
model: claude-opus-5-5
effort: medium
description: Find and rank performance bottlenecks in a Unity mobile project - GC allocation, draw calls, hot paths, leaks - with estimated benefit and effort. Analysis only, changes nothing. Use when the developer types /gd-perf or says "the game stutters", "fps drops", "the phone gets hot", "optimise performance", "GC spike".
---

> **Coding rule — mandatory.** Every line of C# this skill writes, reviews, or proposes must follow `.claude/coding_convention.md` (Allman braces, §9 script layout, field order and naming, `GameDebug` instead of `Debug.Log`, no `{ get; private set; }`). Where any sample or advice below disagrees with that file, the convention wins.

# /gd-perf — performance inspection

**Rule #1: MEASURE BEFORE CHANGING.** No optimising by intuition. Every proposal comes with numbers.

## Phase 1 — Load the budget

From `CLAUDE.md` / `project_setup.md`:
- What is the reference device?
- Target FPS → frame budget: **16.6 ms** (60fps) or **33 ms** (30fps)
- Memory, draw call and SetPass call budgets
- Are there quality tiers by device class?

Any number missing → ask the developer, do not invent it.

## Phase 2 — Static scan (this skill can do it)

**CPU / GC**
- Every `Update` / `FixedUpdate` / `LateUpdate` — list them all, estimate cost
- `new` in hot loops: arrays, Lists, closures, LINQ, boxing
- String concatenation in hot paths
- `GetComponent` / `Find` / `FindObjectOfType` inside loops
- Nested loops over large sets
- Per-frame physics queries (raycast, overlap) — is there a `NonAlloc` variant?

**Memory**
- Continuous Instantiate/Destroy that is not pooled
- Wrong texture / audio import settings (uncompressed, unsuitable mipmaps)
- Leaked references: unsubscribed events, surviving tweens, statics holding objects
- Caches that grow without an eviction policy

**Rendering**
- Draw call estimate, materials that cannot batch
- Overdraw from stacked transparent objects
- Uncapped particle systems
- Missing LOD / occlusion culling

**I/O**
- Synchronous asset loading on the main thread
- Save/load running inside a gameplay frame

## Phase 3 — Confirm with the Profiler

The static scan produces **candidates**; only the Profiler **confirms**. State clearly which
items need real measurement:
- Profiler → CPU → Hierarchy, enable the **GC Alloc** column and **allocation call stacks**
- **Deep Profiling adds 10–100x overhead** — its numbers do not reflect reality.
  Use `ProfilerMarker` to scope regions instead.
- Measure on a **real device**, not the Editor. The Editor always lies about performance.

## Phase 4 — Report

```
## Performance — <scope>

### Budget
| Metric | Budget | Current estimate | Status |

### Bottlenecks (ranked by benefit/effort)
| # | file:line | Problem | Estimated benefit | Effort | Risk |

### Quick wins (< 1 hour each)

### Needs real measurement before any conclusion
```

## Rules

- Never optimise before measuring. "Feels slow" is not data.
- Every proposal needs an **estimated benefit**. "Make it faster" is not actionable.
- Measure on the reference device, not the dev machine.
- **Never propose `GC.Collect()`** to stop hitching — it is what causes the hitch.
  The only exception: handling the OS `Application.lowMemory` event.
- This skill **changes nothing**. The developer decides what to act on.
