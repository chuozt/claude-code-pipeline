---
paths:
  - "**/Scripts/**/Gameplay/**"
  - "**/Scripts/**/Mechanics/**"
---

# Rules For Gameplay Code (Model)

- **No** MonoBehaviour inheritance, **no** touching `UnityEngine.Object`, **no**
  coroutines, **no** `deltaTime`. This is the pure logic layer — every operation
  is instantaneous and deterministic.
- **No** references to UI code. Communicate through events/interfaces.
- **Every gameplay value** comes from external config/data. No hardcoding, no magic numbers.
- Every gameplay system implements an explicit interface.
- A state machine must have a **transition table** written in the docs that matches the code.
- Randomness must take a **seed** so a match can be reproduced.
- State in a comment which design document this code implements.

## Quick check
```bash
grep -rn "UnityEngine" <model directory>/    # any hit = already wrong
```

## Examples

Correct:
```csharp
private readonly int _maxAmmo;
public bool CanFire => _currentAmmo > 0 && !_isLocked;
```

Wrong:
```csharp
public class BoardModel : MonoBehaviour        // VIOLATION: model tied to MonoBehaviour
{
    private float _speed = 5f;                 // VIOLATION: hardcoded, and a model should have no speed
}
```
