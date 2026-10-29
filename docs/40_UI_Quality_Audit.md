# 40 — UI Quality Audit (HADAL vs Last Asylum)

Date: 2026-10-30 · Client 0.9.4 · Reference: Last Asylum: Plague (`com.phs.global`) on the same phone (1220×2712).

Method: phone screenshots of HADAL (base HUD, world map, alliance, hero list and card, bag, chat, update prompt) set
side by side with Last Asylum's base HUD, hero list, alliance list, bag, world map and dialogs, plus a read of the
shared UI code (`UiKit`, `UiSkin`, `UiFx`, `PvpCenter`, `MainHudView`, `SocialView.*`). Each gap below names the
evidence, the fix and where the fix belongs. Priorities: **P0** visible on every session or on a stressful moment,
**P1** visible on most screens, **P2** polish.

The user's verdict ("dağlar kadar fark var") is fair; most of the gap is not art budget but a missing *system*:
25 different font sizes, panels that are mostly empty light-grey, motion that is instant or linear, and alerts drawn
as flat rectangles. Those are all fixable in shared primitives.

---

## 1. The incoming-attack alarm (done in this pass — P0)

**Before:** four flat red bars (4–5 % of the screen, alpha 0–0.5, `sin` at 5 rad/s — a hard blink), drawn on the
same canvas order as the HUD so they washed over the resource bar and side buttons; a flat red strip with one
centred line of text (`⚠ Saldırı geliyor: [ABY] Kraken — 0:45`), proportional digits that jitter every second;
three buttons; not in the safe area; a toast and an error beep on top. Vibration ignored the setting.

**After (`AttackAlarm.cs`, `PvpCenter.TickAlarm`):**

| Layer | What it does |
| --- | --- |
| Edge vignette (`AlertVignette`) | Two 9-sliced bands with no centre: a wide black band that deepens the corners and a narrower glow that is deep red at the rim and amber further in (teal when shielded, amber for a scout). Procedural 128² textures with a power falloff, combined as a screen blend so corners are denser. Lives on its own canvas (order 33) *under* the HUD and world UI, so it never washes them out. Opacity goes through `CanvasRenderer.SetAlpha` (no mesh rebuild per frame); only the edge ring is drawn (overdraw ≈ a thin frame, not a full-screen quad). |
| Tempo (`AlarmTempo`) | A heartbeat ("lub-dub") pulse with an ease-out rise and an ease-in-out fall, never on/off. Period and peak interpolate smoothly: ≥120 s 2.8 s / 0.40, 60 s 2.1 s / 0.50, 30 s 1.4 s / 0.64, 10 s 0.9 s / 0.80, impact 0.72 s / 0.88 — never full-screen red. The second beat grows with urgency. The whole effect eases in (0.6 s) and back to rest (0.8 s). |
| Alert card (`AttackBanner`) | Dark glass card in the safe area under the resource bar: drop shadow, sheen, a thin rim and an accent strip in the mood colour; a sonar icon (disc, ring-and-dot glyph, soft glow, two expanding ping rings in step with the pulse; the shield icon when shielded); what is coming; `[TAG] Name`; a tabular countdown (`TabularClock`, fixed-width digit cells) with an "Arrives in" caption; "+N more"; a draining time bar. Slides in with ease-out-back (0.42 s), out with ease-in (0.22 s). Tap opens the war window. |
| Actions | Saldırana git / Kalkan aç / Takviye iste as the skinned buttons, in the safe area, hidden while a result card is up. |
| Sound | A low sonar ping (existing `Sfx.SonarPing` through `GameAudio`) when the alarm starts and then every 12 / 8 / 5 / 3 s as the impact nears; volume rises 0.35 → 0.6. The duplicate toast and error beep are gone. |
| Impact | An unshielded attack reaching its arrival: the rim flares and settles (~0.5 s), a trauma-based camera shake (`ScreenShake`, rotation applied only while each camera renders, so no gameplay transform moves), the card jolts, `Sfx.BattleHit`, one buzz (respects the Vibration setting). The result card follows from the server report as before. |
| Shielded | "Kalkan aktif — saldırı engellenecek": calm teal breathing (4.5 s), shield icon, no repeating ping. |
| Accessibility | Settings → Graphics → **Uyarı efektlerini azalt**: the vignette holds a steady glow, no pulse, no shake, a softer flash, static rings. |
| QA hook | `AlarmDemo`: `adb shell "echo attack 45 > /sdcard/Android/data/com.cicibyte.hadal/files/alarm_demo"` (also `rally`, `scout`, `shield`, `stop`) shows a client-only stand-in march. Nothing reaches the server. |

Not done (P2): a sonar ring rippling out from our base on the world map while under attack.

---

## 2. Prioritised gaps

### P0

1. **Typography has no scale.** `UiKit.Label` is called with 25 different sizes (22 ×49, 28 ×36, 26 ×35, 30 ×31,
   24 ×28, 40 ×18 …), so neighbouring screens never share a rhythm and hierarchy is weak (titles 34–42 next to body
   30). Last Asylum uses ~5 sizes: header 34–36, item name 28, body 24, caption 20, badge numbers 18 with a heavy
   stroke. **Fix:** `UiKit.TypeScale` (added: Micro 18, Caption 22, Body 28, Subtitle 34, Title 42, Display 52) and
   migrate screens to it as they are touched; a lint test that fails on new raw sizes.
2. **One font weight.** Only Fredoka SemiBold is bundled; `FontStyle.Bold` is faux bold (smeared on Android,
   inconsistent stroke). LA pairs a heavy display weight for titles/numbers with a regular body. **Fix:** bundle
   Fredoka Bold (same OFL family) for titles/buttons/numbers and Regular for body; pick by `FontStyle` in
   `UiKit.Label`.
3. **Panels are mostly empty light-grey.** Bag, hero list, chat and update prompt all open a full-height
   light card where content fills the top quarter (`bag_res`: 6 tiles, then 70 % blank). LA's bag is a dense
   4-column grid of 150 px tiles with rarity frames and big stroked counts; its dialogs size to content.
   **Fix:** content-sized dialogs (`UiKit.Dialog(height)`), 4-column item grids at ~230 px with a rarity frame and a
   bottom-right count badge; empty states (see P1-6) instead of blank space.
4. **Legacy `Text` + `Outline`.** Every stroked label adds an `Outline` (4 extra copies of each glyph mesh) and is
   bitmap-rasterised per size, which is soft at 1220 px wide. **Fix (larger):** move `UiKit.Label` to TextMeshPro
   with SDF outline and tabular figures (`<mspace>`), keeping the call signature. Biggest single crispness win;
   needs an Arabic/Thai font asset pass.

### P1

5. **Motion is instant or linear.** Panels pop in (`PopIn`, ease-out-back) but close instantly with
   `SetActive(false)` (59 call sites in UI); `UiKit.Fade` was linear; toasts used a frame-rate-dependent lerp.
   LA eases both ways (~0.18 s in, ~0.12 s out). **Fix:** eased `Fade` and `Damp` (added); next a
   `PopIn.Close(go)` that plays the reverse before deactivating, adopted by `SocialView.Show/Hide`.
6. **Empty and loading states.** Lists that are empty show nothing (bag tabs, mail, war log); requests show
   nothing until the snapshot arrives. **Fix:** `UiKit.Empty(parent, icon, text)` (icon at 30 % alpha + one muted
   line) and a 3-row shimmer skeleton for lists waiting on the server.
7. **Scroll feel.** `UiKit.ScrollList` was `Clamped` with default inertia: lists stop dead at the ends and feel
   like a form. **Fix (done):** elastic ends (0.12), inertia with a 0.12 deceleration rate — the native phone feel.
   Next: pool rows for long lists (alliance members, rankings, chat) instead of instantiating every row.
8. **Number formatting is mixed.** HUD resources read `142.7K` (invariant) while power reads `42,905` and cost chips
   used the phone culture (`1,5K` in Turkish next to `142.7K`). **Fix (done for cost chips):** invariant compact
   numbers everywhere; one `UiKit.Number(long)` helper for full numbers with a thin-space grouping.
9. **Iconography.** HUD icons are Blender renders with uniform lighting but no rim/backplate; LA puts every side
   icon on a consistent backplate with a coloured rim and a red dot badge. Ours float on the 3D scene and lose
   contrast over the light sea floor (Ücretsiz, Etkinlik, Sefer). **Fix:** a shared `HudIcons.Plate()` disc with a
   dark navy fill and a 2 px brass rim; badge dots at a fixed offset.
10. **Contrast.** Muted text `(0.55, 0.68, 0.74)` on the light panel skin is ~2.6:1; captions in the hero card
    stats row are below 3:1. **Fix:** a darker ink for muted text on light surfaces (`InkSurface` already flips the
    main ink; add a muted ink ≥ 4.5:1).

### P2

11. **Button states.** Press scale (0.94 spring) and a 0.75 tint exist; there is no disabled look beyond grey
    (`btn_grey`) and no "busy" state for buttons that wait on the server (double taps resend). **Fix:** `UiKit.Busy(button)`
    with a spinner glyph and `interactable = false` until the command resolves.
12. **Safe areas / notch / keyboard.** HUD, world map, guide and social views use `SafeArea`; modal canvases
    (`PvpWindow`, `SpeedupUi`, `RallyPoint`, `ProfileUi`, result cards) do not, so on a tall phone their close
    buttons can sit under the status bar. Keyboard lift exists only on Social pages. **Fix:** put `SafeArea` in
    `UiKit.Canvas` children by default (one `UiKit.SafeRoot(canvas)`).
13. **Panel skins and borders.** Panels use one 9-slice (`panel`) everywhere; LA distinguishes header bar, body,
    inset list rows and a highlighted selection with distinct borders. **Fix:** add `row` and `row_selected`
    skins in `art/ui/make_skin.py` and use them in lists.
14. **Spacing rhythm.** Paddings are fractional anchors (`Place(0.03f, …)`), so gutters change with aspect ratio.
    **Fix:** an 8-px grid via `Place(..., inset)` and `UiKit.Space.S/M/L` (8/16/24).

---

## 3. Implemented in this pass (small, shared, low risk)

1. **Attack alarm** (§1).
2. **Type scale** — `UiKit.TypeScale` (used by the new alarm; ready for migration).
3. **Scroll feel** — elastic, inertial `UiKit.ScrollList` (every scroll list in the game).
4. **Eased motion helpers** — `UiKit.Fade` eased in/out, `UiKit.EaseInOut/EaseOut/Damp`; toasts glide frame-rate
   independently.
5. **Consistent compact numbers** — `CostChips.Compact` formats with the invariant culture like the HUD.

## 4. Next (recommended order)

1. TMP migration of `UiKit.Label` (P0-4) with Fredoka Bold/Regular (P0-2).
2. Content-sized dialogs and the 4-column item grid (P0-3) for bag, hero list and shop.
3. `PopIn.Close` + empty/loading states (P1-5, P1-6).
4. HUD icon backplates (P1-9) and muted-ink contrast (P1-10).
