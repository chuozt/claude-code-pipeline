# MANDATORY WORKFLOW (AI – DEVELOPER)

**⚠️ INSTRUCTION TO THE AI:** you act as a **collaborative implementer**,
NOT an **autonomous code generator**.
On any violation the AI must stop of its own accord and warn the developer.

---

## Stage 0 — Load The Rules (BEFORE EVERY SESSION)

Exactly `CLAUDE.md` §2 — the Editor bridge stays off (both doors, §2.1), read the listed
`.claude/*.md`, print the confirmation line. Not repeated here so the two cannot drift apart.

---

## Stage 1 — Gather Context & Analyse

- **Read the living documentation:** scan that feature's `docs/features/*.md`.
- **Consult the knowledge base:** open `.claude/kb/INDEX.md`, read only the relevant document.
- **Read the existing code** before proposing anything — do not propose a new architecture
  when the project already has one.
- **Propose 1–3 options**, always stating **WHY** and the **TRADE-OFF**
  (e.g. *"Option A is fast but hard to extend; option B is more complex but safe"*).
- **Wait for the developer to decide.**

### Questions to ask when the request is underspecified

Do not infer. If any of the following is missing, ask:
- Which config does this value belong to? Who tunes it — developer or designer?
- Does this feature affect existing save data? Is a migration needed?
- Does it have to run headless (without Play mode)?
- Does any existing mechanic collide with this?

---

## Stage 2 — Blueprint

- Sketch the flow **in plain language** first.
- List the files to be **CREATED / MODIFIED / DELETED**.
- For stateful logic: give a **transition table** + an **event list** for approval.
- **Ask permission to write:** *"May I write this to [filepath(s)]?"* → **wait for a yes.**
- Several files → ask approval for the **whole changeset**, not one at a time.

---

## Stage 3 — Iterative Implementation

- Code **in small pieces**, following `coding_convention.md` + `architect.md`.
- Edit with `Edit` — **never rewrite a whole file** for a few lines.
- Say which files will be touched **before** touching them.
- After editing `.cs`: **run the compile gate** — `dotnet build Assembly-CSharp.csproj
  --no-restore` with the Editor open, plus `Assembly-CSharp-Editor` and one `.csproj` per
  game `.asmdef`; the list and the baseline warning counts are in `CLAUDE.md` §2.1, so a
  higher count means the edit introduced one.
  Do not skip it and do not guess. It proves compilation only — report *"compiles, not
  tested"*; behaviour still needs EditMode tests or a play-through.
- With the bridge on: scripts are still edited on disk with `Edit`; verify the Editor is
  **not in Play mode, not compiling, and has no modal popup** (`editor_status`) before
  editing; afterwards run `recompile` and poll `recompile_status` until `failed: false`.

### When the fix needs a file outside your scope

If the solution requires touching a file owned by another developer (see `rules.md` §4):
**STOP and report.** That is usually a sign of a missing interface — the right fix is to
change the interface, not to work around it.

---

## Stage 4 — Verification-Driven Delivery

**Every feature needs a way to prove it works before it counts as done.**
Details in `verification.md`. In short:

| Kind of work | Required evidence |
|---|---|
| Logic (formulas, state machines, algorithms) | EditMode unit test **PASSING** |
| Multi-system interaction | Integration test **or** a recorded play-through scenario |
| Levels / balance | Bot results over N runs, with numbers |
| UI / screens | Screenshots or a step-by-step walkthrough |
| Performance | Profiler numbers before/after |
| Feel (VFX, animation) | Video or images + developer sign-off |

### When a bug is reported — 4 steps, no skipping

1. **Collect evidence** — real logs with stack traces. With MCP off, ask the developer to
   paste them, or read `mcp__terminal__read_terminal`; if the Unity console is needed
   (`console` / `get_console_logs`), ask for `/use-mcp`. Check `git log -5`. **Do not guess.**
2. **Narrow it down** — bisect, add logs at the decision points.
3. **Hypothesise** — state *"I believe X happens because Y"*, then find a way to prove or disprove it.
4. **Fix & re-verify** — minimal fix, re-run the check, run regression tests, remove temporary logs.

Not sure of the cause → **say so** and propose how to find out. Never patch the symptom.

---

## Stage 5 — Closing A Task

- Summarise: what was done, what was not, what risk remains.
- State how the developer can verify it themselves.
- Update `docs/features/*.md` if the feature's behaviour changed.
- **Never commit or merge on your own.** If the developer asks for a commit, use the `/commit`
  skill; if they ask for a merge, run it, resolve the conflicts and gate before committing.
