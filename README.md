# The Say Hey Dash — Unity WebGL Arcade Game

> **Living project tracker.** Updated at the end of every work session. Start here.

| | |
|---|---|
| Client | Fastball Fuel Coffee Company |
| Game | 2D pixel-art arcade recreation of Willie Mays's 1954 World Series catch ("THE CATCH") |
| Engine | **Unity 6.3 LTS (6000.3.9f1)**, C#, URP 2D — *replaces the Godot 4 suggested in the GDD* |
| Target | WebGL (desktop + mobile browsers), embedded in the client's Wix site |
| Design source | `THE SAY HEY DASH 2D GAME.pdf` (GDD) |
| Current milestone | **Milestone 1 — Discovery & Technical Proof: complete, awaiting review** |
| Last updated | 2026-11-17 |

---

## Milestone status

| # | Milestone | Status |
|---|---|---|
| 1 | Discovery & Technical Proof | ✅ Delivered — awaiting client review |
| 2 | Gameplay Prototype | ⏳ Not started (waiting for "Start Milestone 2") |
| 3 | Visual Production | ⏳ |
| 4 | Content & Polish | ⏳ |
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
- **Prototype only** (delete after M2): `Scripts/Prototype/*` + the PoC scene and its generator.

## Pending / next (Milestone 2 — do not start until requested)
- `GameFlowController` state machine (7 states + pause), `Main.unity` single scene.
- Outfielder movement, scripted ball flight + landing indicator, catch window, throw meter, win/retry loop.
- ScriptableObjects: `GameplayTuning`, `DifficultyProfile` (Default/Assisted), `ScoringConfig`, `MissionContent`.
- Pure-C# `ScoreCalculator` + EditMode tests.

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

---

## How to open & run

1. Install **Unity 6000.3.9f1** with **Web Build Support** via Unity Hub.
2. Unity Hub → *Add project from disk* → this folder.
3. Open `Assets/_Project/Scenes/Prototype/M1_TechPoC.unity` → Play.
   Controls: arrows/WASD (or drag the on-screen stick), Space (or the **A** button).

If the scene is missing or broken: **Say Hey Dash → Prototype → Rebuild M1 Tech PoC Scene**.

## How to build for the web

- Editor: **Say Hey Dash → Build → WebGL (Release)** (or *Development*). Output: `Builds/WebGL/`.
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
  Art/Sprites/Prototype/   M1 placeholder art (generated)
  Input/                   SayHeyDashControls.inputactions
  Plugins/WebGL/           BrowserBridge.jslib
  Scenes/Prototype/        M1_TechPoC.unity (temporary)
  Scripts/Runtime/         production code  (SayHeyDash.Runtime.asmdef)
  Scripts/Prototype/       M1 throwaway code (SayHeyDash.Prototype.asmdef)
  Scripts/Editor/          build + import + generators (SayHeyDash.Editor.asmdef)
Assets/WebGLTemplates/SayHeyDash/   custom web page template
Docs/                  milestone documents
Tools/                 ArtGen, WebTest server, WixHarness
Builds/                build output (git-ignored)
```

## Third-party assets
None shipped yet. All art/audio so far is original. Planned candidates and licences: [Docs/01 §11](Docs/01_Scope_and_Architecture.md#11-third-party-assets--licensing-plan).
