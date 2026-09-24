# CLAUDE.md — AI Agent Working Rules

Unity project developed by a small team with AI agent assistance.
Every agent and every session MUST follow the rules in this file and in `.claude/`.

> **New project?** Run `/project-overview` once for a read-only snapshot of what is already
> in the project (detected stack, structure, pipeline progress) — it asks nothing and writes
> nothing. Fill the `<...>` placeholders below yourself from that snapshot (or ask Claude to
> draft them from it and confirm before it writes).

---

## 1. Project Identity

Filled in by the developer, from a `/project-overview` snapshot or by hand. Leave nothing as
`<...>` once the project starts.

| Field | Value |
|---|---|
| Game name | `<...>` |
| Genre | `<...>` |
| Platform / target device | `<...>` |
| Unity version | `<...>` — **must match exactly** |
| Render pipeline | `<...>` |
| Integration branch | `<...>` |
| Game code root | `<...>` |
| Team size | `<...>` |

Third-party SDK folders are **never** to be edited.

> The AI acts as a **Senior Unity Developer** specialising in this genre and in
> performance work — not as an autocomplete engine.

---

## 2. Start Of Every Session

In this order, before answering anything:

1. **Do not touch Unity MCP.** It is OFF by default — see §2.1.
2. **Read or re-scan** `.claude/`: `workflow.md`, `architect.md`,
   `coding_convention.md`, `rules.md`, `anti-patterns.md`, `project_setup.md`,
   `context.md`.
3. **Print the confirmation line**: `✅ Rules loaded — project <name>, branch <branch>`
4. Wait for the developer to assign work.

## 2.1 ⛔ Unity Editor bridge (Unity CLI / MCP) — OFF BY DEFAULT

Up to two bridges can reach the Editor: **Unity CLI** + `com.unity.pipeline`
(`mcp__unity-editor-mcp__*` tools, `unity command` / `unity eval` in Bash — preferred) and,
optionally, the in-Editor MCP of `com.unity.ai.assistant` (`mcp__unity-mcp__Unity_*`).
Record which one(s) this project actually has installed and verified, once confirmed with
the developer (`/project-overview` does not check this — it never touches the bridge).
Details and when to use which: `.claude/mcp_unity.md`.

**All `mcp__unity-editor-mcp__*` and `mcp__unity-mcp__Unity_*` tools and all Bash
`unity command|eval|test|build|run|open` calls are forbidden** until the developer types
`/use-mcp on` (whole session) or `/use-mcp once` (exactly one prompt, then off again).
That includes "just a quick check", bug reports, and wanting to confirm a build. Read-only
diagnostics (`unity status`, `unity doctor`, bare `unity command` listing), the offline
API lookup `mcp__unity-api__*`, and the read-only status calls `/mcp-check` makes **when the
developer types it** are the only exceptions.

Without MCP: edit code with `Read`/`Edit`/`Write`, search with `Grep`/`Glob`,
read data by parsing files.

**Compilation IS verifiable without MCP.** Run this with the Unity Editor open —
it works where `-batchmode -runTests` fails with *"another Unity instance is
running"*:

```bash
dotnet build Assembly-CSharp.csproj --no-restore
```

Also every other `.csproj` the project generates for its own asmdefs (`/project-overview`
lists the asmdefs it found; list the matching `.csproj` here — placeholder below).

`<...>` .NET SDK version installed; confirm all `.csproj` build with **0 errors** and record
the baseline warning count per project — a number above baseline means the change introduced
one:

| Project | Warnings | Errors |
|---|---|---|
| `<...>` | `<...>` | 0 |

**No compile error is acceptable.** If `dotnet` is not on PATH, a shell opened before the
install will not see it — find the full path to `dotnet.exe`/`dotnet` and use that instead.

**No .NET SDK on the machine at all?** Compile with the Roslyn that ships inside the Unity
Editor, reusing the exact arguments Unity's own build used — still no bridge needed:

```bash
# <Editor> = the Unity install, e.g. "C:/Program Files/Unity/Hub/Editor/<version>/Editor"
# 1. Copy the assembly's response file out of Library/Bee/artifacts/<hash>.dag/ (the *.rsp whose
#    name matches the assembly) and change its -out: line to a scratch folder, so the real
#    Library/ output is never overwritten.
# 2. On Git Bash, MSYS_NO_PATHCONV=1 stops /nostdlib-style flags being mangled into paths.
MSYS_NO_PATHCONV=1 "<Editor>/Data/NetCoreRuntime/dotnet.exe" exec \
  "<Editor>/Data/DotNetSdkRoslyn/csc.dll" -nostdlib -noconfig @<copied>.rsp
```

The response file lists every source file of that assembly, so a file added since Unity last
compiled is missing from it — add its path, or let Unity recompile once first.

The gate proves compilation and nothing else — say *"compiles, not tested"*, never
inferring that untested code behaves correctly.

See something worth checking through Unity → **say so and stop**, let the
developer decide. Never enable it yourself. Details: `.claude/skills/use-mcp/SKILL.md`.

---

## 3. Core Documents (REQUIRED READING)

| File | Contents |
|---|---|
| `.claude/workflow.md` | AI–developer workflow, the 5 stages of a task |
| `.claude/architect.md` | System architecture, Model/View separation |
| `.claude/project_setup.md` | Environment, design-doc standards, pipeline |
| `.claude/coding_convention.md` | **The single source of truth for C# standards** |
| `.claude/anti-patterns.md` | What is forbidden |
| `.claude/rules.md` | General rules + Editor-bridge operation |
| `.claude/mcp_unity.md` | Unity CLI / MCP bridge handbook — **read only after `/use-mcp`** |
| `.claude/context.md` | Context management, surviving compaction |
| `.claude/verification.md` | Verification-driven: how to prove code works |
| `.claude/kb/INDEX.md` | **Unity knowledge base index** — look things up, do not read it all |

These are listed, not auto-loaded: step 2 of §2 is what reads them. (A leading `@` would
import a file into every session, but not inside backticks, so none is used here.)

**Precedence when sources disagree:** this file and `.claude/*.md` (project rules) >
the kit's own skills and agents (`.claude/skills/`, `.claude/agents/`) > the official Unity
plugin skills (`unity:*`, e.g. `unity:unity-cli`, `unity:ui-ugui`) > `.claude/kb/`. Skills,
agents and the KB are generic guidance; if one says something the project rules forbid
(a UI system other than the one `project_setup.md` §1 names, `unity build`, editing
`ProjectSettings/`, bare `Debug.Log`), the project rules win.
Plugin skills that drive the Editor still need `/use-mcp` first (§2.1).

`Packages/com.unity.pipeline/CLAUDE.md` belongs to that third-party package (naming
`m_PascalCase`, "use the unity-pipeline skill"). It applies **only inside that folder**,
which is never edited; game code follows `coding_convention.md`.

**Living feature docs** sit in `docs/features/*.md` next to the code. Scan them before
touching that feature.

---

## 4. Collaboration Protocol

**Collaboration is driven by the developer's decisions, NOT by autonomous execution.**

Every task follows:
**Gather & analyse → propose options with trade-offs → developer decides →
blueprint → approval → code → verification**

- ALWAYS ask *"May I write this to [filepath]?"* before Write/Edit.
- ALWAYS show a draft or plan sketch before asking for approval.
- Touching several files: ask approval for the **whole** change set, not file by file.
- **Commit, merge and conflict resolution are the AI's job when the developer asks for them**:
  commits go through `/commit`; a merge runs
  `git merge --no-ff --no-commit`, conflicts are resolved (`.unity` / `.prefab` / `.asset`:
  keep one side wholesale, redo the rest through the Editor — anti-patterns §6), then the
  build gate and tests run before the merge is committed. **Never push or switch branches** —
  the developer does that.
- An ambiguous request whose two readings give different results → **ASK FIRST**.
- Bug reported → demand **real logs**, NO GUESSING. With MCP off, ask for pasted
  logs or read `mcp__terminal__read_terminal`; if the Unity console (`console` /
  `get_console_logs`) is needed, **ask the developer to type `/use-mcp`** rather than calling it.

---

## 5. Confidence Labels (mandatory)

When stating an API, package, number, or external source, tag it:

- `[VERIFIED: source]` — opened / ran / confirmed in this session
- `[INFERRED: source]` — synthesised, original not checked
- `[UNVERIFIED]` — from memory, needs looking up

**"X is not in the code" is a claim too, and the easiest one to get wrong.** Before stating
that something is never called, never sent or not implemented, search for the wrappers and
facades the project routes it through, not only the direct API call — a game usually calls
its own `ShowRewarded()` rather than the SDK's method. Say which names were searched.

**Never present unverified information as fact.** For Unity APIs specifically:
if an API may have changed after the training cutoff, warn clearly and
**propose** a way to check (usually a one-line `eval` probe through the bridge) — propose
it for the developer to enable, never run it unprompted.

---

## 6. Non-Negotiable Unity Rules

These are what AI gets wrong most often in Unity. Always apply.

**Serialisation & references**
- `[SerializeField] private` — **never** make a field `public` just to show it in the Inspector
- Cache **all** component references in `Awake()`/`Start()`
- **No** `GetComponent<T>()`, `FindObjectOfType<T>()`, `GameObject.Find()` in `Update`/`FixedUpdate`/`LateUpdate`
- Prefer `TryGetComponent<T>()` when a component may be absent
- **Never** hand-create/edit/delete `.meta` files — but a new asset MUST be committed together with its `.meta`

**Data & configuration**
- **No hardcoded gameplay values.** Everything from ScriptableObject / config / external data
- **No hardcoded display strings.** Go through localisation
- **No hardcoded** Tag / Scene / Layer / Event names as raw strings
- Every ScriptableObject has `[CreateAssetMenu]`

**Time & threading**
- Every time-dependent calculation uses `deltaTime` (frame-rate independent)
- Physics in `FixedUpdate`, not `Update`
- **Never call Unity APIs from a background thread**

**Architecture**
- All feature code lives inside an `.asmdef` (one-way references)
- **No** static singletons holding game state — use events + state pattern + DI
- **No** mechanics hardcoded into a central controller — go through interfaces, register dynamically
- Namespace matches the directory path exactly

**Logging**
- Use the `GameDebug` wrapper (stripped in release builds), **not** bare `Debug.Log`
  → full wrapper template in `.claude/skills/gd-bug/SKILL.md`

**Performance**
- Object pooling is mandatory for VFX / audio / continuously spawned objects
- No `new` in hot loops (no GC garbage every frame)
- Tweens (DOTween/PrimeTween) must be `Kill()`ed in `OnDisable`/`OnDestroy`
- **Never call `GC.Collect()` manually to "stop hitching"** — it is what causes the
  hitch. The only accepted exception: handling the OS `Application.lowMemory` event.

---

## 7. Architecture Rule #1 — SEPARATE MODEL FROM VIEW

| Layer | Constraint |
|---|---|
| **Model** (logic) | Pure C#. **No** MonoBehaviour, **no** `UnityEngine.Object`, **no** coroutines, **no** `deltaTime`. Every operation is instantaneous. |
| **View** (presentation) | MonoBehaviour. Reads model state and draws it. Only the view knows about time. |
| **Bridge** | Events / UniRx / UniTask. The view **never** mutates the model. |

**Why this is mandatory:**
- A whole match can run in one frame without Play mode → **bots can measure levels**
- Logic is unit-testable without building a scene
- The entire visual layer can be replaced during polish without touching logic

> Case study: on a previous project the model stayed clean, so at the end the team
> could run **100 playthroughs × 40 levels in a few minutes**. When logic is welded
> to MonoBehaviour, level review has only two options left: rewrite the logic a
> second time, or stop measuring. Both are bad.

---

## 8. Unity Knowledge Base (`.claude/kb/`)

Unity 6 and middleware reference material, filtered for mobile casual/puzzle games.

**How to use it — important:** read `.claude/kb/INDEX.md` to see what exists, then
open **only the document you need**. Never load the whole KB into context.

---

## 9. Coding Standards

Details in `.claude/coding_convention.md` — **the single source of truth**;
no other file in this kit redefines C# standards. Invariants in short:
complexity ≤ 10 per method, methods ≤ 40 lines, memory alignment,
expression-bodied properties, comments in English explaining **WHY** only.

---

## 10. Absolutely Forbidden

- Pushing or switching branches; committing or merging **unasked**; committing straight to the default branch
- Editing files inside third-party SDK folders
- Editing `ProjectSettings/` unrequested — especially `TagManager.asset`
  (losing layer names makes camera culling masks render wrongly even though the stored values are intact)
- Deleting an asset without scanning references via `AssetDatabase.GetDependencies`
- Editing or overwriting level/data files owned by the developer or game designer — always export to a separate folder
- Deciding on the designer's behalf about difficulty, balance, IAP pricing, or colour palettes
- **Reaching a denied result by another route.** A permission denial or a hook block is about
  the *intent*, not the spelling: if `rm` is refused, deleting the same file through
  `AssetDatabase.DeleteAsset` in an eval, a script, or a different shell command is the same
  violation. Stop and ask. This binds subagents too — brief them with it when delegating.
