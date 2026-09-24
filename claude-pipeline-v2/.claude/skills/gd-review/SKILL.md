---
name: gd-review
model: claude-opus-5-5
effort: medium
description: Review Unity C# code before a commit or merge against the project's coding convention and anti-patterns. Reports only real problems with file and line numbers, and changes nothing. Use when the developer types /gd-review or says "review this code", "check before merging", "is this code OK".
---

> **Coding rule — mandatory.** Every line of C# this skill writes, reviews, or proposes must follow `.claude/coding_convention.md` (Allman braces, §9 script layout, field order and naming, `GameDebug` instead of `Debug.Log`, no `{ get; private set; }`). Where any sample or advice below disagrees with that file, the convention wins.

# /gd-review — pre-commit review

Read the files under review **in full** before judging. Report only **real** problems, each
with `file:line`. Do not list style opinions. **Change nothing** — the developer decides.

## 1. Architecture (heaviest)
- [ ] Gameplay logic is **not** inside a MonoBehaviour; the model never touches `UnityEngine.Object`
- [ ] Mechanics are **not** hardcoded into a central controller (`if (type == X)`)
- [ ] No static singleton holding game state
- [ ] UI does not own or directly mutate game state
- [ ] asmdef references are one-way, with no cycles

## 2. Anti-patterns
- [ ] No hardcoded gameplay values, display text, tag/scene/layer strings
- [ ] No `GameObject.Find()` / `GetComponent()` inside `Update`
- [ ] No `FindObjectOfType` outside editor code
- [ ] No manual `GC.Collect()` (except handling `Application.lowMemory`)

## 3. Convention
- [ ] `[SerializeField] private` without underscore · ordinary `private` with underscore · `static` with `s_`
- [ ] Declaration order: SerializeField → private → properties → events
- [ ] Memory alignment within each group
- [ ] Properties use a backing field + expression body, **not** an auto-property with a private setter
- [ ] Methods ≤ 40 lines, complexity ≤ 10
- [ ] Comments in English, explaining WHY only
- [ ] Class name matches the file name

## 4. Lifecycle & leaks
- [ ] Events unsubscribed where they were subscribed
- [ ] Tweens `Kill()`ed in `OnDisable`/`OnDestroy`
- [ ] Async operations take a `CancellationToken` and are cancellable
- [ ] `TryGetComponent` instead of `GetComponent` when the component may be absent

## 5. Performance
- [ ] No `new` in hot loops
- [ ] VFX / audio / continuously spawned objects are pooled
- [ ] No string concatenation in a hot path
- [ ] Time-dependent calculations use `deltaTime`

## 6. Data & assets
- [ ] New assets come with their `.meta`
- [ ] `ProjectSettings/` not modified unintentionally
- [ ] No files inside third-party SDK folders modified
- [ ] Designer-owned level/data files not overwritten

## 7. Evidence
- [ ] New logic has EditMode unit tests and they PASS
- [ ] Fixed bugs have a regression test
- [ ] `docs/features/*.md` matches the new behaviour

## Response format

```
## Review — <scope>

Architecture   [Pass | N issues]
Anti-patterns  [Pass | N issues]
Convention     [Pass | N issues]
Lifecycle      [Pass | N issues]
Performance    [Pass | N issues]
Data/assets    [Pass | N issues]
Evidence       [Pass | N issues]

Must fix before committing:
- <file:line> — <problem> — <the concrete failure scenario>

Should fix (non-blocking):
- ...
```

Every reported problem must come with a **concrete failure scenario**, never a generality.
