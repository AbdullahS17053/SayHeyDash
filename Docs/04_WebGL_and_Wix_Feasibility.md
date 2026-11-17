# 04 — WebGL Export & Wix Embed Feasibility Report

**Milestone 1 deliverable** · tested 2026-11-17 · Unity 6000.3.9f1 · build: Release, `M1_TechPoC.unity`

**Verdict:** ✅ **Feasible.** The Unity WebGL build runs, takes keyboard, mouse and touch-style input, unlocks audio, and works inside a cross-origin iframe the way Wix embeds content, without scrolling the host page. Two things are still open: the test on the **real Wix page** (we need an HTTPS host first, Q3/Q4) and the **real-device browser matrix** (Safari, iPhone, Android). Neither is blocked technically.

---

## 1. WebGL export configuration (applied in code by `WebGLBuildPipeline`)

| Setting | Value | Reason |
|---|---|---|
| Linker target | Wasm (WebGL 2) | Only supported target in Unity 6 |
| Compression | **Brotli** + **decompression fallback** | Smallest download. The fallback lets it run on hosts that don't send `Content-Encoding` (the loader decompresses in JS). M5: once the host is configured, turn the fallback off for faster start-up |
| Exceptions | Explicitly thrown only (release) / full stack trace (development) | Size vs debuggability |
| Managed stripping | High, engine code stripping on | Size |
| IL2CPP code generation | Optimize Size | Size |
| Data caching | On (IndexedDB) | Repeat visits load faster |
| Splash screen | Off (optional for Unity Personal since Unity 6) | Faster start; can be re-enabled |
| Web template | `PROJECT:SayHeyDash` (custom) | Responsive, iframe-safe (see §3) |
| DPR cap | `min(devicePixelRatio, 2)` | Pixel art is upscaled from 320×180, so a higher DPR only costs GPU time on phones |

## 2. Measured results (local, Chromium-based browser pane, Windows 10 desktop)

| Metric | Target (GDD) | Measured | Status |
|---|---|---|---|
| Compressed download | ≤ 15 MB | **9.30 MB** (wasm 6.08 MB, data 3.26 MB, framework 0.07 MB, loader 0.12 MB) | ✅ |
| Time to "click to play" | ≤ 10 s on broadband | **2.98 s** locally, including JS Brotli decompression. Over the network, add about 9.3 MB / bandwidth (≈ 1.5 s at 50 Mbps) | ✅ (re-measure on the real host) |
| Desktop frame rate | 60 FPS | **59.8 FPS** (16.7 ms) at 1280×720 | ✅ |
| Console errors | none persistent | No script errors. Two engine log lines: `Hidden/CoreSRP/CoreCopy` and `Hidden/Universal/HDRDebugView` "not supported on this GPU", plus `INVALID_ENUM getInternalformatParameter` warnings during format probing | ⚠️ harmless engine noise, to be cleaned in M5 |
| Build time | — | 20 min first build (shader + IL2CPP cache); about 1–7 min afterwards | — |

> Note: download size is mostly engine runtime + URP. The final game adds roughly 0.5–1.5 MB of pixel art and audio, so it stays well under 15 MB.

## 3. Web template features (`Assets/WebGLTemplates/SayHeyDash`)

- Fills **any** box (full page or iframe), **letterboxed to 16:9, never cropped** (`min()`-based sizing, `dvh` on mobile).
- Branded loader (palette colours, progress bar). No external fonts, scripts or CDNs, so it works under strict CSP and sandboxed iframes.
- **"CLICK OR TAP TO PLAY" gate** after loading. This is a real user gesture inside the iframe, so it:
  1. moves keyboard focus into the iframe (`window.focus()` + `canvas.focus()`), and
  2. lets the browser unlock WebAudio (verified: *"Audio context resumed"* in the console).
- **Refocus hint:** when the player clicks elsewhere on the Wix page, a "CLICK GAME TO RESUME KEYBOARD CONTROL" banner appears. Clicking the game restores control.
- Touch hygiene: `touch-action: none`, no context menu, no pinch-zoom/scroll on the canvas.
- Blink animation at 1 Hz; disabled under `prefers-reduced-motion`.

## 4. Iframe / Wix simulation (`Tools/WixHarness`)

The harness is a fake "Wix page" on `localhost:8602` with scrollable content. It embeds the game from `localhost:8601`, a **different origin**, like Wix does. Iframe size, `sandbox` flags and `allow="fullscreen"` can be switched. Every configuration loads in a fresh iframe.

| Test | Result |
|---|---|
| Loads in a cross-origin iframe, `sandbox="allow-scripts allow-same-origin allow-popups allow-forms"` | ✅ |
| Loads with strict `sandbox="allow-scripts allow-same-origin"` (no popups, no fullscreen) | ✅ |
| Diagnostics report `IFRAME YES` | ✅ |
| After clicking the gate, keyboard goes to the game (Space registered, held key moved the outfielder) | ✅ |
| **Space / arrow keys do NOT scroll the host page** (host `scrollY` unchanged: 821 → 821) | ✅ |
| Click outside the iframe → refocus hint → click game → control returns | ✅ |
| On-screen **A** button and virtual stick drive the same actions (shows as virtual `Gamepad`) | ✅ (A button confirmed by a real click) |
| "Open link" from inside the iframe (needed for the Screen 07 museum button) | ✅ `window.open` is triggered on the user's pointer-up (gesture-safe). The final popup was blocked only because the click was automated; **needs one human click to confirm** |
| Fullscreen | ⏳ Needs a human click (browsers require a real gesture). Expected: works only when the iframe has `allow="fullscreen"` |
| Sizes 960×540, 640×360, 360×640 portrait | ✅ Letterboxed correctly, nothing cropped. ⚠️ Portrait phone boxes leave the game very small (see §6) |
| `sandbox="allow-scripts"` only (opaque origin) | ⏳ Could not be tested here: the Claude browser pane blocks this frame type (`ERR_BLOCKED_BY_CLIENT`). Test in Chrome/Firefox (steps in §7). The local server already sends `Access-Control-Allow-Origin: *`, which this case needs |

## 5. Wix integration options (recommendation order)

Wix **cannot host** the Unity build itself (it needs static hosting for `.wasm`/`.data` files). The build is hosted on an HTTPS static host, and Wix only embeds it.

| Option | How | Pros | Cons |
|---|---|---|---|
| **A. Wix "Embed a site" / HTML iframe element (website address)** — recommended | Wix Editor → Add → Embed Code → *Embed a Site* (or *HTML iframe* → "Website address") → paste the game URL. Size the element 16:9 (for example 960×540 desktop) | Simplest; no code; game updates by re-deploying the host | Wix controls the outer iframe attributes: fullscreen is **likely unavailable** (community reports say Wix's outer iframe lacks `allowfullscreen`); mobile layout needs separate sizing |
| B. Wix "Embed HTML" (custom code) with our own `<iframe … allow="fullscreen; autoplay">` | Paste a small snippet | Lets us set `allow` attributes on *our* iframe | Wix wraps custom code in its own sandboxed iframe. Reports say it is moving to stricter sandboxing (possibly dropping `allow-same-origin`) |
| C. **Standalone page fallback** (GDD-approved fallback) | A Wix button "PLAY THE SAY HEY DASH" opens `https://<host>/` full-page (same tab or new tab) | Always works: full screen, best on phones, no focus issues | Leaves the Wix page |

**Recommendation:** use **A** on desktop with an in-game fullscreen button that falls back gracefully, and **C** as the mobile experience (a "Play full screen" link on the Wix mobile layout). Final choice is made in M5 after the live test.

### Hosting requirements for the chosen host (M4/M5)

- HTTPS.
- Must allow framing by the Wix domain: no `X-Frame-Options: DENY`; if CSP is used, `frame-ancestors https://*.wixsite.com https://<client-domain>`.
- `Content-Type: application/wasm` for `.wasm` (only matters if the decompression fallback is turned off).
- If fallback is off: serve `.br` files with `Content-Encoding: br` (or switch to gzip).
- `Access-Control-Allow-Origin: *` on `/Build/*` (harmless, and required if Wix's sandbox drops `allow-same-origin`).
- Good candidates: Netlify, Cloudflare Pages, GitHub Pages (no custom headers, so keep decompression fallback on), itch.io (has its own iframe wrapper; not ideal for embedding inside Wix).

## 6. Findings, limitations & risks

| # | Finding | Impact | Plan |
|---|---|---|---|
| F1 | The game needs a user click/tap before keyboard and audio work inside an iframe | Expected browser behaviour | Solved by the start gate, which doubles as "PRESS START" (M3 merges it with Screen 01) |
| F2 | Fullscreen inside a Wix embed is likely blocked by Wix's outer iframe | No fullscreen on desktop embed | Hide the fullscreen button when unsupported (`document.fullscreenEnabled`) + standalone-page link |
| F3 | Portrait phone embeds are tiny (16:9 game in a tall box) | Poor mobile UX | M4: "rotate to landscape" overlay + mobile "play full screen" link (option C) |
| F4 | iPhone Safari has **no Fullscreen API** for elements | No fullscreen on iPhone | Standalone page + `viewport-fit=cover`; acceptable per GDD (disclose limitation) |
| F5 | iOS Safari WebGL memory limits | Possible crashes with large builds | Build is small (9.3 MB); keep initial memory modest; test on device in M5 |
| F6 | Storage (PlayerPrefs → IndexedDB) may be partitioned or blocked in third-party iframes (Safari ITP, Chrome partitioning) | Mute/assisted/reduced-motion settings may not persist between visits when embedded | `SettingsService` (M4) treats storage as best-effort; defaults are sensible; no personal data stored |
| F7 | URP prints two "shader not supported" lines on WebGL | Console noise against "no persistent console errors" | M5: strip HDR debug / CoreCopy variants or switch these features off in URP assets |
| F8 | On-screen controls overlap the play area at the far right/left | Visual clash when the player reaches the edge | M4: show touch controls only on touch devices; keep field bounds clear of them |
| F9 | Legacy uGUI `Text` used in the PoC | None (PoC only) | M3: TextMeshPro with OFL pixel font |
| F10 | `window.open('noopener')` always returns `null` | Would have produced false "popup blocked" warnings | **Fixed** in `BrowserBridge.jslib` (opener detached manually) |

## 7. What the client / team must do next (cannot be automated from here)

1. **Pick a host (Q3)** and deploy `Builds/WebGL/` (drag-and-drop to Netlify Drop or Cloudflare Pages works for a test).
2. **Real Wix test (Q4):** on an unpublished Wix test page, add *Embed a Site* with the hosted URL, preview and publish, then run the checklist below in each browser.
3. **Browser matrix** (GDD minimum): open the hosted URL and the Wix page on Chrome desktop, Firefox desktop, Safari macOS, Safari iPhone, Chrome Android. Fill in this table:

| Check | Chrome Win | Firefox | Safari macOS | Safari iPhone | Chrome Android |
|---|---|---|---|---|---|
| Loads ≤ 10 s, no error banner | ✅ (local) | | | | |
| Start gate → keys/touch work | ✅ (local) | | | | |
| Host page doesn't scroll | ✅ (sim) | | | | |
| Audio after first tap | ✅ (local) | | | | |
| On-screen stick + A button (touch) | n/a | n/a | n/a | | |
| FPS (diagnostics panel) | 59.8 | | | | |
| Open-link button opens new tab | ⏳ | | | | |
| Fullscreen (embed / standalone) | ⏳ | | | | |
| Harness `sandbox=allow-scripts` loads | ⏳ | | | | |

Local harness for these checks: start `serve.py` twice (see README), then open `http://localhost:8602/?sandbox=allow-scripts&fs=0` in Chrome and Firefox.

## Sources consulted

- Unity Manual — Web input (keyboard capture in iframes, `captureAllKeyboardInput`): <https://docs.unity3d.com/Manual/webgl-input.html>
- Unity Issue Tracker — WebGL canvas focus in iframes (`tabindex="-1"` fix): <https://issuetracker.unity3d.com/issues/webgl-unity-canvas-containing-webgl-player-unable-to-gain-focus>
- Shapespark forum — Wix outer iframe lacks `allowfullscreen`: <https://forum.shapespark.com/t/fullscreen-button-not-available-when-embedding/1703>
- Wix Studio forum — Wix sandboxing custom elements/iframes: <https://forum.wixstudio.com/t/wix-is-sandboxing-custom-element-and-iframe-extensions/65088>
- Wix Studio forum — HTML iframe element behaviour on mobile: <https://forum.wixstudio.com/t/html-iframe-element-on-mobile-is-it-possible-to-control-fullscreen-behavior-or-iframe-sizing/76611>
