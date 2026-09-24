# Knowledge Base Index (`.claude/kb/`)

Unity 6 + middleware reference material, filtered for mobile casual / puzzle games.

> **HOW TO USE — important:** read this index to see what exists, then **open only the
> document you need**. **Never load the whole KB into context.** Documents run from a few dozen
> to ~1,400 lines; loading blindly burns the context before any work starts.
>
> The right way to ask: *"Read `kb/unity/unity-performance/SKILL.md`, then answer: how to
> measure GC allocation on a real device."*

> ⚠️ **The code samples illustrate an API; `coding_convention.md` is the house style.** The C#
> here was restyled to it (Allman braces, `GameDebug`, `[SerializeField] private` + expression-bodied
> properties, field order) — but plain DTO / `[Serializable]` data structs keep public fields, and a
> few samples keep a coroutine or a static because that is the API being shown. When writing project
> code, **`coding_convention.md` and `CLAUDE.md` §6 win every time**; the UI system and async
> standard are whatever `project_setup.md` §1 names.

---

## A. Unity 6 Core — `kb/unity/`

| Document | Open when you need |
|---|---|
| `unity-foundations` | GameObject, Component, Transform, Prefab, ScriptableObject, project structure |
| `unity-scripting` | Writing MonoBehaviours, `Awaitable`, serialisation, Inspector attributes |
| `unity-lifecycle` | **Initialisation-order traps**, `Awake/Start/OnEnable`, fake-null, DontDestroyOnLoad, editor vs runtime |
| `unity-game-architecture` | Choosing Service Locator vs Singleton vs DI, Event Bus vs SO channel, MonoBehaviour vs plain C# |
| `unity-data-driven` | **ScriptableObject config architecture**, JSON pipeline, designer handoff, versioning and migration |
| `unity-state-machines` | FSM, HFSM, stack-based state machines, testing state machines |
| `unity-editor-tools` | **Building designer tools:** EditorWindow, custom inspectors, PropertyDrawer, Gizmos, Handles, SerializedObject |
| `unity-testing` | Unity Test Framework, EditMode vs PlayMode, `[UnityTest]`, TDD |
| `unity-performance` | **Profiler, Memory Profiler, Frame Debugger**, reading GC allocation, ProfilerMarker |
| `unity-platforms` | Build profiles, `#if UNITY_ANDROID/IOS`, per-platform optimisation |
| `unity-packages-services` | Package Manager, scoped registries, Unity Gaming Services |
| `unity-scene-assets` | Additive scenes, **Addressables vs Resources**, AssetReference, loading screens |
| `unity-save-system` | Choosing a serialisation format, save DTOs, **versioning and migration**, cloud sync, PlayerPrefs |
| `unity-async-patterns` | Async traps: double-await, missing CancellationToken, thread context, errors inside coroutines |
| `unity-game-loop` | **Building the core loop**, session lifecycle, win/lose architecture, tunable difficulty, pacing |
| `unity-procedural-gen` | **Procedural content:** noise, grid/tile systems, seeds and reproducibility, content-budget constraints |
| `unity-ui` | UGUI and UI Toolkit (USS/UXML, data binding) — use only the one `project_setup.md` §1 names |
| `unity-ui-patterns` | **Screen-flow architecture**, View/ViewModel split, HUD, juice, virtualised lists, transitions |
| `unity-input` | The new Input System, Input Actions, touch |
| `unity-input-correctness` | Input traps: `triggered` vs `IsPressed` vs `WasPressedThisFrame`, swapping action maps |
| `unity-audio` | AudioSource, Audio Mixer, audio optimisation |
| `unity-animation` | Animator Controller, blend trees, animation events |
| `unity-graphics` | Built-in / URP, shaders, Shader Graph, materials, post-processing, render optimisation |
| `unity-lighting-vfx` | Lighting, light probes, **Particle System** |
| `unity-physics` | Rigidbody, colliders, triggers, joints (2D and 3D) |
| `unity-physics-queries` | Raycast/overlap traps, `NonAlloc` variants, building LayerMasks, hit ordering — also for tap picking |
| `unity-3d-math` | Maths traps: coordinate systems, Quaternion, Vector3, Bounds, Transform hierarchies, floating-point error |

---

## B. Middleware and Libraries — `kb/middleware/`

| Document | Open when you need |
|---|---|
| `eng-unity-mobile-optimization` | **Mobile-specific optimisation:** memory budget, battery, thermal throttling, quality tiers |
| `tools-unity-profiling` | ProfilerMarker, FrameTimingManager, performance budgets |
| `tools-unity-object-pooling` | **Pooling VFX/projectiles/floating text** to kill GC spikes |
| `tools-unity-scriptable-objects` | SO patterns for data, config, runtime systems |
| `tools-unity-addressables` | Asset loading, reference counting, memory management, remote content |
| `tools-unity-unitask` | **UniTask:** async/await, cancellation, lifetime binding, coroutine interop |
| `tools-unity-vcontainer` | DI: lifetimes, scopes, common traps |
| `tools-unity-state-machine` | Hierarchical states, async transitions, safe teardown |
| `tools-unity-test-framework` | EditMode/PlayMode tests, async tests, mocking, test organisation |
| `tools-unity-ugui` | **Canvas optimisation**, virtualising long lists, mobile-friendly UI |
| `tools-unity-primetween` | High-performance tweening and sequencing (patterns transfer to DOTween; for DOTween itself see the `lib-dotween` skill) |
| `tools-unity-memorypack` | **High-performance binary serialisation** — a good fit for a `.bytes` level format |
| `tools-unity-sentry` | Error and performance monitoring in production |

---

## C. Studio Notes — `kb/goc/`

| Document | Contents |
|---|---|
| `game-feel/gamefeel-theory.md` | **Game feel theory** — the foundation for the polish stage |
| `game-feel/shaders-as-feel.md` | Shaders as a feedback channel: outline flash, dissolve, distortion |
| `chuan-goc/DESIGN_PRINCIPLES.md` | Design principles (studio original) |

---

## D. Quick Lookup By Project Stage

| Stage | Open first |
|---|---|
| **1. Level tool** | `unity-editor-tools` · `unity-data-driven` · `unity-procedural-gen` |
| **2. Core game** | `unity-game-architecture` · `unity-game-loop` · `unity-state-machines` · `unity-lifecycle` · `unity-testing` |
| **3. Mechanics** | `unity-game-architecture` · `tools-unity-state-machine` · `unity-data-driven` · `tools-unity-scriptable-objects` |
| **4. UI** | `unity-ui-patterns` · `unity-ui` · `tools-unity-ugui` |
| **5. Polish** | `goc/game-feel/*` · `unity-lighting-vfx` · `tools-unity-object-pooling` · `unity-performance` · `eng-unity-mobile-optimization` |
| **6. Level review** | `unity-game-loop` · `docs/templates/difficulty-curve.md` · the `gd-level-*` skills |
| **Always relevant** | `unity-lifecycle` · `unity-performance` · `unity-async-patterns` |

---

## E. Deliberately EXCLUDED

Dropped because a mobile casual / puzzle game does not use them. Copy them back from upstream
if a project needs one:

`unity-xr` · `unity-multiplayer` · `unity-ecs-dots` · `unity-ai-navigation` · `unity-npc-behavior` ·
`unity-cinemachine` / `tools-unity-cinemachine` · `unity-level-design` (encounters, checkpoints) ·
`tools-unity-animation` (Animancer) · `tools-unity-physics` (character controllers) · HDRP guide ·
VFX Graph · Timeline · input rebinding/multiplayer · `tools-unity-behavior-designer` ·
`tools-unity-flowcanvas` · `tools-unity-gameplay-ability-system` · `tools-unity-navmesh` ·
`tools-unity-wwise`

`unity-2d` is **worth restoring** for a 2D puzzle game (sprites, atlases, 2D physics, sorting).

Upstream: `claude-unity-game-studio` — `unity-knowledge-skills/skills/` and
`unity-middleware-skills/skills/`.
