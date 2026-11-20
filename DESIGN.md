# Lynxos Bowling — real Unity game (v1.3.0)

Built Nov 19–20, 2026. Hand-written C#, no Editor was available (license block),
so everything is built at runtime from code — no scene edits, no prefabs,
no asset files.

## What was built

Real 3D bowling in Unity 6000 (URP), package com.lynxosbowling.app:

- **Core (v1.0 carryover, rebuilt in real 3D):** 3D lane (18.3 m, real scale),
  10 pins as rigidbodies with real physics, hold OK to wind up / release to
  throw, power meter read aloud, D-pad aim (Up/Down, in boards) + hook spin
  (Left/Right), real 10-frame scoring (strikes, spares, 10th-frame rules),
  scoreboard, full voice narration (Android TTS), synthesized sounds (ball
  roll, pin crash, gutter, cheer, clicks), best score in PlayerPrefs,
  gold #F5C518 on dark #0A0A0A, blind-first (everything spoken + mirrored
  in big HUD text, no TalkBack needed).
- **Extras:** Practice mode (endless throws, full rack resets each throw);
  Spare Practice (7-10 split, 4-7, 6-10, 3-10, 2-4-5, 1-2-4, bucket
  2-4-5-8); 2-player pass-and-play (separate scores, alternate frames,
  winner announced); ball choice — Heavy 16 lb (power), Bouncy 13 lb
  (hook), Accurate 14 lb (straight) as starters, plus unlockable balls,
  colours and alley looks in the Gear Shop (see v1.2.0); Bumpers mode
  (invisible walls, no gutter); spoken aim helper ("3 boards left of
  center"); strike celebrations ("Strike!", "Turkey! Three strikes in
  a row!" + crowd cheer).
- **v1.2.0:** spoken 3-step throw, Quick Match, spoken ball-cam, 20-second
  turn timer, daily rewards + missions, 10 achievements, unlockable balls
  and gear, all earned by playing (see the v1.2.0 section below).
- **v1.3.0:** camera follow + closer framing, lit shader with shadows,
  darker lane so the gold ball reads, finger holes + shadow blob on the
  ball, spare-practice announce fix, controls hint back (re-recorded for
  the 3-step throw), two-player parity, rebuilt power meter, menu
  makeover with an animated 3D bowler and wearable gear (see the v1.3.0
  section below).
- **Deferred (not built):** touch controls for tablet play.

## Files (repo-relative paths — upload to these exact paths)

Line counts verified Nov 20, 2026 with `wc -l` (v1.3.0).

| File | Lines | What it does |
|---|---|---|
| Assets/Scripts/GameBootstrap.cs | 288 | RuntimeInitializeOnLoadMethod: clears the template scene, creates the game root. Holds BowlingConfig (dimensions, pin layout, ball specs, skins, themes, shirts, shoes, accessories, spare setups) and FindGameShader (GameLit first, GameUnlit fallback, Shader.Find last). |
| Assets/Scripts/Scoring.cs | 243 | Pure 10-pin scoring: frames, strikes/spares, 10th frame, display marks. Generalised with TotalFrames (10 or 3 for Quick Match). |
| Assets/Scripts/Narration.cs | 216 | Android TextToSpeech wrapper, fully guarded (no-ops safely off Android). TalkBack detection (isTouchExplorationEnabled); SpeakUIKey skipped under TalkBack. |
| Assets/Scripts/SoundSynth.cs | 283 | Procedural audio clips (crash, gutter, cheer, click, spare, roll loop, music loop), normalised to ~0.9 peak. |
| Assets/Scripts/LaneBuilder.cs | 471 | Lane, gutters, pit, bumpers, decor, lights, camera — all runtime-built. Owns the camera and its Aim/Follow/Return modes (v1.3.0). |
| Assets/Scripts/PinManager.cs | 218 | 10 pin rigidbodies, racking, fallen detection, dead-wood removal. |
| Assets/Scripts/BallController.cs | 258 | Ball physics, throw, hook force, gutter detection, pin-hit reporting, finger holes, shadow blob, aim guide. |
| Assets/Scripts/BowlerCharacter.cs | 271 | NEW in v1.3.0: the menu bowler — primitive-built 3D character with procedural idle animation and wearable gear. Menu-only. |
| Assets/Scripts/BowlingUI.cs | 1014 | All uGUI: menus, HUD, scoreboard strip, game over, D-pad navigation, buttons speak their labels on focus. Power meter rebuilt in v1.3.0; Gear Shop is two-level (categories, then items). |
| Assets/Scripts/BowlingGameManager.cs | 1291 | Modes, turns, 3-step throw, rolling, resolving, camera-follow wiring, slow motion, narration flow, gear purchases, daily rewards/missions/achievements. |
| Assets/Scripts/GameProgression.cs | 523 | Coins, balls, skins, themes, shirts, shoes, accessories (LB12_-prefixed PlayerPrefs), missions, achievements, daily reward. |
| Assets/Scripts/VoiceBank.cs | 120 | Baked Piper voice clips (Resources/Voices/), queued one at a time, PlayNow interrupts for power bands. |
| Assets/Resources/Shaders/GameLit.shader | — | NEW in v1.3.0: URP lit shader (Lambert diffuse + Blinn-Phong shine + shadow receiving) with _Color/_Smoothness. Loaded via Resources.Load; GameUnlit is the fallback. |
| Assets/Resources/Shaders/GameUnlit.shader | — | Flat URP unlit shader (v1.1.5). Kept as the fallback if GameLit fails to load. |
| Assets/Resources/Voices/ | 74 clips | Piper en_US-ryan-medium .ogg clips. Orphaned ball_light, ball_standard, winding_up deleted in v1.3.0; aim_help re-recorded for the 3-step throw. |

Total: 5,196 lines of C# (12 scripts).

## Upload

Copy each file to the same repo-relative path in
justinyocum4-code/Lynxos-Bowling-Unity via the GitHub web UI (create the
Assets/Scripts/ files; the repo already has Assets/, Packages/,
ProjectSettings/ from the template). No other repo files change.

## Version bump (ProjectSettings — do NOT edit by hand in the file list)

- `ProjectSettings/ProjectSettings.asset`: bundleVersion **1.0 → 1.1**.
- versionCode is auto-set by Unity Build Automation = build number.
  Next build is **#5 → versionCode 5**.

## Expected Build #5 flow

Build Automation → Build History → blue Build button → Default Android →
Build (incremental). Then download the APK, verify (versionName 1.1,
versionCode 5, signed with the Lynxos keystore), publish GitHub Release
tag **v1.1+5**, Store picks it up automatically from the latest release
(no catalog.json change needed — the entry already exists).

## Controls (Onn remote)

- Menu: D-pad moves, OK selects, Back goes back. Buttons speak on focus.
- Game: Up/Down = aim (1 board per press, hold to repeat), Left/Right =
  hook, hold OK = wind up, release OK = throw, Back = quit to menu.
- Note: aim is lateral position in boards (changed from v1.0's angle
  control — clearer in 3D; angle comes from aim + hook).

## Tuning that may need on-device adjustment

- Throw speed range 4–10 m/s; hook acceleration 0.10 m/s² per spin step;
  pin mass 1.5 kg; ball masses 5.44 / 6.35 / 7.26 kg; lane/gutter
  friction 0.02 (deliberately low — at 0.05 a soft throw stopped short of
  the pins); ball drag 0.03. If pins barely move, raise ball speed or
  lower pin mass; if the hook curves too much/little, adjust
  HookAccelPerStep.

## v1.1.1 — black-screen + missing TV icon fix (Nov 19, 2026)

- **What Justin saw on v1.1+8:** Unity logo, then a black screen, no voice
  at all. No voice means startup died before the menu ever ran. Root cause
  class: the bootstrap destroys the template scene's camera, and v1.1 built
  its own camera LAST inside the lane build — so any throw in the materials
  or lane left no camera at all. The material builders also did
  `new Material(shader)` with no null check, so a missing shader on device
  throws; the same unguarded pattern existed in PinManager and
  BallController. The menu panel was also 97% opaque black, and its text
  depended on device font lookups that came back empty on the TV.
- **Startup hardening:** camera + lights are built FIRST in LaneBuilder;
  every build step runs in its own try/catch (lane steps via TryStep,
  manager subsystems via TryBuild); all three material builders return null
  instead of throwing and callers keep the primitive's default material;
  BallController ResetBall/ThrowBall and all SoundSynth methods are null-safe;
  every UI show/update method is null-safe. Any failure is collected and
  shown in a big error line on screen AND spoken ("Startup problem. ..."),
  so a broken start can never again fail silent.
- **Font:** DejaVuSans.ttf ships in Assets/Resources/ and is loaded with
  Resources.Load first; built-in LegacyRuntime/Arial and OS fonts are only
  fallbacks. A FontMissing flag is reported as a startup error.
- **Menu:** background alpha 0.97 → 0.55 so the lane shows behind it;
  buttons 0.12 → 0.28 gray so they read even without text.
- **TV launcher icon:** v1.1's APK manifest had only LAUNCHER + MAIN —
  no LEANBACK_LAUNCHER, no leanback feature, no banner — so Google TV never
  listed the game. Added Assets/Plugins/Android/AndroidManifest.xml
  (LEANBACK_LAUNCHER + leanback required=false). A 320x180 gold-on-black
  banner (app_banner.png) was tried first, but Unity 6000 removed raw
  `res/` resources under Assets/Plugins/Android/ and Build #9 failed on
  it ("OBSOLETE - Providing Android resources in Assets/Plugins/Android/res
  was removed, please move your resources to an AAR or an Android Library").
  The banner was dropped for v1.1.1 (Build #10): the game lists on TV via
  the leanback entry with a default banner. A gold banner would need an
  AAR — deferred.
- **Version:** bundleVersion 1.1.1; versionCode auto = build number (9).

## v1.1.2 — menu won't select (Nov 19, 2026)

- **What Justin saw on v1.1.1+10:** menu pops up AND the game icon now
  shows in the TV apps list (leanback fix confirmed on device) — but no
  button ever highlights and OK does nothing.
- **Root cause:** the Unity 6000 template ships `com.unity.inputsystem`
  and sets `activeInputHandler: 1` (new Input System only) in
  ProjectSettings.asset. The whole game is written against the OLD input
  system (`Input.GetKey*` everywhere + the legacy `StandaloneInputModule`
  for menus). With handler 1 the old input is dead, so the remote's
  D-pad presses never reach the menu — or the aiming code either.
  The EventSystem, buttons, navigation wiring and highlight colors were
  all correct; the input itself was switched off at the project level.
- **Fix:** one setting, no code change — `activeInputHandler: 1` → `0`
  (Input Manager) in ProjectSettings.asset, + bundleVersion 1.1.1 →
  1.1.2. The template's InputManager.asset already has the standard
  axes (Submit = Return / joystick button 0, Vertical = up/down),
  so menus and aiming both come alive. The Input System package stays
  installed but unused.
- **Lesson for every future Unity project from this template:** check
  `activeInputHandler` FIRST whenever input feels dead. Unity 6000
  templates default to the new Input System; a game written for the
  old Input Manager needs handler 0 (or 2 = Both).

## v1.1.3 — ball invisible + empty background + tablet play (briefed Nov 19, 2026)

- **What Justin saw on v1.1.2+11 (Practice mode):** "I can't do anything. I don't see a bowling ball or anything, just the lane." Menu navigation now works (he reached Practice), so the v1.1.2 input fix is confirmed on device. `ShowHUD()` deselects the menu button, so no hidden button can eat the OK press.
- **Root cause 1 — the ball is below the screen edge (verified by geometry, not a guess):** camera at (0, 2.8, -3.6) looking at (0, 0, 14), vertical FOV 55° (half-FOV 27.5°). The ball sits at (0, 0.108, -0.6), which is 32.9° off the view centre — outside the frame. The bottom edge of the screen meets the lane floor at z = +0.18, just past the foul line; the ball at z = -0.6 never appears. The throw/aim input path is almost certainly fine (OK reaches the menu, so Submit works) — with no visible ball and no visible change, it felt like nothing works.
- **Root cause 2 — the ball is nearly black** (0.08, 0.08, 0.10) in a dark scene; even in frame it would barely read against the wood lane.
- **Root cause 3 — the background is a void:** the camera's background is near-black and the lane floats in empty dark space — no walls, no ceiling, nothing around it. Justin wants it to look like being inside a real bowling alley.
- **Root cause 4 — TOTAL silence (game sounds + voice), confirmed in code:** there is NO AudioListener anywhere in the game (grep: zero matches). GameBootstrap destroys every template scene root — including the template camera that carried Unity's AudioListener — and the new camera built in LaneBuilder has no AudioListener added. With no listener, every AudioSource (clicks, crash, cheer, roll, spare) is muted by design, no matter how correct SoundSynth is. The narration voice (Android TTS, bypasses Unity audio) is ALSO silent on his TV — he reported no voice in v1.1+8 AND no accessibility sounds in v1.1.2, so TTS has never spoken on this device across three versions; Narration.cs swallows every failure silently (guarded, no on-screen report), so the cause is unconfirmed — possible TTS-engine/init failure on the Onn TV. Music genuinely never existed (no music system was ever built).
- **Planned fix (v1.1.3, Build #12 — JUSTIN SAID GO Nov 19 ~6:20 PM, "go ahead and build everything"):** (1) reframe the camera (0, 1.7, -4.6 looking at (0, 0.4, 12) — ball now 17.2° off centre, in frame, ~56 px tall at 1080p, verified numerically) + bright gold ball + gold aim guide strip on the lane; (2) build a real indoor alley in LaneBuilder — floor, side walls with wood paneling + gold trim, ceiling with light panels, widened pin-deck back wall + masking unit, neighbouring-lane strips, ball return rail with a spare gold ball, extra ceiling light, warmer ambient; (3) tablet touch controls — on-screen aim ◀ ▶ and hook ◀ ▶ buttons + a big HOLD TO THROW button (press = wind up, release = throw), swipe on the lane to aim; touch panel shown only when touch is available, TV layout unchanged; (4) audio — AudioListener added to the main camera (unmutes ALL game sounds), procedural background music loop in SoundSynth (Am–F–C–G pad + bass, 8 s, quiet 0.16 volume, starts at launch), baked Piper voice clips (en_US-ryan-medium, 32 .ogg files, 339,707 bytes in Assets/Resources/Voices/) for every fixed spoken line via new VoiceBank.cs + Narration.SpeakKey (menus, buttons, strike/spare/gutter/turkey/winding up/ball away), TTS kept for dynamic lines (aim, scores) with setLanguage fallback chain (US → ENGLISH → default) and failures now REPORTED on screen (Narration.LastError, checked 2.5 s after start) instead of swallowed. **Files changed:** LaneBuilder, BallController, SoundSynth, Narration, BowlingUI, BowlingGameManager, NEW VoiceBank.cs, 32 clips, bundleVersion 1.1.3. **Pre-upload verification passed:** brace/paren/bracket balance clean on all 10 scripts; undefined-name sweep clean (only delegate/lambda/Unity-API false positives); camera geometry verified; all 32 clips present, valid OggS.
- **Lesson:** always verify the ball/camera framing numerically before shipping — compute the angle from the view centre to the ball against half the FOV. A ball that exists but is out of frame looks exactly like a broken game. And: destroying the template scene also destroys its AudioListener — any runtime-built camera needs one added explicitly.

## Phase 2 (NOT built — after v1.1 is built and Justin tests it)

- Achievements + a gear shop: earn in-game currency by playing, spend it
  on new gear. Assumed earned currency, not real money. Do not build
  until v1.1 is tested.

## Honest limitations

- **Never compiled.** No Unity Editor is available here, so the code was
  verified by brace-balance checks, an undefined-name sweep, and manual
  API re-reads only. A typo the sweep missed would surface as a build
  error in Build Automation (safe: it fails the build, it does not ship
  a broken APK) — or, if it compiles but misbehaves, only Justin's
  device can reveal it. v1.1 proved this: it compiled green and still
  showed a black screen on device, which is why v1.1.1 hardens startup
  and reports failures on screen instead of dying silent.
- TTS `speak(String, int, Bundle, String)` overload resolution is the most
  standard-but-unverified call; failures are caught and the game continues.
- The shipped DejaVuSans font is imported by Cloud Build with default font
  import settings; if its "Include Font Data" default were ever off, the
  font would not be in the build and FontMissing would report it on screen.
- Physics values (hook strength, pin scatter, settle timing) are reasoned
  estimates, not play-tested — expect one tuning round after Justin plays it.

## v1.1.4 (Nov 19, 2026) — pink screen, touch buttons on TV, aim mapping

Justin's v1.1.3 test (~8:11 PM, photo): the whole game screen is solid
pink/magenta — no lane, pins or ball distinguishable. The HUD (frames,
buttons, power bar) renders fine, so it is the 3D scene, not the UI.

Diagnosis (verified in code, not guessed):
- The project is URP (URP package 17.0.4, URP render pipeline asset
  assigned in GraphicsSettings.asset, URPProjectSettings.asset present).
- But NO material in the template uses "Universal Render Pipeline/Lit"
  (no .mat assets in the template at all; the single m_Shader reference
  in Assets/ is inside PC_Renderer.asset), so that shader is almost
  certainly not in the player build. MakeMaterial's Shader.Find came back
  empty, and every 3D object kept the built-in default material — which
  URP paints solid magenta. In v1.1.2 only the lane shape showed against
  the dark void, so the colour never surfaced (Justin cannot see colours
  well); v1.1.3's alley encloses the view, so the whole screen went pink.
- Same null-shader cause also meant the gold aim guide was never created
  (it only exists inside the shader!=null branch in BallController).
- The touch buttons (Aim/Hook/HOLD TO THROW) ARE visible on the TV in the
  photo: Input.touchSupported is true on the Onn box even though it has no
  touchscreen. Those are focusable UI buttons, so the D-pad moved focus
  between them instead of aiming — the aim-navigation complaint.
- Aim mapping was Up/Down = aim, Left/Right = hook, which feels
  backwards; aiming across a lane is naturally Left/Right.
- Speech before the menu: exact words unknown (asked Justin; no fix
  attempted for an undiagnosed symptom). Menu speaking works.

Changes:
- BowlingConfig.FindGameShader() (GameBootstrap.cs): pipeline-aware.
  URP active (GraphicsSettings.currentRenderPipeline != null) tries
  URP/Lit, then URP/Simple Lit, then URP/Unlit; built-in tries Standard,
  Unlit/Color, Legacy Shaders/Diffuse. Never assigns a built-in shader
  in a URP build or vice versa. Logs the picked shader.
- LaneBuilder/PinManager/BallController MakeMaterial (and the ball build)
  all use FindGameShader(). LaneBuilder records BuildError ("No 3D
  shader found — the scene may render pink.") when nothing is found, so
  the failure shows and speaks on the menu instead of going pink
  silently (BuildError is already wired to the startup error line).
- Camera forced to SolidColor clear (was Skybox default; the template
  scene is destroyed at load, so there is no skybox to render).
- Touch panel: SetActive(Input.touchSupported &&
  SystemInfo.deviceType != DeviceType.Television) — TV never gets it.
- Aim remap: Left/Right = aim (one board per press, hold to repeat),
  Up/Down = hook. Spoken aim/hook unchanged. Touch buttons and swipe
  unchanged (they call TouchAim/TouchSpin/ChangeAim directly).
- New baked voice clip aim_help.ogg ("Left and right to aim. Up and
  down for hook. Hold OK to throw."), spoken once at the start of a
  game (controlsHintSpoken flag, reset in StartGame).
- Version bump 1.1.3 -> 1.1.4 (build #14).

## v1.1.5 (Nov 19, 2026) — the shader chain found nothing

Justin's v1.1.4 test (~10:24 PM): the game shows its own startup error
"No 3D shader found — the scene may render pink." The error did its job
(it told the truth instead of going pink silently), and the screen was
still pink: with no shader found, every object kept the built-in default
material, which URP paints magenta.

Diagnosis (the v1.1.4 theory was only half right): it is not that one
shader name is missing — the whole lookup chain came back empty
(URP/Lit, Simple Lit, Unlit, Standard, Unlit/Color, Legacy Diffuse).
The template ships NO materials at all, so Unity strips every URP and
built-in shader out of the player build. Shader.Find can only find
shaders that are in the build, so no name-based lookup can ever work
in this project. The always-included-shaders list in ProjectSettings
could not be relied on either (no entry found in the repo copy).

Fix: the game now ships its OWN shader, Assets/Resources/Shaders/
GameUnlit.shader — a small URP unlit shader (flat _Color, no lighting).
A shader asset in a Resources folder is always included in the build,
so FindGameShader loads it with Resources.Load<Shader>("Shaders/
GameUnlit") first and keeps the Shader.Find chain only as a fallback.
Flat colours are right for this game: wood lane, white pins, red
stripes, gold ball. Version bump 1.1.4 -> 1.1.5 (build #16).

## v1.2.0 (Nov 20, 2026) — fixes + Crew-style features + progression + better graphics

Justin's go Nov 20 ~03:09 ("build all of it" + "make the graphics look
a little better"). The APK hunt for Bowling Crew/Clash/Club was dropped
Nov 20 ~02:38; features were researched from the web instead (report:
~/workspace/research_notes/bowling-games-features-research-20261120-0838/report.md).
Headline: NONE of the three games has any accessibility — a blind player
publicly asked Bowling Crew for a self-voicing mode. IP boundary: Crew/
Clash/Club-LIKE mechanics with our own code/assets; never copy their art,
sounds, or code. Deliberately NOT copied: chip entry fees, Autocharge,
ball-level-beats-skill, first-open ads, live PvP (needs servers).

### The three v1.1.5 bug fixes
- **One voice at a time:** Narration detects TalkBack via Android
  AccessibilityManager.isTouchExplorationEnabled() (same AndroidJavaObject
  pattern as BowlingUI.IsTelevision, cached). When TalkBack is on, the
  game never speaks UI labels (Narration.SpeakUIKey, used by ButtonSpeaker
  and all menu/toggle lines) — TalkBack reads them — and only speaks
  gameplay lines TalkBack cannot know (aim, power, results, score).
  VoiceBank now QUEUES clips (one at a time) instead of PlayOneShot, so
  game clips can never stack; PlayNow() interrupts for the fast-changing
  power-meter bands. SpeakClipThenTTS() plays a baked clip, then speaks
  a dynamic TTS line after the queued clips finish (delay from
  VoiceBank.QueuedSeconds()), so clips and TTS never talk over each other.
- **Real sound:** SoundSynth normalises every clip to ~0.9 peak (music
  0.55) — the v1.1 clips were near-silent by construction (roll ~0.03,
  music ~0.02). Crash is now 1.2 s with a slower decay; roll loop gain
  raised; volumes raised (roll 0.7, music 0.35, gutter/cheer/spare
  0.9–1.0, click 0.8); the manager's roll-volume cap is 0.8, not 0.3.
  All triggers unchanged (they were always wired).
- **Strikes called right:** ResolveRoll counts pins FIRST. A gutter result
  happens only when fallen == 0; the sticky gutterThisRoll flag is now
  used for the gutter SOUND only, never for the result.

### New features
- **Spoken 3-step throw (Crew-style, D-pad + voice):** Step 1 position
  (Left/Right, board spoken, OK locks), Step 2 power (hold OK to charge,
  percent spoken at 25/50/75/100 via interrupting clips, release locks),
  Step 3 hook (Left/Right, hook spoken, OK throws). Back steps back one
  step. Touch: aim buttons in step 1, hook buttons in step 3, big button
  charges in step 2 / taps to throw in step 3. Old Aiming/Windup states
  are gone; the aim/hook/power mechanics underneath are unchanged.
- **Quick Match:** new BowlingMode, 3 frames (frame 3 plays by 10th-frame
  rules). Scoring.cs generalised with BowlingScore.TotalFrames (default
  10); the scoreboard shows only the match's frames. "Win" = 40+ points.
- **Spoken ball-cam:** SpeakResultLine plays a baked down_0..down_10
  clip ("Seven pins down.") first, then the result clip (strike/spare/
  gutter/turkey/all-down); for plain counts the pins left standing are
  spoken by TTS after the clip (PinManager.StandingNumbers(), 1–10).
- **Ball types as side-grades:** Light/Standard/Heavy replaced by Heavy
  16 lb (power), Bouncy 13 lb (hook), Accurate 14 lb (straight) — none
  stronger overall, none paywalled. Each spec now carries Colour, Cost,
  Desc; BallController.SetBallColor recolours the ball.
- **20-second turn timer:** optional, default OFF, toggle in the More
  panel (PlayerPrefs LB12_TimerOn). Spoken warnings at 10 and 5 seconds;
  at zero the ball throws with current aim/hook (power 50 if the power
  step was never reached). Countdown also shows in the HUD status line.
- **Daily rewards + missions:** 25 coins once a day (PlayerPrefs
  LB12_LastDaily), announced on the main menu. Three rotating missions a
  day from a 6-item pool (steps of 2 through the pool by day of year, so
  the three are always distinct), progress tracked per day, coins on
  completion, shown with live progress on the More panel.
- **Achievements:** 10, PlayerPrefs-backed, announced out loud the moment
  one is earned (clip + name, drained after each resolve/game over so
  several in one game all get announced), listed in the Achievements
  panel: First Strike, First Spare, Turkey, Century (100 in a game),
  Quick Winner (40 in a Quick Match), Clean Game (no gutter balls),
  Pin Crusher (50 pins in one game), Regular (10 games), New Gear
  (unlock a ball), Coin Collector (earn 100 coins).
- **Unlockable balls and gear (GameProgression.cs, new):** coins earned
  by playing — 1 per pin in scored games (Practice cannot be farmed),
  +20 full game / +10 Quick Match completion, missions, daily reward.
  Gear Shop (More panel): 5 unlockable balls (Comet 60, Midnight 120,
  Thunder 200, Feather 200, Gold Pro 350 — all side-grades), 4 ball
  colours (Ruby/Ocean 40, Emerald 60, Violet 100), 3 alley looks
  (Midnight 80, Sunset 100, Emerald 120 — recolour the gold accent trim
  via LaneBuilder.SetAccentColor). All earned in-game; NO real money, NO
  IAP code anywhere. All state under the LB12_ PlayerPrefs prefix (no
  collision with LynxosBowlingBest). Ball select lists owned balls only
  (rebuilt each time it opens).
- **Graphics (geometry + colour only, same flat GameUnlit shader):**
  39 board stripes on the lane (alternating wood tones), bright gold
  arrows (were dark), red pin stripes slightly wider/brighter, white
  shine sphere on the ball, gold trim along both gutter walls, warmer
  ceiling light panels (amber) with gold frames. Camera framing and the
  pink-screen fix untouched.

### Voice clips (44 new, Piper en_US-ryan-medium, 77 total in Voices/)
step_position, step_power, step_hook; quick_match, gear_shop,
achievements_btn, more_btn; timer_on, timer_off, time_warning_10,
time_warning_5, time_up; daily_reward, mission_done,
achievement_unlocked; down_0..down_10; power_25, power_50, power_75,
power_100; ball_bouncy, ball_accurate, ball_comet, ball_midnight,
ball_thunder, ball_feather, ball_goldpro; skin_ruby, skin_ocean,
skin_emerald, skin_violet; theme_midnight, theme_sunset, theme_emerald.
Generated with a recreated /tmp/edgevenv (piper-tts + en_US-ryan-medium
from HuggingFace rhasspy/piper-voices), wav -> ogg via ffmpeg
(libvorbis q5). Old ball_light/ball_standard clips were orphaned by the
v1.2.0 ball rename; ball_heavy is still used. (v1.3.0: the orphaned
ball_light, ball_standard and winding_up clips were deleted from the
project, and aim_help was re-recorded — see the v1.3.0 section.)

### Design decisions made while building (Justin's call where noted)
- Quick Match "win" = 40+ points in 3 frames (single-player; there is
  no opponent to beat). The 40-point bar and the achievement share it.
- Step 1's intro clip plays only on the first ball of a game; later
  balls speak the frame line straight away (a 3-second intro every
  throw would drag).
- Missions are visible on the More panel (progress + done state), not a
  separate panel, to keep the menu count down.
- Practice/Spare Practice award no coins (anti-farm) but still count
  pins toward missions.

### Verification done locally (no Editor, no compile)
- Brace/paren/bracket balance clean on all 12 scripts.
- Undefined-name sweep: 35 hits, all false positives (Unity APIs
  Vector3.Angle/Quaternion.Euler, constructors, Action delegate calls,
  lambda parameters).
- Zero old Unity API names (PhysicMaterial, clearColor, .velocity,
  .drag, angularDrag) — only the pre-existing Shader.Find fallback
  chain inside FindGameShader, which is by design (shipped Resources
  shader first).
- All 44 new clips exist, valid OggS, and every clip key referenced in
  code has a file.
- PlayerPrefs keys all LB12_-prefixed; no collision with
  LynxosBowlingBest.
- Panel geometry checked by arithmetic: main menu 9 buttons end at
  -1042, gear 13 buttons end at -1050, ball select (8 owned) ends at
  -1022, achievements end at -878 — all inside 1080.

### Watch out for on device (unverified by build)
- VoiceBank queue: PlayOneShot replaced by clip+Play with
  WaitForSeconds(clip.length) — if a clip's length reads as 0 on device
  the queue could spin fast; the 0.05 s pad and isPlaying check are the
  guard.
- The 3-step throw is a full input-flow replacement (Aiming/Windup
  states deleted). If OK ever feels dead in a step, check the
  okWasReleased gating in that handler first.
- Quick Match frame 3 uses the generalised 10th-frame scoring — the
  first on-device check should be a strike/spare in frame 3.

## v1.3.0 (Nov 20, 2026) — camera follow, lit shader, menu bowler + wearable gear

Justin's v1.2.0 test (Nov 20, midday): graphics not to his liking, very
low quality, nowhere near as satisfying as Bowling Crew; the ball never
shows rolling — he only sees the pins get hit; the alley, pins and ball
all look far away; "everything in the app is off" — a whole-app deep
dive was requested. Main menu: the bowler (the whole human) front and
centre, menu buttons on the side, because gear should be wearable —
give the character a makeover, earn shirts, shoes, accessories, bowling
balls, everything. No visible power bar. Go given Nov 20 ~12:20 PM
("let's go ahead and build it and see where this goes").

### 1. Camera follow + closer framing (the top fix)

Root cause of the invisible ball (five stacked causes, from the audit):
static camera (0, 1.7, -4.6, FOV 55, never moves); the ball shrinks
56 px -> 11 px on 1080p by the pins; the gold ball is camouflaged on
the gold-brown lane with gold arrows; the flat GameUnlit shader gives
no shading, shadow or shine; the roll lasts ~2 s at up to 9.5 m/s.
The "ball-cam" was voice only and never moved the camera.

Design (LaneBuilder owns the camera; BowlingGameManager drives it):
- **Aim mode (static):** camera nudged to (0, 1.6, -4.2) looking at
  (0, 0.4, 11). The ball is 58 px tall instead of 56 and safely in
  frame (18.0 deg off centre against a 27.5 deg half-FOV; the pins are
  0.9 deg off centre; the bottom edge of the screen meets the lane
  floor at z = -1.64, so the ball at z = -0.6 is in frame). A much
  closer static camera was REJECTED by the numbers: at (0, 1.1, -2.6)
  the ball sits 22.1 deg off centre, 5 deg from the bottom edge, and
  the HUD could cover it.
- **Follow mode (during the roll):** the camera stays ~2.8 m behind the
  ball, ~1 m above it, looking ~6 m ahead. The manager calls
  lane.UpdateFollow(ball.Position) every frame while Rolling; the
  camera glides to the target with frame-rate independent smoothing
  (1 - exp(-8 * dt) lerp on position, slerp on view direction) — it
  never snaps. The ball stays ~75 px tall through the roll, 13.8 deg
  below centre (inside the 27.5 deg half-screen; it rises toward the
  centre as the look target clamps at the head pin). The follow target
  is clamped: camera z never passes 15.5, the look target never passes
  the head pin (z = 18.3), so the camera never crosses the deck or
  clips the masking unit.
- **Return mode (after the pins are counted):** ResolveRoll calls
  lane.ReturnToAim(); the camera glides home (rate 4) and snaps exactly
  to the aim framing on arrival. ShowMainMenu also calls ReturnToAim,
  so the menu camera is always the aim framing.
- **Slow motion:** on the first pin hit of a roll, Time.timeScale =
  0.35 for ~1 real second (counted with unscaledDeltaTime, so it always
  ends). StartGame, ShowMainMenu and GameOver all reset timeScale to 1
  as a safety net. The manager's roll timeout uses Time.time, so it is
  unaffected in practice (the roll is ~2 s; slow-mo is ~1 s).

### 2. Lit shader with shadows (GameLit)

- New Assets/Resources/Shaders/GameLit.shader: a small URP shader —
  Lambert diffuse from the main directional light, a Blinn-Phong
  specular shine highlight (_Smoothness), and shadow receiving via
  SHADOW_COORDS / TRANSFER_SHADOW / SHADOW_ATTENUATION (LightMode
  ForwardLit). Flat _Color base like GameUnlit, so the palette is
  unchanged — only the lighting is new.
- FindGameShader (GameBootstrap.cs) now tries Resources.Load
  "Shaders/GameLit" first, then "Shaders/GameUnlit", then the old
  Shader.Find chain. Every material builder (LaneBuilder, PinManager,
  BallController, BowlerCharacter) treats a shader whose name contains
  "GameLit" like a URP shader for the _Smoothness property.
- GameUnlit stays in the project as the FALLBACK: if GameLit ever fails
  to load, every material falls back to the flat shader instead of
  going pink. NEVER assign a built-in (non-URP) shader; NEVER rely on
  Shader.Find for a shader no material uses.
- The directional light now has shadows on (LightShadows.Soft,
  shadowBias 0.05, shadowNormalBias 0.02). **UNCONFIRMED LOCALLY: the
  URP renderer asset (which must also have shadow support on) lives in
  the GitHub repo, not this local copy — confirm it there before
  building.** If it is off, this setting is silently ignored and the
  scene renders lit without real shadows (the shader degrades
  gracefully). The shadow blob under the ball (below) is the insurance
  for that case.

### 3. Contrast + ball readability

- Lane wood darkened (0.55/0.38/0.18 -> 0.32/0.21/0.09), board stripes
  darkened (light 0.62/0.45/0.22 -> 0.40/0.28/0.13, dark 0.48/0.32/0.15
  -> 0.26/0.17/0.08), neighbour lanes darkened too — so the gold ball
  stands out against the lane instead of blending in.
- Lane arrows toned down from bright gold (0.95/0.85/0.45) to muted
  amber (0.55/0.42/0.15) — the old arrows matched the ball exactly.
- Aim guide strip is now WHITE (0.9/0.9/0.9), not gold — the old gold
  strip and the ball merged into one shape.
- **Finger holes:** three small dark spheres (0.026 m) on the ball's
  surface, parented to the ball so they roll with it — a plain sphere
  looks the same spinning as standing still; the holes make the spin
  visible. Local offsets are magnitude ~0.5 (the ball's unit-sphere
  surface, scaled to 0.216 m diameter).
- **Shadow blob:** a dark flat quad (0.34 m) that follows the ball every
  frame (BallController.Update), riding 0.006 m above the lane surface
  — above the board strips (0.002) and dots (0.004), so it never
  z-fights with the lane markings. Cheap insurance if the real-time
  shadows do not come through.

### 4. Spare Practice announce fix

- The "All pins down!" result now triggers when NO pins are standing
  (standing == 0), not when 10 fall — the old check could never fire,
  since a spare setup has at most 4 pins.
- The clear plays the down_N clip for the ACTUAL number knocked down
  ("down_" + fallen), never the hardcoded down_10 (which played even
  for a 2-pin 7-10 split).

### 5. Controls hint back + dead clips out

- aim_help plays once at the start of the first game of the session
  (hintSpokenThisSession, set in AnnounceFrame on the first ball of a
  game; queued ahead of the step-1 clip; play is never blocked by
  speech). The clip was RE-RECORDED with Piper en_US-ryan-medium (same
  voice as all other clips) to teach the 3-step controls: "Left and
  right to aim. Hold OK for power, let go to lock it. Left and right
  for hook, then OK to throw." (5.8 s, 47,187 bytes). The v1.1.4 clip
  said "Up and down for hook", which no longer exists.
- Deleted from the project: ball_light.ogg, ball_standard.ogg,
  winding_up.ogg (no code or clip references remain; 74 clips left in
  Voices/).

### 6. Two-player parity

- GameOver's TwoPlayer branch now gives the same +20 completion coins
  and the same achievement checks as single-player (Century on the
  better of the two scores, Clean Game if neither player guttered,
  Pin Crusher at 50 pins total, Regular at 10 games). Two-player games
  used to earn nothing for finishing.

### 7. Power meter rebuild

- Old: 900x36 px, dark grey background, gold fill, "Power N" text —
  read as missing on the TV.
- New: 1200x90 px bar with a bright gold border (1224x114), dark
  background (0.08), fill that changes colour as it climbs (green
  below 50%, gold to 75%, red-orange above), and a big 48 pt gold
  percent number beside the bar ("73%"). It fills through the whole
  power step. The HUD spoken-line mirror moved up (y 150 -> 215) to
  clear the taller bar. The spoken power bands (25/50/75/100 via
  interrupting clips) are unchanged.

### 8. Menu makeover + wearable gear + animated character

- **BowlerCharacter.cs (new, 271 lines):** a full 3D bowler built from
  primitives — shoes, legs, torso (shirt), two arms on shoulder pivots,
  a head on its own pivot with hair and eyes, and the ball in one hand.
  Stands at (0.45, 0, -1.2) facing the camera (rotation y = 180), so
  he is right of centre and in front of the lane, clear of the menu
  buttons. MENU-ONLY: the bowler never appears in-game (Justin's call)
  — the manager shows him in ShowMainMenu and hides him in StartGame
  and GameOver.
- **Idle animation (all procedural, sine-based, smoothly looped):**
  weight shifting foot to foot (x sway +-0.02 m, lean +-1.5 deg),
  breathing (torso scale y +-1.5%), looking around (head yaw +-25 deg,
  nod +-4 deg), the ball turning slowly in his hand (40 deg/s), arms
  swinging gently in opposite phase, and a slow practice swing every
  7 seconds (1.4 s window: ball arm swings forward 55 deg, torso
  leans in 6 deg). He can never freeze mid-pose — everything is driven
  by one accumulating clock.
- **Menu layout redo:** title, subtitle and best-score/coins line move
  to the left over the button strip; all nine menu buttons sit on the
  LEFT side (x = -560, 760 px wide, ending at screen x = 780) so the
  bowler owns the centre; the full-screen dim is lighter (alpha 0.30,
  not 0.55) with a darker strip (alpha 0.55, 860 px wide) behind the
  buttons only. TalkBack behaviour unchanged — every button is still
  reachable, D-pad order wired with WireVertical, dialog/list
  semantics as before.
- **Wearable gear (GameProgression.cs + BowlingConfig):** new slots —
  Shirts (Classic Black 0, Gold 40, Crimson 60, Royal Blue 80, Emerald
  100), Shoes (Classic Black 0, White 40, Gold 60, Crimson 80),
  Accessories (Gold Headband 40, Wristband 60, Gold Cap 80; one slot,
  re-tapping the worn one takes it off). Same rules as balls/skins/
  themes: LB12_-prefixed PlayerPrefs (OwnShirt_/EquippedShirt,
  OwnShoe_/EquippedShoe, OwnAcc_/EquippedAcc), earned coins only, no
  real money. ApplyGear() on the character recolours the shirt/shoes/
  held ball and shows the equipped accessory at once — buying in the
  shop updates the bowler live. The held ball uses the equipped ball
  colour, or the equipped skin colour if one is equipped.
- **Gear Shop is now two levels** (BowlingUI): the shop lists six
  categories (Balls, Ball Colours, Alley Looks, Shirts, Shoes,
  Accessories), then the items in the chosen category, with Back
  returning to the categories. 24 buttons no longer have to fit on one
  screen; no screen holds more than 7. The shop always opens at the
  categories.

### Files changed in v1.3.0

- NEW: Assets/Scripts/BowlerCharacter.cs (271), Assets/Resources/
  Shaders/GameLit.shader.
- CHANGED: GameBootstrap.cs (FindGameShader order, Shirts/Shoes/
  Accessories catalogues), GameProgression.cs (BuyShirt/BuyShoe/
  BuyAccessory + owned/equipped state), LaneBuilder.cs (camera expose
  + Aim/Follow/Return, shadows on, darker lane/arrows, aim framing),
  BallController.cs (finger holes, shadow blob, white aim guide),
  PinManager.cs (GameLit _Smoothness), BowlingGameManager.cs (camera
  follow wiring, slow-mo, spare fix, aim_help once per session,
  two-player parity, bowler show/hide, gear kinds 3/4/5),
  BowlingUI.cs (menu side layout, gear categories, power meter
  rebuild), DESIGN.md (this file).
- DELETED: Assets/Resources/Voices/ball_light.ogg,
  ball_standard.ogg, winding_up.ogg.
- REGENERATED: Assets/Resources/Voices/aim_help.ogg (3-step wording).
- NOT changed (parent handles it): ProjectSettings.asset —
  bundleVersion 1.2.0 -> 1.3.0 via the GitHub web editor at upload;
  versionCode is auto = build number.

### Verification done locally (no Editor, no compile)

- Brace/paren/bracket balance clean on every touched file.
- Undefined-name sweep on every touched file: only Unity-API /
  delegate / lambda false positives (see the build report for the list).
- Camera geometry re-checked numerically: aim framing ball 58 px,
  18.0 deg off centre; follow ball ~75 px, 13.8 deg below centre;
  pins 0.9 deg off centre at the deck; camera z never passes 15.5.
- GameLit Resources.Load path + GameUnlit fallback traced live in code
  (FindGameShader tries GameLit, then GameUnlit, then Shader.Find).
- No file references a deleted clip or a renamed ball (grep clean).
- GameProgression keys all LB12_-prefixed; Practice still awards no
  coins; no new coin route was opened.

### Watch out for on device (unverified by build)

- **URP renderer shadow setting:** confirm in the GitHub repo that the
  URP renderer asset has shadow support on — it is not in this local
  copy. If it is off, real shadows silently do not appear (only the
  shadow blob will show).
- Camera follow + slow-mo are the biggest new moving parts: watch that
  the camera never snaps, never passes the deck, and that slow-mo
  always returns to normal speed after a roll.
- The bowler is a new 3D object in the menu camera's view: check he
  reads clearly (not too small, not clipped by the menu strip) and that
  his idle animation is smooth.
- The power meter is 2.5x taller than before: check it does not cover
  the spoken-line mirror or the touch buttons on tablet.
- First on-device check for the spare fix: a 2-pin spare setup should
  announce "2 pins down" (not 10), and "All pins down!" on a clear.
