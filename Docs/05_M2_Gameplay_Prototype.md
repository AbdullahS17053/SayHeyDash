# 05 — Milestone 2: Gameplay Prototype

**Status:** delivered for review (2026-11-18). Placeholder graphics and placeholder UI throughout. Styled screens, animations and sound come in Milestone 3.

## 1. GDD deliverables

| GDD deliverable | Where it is implemented | Verified by |
|---|---|---|
| Character movement | `OutfielderController`: keyboard, gamepad and on-screen stick, with acceleration, clamped to the field | PlayMode tests, WebGL build in the browser |
| Ball trajectory + landing indicator | `BallTrajectory` (deterministic arc, no randomness), `BallFlight` (ball + shadow), `LandingIndicator` (ring: **dashed** = outside, **solid** = inside) | EditMode trajectory tests, browser |
| Catching mechanic | `CatchSystem` + `CatchJudge`: stand in the ring, press Action in the timing window. Screen cues: GET READY… → NOW! | EditMode rules tests, PlayMode (caught / too early / too late / out of zone) |
| Throwing mechanic | `ThrowSystem` + `ThrowJudge`: a needle sweeps a meter; press while it is inside the notched target zone; 5 s time limit | EditMode rules tests, PlayMode (hit / miss), browser |
| Basic win/retry loop | `GameFlowController`: all 7 states plus a pause overlay. A miss shows TRY AGAIN (BALL MISSED / THROW MISSED + hint). Success shows the score, rank and runner outcome. Unlimited retries; Replay, Replay Mode and Back to Title | PlayMode: 25 consecutive missions, 12 fail→retry loops, pause/restart; browser |
| Placeholder graphics | M1 placeholder sprites, plus generated landing rings (`Tools/ArtGen`) | — |

Also delivered: the basic `ScoreCalculator` (Docs/01 §6 formula, pending Q6), Assisted mode toggle on the title screen (catch window ×2, ring ×1.25, throw zone ×1.5), and all text in `MissionContent`.

## 2. How a mission plays

1. **Title.** Press START (Enter/Space/tap). ASSISTED MODE can be switched on here.
2. **Mission objective.** Press CONTINUE.
3. **Track (Stage A).** The ball is launched at once. Run to the ring and follow the shadow. Presses are **ignored** until 0.65 s before the catch moment, so an early stray press while running never fails you.
4. **Catch (Stage B).** "GET READY…" appears (from here a press counts), then "NOW!" when the ±0.2 s window opens. Pressing outside the ring, too early or too late, or not pressing at all, gives BALL MISSED with a hint that matches the mistake.
5. **Catch sequence (04).** "OVER THE SHOULDER!" lasts 1.2 s. Action skips it. Reduced motion shortens it to 0.4 s; that switch is on `GameFlowController` until the settings menu arrives in M4.
6. **Throw (Stage C).** Press Action while the needle is between the notches. A hit goes to MISSION COMPLETE. A miss or the 5 s timeout gives THROW MISSED.
7. **Complete (06).** Shows the score breakdown, the rank (with stars as a non-colour cue) and "LEAD RUNNER HOLDS AT THIRD – NO RUN SCORES". Buttons: REPLAY or REPLAY MODE (07 is a placeholder until M4). From 07, BACK TO TITLE.

**Pause:** Esc or the **II** button. Time stops and RESUME / RESTART appear.

## 3. Tuning guide (no code changes needed)

Select the asset in the Project window and edit it in the Inspector. Changes apply on the next Play.

| Want to… | Asset → field |
|---|---|
| Make the outfielder faster/slower | `Data/Tuning/GameplayTuning` → `playerRunSpeed` (3.4), `playerAcceleration` (24) |
| Give more/less time to reach the ball | `ballFlightDuration` (3.6 s), or move `playerStartPosition` / `landingPoint` |
| Make positioning easier | `landingZoneRadius` (1.0 unit = 16 px; the ring scales with it) |
| Make the catch timing easier | `catchWindowSeconds` (0.4 s total) |
| Change when "GET READY" starts / presses count | `catchInputOpenSeconds` (0.65 s) |
| Make the throw easier | `throwMeterSpeed` (1.1 widths/s), `throwTargetZoneWidth` (0.16), `throwTimeLimitSeconds` (5) |
| Tune Assisted mode | `Data/Tuning/Difficulty_Assisted` multipliers |
| Change score maxima / rank thresholds | `Data/Scoring/ScoringConfig` |
| Change any on-screen text (incl. the generic-player variant, Q1) | `Data/Content/MissionContent` |

If the scene breaks: **Say Hey Dash → Build Main Scene** regenerates `Main.unity` from code. Data assets are never overwritten, so your tuning and text edits are kept.

## 4. Test results (batch mode, 2026-11-18)

| Suite | Result |
|---|---|
| EditMode (`Tests/EditMode`): score formula + rank boundaries, catch/throw rules, trajectory determinism, state-transition table | **51 / 51 passed** |
| PlayMode (`Tests/PlayMode`): 25 consecutive full missions (with Replay Mode → Title every 5th), 12 fail→retry loops covering every miss type, pause freezes the ball + restart, assisted mode widens the window, and the throw meter's zone is actually drawn (render check; needs a graphics device, so it is skipped with `-nographics`). Any logged error fails the test | **5 / 5 passed** |
| WebGL release build | 9.33 MB compressed (target ≤ 15 MB). Title → briefing → track → **catch** → throw → throw timeout → TRY AGAIN → retry, and ball-miss → TRY AGAIN with the matching hint, were all played in Chromium with no console errors |
| Fixed during review | (1) The throw meter's target zone was drawn before the throw started, so it had zero width. (2) A missed ball slid along the wall. (3) The zone's green fill did not show in WebGL: a resized image drawn over a same-material background stayed invisible. The meter is now a left track piece, the zone and a right track piece that do not overlap, laid out in pixels. All three fixes are covered by tests (`ThrowMeterRenderTests` renders the UI and checks the zone pixel colour). (3) is also confirmed in the WebGL build |

Run them from the Unity Test Runner (Window → General → Test Runner), or with the editor closed:
```bash
"D:\Gaming\6000.3.9f1\Editor\Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode.xml
```
```bash
"D:\Gaming\6000.3.9f1\Editor\Unity.exe" -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults Logs/playmode.xml
```

## 5. Known limitations of this prototype (by design: later milestones)

- The throw meter's green zone is the same green as the grass, so it reads like a window in the meter. The notches and the dark track pieces frame it, but M3 should restyle it for contrast.
- The UI is a single placeholder view (`PlaceholderFlowView`) using legacy uGUI Text and Unity's built-in font. Styled screens, pixel font and TextMeshPro come in **M3**.
- There are no animations yet. The catch "sequence" holds the ball in the glove, and the turn to throw is a sprite flip. **M3**.
- Screen 07 shows placeholder text and has no animated replay. The museum button stays hidden until the URL (Q5) is set in `MissionContent`. **M4**.
- The runner outcome is a line of text on Screen 06. A runner animation is planned for **M4**.
- Reduced motion and Assisted mode are not saved between sessions. Reduced motion is an Inspector checkbox. The settings menu and `SettingsService` come in **M4**.
- The on-screen stick and A button always show during gameplay; they will be shown on touch devices only in **M4**.
- There is no audio yet. **M3**.
- The M1 prototype (`Scripts/Prototype`, `Scenes/Prototype/M1_TechPoC.unity`, `Editor/Prototype`) is still in the project. It can be deleted once M2 is approved. **Do not** run *Rebuild M1 Tech PoC Scene*: it would make the PoC the only build scene again (fix with *Build Main Scene*).
