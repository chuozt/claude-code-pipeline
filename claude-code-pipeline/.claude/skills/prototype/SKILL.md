---
name: prototype
model: claude-opus-5-5
effort: medium
description: "Throwaway prototype to answer ONE question about fun or feel (does this mechanic feel good, is this camera readable) — relaxed standards, playable in the Editor, deleted afterwards, plus a short report. Not the bot-playable simulation: that is /gd-prototype-sim. Use when typing /prototype or saying 'quick throwaway test', 'hack it together to see if it is fun', 'spike this mechanic'."
argument-hint: "[concept-description]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Edit, Bash, Task
agent: prototyper
isolation: worktree
---

> **Coding rule — mandatory.** Every line of C# this skill writes, reviews, or proposes must follow `.claude/coding_convention.md` (Allman braces, §9 script layout, field order and naming, `GameDebug` instead of `Debug.Log`, no `{ get; private set; }`). Where any sample or advice below disagrees with that file, the convention wins.

## Phase 1: Define the Question

Read the concept description from the argument. Identify the core question this prototype must answer. If the concept is vague, state the question explicitly before proceeding — a prototype without a clear question wastes time.

---

## Phase 2: Load Project Context

Read `CLAUDE.md` for project context and the current tech stack. Understand what engine, language, and frameworks are in use so the prototype is built with compatible tooling.

---

## Phase 3: Plan the Prototype

Define in 3-5 bullet points what the minimum viable prototype looks like:

- What is the core question?
- What is the absolute minimum code needed to answer it?
- What can be skipped (error handling, polish, architecture)?

Present this plan to the user before building. Ask for confirmation if scope seems unclear.

---

## Phase 4: Implement

Ask: "May I create the prototype directory at `prototypes/[concept-name]/` and begin implementation?"

If yes, create the directory. Every file must begin with:

```
// PROTOTYPE - NOT FOR PRODUCTION
// Question: [Core question being tested]
// Date: [Current date]
```

Standards are intentionally relaxed:

- Hardcode values freely
- Use placeholder assets
- Skip error handling
- Use the simplest approach that works
- Copy code rather than importing from production

Run the prototype. Observe behavior. Collect any measurable data (frame times, interaction counts, feel assessments).

---

## Phase 5: Generate Prototype Report

Draft the report:

```markdown
## Prototype Report: [Concept Name]

### Hypothesis
[What we expected to be true -- the question we set out to answer]

### Approach
[What we built, how long it took, what shortcuts we took]

### Result
[What actually happened -- specific observations, not opinions]

### Metrics
[Any measurable data collected during testing]
- Frame time: [if relevant]
- Feel assessment: [subjective but specific -- "response felt sluggish at
  200ms delay" not "felt bad"]
- Player action counts: [if relevant]
- Iteration count: [how many attempts to get it working]

### Recommendation: [PROCEED / PIVOT / KILL]

[One paragraph explaining the recommendation with evidence]

### If Proceeding
[What needs to change for a production-quality implementation]
- Architecture requirements
- Performance targets
- Scope adjustments from the original design
- Estimated production effort

### If Pivoting
[What alternative direction the results suggest]

### If Killing
[Why this concept does not work and what we should do instead]

### Lessons Learned
[Discoveries that affect other systems or future work]
```

Ask: "May I write this report to `prototypes/[concept-name]/REPORT.md`?"

If yes, write the file.

---

## Phase 6: Developer Decision

Present the prototyper's PROCEED / PIVOT / KILL recommendation, with the evidence behind
it and how it sits against the game pillars in `design/gdd/game-concept.md` (if it exists).
Use `AskUserQuestion` to let the developer confirm or override it. Their decision is
final; update the REPORT.md `Recommendation` section if it differs from the prototyper's.

---

## Phase 7: Summary and Next Steps

Output a summary to the user: the core question, the result, the prototyper's initial recommendation, and the developer's final decision. Link to the full report at `prototypes/[concept-name]/REPORT.md`.

If **PROCEED**: run `/design-system` to begin the production GDD for this mechanic, or `/architecture-decision` to record key technical decisions before implementation.

If **PIVOT** or **KILL**: no further action needed — the prototype report is the deliverable.

Verdict: **COMPLETE** — prototype finished. Recommendation is PROCEED, PIVOT, or KILL based on findings above.

### Important Constraints

- Prototype code must NEVER import from production source files
- Production code must NEVER import from prototype directories
- If the recommendation is PROCEED, the production implementation must be written from scratch — prototype code is not refactored into production
- Total prototype effort should be timeboxed to 1-3 days equivalent of work
- If the prototype scope starts growing, stop and reassess whether the question can be simplified

---

## Recommended Next Steps

- **If PROCEED**: Run `/design-system [mechanic]` to author the production GDD, or `/architecture-decision` to record key technical decisions before implementation
- **If PIVOT**: Run `/prototype [revised-concept]` to test the adjusted direction
- **If KILL**: No further action required — the prototype report is the deliverable
- Run `/playtest-report` to formally document any playtest sessions conducted during prototyping
- **If PROCEED and this is a puzzle/resource-flow game**: phase 1 is finished — hand over to the
  designer with `/gd-mode` then `/gd-map-flow`. See `/gd-workflow-help` for the full pipeline.
