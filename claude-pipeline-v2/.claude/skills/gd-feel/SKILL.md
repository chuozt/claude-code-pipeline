---
name: gd-feel
model: claude-opus-5-5
effort: medium
description: Add juice and game feel to gameplay that already works correctly - screen shake, tweens, particles, layered audio, haptics, hitstop - without touching a logic file. Use when the developer types /gd-feel or says "add juice", "make it feel good", "polish the feel", "add an effect when", "the hit feels weak".
---

> **Coding rule — mandatory.** Every line of C# this skill writes, reviews, or proposes must follow `.claude/coding_convention.md` (Allman braces, §9 script layout, field order and naming, `GameDebug` instead of `Debug.Log`, no `{ get; private set; }`). Where any sample or advice below disagrees with that file, the convention wins.

# /gd-feel — making the game feel good

## Precondition — check this first

Are the core logic and all mechanics **frozen**? If not, tell the developer: polishing on
logic that is still changing means doing the work twice.

## Rule #1 — DO NOT TOUCH LOGIC FILES

Everything added lives in the **view / presentation** layer.

If attaching an effect seems to require editing a logic file: **STOP, tell the developer,
explain why.** That is a sign the view layer is missing an event — the right fix is to
**add the event**, not to push effect code into the logic. At the end, prove with
`git diff` that no logic file was touched.

## The Three Channels Rule

Every meaningful action needs feedback on **at least 3 channels**:
**Visual** (tween, particle, flash, shake) · **Audio** (SFX) · **Tactile** (haptics, hitstop).

Missing a channel is what makes an action feel hollow.

## Feel checklist

- [ ] **Anticipation** — is the action telegraphed before it happens?
- [ ] **Impact** — does the contact/explosion moment carry weight (scale punch, flash, brief freeze frame)?
- [ ] **Follow-through** — is there an aftermath (falling particles, light rumble, audio tail)?
- [ ] **Layered audio** — ambience / action / feedback / reward, without stacking into noise
- [ ] **Failure should feel good too** — do not reserve effects for success
- [ ] **Escalation** — does a success streak build (rising pitch, growing effect size)?
- [ ] **No dead waits** — where does the player wait with nothing happening?

## Shared patterns

Hitstop:
```csharp
private async UniTask Hitstop(float duration = 0.05f, CancellationToken ct = default)
{
    Time.timeScale = 0f;
    await UniTask.Delay(TimeSpan.FromSeconds(duration), DelayType.UnscaledDeltaTime,
                        cancellationToken: ct);
    Time.timeScale = 1f;
}
```

Shake profiles live in an asset, so the designer tunes them in the Inspector and no number is
scattered through code. One asset per strength (Light / Medium / Heavy); the view holds a
`[SerializeField]` reference to the one it needs:
```csharp
[CreateAssetMenu(menuName = "Game/Feel/Shake Profile", fileName = "ShakeProfile")]
public sealed class ShakeProfile : ScriptableObject
{
    [Tooltip("Seconds. Starting points: Light 0.10, Medium 0.20, Heavy 0.35.")]
    [Min(0f)] [SerializeField] private float duration = 0.2f;
    [Tooltip("World units. Starting points: Light 0.1, Medium 0.3, Heavy 0.6.")]
    [Min(0f)] [SerializeField] private float magnitude = 0.3f;

    public float Duration => duration;
    public float Magnitude => magnitude;
}
```

## Mandatory

- **Pool** everything spawned continuously: particles, floating text, audio. No
  Instantiate/Destroy during gameplay.
- **`Kill()` tweens** in `OnDisable`/`OnDestroy`. A tween outliving its object is a very
  hard bug to find.
- Every feel parameter lives in a **ScriptableObject**, never hardcoded — the designer must be able to tune it.
- With sound and haptics disabled in Settings, the game must still work correctly.
- Never drop below the target FPS. Run `/gd-perf` afterwards to confirm.

## References

- Game feel theory: `.claude/kb/goc/game-feel/gamefeel-theory.md`
- DOTween: the `lib-dotween` skill
- PrimeTween: `.claude/kb/middleware/tools-unity-primetween/SKILL.md`
- Shaders as a feel channel: `.claude/kb/goc/game-feel/shaders-as-feel.md`
