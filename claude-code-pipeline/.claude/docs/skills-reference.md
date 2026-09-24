# Skills Reference

Every skill this kit ships. Generated from the `SKILL.md` frontmatter — if a command is
not listed here, it is not in this package.

Run `/gd-workflow-help` for the pipeline with expected outputs and a "you are here" marker.

---

## The pipeline — 1A / 1B / 2 / 3 / 4

### Phase 1A — idea to architecture *(developer track)*

Assumes a game concept already exists. `/start` and `/brainstorm` are still available as
standalone commands but are not tracked steps of this sequence.

| Command | Purpose |
|---|---|
| `/map-systems` | Decompose a game concept into individual systems, map dependencies, prioritize design order, and create the… |
| `/design-system` | Guided, section-by-section GDD authoring for every undesigned system in the index, back-to-back in one run (or a single named system) |
| `/create-architecture` | Guided, section-by-section authoring of the master architecture document for the game |

### Phase 1B — define difficulty *(game designer track)*

⭐ **Runs in parallel with Phase 1A — neither reads the other's output.**

| Command | Purpose |
|---|---|
| `/gd-mode` | Switch to conversing the way a game designer thinks — discussing player experience, not implementation |
| `/gd-map-flow` | Map a puzzle game onto the INPUT/MID/OUTPUT model of the resource-flow frame — the mandatory gateway before… |
| `/gd-core-difficulty` | Interview the designer to define what makes the core gameplay hard — rank the 5 factors, settle the visibility… |
| `/gd-mechanic-difficulty` | Interview the designer to define where one mechanic creates difficulty — how it changes the legal move set,… |
| `/gd-mechanic-object-mix` | Decide which mechanics one OBJECT can carry at the same time — checking self-referential deadlock, attachme… |
| `/gd-mechanic-mix` | Derive the LEVEL-scale mechanic combination matrix from loading factors — which pairs are forbidden, which are… |
| `/gd-level-definition` | Interview the designer to define HOW levels are generated — what is human-owned identity, what is a free va… |
| `/gd-level-intent` | Read the designer's sheet/docx describing per-level intent — pacing, difficulty curve (easy start, hard mid… |

### Phase 2 — teach a bot (optional)

Needs both Phase 1A (a Model/View split to simulate) and Phase 1B (a difficulty model to
simulate against) finished first.

| Command | Purpose |
|---|---|
| `/gd-bot-playstyle` | Interview the designer to turn 'what playing this game well means' into three machine-executable playstyle… |
| `/gd-prototype-sim` | Build the game's first prototype as a pure-C# simulation a bot can play, with no graphics — from the existi… |

### Phase 3 — generate and measure

| Command | Purpose |
|---|---|
| `/gd-level-gen` | Generate levels from the defined formula — produce candidates, filter in two passes (static DSL, then bots)… |
| `/gd-level-audit` | Measure the difficulty and colour pairing of an entire level set with headless bots, never by eye |

### Phase 4 — calibrate

| Command | Purpose |
|---|---|
| `/gd-calibrate` | Close the difficulty-model calibration loop — designer hand-scoring before release, or comparing bot predic… |

---

## Everything else

| Command | Purpose |
|---|---|
| `/architecture-decision` | Creates an Architecture Decision Record (ADR) documenting a significant technical decision, its context, al… |
| `/architecture-review` | Validates completeness and consistency of the project architecture against all GDDs |
| `/art-bible` | Guided, section-by-section Art Bible authoring |
| `/commit` | Commit the current changes in this Unity repo, including a review pass for unexpected changes and a commit … |
| `/consistency-check` | Scan all GDDs against the entity registry to detect cross-document inconsistencies: same entity with differ… |
| `/create-control-manifest` | After architecture is complete, produces a flat actionable rules sheet for programmers — what you must do, … |
| `/dev-brief` | Switch replies to short, dense output for a developer — every point kept, fewer words, sharper word choice |
| `/design-review` | Reviews a game design document for completeness, internal consistency, implementability, and adherence to p… |
| `/gd-bug` | The mandatory 4-step Unity debugging procedure - collect evidence, narrow down, hypothesise, fix and re-verify |
| `/gd-feel` | Add juice and game feel to gameplay that already works correctly - screen shake, tweens, particles, layered… |
| `/gd-mechanic-doc` | Generate a mechanic design document skeleton with all 8 required sections, plus prompting questions for the… |
| `/gd-perf` | Find and rank performance bottlenecks in a Unity mobile project - GC allocation, draw calls, hot paths, lea… |
| `/gd-review` | Review Unity C# code before a commit or merge against the project's coding convention and anti-patterns |
| `/gd-workflow-help` | Show the game designer's end-to-end workflow — every phase, every command in order, with one line each on w… |
| `/mcp-check` | Check whether Unity MCP is attached to the right project and whether the Editor is in a state where files c… |
| `/playtest-report` | Generates a structured playtest report template or analyzes existing playtest notes into a structured format |
| `/project-overview` | Read-only snapshot of a Unity project - detected stack, structure, pipeline progress. Asks nothing, never t… |
| `/project-stage-detect` | Automatically analyze project state, detect stage, identify gaps, and recommend next steps based on existin… |
| `/quick-design` | Lightweight design spec for small changes — tuning adjustments, minor mechanics, balance tweaks |
| `/reverse-document` | Generate design or architecture documents from existing implementation |
| `/review-all-gdds` | Holistic cross-GDD consistency and game design review |
| `/track-prompt-duration` | Enable/disable measuring how long each Claude turn takes, from the moment the developer sends a prompt unti… |
| `/use-mcp` | Controls Unity MCP access |
| `/ux-design` | Guided, section-by-section UX spec authoring for a screen, flow, or HUD |

---

## Imported (third-party, each carries a "Kit note" header)

| Command | Source | Purpose |
|---|---|---|
| `/lib-dotween` | everything-claude-unity (MIT) | DOTween sequences, easing, kill strategy |
| `/lib-unitask` | everything-claude-unity | UniTask cancellation, PlayerLoop, WhenAll (Addressables section removed) |
| `/lib-vcontainer` | everything-claude-unity | VContainer registration into the project's existing scopes |
| `/lib-textmeshpro` | everything-claude-unity | TMP zero-alloc `SetText`, material presets, rich text |
| `/genre-puzzle` | everything-claude-unity | Mobile puzzle patterns: undo, level packs, hints, drag |
| `/test-designing-guide` | nowsprinting/unity-coding-skills (Unlicense) | Test-case design method; Model layer tests in EditMode here |
| `/unity-yaml-reading-guide` | nowsprinting/unity-coding-skills | Read-only reference for `.asset`/`.mat` YAML |
| `/gd-balance-analyst` | claude-game-design-suite (MIT) | Dominant strategies, dead options, balance levers |
| `/gd-playtest-protocol` | claude-game-design-suite | Design a playtest session (complements `/playtest-report`) |
| `/gd-systems-interaction` | claude-game-design-suite | Feedback loops and degenerate strategies |
| `/prompt-master` | nidhinjs/prompt-master (MIT) | Write optimised prompts for other AI tools |

Vietnamese user guide with one example per skill, kit + plugin:
`Assets/_Project/Docs/HUONG-DAN-SKILL.md` (source) and `HUONG-DAN-SKILL.docx` next to it
(regenerate the docx from the md after editing).

---

## Not in this package

The upstream kit this was derived from also carried a story/sprint/QA production track
(`/create-epics`, `/create-stories`, `/dev-story`, `/gate-check`, `/sprint-plan`, `/adopt`,
`/story-done`, `/story-readiness`, `/qa-plan`, `/team-*`…). Those were removed because their
chain was incomplete and half the commands did not exist.

If a document in `.claude/docs/` still mentions one of them, treat the mention as historical.
