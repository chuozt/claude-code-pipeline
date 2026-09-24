---
name: project-overview
description: "Give a quick, read-only snapshot of a Unity project - detected tech stack, structure, and how far the pipeline docs have progressed. Never asks the developer anything and never touches the Unity Editor bridge. Use when the developer types /project-overview or says \"give me an overview of this project\", \"what's in this project\", \"summarize the project setup\", \"I just copied the kit over\"."
---

# /project-overview — read-only project snapshot

Purely observational. Scans what already exists in the project and reports it back —
nothing more.

- **Never asks the developer anything.** No confirmation prompts, no "which of these is
  right?", no list of questions to answer. If something cannot be found on disk, report it
  as unknown and move on.
- **Never touches the Unity Editor bridge.** No `mcp__unity-editor-mcp__*` / `mcp__unity-mcp__Unity_*`
  tool, no `unity status` / `unity doctor` / any `unity` CLI call, read-only or not. Everything
  below comes from files already on disk.
- **Never writes anything.** No filling `CLAUDE.md` placeholders, no creating the `design/` /
  `production/` / `docs/` scaffold. If the developer wants either done, that is a separate,
  explicit request — point them at it, do not do it as part of this skill.

## What to scan

All of this is `Read` / `Glob` / `Grep` on files, or a plain `git` command — nothing else.

| Item | Where to look |
|---|---|
| Unity version | `ProjectSettings/ProjectVersion.txt` |
| Render pipeline | `Packages/manifest.json` (`com.unity.render-pipelines.*`) |
| Packages in use | `Packages/manifest.json` |
| Async / tween / DI | search for `UniTask`, `DOTween`, `PrimeTween`, `VContainer`, `Zenject` in `Packages/` and `Assets/` |
| Asset loading | is there Addressables, or is it `Resources/` |
| UI system | is there `UIElements`/`UIDocument`, UGUI, or a custom framework |
| Git branches | `git branch -a` |
| Main code folder | the largest non-SDK subfolder in `Assets/` |
| asmdef layout | `find . -name "*.asmdef"` |
| `CLAUDE.md` state | does §1 still have `<...>` placeholders, or is it filled? |
| Coding convention | `.claude/coding_convention.md`, if present |
| Working scaffold | do `design/`, `production/`, `docs/` exist, and what is in each? |
| Session state | does `docs/_session/active.md` exist — if so, read its `<!-- STATUS -->` block only |

## Report format

```
## Project overview — <game name from CLAUDE.md if filled, else the folder name>

### Detected
Unity <version> · <pipeline> · <UI> · <async> · <tween> · <DI>
Branches: <list>   Code folder: <path>   asmdefs: <count>

### CLAUDE.md
<filled | has N placeholders left in §1 | not present>

### Pipeline progress
design/     <what exists, or "empty/absent">
production/ <what exists, or "empty/absent">
docs/       <what exists, or "empty/absent">
Session state: <one line from active.md's STATUS block, or "none found">

### Worth a look
<anything odd, purely observational — e.g. no asmdef under the code folder, model code
still references UnityEngine, CLAUDE.md placeholders left unfilled. State it, do not act
on it and do not ask what to do about it.>
```

Nothing in this report is a question, a checklist item awaiting an answer, or a "should I…" —
it is a description of what is there right now.

## After the snapshot: the coding convention is now binding

This is the one rule in this skill that reaches past the report itself. Once
`.claude/coding_convention.md` has been read as part of this snapshot, **every C# file
touched for the rest of the session must follow it** — naming, formatting, complexity
limits, comment style, whatever it specifies — the same way `CLAUDE.md`/`coding_convention.md`
already bind every session per `.claude/README.md`'s load order. This skill does not relax
that; it is simply the point where the convention has now definitely been read, so there is
no "I hadn't seen it yet" excuse afterward.

**Scope: applies whenever `/gd-mode` is off** (the normal, technical mode this skill itself
runs in). While `/gd-mode` is on, its own hard rule already bans touching or reading code at
all (`skills/gd-mode/SKILL.md` §0/§3.0), so the coding convention has nothing to attach to
until the developer leaves that mode — at which point this rule resumes applying.

If `.claude/coding_convention.md` does not exist, say so in "Worth a look" and do not invent
a convention to follow instead.
