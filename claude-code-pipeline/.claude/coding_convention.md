# Coding Conventions

Mandatory C# standards. Violating any item means the developer rejects the code.

---

## 1. Complexity Limits

- **Cyclomatic complexity ≤ 10** per method.
- **Method length ≤ 40 lines** (excluding data declaration lines).
- A file growing large → split with `partial class` by feature group,
  named `Class.FeatureGroup.cs` (e.g. `LevelToolWindow.Mechanics.cs`).

---

## 2. Naming & Field Ordering

| Kind | Rule | Example |
|---|---|---|
| `[SerializeField] private` | **No** leading underscore | `[SerializeField] private int maxAmmo;` |
| Ordinary `private` | Leading underscore **required** | `private int _currentAmmo;` |
| `static` field | `s_` prefix | `private static int s_instanceCount;` |
| Public property, method | PascalCase | `public int MaxAmmo => maxAmmo;` |
| Parameter, local | camelCase | `void Fire(int shotCount)` |
| Boolean | `is/has/can/should` prefix | `_isPaused`, `CanShoot` |
| Event | `On` prefix | `OnLevelCompleted` |
| Constant | PascalCase | `private const int MaxRetry = 3;` |

**Declaration order inside a class:**
1. `[SerializeField] private` → 2. ordinary `private` → 3. public properties → 4. events

**Memory alignment** — within each group, order by descending size:
8-byte (reference / `long` / `double`) → 4-byte (`int` → `float`) → 2-byte (`short`) → 1-byte (`bool` / `byte`)

Within the 4-byte group, `int` comes before `float`.

```csharp
private Tween _punch;       // 8-byte
private int _streak;        // 4-byte: int first
private float _remaining;   // 4-byte: then float
private bool _isDraining;   // 1-byte
```

**Clustering:** fields in the same logical group get **no** blank line between them;
different logical groups are separated by **exactly one** blank line.

---

## 3. Properties & State

Private backing field + expression body is required:

```csharp
private bool _isPaused;
public bool IsPaused => _isPaused;          // CORRECT
```

```csharp
public bool IsPaused { get; private set; }  // WRONG — auto-property with a private setter is forbidden
```

---

## 4. Method Structure & Formatting

- **Time:** `deltaTime` is required for **ALL** time-dependent calculations.
- **Single statement:** in `if/else/for`, a single statement drops the braces and moves to its own line:
  ```csharp
  if (_isPaused)
      return;
  ```
- **Long operators:** `&&` / `||` go at the **start of the new line**, aligned:
  ```csharp
  if (_isReady
      && _ammo > 0
      && !_isPaused)
  ```
- **Allman braces:** every opening brace goes on **its own line** (namespace, type, method,
  property body, `if`/`else`/`for`/`while`/`switch`, `try`/`catch`). Indent **4 spaces**, never tabs.
  ```csharp
  private void HideAfterDelay()
  {
      try
      {
          Hide();
      }
      catch (OperationCanceledException)
      {
          GameDebug.Log("Hide cancelled");
      }
  }
  ```
- **Blank lines:** separate logical steps inside a method with one blank line, including one
  after an early `return`.
  ```csharp
  if (_hideCts == null)
      return;

  _hideCts.Cancel();
  _hideCts.Dispose();
  ```
- Class name matches the file name.

---

## 5. Comments

- **English only.** Concise.
- **Comment ONLY to explain "WHY".** Never restate what the code already says.
- `/// <summary>` goes only on **public API that needs explaining** — not sprayed over every class.

```csharp
// Shooter only leaves the holder when ammo hits zero, so an unreachable
// colour strands it for the rest of the match.
if (shooter.Ammo > 0)
    return false;
```

---

## 6. Null Safety & Lifecycle

- `TryGetComponent<T>(out var x)` instead of `GetComponent<T>()` when a component may be absent.
- Beware Unity's **fake null**: a destroyed object is still non-null to plain C#.
  Pure C# model classes **must not** rely on Unity-style null comparison.
- Subscribe and unsubscribe in matching places: `OnEnable`↔`OnDisable`, `Awake`↔`OnDestroy`.
- Tweens must be `Kill()`ed in `OnDisable`/`OnDestroy` — a tween outliving its object is a very hard bug to find.

---

## 7. Async

- Follow the project's async standard (`UniTask` or `Awaitable` — stated in `project_setup.md`).
  **Never mix the two** within one system.
- Every long-running async operation takes a `CancellationToken` and is cancellable on destroy.
- Catch `OperationCanceledException` separately; never swallow it into a general `catch (Exception)`.
- **Never** call Unity APIs from a background thread.

---

## 8. Logging

Use a wrapper (stripped in release via `[Conditional]`), not bare `Debug.Log`:

```csharp
GameDebug.Log("Level loaded", LogTopic.Gameplay);
// Output: [Gameplay] LevelLoader.Load:42 — Level loaded
```

The wrapper uses `[CallerFilePath]` / `[CallerMemberName]` / `[CallerLineNumber]` to
supply context automatically — never describe where a log came from by hand.
Full template: `.claude/skills/gd-bug/SKILL.md`.

---

## 9. Script Layout

Every script is laid out in the same order, so any file reads top to bottom the same way.

**File**
1. `using` directives: `System.*` first, then the rest alphabetically. No unused ones.
2. `namespace` matching the folder path.
3. One public type per file, named like the file (a `partial` split is `Class.FeatureGroup.cs`).

**Members** (one blank line between groups, none inside a group)
1. Constants.
2. `[SerializeField] private` fields, grouped under `[Header("…")]` by what they configure
   (References / Timing / Look …). Inside each group, sort by size (§2).
3. `static` fields.
4. Ordinary `private` fields.
5. Public properties.
6. Events.

**Methods** (one blank line between methods)
1. Unity messages: setup and teardown first, in pairs, so each subscribe sits next to its
   unsubscribe (`Awake`, `OnEnable`, `OnDisable`, `Start`, `OnDestroy`). Then the per-frame
   ones (`Update`, `FixedUpdate`, `LateUpdate`).
2. Public methods.
3. Event handlers (`Handle…`).
4. Private helpers, grouped by topic (visuals, async, setup…). Each group is ordered as its
   methods are first called.

**Inside a method:** Allman braces, and a blank line between logical steps (§4).

**No `#region`, no banner comments.** The fixed order is what makes a file easy to scan, and
comments still explain only *why* (§5).

```csharp
using System;
using DG.Tweening;
using UnityEngine;

namespace Game.UI
{
    public sealed class StreakMeterView : MonoBehaviour
    {
        private const float MinDrainSeconds = 0.1f;

        [Header("References")]
        [SerializeField] private ComboTracker combo;
        [SerializeField] private RectTransform fill;

        [Header("Look")]
        [SerializeField] private int maxStreakShown = 99;
        [SerializeField] private float punchScale = 1.2f;
        [SerializeField] private float punchSeconds = 0.25f;
        [SerializeField] private bool isPunchEnabled = true;

        private static string[] s_numbers;

        private Tween _punch;
        private int _streak;
        private float _remaining;
        private bool _isDraining;

        public int Streak => _streak;

        public event Action OnMeterEmptied;

        private void OnEnable()
        {
            combo.OnStreakChanged += HandleStreakChanged;
        }

        private void OnDisable()
        {
            combo.OnStreakChanged -= HandleStreakChanged;
            _punch?.Kill();
        }

        private void Update()
        {
            if (!_isDraining)
                return;

            _remaining -= Time.deltaTime;
            SetFill(_remaining / MinDrainSeconds);
        }

        private void HandleStreakChanged(int streak)
        {
            _streak = streak;
            _isDraining = _streak > 0;

            if (isPunchEnabled)
                Punch();
        }

        private void SetFill(float share)
        {
            fill.localScale = new Vector3(Mathf.Clamp01(share), 1f, 1f);
        }

        private void Punch()
        {
            _punch?.Kill();
            _punch = transform.DOPunchScale(Vector3.one * (punchScale - 1f), punchSeconds);
        }
    }
}
```
