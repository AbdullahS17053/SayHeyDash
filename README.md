# The Say Hey Dash — Unity WebGL Arcade Game

> **Living project tracker.** Updated at the end of every work session. Start here.

| | |
|---|---|
| Client | Fastball Fuel Coffee Company |
| Game | 2D pixel-art arcade recreation of Willie Mays's 1954 World Series catch ("THE CATCH") |
| Engine | **Unity 6.3 LTS (6000.3.9f1)**, C#, URP 2D — *replaces the Godot 4 suggested in the GDD* |
| Target | WebGL (desktop + mobile browsers), embedded in the client's Wix site |
| Design source | `THE SAY HEY DASH 2D GAME.pdf` (GDD) |
| Current milestone | **Milestone 3 — Visual Production: complete, awaiting review** |
| Last updated | 2026-11-19 |

---

## Milestone status

| # | Milestone | Status |
|---|---|---|
| 1 | Discovery & Technical Proof | ✅ Delivered — awaiting client review |
| 2 | Gameplay Prototype | ✅ Delivered — awaiting client review |
| 3 | Visual Production | ✅ Delivered — awaiting client review (art/palette approval: Q1, Q2) |
| 4 | Content & Polish | ⏳ Not started (waiting for "Start Milestone 4") |
| 5 | Testing & Final Handover | ⏳ |

### Milestone 1 deliverables

| GDD deliverable | Where | Status |
|---|---|---|
| Confirmed scope & project architecture | [Docs/01_Scope_and_Architecture.md](Docs/01_Scope_and_Architecture.md) | ✅ |
| Art-direction reference sheet | [Docs/02_Art_Direction.html](Docs/02_Art_Direction.html) | ✅ draft for approval |
| Basic gameplay storyboard | [Docs/03_Gameplay_Storyboard.html](Docs/03_Gameplay_Storyboard.html) | ✅ |
| Technical proof of concept (Unity) | `Assets/_Project/Scenes/Prototype/M1_TechPoC.unity` | ✅ |
| HTML5 / WebGL browser test | [Docs/04_WebGL_and_Wix_Feasibility.md](Docs/04_WebGL_and_Wix_Feasibility.md) | ✅ local Chromium; other browsers pending |
| Wix embed feasibility test | same report + `Tools/WixHarness` | ✅ simulated cross-origin iframe; real Wix page pending hosting (Q3/Q4) |

### Milestone 2 deliverables
Full report, tuning guide and test results: [Docs/05_M2_Gameplay_Prototype.md](Docs/05_M2_Gameplay_Prototype.md).

| GDD deliverable | Where | Status |
|---|---|---|
| Character movement | `Gameplay/OutfielderController` | ✅ |
| Ball trajectory & landing indicator | `Gameplay/BallTrajectory`, `BallFlight`, `LandingIndicator` | ✅ deterministic arc + shadow + dashed/solid ring |
| Catching mechanic | `Gameplay/CatchSystem`, `CatchJudge` | ✅ zone + timing window, READY/NOW cues |
| Throwing mechanic | `Gameplay/ThrowSystem`, `ThrowJudge`, `UI/ThrowMeterView` | ✅ needle meter, notched zone, time limit |
| Basic win/retry loop | `Flow/GameFlowController` + `Scenes/Main.unity` | ✅ all 7 states + pause; 25-mission soak test passes |
| Placeholder graphics | `Art/Sprites/Prototype` (+ landing rings) | ✅ |

### Milestone 3 deliverables
Full report, editing guide and test results: [Docs/06_M3_Visual_Production.md](Docs/06_M3_Visual_Production.md). Screenshots of every screen: `Docs/img/m3_screens/`.

| GDD deliverable | Where | Status |
|---|---|---|
| Approved pixel-art character | `Art/Sprites/Character` · sheet `Docs/img/m3_outfielder_sheet.png` | ✅ submitted for approval |
| Stadium environment | `Art/Sprites/Environment` (Polo Grounds-style centre field, clubhouse scoreboard, 483 marker) | ✅ submitted for approval |
| Animations | `Visuals/SpriteAnimator`, `OutfielderAnimator`, `EnvironmentAnimator` · `Data/Visuals/OutfielderAnimations` | ✅ idle, run, over-the-shoulder run, catch, whiff, spin, throw; ball, crowd, flags |
| Title and mission screens | `UI/TitleScreenView` (+ credits), `UI/BriefingScreenView` | ✅ |
| HUD, menus and results screens | `UI/HudView`, `PauseMenuView`, `MissionFailedView`, `MissionCompleteView`, `ReplayScreenView`, `PixelButton` | ✅ (Screen 07 content is M4) |
| Initial sound implementation | `Audio/AudioService`, `Data/Audio/AudioCueLibrary`, `Assets/_Project/Audio` | ✅ 14 original cues + mute |

---

## Completed features (cumulative)

**Milestone 1**
- Project structure under `Assets/_Project` with 3 assembly definitions (Runtime / Prototype / Editor).
- `GameInput` — device-agnostic input (keyboard, gamepad, mobile on-screen stick/button) over `SayHeyDashControls.inputactions` (Move, Action, Submit, Pause).
- `BrowserBridge` + `BrowserBridge.jslib` — iframe detection, page focus, touch detection, DPR, gesture-safe URL opening.
- `WebGLBuildPipeline` — WebGL player settings in code (Brotli + decompression fallback, Wasm, High stripping, size-optimised IL2CPP) and one-click / batch-mode builds.
- `PixelArtTexturePostprocessor` — enforces Point filter, 16 PPU, no mips, no compression for new sprites.
- Custom WebGL template `Assets/WebGLTemplates/SayHeyDash` — responsive 16:9 letterbox, branded loader, click/tap-to-play gate (iframe focus + audio unlock), refocus hint, no external dependencies.
- Pixel Perfect Camera setup: 320×180 reference, 16 PPU, Upscale RT, Stretch Fill.
- Placeholder art in the proposed palette (outfielder 32×32, ball, shadow, Polo Grounds-style centre-field background) via `Tools/ArtGen`.
- **Prototype only** (delete once M2 is approved): `Scripts/Prototype/*` + the PoC scene and its generator.

**Milestone 2**
- `Main.unity` — the single gameplay scene, generated by **Say Hey Dash → Build Main Scene** (`Editor/Scenes/MainSceneBuilder`); only build scene.
- `GameFlowController` — state machine for the 7 states (Title, MissionBriefing, TrackBall/CatchWindow, CatchSequence, Throw, MissionFailed, MissionComplete, Replay) + pause overlay (`Time.timeScale`), in-place reset for unlimited retries; pure transition table `GameFlowRules`.
- `OutfielderController` (accelerated movement in bounds), `BallFlight` + pure `BallTrajectory` (deterministic arc + shadow), `LandingIndicator` (dashed/solid ring — shape cue).
- `CatchSystem` + pure `CatchJudge` — presses ignored until 0.65 s before the catch, then judged: caught / too early / too late / out of zone (hint per mistake).
- `ThrowSystem` + pure `ThrowJudge` — ping-pong needle, notched target zone, 5 s limit.
- Pure `ScoreCalculator` (Docs/01 §6 formula; 500–1000 on success; ROOKIE / ALL-STAR / LEGENDARY with star markers).
- ScriptableObjects in `Assets/_Project/Data/`: `GameplayTuning`, `Difficulty_Default`, `Difficulty_Assisted` (catch ×2, ring ×1.25, throw zone ×1.5), `ScoringConfig`, `MissionContent` (every on-screen string).
- Assisted mode toggle on the title screen; reduced-motion switch on `GameFlowController` (Inspector) shortens the catch sequence.
- Placeholder UI (`UI/PlaceholderFlowView`, `ThrowMeterView`, `BlinkingText`) — one panel per state, keyboard/gamepad navigable, touch buttons, pause **II** button.
- `UI` action map in `SayHeyDashControls.inputactions` (Space/Enter submit, multi-touch pointers) wired to the EventSystem.
- Tests: `Tests/EditMode` (51) + `Tests/PlayMode` (5: `MissionLoopTests` incl. 25 consecutive missions, and `ThrowMeterRenderTests`, a pixel check that the throw zone is drawn).

**Milestone 3**
- `Tools/ArtGen/generate_m3_art.py` — draws all production art from code: outfielder pose rig (31 frames, 32×32), ball spin, stadium + crowd/flag layers, title backdrop and logo, UI kit (9-slice panels/buttons, cursor, icons, rank badges, touch controls), the **Say Hey Pixel** font atlas, and preview sheets in `Docs/img/`.
- `Tools/AudioGen/generate_audio.py` — synthesises 14 original cues (SFX, crowd bed + cheer, jingles, title loop) into `Assets/_Project/Audio/`.
- `Visuals/` — `SpriteAnimationClip` (pure frame maths), `SpriteAnimator` (SpriteRenderer or UI Image), `OutfielderAnimator` (clip from state + catch/throw events; spin faces the infield), `EnvironmentAnimator` (crowd cheer, flags; frozen by reduced motion).
- `Audio/` — `AudioCue`, `AudioCueLibrary` SO, `AudioService` (event-driven; SFX / music / crowd sources; mute; pause pauses music + crowd).
- `UI/` — `ScreenView` base + one view per screen (Title with credits overlay, Briefing, HUD, Pause, Failed, Complete with count-up and rank badge, Replay), `PixelButton` (cursor arrow, hover = focus, menu sounds), `UiSelection`. `PlaceholderFlowView` deleted.
- `Editor/Fonts/PixelFontBuilder` (**Say Hey Dash → Rebuild Pixel Fonts**) — builds the original font into two legacy custom fonts (Regular/Bold, size 10: `fontSize 40` = 4×).
- `PixelArtTexturePostprocessor` — `*_slice{N}.png` gets an N-px 9-slice border; `Apply()` reused by the scene builder.
- `MainSceneBuilder` rebuilt for M3: stadium layers, animators, audio, 7 styled screens on the 320×180 art grid (1 art px = 4 canvas units, `referencePixelsPerUnit` 64), new data assets `Data/Visuals/OutfielderAnimations.asset` and `Data/Audio/AudioCueLibrary.asset` (created if missing; only empty clips/entries are filled).
- Gameplay touch-ups: `catchHeight` 0.65 → 1.1 (glove height; timing unchanged), ball hidden while in the glove, throw leaves from the throwing hand, throw meter recoloured (yellow zone, red needle), word feedback after the throw.
- Tests: EditMode 70 (+ frame timing, font covers every on-screen string, every cue has a clip, every clip has frames, throw feedback words); PlayMode 9 (+ `PresentationTests`: one screen per state, animation follows the play, ball in glove, run → idle, mute). `ScreenGalleryCapture` (Explicit) writes `Docs/img/m3_screens/*.png`.

## Pending / next (Milestone 4 — do not start until requested)
- Historical narrative + animated replay on Screen 07 (client-approved copy, Q1/Q6), runner animation (lead runner holds at third).
- Final scoring and rankings sign-off (Q6).
- Sound mixing and a settings screen: `SettingsService` (mute, reduced motion, assisted mode persisted).
- Mobile controls: show the on-screen stick/button on touch devices only; rotate-to-landscape overlay.
- Accessibility pass (reduced-motion toggle in the UI, focus order, contrast check).
- Client feedback on the M3 art (character, stadium, palette) and the developer credit line.
- Delete the M1 prototype code/scene once the client confirms M2.

## Open questions for the client
See [Docs/01 §1.2](Docs/01_Scope_and_Architecture.md#12-decisions-needed-from-the-client-blocking-later-milestones-not-m1): name/likeness licensing (Q1), palette extensions (Q2), HTTPS host (Q3), Wix editor & page (Q4), museum URL (Q5), scoring formula (Q6).

## Known issues / limitations
Full table: [Docs/04 §6](Docs/04_WebGL_and_Wix_Feasibility.md#6-findings-limitations--risks). Summary:
- **Fullscreen inside the Wix embed is probably blocked** by Wix's outer iframe → standalone-page fallback + hide the button when unsupported (M4/M5).
- **Portrait phone embeds are tiny** → "rotate to landscape" overlay + mobile "play full screen" link (M4).
- **iPhone Safari has no element Fullscreen API** → disclose as a limitation; standalone page works.
- **Settings persistence may fail in third-party iframes** (storage partitioning) → best-effort `SettingsService` (M4).
- URP logs two harmless "shader not supported" lines on WebGL → strip in M5.
- On-screen controls overlap the field edges → show only on touch devices (M4).
- **M3 by design (later milestones):** Screen 07 has placeholder history text and no animated replay; mute / assisted / reduced motion are not saved and reduced motion has no UI toggle yet; on-screen controls always visible; first-pass sound levels; credits show "[DEVELOPER CREDIT - TO CONFIRM]". See [Docs/06 §6](Docs/06_M3_Visual_Production.md#6-known-limitations-later-milestones).
- **M3 art is generated from code** (`Tools/ArtGen`), delivered as PNG frames + sheets, not Aseprite files.
- **In-browser scripted play is unreliable while the browser pane is hidden** (background tab throttling). Visual checks of the catch/throw/results screens use the `ScreenGalleryCapture` PlayMode capture instead.
- **Gameplay feel not yet tuned with real players:** the default values (3.6 s flight, ±0.2 s catch window, 0.16 throw zone) were checked only by automated tests and a scripted browser run. Client playtest feedback is wanted before M3.
- **Re-running *Rebuild M1 Tech PoC Scene* makes the PoC the only build scene.** Fix it with *Build Main Scene*.
- **Not yet verified:** real Wix page, Safari macOS/iPhone, Firefox, Chrome Android, fullscreen and popup with a real human click, `sandbox="allow-scripts"`-only embeds.

## Milestone 1 measured results
| Metric | Target | Result |
|---|---|---|
| Compressed download | ≤ 15 MB | **9.30 MB** |
| Load to "click to play" (local) | ≤ 10 s | **2.98 s** |
| Desktop FPS @1280×720 | 60 | **59.8** |
| Cross-origin iframe: keys, focus, no host scroll | works | ✅ |

## Decision log
| Date | Decision | Reason |
|---|---|---|
| 2026-11-17 | Unity 6.3 LTS + C# instead of Godot 4 | Client/team instruction |
| 2026-11-17 | Single gameplay scene, state-machine driven | Fast WebGL start, trivial restart (AC-08) |
| 2026-11-17 | 320×180 @ 16 PPU, Pixel Perfect Camera (Upscale RT, Stretch Fill) | Authentic chunky pixels at any iframe size |
| 2026-11-17 | Input System actions + On-Screen Controls (virtual gamepad) for mobile | One input path for every device |
| 2026-11-17 | Brotli + decompression fallback | Smallest download; works on hosts without `Content-Encoding` headers. Revisit in M5 |
| 2026-11-17 | Click/tap-to-play gate in the web template | Guarantees iframe keyboard focus + audio unlock in Wix |
| 2026-11-17 | Legacy uGUI `Text` in the PoC only; TextMeshPro + OFL pixel font from M3 | Avoids importing TMP essentials for throwaway UI |
| 2026-11-18 | M2 placeholder UI also uses legacy `Text` (single `PlaceholderFlowView`) | Same reason; replaced wholesale by styled per-screen views in M3 |
| 2026-11-18 | `catchZoneRadius` merged into `landingZoneRadius` | The ring the player sees is exactly the zone they must stand in, so there is one value to tune, not two |
| 2026-11-18 | Catch presses ignored until `catchInputOpenSeconds` before the catch, then judged | Fair: a stray press while running can't fail you, but button-mashing near the ball still does |
| 2026-11-18 | Flow controller routes the Action button and ignores it on the frame a state changes | One Space press can't both confirm a menu and catch/throw (Space is bound to Action and UI Submit) |
| 2026-11-18 | Project `UI` action map on the EventSystem instead of package defaults | The GDD needs Space to confirm menus; multi-touch pointers for the on-screen stick and button |
| 2026-11-18 | Throw has a 5 s time limit (`throwTimeLimitSeconds`) → THROW MISSED | The mission never stalls; set it to 0 to disable |
| 2026-11-18 | Throw meter built from three non-overlapping images (track left / zone / track right) | In WebGL a resized fill drawn over a same-material background did not render. Laying them side by side avoids depending on draw order |
| 2026-11-18 | Scene and data assets are generated by `MainSceneBuilder`; existing data assets are never overwritten | The scene is reproducible, and designers' tuning/text edits survive a rebuild |
| 2026-11-19 | **Original bitmap font "Say Hey Pixel" + legacy uGUI `Text`** instead of TextMeshPro + OFL fonts (reverses the 2026-11-17 plan) | GDD asks for original arcade typography; no licence file; no TMP essentials import; one pixel grid with the game |
| 2026-11-19 | All M3 art and audio generated by scripts (`Tools/ArtGen`, `Tools/AudioGen`) | Reproducible, original, palette-locked; a pose or colour change updates every frame |
| 2026-11-19 | Own `SpriteAnimator` + `OutfielderAnimator` instead of Animator controllers | No controller assets, no allocations, clip state visible to tests |
| 2026-11-19 | UI authored on the 320×180 art grid (1 art px = 4 canvas units, `referencePixelsPerUnit` 64) | UI pixels match game pixels; 9-slice borders scale with the art |
| 2026-11-19 | `catchHeight` 0.65 → 1.1 | Ball meets the glove of the over-the-shoulder pose; perfect-catch time unchanged |
| 2026-11-19 | HUD prompt in the top sky band, zone label bottom centre | The fly ball never rises above row ~37, so prompts never hide the ball or the scoreboard |
| 2026-11-19 | `OutfielderController.MoveOverride` test hook | Batch-mode runs have no window focus, so simulated keyboard input is dropped |

---

## How to open & run

1. Install **Unity 6000.3.9f1** with **Web Build Support** via Unity Hub.
2. Unity Hub → *Add project from disk* → this folder.
3. Open `Assets/_Project/Scenes/Main.unity` → Play.
   Controls: arrows/WASD (or drag the on-screen stick) to run; Space (or the **A** button) to catch, throw and skip; Enter/Space to confirm menus; Esc or **II** to pause.

If the scene is missing or broken: **Say Hey Dash → Build Main Scene**. To regenerate art or sound (from the repo root): `py -I Tools/ArtGen/generate_m3_art.py` and `py -I Tools/AudioGen/generate_audio.py`, then Build Main Scene. Gameplay numbers and all text are edited in `Assets/_Project/Data/` (see the [tuning guide](Docs/05_M2_Gameplay_Prototype.md#3-tuning-guide-no-code-changes-needed)).

## How to run the tests

Unity → Window → General → **Test Runner** → EditMode / PlayMode → Run All. From the command line (with the editor closed):
```bash
"D:\Gaming\6000.3.9f1\Editor\Unity.exe" -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults Logs/playmode.xml
```

## How to build for the web

- Editor: **Say Hey Dash → Build → WebGL (Release)** (or *Development*). Output: `Builds/WebGL/`.
- Milestone 3 deliverable build: `Builds/SayHeyDash_M3_WebGL.zip` (2026-11-19). M2 build: `Builds/SayHeyDash_M2_WebGL.zip`. Upload the zip as-is to itch.io (Kind: HTML, "played in the browser"), or unzip it onto any HTTPS static host. No special server headers are needed.
- **Always package with** `py -I Tools/Release/zip_webgl.py Builds/WebGL Builds/<name>.zip`. Do **not** use Windows' `Compress-Archive` or "Send to → Compressed folder": they write backslash paths, and itch.io then shows an unstyled page with "Could not load the game files".
- Command line (editor closed):
  ```bash
  "D:\Gaming\6000.3.9f1\Editor\Unity.exe" -batchmode -quit -projectPath . -buildTarget WebGL -executeMethod SayHeyDash.EditorTools.Build.WebGLBuildPipeline.BuildFromCommandLine -logFile Logs/webgl_build.log
  ```

## How to test locally (incl. Wix simulation)

```bash
py -I Tools/WebTest/serve.py Builds/WebGL 8601
```
```bash
py -I Tools/WebTest/serve.py Tools/WixHarness 8602
```
Open <http://localhost:8601/> (standalone) and <http://localhost:8602/> (simulated Wix page embedding the game cross-origin; switch sizes / sandbox flags there). Opening `index.html` directly from disk does **not** work — browsers block WebGL builds on `file://`.

## Project layout

```
Assets/_Project/       all game content (see Docs/01 §9 for the full tree)
  Art/Sprites/             Character/, Environment/, Gameplay/, Title/, UI/ (M3, generated) · Prototype/ (M1)
  Art/Fonts/               Say Hey Pixel atlas + metrics + Regular/Bold font assets
  Audio/                   SFX/, Music/ (M3, generated by Tools/AudioGen)
  Data/                    Tuning/, Scoring/, Content/, Visuals/, Audio/ ScriptableObjects (edit these to tune)
  Input/                   SayHeyDashControls.inputactions (Gameplay + UI maps)
  Plugins/WebGL/           BrowserBridge.jslib
  Scenes/                  Main.unity (the game) · Prototype/M1_TechPoC.unity (temporary)
  Scripts/Runtime/         production code  (SayHeyDash.Runtime.asmdef)
                           Controls/ Platform/ Data/ Flow/ Gameplay/ Scoring/ UI/ Visuals/ Audio/
  Scripts/Prototype/       M1 throwaway code (SayHeyDash.Prototype.asmdef)
  Scripts/Editor/          build + import + scene generators (SayHeyDash.Editor.asmdef)
  Tests/EditMode/          unit tests          Tests/PlayMode/  mission loop tests
Assets/WebGLTemplates/SayHeyDash/   custom web page template
Docs/                  milestone documents
Tools/                 ArtGen, AudioGen, WebTest server, WixHarness, Release/zip_webgl.py (upload zip)
Builds/                build output (git-ignored)
```

## Third-party assets
None. All art, the font and all audio are original (generated by the project's own scripts). Licence record: [Docs/ThirdPartyLicenses.md](Docs/ThirdPartyLicenses.md).
