---
name: tools-unity-scriptable-objects
description: Unity ScriptableObject patterns for data management, configuration, and runtime systems.
---

# Unity ScriptableObjects

## Overview

ScriptableObjects are data containers that exist outside the scene hierarchy, ideal for configuration, shared data, and decoupling systems.

## When to Use

- Game configuration and settings
- Item/ability/character definitions
- Shared runtime data
- Event systems
- Dependency injection alternatives

## Basic Patterns

### Data Definition

```csharp
[CreateAssetMenu(fileName = "NewItem", menuName = "Game/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    [Header("Basic Info")]
    [SerializeField] private string id;
    [SerializeField] private string displayName;
    [TextArea] [SerializeField] private string description;
    [SerializeField] private Sprite icon;

    [Header("Stats")]
    [SerializeField] private ItemRarity rarity;
    [SerializeField] private int maxStackSize = 99;
    [SerializeField] private int baseValue;

    [Header("Gameplay")]
    [SerializeField] private EquipmentSlot equipSlot;
    [SerializeField] private bool isConsumable;
    [SerializeField] private bool isEquippable;

    // Built lazily so the first lookup pays the Resources scan, not asset load
    private static Dictionary<string, ItemDefinition> s_lookup;

    public string Id => id;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public ItemRarity Rarity => rarity;
    public int MaxStackSize => maxStackSize;
    public int BaseValue => baseValue;
    public EquipmentSlot EquipSlot => equipSlot;
    public bool IsConsumable => isConsumable;
    public bool IsEquippable => isEquippable;

    public static ItemDefinition GetById(string itemId)
    {
        if (s_lookup == null)
            BuildLookup();

        return s_lookup.TryGetValue(itemId, out var item) ? item : null;
    }

    private static void BuildLookup()
    {
        s_lookup = new Dictionary<string, ItemDefinition>();
        var items = Resources.LoadAll<ItemDefinition>("Items");

        foreach (var item in items)
            s_lookup[item.Id] = item;
    }
}
```

### Database Pattern

```csharp
[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Game/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [SerializeField] private List<ItemDefinition> items = new();

    private Dictionary<string, ItemDefinition> _lookup;

    public IReadOnlyList<ItemDefinition> AllItems => items;

    public void Initialize()
    {
        _lookup = new Dictionary<string, ItemDefinition>();

        foreach (var item in items)
        {
            if (item != null && !string.IsNullOrEmpty(item.Id))
                _lookup[item.Id] = item;
        }
    }

    public ItemDefinition GetItem(string id)
    {
        if (_lookup == null)
            Initialize();

        return _lookup.TryGetValue(id, out var item) ? item : null;
    }

    public IEnumerable<ItemDefinition> GetItemsByRarity(ItemRarity rarity)
    {
        return items.Where(i => i.Rarity == rarity);
    }

#if UNITY_EDITOR
    [ContextMenu("Collect All Items")]
    private void CollectItems()
    {
        items.Clear();
        var guids = UnityEditor.AssetDatabase.FindAssets("t:ItemDefinition");

        foreach (var guid in guids)
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var item = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);

            if (item != null && item != this)
                items.Add(item);
        }

        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
```

## Runtime Data

### Shared Runtime Value

```csharp
[CreateAssetMenu(fileName = "RuntimeInt", menuName = "Runtime/Int Value")]
public class RuntimeInt : ScriptableObject
{
    [SerializeField] private int initialValue;

    [NonSerialized] private int _runtimeValue;
    [NonSerialized] private bool _isInitialized;

    public int Value
    {
        get
        {
            EnsureInitialized();
            return _runtimeValue;
        }
        set
        {
            EnsureInitialized();

            if (_runtimeValue != value)
            {
                _runtimeValue = value;
                OnValueChanged?.Invoke(value);
            }
        }
    }

    public event Action<int> OnValueChanged;

    private void EnsureInitialized()
    {
        if (_isInitialized)
            return;

        _runtimeValue = initialValue;
        _isInitialized = true;
    }

    private void OnEnable()
    {
        _isInitialized = false;
    }

    public void Reset()
    {
        _runtimeValue = initialValue;
        _isInitialized = true;
        OnValueChanged?.Invoke(_runtimeValue);
    }
}
```

### Observable Collection

```csharp
[CreateAssetMenu(fileName = "RuntimeList", menuName = "Runtime/List")]
public class RuntimeList<T> : ScriptableObject
{
    [NonSerialized] private List<T> _items = new();

    public IReadOnlyList<T> Items => _items;
    public int Count => _items.Count;

    public event Action<T> OnItemAdded;
    public event Action<T> OnItemRemoved;
    public event Action OnCleared;

    public void Add(T item)
    {
        _items.Add(item);
        OnItemAdded?.Invoke(item);
    }

    public bool Remove(T item)
    {
        if (!_items.Remove(item))
            return false;

        OnItemRemoved?.Invoke(item);
        return true;
    }

    public void Clear()
    {
        _items.Clear();
        OnCleared?.Invoke();
    }

    private void OnEnable()
    {
        _items = new List<T>();
    }
}
```

## Event System

### Game Event

```csharp
[CreateAssetMenu(fileName = "GameEvent", menuName = "Events/Game Event")]
public class GameEvent : ScriptableObject
{
    private readonly List<GameEventListener> _listeners = new();

    public void Raise()
    {
        // Backwards so a listener may unregister itself while being raised
        for (int i = _listeners.Count - 1; i >= 0; i--)
            _listeners[i].OnEventRaised();
    }

    public void RegisterListener(GameEventListener listener)
    {
        if (!_listeners.Contains(listener))
            _listeners.Add(listener);
    }

    public void UnregisterListener(GameEventListener listener)
    {
        _listeners.Remove(listener);
    }
}

public class GameEventListener : MonoBehaviour
{
    [SerializeField] private GameEvent gameEvent;
    [SerializeField] private UnityEvent response;

    private void OnEnable()
    {
        gameEvent?.RegisterListener(this);
    }

    private void OnDisable()
    {
        gameEvent?.UnregisterListener(this);
    }

    public void OnEventRaised()
    {
        response?.Invoke();
    }
}
```

### Typed Event

```csharp
public abstract class GameEvent<T> : ScriptableObject
{
    private readonly List<IGameEventListener<T>> _listeners = new();

    public void Raise(T value)
    {
        for (int i = _listeners.Count - 1; i >= 0; i--)
            _listeners[i].OnEventRaised(value);
    }

    public void RegisterListener(IGameEventListener<T> listener)
    {
        if (!_listeners.Contains(listener))
            _listeners.Add(listener);
    }

    public void UnregisterListener(IGameEventListener<T> listener)
    {
        _listeners.Remove(listener);
    }
}

public interface IGameEventListener<T>
{
    void OnEventRaised(T value);
}

[CreateAssetMenu(fileName = "IntEvent", menuName = "Events/Int Event")]
public class IntEvent : GameEvent<int> { }

[CreateAssetMenu(fileName = "StringEvent", menuName = "Events/String Event")]
public class StringEvent : GameEvent<string> { }
```

## Configuration

### Game Settings

```csharp
[CreateAssetMenu(fileName = "GameSettings", menuName = "Config/Game Settings")]
public class GameSettings : ScriptableObject
{
    [Header("Gameplay")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float playerMoveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;

    [Header("Audio")]
    [Range(0, 1)] [SerializeField] private float masterVolume = 1f;
    [Range(0, 1)] [SerializeField] private float musicVolume = 0.8f;
    [Range(0, 1)] [SerializeField] private float sfxVolume = 1f;

    [Header("Graphics")]
    [SerializeField] private QualityPreset defaultQuality = QualityPreset.Medium;
    [SerializeField] private int targetFrameRate = 60;
    [SerializeField] private bool isVSyncEnabled = true;

    private static GameSettings s_instance;

    public int MaxHealth => maxHealth;
    public float PlayerMoveSpeed => playerMoveSpeed;
    public float JumpForce => jumpForce;
    public float MasterVolume => masterVolume;
    public float MusicVolume => musicVolume;
    public float SfxVolume => sfxVolume;
    public QualityPreset DefaultQuality => defaultQuality;
    public int TargetFrameRate => targetFrameRate;
    public bool IsVSyncEnabled => isVSyncEnabled;

    public static GameSettings Instance
    {
        get
        {
            if (s_instance == null)
                s_instance = Resources.Load<GameSettings>("GameSettings");

            return s_instance;
        }
    }
}
```

### Platform-Specific Config

```csharp
[CreateAssetMenu(fileName = "PlatformConfig", menuName = "Config/Platform Config")]
public class PlatformConfig : ScriptableObject
{
    [SerializeField] private PlatformSettings ios;
    [SerializeField] private PlatformSettings android;
    [SerializeField] private PlatformSettings desktop;

    public PlatformSettings Current
    {
        get
        {
#if UNITY_IOS
            return ios;
#elif UNITY_ANDROID
            return android;
#else
            return desktop;
#endif
        }
    }
}

[Serializable]
public class PlatformSettings
{
    public int MaxEnemies = 20;
    public int MaxParticles = 100;
    public int TextureQuality = 2;
    public float LodBias = 1f;
    public bool EnableShadows = true;
}
```

## Validation

### Editor Validation

```csharp
public abstract class ValidatedScriptableObject : ScriptableObject
{
#if UNITY_EDITOR
    private void OnValidate()
    {
        Validate();
    }

    protected virtual void Validate()
    {
        var errors = GetValidationErrors();

        foreach (var error in errors)
            GameDebug.LogError($"[{name}] {error}", this);
    }

    protected virtual IEnumerable<string> GetValidationErrors()
    {
        yield break;
    }
#endif
}

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Game/Weapon")]
public class WeaponDefinition : ValidatedScriptableObject
{
    [SerializeField] private string id;
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;
    [SerializeField] private int damage;
    [SerializeField] private float attackSpeed;

    public string Id => id;
    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public int Damage => damage;
    public float AttackSpeed => attackSpeed;

#if UNITY_EDITOR
    protected override IEnumerable<string> GetValidationErrors()
    {
        if (string.IsNullOrEmpty(id))
            yield return "Id is required";

        if (string.IsNullOrEmpty(displayName))
            yield return "DisplayName is required";

        if (damage <= 0)
            yield return "Damage must be positive";

        if (attackSpeed <= 0)
            yield return "AttackSpeed must be positive";

        if (icon == null)
            yield return "Icon is required";
    }
#endif
}
```

### ID Uniqueness Check

```csharp
#if UNITY_EDITOR
public static class ScriptableObjectValidator
{
    [UnityEditor.Callbacks.DidReloadScripts]
    private static void ValidateAllItems()
    {
        ValidateUniqueIds<ItemDefinition>("Items");
        ValidateUniqueIds<WeaponDefinition>("Weapons");
    }

    private static void ValidateUniqueIds<T>(string folder) where T : ScriptableObject
    {
        var guids = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T).Name}");
        var idToAsset = new Dictionary<string, string>();

        foreach (var guid in guids)
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);

            // The id is a serialized private field, so reflection must ask for non-public
            var idField = typeof(T).GetField("id", BindingFlags.NonPublic | BindingFlags.Instance);
            if (idField == null)
                continue;

            var id = (string)idField.GetValue(asset);
            if (string.IsNullOrEmpty(id))
                continue;

            if (idToAsset.TryGetValue(id, out var existingPath))
                GameDebug.LogError($"Duplicate ID '{id}' found in:\n{existingPath}\n{path}");
            else
                idToAsset[id] = path;
        }
    }
}
#endif
```

## Best Practices

1. **Use CreateAssetMenu** for easy creation
2. **Initialize runtime state in OnEnable**
3. **Clear runtime state** - SO persists in editor
4. **Use databases** for collections
5. **Validate in editor** with OnValidate
6. **Keep IDs unique** - Validate automatically
7. **Use SerializeField** for private fields
8. **Avoid scene references** in SO
9. **Reset state between plays** in editor
10. **Use Resources.Load sparingly** - Prefer Addressables

## Troubleshooting

| Issue | Solution |
|-------|----------|
| State persists between plays | Reset in OnEnable |
| Null reference to SO | Check asset assignment |
| Duplicate IDs | Add validation check |
| SO not saving changes | Call EditorUtility.SetDirty |
| Large SO slows editor | Split into smaller assets |
| Events not firing | Check listener registration |
