# 06 — Milestone 3: Visual Production

**Status:** delivered for review (2026-11-19). All art and audio are original and still need the client's approval (palette Q2, names/likeness Q1). Previews: [outfielder sheet](img/m3_outfielder_preview.png), [stadium](img/m3_stadium_preview.png), [title](img/m3_title_preview.png).

## 1. GDD deliverables

| GDD deliverable | What was made | Where |
|---|---|---|
| Approved pixel-art character | One outfielder, 32×32 cells, 1950s flannel uniform (off-white, red socks and belt), dark cap, leather glove. No logos or numbers. Submitted for approval | `Art/Sprites/Character/` · sheet `Docs/img/m3_outfielder_sheet.png` |
| Stadium environment | Polo Grounds–style centre field: deep green wall with the **483** marker, warning track, the centre-field clubhouse with its scoreboard (VIS 2 · HOME 2 · TOP 8 · OUT 0), bleachers, light towers, a small memorial stone in deep centre, widening mowing bands | `Art/Sprites/Environment/` |
| Animations | Outfielder: idle (4 frames), run (6), over-the-shoulder run with the glove up and eyes back on the ball (6), catch (4), missed catch (3), spin to the infield (3), throw set + throw and follow-through (5). Ball spin (4). Crowd shuffle and cheer (3), waving flags (2), title-screen runner, blinking PRESS START, score count-up | `Scripts/Runtime/Visuals/` · `Data/Visuals/OutfielderAnimations.asset` |
| Title and mission screens | 01 Title: sunburst backdrop, pixel "THE CATCH" logo, the GDD lines, blinking PRESS START, START / SOUND / ASSISTED MODE / CREDITS, credits overlay. 02 Mission objective: scoreboard board with the situation, objective, short setup, controls, CONTINUE | `UI/TitleScreenView`, `UI/BriefingScreenView` |
| HUD, menus and results screens | HUD: prompts, GET READY / NOW cues, in-zone label, situation chip, sound and pause icons, restyled throw meter with word feedback (PERFECT THROW! / GOOD THROW! / WIDE! / TOO SLOW!), OVER THE SHOULDER! banner. Pause menu (RESUME / RESTART / SOUND). 05 TRY AGAIN, 06 MISSION COMPLETE (count-up, rank badge with stars + word), 07 Replay screen styled (content M4). Pixel buttons with a cursor arrow on the focused one | `UI/HudView`, `PauseMenuView`, `MissionFailedView`, `MissionCompleteView`, `ReplayScreenView`, `PixelButton` |
| Initial sound implementation | 14 original cues: bat crack, crowd bed + cheer, GET READY / NOW beeps, glove pop, whiff, throw whoosh, success and fail jingles, title music loop, menu move / confirm / back. Mute toggle on the title, pause menu and HUD | `Audio/AudioService`, `Data/Audio/AudioCueLibrary.asset`, `Assets/_Project/Audio/` |

## 2. How the pieces fit

- **Art is generated from code.** `py -I Tools/ArtGen/generate_m3_art.py` redraws every sprite, the font atlas and the preview sheets. The outfielder is a small pose rig: each frame is a list of joint positions, and each body part is drawn as capsules with one shared 1 px outline. Changing a pose or a colour updates every frame the same way.
- **Animation without Unity's Animator.** `SpriteAnimator` plays flip-book clips (`SpriteAnimationClip`: sprites + fps + loop) on a SpriteRenderer or a UI Image. `OutfielderAnimator` picks the clip from the game state and the catch/throw events. It is presentation only and never changes gameplay.
- **One view per screen.** Each screen is a `ScreenView` that shows its panel in its own state(s) and calls `GameFlowController` commands from its buttons. No game logic lives in the UI. `PlaceholderFlowView` (M2) is deleted.
- **Pixel grid UI.** The canvas is 1280×720 and the layout is authored on the 320×180 art grid (1 art pixel = 4 canvas units), so UI pixels match game pixels. Sprites use 9-slice borders from their file name (`*_slice3.png`).
- **Original typeface "Say Hey Pixel".** 5×7 capitals with a bold face, built from the generated atlas into two Unity custom fonts (`Editor/Fonts/PixelFontBuilder`). In the font, `*` is drawn as a star, so rank text like `*** LEGENDARY` shows stars.
- **Sound is event-driven.** `AudioService` listens to state changes, catch cues and throw results and plays from `AudioCueLibrary`. There are three sources: SFX, music/jingles and the crowd bed. Pause pauses the music and the crowd.

## 3. Editing guide (no code changes needed)

| Want to… | Do this |
|---|---|
| Swap or retime an outfielder animation | `Data/Visuals/OutfielderAnimations` → frames / `framesPerSecond` / `loop` |
| Redraw the art | Edit `Tools/ArtGen/generate_m3_art.py` (poses, palette, stadium), run it, then **Say Hey Dash → Build Main Scene** |
| Change a sound or its volume | `Data/Audio/AudioCueLibrary` → clip / volume per cue. Regenerate the originals with `py -I Tools/AudioGen/generate_audio.py` |
| Change any text (credits, labels, feedback words) | `Data/Content/MissionContent` |
| Add a character to the font | Add it to `GLYPHS` in the art script, rerun it, then **Say Hey Dash → Rebuild Pixel Fonts**. The EditMode test `Font_CoversEveryCharacterInMissionContent` lists any text the font cannot draw |
| Freeze decorative animation | `GameFlowController` → Reduced Motion (a settings menu toggle comes in M4) |

## 4. Changes to existing behaviour

| Change | Why |
|---|---|
| `catchHeight` 0.65 → **1.1** (glove height of the over-the-shoulder pose) | At 0.65 the ball arrived at knee height, below the glove. Timing is unchanged: the perfect moment is still `ballFlightDuration` |
| The ball sprite hides while it is held, and the throw starts from the throwing hand | The catch and spin frames draw the ball in the glove |
| Throw meter: yellow zone, off-white notches, red needle (was green zone on the grass green) | The M2 zone did not stand out from the field (Docs/05 §5) |
| HUD sits in the sky band (top 30 rows) | The fly ball never rises above row ~37, so prompts never hide the ball |
| Controls help on Screen 02 is three lines | Readable at the pixel font size |

## 5. Test results (batch mode, 2026-11-19)

| Suite | Result |
|---|---|
| EditMode: M2 suites + frame timing, font covers every on-screen string, every cue has a clip, every animation has frames, throw feedback words | **70 / 70 passed** |
| PlayMode: the M2 mission loop suites (25 missions, 12 fail→retry, pause, assisted, throw-meter render) + `PresentationTests` (exactly one screen per state, animation follows the play, ball in the glove until the throw, run → idle, mute silences every source and updates every label) | **9 / 9 passed** |
| WebGL release build | **10.26 MB** compressed (target ≤ 15 MB; M2 was 9.3 MB). Upload zip `Builds/SayHeyDash_M3_WebGL.zip` is 9.74 MB. Title → objective → gameplay → TRY AGAIN checked in Chromium at 1280×720, no console errors. The catch, spin, throw, results, pause, credits and replay screens were checked with `ScreenGalleryCapture` renders (`Docs/img/m3_screens/`) |

## 6. Known limitations (later milestones)

- **Art and palette need client approval** (Q1, Q2). The name lines still read WILLIE MAYS / POLO GROUNDS from `MissionContent`, so the generic variant is a text edit.
- **Art source files:** the GDD asks for "original art files". Ours are the generator script plus PNG frames and sheets. Aseprite files are not produced; if the client's artists want them, the PNGs import into Aseprite directly.
- Mute, assisted mode and reduced motion are not saved between visits; there is no settings screen yet (M4 `SettingsService`).
- On-screen stick and A button always show; they will be shown on touch devices only (M4).
- Screen 07 still has placeholder history text and no animated replay (M4). The runner outcome is a line of text (M4 animation).
- Credits show "[DEVELOPER CREDIT - TO CONFIRM]" until the client says how the developer should be credited.
- Sound is a first pass; mixing and levels come in M4 ("Sound mixing and settings").
- The font has capitals, digits and common punctuation only; lower-case letters draw as capitals.
