# ScriptableObject Reference

> Source: [Unity 6.3 ScriptableObject Manual](https://docs.unity3d.com/6000.3/Documentation/Manual/class-ScriptableObject.html)

## What Are ScriptableObjects?

ScriptableObjects are serializable Unity types derived from `UnityEngine.Object`. Unlike MonoBehaviours, they are NOT attached to GameObjects. They exist as independent project assets and are referenced via Inspector fields.

## Key Characteristics

- Stored as `.asset` files in the project
- Shared across multiple objects (single instance in memory)
- Survive scene transitions (they are project assets, not scene objects)
- In Edit mode: Inspector changes save automatically; script changes require `EditorUtility.SetDirty()`
- At runtime: read-only in builds (runtime modifications exist only in memory, not persisted to disk)

## Creating ScriptableObjects

### Define the Class

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/SpawnManagerScriptableObject", order = 1)]
public class SpawnManagerScriptableObject : ScriptableObject
{
    [SerializeField] private string prefabName;
    [SerializeField] private Vector3[] spawnPoints;
    [SerializeField] private int numberOfPrefabsToCreate;

    public string PrefabName => prefabName;
    public Vector3[] SpawnPoints => spawnPoints;
    public int NumberOfPrefabsToCreate => numberOfPrefabsToCreate;
}
```

The `[CreateAssetMenu]` attribute adds an entry to the **Assets > Create** menu in the Unity Editor.

### Create an Instance in Editor

1. Right-click in Project window
2. **Create > ScriptableObjects > SpawnManagerScriptableObject**
3. Configure values in the Inspector

### Create an Instance via Script

```csharp
// Runtime creation (not saved as asset). Fields are [SerializeField] private,
// so values come from the Inspector or an explicit setup method, never public fields.
var config = ScriptableObject.CreateInstance<SpawnManagerScriptableObject>();

// Editor-only: save as asset
#if UNITY_EDITOR
UnityEditor.AssetDatabase.CreateAsset(config, "Assets/Data/NewConfig.asset");
UnityEditor.AssetDatabase.SaveAssets();
#endif
```

### Reference from MonoBehaviour

```csharp
public class SpawnManager : MonoBehaviour
{
    [SerializeField] private SpawnManagerScriptableObject spawnData;

    private void Start()
    {
        for (int i = 0; i < spawnData.NumberOfPrefabsToCreate; i++)
        {
            Vector3 pos = spawnData.SpawnPoints[i % spawnData.SpawnPoints.Length];
            Instantiate(Resources.Load(spawnData.PrefabName), pos, Quaternion.identity);
        }
    }
}
```

## Use Case Patterns

### 1. Shared Configuration Data

Store game balance values, enemy stats, weapon data -- referenced by many objects without duplication.

```csharp
[CreateAssetMenu(menuName = "Config/Weapon Stats")]
public class WeaponStats : ScriptableObject
{
    [SerializeField] private string weaponName;
    [SerializeField] private AudioClip fireSound;
    [SerializeField] private GameObject muzzleFlashPrefab;
    [SerializeField] private int magazineSize;
    [SerializeField] private float damage;
    [SerializeField] private float fireRate;
    [SerializeField] private float range;

    public string WeaponName => weaponName;
    public AudioClip FireSound => fireSound;
    public GameObject MuzzleFlashPrefab => muzzleFlashPrefab;
    public int MagazineSize => magazineSize;
    public float Damage => damage;
    public float FireRate => fireRate;
    public float Range => range;
}

public class Weapon : MonoBehaviour
{
    [SerializeField] private WeaponStats stats;

    private int _currentAmmo;
    private float _nextFireTime;

    private void Start()
    {
        _currentAmmo = stats.MagazineSize;
    }

    private void Fire()
    {
        if (Time.time < _nextFireTime || _currentAmmo <= 0)
            return;

        _nextFireTime = Time.time + (1f / stats.FireRate);
        _currentAmmo--;
        // Use stats.Damage, stats.Range, etc.
    }
}
```

**Memory advantage:** 100 enemies referencing the same `WeaponStats` asset share one copy of the data instead of each holding their own duplicate.

### 2. Enum-Like Collections

Replace enums with ScriptableObject instances for extensibility.

```csharp
[CreateAssetMenu(menuName = "Config/Item Type")]
public class ItemType : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;
    [SerializeField] private Color rarityColor;
    [SerializeField] private int maxStackSize;

    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public Color RarityColor => rarityColor;
    public int MaxStackSize => maxStackSize;
}
```

Designers can create new item types without modifying code.

### 3. Event Channels (Decoupled Communication)

ScriptableObject-based events allow systems to communicate without direct references.

```csharp
[CreateAssetMenu(menuName = "Events/Void Event Channel")]
public class VoidEventChannel : ScriptableObject
{
    private System.Action _onEventRaised;

    public void RaiseEvent()
    {
        _onEventRaised?.Invoke();
    }

    public void Subscribe(System.Action listener)
    {
        _onEventRaised += listener;
    }

    public void Unsubscribe(System.Action listener)
    {
        _onEventRaised -= listener;
    }
}
```

```csharp
[CreateAssetMenu(menuName = "Events/Int Event Channel")]
public class IntEventChannel : ScriptableObject
{
    private System.Action<int> _onEventRaised;

    public void RaiseEvent(int value)
    {
        _onEventRaised?.Invoke(value);
    }

    public void Subscribe(System.Action<int> listener)
    {
        _onEventRaised += listener;
    }

    public void Unsubscribe(System.Action<int> listener)
    {
        _onEventRaised -= listener;
    }
}
```

**Publisher (knows nothing about subscribers):**

```csharp
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private IntEventChannel onHealthChanged;
    [SerializeField] private VoidEventChannel onPlayerDeath;

    private int _currentHealth = 100;

    public void TakeDamage(int amount)
    {
        _currentHealth -= amount;
        onHealthChanged.RaiseEvent(_currentHealth);

        if (_currentHealth <= 0)
            onPlayerDeath.RaiseEvent();
    }
}
```

**Subscriber (knows nothing about publishers):**

```csharp
public class HealthUI : MonoBehaviour
{
    [SerializeField] private IntEventChannel onHealthChanged;

    private void OnEnable() => onHealthChanged.Subscribe(UpdateHealthBar);
    private void OnDisable() => onHealthChanged.Unsubscribe(UpdateHealthBar);

    private void UpdateHealthBar(int health)
    {
        // Update UI elements
    }
}
```

### 4. Runtime Data Sets

Track runtime collections without singletons.

```csharp
[CreateAssetMenu(menuName = "Data/Runtime Set")]
public class RuntimeSet<T> : ScriptableObject
{
    [System.NonSerialized] private List<T> _items = new List<T>();

    public IReadOnlyList<T> Items => _items;

    public void Add(T item)
    {
        if (!_items.Contains(item))
            _items.Add(item);
    }

    public void Remove(T item) => _items.Remove(item);
}
```

### 5. Variable References (Shared State)

```csharp
[CreateAssetMenu(menuName = "Variables/Float Variable")]
public class FloatVariable : ScriptableObject
{
    [SerializeField] private float value;

    public float Value => value;

    public void SetValue(float newValue) => value = newValue;
    public void ApplyChange(float delta) => value += delta;
}
```

```csharp
public class PlayerScore : MonoBehaviour
{
    [SerializeField] private FloatVariable score;

    public void AddPoints(float points)
    {
        score.ApplyChange(points);
    }
}

public class ScoreDisplay : MonoBehaviour
{
    [SerializeField] private FloatVariable score;
    [SerializeField] private TMPro.TextMeshProUGUI label;

    private void Update()
    {
        label.text = $"Score: {score.Value:F0}";
    }
}
```

Both components reference the same `FloatVariable` asset -- no direct coupling.

## ScriptableObject Lifecycle

ScriptableObjects have a limited set of lifecycle callbacks compared to MonoBehaviour:

| Callback | When |
|----------|------|
| `Awake()` | When the instance is created (CreateInstance or loaded from asset) |
| `OnEnable()` | When the object is loaded/enabled |
| `OnDisable()` | When the object is about to be unloaded |
| `OnDestroy()` | When the object is being destroyed |
| `OnValidate()` | Editor-only: when loaded or Inspector values change |

```csharp
public class GameConfig : ScriptableObject
{
    [SerializeField] private float maxSpeed = 10f;

    private void OnEnable()
    {
        // Called when asset is loaded
        // Good place for runtime initialization
    }

    private void OnValidate()
    {
        // Editor-only: enforce constraints
        maxSpeed = Mathf.Max(0f, maxSpeed);
    }
}
```

## Data Persistence

| Context | Behavior |
|---------|----------|
| **Editor, Inspector changes** | Saved automatically on project save |
| **Editor, script changes** | Must call `EditorUtility.SetDirty(obj)` then save |
| **Runtime (Play mode)** | Changes persist until Play mode ends, then revert |
| **Runtime (Built player)** | Read-only; modifications exist only in memory |

For persistent runtime data, combine ScriptableObjects with a save system (JSON, binary, PlayerPrefs).

## Architecture Recommendations

### Do

- Use ScriptableObjects for shared read-only configuration
- Use event channels for decoupled communication across scenes
- Use `[CreateAssetMenu]` so designers can create assets without code
- Reset runtime state in `OnEnable()` if the ScriptableObject carries mutable runtime data

### Do Not

- Store large amounts of transient runtime state (use MonoBehaviours or plain C# classes)
- Expect runtime modifications to persist in builds
- Forget that all references point to the same instance (mutations are shared)
- Create ScriptableObject instances with `new` -- always use `ScriptableObject.CreateInstance<T>()`

## Anti-Patterns

| Anti-Pattern | Problem | Fix |
|-------------|---------|-----|
| `new MyScriptableObject()` | Bypasses Unity lifecycle | Use `ScriptableObject.CreateInstance<T>()` |
| Mutable runtime data without reset | Data persists between Play sessions in editor | Reset in `OnEnable()` or use `[NonSerialized]` |
| Massive ScriptableObjects | Entire asset loads into memory | Split into smaller focused assets |
| Direct field access without encapsulation | Hard to add validation or events later | Use properties or methods |
| Circular references between ScriptableObjects | Serialization issues, hard to reason about | Use event channels for indirect communication |
