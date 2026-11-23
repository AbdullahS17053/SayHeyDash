# 07 — Milestone 4: Content & Polish

**Status:** delivered for review (2026-11-22). All historical copy is a **draft for client approval** (AC-11, Q1/Q6). Screenshots of every screen: `Docs/img/m4_screens/`. Replay diagram preview: [img/m4_replay_diagram_preview.png](img/m4_replay_diagram_preview.png).

## 1. GDD deliverables

| GDD deliverable | What was made | Where |
|---|---|---|
| Historical narrative and replay screen | Screen 07 is now an animated replay of the real play on an original top-down, Polo Grounds-style field diagram (deep centre field, 483 marker). Six steps, each with a short narration: the setting, the drive, the catch, the throw, the result (lead runner tags up and takes third, **no run scores**, runner on first goes back) and why it matters. Plays on its own; NEXT skips ahead, WATCH AGAIN restarts; sounds mark the hit, catch, throw and cheer. Shape legend (round = Mays, diamond = runners). Museum / STORY MODE button appears when the URL is set | `Replay/ReplayTimeline`, `Replay/ReplayDirector`, `UI/ReplayScreenView`, `MissionContent → replayBeats`, `Art/Sprites/Replay/` |
| Final scoring and rankings | Formula unchanged (400 / 300 / 300, every win ≥ 500, ranks 500 / 750 / 900; still waiting for sign-off Q6). Screen 06 now also shows the **best score on this device** (or NEW BEST!) and the **next rank to aim for** ("LEGENDARY AT 900", or TOP RANK!) | `Scoring/ScoreCalculator.TryGetNextRank`, `UI/MissionCompleteView`, `Settings/SettingsService` |
| Sound mixing and settings | Every cue's level set from measured loudness (§4). Two player volumes, MUSIC and EFFECTS, plus mute. The crowd ducks under the result jingles; the title music fades out on the crack of the bat. New **Settings** menu (from the title screen and the pause menu): SOUND, MUSIC %, EFFECTS %, REDUCED MOTION, ASSISTED MODE. Settings and the best score are **saved on the device** (best-effort; if the browser blocks storage the menu says so and they last for the visit) | `Audio/AudioService`, `Data/Audio/AudioCueLibrary`, `Settings/*`, `UI/SettingsMenuView` |
| Mobile controls | On-screen stick and action button now show **only on touch devices** (or after the first touch). The word above the button says what it does: CATCH, then THROW. Touch instructions on Screen 02. The field is narrowed slightly so the outfielder never runs under the controls. Web page: "please rotate your device" screen in portrait, "open full screen" link when the game is embedded on a phone or tablet, and the game **pauses itself** when the tab is hidden, the page loses focus or the rotate screen shows | `UI/TouchControls`, `Platform/BrowserEvents`, `WebGLTemplates/SayHeyDash` |
| Accessibility features | Reduced-motion switch in Settings; on a first visit it follows the browser / OS "reduce motion" preference. Reduced motion now also stops the PRESS START blink at once and makes the replay markers jump instead of glide. Overlay menus keep keyboard focus inside them. Text colours checked against WCAG contrast. Full checklist in §5 | `Settings/SettingsService`, `UI/*`, `Tests/EditMode/ContentAndPolishTests` |
| Full browser build for review | WebGL release build + itch.io-ready zip (§6) | `Builds/SayHeyDash_M4_WebGL.zip` |

## 2. How the pieces fit

- **Settings are remembered, not owned, by `SettingsService`.** `GameFlowController` still owns reduced motion and assisted mode, `AudioService` owns mute and volumes. `SettingsService` loads the saved values at start-up (it runs first), pushes them in, and saves whenever one of them raises its "changed" event. Storage goes through `ISettingsStore`: `PlayerPrefsSettingsStore` in builds (never throws; if storage is blocked the game carries on), `MemorySettingsStore` in the Editor and in tests, so Play mode always starts from the defaults unless **Persist In Editor** is ticked.
- **The replay cannot show an alternative history.** `ReplayTimeline` is a plain C# table of where each marker goes in each step. EditMode tests check that the lead runner tags up at second, ends on third and never comes near home, that the ball and the fielder meet at the catch, and that the batter is out.
- **The web page talks to the game in one place.** The template calls `SendMessage("BrowserEvents", "OnBrowserPause")`; `Platform/BrowserEvents` asks the flow controller to pause, which does nothing outside live play.
- **Mobile controls are still a virtual gamepad.** `TouchControls` only decides whether the stick and button are visible and what the label says; there is still no touch-specific gameplay code.

## 3. Editing guide (no code changes needed)

| Want to… | Do this |
|---|---|
| Edit the history text | `Data/Content/MissionContent` → **07 Replay / history**: `replayBeats` (title, narration, seconds per step; keep 6 entries), legend words, button labels. The EditMode test `ReplayContent_HasOneBeatPerStep_WithWordsThatFitTheScreen` fails if a narration is too long for its box |
| Turn on the museum / STORY MODE button | `MissionContent → museumUrl` (Q5) |
| Change the settings menu wording or the "saved" note | `MissionContent → Settings menu` |
| Change touch labels or touch instructions | `MissionContent → touchCatchLabel / touchThrowLabel / controlsHelpTouch` |
| Re-balance the sound | `Data/Audio/AudioCueLibrary` → volume per cue. `AudioService` (scene object *Audio*) → crowd duck level, jingle duck time, music fade-out time |
| Change the best-score / next-rank lines | `MissionContent → bestScoreLabel, newBestLabel, nextRankFormat, topRankLabel` |
| Keep settings between Play sessions in the Editor | Scene object *Settings* → **Persist In Editor** |
| Redraw the replay diagram | `Tools/ArtGen/generate_m3_art.py` → `replay_field()` (keep the base / catch-spot coordinates in step with `ReplayTimeline.cs`), run it, then **Build Main Scene** |

## 4. Sound mix

Levels were set from the measured loudness of each original clip (loudest 400 ms, RMS, after the cue volume):

| Group | Cues (volume) | Level |
|---|---|---|
| Gameplay hits — short, peaks near full scale, so they cut through | bat crack (1.0), glove pop (1.0), throw (0.85) | −16 to −19 dB |
| Timing cues | GET READY (0.4), NOW! (0.5), whiff (0.45) | −17 to −18 dB |
| Result jingles (the crowd ducks to 40 % under them) | success (0.6), fail (0.65) | about −16 dB |
| Background and menus | crowd cheer (0.5), title music (0.5), confirm / back (0.5), menu move (0.4) | −19 to −22 dB |
| Crowd bed | crowd loop (0.35) | about −30 dB |

Changes from M3: bat crack 0.9 → 1.0, catch 0.9 → 1.0, crowd cheer 0.7 → 0.5, GET READY 0.5 → 0.4, NOW 0.6 → 0.5, whiff 0.6 → 0.45, throw 0.8 → 0.85, success jingle 0.7 → 0.6, fail jingle 0.6 → 0.65, title music 0.45 → 0.5, menu move 0.5 → 0.4, confirm 0.6 → 0.5. MUSIC volume scales the title loop and jingles; EFFECTS scales everything else including the crowd. No AudioMixer is used, so the mix behaves the same in every browser.

## 5. Accessibility checklist (GDD "Accessibility and Usability", AC-12)

| GDD requirement | How it is met |
|---|---|
| Readable pixel fonts | Original Say Hey Pixel, 5×7 capitals drawn at 4× (1 font pixel = 1 game pixel); every on-screen string is checked by a test |
| Clearly labelled menu controls | Every button is a word; toggles show their value ("REDUCED MOTION: ON", "MUSIC: 80%"); the focused button has a cursor arrow as well as a colour change |
| Keyboard and touchscreen play | Keyboard / gamepad everywhere; touch: on-screen stick + CATCH/THROW button, every menu button is tappable |
| Visible gameplay instructions | Screen 02 controls (touch version on touch devices), HUD prompts, hints on TRY AGAIN |
| Audio mute control | Title, HUD icon, pause menu and Settings; remembered between visits |
| Pause and restart functions | Esc / II button; RESTART in the pause menu; auto-pause when the tab is hidden, focus leaves the game or the device is turned to portrait |
| No mandatory rapidly flashing effects | Only the PRESS START blink (1 Hz, test-checked ≤ 3 Hz), which reduced motion turns off |
| Reduced-motion option for decorative animations | Settings → REDUCED MOTION (default follows the browser preference): freezes crowd and flags, steady PRESS START, no score count-up, short catch sequence, replay markers jump instead of glide |
| Meaningful non-colour indicators | Dashed / solid ring, words for every cue, stars + badge + word for ranks, notched throw zone, round vs diamond replay markers |
| Skippable historical introduction | CONTINUE on Screen 02, skip on the catch sequence, NEXT on the replay |
| Contrast | All menu text / background pairs ≥ 4.5:1 (TRY AGAIN, large text, ≥ 3:1) — `TextColours_MeetWcagContrast` |
| No personal information | Only options and a best score are stored, on the device |

## 6. Test results (batch mode, 2026-11-22)

| Suite | Result |
|---|---|
| EditMode: M2/M3 suites + settings load/save/defaults/clamping, volume steps, replay choreography (runner ends on third and never reaches home, fielder and ball meet at the catch, everything stays on the diagram), replay text fits its box, next-rank boundaries, WCAG contrast, mix order | **96 / 96 passed** |
| PlayMode: M2/M3 suites + `ContentAndPolishPlayTests` (settings menu changes and saves every option, saved settings restored, Settings from pause locks assisted mode and closes on resume, NEW BEST! and next rank, replay steps to the final picture with the runner on third, replay advances on its own, touch controls only in touch mode with CATCH → THROW, browser pause only during play, crowd ducks under the jingle, blink ≤ 3 Hz and stops with reduced motion) | **19 / 19 passed** |
| WebGL release build | **9.80 MB** compressed (target ≤ 15 MB; M3 was 10.26 MB). Upload zip `Builds/SayHeyDash_M4_WebGL.zip` is 9.76 MB. Checked in the browser pane: title → objective → play → TRY AGAIN; settings menu from the title and the pause menu; MUSIC change survives a page reload (`SETTINGS ARE SAVED ON THIS DEVICE`); the page's pause request pauses live play only; phone portrait shows the rotate screen; touch-phone landscape shows the touch instructions and the stick + CATCH button, desktop shows none. No errors in the console apart from the two known URP shader lines (M5). Catch, throw, results, replay and settings screens checked with `ScreenGalleryCapture` renders (`Docs/img/m4_screens/`) |

## 7. Changes to existing behaviour

| Change | Why |
|---|---|
| `playerBounds` x: ±9 → **±6.5** | Keeps the outfielder clear of the on-screen stick and button (Docs/04 F8). The landing point (x 3.5) and the start (x −2) are well inside, so difficulty is unchanged |
| Title: START and SETTINGS share the top row | Room for the Settings button; SOUND / ASSISTED MODE / CREDITS stay on the bottom row |
| Pause menu gains SETTINGS | Volume and reduced motion can be changed mid-mission |
| Screen 07 layout replaced (diagram + narration instead of one text block); `MissionContent.historicalRecap` replaced by `replayBeats` | Animated replay (GDD) |
| Screen 06: runner outcome moved down one line; best score and next rank added | Final scoring and rankings |
| Credits BACK button no longer lets arrow keys wander to the title buttons underneath | Focus order (accessibility) |

## 8. Known limitations / open items

- **Historical copy and names need client approval** (AC-11, Q1). The text uses Willie Mays, Vic Wertz, Larry Doby, New York and Cleveland (no team nicknames or marks); every word is in `MissionContent`, so the generic variant is a text edit.
- **Scoring sign-off** (Q6) is still open; thresholds and maxima are in `Data/Scoring/ScoringConfig`.
- **Museum / STORY MODE URL** (Q5) not yet supplied: the button stays hidden.
- **Credits** still show "[DEVELOPER CREDIT - TO CONFIRM]".
- **Settings may not persist inside some embeds** (Safari / partitioned iframes). The menu then says "THIS BROWSER BLOCKS SAVING"; detection covers blocked storage, not every case where a browser silently clears it.
- **Touch targets:** menu buttons are 15–16 game pixels tall (about 30–35 CSS px on a landscape phone), a little under the 44 px guideline; the on-screen stick and button are large. To be checked on real phones in M5.
- **Not yet verified on real devices:** iPhone Safari, Chrome Android, the rotate screen and the open-full-screen link (M5 browser matrix).
