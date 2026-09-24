---
name: tools-unity-primetween
description: PrimeTween high-performance tweening library patterns for animations, UI, and sequencing.
---

# PrimeTween

## Overview

PrimeTween is a high-performance tweening library for Unity with zero allocations and excellent performance. This skill covers common patterns for UI animations, gameplay effects, and sequencing.

## When to Use

- UI animations (fade, scale, move)
- Gameplay animations (damage numbers, pickups)
- Camera effects
- Value interpolation
- Complex animation sequences

## Basic Tweens

### Transform Tweens

```csharp
using PrimeTween;

public class TransformAnimator : MonoBehaviour
{
    public void MoveToPosition(Vector3 target, float duration)
    {
        Tween.Position(transform, target, duration);
    }
    
    public void MoveWithEase(Vector3 target, float duration, Ease ease)
    {
        Tween.Position(transform, target, duration, ease);
    }
    
    public void ScalePunch()
    {
        Tween.Scale(transform, 1.2f, 0.1f, Ease.OutQuad)
            .Chain(Tween.Scale(transform, 1f, 0.2f, Ease.OutElastic));
    }
    
    public void Rotate360(float duration)
    {
        Tween.Rotation(transform, Quaternion.Euler(0, 360, 0), duration)
            .SetRelative();
    }
    
    public void ShakePosition(float intensity, float duration)
    {
        Tween.ShakeLocalPosition(transform, intensity, duration);
    }
}
```

### UI Tweens

```csharp
public class UIAnimator : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform rectTransform;
    
    public Tween FadeIn(float duration = 0.3f)
    {
        canvasGroup.alpha = 0f;
        return Tween.Alpha(canvasGroup, 1f, duration, Ease.OutQuad);
    }
    
    public Tween FadeOut(float duration = 0.2f)
    {
        return Tween.Alpha(canvasGroup, 0f, duration, Ease.InQuad);
    }
    
    public Tween SlideIn(Vector2 fromOffset, float duration = 0.3f)
    {
        Vector2 targetPos = rectTransform.anchoredPosition;
        rectTransform.anchoredPosition = targetPos + fromOffset;
        return Tween.UIAnchoredPosition(rectTransform, targetPos, duration, Ease.OutCubic);
    }
    
    public Tween PopIn(float duration = 0.4f)
    {
        rectTransform.localScale = Vector3.zero;
        return Tween.Scale(rectTransform, 1f, duration, Ease.OutBack);
    }
    
    public Tween PopOut(float duration = 0.2f)
    {
        return Tween.Scale(rectTransform, 0f, duration, Ease.InBack);
    }
}
```

### Color Tweens

```csharp
public class ColorAnimator : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Image image;
    
    public Tween FlashColor(Color flashColor, float duration = 0.2f)
    {
        Color originalColor = spriteRenderer.color;
        return Tween.Color(spriteRenderer, flashColor, duration / 2, Ease.OutQuad)
            .Chain(Tween.Color(spriteRenderer, originalColor, duration / 2, Ease.InQuad));
    }
    
    public Tween FadeSprite(float targetAlpha, float duration)
    {
        return Tween.Alpha(spriteRenderer, targetAlpha, duration);
    }
    
    public Tween FadeImage(float targetAlpha, float duration)
    {
        return Tween.Alpha(image, targetAlpha, duration);
    }
}
```

## Sequences

### Basic Sequence

```csharp
public class SequenceAnimator : MonoBehaviour
{
    public Sequence PlayIntroSequence()
    {
        return Sequence.Create()
            .Chain(Tween.Scale(transform, 0f, 1f, 0.3f, Ease.OutBack))
            .Chain(Tween.Position(transform, Vector3.up * 2f, 0.5f, Ease.OutQuad))
            .Chain(Tween.Rotation(transform, Quaternion.Euler(0, 360, 0), 0.4f))
            .ChainDelay(0.2f)
            .Chain(Tween.Scale(transform, 1.2f, 0.1f))
            .Chain(Tween.Scale(transform, 1f, 0.2f, Ease.OutElastic));
    }
}
```

### Parallel Tweens in Sequence

```csharp
public class ComplexAnimator : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform rectTransform;
    
    public Sequence SlideAndFadeIn()
    {
        Vector2 startPos = rectTransform.anchoredPosition + Vector2.down * 100f;
        rectTransform.anchoredPosition = startPos;
        canvasGroup.alpha = 0f;
        
        return Sequence.Create()
            .Group(Tween.UIAnchoredPosition(rectTransform, 
                rectTransform.anchoredPosition + Vector2.up * 100f, 0.4f, Ease.OutCubic))
            .Group(Tween.Alpha(canvasGroup, 1f, 0.3f, Ease.OutQuad));
    }
    
    public Sequence SlideAndFadeOut()
    {
        return Sequence.Create()
            .Group(Tween.UIAnchoredPosition(rectTransform, 
                rectTransform.anchoredPosition + Vector2.down * 100f, 0.3f, Ease.InCubic))
            .Group(Tween.Alpha(canvasGroup, 0f, 0.2f, Ease.InQuad));
    }
}
```

### Looping Sequences

```csharp
public class LoopingAnimator : MonoBehaviour
{
    private Sequence _idleSequence;
    
    public void StartIdleAnimation()
    {
        _idleSequence = Sequence.Create(cycles: -1) // -1 = infinite
            .Chain(Tween.Scale(transform, 1.05f, 1f, Ease.InOutSine))
            .Chain(Tween.Scale(transform, 1f, 1f, Ease.InOutSine));
    }
    
    public void StopIdleAnimation()
    {
        _idleSequence?.Stop();
        _idleSequence = null;
    }
    
    private void OnDisable()
    {
        StopIdleAnimation();
    }
}
```

## Value Tweens

### Custom Value Interpolation

```csharp
public class ValueTweener : MonoBehaviour
{
    private float _currentValue;
    
    public Tween TweenValue(float from, float to, float duration, Action<float> onUpdate)
    {
        _currentValue = from;
        return Tween.Custom(from, to, duration, value =>
        {
            _currentValue = value;
            onUpdate?.Invoke(value);
        });
    }
    
    public Tween TweenHealth(float targetHealth, float duration)
    {
        var healthBar = GetComponent<HealthBar>();
        float startHealth = healthBar.CurrentHealth;
        
        return Tween.Custom(startHealth, targetHealth, duration, value =>
        {
            healthBar.SetHealth(value);
        }, Ease.OutQuad);
    }
}
```

### Vector Interpolation

```csharp
public class VectorTweener : MonoBehaviour
{
    public Tween TweenVector3(Vector3 from, Vector3 to, float duration, Action<Vector3> onUpdate)
    {
        return Tween.Custom(from, to, duration, onUpdate);
    }
    
    public Tween TweenBetweenPoints(Transform target, Vector3 start, Vector3 end, float duration)
    {
        return Tween.Custom(start, end, duration, pos =>
        {
            target.position = pos;
        }, Ease.InOutQuad);
    }
}
```

## Callbacks

### Tween Callbacks

```csharp
public class CallbackAnimator : MonoBehaviour
{
    public void AnimateWithCallbacks()
    {
        Tween.Position(transform, Vector3.up * 5f, 1f)
            .OnStart(() => GameDebug.Log("Started"))
            .OnUpdate(progress => GameDebug.Log($"Progress: {progress:P0}"))
            .OnComplete(() => GameDebug.Log("Completed"));
    }
    
    public async UniTask AnimateAsync()
    {
        await Tween.Scale(transform, 2f, 0.5f);
        GameDebug.Log("Scale complete");
        
        await Tween.Position(transform, Vector3.forward * 10f, 1f);
        GameDebug.Log("Move complete");
    }
    
    public void AnimateWithCancellation(CancellationToken ct)
    {
        Tween.Position(transform, Vector3.up * 5f, 1f)
            .OnComplete(() =>
            {
                // The owner may have been torn down while the tween was running.
                if (ct.IsCancellationRequested)
                    return;

                // Continue with next action
            });
    }
}
```

## Common Patterns

### Damage Number Animation

```csharp
public class DamageNumberAnimator : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform rectTransform;
    
    public void PlayDamageAnimation(int damage, bool isCrit)
    {
        text.text = damage.ToString();
        canvasGroup.alpha = 1f;
        
        float scale = isCrit ? 1.5f : 1f;
        float duration = isCrit ? 1.2f : 0.8f;
        
        Sequence.Create()
            // Pop in
            .Chain(Tween.Scale(rectTransform, 0f, scale * 1.2f, 0.1f, Ease.OutQuad))
            .Chain(Tween.Scale(rectTransform, scale, 0.1f, Ease.OutElastic))
            // Float up
            .Group(Tween.UIAnchoredPositionY(rectTransform, 
                rectTransform.anchoredPosition.y + 100f, duration, Ease.OutQuad))
            // Fade out at end
            .Insert(duration * 0.6f, Tween.Alpha(canvasGroup, 0f, duration * 0.4f))
            .OnComplete(() => gameObject.SetActive(false));
    }
}
```

### Button Press Animation

```csharp
public class ButtonAnimator : MonoBehaviour
{
    [SerializeField] private RectTransform buttonRect;
    
    private Tween _currentTween;
    private Vector3 _originalScale;
    
    private void Awake()
    {
        _originalScale = buttonRect.localScale;
    }
    
    public void OnPointerDown()
    {
        _currentTween?.Stop();
        _currentTween = Tween.Scale(buttonRect, _originalScale * 0.9f, 0.1f, Ease.OutQuad);
    }
    
    public void OnPointerUp()
    {
        _currentTween?.Stop();
        _currentTween = Tween.Scale(buttonRect, _originalScale, 0.15f, Ease.OutBack);
    }
    
    public void OnClick()
    {
        _currentTween?.Stop();
        _currentTween = Sequence.Create()
            .Chain(Tween.Scale(buttonRect, _originalScale * 1.1f, 0.1f, Ease.OutQuad))
            .Chain(Tween.Scale(buttonRect, _originalScale, 0.2f, Ease.OutElastic));
    }
}
```

### Screen Shake

```csharp
public class ScreenShaker : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    
    private Tween _shakeTween;
    private Vector3 _originalPosition;
    
    private void Awake()
    {
        _originalPosition = cameraTransform.localPosition;
    }
    
    public void Shake(float intensity = 0.5f, float duration = 0.3f)
    {
        _shakeTween?.Stop();
        cameraTransform.localPosition = _originalPosition;
        
        _shakeTween = Tween.ShakeLocalPosition(cameraTransform, intensity, duration)
            .OnComplete(() => cameraTransform.localPosition = _originalPosition);
    }
    
    public void ImpactShake()
    {
        Shake(0.8f, 0.2f);
    }
    
    public void ContinuousShake(float intensity = 0.3f)
    {
        _shakeTween?.Stop();
        _shakeTween = Tween.ShakeLocalPosition(cameraTransform, intensity, 99f, cycles: -1);
    }
    
    public void StopShake()
    {
        _shakeTween?.Stop();
        cameraTransform.localPosition = _originalPosition;
    }
}
```

### Pickup Collection Animation

```csharp
public class PickupAnimator : MonoBehaviour
{
    public void AnimateCollection(Transform pickup, Transform target, Action onComplete)
    {
        Sequence.Create()
            // Initial bounce
            .Chain(Tween.Scale(pickup, 1.3f, 0.1f, Ease.OutQuad))
            .Chain(Tween.Scale(pickup, 1f, 0.1f))
            // Fly to target
            .Chain(Tween.Position(pickup, target.position, 0.4f, Ease.InQuad))
            .Group(Tween.Scale(pickup, 0f, 0.4f, Ease.InQuad))
            .OnComplete(() =>
            {
                onComplete?.Invoke();
                Destroy(pickup.gameObject);
            });
    }
}
```

## Performance Tips

### Pooling Tweens

```csharp
public class TweenPooling : MonoBehaviour
{
    // PrimeTween pools internally, but avoid creating many simultaneous tweens
    
    private Tween _reusableTween;
    private float _currentValue;
    
    public void UpdateValue(float target)
    {
        // Two live tweens on the same value would fight each other every frame.
        _reusableTween?.Stop();
        _reusableTween = Tween.Custom(
            getter: () => _currentValue,
            setter: v => _currentValue = v,
            endValue: target,
            duration: 0.3f
        );
    }
}
```

### Batch Operations

```csharp
public class BatchAnimator : MonoBehaviour
{
    [SerializeField] private Transform[] items;
    
    public void AnimateAllItems()
    {
        var sequence = Sequence.Create();
        
        for (int i = 0; i < items.Length; i++)
        {
            float delay = i * 0.05f;
            
            sequence.Insert(delay, 
                Tween.Scale(items[i], 0f, 1f, 0.3f, Ease.OutBack));
        }
    }
}
```

## Best Practices

1. **Stop tweens before creating new** - Prevent overlap
2. **Cache references** to tweens for stopping
3. **Use sequences** for complex animations
4. **Choose appropriate easing** for motion type
5. **Clean up in OnDisable** - Stop ongoing tweens
6. **Use async/await** for sequential logic
7. **Batch related animations** in sequences
8. **Profile tween count** - Keep reasonable limits
9. **Use Group for parallel** animations
10. **Test with time scale** changes

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Tween not playing | Check if object is active |
| Overlapping animations | Stop previous tween first |
| Memory issues | Reduce simultaneous tweens |
| Unexpected position | Check relative vs absolute |
| Sequence not completing | Verify all tweens are valid |
| Callbacks not firing | Check if tween was stopped |
