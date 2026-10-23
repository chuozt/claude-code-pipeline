---
name: exp-ui
description: Build a Unity uGUI screen or popup from the designer's mock so it matches the mock to within a pixel, holds up on every phone/tablet aspect ratio and safe area, and ships fully wired - button events, analytics, touch locking, sorting over dims, coin bar, banner space, UI sounds and haptics (including what pauses under a popup). Reads the designer's PSD (layers, text specs, effects), checks that every font exists in the project, and solves TMP material presets for the text styles. Use when the developer types /exp-ui or says "ghép UI", "dựng popup theo preview", "build this screen from the mock", "UI lệch so với mock", "font lệch", "chữ lệch", "popup tràn màn hình", "popup bị to/nhỏ", "add sound to this popup", "nút bấm không ăn", "popup nằm dưới dim", "chừa chỗ banner", "phân tích file psd", "thiếu font", "tạo preset font", "scene loading", "scene transition", "chuyển scene bị lệch", "màn hình fullscreen", "dựng màn home/shop", "UIButton".
---

# /exp-ui — mock → exact, multi-screen, fully wired UI

Distilled from two mobile puzzle UI passes — a card-sort game (CardSort) and a sand-truck game
(SandTruck): ~30 popups, two gameplay HUDs and a Home screen rebuilt against the mocks, plus every
trap that cost a round of rework. Names in `code` are from those projects (SS.UI + Puzzle Base
`_Framework`). This skill is meant to travel: §1.0 describes the popup structure in full, so a new
project does not need either source project open to copy from. In another project,
map each name to its local equivalent in Step 0 before building anything.

**The one-line lesson:** measure, never eyeball. Every "looks right" that was not measured was
wrong by 5–20%. The worst error (a 12% oversized popup) survived a careful measuring pass because
the thing being measured had the wrong root scale.

**Accuracy target: ≤ 1 px at 1080×1920 on every edge, baseline and size.** 5 px is visible on a
phone — on a 1080-wide screen it is a whole outline width, and a text block 5 px low reads as "off".
Anything over 1 px must have a named reason (a deliberate designer offset, anti-aliasing of a soft
edge) written next to the number. The final gate is the overlay test in §5.2: art that exists in
both the mock and the build must look **sharp** when the two are blended, with no double edges.

---

## Step 0 — Survey before touching a prefab

Answer all of these in writing first. Each one is a trap that has already bitten.

| Question | Why it matters |
|---|---|
| Where are the mocks (`2D_Art/UI/Preview/*.png`) and the cut sprites (`2D_Art/UI/Export/`)? What resolution is the mock? Is there a design source (Figma/PSD) with text specs? | The mock is the spec. Sprites from Export are used at **native size** — the mock was drawn with them. A mock not 1080 wide needs a scale factor for every number (§5.1). Text specs from the design file beat measuring pixels (§1.5). |
| Which **canvas** will this UI render on? A framework popup canvas (`Core.Add` → GeneralManager `ScreenCanvas`) or a **scene** canvas (UI embedded in `Game.prefab`)? | They can have different CanvasScaler modes → different scale factors on the same phone. |
| What is that canvas's CanvasScaler mode, reference resolution, match value — **and does any framework code rewrite it at runtime**? | SS.UI `SceneManager.SetupCanvasScaler` sets `match = w/h > 0.44 ? 1 : 0` on every scaler. The serialized value is not what ships. |
| Canvas sort order of: gameplay canvas, popup canvas, tutorial dim, toast, loading, cheat UI? | Write the sort map down (CardSort: Game 98, popups 99, tutorial dim 100, tutorial toast 101, NoInternet 200, loading 515, coin flight 1000, cheats 9999). |
| Who draws the dim behind a popup — the framework shield or the prefab? | If both do, the dim doubles (CardSort: 215/255 shield + prefab `BG` → ~97% black). |
| How does gameplay read input — EventSystem or raw pointer? | Raw-pointer gameplay **cannot see UI**: a tap on a popup also taps the board underneath. |
| Audio entry points: SFX key constants, sound list asset, `PlaySfx` API, mixer params, haptic API (one-shot and loop), analytics API (`button_click`, `popup_show`)? | Every new screen needs all four channels wired the same way as its siblings. |
| Does the game show a **banner ad**? On which screens, from which level, top or bottom, removable by an IAP? | Decides the banner policy in §0.1-B for every full screen. |
| Which popups **spend coin**? | Each one gets the coin-bar decision in §0.1-A. |
| Which gameplay sounds can be **playing** when a popup opens, and which **haptic loops** exist? | Inputs for §0.1-C and §0.1-D. |
| Localisation system? TMP font assets, their weights, material presets, known metric quirks? | Fonts are the part that drifts most (§1.5). CardSort had no localisation — strings in prefabs. Note it as debt; don't invent a system unasked. |
| Shared UI prefabs (coin bar, buttons) — do they carry their own sprites, or does every instance override them? | CardSort `CoinUI.prefab` has **no sprites**; every instance overrides them or draws white boxes. |
| Which **button class** does the project use (CardSort `UIButton`)? Is the new screen a popup or a full screen? | Every tappable is built on that class (§3); a full screen triggers the questions in §0.1-E. |
| Is there a **similar project** on this machine (same framework, same studio) that already has this kind of screen? | Optional reference. **Ask the developer for permission and the path before reading another project**; read-only, never copy its files across without saying so. §1.0 is written so this is not required. |

Then sketch the plan (hierarchy, which sprites, which canvas, which events/sounds) and get the
developer's approval before writing.

## Step 0.1 — Decisions to confirm with the developer (never decide them alone)

Ask these as explicit questions, with the recommendation and the trade-off. Record the answers in
the feature doc so the next screen follows the same rule.

### A. Coin bar on popups that spend coin

For every popup with a coin price (buy booster, add slot, revive, unlock…):
1. Look at the preview: does it show the coin bar / balance (usually top of screen, highlighted
   above the dim)?
2. **Shown** → build it exactly as drawn.
3. **Not shown** → ask: *"Popup X spends coin but its preview has no coin bar. Add one so the
   player sees the balance before and after paying?"* Recommend yes for any popup that can answer
   "Not enough coins!" — a player told they are short should see their balance.
4. If added: reuse the shared coin-bar prefab with the same overrides/position as an existing
   popup (CardSort: Win's `CoinUI` — `base_coin_bar`, `icon_Coin`, "+" hidden, anchoredPosition
   (-27.7, -80.5)), inside the popup's `Content` so it sits above the dim and respects the safe
   area; RaycastTarget off unless "+" opens a shop; it must update on the purchase (resource event
   or a count-down tween), not only on the next open.

### B. Banner policy for full screens (Home, Gameplay, Level select…)

First establish whether the game has a banner at all (SDK config, remote-config keys such as a
"banner from level N" gate, a Remove Ads IAP). If it does, ask **per screen**:

| Policy | Behaviour | Pros | Cons |
|---|---|---|---|
| **Lift on show** | UI sits on the bottom edge; when the banner appears the bottom HUD rises by exactly the overlap (§2 Banner) | full screen used when no banner (early levels, no-ads buyers) | layout moves when the banner loads mid-session → needs a deferred, idempotent relayout; more code paths to test |
| **Reserve always** | a fixed banner-height strip is kept empty at the bottom from the start | layout never jumps; simplest | wasted space when no banner / after Remove Ads; strip must be filled (bleed colour), not show the 3D backdrop |

CardSort Gameplay uses **lift on show**. Whichever is chosen, the reserved/lifted height comes from the
real banner height in pixels (`safeArea.yMin + bannerDp × density`), never a constant, and a
mock never shows the banner — compare with it hidden.

### C. Gameplay sounds while a popup is open

Ask: *"When popup X opens, should gameplay sounds that are already playing pause? When it closes,
should they resume, or be dropped?"* Then **audit** before implementing:

1. List every SFX that can be playing at the moment a popup can open: clip length, loop flag,
   what triggers it, whether it is tied to an animation that `timeScale = 0` freezes.
2. Classify:
   | Kind | Risk | Default |
   |---|---|---|
   | short one-shot (< ~0.5 s: tap, pop) | none | leave it |
   | long one-shot (whoosh, ceremony, ice crack, coin cascade) | rings on under a frozen board; out of sync with its animation on resume | pause → resume |
   | **loop** (engine hum, continuous suck, timer tick) | **rings forever** if nothing stops it | pause → resume, and a safety stop on destroy |
   | music | — | ask: keep, duck, or pause |
3. Resume only if the popup returns to the same gameplay. A popup that leads to Restart / Home /
   Next level must **stop**, not resume (the emitters may already be destroyed or back in the pool).
4. Exclude the UI tap that opened the popup, and never pause the popup's own sounds.
5. Fullscreen ads are a separate path (the SDK usually mutes/pauses the app); don't conflate.

Known failure modes of pause/resume with a pooled emitter (all hit in CardSort):
`timeScale = 0` does not stop an `AudioSource`; `UnPause` on an inactive (pooled) object throws;
an emitter waiting for `!isPlaying` returns itself to the pool the moment it is paused unless that
wait is stopped first; a Restart destroys emitters → null-check on resume.

### D. Haptics while a popup is open

One-shot haptics (tap, win) need nothing. **Continuous haptics** (a vibration loop for a
ceremony/booster) do. Ask: *"Should a vibration loop hold while popup X is open and resume when it
closes?"* Recommended (CardSort rule, `haptics.md` §5): keep the **request** separate from the **motor**
— the loop plays only while no UI screen is up (`Core.IsNoMoreScreen()`) **and** the app has focus
(`OnApplicationPause` and `OnApplicationFocus`); when either fails it is held, and it resumes by
itself if the request still stands. A ceremony that ends while held just calls stop. Safety stops
in `finally` and every `OnDestroy` — Android loops the waveform natively and buzzes on forever if
nothing cancels it. Toasts and tutorial overlays do not count as UI unless the designer says so.

### E. Full-screen screens (Home, Shop, Level map, Collection, full-screen Win…)

A mock that fills the whole screen does not say how it is *made* or how it *comes and goes*. Never
assume "it's a popup". Ask, with a recommendation for each:

| Question | Options (pick one, with the developer) | Consequences |
|---|---|---|
| **What kind of screen is it?** | (a) a screen on the popup stack over the current scene (`Core.Add`) · (b) its own **scene** (`Core.Load`, with the scene transition) · (c) a **page/tab** inside an existing screen (show/hide a panel) | (a) shares the popup canvas and lifecycle, keeps the scene under it alive; (b) frees the previous scene's memory, needs the transition (§2 "Loading ↔ transition"); (c) no load at all, but its state lives in the host screen |
| **How does it open and close?** | none · fade (CanvasGroup alpha) · slide in from a side/bottom · scale-in like a popup · scene transition (iris/curtain) · designer's own animation | durations and easing come from the designer; every tween `SetUpdate(true)` if anything freezes time; slides move by the **canvas rect width/height**, never a 1080/1920 constant |
| **How does it switch to/from its neighbours?** | replace (old one closes) · stack (old one hidden, `hideTopScreen`) · cross-fade / push-slide between tabs | decides whether the screen under it keeps rendering (3D camera, particles — turn off what is fully covered) and whether gameplay must freeze (§0.1-C/D) |
| **How does the player go back?** | X / back arrow / Android back / tab bar / swipe — and back *to where* | wire `IKeyBack` to the same handler as the on-screen back |
| **Dim / shield?** | usually none for full screens; still block input to what is under it | no framework shield → the root must catch raycasts |
| **Banner and safe area** | §0.1-B per screen; background full-bleed outside SafeArea, content inside | top/bottom bars are the elements most exposed to ratios — anchor them to the edges |
| **Kept or rebuilt?** | destroyed on close vs cached; restore scroll position / selected tab? | memory vs reopen speed |
| **Analytics name** | `popup_show` vs a screen-view event | ask which the tracking sheet expects |

Everything else in this skill applies unchanged: same pixel-exact recipe (§1.1), fonts (§1.5),
multi-screen rules (§2), events/buttons (§3), sounds (§4) and verification (§5).

## Step 0.2 — Read the PSD when there is one (specs beat measuring pixels)

A PSD gives exact numbers a PNG preview cannot: every layer's position and size, text strings,
**font PostScript name, size, colour per run, tracking, alignment**, and every effect (stroke size
and colour, drop-shadow angle/distance/spread/size, gradient and colour overlays). Use it as the
source of truth for numbers, and the Preview PNG as the source of truth for the final look.

Tool: `tools/psdtool/` next to this file (a small .NET 9 console app, read-only, no packages, no
Photoshop needed; parses an 800 MB PSD in under a second). Build once:

```bash
dotnet build -c Release .claude/skills/exp-ui/tools/psdtool   # from the project root
```
The binary lands in `%TEMP%/psdtool/bin/Release/net9.0/psdtool.dll` (kept out of the skill folder).
Run it as `dotnet <dll> <command> …`:

| Command | Output |
|---|---|
| `tree <psd> <out>` | `tree.txt` (whole layer tree: kind, visibility, opacity, blend, bounds, text runs, effects) + `composite.png` (the image as saved) |
| `spec <psd> <out> [--project <unity root>] [--group "<name>"]…` | `spec_<group>.md` per top-level group: element table with PSD x/y/w/h, centre as a Unity anchored position, bounds **with** effects, opacity/blend/clip, effects; a Text section with font, size, colour runs, alignment and — with `--project` — the TMP fontSize/colour/material values to use and the closest existing preset |
| `layers <psd> <out> (--group "<name>" \| idx…)` | PNG of each layer's own pixels |
| `fonts <psd> --project <root> [--group …] [--out dir] [--write-presets dir]` | `fonts.md`: font check + text styles → TMP presets (below) |
| `prefab <prefab> --project <root> --psd <psd> --group "<name>" [--out dir]` | `prefab_<name>.md`: every RectTransform resolved to canvas px (nested prefabs and variants expanded), not-native-size sprites, TMP settings; then prefab vs PSD: text by string (size, colour, font, multi-colour runs, auto-size, centre x), images by overlap of the sprite's **opaque ink** (transparent margins trimmed) with the layer's own or effect-extended bounds, Δ > 1 px flagged. `--canvas WxH` lays it out on another canvas size (AspectRatioFitter applied) |
| `pair <prefabA> <prefabB> --project <root> [--screens 1080x1920,…] [--matchA expand\|0..1] [--matchB …] [--ignore name,…] [--out dir]` | `pair_A_B.md`: two screens that must look identical (loading ↔ transition) laid out per screen ratio, in screen px, every Δ ≥ 0.5 px flagged — see §2 "Loading screen ↔ scene transition" |

Traps when reading a PSD:
- **Layer bounds exclude effects.** A text with a 3 px outside stroke and a 4 px shadow is 3 px wider
  each side and 7 px taller on screen than its bounds. `spec` prints both; exported sprites usually
  include the effects (and transparent margins), so compare ink to `visual`, not to the bounds.
- **Text bounds are glyph ink**, not a text box. Centre the TMP box on the ink horizontally; fix
  the baseline by render (TMP `Middle` centres line metrics).
- **Font size = `FontSize × transform scale`** — the text layer is usually scaled; the tool does
  this. At 72 ppi 1 pt = 1 px; at other ppi convert.
- **Drop-shadow "spread" is a % of size:** spread 100% + size 3 = a hard shadow grown by 3 px.
  Photoshop's shadow is cast by the stroked shape.
- **Hidden groups are still there.** One PSD often holds every popup with all but one hidden; the
  composite shows only what was visible at save, and effects are only rendered in the composite —
  `layers` exports raw pixels without effects. For a reference image of a hidden popup, ask the
  designer for its Preview PNG.
- The PSD and the Preview PNG can disagree (CardSort: the "Add Slot" title is centred on the board in
  the PSD but centred on the 15 px off-centre header plate in the built prefab). When they do, ask
  which one is right — don't pick silently.
- Read-only always: never write to the designer's PSD. Put generated reports in a folder the
  developer agrees to (CardSort used `docs/ui-spec/`).

---

## Step 1 — Build to the mock

### 1.0 Reference popup structure (portable)

Use this as the default for any popup. Deviate only when the mock forces it.

```
PopupX (root)                RectTransform stretch-full, scale 1, z 0
│                            CanvasGroup (alpha driven by the show/hide tween)
│                            PopupXController : BasePopup, IKeyBack[, IToastAnchor]
│                            [nested Canvas + GraphicRaycaster only if it must sort above a dim/tutorial]
├─ BG (optional, usually OFF) full-screen Image — only when the popup opens with shieldAlpha 0
└─ Content                   stretch-full, SafeArea (ConformX/Y) — the show/hide tween target
   ├─ CoinBar (optional §0.1-A) shared prefab instance, top-anchored, overrides set
   └─ MainBox                9-slice frame at native border sizes, RaycastTarget ON — the zones live in it (§1.0a)
      ├─ PanelTop
      │  ├─ ImageHeader      ribbon/title plate
      │  │  └─ TextTitle     TMP, auto-size OFF, Midline/Capline aligned, material preset
      │  └─ ButtonClose      UIButton, top-right, hit area ≥ 120×120 units (raycast padding)
      ├─ PanelMid
      │  ├─ PanelInner       inner 9-slice box (Base_Popup_M), sized per popup
      │  ├─ ImagePreview     Image native size (SetNativeSize after a sprite swap), raycast OFF
      │  └─ TextDescription  TMP, wrapping ON, fixed box, raycast OFF
      └─ PanelBottom
         ├─ ButtonBuy        UIButton (coin)  → IconCoin + TextPrice under a HorizontalLayoutGroup
         ├─ ButtonAds        UIButton (ads)   → IconAds + TextCaption; hidden → ButtonBuy recentres (layout group)
         └─ TextHint         (optional) hint under the buttons; a ToastAnchor goes here too
```

Rules that go with it: only `MainBox` and buttons catch raycasts; decorative children are raycast
off; nothing under `Content` has a non-1 scale or non-0 z unless the mock demands it; every
button's persistent `m_OnClick` list is empty.

**GameObject names carry their kind as a prefix** (developer; the prefabs cloned from the sister project
had `Box`, `Caption`, `Count`, `Plus`, `Reward` — nothing said what a node was). One prefix per
kind, then the role in PascalCase, no spaces, no underscores, no numbers for "the second one":

| Prefix | Kind | Examples |
|---|---|---|
| `Popup<Name>` / `Screen<Name>` | prefab root | `PopupBuyBooster`, `ScreenHome` |
| `BG` | the dim or the full-screen background | `BG` |
| `MainBox` | the popup's outer 9-slice frame — `GamePopup.content` | `MainBox` |
| `Panel<Role>` | the three zones, and every other 9-slice box or grouping rect | `PanelTop`, `PanelMid`, `PanelBottom`, `PanelInner`, `PanelButtons` |
| `Image<Role>` | a decorative or structural Image (plate, bar, preview, badge base) | `ImageHeader`, `ImagePreview`, `ImageTimerBase` |
| `Icon<Role>` | a small symbolic Image (coin, ad, heart, lock, gear) | `IconCoin`, `IconAds`, `IconHeart` |
| `Text<Role>` | every TMP label | `TextTitle`, `TextPrice`, `TextCaption`, `TextTimer` |
| `Button<Role>` | every tappable (the project's button class) | `ButtonClose`, `ButtonBuy`, `ButtonAds`, `ButtonClaim` |
| `VFX<Name>` / `Spine<Name>` | particles / skeletons | `VFXConfetti`, `SpineHeart` |

A button's children follow the same rule (`ButtonBuy/IconCoin`, `ButtonBuy/TextPrice`). A node
that only groups others is a `Panel`. Renaming is safe when no code does `transform.Find` by path
(SandTruck: none in popups — references are serialized fileIDs); build scripts that find nodes by path
must be updated with the names. New prefabs use these names from the first node.

Controller skeleton (adapt names to the project's framework):

```csharp
public class PopupXController : BasePopup, IKeyBack
{
    public const string NAME = "PopupX";                 // == Resources/Screens/PopupX.asset

    [SerializeField] private UIButton btnX;
    [SerializeField] private UIButton btnPrimary;
    [SerializeField] private TMP_Text txtPrice;

    private Action _onClosed;
    private bool _actionTaken;

    protected override void Awake()
    {
        base.Awake();
        btnX.TrackClick(ButtonName.ClosePopupX);          // analytics listener first
        btnPrimary.TrackClick(ButtonName.BuyPopupX);
        btnX.onClick.RemoveListener(OnClickX);
        btnX.onClick.AddListener(OnClickX);
        btnPrimary.onClick.RemoveListener(OnClickPrimary);
        btnPrimary.onClick.AddListener(OnClickPrimary);
    }

    public void Init(int price, Action onClosed)            // called from Core.Add's onScreenLoad
    {
        DataBucket.Instance.PopupShow(PopupName.PopupX);    // popup_show: first line
        _onClosed = onClosed;
        _actionTaken = false;
        txtPrice.text = price.ToString();
    }

    protected override void OnEnable()
    {
        base.OnEnable();                                    // show tween, IsLockedTouch = true
        Core.SetCloseOnTappingShield(true);                 // only if the design closes on outside tap
        GameplayFreeze.Enter();                             // timeScale + SFX pause (§0.1-C)
    }

    private void OnDisable()
    {
        Core.SetCloseOnTappingShield(false);
        GameplayFreeze.Exit();                              // resume — or stop if leaving gameplay
        var onClosed = _onClosed;                           // cleanup here: a shield tap skips buttons
        _onClosed = null;
        onClosed?.Invoke();
    }

    private void OnClickPrimary()
    {
        if (_actionTaken) return;
        _actionTaken = true;
        // pay / grant, then:
        Close();
    }

    private void OnClickX() { if (!_actionTaken) { _actionTaken = true; Close(); } }
    public void OnKeyBack() => OnClickX();
    private void Close() => Hide(() => Core.Close(this));
}
```

Lifecycle order to rely on: `Awake` → `OnEnable` (show tween starts) → `Init` (data) → `Start` →
… → `Hide` tween → `Core.Close` → `OnDisable` (cleanup, `onClosed` once) → destroy.
Open with `Core.Add<PopupXController>(PopupXController.NAME, hideTopScreen: false, onScreenLoad: p => p.Init(...))`.

Base popup behaviour to reproduce if the project has none: show = `Content` scale 0.8→1 OutBack
0.25 s + alpha 0→1 InOutSine, `SetUpdate(true)`, `interactable = true` only on complete; hide =
reverse with InBack, `IsLockedTouch = false` and the callback on complete.

### 1.0a Zones: Top / Mid / Bottom (SandTruck rule)

Every popup's frame is split into three **animation groups**. They are not layout containers:
each zone is an empty `RectTransform` stretched over the whole frame (anchors 0,0 → 1,1,
offsets 0, `anchoredPosition` 0, scale 1), so a child's position inside a zone is the same
number it would have directly under the frame. Moving an element between zones never changes
where it sits; it only changes *how it enters*.

```
PopupX (root)                 CanvasGroup, PopupXController : GamePopup
└─ Content                    SafeArea (stretch-full)
   └─ MainBox                 the 9-sliced frame (Base_Popup_L) — GamePopup.content: scales 0.9 → 1 on show
      ├─ PanelTop             GamePopup has no field for it: it never moves
      │  ├─ ImageHeader       header plate on the frame's top edge
      │  │  └─ TextTitle
      │  └─ ButtonClose       ✕ on the frame's corner
      ├─ PanelMid             GamePopup.midZone: starts 10 % of the frame height lower, rises OutBack
      │  ├─ ImageBox          inner light box (Base_Popup_M) sized per popup
      │  ├─ ImagePreview / IconX / SpineX
      │  └─ TextDescription
      └─ PanelBottom          GamePopup.bottomZone: same rise, 0.05 s after Mid
         ├─ ButtonPrimary     (coin)  → IconCoin + TextPrice
         ├─ ButtonSecondary   (ads)   → IconAds + TextCaption
         └─ TextHint          anything that belongs with the buttons (hints, tags, counters)
```

**What goes where** — decide by what the element is *attached to*, not by its y:
- **Top**: everything fixed to the frame's top edge — header plate, title, ✕. It does not slide,
  because it is the part the player reads first and the anchor the eye uses while the rest rises.
- **Mid**: the popup's own content — the inner box, art/preview/Spine, body text, counters inside
  the box. If it is unique to this popup, it is Mid.
- **Bottom**: the actions and whatever hangs on them — buttons, their price/tag badges, the hint
  under a button, "Hold to see the board". A badge that overlaps a button goes with the button.

**Show** (`GamePopup.Show`): `MainBox` scale 0.9 → 1 OutBack (0.25 s) + alpha 0 → 1; Mid and
Bottom are placed at `−frameHeight × zoneOffsetRatio` (0.1) and tween to 0 with `zoneEase`
(OutBack), Bottom with `bottomZoneDelay` (0.05 s). Hide is BasePopup's. A popup that reveals its
content later (Win after its opening) calls `SlideZonesIn()` again at that moment.

**Fitting the height** (`PopupHeightFitter`): the fitter measures the lowest element, and during
`Init` the zones are still offset, so call `FitHeight(fitter)` (GamePopup) — it zeroes both zones,
fits, and puts them back; measured with the zones offset the panel came out ~100 px too tall.

**Traps**: a zone with a non-zero `anchoredPosition` in the prefab is an offset that every show
adds to (the prefab must store 0 — `ResetZones` in `OnDisable` restores it); a Spine or VFX
parented under `MainBox` directly (outside the zones) stays still while the content rises, which
reads as a glitch — put it in the zone of the element it decorates; the ✕ in Top must keep its
`RaycastTarget` while Mid/Bottom are still moving, so the player can always close.

### 1.0b Reference full-screen structure

```
ScreenX (root)               RectTransform stretch-full, scale 1, CanvasGroup, ScreenXController
│                            root Image (alpha 0) or Background catches raycasts — nothing passes through
├─ Background                full-bleed, OUTSIDE SafeArea; AspectRatioFitter EnvelopeParent (or
│                            oversized + preserveAspect) so 9:22 and 3:4 are both covered, never stretched
└─ Content                   stretch-full + SafeArea — the open/close animation target
   ├─ TopBar                 anchored to the top edge (offset 0): back/X, title, currency bars;
   │                         its colour continues into the notch with a bleed child (§2)
   ├─ Body                   stretched between TopBar and BottomBar (anchors, not fixed heights);
   │                         a ScrollRect when the list can grow; tall phones get more rows, not bigger rows
   └─ BottomBar              anchored to the bottom edge; sits above the banner per §0.1-B;
                             tab bar / main CTA
```

Rules: fixed-size art (cards, icons, buttons) keeps native size and is laid out by anchors/layout
groups — the extra height of tall phones goes to the Body, never to scaling the art. Check the
first and last item of every list at 9:22 and 3:4. The open/close animation targets `Content` (or
the root for a fade), never the Background bleed, so the background never shows a seam mid-animation.

### 1.1 Pixel-exact recipe

1. **Root first.** Root `localScale = 1`, RectTransform stretched to the canvas. Template popups
   shipped with root scale **1.12**; the show tween only animates `Content`, so the 1.12 survived
   at runtime and every measurement was "right" relative to a popup 12% too big. Check this before
   any other number — and check `lossyScale` of every text and image you measure.
2. **Sprites at native size** (`Image.SetNativeSize()`, also after any runtime sprite swap), then
   position. Resizable bars/panels are **9-sliced** with real slice borders set in the Sprite
   Editor — never stretch a non-sliced sprite. Confirm *which* sprite the mock used (CardSort: the light
   inner panel is `Base_Popup_M`, not the similar-looking `VideoFrame`).
   **A 9-sliced box is sized per popup, never copied.** The inner box (`Base_Popup_M`), the header
   plate and every stretched bar take the height the *mock of that popup* shows — measure it there.
   SandTruck (developer pass): the template's `Mid/Box` height 409 had been pasted into every
   popup; the mocks needed 380 (Are You Sure), 402 (Buy Booster) and 348 (No Internet), and two of
   those boxes were still `Image.type Simple`, so they could not be resized without stretching the
   corners. Checklist for every sliced sprite: `Image.type = Sliced`, borders set, `fillCenter` on,
   width/height from this popup's mock, the content's padding inside it also from the mock (a
   `PopupHeightFitter` gets its padding from there, not the other way round). The same goes for
   anything placed *on* a sliced sprite per popup — title on its plate (SandTruck: y 15 / 17 / 18 / 19
   across four popups), a tag's text inside its badge, a text box's width where the mock wraps the
   line (No Internet: 460, not the template's 600): measure each popup, never inherit the number.
3. **Sprite import for sharpness:** UI sprites with mipmaps off, Max Size ≥ the sprite's real size
   (a 2200 px sprite under a 2048 cap is silently downscaled → blurry), compression that does not
   smear flat colours, consistent Pixels Per Unit. A blurry demo image in the overlay test (§5.2) is
   usually this or a non-1 scale. `[INFERRED: general Unity import behaviour, not measured in CardSort]`
4. **Anchors:** pin edge UI with anchor = the edge and offset 0. Never combine an anchor fraction
   with a pixel offset (`anchor y 0.03` + `pos -58` only cancels out on a 1920-tall canvas). Rows
   that must sit on a sprite feature (a highlight line) anchor to the 9-slice border, not to a
   hand-picked offset, so they stay put when the bar resizes.
5. **Positions are whole numbers or measured halves.** A position typed as 312.37 because it
   "looked right" is a guess; derive every number from the mock measurement.
6. **Dim:** use the framework shield. Disable any full-screen `BG` Image inside the prefab unless
   the popup is opened with `shieldAlpha: 0`. Once the BG is gone, the visible frame (`Board`,
   `Header`) must have **RaycastTarget on**, or taps inside the popup fall through to the
   tap-to-close shield.
7. **RaycastTarget off** on every decorative Image/TMP (icons, glows, bleed strips, labels on
   buttons). Only buttons and the frame catch raycasts.
8. **After any reparent, zero `localPosition.z`.** Objects moved in world space carried
   `z = -10123`; `anchoredPosition` never touches z and an orthographic render hides it, but a
   perspective camera shows them flying away. Exception: VFX deliberately pushed in front of the
   UI (CardSort `Lightray` z = -400).
9. **Copying Images under a SoftMask:** check `maskable = true` on each. Copied Images carried
   `m_Maskable: 0`, so the mask silently did nothing — no error, no warning.
10. **Shared prefab instances:** copy the sprite/position overrides from a known-good sibling.
11. **Spine in UI:** `SkeletonGraphic` with `startingAnimation` **empty** (otherwise it plays while
    the panel is still at scale 0). Start it from the scale-in tween's `OnStart`:
    `Skeleton.SetToSetupPose(); AnimationState.SetAnimation(0, name, false);`, unscaled time. The
    skeleton origin is not the art's centre — offset the RectTransform by measurement.
12. **Mocks are not symmetric by accident.** CardSort: the header sits 15 px left of board centre when
    there is an X button. Measure; don't "fix" the designer's offsets.

### 1.5 Fonts — the part that drifts most

Text was the most frequent source of "close but wrong" in CardSort, and it hides well: a font 4% too
small or 3 px low looks fine alone and wrong next to the mock. Do every step.

0. **Font check first (blocking).** List every font the design uses (`psdtool fonts`, or ask the
   designer when there is no PSD) and, for each PostScript name, confirm the project has:
   (a) the font **file** with that exact PostScript name (weight included), (b) a **TMP font
   asset** made from it, (c) every glyph the texts use — in the file's cmap, and in the atlas if
   it is Static. **A font missing from the project is reported to the developer and the work
   stops for that text** — ask for the exact file the designer used (same family *and* weight).
   Never substitute a look-alike, never download a font yourself, never fake a weight with
   Faux Bold / Face Dilate. When it arrives, the TMP asset is made in Unity (Font Asset Creator, or
   `TMP_FontAsset.CreateFontAsset` via MCP when enabled): SDF render mode, sampling point size ≈
   the largest size used, **padding large enough for the biggest stroke + shadow** (the `fonts`
   report says when a style does not fit the current padding), character set = the texts used
   (+ digits) for a Static atlas.

0b. **Text styles → TMP material presets.** In TMP the stroke and shadow live in the *material*,
   and a material's values are **relative to font size**: the same 3 px stroke needs different
   values at 60 px and at 110 px. The fill colour is the component's vertex colour, not part of the
   preset. `psdtool fonts` groups the design's text styles (font + stroke + shadow, size bands
   within 0.5 px), solves the TMP values for each from the TMP source code
   (`TMP_ShaderUtilities.UpdateShaderRatios`/`GetPadding`, `TMP_SDF.shader`):
   - Photoshop **outside** stroke S px at font size P with atlas sampling Ps and padding p:
     `r = P/Ps`, `_OutlineWidth = _FaceDilate = S / (2 · ratioA · (p+1) · r)` (outline is centred on
     the edge, so dilate pushes the face out by half the stroke and the outline covers the rest).
   - Drop shadow → Underlay: offset/dilate/softness share one budget of `(p − (weight+dilate)·p)·r` px
     (`weight = _WeightBold/4`, which counts even for regular text). Past it there is no solution;
     the tool clamps and says so, and tries `_WeightBold 0` first to free the budget.
   - It then scores every existing preset on that font's atlas (MATCH / CLOSE / DIFFERENT, with
     the px each one actually renders) and, with `--write-presets <dir>`, writes a cloned `.mat` per
     missing style into a **staging folder outside Assets** (line endings kept, no `.meta` — Unity
     makes it on import). Copy into the project only after the developer agrees.
   - These values are **derived, not render-verified** `[INFERRED]` — confirm each new preset with
     the overlay test (§5.2) before calling it done.
   What the tool cannot do: create the font file or the SDF atlas (Unity does that), express text
   gradient/bevel/inner-shadow/double strokes in one preset (vertex colour gradient, a second text
   layer, or a sprite — ask).

0c. **Presets: as few as the colours, where the project keeps them.** Two SandTruck mistakes
   (developer) to never repeat:
   - *Too many.* A preset is **one stroke colour on one font**, never "one per use". Before
     creating one, list every preset of that font with its `_OutlineColor` / `_UnderlayColor`
     (a one-line script over the `.mat` files) and reuse any within ±12 per channel — the mock's
     navy on a title, a body text and a HUD pill is the same navy. SandTruck ended with three Lilita
     navies (`E89 Body` 24,43,147 · `E89 Hud Blue` 12,60,116 · `UL DarkBlue` 35,46,106) and three
     reds where one of each was meant. Name by font and colour (`LILITAONE-REGULAR E89 Navy`),
     not by where it is used (`… Hud Blue`, `… Btn Green`); the width/dilate is the family's one
     value (item 15), so a clone differs from its source by colour only — check that before
     saving it. A project already carrying dozens of unused presets (SandTruck: 40 of 61) is a sign to
     reuse, not to add.
   - *Wrong folder.* Presets live **next to their font asset**, in the folder the developer
     already uses (SandTruck: `Assets/_Project/Fonts/<Family>/`, beside `<Family> SDF.asset`) — look
     where the existing ones are and put new ones there. `Resources/Fonts & Materials/` is not a
     preset folder: TMP only needs it for the rich-text `<material="name">` tag (it
     `Resources.Load`s from there) and nothing else. Avoid that tag when a second label or a
     vertex-colour run (`<color>`) does the job; if a tag is unavoidable, exactly that one preset
     goes to `Resources/Fonts & Materials/`, with a note on it saying why.
1. **Get the spec, don't guess it.** Ask for the design source's text properties per label:
   family, **weight**, size, line height, letter spacing, colour/gradient, stroke width + colour,
   shadow offset/blur/colour, case. Only fall back to measuring pixels when there is no source.
2. **Right asset, right weight.** Bold vs ExtraBold vs Black of the same family look alike at a
   glance and differ by several % in width and stroke — compare a wide glyph (`W`, `M`, `0`)
   zoomed side by side. The SDF asset must contain every glyph used (digits, `+ × %`, currency,
   accented letters if localised); a missing glyph renders as a box or falls back to another font.
3. **Units.** Design px = canvas units only when the mock is 1080 wide (otherwise scale). Photoshop
   *pt* is not px. TMP `fontSize` is the em size, same as the design tool's size for the same
   font — if it does not match, suspect weight, a non-1 `lossyScale`, or auto-size, not "TMP renders
   smaller".
4. **Auto-size OFF while matching.** TMP auto-size silently clamps to the box height; a size change
   that "does nothing" means the box is too short. Set the exact size, then give single-line boxes
   ≥ 1.8× the font size and turn wrapping off. Re-enable auto-size afterwards only where text
   length really varies, with Min/Max around the matched size.
5. **Vertical alignment is the classic offset.** `Middle` centres the font's **line metrics**
   (ascender/descender), not the visible glyphs, so caps and digits sit off-centre (CardSort: Baloo2
   digits 2.3 px above box centre). Use `Midline`/`Capline` for caps-and-digits labels, or apply a
   measured offset per font — once per font, written down.
6. **Outline and shadow are SDF units, not px.** TMP outline is centred on the glyph edge — half
   of it eats into the face, so a thick outline makes the letter look thinner and smaller than the
   mock's outside stroke. Compensate with Face Dilate, then measure the outer edge of the stroke
   against the mock. Drop shadows are Underlay offset/softness, also measured, not typed from the
   design tool's px. The atlas **padding** must cover the widest outline/underlay used, or the
   stroke clips into squares.
7. **Never edit the shared default material.** Each style (white + brown outline, yellow + shadow…)
   is its own Material Preset; editing the font's default material restyles every label in the game.
8. **Width last.** Match cap height first (size), baseline second (position/alignment), string
   width third (character spacing / word spacing), then stroke and shadow. Changing size after
   spacing undoes it.
9. **Measure with the same string** on both sides — temporarily type the mock's exact wording,
   including case and punctuation. Different strings make width meaningless and height depend on
   descenders. Restore the real text binding afterwards.
10. **Numbers change width.** Proportional digits make "1111" narrower than "8888": price/icon pairs
    go under a HorizontalLayoutGroup + ContentSizeFitter so 3- and 4-digit values stay centred and
    never run into the icon. Check the widest realistic value.
11. **Dynamic font assets dirty the repo.** A Dynamic atlas adds glyphs at runtime and rewrites the
    `.asset` (CardSort's working tree routinely shows modified `* SDF.asset` files). Prefer a Static
    atlas with the needed character set for shipped fonts, or agree with the team how that churn is
    committed. `[INFERRED: observed git status + TMP behaviour]`
12. **Text is where the overlay test (§5.2) shows misalignment first** — double-stroked letters
    mean an offset, a "bold halo" means a size or dilate mismatch.
13. **A label on art that comes in variants gets a preset per variant.** When a plate, pill or bar
    changes colour with a state (difficulty Normal / Hard / Super Hard, theme, rarity, on/off), the
    designer recolours the text's outline and shadow to match the plate — the mock for each variant
    shows it, the default variant's preset hides it. SandTruck: the "Level N" pill swapped its
    sprite per difficulty while its text kept the navy outline on the red and purple pills. So: for
    every variant mock, sample the outline/underlay colour of every label on the recoloured art,
    clone the base preset per colour (`<Font> E89 Hud Blue / Red / Purple`), and give the controller
    a `Material[]` indexed like its `Sprite[]`, applied in the same refresh that swaps the sprite.
    Labels on art that does not change (the green x2 button) keep one preset.
14. **Fill colour is per label, read from the glyph interior, then snapped.** The preset carries
    outline and shadow only; the vertex colour comes from each label's mock. Take the mode of the
    glyph interior, never one pixel, and snap: near-white → `#FFFFFF`, anything close to a palette
    colour → that colour. SandTruck: titles came out (250,253,249) from an anti-aliased sample and were
    fixed to white; body texts kept the template's brown where the mock had white (Are You Sure,
    No More Space, "LOST 1 life"), and three "Next life" / Settings labels needed (144,72,48), not
    the template's (149,72,54) or (139,88,76).
15. **One stroke width per family.** Solved widths that spread across presets of the same font
    (SandTruck: dilate 0.19–0.2, outline 0.3–0.32) are measurement noise, not design: the designer uses
    one stroke. Pick one value (ask), then clone presets per *colour* only. SandTruck settled on
    `_FaceDilate 0.21` / `_OutlineWidth 0.21` for every Lilita E89 preset and 0.21 for Mikado.
16. **Measured sizes run large; round down to the designer's number.** The white-face bbox includes
    the dilated face and anti-aliasing, so the solved size came out 2–20 % over the real one (88 vs
    80, 160 vs 130, 59 vs 55, 56 vs 50, 82 vs 80, 45 vs 40, 34 vs 32). Designers type round sizes:
    after matching, try the nearest multiple of 5 below and overlay-verify; prefer it when the
    overlay is equal. Text case follows the mock exactly ("MAX", not "Max"); a trailing period too.

---

## Step 2 — Multi-screen: make it hold on every ratio

### Canvas scaler — the decision

**Use `Expand`, reference 1080×1920, on every canvas the player sees**, popup and scene canvases
alike. `scale = min(w/1080, h/1920)`: the whole 1080×1920 design rect is always visible, tall
phones get extra height, tablets get extra width. Never overflows.

Why not MatchWidthOrHeight: with match = height on a 1080×2400 phone the canvas is only 864 units
wide, so a 903-wide popup spilled off both edges and its X was cut off (CardSort).
Then check:
- No framework code rewrites `matchWidthOrHeight` / `screenMatchMode` at runtime on the canvases
  you care about (the SS.UI rewrite is harmless only because Expand ignores `match`).
- A prefab can hold **two** CanvasScalers — don't trust the first `match` value you grep.
- Per-popup editing scenes (`Screens/<Name>/<Name>.unity`) never reach a build; their scaler
  settings are irrelevant. The runtime parent canvas decides.
- Switching a canvas to Expand changes the HUD size on 18:9–20:9 (−11–20%) — re-check anything
  tuned against the old size.

### Ratios to test (minimum set)

| Ratio | Resolution | Represents |
|---|---|---|
| 9:16 | 1080×1920 | the mock — must be ≤ 1 px |
| 9:19.5 | 1080×2340 | iPhone X-class, most modern Androids |
| 9:20 | 1080×2400 | common Android |
| 9:21–9:22 | 1080×2520 / 1080×2640 | tallest phones |
| 3:4 | 1536×2048 | iPad |
| 4:5 | 1600×2000 | wide tablets / foldables |
| low-end | 720×1280 | reference low-end device |

### Safe area, notch, status bar

- `SafeArea` component (Crystal) on `Content` of every screen and popup, ConformX/Y on. It re-applies
  on safe-area / size / orientation change; keep its Samsung NaN guard.
- Background that should fill the inset (bottom bar's blue base, top bar colour) is a **bleed
  child**: same 9-slice sprite, pivot at the inner edge, `sizeDelta` 1000 outward, raycast off.
  Disable the parent's own Image if the bleed now draws the same highlight twice.
- Android status bar visible in fullscreen → turn on Player Settings **"Render outside safe area"**
  first. Unity 6 explicitly calls `show(statusBars)` on Android 11+ when it is off, overriding
  theme/JNI/`Screen.fullScreen` fixes (cost a day of wrong fixes). Then rely on SafeArea for the
  punch-hole. Device evidence: `adb shell dumpsys window windows`.
- Keep the Android "Application Entry Point" consistent with the manifest LAUNCHER activity.

### Banner ad (implementing the policy chosen in §0.1-B)

- Banner height in pixels = `safeArea.yMin + bannerDp × density`; on device density =
  `MaxSdkUtils.GetScreenDensity() × adaptiveResolutionScale` (MAX density is native pixels,
  `Screen` is scaled pixels — forgetting the scale lifts the HUD 1/scale too high).
- Convert pixels → UI units through the panel's **measured** rect height, not `canvas.scaleFactor`.
- *Lift on show:* lift only the **overlap**, measured on the element that must stay clear (CardSort:
  the lowest booster icon, `bannerIconGapRatio` 0.5); a gap measured from the panel's lower edge
  leaves a strip of backdrop between bar and banner. Show the banner **before** the first layout
  (in `OnEnable`); re-read the lift on the banner event and relayout only on a real change,
  deferred until nothing is moving.
- *Reserve always:* the reserved strip uses the maximum adaptive banner height for the device, is
  filled with the bar's bleed colour, and is released (or kept, per decision) after Remove Ads.
- Editor: the MAX stub draws a fixed 168 px strip while density is 1 → emulate a 360dp phone
  (`density = min(w,h)/360`) and shrink the stub to match, or the editor lies about overlap.
  Deactivate the mock banner before comparing against a mock.

### 3D content behind/between UI

- Keep the **camera locked**; move/scale the content. Fixed zoom + variable content = clipping.
- Usable area = the real gap between HUD panels, read from their RectTransforms in viewport space
  (never a ratio constant).
- Split the usable area into % bands (configured per aspect ratio, lerped between entries,
  clamped outside the range; normalise once, *after* the lerp; an unconfigured entry must not be
  lerped toward — take the configured end).
- Fit each zone uniformly (`min(band.h/rect.h, band.w/rect.w)`), then one translation.
  **Idempotent:** reset scale to 1 → measure → scale → translate.
- Measure silhouettes from mesh **vertices**, not AABBs; meshes need Read/Write on — the Editor
  reads non-readable meshes anyway, so the bug shows only in builds.
- Cache measurements at build; app resume reports a new screen size and triggers re-layout — a
  re-measure mid-level measures the wrong content. Defer relayout until nothing is moving.

### Loading screen ↔ scene transition: a hand-off nobody can see

When the Preview for the loading screen (DataLoading / splash) and the scene-transition curtain
shows the same art — background + logo — the switch from one to the other must not move **a single
pixel on any screen ratio**. Matching at 1080×1920 is not enough: the two usually live on different
canvases, and the jump only appears on 9:20, 9:22 or an iPad. The loading bar / percent / tip text
belongs to the loading screen only; the transition may drop it (that is the one allowed difference).

1. **One definition of the shared art.** Same sprites, and for each element the same anchors,
   pivot, size, position, scale, Image type, `preserveAspect`, colour and material, and the same
   `AspectRatioFitter`. Best: a shared nested prefab (e.g. `SplashArt`) used by both, so they cannot
   drift. Copying is acceptable (CardSort copied DataLoading's `Background` + `Top/Logo` into
   SceneTransition) but then every later edit must be made twice — say so in a comment.
2. **Same canvas behaviour at runtime.** Find the canvas each one renders on (the loading screen is
   often a scene canvas, the transition a framework/persistent canvas) and confirm both CanvasScalers
   resolve to the same mode, reference resolution and match **after** any framework rewrite. CardSort:
   `DataLoading.unity` ScreenCanvas and GeneralManager's ScreenCanvas are both Expand 1080×1920.
   Different scalers = identical at 9:16, different size everywhere else.
3. **Same safe-area treatment.** Either both elements sit under a `SafeArea` or neither does —
   otherwise the logo jumps by the notch height on notched phones only. Full-bleed backgrounds stay
   outside SafeArea.
4. **Background covers every ratio without distortion:** `AspectRatioFitter` EnvelopeParent, or an
   oversized rect with `preserveAspect`. Never a non-uniform stretch. Check 9:22 and 3:4 extremes.
5. **Same pose at the hand-off.** If the loading logo animates (Spine idle, pulse, shine), the
   transition must take over at the same pose, or both must be static at that instant.
6. **The overlay must be complete before the scene under it changes**, and on top of everything
   (including the framework's loading/shield canvases). CardSort traps: SS.UI fades an opaque black
   *Scene Shield* in under the overlay during `Core.Load` → hide its Graphic for the transition; the curtain opened before the new scene had built → open only after the board is
   built; copied Images had `Maskable` off → the SoftMask circle cut no hole.
7. **Verify, in this order:**
   - static, every ratio: `psdtool pair <loading.prefab> <transition.prefab> --project <root>
     --ignore <loadingBarNode>,<maskNode> [--matchA … --matchB …]` → must end with
     "identical on every screen tested". It lays both out on the canvas each would get per screen
     (each side with its own scaler), converts to screen px and flags every Δ ≥ 0.5 px, SafeArea /
     fitter / image-type / tint / scale differences, and elements present on one side only.
     CardSort result: `BG_Loading` and `Logo_Game` Δ 0 px on 7 screens.
   - render: with the transition fully closed and the loading bar hidden, capture both at the same
     resolution on at least 9:16, 9:20 and 3:4 → pixel diff 0 (CardSort did 1080×1920: 0 of 2,073,600
     pixels differ). Static layout ignores SafeArea, layout groups and code — the render is the proof.
   - device: cold start → first level on a tall phone and a tablet; watch the swap frame.

### Measuring UI in the frame it was created

`Canvas.ForceUpdateCanvases()` before reading any RectTransform position/size in the same frame
it was instantiated (tutorial cut-outs and hands landed off-target on every scale factor ≠ 1).

---

## Step 3 — Events, input and sorting

### Opening/closing

See §1.0 for the skeleton. Also:
- `Init` is called after `Awake`/`OnEnable`, before `Start` — don't read Init data in `OnEnable`.
- Every tween: `SetUpdate(true)` + `SetLink(gameObject)`; kill in `OnDisable`. Use
  `DOTween.To(() => cg.alpha, …)` if the DOTween UI module is unreachable from your asmdef.

### Buttons — use the project's button component, and say so

**Every tappable thing is built on the project's button class**, whatever it looks like: normal
buttons, icon buttons, the X, tabs, list cells and cards, toggles drawn as buttons, a full-area
"tap to continue". In CardSort that is `UIButton : Button` (`Assets/_Framework/UI/UIButton.cs`), used in
23 prefabs; a plain `Button` exists only in `CheatTools.prefab`. In a new project, find the
equivalent first (`grep -rn ": Button" Assets --include=*.cs`); if there is none, ask.

What `UIButton` adds over `Button` (so the developer knows what they would lose):
| Field | Default | Does |
|---|---|---|
| `isScale` / `scale` | on / 0.95 | press shrinks to 95% and springs back (unscaled DOTween, 0.1 s) |
| `isUsingParentTf` | off | scales the parent instead (when the button's own transform is animated) |
| `isSound` | on | plays the UI tap SFX through `IAudioService` on click |
| `isGray` | off | grey material while not interactable |
| `isPress` | off | hold-to-repeat: fires `onClick` repeatedly, accelerating |
| `onDisableClick` | — | callback for a tap on a non-interactable button (e.g. "Not enough coins!") |
Haptics (`UiTapHaptics`) and `TrackClick` analytics work on any `Button`, so they come either way.

**Tell the developer, then let them decide.** When a screen creates buttons, list them in the plan
with the component and settings you intend, e.g.:
> Buttons in PopupX: `BtnX`, `BtnBuy`, `BtnAds` → `UIButton` (scale 0.95, sound on).
> `TapAnywhere` (full-screen, invisible) → `UIButton` with sound **off**, scale **off** — or a plain `Button`?
> Shop item cells inside a ScrollRect → `UIButton` (press-scale may fight the scroll) — keep or plain `Button`?
Do not silently switch any of them to a plain `Button` (or the reverse); default to `UIButton` and
ask about the odd ones: invisible/full-area taps, cells inside scroll views, toggles, debug UI.

`UIButton` traps:
- Press scale returns to the scale **captured in `Awake`**. Never animate the button's own
  `localScale` afterwards (pop-in, bounce, pulse) — animate a parent or a child, or set
  `isUsingParentTf`. A button that is scale 0 at `Awake` falls back to 1.
- `isGray` re-tweens the scale every `FixedUpdate` — it fights any scale animation on that button.
- `isPress` repeats `onClick`: every repeat is a click for analytics and debounce.
- The tap sound plays on click (pointer up); latency-critical feedback goes on pointer down.
- An invisible full-screen `UIButton` plays the tap sound on every touch — turn `isSound` off or
  toggle it with its popup (§3 Touch locking).

Wiring rules:
- Wire in **code only**, in `Awake`. Keep every prefab's persistent `m_OnClick` **empty** —
  persistent calls run *before* runtime listeners. CardSort: a stray persistent `OnClickBtnContinue`
  on the x2 button made one tap run Continue first, whose lock then refused the ad handler ("x2
  pays x1, no ad"). When one tap seems to hit two handlers, `grep m_MethodName` the prefab first.
- Analytics: `TrackClick` as **its own listener, added before** the handler — never inside it
  (back-key and shield paths reuse the handler but are not presses). A disabled button sends
  nothing. Use a `Func<string>` overload for values read at press time. `popup_show` is the
  **first line of `Init`**. All names are constants, never raw strings.
- Debounce every action that pays, spends, opens an ad or closes (`_actionTaken` /
  `TryBeginEndAction()` + `CanvasGroup.blocksRaycasts = false`); reset when a rewarded ad fails.
  Guard ad callbacks with `if (this == null) return;`.
- A short balance: keep the buy button **live** and answer with a toast ("Not enough coins!"),
  never a dead grey button with no reason.
- Android back = `IKeyBack.OnKeyBack()` → same handler as X.
- **Cleanup in `OnDisable`, not in button handlers** — a shield tap closes the popup without
  passing through any button. Fire `onClosed` exactly once.
- Shield tap-to-close: enable per popup in `OnEnable`, disable in `OnDisable`.

### Touch locking (gameplay under UI)

- If gameplay reads the raw pointer, give it a cover check: skip taps while
  `!Core.IsNoMoreScreen() || IsLockedTouch`. Both halves are needed — UI embedded in a scene never
  goes through `Core.Add`, so it only shows up as `IsLockedTouch`. Cover the gap between
  `Core.Add` and the screen being counted with a `HoldInput`/`ReleaseInput` bracket.
- A popup that never clears `IsLockedTouch` freezes the board forever — release it in `OnDisable`.
- **Invisible full-screen buttons are poison:** an always-active transparent button ate taps and
  played the tap sound on every touch of the game screen. Toggle such shields with the popup.

### Sorting over dims and tutorials

- Two canvases at the **same** sort order → the later one draws on top and its invisible shield
  eats the other's taps (CardSort: Game 99 = popups 99 → Game set to 98).
- A popup that can open during a tutorial needs its own nested `Canvas` (overrideSorting, order
  above the dim) **plus a `GraphicRaycaster`**.
- `overrideSorting`/`sortingOrder` written while the GameObject is **inactive** are lost on
  activation. `SetActive(true)` first, then configure the Canvas.
- Toast above a popup's dim: nested override Canvas, order read at runtime as
  `popup.GetComponentInParent<Canvas>().rootCanvas.sortingOrder + 1`, created after `SetActive`.
  Toasts are not screens: no raycast target, unscaled tween, a new message replaces the old one.
- A 3D object can never sort above a ScreenSpaceOverlay dim; redraw it with a second camera
  (one borrowed layer) into a RenderTexture on a RawImage placed between dim and hand.

---

## Step 4 — Sound and haptics per UI

Fill this table for every screen **before** coding and get it confirmed (together with §0.1-C/D).
Missing cells are decisions for the designer, not for the AI.

| Element / moment | SFX key | Haptic | Analytics | Notes |
|---|---|---|---|---|
| any button press | `sfx_ui_tap` (button base class) | `VIB_UI_Tap` | `button_click` | |
| popup open | (designer) | — | `popup_show` | gameplay SFX pause? (§0.1-C) haptic loop hold? (§0.1-D) |
| popup close | (designer) | — | | resume or stop? |
| reward burst | `sfx_coin_spawn` once | | `resource_earn` | |
| each coin landing | `sfx_coin_receive` per coin, rate-limited | `VIB_Coin_Receive` (min interval) | | |
| win / lose | `sfx_win` after delay | `VIB_Win` | | |

Rules:
- **Keys are constants** (`SFXName.X`) + one entry in the sound list ScriptableObject; run the
  sound-data validator after adding one. Clip files `SFX_*.ogg`.
- **The tap sound lives in the button base class** (`UIButton.isSound`), not in each handler. Play
  gameplay tap feedback on pointer **down** when latency matters.
- **Haptic is a separate component** (raycast on down/up, vibrate when both hit the same
  interactable Button) so a muted button still vibrates and framework code stays untouched.
  Rapid events (coins, balls) get a per-event min interval so they don't merge into a buzz.
- **Pause/resume under popups** exactly as decided in §0.1-C (one `GameplayFreeze.Enter/Exit`
  switching `timeScale` and the SFX pause together) and §0.1-D (haptic loop request vs motor).
- **Per-item sounds in a burst** (coin flight): play per landing, *before* any once-per-sequence
  early return, and rate-limit in the sound entry (CardSort: window 0.05 s, max 1 instance, volume 0.5).
- Music/SFX toggles drive mixer params; convert linear→dB with a floor (log10(0) = −∞ → clamp
  −80 dB). Save the setting immediately. The Haptic toggle gates every vibration.
- Pool everything that repeats (audio emitters, coin Images, sparks).
- Per-item visual feedback too: each coin landing resets and punches the icon
  (`DOPunchScale(0.2, 0.15, 1)`) — one bounce for the whole flight reads as frozen.
- A sequence the next step waits for (coin flight → load next level) must have a timeout fallback
  (`DOVirtual.DelayedCall(timeout).SetLink(gameObject)`), and the animator must fire all callbacks
  immediately when it has no visuals, so a caller can never hang.

---

## Step 5 — Verify (measure, don't eyeball)

### 5.1 Coordinates

If the mock is 1080×1920, mock px = canvas units at scale factor 1. Otherwise every mock number is
multiplied by `1080 / mockWidth` (and the mock's height ratio must be checked against 16:9 — a mock
drawn on 1242×2688 is a 9:19.5 screen, so compare against a render at that ratio, not 1080×1920).

### 5.2 Overlay test — the final gate (aim: indistinguishable)

Capture the built screen at the mock's exact resolution and blend it with the preview:
- **50% blend (onion skin):** where art exists in both (frame, icons, the popup's demo picture,
  text), the result must look **as sharp as either image alone**. Double edges = position offset;
  a soft halo = size/scale mismatch; a uniformly blurry demo picture = non-native size, non-1
  scale or the sprite import (§1.1-3). Fix until the demo picture looks like a single image.
- **Difference image:** `|build − mock|` per pixel, amplified ×4. Matching regions go black; every
  bright outline is an error with a direction you can read off. Report the % of pixels over a small
  threshold inside the popup's frame, per region.
- **Flicker:** toggle the two at the same position; anything that "jumps" is off.
Expected unavoidable differences (write them down instead of chasing them): live text that differs
from the mock's text, dynamic values, particle/Spine frames, the dim (framework shield).

**In-Editor overlay without MCP** (the developer can do this by hand): in the popup's *editing
scene* only, add a `MockOverlay` RawImage as the last child of the canvas, stretched full, preview
texture assigned, alpha 0.5, raycast off, tag `EditorOnly`; set the Game view to exactly the mock
resolution; nudge until nothing doubles. Never add it to the prefab, and delete or disable it
before committing the scene.

### 5.3 With Unity MCP (only if the developer enabled it)

Put `Application.dataPath` in the first log of every mutating command — the bridge can silently
attach to another open Editor.

**Offline render → measure → blend → diff** (edit mode, nothing saved):
1. Open the screen in a preview scene (`EditorSceneManager.OpenPreviewScene` or the editing scene),
   set canvas `ScreenSpaceCamera` + the camera, CanvasScaler Expand 1080×1920, force
   `contentRect` scale 1 and `CanvasGroup.alpha = 1` (both are runtime-only states).
2. `cam.targetTexture = new RenderTexture(W,H,24)` at the mock's resolution →
   **`Canvas.ForceUpdateCanvases()` after assigning the target** (the scaler reads
   `camera.pixelRect`) → `cam.Render()` → `ReadPixels` → PNG. Log `canvas.scaleFactor`.
3. Load the mock with `new Texture2D(2,2).LoadImage(bytes)` in the same command. Write three PNGs:
   the render, a 50% blend, and an amplified difference — then **look at the blend and the diff**
   with the Read tool, not just at numbers.
4. Region by region, compute tight colour-predicate bounding boxes on **both** images (the same
   probe on both cancels its own bias) and list every edge with Δ > 1 px.
5. Colour boxes lie when the art has the same colour nearby — confirm with a zoomed crop.
6. Repeat at each ratio in the table in Step 2 (change RT size; Expand keeps the design rect whole).

Traps: ScreenSpaceOverlay never renders into a camera (blank capture, no error) — hence the
ScreenSpaceCamera swap. Offline renders show **neither the framework dim shield nor SoftMask** —
"matches" offline can still be a doubled dim or an unmasked overlay. In a `RunCommand`,
`Image` resolves to `Unity.AI.Image` → `using UImage = UnityEngine.UI.Image;`. Put helpers in a
temporary editor script and delete it afterwards.

**Play-mode check** for overlay UI, dim, SoftMask and animation:
`ScreenCapture.CaptureScreenshot(path, 2)` from a separate command (written at end of frame),
read it in the next turn. Turn off the editor mock banner first.

### 5.4 Without MCP

Say plainly **"not compiled, not tested"**. Hand the developer: the in-Editor overlay recipe
(§5.2), the ratios table, the sounds/haptics table with the §0.1 decisions, and the taps to try
(tap inside frame, tap on dim, back key, double tap on every paying button, open during tutorial,
open while a long sound / haptic loop is playing, close into Restart/Home).

### Done checklist

- [ ] §0.1 decisions recorded: coin bar, banner policy, SFX pause/resume, haptic hold
- [ ] Root scale 1; tween on `Content`; sprites native size; 9-slices have borders; import sharp
- [ ] Every edge/baseline ≤ 1 px vs the mock (exceptions named); overlay blend shows no double edges
- [ ] Every design font present (file + TMP asset + glyphs); missing ones reported, not substituted
- [ ] Fonts: right weight, auto-size off while matched, Midline/measured offset, presets not shared default; new presets overlay-verified
- [ ] Canvas is Expand 1080×1920 at runtime (checked what code does to the scaler)
- [ ] Checked 9:16, 9:19.5, 9:20, 9:21+, 3:4, 4:5, 720×1280: nothing clipped, X reachable
- [ ] Loading screen ↔ transition: same art, canvas, SafeArea and pose; `pair` 0 px on every ratio; render diff 0 with the bar hidden
- [ ] SafeArea on Content; bleed children fill insets; banner never covers a button
- [ ] One dim only; frame has RaycastTarget; decorative graphics don't
- [ ] Full-screen screens: kind (popup stack / scene / page), open-close and switch animation, back path confirmed with the developer
- [ ] Every tappable uses the project's button class (`UIButton`), exceptions approved by the developer; no scale tween on a button's own transform
- [ ] No persistent `m_OnClick`; listeners in `Awake`; `TrackClick` before handler; `popup_show` first in `Init`
- [ ] Every paying/closing action debounced; ad callbacks null-guarded; back key works
- [ ] Gameplay taps blocked while open; `IsLockedTouch` released in `OnDisable`
- [ ] Sort order correct over dim/tutorial; nested Canvas configured after `SetActive`
- [ ] Sound table implemented; long/loop SFX paused or stopped as decided; haptic loops held/resumed
- [ ] All tweens `SetUpdate(true)` + `SetLink` / killed in `OnDisable`; repeating FX pooled
- [ ] Shared prefab instances carry their overrides (no white boxes); `maskable` on under SoftMask
- [ ] No `MockOverlay` left in any committed scene or prefab
- [ ] `git diff` touches no framework / third-party folder

---

## Appendix — trap index (symptom → cause)

| Symptom | Cause | Section |
|---|---|---|
| Popup ~12% bigger than mock though "measured" | root `localScale 1.12` from the template | 1.1 |
| Demo picture blurry in the overlay | non-native size, non-1 scale, or import Max Size/compression | 1.1 / 5.2 |
| Dim much darker than mock | prefab BG + framework shield | 1.1 |
| Tap inside popup closes it | BG removed, frame has no RaycastTarget | 1.1 |
| Font size change does nothing | TMP auto-size clamped by box height | 1.5 |
| Text a few px high/low | `Middle` aligns line metrics, not glyphs | 1.5 |
| Letters thinner than mock with outline | TMP outline eats half into the face | 1.5 |
| Every label restyled at once | shared default font material edited | 1.5 |
| Text right size, wrong width | wrong weight, or spacing tuned before size | 1.5 |
| Label outline wrong colour on one variant (Hard / theme) | sprite swapped per variant, text preset not | 1.5 |
| Inner box too tall / corners stretched in one popup | template height copied; `Image.type Simple` on a sliced sprite | 1.1 |
| Titles slightly grey, body text brown where mock is white | colour sampled from one pixel / inherited from the template | 1.5 |
| Every label a few % too big | size solved from the dilated white bbox; not rounded to the designer's size | 1.5 |
| Same font, presets with different stroke widths | per-preset measurement noise; one width per family | 1.5 |
| Three navy presets on one font / presets named by screen | preset made per use, not per colour; no reuse scan first | 1.5 |
| Presets in `Resources/Fonts & Materials/` the developer did not expect | put there "for TMP" though only the `<material>` tag needs it | 1.5 |
| Nodes named `Box`, `Caption`, `Count` — nobody knows what they are | no kind prefix; see the naming table | 1.0 |
| UI "flies away" in Scene view | huge local z after reparent | 1.1 |
| Overlay has no hole / mask ignored | `maskable = false` on copied Images | 1.1 |
| White boxes in coin bar | shared prefab has no sprites; instance lacks overrides | 1.1 |
| Spine plays before panel appears | `startingAnimation` set | 1.1 |
| Player told "Not enough coins" can't see balance | spending popup without coin bar | 0.1-A |
| Popup spills off both edges on 9:20 | MatchWidthOrHeight matched height (+ framework rewrite) | 2 |
| Bar floats above the bottom on tall phones | anchor fraction + pixel offset | 1.1 / 2 |
| Layout jumps when banner loads | lift-on-show without deferred relayout | 0.1-B / 2 |
| Strip of backdrop between bar and banner | banner gap measured from the panel edge | 2 |
| Editor banner covers buttons, device fine | editor density 1 vs stub 168 px | 2 |
| HUD lifted too high on device | MAX density native vs scaled `Screen` | 2 |
| Status bar won't hide (Android 11+) | "Render outside safe area" off | 2 |
| Tutorial hand/cut-out off target | measured before `ForceUpdateCanvases` | 2 |
| Content scaled wrong after app resume | re-measure on resize mid-level | 2 |
| Logo/background jumps when loading hands off to the transition (only on 9:20+/iPad) | the two canvases scale differently, or SafeArea on one side only | 2 |
| Black disc / black flash in the transition | framework scene shield fading under it, or curtain opened before the scene was built | 2 |
| Transition circle cuts no hole | copied Images have `Maskable` off under SoftMask | 2 / 1.1 |
| Tap on popup also taps the board | raw-pointer input, no cover check | 3 |
| One tap runs two handlers | persistent `m_OnClick` left in prefab | 3 |
| Button snaps to a wrong size after being pressed | its own scale was animated after `Awake` (`UIButton` restores the Awake scale) | 3 |
| Button pulse/bounce animation stutters | `isGray` re-tweens scale every FixedUpdate | 3 |
| Full screen slides in from the wrong place on tablets | slide distance was 1080/1920, not the canvas rect | 0.1-E |
| Popup taps swallowed | two canvases on the same sort order | 3 |
| Tap sound on every touch of the game screen | always-on invisible full-screen button | 3 |
| Popup under tutorial dim | no own Canvas/GraphicRaycaster, or set while inactive | 3 |
| Sound rings on under a paused popup | `timeScale 0` doesn't stop AudioSource | 0.1-C |
| Loop sound never stops after popup → Home | resumed instead of stopped; no safety stop | 0.1-C |
| Exception on popup close | `UnPause` on a pooled (inactive) emitter | 0.1-C |
| Phone keeps buzzing under a popup / after leaving | haptic loop not held; no stop on destroy | 0.1-D |
| Only the first coin is heard | sound after a once-per-sequence early return | 4 |
| Win waits seconds for coins | slow particle attractor; no timeout | 4 |
