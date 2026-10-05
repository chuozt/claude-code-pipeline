---
name: deconstruct-game
description: A technique for deconstructing (tearing down) a game to extract design insight instead of just listing features. Use this skill whenever the user wants to analyze, deconstruct, teardown, review, or benchmark a game (especially mobile puzzle/casual games like Royal Match, Candy Crush, Toon Blast...), write a report or competitor analysis, asks "why did game X design it this way", wants to compare mechanics across games, or wants to learn from another game's design decisions — even if they never use the word "deconstruct". Also use when typing /deconstruct-game.
argument-hint: "[game name | path to your play log or notes]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Edit, AskUserQuestion, WebSearch, WebFetch
---

> **Kit note.** This skill studies *other people's* games. It reads and changes **none** of the
> project's pipeline files (`design/gdd/`, `design/dev-system/`, the code), so it is safe in
> `gd-mode` and for either role. Its source is the studio's own `DECONSTRUCT_GAME` document
> (names no author or sources; see *Confidence labels* and *References* below).
>
> **Output:** when a report is requested, write it to `design/research/deconstruct-<game>-<date>.md`
> (ask first, per `CLAUDE.md` §4). Reports are research, not GDDs: the 8-section rule of
> `rules/design-docs.md` does not apply to them. Nothing here changes a pipeline file by itself;
> a lesson the team adopts goes through the normal skills (`/brainstorm`, `/design-system`,
> `/quick-design`). Related: `/gd-systems-interaction` and `/gd-balance-analyst` analyse *our*
> game; this one analyses someone else's.

# DECONSTRUCT GAME — How to Tear Down Someone Else's Game

## Core idea

> Deconstruction is **not** describing what a game has. Deconstruction explains **why** the game was designed this way, what it **trades off**, and what the team can **learn** from that decision.

Every output must build toward a **thesis** in this form:

> **"Game X trades [A] for [B], which shows they are aiming at [C]."**

If you cannot yet write this sentence, you do not understand the game deeply enough — go back to playing and analyzing.

---

## Confidence labels (read before using)

Not every part of this skill is equally solid. Each framework or claim carries a label:

| Label | Meaning |
|---|---|
| `[Academic]` | Has a clear academic source or named author (see *References* at the end of this file) |
| `[Practice]` | Process or experience from the source document or industry practice; **no verified citation**. Treat as a reasonable way of working, not an industry standard |
| `[Hypothesis]` | Must be verified in each specific game; never apply as a rule |

When writing a report, Claude **must** also tag each statement as **[Observation]** (directly seen, backed by numbers/screenshots) or **[Inference]** (a hypothesis about the design reason). Never present inference as fact.

---

## General workflow

```
PLAY → OBSERVE → FIND DEVIATION → ASK WHY → FIND TRADE-OFF
     → CONNECT SYSTEMS → FORM THESIS → EXTRACT LESSON
```

Short version to remember:

```
Observation → Why → Trade-off → Strategy → Lesson
```

Work through the 6 sections below in order.

---

## 1. Deconstruct ≠ listing features

| Weak report | Strong report |
|---|---|
| Blocker → Booster → Popup → Event → Economy → IAP… | Has a thesis, causal reasoning, and a conclusion for the team |
| Correct but produces no insight | Changes a team decision |

**Rule:** Never let a report item stop at "the game has X". Always continue: *"X is likely used to…"*

Example:
- ❌ "The game has a lives system."
- ✅ "The lives system is likely used to control session pacing, create a monetization touchpoint when the player loses, and give a reason to return."

---

## 2. MDA thinking — work backwards from feeling to mechanism `[Academic]`

MDA was developed by Hunicke, LeBlanc and Zubek in workshops at GDC 2001–2004 and published in 2004. Designers build a game in the direction **Mechanics → Dynamics → Aesthetics**.
When deconstructing, you stand on the player's side, so go in **reverse**:

```
AESTHETICS (feeling)  →  DYNAMICS (behavior)  →  MECHANICS (rules/systems)
```

**Example reasoning chain:**

```
"Addictive"
   ↓ What behavior shows it?
"Wanting to play one more round"
   ↓ What mechanism produces that behavior?
"The event is only 2 stars away from a reward"
```

This turns gut reactions ("good game", "gripping game") into design analysis.

**Do not use "fun" as a vague word.** Classify the kind of fun being produced. Marc LeBlanc's original list of 8 kinds of fun (`[Academic]`) is: sense-pleasure, make-believe, unfolding story, obstacle course, social framework, uncharted territory, soap box, mindless pastime; the short names in the table below are the commonly used labels. Note: LeBlanc himself says the 8 are **not meant to be exhaustive** — use them as suggestive labels, not an absolute taxonomy, and allow several kinds for one experience.

| Kind of fun | How to recognize it |
|---|---|
| Challenge | Overcoming obstacles, reaching mastery |
| Discovery | Exploring something new, mystery |
| Sensation | Sensory pleasure: effects, sound, juice |
| Fantasy | Role-play, imaginary world |
| Narrative | Story, drama |
| Fellowship | Connection, cooperation, community |
| Expression | Self-expression, customization, decoration |
| Submission | Relaxing pastime, playing on autopilot, "switching the brain off" |

---

## 3. Play protocol BEFORE analyzing `[Practice]`

> This protocol comes from the source document. Industry sources (e.g. the Adam Telfer interview) show that F2P companies regularly deconstruct competitor games but their **methods differ**; this is one reasonable approach, not a standard.

Four required principles:

1. **Play long enough to give yourself a chance to change your mind.**
   Don't write a report after the first session — at that point you are usually only judging Art / UI / Onboarding.

2. **Stop based on mastery, not hours.**
   Only start deconstructing when you are comfortable enough to explain the main systems.

3. **F2P first, payer later.**
   Play a few days as a non-payer to feel the real friction. Only then buy the starter pack / IAP to explore the economy and mid-game.

4. **Record data while you play.**
   Screenshot popups the first time they appear; note moves, lives, offer prices, timing… at the moment you encounter them.

> In mobile puzzle, many design decisions live in **timing + numbers + context**, not in isolated UI screenshots.

**Suggested play log (mobile puzzle):**

| Column | What to record |
|---|---|
| When | Level / day of play / which session |
| Event | Popup, offer, tutorial, booster unlock, new event… |
| Numbers | Moves, lives, price, % discount, countdown time… |
| Context | What the player just did (won / lost / ran out of lives / …) |
| Your reaction | Annoyed, excited, confused, wanting to keep playing… |

---

## 4. Analyze at 3 zoom levels: WHOLE → LOOPS → PARTS `[Practice]`

| Level | Question to answer |
|---|---|
| **Whole** | What experience is the game selling? To which player group? |
| **Loops** | What behavior does the player repeat per **session / day / week**? |
| **Parts** | Which specific mechanics create those loops? |

- A game is a system of loops → **use diagrams** to show loops; don't try to express everything in prose.
- **Analysis order for beginners:**

```
Core Gameplay / Core Loop → Retention → Monetization
```

- When digging deeper, check the **5 mechanic groups**:

| Group | Guiding questions |
|---|---|
| Physics | Rules governing the board/objects; input → result |
| Internal Economy | Which resources flow in and out? Where are the sinks and sources? |
| Progression | What leads the player upward? How do difficulty and novelty grow? |
| Tactical Maneuvering | What tactical decisions does the player make each turn/level? |
| Social Interaction | Is there interaction, competition, or cooperation with other players? |

---

## 5. Two core questions for EVERY notable design decision

**① WHY did they design it this way?**
Don't stop at observation; offer a reasoned hypothesis.

**② What is the TRADE-OFF of this decision?**
Every solution has a cost.

```
Gain → ?
Cost → ?
```

This is the step that turns **observation → analysis**.

---

## 6. Don't analyze everything — look for DEVIATIONS

If a mechanic is identical to every other game in the genre, its **information value is very low**. Prioritize where the game **does something differently from the norm**.

```
Genre convention
   ↓
Where does Game X do it differently?
   ↓
Why?
   ↓
What does it gain?
   ↓
What does it lose?
   ↓
What does this say about product strategy / target audience?
```

This filters hundreds of mechanics down to the few design decisions truly worth studying.

**Common hypotheses in mobile puzzle — `[Hypothesis]`: verify in each game, never apply as rules:**

| Topic | What is known | What is NOT established |
|---|---|---|
| Difficulty balance (flow) | Flow theory is used to tune difficulty curves to player ability; research on pacing shows a **constant** skill–difficulty balance throughout a game is **not optimal** | A specific curve shape (e.g. "sawtooth") has **no confirming source** |
| Near-miss | Some studies link near-misses to higher arousal/motivation | Another experiment found near-misses felt more boring and less exciting than winning by a large margin → **results are inconsistent** |
| Hard levels becoming "booster checks" | Appears in a few blog analyses | Low-reliability sources; verify with your own play data |

**Tip:** Before playing, quickly list the "genre conventions" (e.g. goal types, lives system, booster types, monetization approach) to have a yardstick. Wherever the game departs from that yardstick = a candidate for deeper study.

---

## Complete worked example (Royal Match)

> ⚠️ This example comes from the source document to **illustrate the reasoning style**. The Why/Trade-off parts are the **author's interpretation, not verified** against data or external sources. For a real report, verify by playing and cross-checking.

| Step | Content |
|---|---|
| **Observation** | You almost always have to clear all obstacles to win. |
| **Why?** `[Inference]` | It may let the board state show progress toward victory at the same time, reducing the information the player must process. |
| **Benefit** | The board is extremely easy to read. |
| **Trade-off** `[Inference]` | Level designers lose some ability to create variation through different goal types. |
| **Strategic interpretation** | Prioritizes cognitive simplicity for a casual audience. |
| **Thesis** | *Royal Match trades goal diversity for board readability and lower cognitive friction.* |

This is the level of insight a deconstruction should aim for.

---

## 5-QUESTION CHECKLIST for each important mechanic

```
WHAT?       — What does it do?
WHY?        — Why might they have chosen this design?
TRADE-OFF?  — What is gained and what is lost?
SO WHAT?    — What does it say about the target player / product strategy?
LEARN?      — What should our team learn — or NOT learn — from it?
```

---

## Output template for a deconstruction report

When the user asks for a report, use this structure (shorten if the scope is small):

```markdown
# Deconstruction: [Game name]

## 0. TL;DR — Main thesis
"[Game X] trades [A] for [B], showing they are aiming at [C]."
(State which parts are observation and which are inference; give confidence.)

## 1. WHOLE
- Core experience being sold (use fun labels: Challenge/Discovery/Sensation/...)
- Assumed target player
- Play conditions: days played, mastery level, F2P/payer

## 2. LOOPS
- Diagram: loops within a session / day / week
- How the loops connect (core → retention → monetization)

## 3. DESIGN DECISIONS (only DEVIATIONS or important points)
### Decision 1: [Name]
- Observation `[Observation]` (with numbers/screenshots/timing)
- Genre convention vs. what this game does
- Why `[Inference]` (hypothesis; say how it could be verified)
- Gain / Cost
- Confidence: High / Medium / Low (and why: number of observations, cross-checked with external sources or not)
- So what (strategy / audience)
- Learn (what to do / not do / test)

### Decision 2: ...

## 4. SYSTEM CONNECTIONS
- How do the decisions above reinforce or contradict each other?

## 5. LESSONS & DECISIONS FOR THE TEAM
- How will the team decide differently after reading this?
- What to do / not do / test
```

---

## How Claude applies this skill when working with the user

1. **Establish context:** Has the user played the game? Do they have data, screenshots, notes? If not → point to the protocol in Section 3 first; don't analyze in a vacuum.
2. **Never invent data** about the game (numbers, prices, timing). Use only what the user provides or what can be verified; label the rest clearly as *hypothesis*.
3. **Force the thesis:** For each remark from the user, ask back "Why?" and "What does it cost?" until a thesis emerges.
4. **Filter by deviation:** Help the user drop generic items and keep the few valuable decisions.
5. **Always end with "Learn":** Turn insight into a decision for the team.
6. **Separate** *observation (fact)* from *inference (hypothesis)*. Use language like "likely" or "may" for inference, and tag `[Observation]` / `[Inference]`.
7. **Don't promote hypotheses to rules.** For items tagged `[Hypothesis]` (near-miss, difficulty curve shapes…), present them only as things to verify in the game being analyzed.
8. **When citing external sources** (market share, revenue, game figures), find and cite a specific source; if there is no reliable source, say plainly that it is unverified. Blogs or analyses with no identifiable author → low reliability.

---

## Common mistakes (Anti-patterns)

| Mistake | Symptom | Fix |
|---|---|---|
| Listing features | Report is a list of system names | Every system needs Why + Trade-off |
| Writing too early | Only discusses art/UI/onboarding | Keep playing until mastery |
| Playing only as a payer | Never felt F2P friction | Play F2P for a few days first |
| Relying on memory instead of notes | Can't recall exact moves/prices/timing | Log at the moment of encounter |
| Scattered analysis | Discusses everything, no focus | Look for deviations from the genre |
| Gut-feeling "good because…" | Uses vague words like fun/gripping/addictive | Go backwards through MDA, classify the kind of fun |
| Presenting inference as fact | No Observation/Inference labels, no way to verify | Add labels, state confidence |
| Conclusion without action | Can't answer "what will the team do differently?" | Add a Learn / Decision section for the team |

---

## Final check (Quality gate)

Before treating a report as finished, you must be able to answer:

> **"After reading this document, how will the team make decisions differently?"**

If there is no answer, the report is only **documentation**, not **deconstruction**.

---

## References

**Academic / primary sources**
- Hunicke, LeBlanc, Zubek (2004). *MDA: A Formal Approach to Game Design and Game Research.* https://users.cs.northwestern.edu/~hunicke/MDA.pdf
- Marc LeBlanc — *8 Kinds of Fun* list. https://www.8kindsoffun.com/algorithmancy/
- Baumann, Lürig, Engeser (2016). Study on pacing curves, flow and enjoyment. *Motivation and Emotion* 40:507–519. https://www.uni-trier.de/fileadmin/fb1/prof/PSY/PGA/unterlagen/BaumannL%C3%BCrigEngeser_Flow_MOEM_2016.pdf
- Finserås et al. (2021). *Near miss in a video game: an experimental study.* *Int. J. Mental Health and Addiction* 19:418–428. https://irep.ntu.ac.uk/id/eprint/36202
- Study of skill–challenge balance and flow in a mobile puzzle game (mentions near-miss). https://pmc.ncbi.nlm.nih.gov/articles/PMC8943660
- Experimental study of near-miss and intrinsic motivation. *Frontiers in Neuroscience* (2017), doi:10.3389/fnins.2017.00131
- Transforming Game Difficulty Curves (NSF PAR). https://par.nsf.gov/servlets/purl/10133937

**Industry practice (medium reliability)**
- Interview with Adam Telfer on how he deconstructs games. https://www.pocketgamer.biz/how-adam-telfer-deconstructs-games/

**Royal Match analyses (low–medium reliability; for reference/cross-checking only)**
- Analysis post on Medium. https://medium.com/@ekinmelissezer/game-analysis-for-royal-match-and-toon-blast-9c4bff8ef48b
- Naavik — Royal Match deconstruction (only the description has been viewed; **content not yet read** — read it before using as a source). https://naavik.co/category/deep-dives/page/10/

**Note:** The source document (DECONSTRUCT_GAME.docx) names no author or sources; the `[Practice]` parts originate from that document.
