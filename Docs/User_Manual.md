# The Say Hey Dash — User Manual

*A Fastball Fuel Coffee Co. browser game. Recreate "The Catch" from the 1954 World Series: run down the deep fly ball, make the over-the-shoulder catch and fire it back to the infield.*

Sections 1–6 are for **players**. Section 7 is for the **website owner** (hosting, embedding in Wix, updating). Developers: see [Developer_Guide.md](Developer_Guide.md).

---

## 1. Starting the game
1. Open the game page. The Fastball Fuel logo and a loading bar appear (the first visit downloads about 10 MB; later visits load from the browser cache).
2. Click or tap **CLICK OR TAP TO PLAY**. This also turns on sound and keyboard control.
3. The Fastball Fuel Coffee Co. logo plays for a moment, then the title screen appears.

Works in current desktop browsers (Chrome, Edge, Firefox, Safari) and on phones and tablets held **sideways** (landscape).

## 2. Controls

| Action | Keyboard | Gamepad | Touch screen |
|---|---|---|---|
| Run | Arrow keys or W A S D | Left stick / D-pad | Drag the on-screen stick (bottom left) |
| Catch / throw / skip | Space | A (south button) | The round button (bottom right) — its label says CATCH or THROW |
| Confirm a menu button | Enter or Space | A or Start | Tap the button (or click with the mouse) |
| Move between buttons | Arrow keys or W A S D | D-pad / left stick | — |
| Back (close a menu) | — | B (east button) | Tap BACK |
| Pause | Esc | Select / View | **II** button (top right) |

If you click outside the game while it's embedded in a page, the game pauses and shows *CLICK GAME TO RESUME KEYBOARD CONTROL* — click the game to carry on.

## 3. How to play

1. **Title screen** — START begins the mission. SETTINGS, SOUND on/off, ASSISTED MODE and CREDITS are here too.
2. **Mission objective** — a short briefing and the controls. Press CONTINUE.
3. **Track the ball** — the ball is hit deep to centre field. Run towards the **landing ring** and follow the ball's shadow. The ring is *dashed* while you're outside it and turns *solid* when you're inside.
4. **Catch** — *GET READY…* appears, then **NOW!** Press catch exactly then, while standing inside the ring. Pressing too early or too late, outside the ring, or not at all means BALL MISSED.
5. **Over the shoulder!** — the famous catch plays out (press the action button to skip).
6. **Throw** — a needle sweeps across the throw meter. Press when it's inside the **notched zone** (5 seconds to decide).
7. **Mission complete** — your score, rank and what happened to the runners. Choose REPLAY to play again or REPLAY MODE to watch the history.
8. **Replay / history** — an animated diagram of the real 1954 play in six short steps. NEXT skips ahead, WATCH AGAIN restarts, BACK TO TITLE returns to the start.

Missed? The **TRY AGAIN** screen tells you what went wrong (too early, too late, out of position, missed throw) and how to fix it. Retries are unlimited.

## 4. Scoring and ranks

| Part | Up to |
|---|---|
| Positioning (how close to the landing spot) | 400 |
| Catch timing (how close to the perfect moment) | 300 |
| Throw accuracy (how close to the centre of the zone) | 300 |
| **Total** | **1,000** |

Every successful mission scores at least 500. **ROOKIE** 500–749 · **ALL-STAR** 750–899 · **LEGENDARY** 900–1,000. Ranks also show stars and a badge, not just a colour. The results screen shows your **best score on this device** (or NEW BEST!) and the score needed for the next rank.

## 5. Settings and accessibility

Open **SETTINGS** from the title screen or the pause menu.

| Setting | What it does |
|---|---|
| SOUND | All sound on / off (also on the title screen, the HUD and the pause menu) |
| MUSIC % / EFFECTS % | Separate volumes for music and for effects + crowd |
| REDUCED MOTION | Stops decorative movement (crowd, flags, blinking text, score count-up), shortens the catch scene and makes the replay jump instead of glide. On a first visit it follows your device's "reduce motion" setting |
| ASSISTED MODE | Wider catch timing window, bigger ring, wider throw zone. Can't be changed in the middle of an attempt |

Settings and your best score are saved **on this device only**. No personal information is collected. Some browsers block saving inside embedded pages; the Settings menu then says *settings last for this visit*.

Other accessibility features: everything can be played with keyboard only or touch only; all cues are words or shapes as well as colours; the only flashing element blinks once per second and stops with reduced motion; the history introduction and catch scene can be skipped.

## 6. Troubleshooting (players)

| Problem | Try this |
|---|---|
| Stuck on the loading bar | Refresh the page. Check the connection; the first load is about 10 MB |
| "Could not load the game files" | Refresh. If it keeps happening, tell the site owner (the upload may be damaged) |
| No sound | Click the game once; check SOUND is ON and the volumes in SETTINGS; check the device isn't muted |
| Keys do nothing | Click inside the game; the page may have taken keyboard focus |
| On a phone, the game is tiny or says "Please rotate your device" | Turn the phone sideways. When the game is inside another page, tap **OPEN FULL SCREEN** for a bigger view |
| Game paused by itself | It pauses when you switch tabs, click outside it or turn the phone upright. Press RESUME |

---

## 7. For the website owner: hosting and embedding

### 7.1 What you get
A finished web build is a folder (`index.html`, `Build/`, `TemplateData/`, sometimes `StreamingAssets/`), also delivered as a zip (e.g. `Builds/SayHeyDash_M4_WebGL.zip`). The logo, browser-tab icon and phone home-screen icon are already inside.

### 7.2 Host the game (once)
Wix can't run the game files itself; they go on a static web host with **HTTPS**, and Wix shows that page.
- Good options: Netlify, Cloudflare Pages, GitHub Pages, or itch.io for review links.
- Upload the **contents** of the build folder (so `index.html` is at the top level). No special server settings are needed.
- Open the game's address in a browser to check it loads and plays.

### 7.3 Put it on the Wix site
1. Wix Editor → **Add ▸ Embed Code ▸ Embed a Site** (or *HTML iframe ▸ Website address*).
2. Paste the game's HTTPS address.
3. Make the element **16:9** — e.g. 960 × 540 on desktop. The game letterboxes itself to fit any box, so it never gets cropped.
4. On Wix's mobile layout, make the element full width (it keeps 16:9). Phone players also get an **OPEN FULL SCREEN** link.
5. Publish and test: the game should load, take a click, and play with the keyboard without scrolling the page.

Known limits inside Wix: browser full-screen mode is usually blocked by Wix's own frame (the standalone page still works); iPhone Safari has no full-screen mode at all; saving settings may be blocked inside the embed (see §5).

### 7.4 Updating the game later
1. A developer makes the change and a new build ([Developer_Guide.md §4](Developer_Guide.md#4-build-for-the-web)).
2. Replace the files on the host with the new build folder. The Wix page doesn't need to change — it always shows whatever is at that address.
3. Players may need one refresh to get the new version.

### 7.5 Things you can ask a developer to change without code
Any text (including the history narration and credits), difficulty and timing, scoring thresholds, assisted-mode strength, sound levels, the museum / STORY MODE link, and the logo / icon / splash screen. See [Developer_Guide.md §3](Developer_Guide.md#3-change-the-game-in-the-unity-editor-no-code).
