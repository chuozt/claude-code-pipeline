---
paths:
  - "**/Scripts/**/UI/**"
  - "**/Screens/**"
---

# Rules For UI Code

- UI **never** owns or directly mutates game state. It displays, and it sends commands/events.
- UI **never** blocks the game loop (no synchronous waiting).
- **No** instantiating UI prefabs directly — go through the project's UI framework.
- **No** `GameObject.Find()`, **no** `GetComponent()` in `Update()`.
- **No** hardcoded display strings — go through localisation.
- **One screen, one prefab.** Never merge several screens into one large prefab.
- Subscribe in `OnEnable`, unsubscribe in `OnDisable`. `Kill()` tweens on disable/destroy.
- Support safe area and the full aspect-ratio range declared in `project_setup.md`.

## Checklist before handing over a screen
- [ ] Enter, exit, re-enter — no errors, no leaked events
- [ ] Correct at the narrowest and the widest supported aspect ratio
- [ ] Holds no reference to the model after closing
