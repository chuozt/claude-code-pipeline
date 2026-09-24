---
paths:
  - "**/Editor/**"
---

# Rules For Editor / Tool Code

- Every edit operation must support **Undo** (`Undo.RecordObject` / `Undo.RegisterCreatedObjectUndo`).
  Designers will misclick — a tool without Undo is a hated tool.
- **Never overwrite a designer's data file without asking.** There must be an explicit
  confirmation step, and preferably a mode that exports to a separate folder for comparison first.
- Script-generated assets must ship with their `.meta` (call `AssetDatabase.Refresh()` / `ImportAsset`).
- Deleting an asset requires scanning `AssetDatabase.GetDependencies` first.
- **Business logic (validators, simulators) must NOT live inside an EditorWindow.**
  It belongs in a plain C# class that can run headless — otherwise it cannot run in
  batch and cannot go into CI.
- An EditorWindow growing large → split into `partial class` per tab.
- Editor code lives in its own `.asmdef` with `includePlatforms: [Editor]`.
- Every batch-capable feature should expose a **static method returning JSON** so AI/CI
  can call it in one line.

## Three things every tool must have
1. A validator checking data invariants, reporting errors with an exact location
2. A headless simulator that runs N trials without Play mode
3. A "check everything" button that runs in batch and writes a report to file
