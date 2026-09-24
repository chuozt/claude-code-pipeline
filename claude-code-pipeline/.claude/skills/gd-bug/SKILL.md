---
name: gd-bug
model: claude-opus-5-5
effort: medium
description: The mandatory 4-step Unity debugging procedure - collect evidence, narrow down, hypothesise, fix and re-verify. No guessing and no symptom patching. Use when the developer types /gd-bug or says "there is a bug", "crash", "not working", "null reference", "test failing", "why is this happening".
---

> **Coding rule — mandatory.** Every line of C# this skill writes, reviews, or proposes must follow `.claude/coding_convention.md` (Allman braces, §9 script layout, field order and naming, `GameDebug` instead of `Debug.Log`, no `{ get; private set; }`). Where any sample or advice below disagrees with that file, the convention wins.

# /gd-bug — Unity debugging

**Never jump straight to the fix.** Four steps, in order.

## Step 1 — Collect evidence

1. Get the Unity console logs with stack traces. **DO NOT GUESS.** No logs means go get logs.
2. Ask the developer for exact reproduction steps. If it does not reproduce, say so explicitly.
3. Determine the scope: one script? several systems? or a timing problem?
4. `git log -5` — recent changes are the prime suspect.

## Step 2 — Narrow it down

Disable and bisect until the bug disappears. Add logs at **decision points**, not scattered around.

Six causes account for most Unity bugs:
- **Initialisation order** — `Awake` vs `Start` vs `OnEnable`; system A using B before B is ready
- **Null / fake-null** — a destroyed object is still non-null to plain C#; a field left unassigned in the Inspector
- **Calling Unity APIs from a background thread**
- **State not reset** in `OnDisable`/`OnDestroy`; events left subscribed
- **Physics placed in `Update`** instead of `FixedUpdate`
- **Tweens outliving** the object they animate

## Step 3 — Hypothesise

Say it out loud: *"I believe X happens because Y."*
Then **find a way to prove or disprove it** — usually a small unit test or a conditional log.

Not sure → **say you are not sure**, and propose how to check. Never guess and then fix.
If the test disproves the hypothesis → go back to step 2, do not fix blindly.

## Step 4 — Fix & re-verify

1. Make the **minimal** fix, at the root cause. No opportunistic refactoring.
2. Re-run the test from step 3.
3. Run the regression tests around that area.
4. **Write a test that catches this exact bug** if it comes back.
5. Remove every temporary log.
6. Confirm the project compiles cleanly.

## Rules

- Root cause, never symptom patching.
- Every bug leaves behind a regression test.
- Use `GameDebug`, not bare `Debug.Log`.
- Without real logs, no conclusion about the cause is allowed.

## The GameDebug wrapper (create once per project)

```csharp
using System.Diagnostics;
using System.Runtime.CompilerServices;

public enum LogTopic { General, Gameplay, UI, Audio, Level, Mechanic }

public static class GameDebug
{
    [Conditional("ENABLE_LOGS")]
    public static void Log(string message, LogTopic topic = LogTopic.General,
        [CallerFilePath] string file = "", [CallerMemberName] string member = "",
        [CallerLineNumber] int line = 0) =>
        UnityEngine.Debug.Log(Format(topic, message, file, member, line));

    [Conditional("ENABLE_LOGS")]
    public static void LogWarning(string message, LogTopic topic = LogTopic.General,
        [CallerFilePath] string file = "", [CallerMemberName] string member = "",
        [CallerLineNumber] int line = 0) =>
        UnityEngine.Debug.LogWarning(Format(topic, message, file, member, line));

    [Conditional("ENABLE_LOGS")]
    public static void LogError(string message, LogTopic topic = LogTopic.General,
        [CallerFilePath] string file = "", [CallerMemberName] string member = "",
        [CallerLineNumber] int line = 0) =>
        UnityEngine.Debug.LogError(Format(topic, message, file, member, line));

    private static string Format(LogTopic topic, string msg, string file, string member, int line)
    {
        var fileName = System.IO.Path.GetFileNameWithoutExtension(file);
        return $"[{topic}] {fileName}.{member}:{line} - {msg}";
    }
}
```

Add `ENABLE_LOGS` to the Scripting Define Symbols **for Development builds only**.
Output: `[Gameplay] BoardModel.Collect:117 - piece stranded with no legal target`
