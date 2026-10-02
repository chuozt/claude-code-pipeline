# Auto Mode — `/map-systems auto` · `/design-system auto` · `/create-architecture auto`

The default is interactive: every skill asks, drafts, waits for approval. **Auto mode** runs the
same skill start to finish **without questions**, drafting from the designer's GDD. It saves the
developer's attention, **not the review**: the questions do not disappear, they are collected
into one list for the developer to settle afterwards.

Turned on only by the word `auto` in the command. Nothing enables it implicitly.

---

## 1. What typing `auto` authorises — and nothing more

`CLAUDE.md` §4 normally requires "May I write this to …?". In auto mode the developer's `auto`
argument **is** the approval, for exactly these paths and no others:

- `design/dev-system/**` (the index, system documents, the architecture, the files below)
- `design/registry/entities.yaml` — **append only**, never overwrite an existing entry
- `docs/_session/active.md`

Auto mode never edits `Assets/`, `ProjectSettings/`, `.claude/`, the designer's files
(`design/gdd/**`, the workbook), never commits, never pushes, never touches the Editor bridge.
The harness's own permission prompts are separate: to run unattended, start the session in a
permission mode that allows these writes; a skill cannot turn those prompts off.

## 2. Required inputs — the only questions auto mode may ask

Auto mode may ask **once, before it starts**, and only when a required input is missing:

| Skill | Required |
|---|---|
| `/map-systems auto` | the designer's workbook exported to `.xlsx` in the project **and** `tabs="Tab A, Tab B, …"` (the in-scope tabs); or no workbook and `gd-game-concept.md` (flat mode) |
| `/design-system auto` | `dev-map-systems.md` from `/map-systems` |
| `/create-architecture auto` | the system documents written by `/design-system` |

After that, no more questions until the final report.

## 3. The markers

Never invent a value. Use these, and **cite the source for every value you do write**
(`Mechanic Overview!r58`, or the workbook tab and row — whatever lets a person find it in
30 seconds):

| Marker | Meaning | Write |
|---|---|---|
| `UNDEFINED` | the source says nothing about it | `UNDEFINED` — and, if a person must decide, the question and its owner |
| `UNCLEAR` | the source says something, but it is ambiguous, incomplete or contradicts itself | `UNCLEAR` — **both** quotes with their sources, and what is unclear |
| `[INFERRED]` | derived by you from stated facts, not stated itself | the value, the facts it follows from, and their sources |
| `[VERIFIED: source]` | quoted exactly from the source | the value and the source (this is the normal case for a GDD value) |

`[PLACEHOLDER]` (a working value so code can run) is **not used** in auto mode: nobody is there
to own it. A value that is neither stated nor derivable is `UNDEFINED`.

If you are unsure whether something is `UNCLEAR` or `[VERIFIED]`, it is `UNCLEAR`.

**Never fill in**, whatever the pressure to produce a complete document: difficulty, balance,
prices, drop rates, colour palettes, win-rate targets, anything the designer owns
(`CLAUDE.md` §10). Those are `UNDEFINED`.

**Pictures.** Text export loses images. A tab that is mostly screenshots (UI layouts, level
pictures) cannot be read from the text: write `UNCLEAR — visual content not readable from the
text export` for what depends on it, and list the image-dependent parts. Do not describe what
you cannot see.

## 4. Every document says what it is

Top of every document written in auto mode:

```
> **Status**: Auto-drafted — NOT reviewed
> **Source**: [tab / file] · **Mode**: auto · **Date**: [today]
> **Markers**: [n] VERIFIED · [n] INFERRED · [n] UNCLEAR · [n] UNDEFINED
```

A document with more than **half of its sections** `UNDEFINED`/`UNCLEAR` is marked
`Insufficient source` in the Status line and not developed further; the run continues with the
next one and the report says so.

## 5. One list of open questions

Alongside the documents, maintain `design/dev-system/dev-open-questions-<date>.md`: every
`UNDEFINED`, `UNCLEAR` and each technical choice left undecided, one row each:

| # | Document · section | Marker | What is missing / unclear (quotes + sources) | Owner (designer / developer) |
|---|---|---|---|---|

Technical choices (data shape, interface, state layout) are **not decided** in auto mode: write
the two to four options with pros/cons as a row and leave it open.

## 6. Verify before anyone relies on it

After `/design-system auto` finishes, before anything is built on the documents:

1. **Mechanical check.** For each document, every number it quotes must appear in the text of
   its source tab (use the `gdd-sync` extraction). A number that does not appear is rewritten as
   `UNCLEAR` with a note. Write the result to `design/dev-system/dev-auto-verification-<date>.md`.
2. **Independent re-read.** Spawn a fresh, read-only subagent per ~5 documents with only the
   documents and the source tab text; it re-checks each `[VERIFIED]` value against the source and
   reports mismatches. A confirmed mismatch is rewritten as `UNCLEAR`, never silently fixed.
3. Then run `/consistency-check` and `/review-all-gdds` and add their findings to the open-questions
   list. In a fresh session, `/design-review` each important document.

The check catches wrong quotes. It cannot catch a wrong reading that quotes correctly, which is why
the sample review in the final report is not optional.

## 7. What each skill decides on its own, and what it never does

| | Decides alone | Never decides |
|---|---|---|
| `/map-systems auto` | folder names (rule in `/map-systems` Phase 0), the list of main systems from the tabs' top-level headings, the dependency order from references the tabs state | priorities beyond what the tabs state (`UNDEFINED`), hidden "implicit" systems (not inferred) |
| `/design-system auto` | the content of each section, quoted from the source with citations, in the queue order | any number or rule the source lacks; technical choices (listed as options); Approved status |
| `/create-architecture auto` | **proposes** layers, module ownership, data flow, API boundaries, the ADR list — all marked `PROPOSED (auto)` | **signs off**. `Technical Sign-Off: PENDING — auto-proposed, requires developer sign-off`. The self-review it performs is advisory and is recorded as such, never as a sign-off |

Architecture is the dangerous one: it constrains every line of code afterwards. Its output in
auto mode is a proposal for the developer to accept, change or reject.

## 8. Resuming, chaining, stopping

- Session state in `docs/_session/active.md` records `Mode: auto`, the queue and each system's
  state. Running the same command with `auto` again in a fresh session **continues** where it
  stopped, without asking.
- **`chain`** (`/map-systems auto chain …`): after the final report, start the next command in
  auto mode — `/design-system auto chain`, then the verification (section 6), then
  `/create-architecture auto`. Chaining stops, and reports why, if the verification finds any
  mismatch it could not rewrite, if the workbook cannot be read, or if more than a third of the
  documents are `Insufficient source`.
- Context ≥70%: finish the system in progress, record state, report the exact command to resume.

## 9. The final report (always)

A table the developer reads first: documents written, per document the counts of
`VERIFIED / INFERRED / UNCLEAR / UNDEFINED`, documents marked `Insufficient source`, the open
questions file, the verification result, the **ten values most worth checking by hand** (highest
impact, lowest source quality), and the next commands to run. State plainly that nothing was
reviewed by a person.
