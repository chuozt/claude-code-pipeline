# Context Management

Context is the scarcest resource in a working session. Manage it actively; do not wait
for it to fill up.

---

## 1. First Principle — FILES Are The Memory, Not The Conversation

The conversation gets compacted or lost. Files on disk survive.
**Every important decision is written to a file the moment it is settled**, never left in chat.

### The session state file

Keep `docs/_session/active.md` as a live save point. Update it after every milestone:
- A design section is approved and written to file
- An architectural decision is settled
- An implementation milestone completes
- A test or measurement produces results

Minimum contents: **current task · progress checklist · settled decisions · files being edited · open questions.**

After any interruption (compaction, crash, `/clear`): **read this file first.**
Template: `.claude/templates/SessionState.md`

---

## 2. Write Files Incrementally

When producing a multi-section document (GDD, architecture doc, report):

1. Create the file **immediately** with an empty skeleton (all section headings)
2. Discuss and draft **one section at a time**
3. As each section is approved, **write it to the file right away**
4. Update the state file
5. Once written, the discussion of that section can be compacted safely — the decision is in the file

This keeps context holding only the **current section** (~3–5k tokens) instead of the entire
discussion history of the whole document (~30–50k tokens).

---

## 3. Compact Proactively

- **Compact at ~60–70% of context**, not at the limit
- Use `/clear` between unrelated tasks, or after two consecutive failed fixes
- Natural compaction points: after writing a section to file · after a commit ·
  after finishing a task · before opening a new topic
- Compact with direction:
  `/compact Focus on <current task> — sections 1-3 are written to file, working on section 4`

---

## 4. Context Budget By Kind Of Work

| Kind of work | Start-up budget |
|---|---|
| Reading / review | ~3k tokens |
| Implementing one feature | ~8k tokens |
| Refactoring across systems | ~15k tokens |

Exceeding the budget during start-up means too much is being loaded. Cut the reading list.

---

## 5. Four Rules For Saving Context With The Unity Editor Bridge

1. **Never** "read the whole Scripts folder". Name specific files, and read them from disk.
2. `eval` snippets, `console` dumps and `get_scene_hierarchy` are the token sinks. Keep
   snippets to one line calling a static method that already exists in the project; always
   pass `--tail` / `--limit` / `--level` to log reads; write bulk results to a file.
3. While debugging → ask for *"file + line number + cause only, do not reprint the code"*.
4. Long measurement results / reports → **write them to a file**, do not print them all to chat.

---

## 6. One Session = One Stage = One Branch

Do not work on two different stages in the same chat session. The context mixes and the AI
starts proposing UI changes while being asked about logic.

- Task done → close the session, open a new one. Cheaper than carrying old context
- Debugging a specific bug → give that bug its own session

---

## 7. What Must Survive Compaction

The post-compaction summary must retain:
- The path to `docs/_session/active.md` (re-read it to recover)
- The list of files edited this session and why
- Architectural decisions settled, with reasons
- Test results (pass/fail, specific errors)
- The current task and which step it is at
- Which document sections are written to file and which are unfinished
- Questions/blockers waiting on the developer

**After compaction:** re-read the state file and the files being edited. Files hold the
decisions; the conversation history is secondary.
