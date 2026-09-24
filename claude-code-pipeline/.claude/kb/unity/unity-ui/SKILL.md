---
name: unity-ui
description: >
  Unity 6 UI development guide. Use when building user interfaces, menus, HUD, buttons, or any UI elements. Covers UI Toolkit (USS, UXML, UI Builder, data binding), uGUI/Canvas, and IMGUI. The project uses only the UI system named in project_setup.md §1; mobile projects here usually run UGUI. Based on Unity 6.3 LTS documentation.
---

# Unity UI Systems

Unity provides three UI frameworks. **Use the UI system the project names in `project_setup.md` §1.** UI Toolkit and uGUI both serve runtime game UI; this guide covers both. IMGUI is strictly for Editor tooling and debugging.

## UI System Comparison

| Feature | UI Toolkit | uGUI (Canvas) | IMGUI |
|---|---|---|---|
| Use on this project | Only if `project_setup.md` §1 names it | Only if `project_setup.md` §1 names it (the usual mobile choice) | Editor / debug only |
| Runtime game UI | Yes | Yes | Not recommended |
| Editor extensions | Yes | No | Yes |
| Approach | Web-inspired (UXML + USS + C#) | GameObject + Component | Code-only (OnGUI) |
| Layout system | Flexbox (Yoga) | RectTransform + Anchors | Immediate mode |
| Styling | USS stylesheets | Per-component properties | GUIStyle / GUISkin |
| Visual authoring | UI Builder | Scene View | None |
| Performance | Optimized retained mode | Canvas batching | Redraws every frame |
| Data binding | SerializedObject + Runtime binding | Manual via code | Manual via code |
| World-space UI | Supported | Canvas World Space mode | Not supported |
| Input integration | Pointer/Keyboard events | EventSystem + Raycasters | Event.current |

**Decision guide:**
- New runtime UI (menus, HUD, inventory) --> **the UI system named in `project_setup.md` §1**
- New Editor windows / inspectors --> **UI Toolkit**
- Existing project with uGUI --> Continue with **uGUI**; do not migrate unless `project_setup.md` says so
- Quick debug overlays in Editor --> **IMGUI**
- World-space UI on 3D objects --> Either **UI Toolkit** or **uGUI World Space Canvas**

---

## UI Toolkit

UI Toolkit is Unity's modern UI framework inspired by web technologies. It uses UXML for structure, USS for styling, and C# for logic.

### Core Architecture

```
UIDocument (MonoBehaviour on GameObject)
  --> VisualTreeAsset (.uxml)  -- defines structure
  --> StyleSheet (.uss)         -- defines appearance
  --> C# script                 -- defines behavior
```

All UI elements inherit from `VisualElement`. The root is accessed via `rootVisualElement`.

### UXML Structure

UXML defines the UI hierarchy declaratively:

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:uie="UnityEditor.UIElements">
    <ui:Style src="MainMenu.uss" />
    <ui:VisualElement name="root-container" class="container">
        <ui:Label text="Game Menu" class="title" />
        <ui:Button text="Play" name="play-button" class="menu-btn" />
        <ui:Button text="Settings" name="settings-button" class="menu-btn" />
        <ui:Toggle label="Fullscreen" name="fullscreen-toggle" />
        <ui:Slider label="Volume" low-value="0" high-value="100" name="volume-slider" />
        <ui:TextField label="Player Name" name="player-name" />
    </ui:VisualElement>
</ui:UXML>
```

Key points:
- `xmlns:ui="UnityEngine.UIElements"` is the standard namespace
- Reference USS files with `<ui:Style src="..." />`
- Use `name` attribute for C# queries, `class` for USS styling
- Templates can be imported: `<ui:Template src="other.uxml" name="other" />`

### USS Styling

USS uses CSS-like syntax with Unity-specific extensions. All USS properties use the prefix `-unity-` for Unity-specific features.

```css
/* Type selector */
Button {
    background-color: #2D2D2D;
    border-radius: 4px;
    padding: 8px 16px;
    -unity-font-style: bold;
}

/* Class selector */
.menu-btn {
    width: 200px;
    height: 40px;
    margin: 4px 0;
    font-size: 16px;
    color: #FFFFFF;
}

/* Name selector */
#play-button {
    background-color: #4CAF50;
}

/* Pseudo-class */
.menu-btn:hover {
    background-color: #555555;
    scale: 1.05 1.05;
}

.menu-btn:active {
    background-color: #333333;
}

.menu-btn:disabled {
    opacity: 0.5;
}

/* Descendant selector */
.container > Label {
    -unity-text-align: middle-center;
}

/* USS variables */
:root {
    --primary-color: #4CAF50;
    --font-large: 24px;
}

.title {
    color: var(--primary-color);
    font-size: var(--font-large);
}
```

**Selector types:** Type (`Button`), Name (`#name`), Class (`.class`), Universal (`*`), Descendant (`A B`), Child (`A > B`), Multiple (`A.class`), Pseudo-classes (`:hover`, `:active`, `:focus`, `:disabled`, `:checked`).

**Layout is Flexbox-based:** Use `flex-direction`, `flex-grow`, `flex-shrink`, `justify-content`, `align-items`, `align-self`, `flex-wrap`. Default direction is `column`.

### C# Setup and Interaction

```csharp
using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private Button _playButton;
    private Button _settingsButton;
    private Toggle _fullscreenToggle;
    private Slider _volumeSlider;
    private TextField _playerNameField;

    private void OnEnable()
    {
        var root = uiDocument.rootVisualElement;

        // Query single elements by name
        _playButton = root.Q<Button>("play-button");
        _settingsButton = root.Q<Button>("settings-button");
        _fullscreenToggle = root.Q<Toggle>("fullscreen-toggle");
        _volumeSlider = root.Q<Slider>("volume-slider");
        _playerNameField = root.Q<TextField>("player-name");

        _playButton.RegisterCallback<ClickEvent>(OnPlayClicked);
        _settingsButton.RegisterCallback<ClickEvent>(OnSettingsClicked);
        _fullscreenToggle.RegisterValueChangedCallback(OnFullscreenChanged);
        _volumeSlider.RegisterValueChangedCallback(OnVolumeChanged);

        // Query multiple elements by class
        var allButtons = root.Query<Button>(className: "menu-btn").ToList();
    }

    private void OnDisable()
    {
        _playButton.UnregisterCallback<ClickEvent>(OnPlayClicked);
        _settingsButton.UnregisterCallback<ClickEvent>(OnSettingsClicked);
        _fullscreenToggle.UnregisterValueChangedCallback(OnFullscreenChanged);
        _volumeSlider.UnregisterValueChangedCallback(OnVolumeChanged);
    }

    private void OnPlayClicked(ClickEvent evt) => GameDebug.Log("Play clicked");
    private void OnSettingsClicked(ClickEvent evt) => GameDebug.Log("Settings clicked");

    private void OnFullscreenChanged(ChangeEvent<bool> evt)
    {
        Screen.fullScreen = evt.newValue;
    }

    private void OnVolumeChanged(ChangeEvent<float> evt)
    {
        AudioListener.volume = evt.newValue / 100f;
    }
}
```

**Programmatic UI creation (no UXML):**

```csharp
private void CreateUIFromCode()
{
    var root = uiDocument.rootVisualElement;

    var container = new VisualElement();
    container.AddToClassList("container");
    root.Add(container);

    var label = new Label("Created from C#");
    container.Add(label);

    var button = new Button(() => GameDebug.Log("Clicked")) { text = "Click Me" };
    button.name = "dynamic-button";
    container.Add(button);
}
```

### Event System

UI Toolkit events propagate in two phases:
1. **Trickle-down** -- from root to target element
2. **Bubble-up** -- from target back to root

```csharp
// Default: bubble-up phase
element.RegisterCallback<PointerDownEvent>(OnPointerDown);

// Trickle-down phase (parent reacts before children)
element.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);

// Pass custom data to callbacks
element.RegisterCallback<ClickEvent, string>(OnClickWithData, "my-data");

// Set value without triggering ChangeEvent
myControl.SetValueWithoutNotify(newValue);
```

### Data Binding

**SerializedObject binding (Editor / Inspector UI):**

```csharp
// In UXML: <ui:IntegerField binding-path="m_Health" label="Health" />
// In C#:
var healthField = new IntegerField("Health") { bindingPath = "m_Health" };
root.Add(healthField);
root.Bind(new SerializedObject(targetComponent));
```

Bindable objects: MonoBehaviour, ScriptableObject, native Unity types, primitives.
Only the `value` property of `INotifyValueChanged` elements can be bound.

**Runtime binding** connects plain C# objects to UI controls, works in both Editor and runtime contexts. Set data sources on elements and define binding modes for synchronization direction.

See: [references/ui-data-binding.md](references/ui-data-binding.md)

### Manipulators

Manipulators encapsulate event-handling logic, separating interaction from UI code:

```csharp
public class DragManipulator : PointerManipulator
{
    private Vector3 _startPosition;
    private bool _isDragging;

    public DragManipulator(VisualElement target)
    {
        this.target = target;
    }

    protected override void RegisterCallbacksOnTarget()
    {
        target.RegisterCallback<PointerDownEvent>(OnPointerDown);
        target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        target.RegisterCallback<PointerUpEvent>(OnPointerUp);
    }

    protected override void UnregisterCallbacksFromTarget()
    {
        target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
        target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
        target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        _startPosition = evt.position;
        _isDragging = true;
        target.CapturePointer(evt.pointerId);
        evt.StopPropagation();
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (!_isDragging)
            return;

        var delta = evt.position - _startPosition;
        target.transform.position += (Vector3)delta;
        _startPosition = evt.position;
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        _isDragging = false;
        target.ReleasePointer(evt.pointerId);
        evt.StopPropagation();
    }
}

// Usage:
myElement.AddManipulator(new DragManipulator(myElement));
```

**Built-in manipulator classes:** `Manipulator` (base), `PointerManipulator`, `MouseManipulator`, `Clickable`, `ContextualMenuManipulator`, `KeyboardNavigationManipulator`.

### Custom Controls

```csharp
// Unity 6+ recommended pattern: [UxmlElement] attribute (replaces deprecated UxmlFactory/UxmlTraits)
[UxmlElement]
public partial class HealthBar : VisualElement
{
    [UxmlAttribute]
    public float MaxHealth { get; set; } = 100f;

    private VisualElement _fillBar;
    private float _currentHealth;

    public float CurrentHealth
    {
        get => _currentHealth;
        set
        {
            _currentHealth = Mathf.Clamp(value, 0, MaxHealth);
            _fillBar.style.width = Length.Percent(_currentHealth / MaxHealth * 100f);
        }
    }

    public HealthBar()
    {
        AddToClassList("health-bar");
        _fillBar = new VisualElement();
        _fillBar.AddToClassList("health-bar__fill");
        Add(_fillBar);
    }
}
```

---

## uGUI / Canvas System

uGUI is Unity's older GameObject-based UI system. It uses Canvas, RectTransform, and the EventSystem.

### Canvas Render Modes

| Mode | Description | Use Case |
|---|---|---|
| **Screen Space - Overlay** | Renders on top of everything, scales with screen | Standard HUD, menus |
| **Screen Space - Camera** | Rendered by a specific camera, affected by perspective | UI with depth effects |
| **World Space** | Canvas as a 3D object in the scene | In-world displays, VR UI |

### Core Components

**Visual:** Text, Image, RawImage
**Interaction:** Button, Toggle, ToggleGroup, Slider, Scrollbar, Dropdown, InputField, ScrollRect
**Layout:** HorizontalLayoutGroup, VerticalLayoutGroup, GridLayoutGroup, ContentSizeFitter, AspectRatioFitter, LayoutElement

### RectTransform and Anchoring

All uGUI elements use RectTransform instead of Transform. Anchors define how an element positions relative to its parent:
- Anchor Min/Max as fractions (0.0 = left/bottom, 1.0 = right/top)
- Together anchors: fixed position (Pos X, Pos Y, Width, Height)
- Separated anchors: stretching (Left, Right, Top, Bottom padding)
- Pivot: center point for rotation and scaling

### uGUI Example

```csharp
using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Toggle muteToggle;

    private void OnEnable()
    {
        playButton.onClick.AddListener(OnPlayClicked);
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        muteToggle.onValueChanged.AddListener(OnMuteToggled);
    }

    private void OnDisable()
    {
        playButton.onClick.RemoveListener(OnPlayClicked);
        volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
        muteToggle.onValueChanged.RemoveListener(OnMuteToggled);
    }

    private void OnPlayClicked() => GameDebug.Log("Play");
    private void OnVolumeChanged(float value) => AudioListener.volume = value;
    private void OnMuteToggled(bool muted) => AudioListener.pause = muted;
}
```

### Draw Order

Elements render in Hierarchy order: first child drawn first, last child drawn on top. Reorder with `Transform.SetAsFirstSibling()`, `SetAsLastSibling()`, `SetSiblingIndex()`.

See: [references/ugui-legacy.md](references/ugui-legacy.md)

---

## Anti-Patterns

| Anti-Pattern | Problem | Correct Approach |
|---|---|---|
| Using inline styles everywhere | Per-element memory overhead | Use USS files for shared styles |
| Universal selectors in complex USS (`A * B`) | Poor selector performance at scale | Use BEM class naming, child selectors |
| Heavy `:hover` on elements with many descendants | Mouse movement invalidates entire hierarchies | Limit `:hover` to leaf elements |
| Calling `Bind()` inside `CreateInspectorGUI()` | Double-binding, automatic binding occurs after return | Let auto-binding handle it, or call Bind only on manually created UI |
| Rebuilding entire UI every frame | Defeats retained-mode benefits | Update only changed elements |
| Multiple Canvases with dynamic content (uGUI) | Canvas rebuild batches on any child change | Split static and dynamic UI into separate Canvases |
| Not unregistering callbacks | Memory leaks, stale references | Always unregister in `OnDisable` or `OnDestroy` |
| Using IMGUI for runtime game UI | Redraws every frame, poor performance | Use UI Toolkit or uGUI |
| Forgetting EventSystem in scene (uGUI) | No input events processed | Ensure one EventSystem exists in scene |

---

## Key API Quick Reference

### UI Toolkit

| API | Purpose |
|---|---|
| `UIDocument` | MonoBehaviour that hosts a VisualTreeAsset |
| `rootVisualElement` | Root of the visual tree |
| `Q<T>("name")` | Query single element by name |
| `Q<T>(className: "cls")` | Query single element by class |
| `Query<T>().ToList()` | Query multiple elements |
| `RegisterCallback<TEvent>(callback)` | Register event handler |
| `UnregisterCallback<TEvent>(callback)` | Remove event handler |
| `RegisterValueChangedCallback(callback)` | Listen for value changes |
| `SetValueWithoutNotify(value)` | Set value silently |
| `AddToClassList("class")` | Add USS class |
| `RemoveFromClassList("class")` | Remove USS class |
| `AddManipulator(manipulator)` | Attach event manipulator |
| `style.display = DisplayStyle.None` | Hide element |
| `style.display = DisplayStyle.Flex` | Show element |
| `VisualTreeAsset.Instantiate()` | Create instance from UXML |
| `element.Bind(serializedObject)` | Bind to SerializedObject |

### uGUI

| API | Purpose |
|---|---|
| `Canvas` | Root container for all uGUI elements |
| `CanvasScaler` | Controls UI scaling across resolutions |
| `GraphicRaycaster` | Enables input detection on Canvas |
| `EventSystem` | Central input event dispatcher |
| `RectTransform` | Transform with anchoring and sizing |
| `Button.onClick` | UnityEvent for click |
| `Toggle.onValueChanged` | UnityEvent for toggle change |
| `Slider.onValueChanged` | UnityEvent for slider change |
| `LayoutGroup` | Auto-layout for children |

---

## Related Skills

- **unity-foundations** -- GameObject, Component, MonoBehaviour lifecycle
- **unity-scripting** -- C# patterns, SerializeField, events
- **unity-input** -- Input System integration with UI

## TextMeshPro

For all text rendering, use **TextMeshPro** (TMP) — not legacy `UI.Text`. TMP uses SDF rendering for crisp text at any scale. Use `TextMeshProUGUI` for Canvas UI, `TextMeshPro` for 3D world text. Use `SetText("Score: {0}", value)` for zero-allocation updates. See [references/textmeshpro.md](references/textmeshpro.md) for full API, rich text tags, font assets, and patterns.

## Additional Resources

- [UI Toolkit](https://docs.unity3d.com/6000.3/Documentation/Manual/UIToolkits.html) | [USS](https://docs.unity3d.com/6000.3/Documentation/Manual/UIE-USS.html) | [UXML](https://docs.unity3d.com/6000.3/Documentation/Manual/UIE-UXML.html) | [Data Binding](https://docs.unity3d.com/6000.3/Documentation/Manual/UIE-Binding.html) | [Manipulators](https://docs.unity3d.com/6000.3/Documentation/Manual/UIE-manipulators.html)
- [uGUI Canvas](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/UICanvas.html) | [uGUI Layout](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/UIBasicLayout.html)
