# Technical Preferences

<!-- Filled by the developer (a /project-overview snapshot helps). Updated as decisions are made. -->
<!-- All agents reference this file for project-specific standards and conventions. -->
<!-- Kit template: every <...> is filled in per project. Leave nothing as <...> once the project starts. -->

## Engine & Language

- **Engine**: `<...>` (exact Unity version, must match `CLAUDE.md` §1)
- **Language**: `<...>` (C# version)
- **Rendering**: `<...>` (Built-in / URP / HDRP)
- **Physics**: `<...>`
- **Async**: `<...>` (UniTask / `Awaitable` — never both in one system)

## Input & Platform

- **Target Platforms**: `<...>`
- **Input Methods**: `<...>`
- **Primary Input**: `<...>`
- **Gamepad Support**: `<...>`
- **Touch Support**: `<...>`
- **Platform Notes**: `<...>` (input system, target SDK levels, anything a platform forbids)

## Naming Conventions

Defined in `.claude/coding_convention.md` §2 — the single source of truth; do not restate
them here. Add below only what that file does not cover:

- **Scenes/Prefabs**: `<...>`

## Performance Budgets

- **Target Framerate**: `<...>`
- **Frame Budget**: `<...>` ms
- **Draw Calls**: `<...>`
- **Memory Ceiling**: `<...>`
- **GC**: zero per-frame allocation in hot paths

## Testing

- **Framework**: Unity Test Framework (NUnit), EditMode tests
- **Minimum Coverage**: `<...>`
- **Required Tests**: `<...>`
- **Model layer must be testable without a scene** — pure C#, no MonoBehaviour

## Forbidden Patterns

Inherited from the CCGS defaults:

- Legacy Input Manager (`Input.GetKey`, `Input.GetAxis`)
- `GameObject.Find()` at runtime
- Singleton MonoBehaviours without proper lifecycle management
- Hardcoded gameplay values (must be in ScriptableObjects or config)

Kit hard rules (CLAUDE.md section 6 — non-negotiable):

- `public` fields used only to expose a value in the Inspector — use
  `[SerializeField] private`
- `GetComponent<T>()`, `FindObjectOfType<T>()`, `GameObject.Find()` inside
  `Update` / `FixedUpdate` / `LateUpdate`
- Raw `Debug.Log` — use the `GameDebug` wrapper (stripped in release builds)
- Manual `GC.Collect()` to "fix stutter" — it is the cause, not the cure. The
  only accepted exception is handling the OS `Application.lowMemory` event
- Hardcoded display strings (must go through localization)
- Hardcoded Tag / Scene / Layer / Event names as inline string literals
- Physics logic in `Update` — it belongs in `FixedUpdate`
- Unity API calls from a background thread
- Tweens without `Kill()` in `OnDisable` / `OnDestroy`
- `new` inside hot loops
- Model layer touching `UnityEngine.Object`, coroutines, or `deltaTime`
- Feature code outside an `.asmdef`
- Editing an immutable framework/template folder to implement a game
- Editing third-party SDK folders

Project-specific additions: `<...>`

## Allowed Libraries / Addons

Currently installed and in active use:

- `<...>`

> Do not add speculative dependencies here. A library is listed only once its
> integration has actually begun.

## Architecture Decisions Log

One line per accepted ADR (`docs/architecture/`): id, title, and the decision in a sentence.

- `<...>`

## Engine Specialists

- **Primary**: unity-specialist
- **Language/Code Specialist**: unity-specialist (C# review — primary covers it)
- **Shader Specialist**: unity-shader-specialist (HLSL, shaders, materials)
- **UI Specialist**: unity-ui-specialist (`<...>` — the project's UI system)
- **Additional Specialists**: `<...>` (e.g. unity-addressables-specialist if Addressables is used)
- **Routing Notes**: Invoke primary for architecture and general C# code review.
  Invoke shader specialist for rendering and visual effects. Invoke UI specialist
  for all interface implementation. `<...>` (list the specialists that do NOT apply,
  e.g. unity-shader-specialist on a project with no custom shaders.)

### File Extension Routing

| File Extension / Type | Specialist to Spawn |
|-----------------------|---------------------|
| `*.cs` (game code) | unity-specialist |
| `*.shader`, `*.hlsl`, `*.cginc` | unity-shader-specialist |
| UI prefabs, screen prefabs | unity-ui-specialist |
| `*.unity`, `*.prefab` (scenes/prefabs) | unity-specialist |
| `*.asmdef` (assembly definitions) | unity-specialist |
| General architecture review | unity-specialist |
