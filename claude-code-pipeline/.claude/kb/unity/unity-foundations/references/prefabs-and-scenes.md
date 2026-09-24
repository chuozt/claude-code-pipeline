# Prefabs, Scenes, and ScriptableObjects

> Based on Unity 6.3 documentation

## Prefabs

### What They Are

From the docs: "The prefab asset acts as a template from which you can create new prefab instances in the Scene." Prefabs store a complete GameObject configuration -- all components, property values, and child GameObjects -- enabling efficient reuse and consistency across a project.

### Creating Prefabs

In the Editor: drag a GameObject from the Hierarchy window into the Project window. This creates a `.prefab` asset file and converts the original GameObject into a prefab instance.

### Instantiating Prefabs at Runtime

From the Unity docs:

```csharp
using UnityEngine;

public class InstantiationExample : MonoBehaviour
{
    // Reference to the prefab. Drag a prefab into this field in the Inspector.
    [SerializeField] private GameObject myPrefab;

    // This script will simply instantiate the prefab when the game starts.
    private void Start()
    {
        // Instantiate at position (0, 0, 0) and zero rotation.
        Instantiate(myPrefab, new Vector3(0, 0, 0), Quaternion.identity);
    }
}
```

### Instantiate Method Signatures

```csharp
// Basic clone
Object Instantiate(Object original);

// Clone at position and rotation
Object Instantiate(Object original, Vector3 position, Quaternion rotation);

// Clone as child of parent
Object Instantiate(Object original, Transform parent);

// Clone at position/rotation under parent
Object Instantiate(Object original, Vector3 position, Quaternion rotation, Transform parent);
```

### Typed Instantiation

```csharp
using UnityEngine;

public class TypedInstantiation : MonoBehaviour
{
    [SerializeField] private Rigidbody projectilePrefab;

    private void Fire()
    {
        // Generic Instantiate returns the same type as the input
        Rigidbody clone = Instantiate(projectilePrefab,
            transform.position + transform.forward,
            transform.rotation);

        clone.velocity = transform.forward * 20f;

        // Destroy after 5 seconds
        Destroy(clone.gameObject, 5f);
    }
}
```

### Prefab Features

| Feature | Description |
|---------|-------------|
| **Nested Prefabs** | Include prefab instances within other prefabs for hierarchical composition. Changes to the nested prefab propagate to all parents that use it. |
| **Prefab Variants** | Create predefined variations maintaining a base prefab relationship. Override specific properties while inheriting the rest. |
| **Overrides** | Modify components, data, and child objects on specific instances without affecting the template. Overridden properties appear bold in the Inspector. |
| **Unpacking** | Convert prefab instances back to standalone GameObjects, severing the template link. Right-click > Prefab > Unpack. |

### Prefab Best Practices

```csharp
using UnityEngine;

public class PrefabPool : MonoBehaviour
{
    [SerializeField] private GameObject prefab;

    private Queue<GameObject> _pool = new Queue<GameObject>();

    // Object pooling avoids Instantiate/Destroy overhead
    public GameObject Get()
    {
        if (_pool.Count > 0)
        {
            GameObject obj = _pool.Dequeue();
            obj.SetActive(true);
            return obj;
        }

        return Instantiate(prefab);
    }

    public void Return(GameObject obj)
    {
        obj.SetActive(false);
        _pool.Enqueue(obj);
    }
}
```

### PrefabUtility (Editor Only)

For Editor scripts that manipulate prefabs programmatically, use `PrefabUtility`:

```csharp
#if UNITY_EDITOR
using UnityEditor;

// Instantiate a prefab and maintain the prefab link
GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);

// Apply overrides back to prefab asset
PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.UserAction);

// Check if a GameObject is part of a prefab
bool isPrefab = PrefabUtility.IsPartOfPrefabInstance(gameObject);
#endif
```

---

## Scenes

### What They Are

From the docs: "Scenes are where you work with content in Unity. They are assets that contain all or part of a game or application." A default new scene contains a Camera and a directional Light.

### Creating Scenes

- **Menu**: File > New Scene (Ctrl/Cmd + N)
- **Scene Templates**: Blueprints for creating new scenes with predefined content
- Templates can be pinned in the New Scene dialog for quick access

### SceneManager API

From the [SceneManager docs](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager.html):

#### Static Methods

| Method | Description |
|--------|-------------|
| `LoadScene(string/int)` | Loads scene by name or build index |
| `LoadSceneAsync(string/int, LoadSceneMode)` | Loads scene asynchronously (Single or Additive) |
| `UnloadSceneAsync(string/Scene)` | Destroys all GameObjects and removes the scene |
| `GetActiveScene()` | Returns the currently active Scene struct |
| `SetActiveScene(Scene)` | Sets which loaded scene is active |
| `GetSceneByName(string)` | Searches loaded scenes by name |
| `GetSceneByBuildIndex(int)` | Gets scene by Build Settings index |
| `GetSceneAt(int)` | Gets loaded scene at runtime index |

#### Events

| Event | Description |
|-------|-------------|
| `sceneLoaded` | Fires when a scene finishes loading |
| `sceneUnloaded` | Fires when a scene is unloaded |
| `activeSceneChanged` | Fires when the active scene changes |

#### Properties

| Property | Description |
|----------|-------------|
| `sceneCount` | Total number of currently loaded scenes |
| `loadedSceneCount` | Number of fully loaded scenes |
| `sceneCountInBuildSettings` | Total scenes in Build Settings |

### Scene Management Code Examples

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    private void Start()
    {
        // Subscribe to scene events
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent leaks
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    // Load scene replacing all current scenes
    public void LoadLevel(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // Load scene by build index
    public void LoadLevelByIndex(int index)
    {
        SceneManager.LoadScene(index);
    }

    // Async load with progress tracking. Uses the project's async standard
    // (Awaitable or UniTask, project_setup.md §1) rather than a coroutine.
    public async Awaitable LoadLevelAsync(string sceneName)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        while (!op.isDone)
        {
            // Progress goes from 0 to 0.9, then waits for activation
            float progress = Mathf.Clamp01(op.progress / 0.9f);
            GameDebug.Log("Loading: " + (progress * 100f) + "%");

            if (op.progress >= 0.9f)
                op.allowSceneActivation = true;

            await Awaitable.NextFrameAsync();
        }
    }

    // Additive scene loading (keeps current scenes)
    public void LoadAdditive(string sceneName)
    {
        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
    }

    // Unload an additively-loaded scene
    public void UnloadScene(string sceneName)
    {
        SceneManager.UnloadSceneAsync(sceneName);
    }

    // Event callbacks
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        GameDebug.Log("Loaded: " + scene.name + " (mode: " + mode + ")");
    }

    private void OnSceneUnloaded(Scene scene)
    {
        GameDebug.Log("Unloaded: " + scene.name);
    }

    private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        GameDebug.Log("Active scene changed from " + oldScene.name + " to " + newScene.name);
    }
}
```

### Multi-Scene Setup

From the docs, multi-scene editing is for developers who need to "create large streaming worlds or want to effectively manage multiple scenes at runtime."

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

public class WorldStreaming : MonoBehaviour
{
    public void StreamIn(string sceneName)
    {
        // Check if scene is already loaded
        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.isLoaded)
            SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
    }

    public void StreamOut(string sceneName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (scene.isLoaded)
            SceneManager.UnloadSceneAsync(scene);
    }

    public void SetAsActive(string sceneName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (scene.isLoaded)
            SceneManager.SetActiveScene(scene);
    }

    public void ListLoadedScenes()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            GameDebug.Log("Scene " + i + ": " + scene.name
                + " (loaded: " + scene.isLoaded + ")");
        }
    }
}
```

**Important**: Scenes must be added to Build Settings (File > Build Settings) to be loaded by name or index at runtime.

---

## ScriptableObjects

### What They Are

From the docs: ScriptableObject is "a serializable Unity type derived from `UnityEngine.Object`" that serves as a data container independent of GameObjects. They persist as `.asset` files in the project.

### Creating a ScriptableObject Class

From the Unity docs:

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/SpawnData", order = 1)]
public class SpawnDataScriptableObject : ScriptableObject
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private Vector3[] positions;
    [SerializeField] private int count;

    public GameObject Prefab => prefab;
    public Vector3[] Positions => positions;
    public int Count => count;
}
```

The `CreateAssetMenu` attribute adds an entry to **Assets > Create** so you can create instances in the Editor.

### Using ScriptableObjects at Runtime

From the docs:

```csharp
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [SerializeField] private SpawnDataScriptableObject spawnData;

    private void Start()
    {
        for (int i = 0; i < spawnData.Count; i++)
            Instantiate(spawnData.Prefab, spawnData.Positions[i], Quaternion.identity);
    }
}
```

### ScriptableObject as Game Configuration

```csharp
using UnityEngine;

[CreateAssetMenu(menuName = "Config/GameSettings")]
public class GameSettings : ScriptableObject
{
    [Header("Player")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 8f;

    [Header("Difficulty")]
    [SerializeField] private int enemiesPerWave = 5;
    [SerializeField] private float enemySpeedMultiplier = 1f;

    public int MaxHealth => maxHealth;
    public float MoveSpeed => moveSpeed;
}

public class PlayerController : MonoBehaviour
{
    [SerializeField] private GameSettings settings;

    private int _currentHealth;

    private void Start()
    {
        _currentHealth = settings.MaxHealth;
    }

    private void Update()
    {
        float h = Input.GetAxis("Horizontal");
        transform.Translate(Vector3.right * h * settings.MoveSpeed * Time.deltaTime);
    }
}
```

### ScriptableObject as Event Channel

```csharp
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Events/GameEvent")]
public class GameEvent : ScriptableObject
{
    private readonly System.Collections.Generic.List<GameEventListener> _listeners =
        new System.Collections.Generic.List<GameEventListener>();

    public void Raise()
    {
        // Iterate backwards so a listener may unregister itself while the event is raised
        for (int i = _listeners.Count - 1; i >= 0; i--)
            _listeners[i].OnEventRaised();
    }

    public void RegisterListener(GameEventListener listener)
    {
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

    private void OnEnable() => gameEvent.RegisterListener(this);
    private void OnDisable() => gameEvent.UnregisterListener(this);
    public void OnEventRaised() => response.Invoke();
}
```

### Saving ScriptableObject Changes (Editor Only)

From the docs: "Unity doesn't automatically save changes to a `ScriptableObject` made via script in Edit mode."

```csharp
#if UNITY_EDITOR
using UnityEditor;

// After modifying a ScriptableObject via code in the Editor:
settings.value += 10;
EditorUtility.SetDirty(settings); // Required for persistence!

// Inspector modifications auto-save; script-based edits require explicit dirting
#endif
```

### Key Characteristics

| Aspect | ScriptableObject | MonoBehaviour |
|--------|-----------------|---------------|
| Attached to GameObjects | No | Yes |
| Lives as project asset | Yes (`.asset` file) | No (lives in scenes) |
| Survives scene changes | Yes | Only with DontDestroyOnLoad |
| Shared across instances | Yes (single reference) | No (data duplicated per instance) |
| Runtime modifications | In-memory only (not persisted in builds) | In-memory only |
| Editor modifications | Persisted if marked dirty | Persisted with scene save |
| Lifecycle callbacks | OnEnable, OnDisable, OnDestroy | Full MonoBehaviour lifecycle |

---

## Source Documentation

- [Prefabs](https://docs.unity3d.com/6000.3/Documentation/Manual/Prefabs.html)
- [Instantiating Prefabs](https://docs.unity3d.com/6000.3/Documentation/Manual/instantiating-prefabs-intro.html)
- [Scenes](https://docs.unity3d.com/6000.3/Documentation/Manual/CreatingScenes.html)
- [Multi-Scene Editing](https://docs.unity3d.com/6000.3/Documentation/Manual/MultiSceneEditing.html)
- [SceneManager API](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager.html)
- [ScriptableObject](https://docs.unity3d.com/6000.3/Documentation/Manual/class-ScriptableObject.html)
