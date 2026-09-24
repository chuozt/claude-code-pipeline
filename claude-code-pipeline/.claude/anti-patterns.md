# Anti-patterns — What Is Forbidden

This list is drawn from real mistakes made on real projects, not from theory.
The AI must never do these, and must warn when it sees the developer about to.

---

## 1. Hardcoding

- **Gameplay values:** NEVER hardcode. Everything comes from external config/data.
- **Display text:** NEVER hardcode a string a player will read. Go through localisation.
- **Tag / Scene / Layer / Event:** no raw strings. Use constants or enums.
- **Magic numbers:** no unexplained numbers in logic. Name it or move it to config.

```csharp
if (score > 1000)  _level++;                         // WRONG
if (score > config.LevelUpThreshold)  _level++;      // CORRECT
```

---

## 2. UI Architecture

- **UI owning game state** — FORBIDDEN. UI displays, and sends commands/events to *request* change.
- **UI blocking the game loop** — FORBIDDEN. No synchronous waiting in UI.
- **Instantiating UI prefabs directly** — FORBIDDEN. Go through the project's UI framework.
- **Several screens in one prefab** — FORBIDDEN. One screen per prefab (merge-conflict protection).

---

## 3. Code Thinking

- **Hardcoding a mechanic into a central controller** — FORBIDDEN. Use interfaces, register dynamically.
  Seeing `if (type == MechanicType.X)` in a controller already means it is wrong.
- **`GameObject.Find()` / `GetComponent()` inside `Update()`** — FORBIDDEN.
- **`FindObjectOfType<T>()` outside editor code** — FORBIDDEN.
- **Static singletons holding game state** — FORBIDDEN. Use events + state pattern + DI.
- **Gameplay logic living in a MonoBehaviour** — FORBIDDEN. See `architect.md` §1.
- **Coroutines / animation inside the logic layer** — FORBIDDEN. Logic is instantaneous; only the view has time.
- **Mechanic A knowing mechanic B by concrete type** — FORBIDDEN. Go through an interface or an event.

---

## 4. Performance

- **Calling `GC.Collect()` by hand to "stop hitching"** — FORBIDDEN. It is what causes the hitch.
  *(A previous project had exactly one such call in an SDK's UI screen transition — the result was
  a freeze at the very moment it was meant to feel smooth.)* The only exception: handling the OS
  `Application.lowMemory` event.
- **`new` inside `Update`/`FixedUpdate`/a hot loop** — FORBIDDEN (GC garbage every frame).
- **Continuous Instantiate/Destroy for VFX, projectiles, floating text** — FORBIDDEN. Object pooling is mandatory.
- **String concatenation (`string +`) in a hot path** — FORBIDDEN.
- **Optimising by intuition** — FORBIDDEN. Profile first, quote the number, then change.

---

## 5. Data & Assets

- **Hand-editing a level file outside the tool** — FORBIDDEN.
- **Overwriting the designer's level/data without asking** — FORBIDDEN. Always export to a separate folder to compare.
- **Adding a new asset without its `.meta`** — FORBIDDEN. A missing `.meta` makes another machine
  import it with a fresh GUID and **every reference to that asset breaks**.
- **Deleting an asset without scanning `AssetDatabase.GetDependencies`** — FORBIDDEN. Do not rely on file names.
- **Hand-editing the contents of a `.meta` file** — FORBIDDEN.

---

## 6. Git & Multi-Developer Work

- **Pushing / switching branches** — FORBIDDEN. **Committing / merging unasked** — FORBIDDEN;
  when the developer asks, the AI commits (`/commit`) and merges (`--no-ff --no-commit`,
  resolve, gate, then commit) itself.
- **Committing straight to the default branch (`main`)** — FORBIDDEN.
- **Auto-merging `.unity` / `.prefab` / `.asset` files** — FORBIDDEN. YAML merges break often;
  keep one side wholesale and redo the rest by hand.
- **Editing `ProjectSettings/` unrequested** — FORBIDDEN.
  `TagManager.asset` is especially dangerous: losing layer names makes camera culling masks
  display wrongly in the Inspector even though the stored values are still correct — very hard to trace.
- **Editing files owned by another developer** — FORBIDDEN. Stop and report.
- **Editing files inside a third-party SDK folder** — FORBIDDEN. Handle it at the call site.

---

## 7. How To Work With AI

- **Guessing at a bug's cause instead of reading logs** — FORBIDDEN. Get the console logs first.
- **Patching the symptom instead of finding the root cause** — FORBIDDEN.
- **Presenting unverified information as fact** — FORBIDDEN. Tag it `[UNVERIFIED]`.
- **Saying "done" with no way to prove it** — FORBIDDEN. See `verification.md`.
- **Choosing for yourself on an ambiguous request** — FORBIDDEN when the two readings give
  different results. Ask.
- **Filling in a blank design decision on the designer's behalf** — FORBIDDEN. Write `UNDEFINED` and ask.

---

## 8. Level Design & Difficulty

- **Trusting that a clean validator means a playable level** — WRONG.
  *(Previous project: the validator reported 0 errors and 0 warnings across all 40 levels, but bot
  measurement found 14/40 levels with a 0–3% win rate, including one where only 2% of the content
  could be cleared before hard-locking.)*
- **Judging difficulty by eye** — FORBIDDEN. Run bots, take numbers.
- **Raising difficulty by piling on more mechanics** — FORBIDDEN. That is *fake difficulty*:
  confusing rather than hard, and it breaks the teaching rhythm.
- **Judging colour pairing from a level's overall palette** — WRONG. The player never sees the whole
  level at once. Judge it on a sliding window: the colours visible **simultaneously** at each moment.
- **Reviewing colour by eye alone** — NOT ENOUGH.
  *(A Blue+Cyan pair on a previous project measured ΔE 43.7 to normal vision — perfectly fine — but
  only 2.2 once red–green colour blindness was simulated. It appeared in 17/40 levels and no manual
  review session ever caught it.)*
