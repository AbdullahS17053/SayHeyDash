# 08 — Milestone 5: Testing & Final Handover

**Status:** in progress (started 2026-11-23). This first part delivers the client branding and the handover material (documentation, user manual, tutorial video). Still to do in M5: the real-device browser matrix, the agreed Wix embed / HTTPS host, the final clean-ups and the client sign-offs listed in §5.

## 1. Delivered so far

| Item | What was made | Where |
|---|---|---|
| Client logo in the project | The Fastball Fuel Coffee Co. logo supplied by the client, kept unchanged as the source file, plus files generated from it: a trimmed transparent logo, a 1024 px square icon (logo on black), the browser-tab icon and the phone home-screen icon | `Assets/_Project/Art/Branding/`, `Assets/WebGLTemplates/SayHeyDash/TemplateData/` |
| Project / Player Settings | Company *Fastball Fuel Coffee Company*, product *The Say Hey Dash*, **default icon** = the logo icon, **splash screen** = the logo on black for 2.5 s with a dolly zoom, *Made with Unity* logo off. Applied by a menu command and re-applied by every WebGL build, so the settings can't drift | *Say Hey Dash ▸ Branding*, `Editor/Branding/BrandingBuilder.cs`, `Editor/Build/WebGLBuildPipeline.cs` |
| Splash screen in the browser | The web page covers the game with "CLICK OR TAP TO PLAY" until the first click, so Unity's start-up splash would play unseen behind it, and Unity can't restart its splash onto the web canvas afterwards (tested: `SplashScreen.Begin/Draw` drew nothing visible). In web builds the start-up splash is therefore stopped and the game shows the **same logo-on-black splash itself right after the click**: fade in, 2.2 s hold with a gentle zoom (none with reduced motion), fade out; a click / key skips it; menu keys are muted while it shows so Space/Enter can't start a mission behind it. Once per page load | `Platform/BrandSplash.cs`, `UI/BrandSplashView.cs` (own canvas above all UI, built by *Build Main Scene*), `Platform/BrowserEvents.OnBrowserStart`, web template |
| Web page branding | Loading screen shows the logo above "PRESENTS / THE SAY HEY DASH"; favicon and Apple touch icon | `WebGLTemplates/SayHeyDash/index.html`, `style.css` |
| Developer documentation | Set-up (clone, Unity version, first open), project map, no-code editing table, logo replacement, build / package / local test / publish, tests, Git basics, troubleshooting | [Developer_Guide.md](Developer_Guide.md) |
| User manual | Players: starting, controls (keyboard / gamepad / touch), how to play, scoring and ranks, settings and accessibility, troubleshooting. Website owner: hosting, Wix embed steps and limits, updating | [User_Manual.md](User_Manual.md) |
| Tutorial video | ≈ 4 min narrated video: clone → open in Unity Hub (title cards), then a real recording of the Unity Editor: open the scene, play a mission, edit gameplay numbers / text / difficulty / scoring, replace the logo, Player Settings, web build, tests | `Builds/SayHeyDash_Tutorial.mp4` |
| In-Editor guided tour | The same walk-through runs inside Unity with captions; restores the one value it edits | *Say Hey Dash ▸ Handover ▸ Play Editor Tour*, `Editor/Handover/EditorTour.cs` |
| Re-recordable video | Title cards, narration (built-in Windows voice), screen capture and captions from text files; encoded by Unity's own `MediaEncoder` (no extra software) | `Tools/Tutorial/` (`make_tutorial.ps1`, `tutorial_script.txt`, `cards.txt`), `Editor/Handover/TutorialVideoEncoder.cs` |

## 2. How the branding works

- **One source file.** `FastballFuel_Logo.png` is the client's file. *Say Hey Dash ▸ Branding ▸ Rebuild Logo Files + Apply* derives every other logo file from it: it finds the logo's edges, keeps a 4 % margin, turns the black background into transparency (brightness becomes alpha, so the brush texture and soft edges survive on any colour), and resizes with filtering for the icon sizes. It then sets Player Settings.
- **Splash timing on the web.** `BrandSplash` runs before Unity's splash would start and stops it (WebGL builds only). When the player clicks the start gate, the page sends `OnBrowserStart`; `BrowserEvents` asks `BrandSplashView` to show the logo (once per page load). Desktop / mobile app builds would show the Player Settings splash at start-up as usual; the Editor never shows Unity's splash, but `BrandSplashView.Play()` works in Play mode. Timing is on the *BrandSplash* object in the scene (fade in / hold / fade out / zoom).
- **Icon quality.** The supplied logo is 931 × 616 px; the 1024 px icon is slightly upscaled. A larger or vector logo from the client would make the icon sharper — drop it in and run the menu command.

## 3. Test results (batch mode, 2026-11-23)

| Suite | Result |
|---|---|
| EditMode: previous suites + `BrandingTests` (logo files exist, icon square, logo background transparent, Player Settings use the logo for icon and splash with no Unity logo, web page links the favicon / logo and starts the splash) | **106 / 106 passed** |
| PlayMode: previous suites + `BrandSplashPlayTests` (start click shows the logo above everything, mutes menu keys, the title stays put, ends by itself in 2.5–4.5 s, menus work again, only once per page load; skipping works) | **21 / 21 passed** (+ `ScreenGalleryCapture`, explicit, not run). `ThrowMeterRenderTests` / `ScreenGalleryCapture` now pick the game UI canvas by name (there are two root canvases now) |
| WebGL release build | **9.99 MB** compressed (M4: 9.80 MB; +0.2 MB for the splash logo). Loading screen with the logo checked in the browser pane |

## 4. Changes to existing behaviour

| Change | Why |
|---|---|
| Splash screen on (was off), client logo only | Client branding (M5) |
| Loading screen: logo image instead of the "FASTBALL FUEL COFFEE COMPANY PRESENTS" text line | Client branding |
| `WebGLBuildPipeline` applies names / icon / splash through `BrandingBuilder` | One place for branding |
| Main scene gains a *BrandSplash* canvas; `BrowserEvents` gains `splash` + `OnBrowserStart` | In-game logo splash after the start click |
| Runtime internals visible to the Editor assembly | The Editor tour drives the demo mission through the same test hooks as the PlayMode tests; nothing in a build can see them |

## 5. Still open in Milestone 5

- Real-device browser matrix: Firefox, Safari (macOS, iPhone), Chrome Android; touch controls, rotate screen and full-screen link on real phones; phone performance.
- Agreed HTTPS host + real Wix page embed (Q3/Q4); hide full screen where unsupported; decide whether to turn the Brotli decompression fallback off on the final host.
- Strip the two harmless URP "shader not supported" console lines; delete the M1 prototype code and scene.
- Client sign-offs: history copy and names (AC-11 / Q1), scoring formula (Q6), museum URL (Q5), developer credit line, "sunset arcade" palette, and now the splash / icon look.
- Final handover package: source, final build, documentation, video.
