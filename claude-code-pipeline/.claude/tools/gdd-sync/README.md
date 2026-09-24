# GDD Sync — xlsx → design/gdd/*.md

The design source of truth is the designer's xlsx workbook (they own it and will keep
updating it). The `.md` files under `design/gdd/` are derived copies that serve the skills
(`/design-system`, `/create-architecture`, …). **Never hand-edit content that originates in
the xlsx** — edit the xlsx and sync downward.

> Project-specific values below (`<workbook path>`, the sheet map) are filled in by the
> developer, or by Claude the first time this is run and confirmed.

## When to sync

At the start of any design-related session, or when the developer says "the GDD changed":

```bash
md5sum "<workbook path>"
```

Compare against `Source Hash (md5)` in the header of `design/gdd/systems-index.md`.
Different → run the procedure below. Same → do nothing.

## Procedure (performed by the AI agent)

1. **Unzip and extract text** (no Python needed — this machine may not have it):

   ```bash
   WORK=$(mktemp -d)
   cp "<workbook path>" "$WORK/gdd.zip"
   mkdir "$WORK/x" && tar -xf "$WORK/gdd.zip" -C "$WORK/x"
   ```

2. **Build the shared strings** (one string per line, newlines inside a cell become `⏎`):

   ```bash
   perl -0777 -ne '
     while (/<si>(.*?)<\/si>/gs) {
       my $si=$1; my $s="";
       while ($si=~/<t[^>]*>(.*?)<\/t>/gs){ $s.=$1; }
       $s=~s/&amp;/&/g; $s=~s/&lt;/</g; $s=~s/&gt;/>/g; $s=~s/&quot;/"/g;
       $s=~s/&#10;/ ⏎ /g; $s=~s/[\r\n]+/ ⏎ /g; $s=~s/\s+/ /g;
       print "$s\n";
     }' "$WORK/x/xl/sharedStrings.xml" > ss.txt
   ```

3. **Extract each sheet** with `parse-sheet.sh` (run from the directory holding `ss.txt`):

   ```bash
   .claude/tools/gdd-sync/parse-sheet.sh "$WORK/x/xl/worksheets/sheetN.xml" > sheetN.txt
   ```

   Output format: `R<row>| <col>: <value> | <col>: <value> ...`

4. **Reconcile sheet ↔ .md file** using the "Source sheet" table in
   `design/gdd/systems-index.md`, diff against the previous extraction (if kept), and update
   the affected `.md` files. Every update still follows the collaboration protocol: present
   the diff, get approval, then write.

5. **Update the headers** — `Source Hash (md5)` + `Last Updated` in `systems-index.md` and in
   every `.md` file just changed.

## Sheet map (in workbook.xml order)

Fill this in per project. Example shape:

| # | Sheet | Feeds |
|---|-------|-------|
| 1 | `<sheet name>` | `<which .md files>` |
| 2 | `<hidden / obsolete>` | not used |

## Traps already hit (do not repeat them)

- `perl -0777` sets `$/ = undef` **for the BEGIN block too** — reading ss.txt requires
  `local $/ = "\n"` first (`parse-sheet.sh` already handles this).
- Self-closing cells `<c r="A5" s="28"/>` — the regex must branch on `/>` vs `>...</c>`, or
  the following cell's value gets assigned to the wrong column.
- Inline strings (`<is>`) can contain raw newlines in the XML — clean them the same way as
  shared strings.
- An ss.txt produced on Windows may carry CR characters — `tr -d '\r'` if in doubt.
