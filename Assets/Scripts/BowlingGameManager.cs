// Lynxos Bowling — game orchestration: modes, turns, input, narration.
// Throwing is a spoken 3-step flow (v1.2.0, Bowling Crew style, adapted
// to D-pad + OK + voice): Step 1 position (Left/Right, OK locks it),
// Step 2 power (hold OK to charge, release locks it), Step 3 hook
// (Left/Right, OK throws). Every step is announced out loud.
// Escape/Back returns to the menu. While a menu is open the D-pad drives
// the buttons instead (EventSystem); while throwing, no button is
// selected so the arrows reach the game.
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum BowlingMode { FullGame, Practice, SparePractice, TwoPlayer, QuickMatch }

public class BowlingGameManager : MonoBehaviour
{
    private enum GameState { Menu, BallSelect, SpareSelect, More, Gear, Achievements, StepPosition, StepPower, StepHook, Rolling, Resolving, GameOver }

    private const float TurnTimerSeconds = 20f;

    private GameState state;
    private BowlingMode mode;
    private LaneBuilder lane;
    private PinManager pins;
    private BallController ball;
    private BowlingUI ui;
    private SoundSynth sound;
    private VoiceBank voiceBank;
    private bool ttsErrorReported;
    private float swipeAccum;

    private BowlingScore[] scores = new BowlingScore[2];
    private int[] consecutiveStrikes = new int[2];
    private int currentPlayer;
    private int currentFrame = 1;
    private int currentBall = 1;
    private int spareIndex;
    private bool bumpersOn;
    private bool voiceOn = true;
    private int bestScore;

    private float aimX;
    private int spin; // -3..+3
    private float power; // 0..100
    private float windupTime;
    private bool okWasReleased = true;
    private float inputLockout;
    private float aimRepeatTimer;
    private int aimHeldDir;
    private float lastAimSpeak;
    private float rollStartTime;
    private float settleTimer;
    private bool crashPlayed;
    private bool gutterPlayed;
    private bool gutterThisRoll;
    private int practiceThrows;
    private string lastResult = "";
    private string startupError;

    // v1.2.0: turn timer + per-game progression tracking.
    private float turnTimer;
    private bool warned10;
    private bool warned5;
    private int lastPowerBand = -1;
    private int gamePinsTotal;
    private bool gameHadGutter;
    private bool dailyClaimed;
    private bool firstBallOfGame;

    // Frames in the current match: 3 for a Quick Match, 10 otherwise.
    private int TotalFrames
    {
        get { return mode == BowlingMode.QuickMatch ? 3 : 10; }
    }

    private bool IsScoredMode()
    {
        return mode == BowlingMode.FullGame || mode == BowlingMode.QuickMatch || mode == BowlingMode.TwoPlayer;
    }

    private void Awake()
    {
        scores[0] = new BowlingScore();
        scores[1] = new BowlingScore();
        bestScore = PlayerPrefs.GetInt("LynxosBowlingBest", 0);

        // Every subsystem builds inside its own guard. A failure in one
        // step must never leave a black silent screen: the camera is built
        // first (LaneBuilder), and any failure is shown + spoken at the end.
        GameObject laneObj = new GameObject("Lane");
        laneObj.transform.SetParent(transform, false);
        lane = laneObj.AddComponent<LaneBuilder>();
        TryBuild("Lane", () => lane.Build());
        if (lane.BuildError != null) NoteError(lane.BuildError);
        if (lane != null) lane.SetAccentColor(GameProgression.AccentColor());

        GameObject pinObj = new GameObject("Pins");
        pinObj.transform.SetParent(transform, false);
        pins = pinObj.AddComponent<PinManager>();
        TryBuild("Pins", () => pins.Build());

        GameObject ballObj = new GameObject("Ball");
        ballObj.transform.SetParent(transform, false);
        ball = ballObj.AddComponent<BallController>();
        TryBuild("Ball", () => ball.Build());
        if (ball != null) ball.OnPinHit += OnBallHitPin;

        GameObject soundObj = new GameObject("Sound");
        soundObj.transform.SetParent(transform, false);
        try
        {
            // AddComponent runs SoundSynth.Awake (clip synthesis) at once.
            sound = soundObj.AddComponent<SoundSynth>();
        }
        catch (System.Exception e)
        {
            NoteError("Sound: " + e.GetType().Name + " " + e.Message);
            sound = null;
        }

        GameObject uiObj = new GameObject("UI");
        uiObj.transform.SetParent(transform, false);
        ui = uiObj.AddComponent<BowlingUI>();
        TryBuild("UI", () => ui.Build());
        if (ui != null) WireUI();
        if (ui != null && ui.FontMissing) NoteError("No font found — menu text may be invisible.");

        // Baked voice clips for the fixed spoken lines (menus, buttons,
        // strike/spare/gutter). Android TTS is silent on Justin's TV, so
        // without these the game can never speak its buttons.
        GameObject vbObj = new GameObject("Voice");
        vbObj.transform.SetParent(transform, false);
        try
        {
            voiceBank = vbObj.AddComponent<VoiceBank>();
            Narration.Bank = voiceBank;
        }
        catch (System.Exception e)
        {
            NoteError("Voice: " + e.GetType().Name + " " + e.Message);
            voiceBank = null;
        }

        ApplyBallSpec();
        if (lane != null) lane.SetBumpers(bumpersOn);
        Narration.Enabled = voiceOn;
        if (ui != null)
        {
            try
            {
                ShowMainMenu();
            }
            catch (System.Exception e)
            {
                NoteError("Menu: " + e.GetType().Name + " " + e.Message);
                Debug.LogError("[BowlingGameManager] ShowMainMenu failed: " + e);
            }
            if (!string.IsNullOrEmpty(startupError))
            {
                try
                {
                    ui.ShowStartupError(startupError);
                    Narration.Speak("Startup problem. " + startupError);
                }
                catch (System.Exception e)
                {
                    Debug.LogError("[BowlingGameManager] Cannot show startup error: " + e.Message);
                }
            }
        }
    }

    private void TryBuild(string name, System.Action build)
    {
        try
        {
            build();
        }
        catch (System.Exception e)
        {
            NoteError(name + ": " + e.GetType().Name + " " + e.Message);
            Debug.LogError("[BowlingGameManager] " + name + " build failed: " + e);
        }
    }

    private void NoteError(string message)
    {
        if (string.IsNullOrEmpty(startupError)) startupError = message;
        else startupError += " | " + message;
    }

    private void WireUI()
    {
        ui.OnAnyButton = () => { if (sound != null) sound.PlayClick(); };
        ui.OnFullGame = () => StartGame(BowlingMode.FullGame);
        ui.OnQuickMatch = () => StartGame(BowlingMode.QuickMatch);
        ui.OnPractice = () => StartGame(BowlingMode.Practice);
        ui.OnSparePracticeMenu = () => { state = GameState.SpareSelect; ui.ShowSpareSelect(); };
        ui.OnTwoPlayers = () => StartGame(BowlingMode.TwoPlayer);
        ui.OnOpenBallSelect = () => { state = GameState.BallSelect; ui.ShowBallSelect(); };
        ui.OnBallSelected = (i) =>
        {
            GameProgression.EquippedBall = i;
            ApplyBallSpec();
            ShowMainMenu();
        };
        ui.OnSpareSelected = (i) =>
        {
            spareIndex = i;
            StartGame(BowlingMode.SparePractice);
        };
        ui.OnToggleBumpers = () =>
        {
            bumpersOn = !bumpersOn;
            lane.SetBumpers(bumpersOn);
            ui.RefreshMenuLabels(bestScore, CurrentBallName(), bumpersOn, voiceOn);
            Narration.SpeakUIKey(bumpersOn ? "bumpers_on" : "bumpers_off",
                bumpersOn ? "Bumpers on." : "Bumpers off.");
        };
        ui.OnToggleVoice = () =>
        {
            voiceOn = !voiceOn;
            Narration.Enabled = voiceOn;
            ui.RefreshMenuLabels(bestScore, CurrentBallName(), bumpersOn, voiceOn);
            if (voiceOn) Narration.SpeakUIKey("voice_on", "Voice on.");
        };
        ui.OnPlayAgain = () => StartGame(mode);
        ui.OnQuitToMenu = () => ShowMainMenu();
        ui.OnOpenMore = () => { state = GameState.More; ui.ShowMore(GameProgression.TimerOn); };
        ui.OnOpenGear = () => { state = GameState.Gear; ui.ShowGear(); };
        ui.OnOpenAchievements = () => { state = GameState.Achievements; ui.ShowAchievements(); };
        ui.OnToggleTimer = () =>
        {
            GameProgression.TimerOn = !GameProgression.TimerOn;
            ui.ShowMore(GameProgression.TimerOn);
            Narration.SpeakUIKey(GameProgression.TimerOn ? "timer_on" : "timer_off",
                GameProgression.TimerOn ? "Timer on." : "Timer off.");
        };
        ui.OnGearSelected = (kind, index) => HandleGearSelected(kind, index);
        ui.OnTouchAimLeft = () => TouchAim(-1);
        ui.OnTouchAimRight = () => TouchAim(1);
        ui.OnTouchHookLeft = () => TouchSpin(-1);
        ui.OnTouchHookRight = () => TouchSpin(1);
        ui.OnTouchThrowDown = () => TouchThrowDown();
        ui.OnTouchThrowUp = () => TouchThrowUp();
    }

    private void ApplyBallSpec()
    {
        if (ball == null) return;
        int equipped = GameProgression.EquippedBall;
        if (equipped < 0 || equipped >= BowlingConfig.Balls.Length) equipped = 0;
        BowlingConfig.BallSpec spec = BowlingConfig.Balls[equipped];
        ball.SetBallSpec(spec);
        int skin = GameProgression.EquippedSkin;
        Color color = (skin >= 0 && skin < BowlingConfig.Skins.Length)
            ? BowlingConfig.Skins[skin].Color : spec.Color;
        ball.SetBallColor(color);
    }

    private string CurrentBallName()
    {
        int i = GameProgression.EquippedBall;
        if (i < 0 || i >= BowlingConfig.Balls.Length) i = 0;
        return BowlingConfig.Balls[i].Name;
    }

    // --- Gear Shop ---

    private void HandleGearSelected(int kind, int index)
    {
        if (kind == 0)
        {
            bool owned = GameProgression.OwnsBall(index);
            if (GameProgression.BuyBall(index))
            {
                ApplyBallSpec();
                Narration.Speak(owned
                    ? BowlingConfig.Balls[index].Name + " equipped."
                    : BowlingConfig.Balls[index].Name + " unlocked!");
                DrainAchievements();
            }
            else
            {
                Narration.Speak("Not enough coins. You have " + GameProgression.Coins + ".");
            }
        }
        else if (kind == 1)
        {
            bool owned = GameProgression.OwnsSkin(index);
            if (GameProgression.BuySkin(index))
            {
                ApplyBallSpec();
                Narration.Speak(owned
                    ? BowlingConfig.Skins[index].Name + " equipped."
                    : BowlingConfig.Skins[index].Name + " unlocked!");
            }
            else
            {
                Narration.Speak("Not enough coins. You have " + GameProgression.Coins + ".");
            }
        }
        else
        {
            bool owned = GameProgression.OwnsTheme(index);
            if (GameProgression.BuyTheme(index))
            {
                if (lane != null) lane.SetAccentColor(GameProgression.AccentColor());
                Narration.Speak(owned
                    ? BowlingConfig.Themes[index].Name + " equipped."
                    : BowlingConfig.Themes[index].Name + " unlocked!");
            }
            else
            {
                Narration.Speak("Not enough coins. You have " + GameProgression.Coins + ".");
            }
        }
        ui.ShowGear();
    }

    // --- Game flow ---

    private void StartGame(BowlingMode newMode)
    {
        mode = newMode;
        int frames = newMode == BowlingMode.QuickMatch ? 3 : 10;
        scores[0].Reset();
        scores[1].Reset();
        scores[0].TotalFrames = frames;
        scores[1].TotalFrames = frames;
        consecutiveStrikes[0] = 0;
        consecutiveStrikes[1] = 0;
        currentPlayer = 0;
        currentFrame = 1;
        currentBall = 1;
        practiceThrows = 0;
        gamePinsTotal = 0;
        gameHadGutter = false;
        firstBallOfGame = true;
        lastResult = "";
        ui.ShowHUD();
        inputLockout = 0.4f;
        okWasReleased = false;
        BeginFrame();
    }

    private void BeginFrame()
    {
        if (mode == BowlingMode.SparePractice)
        {
            pins.RackPins(BowlingConfig.SpareSetups[spareIndex].Pins);
        }
        else
        {
            pins.RackAll();
        }
        BeginBall();
    }

    private void BeginBall()
    {
        aimX = 0f;
        spin = 0;
        power = 0f;
        ball.ResetBall(aimX);
        state = GameState.StepPosition;
        okWasReleased = false;
        inputLockout = 0.25f;
        turnTimer = TurnTimerSeconds;
        warned10 = false;
        warned5 = false;
        lastPowerBand = -1;
        AnnounceFrame();
    }

    private void AnnounceFrame()
    {
        int standing = pins.CountStanding();
        string line;
        if (mode == BowlingMode.Practice)
        {
            line = "Practice. Throw " + (practiceThrows + 1) + ". " + standing + " pins standing.";
        }
        else if (mode == BowlingMode.SparePractice)
        {
            line = "Spare practice. " + BowlingConfig.SpareSetups[spareIndex].Name + ". "
                + standing + " pins standing.";
        }
        else if (mode == BowlingMode.QuickMatch)
        {
            line = "Quick match. Frame " + currentFrame + " of 3, ball " + currentBall + ". "
                + standing + " pins standing.";
        }
        else
        {
            string who = mode == BowlingMode.TwoPlayer ? "Player " + (currentPlayer + 1) + ". " : "";
            line = who + "Frame " + currentFrame + ", ball " + currentBall + ". "
                + standing + " pins standing.";
        }
        lastResult = line;
        // Step 1 clip first, then the frame line (TTS) after the clip ends
        // — but only on the first ball of a game, so later throws are not
        // held up by a 3-second intro every time.
        if (firstBallOfGame)
        {
            firstBallOfGame = false;
            SpeakClipThenTTS("step_position",
                "Step 1. Position. Left and right to move, OK to lock it in.", line);
        }
        else
        {
            Narration.Speak(line);
        }
        UpdateHUD();
    }

    private void ShowMainMenu()
    {
        state = GameState.Menu;
        if (sound != null) sound.StopRoll();
        ui.ShowMainMenu(bestScore, CurrentBallName(), bumpersOn, voiceOn);
        Narration.SpeakUIKey("main_menu", "Lynxos Bowling. Main menu.");
        // Daily reward, once per session (and once per day in PlayerPrefs).
        if (!dailyClaimed)
        {
            dailyClaimed = true;
            int reward = GameProgression.ClaimDaily();
            if (reward > 0)
            {
                SpeakClipThenTTS("daily_reward", "Daily reward. 25 coins.",
                    "Daily reward. " + reward + " coins.");
                DrainAchievements();
            }
        }
    }

    private void GameOver()
    {
        state = GameState.GameOver;
        if (sound != null) sound.StopRoll();
        bool quick = mode == BowlingMode.QuickMatch;
        int s0 = scores[0].Total();
        int s1 = scores[1].Total();
        string detail;
        if (mode == BowlingMode.TwoPlayer)
        {
            string winner = s0 == s1 ? "It is a tie!" : (s0 > s1 ? "Player 1 wins!" : "Player 2 wins!");
            detail = "Player 1: " + s0 + "\nPlayer 2: " + s1 + "\n" + winner;
            Narration.SpeakKey("game_over", "Game over. Player 1 scored " + s0 + ". Player 2 scored " + s1 + ". " + winner);
            foreach (string m in GameProgression.NoteGamePlayed()) AnnounceMission(m);
            DrainAchievements();
        }
        else
        {
            bool newBest = false;
            if (!quick && s0 > bestScore)
            {
                newBest = true;
                bestScore = s0;
                PlayerPrefs.SetInt("LynxosBowlingBest", bestScore);
                PlayerPrefs.Save();
            }
            bool quickWin = quick && s0 >= 40;
            detail = "Final score: " + s0 + (quickWin ? "\nQuick Match win!" : "")
                + (newBest ? "\nNew best score!" : "\nBest score: " + bestScore);
            Narration.SpeakKey("game_over", "Game over. Final score " + s0 + "."
                + (quickWin ? " Quick Match win!" : "") + (newBest ? " New best score!" : ""));
            // Progression: games played, completion coins, achievements.
            foreach (string m in GameProgression.NoteGamePlayed()) AnnounceMission(m);
            if (s0 >= 100) GameProgression.UnlockAchievement("century");
            if (!quick && !gameHadGutter) GameProgression.UnlockAchievement("clean_game");
            if (gamePinsTotal >= 50) GameProgression.UnlockAchievement("fifty_frame");
            if (GameProgression.GamesPlayed >= 10) GameProgression.UnlockAchievement("ten_games");
            if (quickWin)
            {
                GameProgression.UnlockAchievement("quick_win");
                foreach (string m in GameProgression.NoteQuickWin()) AnnounceMission(m);
            }
            GameProgression.AddCoins(quick ? 10 : 20);
            DrainAchievements();
        }
        ui.ShowGameOver(quick ? "Quick Match Over" : "Game Over", detail);
    }

    // --- Input ---

    private bool IsOkDown()
    {
        return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)
            || Input.GetKeyDown(KeyCode.JoystickButton0) || Input.GetKeyDown(KeyCode.Space);
    }

    private bool IsOkHeld()
    {
        return Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter)
            || Input.GetKey(KeyCode.JoystickButton0) || Input.GetKey(KeyCode.Space);
    }

    private bool IsOkUp()
    {
        return Input.GetKeyUp(KeyCode.Return) || Input.GetKeyUp(KeyCode.KeypadEnter)
            || Input.GetKeyUp(KeyCode.JoystickButton0) || Input.GetKeyUp(KeyCode.Space);
    }

    private bool IsBackDown()
    {
        return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton1);
    }

    private void Update()
    {
        if (inputLockout > 0f) inputLockout -= Time.deltaTime;

        // Android TTS init is async: if it failed, say so on screen once
        // the game has settled, instead of staying silent about the voice.
        if (!ttsErrorReported && Time.time > 2.5f)
        {
            ttsErrorReported = true;
            if (!string.IsNullOrEmpty(Narration.LastError) && ui != null)
                ui.ShowStartupError("Voice: " + Narration.LastError);
        }

        switch (state)
        {
            case GameState.StepPosition:
                TickTurnTimer();
                HandleStepPosition();
                break;
            case GameState.StepPower:
                TickTurnTimer();
                HandleStepPower();
                break;
            case GameState.StepHook:
                TickTurnTimer();
                HandleStepHook();
                break;
            case GameState.Rolling:
                HandleRolling();
                break;
            case GameState.Menu:
            case GameState.BallSelect:
            case GameState.SpareSelect:
            case GameState.More:
            case GameState.Gear:
            case GameState.Achievements:
                if (IsBackDown() && state != GameState.Menu) ShowMainMenu();
                break;
        }
    }

    // 20-second turn timer (optional, default off). Spoken warnings at 10
    // and 5 seconds; at zero the ball is thrown with the current aim and
    // hook (power 50 when the power step was never reached).
    private void TickTurnTimer()
    {
        if (!GameProgression.TimerOn) return;
        turnTimer -= Time.deltaTime;
        if (!warned10 && turnTimer <= 10f)
        {
            warned10 = true;
            Narration.SpeakKey("time_warning_10", "Ten seconds left.");
        }
        else if (!warned5 && turnTimer <= 5f)
        {
            warned5 = true;
            Narration.SpeakKey("time_warning_5", "Five seconds left.");
        }
        if (turnTimer <= 0f)
        {
            if (state != GameState.StepPower) power = 50f;
            Narration.SpeakKey("time_up", "Time's up. Throwing.");
            ThrowBall();
        }
    }

    // Step 1 — position: Left/Right moves the starting spot, OK locks it.
    private void HandleStepPosition()
    {
        if (state != GameState.StepPosition) return;
        if (inputLockout > 0f) return;

        // Tablet: swipe sideways on the lane to aim (about one board per
        // 60 px of drag). The touch buttons do the same job one tap at a
        // time; the swipe is the fast way.
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Moved)
            {
                swipeAccum += t.deltaPosition.x;
                if (swipeAccum > 60f) { swipeAccum = 0f; ChangeAim(1); }
                else if (swipeAccum < -60f) { swipeAccum = 0f; ChangeAim(-1); }
            }
        }

        if (IsBackDown())
        {
            ShowMainMenu();
            return;
        }

        bool okHeld = IsOkHeld();
        if (!okHeld) okWasReleased = true;

        int aimDir = 0;
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            aimDir = 1;
            aimHeldDir = 1;
            aimRepeatTimer = 0.4f;
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            aimDir = -1;
            aimHeldDir = -1;
            aimRepeatTimer = 0.4f;
        }
        else if (Input.GetKeyUp(KeyCode.RightArrow) && aimHeldDir == 1)
        {
            aimHeldDir = 0;
        }
        else if (Input.GetKeyUp(KeyCode.LeftArrow) && aimHeldDir == -1)
        {
            aimHeldDir = 0;
        }
        else if (aimHeldDir != 0 && (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow)))
        {
            aimRepeatTimer -= Time.deltaTime;
            if (aimRepeatTimer <= 0f)
            {
                aimDir = aimHeldDir;
                aimRepeatTimer = 0.12f;
            }
        }
        if (aimDir != 0) ChangeAim(aimDir);

        // OK locks the position and moves to the power step (only on a
        // fresh press, after the key was released).
        if (okWasReleased && IsOkDown())
        {
            state = GameState.StepPower;
            windupTime = 0f;
            power = 0f;
            lastPowerBand = -1;
            Narration.SpeakKey("step_power", "Step 2. Power. Hold OK to charge, let go to lock it in.");
            UpdateHUD();
        }
    }

    // Step 2 — power: hold OK to charge (the meter sweeps up and down),
    // release to lock the power. The percent is spoken at 25/50/75/100.
    private void HandleStepPower()
    {
        if (state != GameState.StepPower) return;
        windupTime += Time.deltaTime;
        power = Mathf.PingPong(windupTime * 110f, 100f); // ~0.9 s up, ~0.9 s down
        if (!IsOkHeld()) okWasReleased = true;

        int band = power >= 100f ? 4 : power >= 75f ? 3 : power >= 50f ? 2 : power >= 25f ? 1 : 0;
        if (band > lastPowerBand)
        {
            lastPowerBand = band;
            if (band > 0)
            {
                string key = band == 1 ? "power_25" : band == 2 ? "power_50" : band == 3 ? "power_75" : "power_100";
                string text = band == 1 ? "25 percent." : band == 2 ? "50 percent." : band == 3 ? "75 percent." : "Full power.";
                // Interrupt: a queued band clip would lag behind the meter.
                Narration.SpeakKeyNow(key, text);
            }
        }

        if (IsBackDown())
        {
            state = GameState.StepPosition;
            power = 0f;
            UpdateHUD();
            return;
        }
        if (IsOkUp())
        {
            state = GameState.StepHook;
            Narration.SpeakKey("step_hook", "Step 3. Hook. Left and right to set the hook, OK to throw.");
            UpdateHUD();
            return;
        }
        UpdateHUD();
    }

    // Step 3 — hook: Left/Right sets the hook, OK throws.
    private void HandleStepHook()
    {
        if (state != GameState.StepHook) return;
        if (inputLockout > 0f) return;

        if (IsBackDown())
        {
            state = GameState.StepPower;
            windupTime = 0f;
            power = 0f;
            lastPowerBand = -1;
            Narration.SpeakKey("step_power", "Step 2. Power. Hold OK to charge, let go to lock it in.");
            UpdateHUD();
            return;
        }

        bool okHeld = IsOkHeld();
        if (!okHeld) okWasReleased = true;

        if (Input.GetKeyDown(KeyCode.LeftArrow)) ChangeSpin(-1);
        else if (Input.GetKeyDown(KeyCode.RightArrow)) ChangeSpin(1);

        if (okWasReleased && IsOkDown())
        {
            ThrowBall();
        }
    }

    // --- Touch controls (tablet) ---

    public void TouchAim(int dir)
    {
        if (state == GameState.StepPosition && inputLockout <= 0f) ChangeAim(dir);
    }

    public void TouchSpin(int dir)
    {
        if (state == GameState.StepHook && inputLockout <= 0f) ChangeSpin(dir);
    }

    public void TouchThrowDown()
    {
        if (inputLockout > 0f) return;
        if (state == GameState.StepPosition)
        {
            state = GameState.StepPower;
            windupTime = 0f;
            power = 0f;
            lastPowerBand = -1;
            Narration.SpeakKey("step_power", "Step 2. Power. Hold OK to charge, let go to lock it in.");
            UpdateHUD();
        }
        else if (state == GameState.StepHook)
        {
            // In the hook step the big button is a tap-to-throw.
            ThrowBall();
        }
    }

    public void TouchThrowUp()
    {
        if (state == GameState.StepPower)
        {
            state = GameState.StepHook;
            Narration.SpeakKey("step_hook", "Step 3. Hook. Left and right to set the hook, OK to throw.");
            UpdateHUD();
        }
    }

    private void ChangeAim(int dir)
    {
        float maxAim = BowlingConfig.AimMaxBoards * BowlingConfig.BoardWidth;
        aimX = Mathf.Clamp(aimX + dir * BowlingConfig.BoardWidth, -maxAim, maxAim);
        ball.ResetBall(aimX);
        if (Time.time - lastAimSpeak > 0.45f)
        {
            lastAimSpeak = Time.time;
            Narration.Speak("Aim " + AimText() + ".");
        }
        UpdateHUD();
    }

    private void ChangeSpin(int dir)
    {
        int next = Mathf.Clamp(spin + dir, -BowlingConfig.MaxSpin, BowlingConfig.MaxSpin);
        if (next == spin) return;
        spin = next;
        Narration.Speak("Hook " + HookText() + ".");
        UpdateHUD();
    }

    private string AimText()
    {
        int boards = Mathf.RoundToInt(aimX / BowlingConfig.BoardWidth);
        if (boards == 0) return "center";
        if (boards > 0) return boards + " boards right";
        return (-boards) + " boards left";
    }

    private string HookText()
    {
        if (spin == 0) return "none";
        if (spin > 0) return spin + " right";
        return (-spin) + " left";
    }

    private void ThrowBall()
    {
        int equipped = GameProgression.EquippedBall;
        if (equipped < 0 || equipped >= BowlingConfig.Balls.Length) equipped = 0;
        ball.SetBallSpec(BowlingConfig.Balls[equipped]);
        ball.ThrowBall(power / 100f, spin);
        state = GameState.Rolling;
        rollStartTime = Time.time;
        settleTimer = 0f;
        crashPlayed = false;
        gutterPlayed = false;
        gutterThisRoll = false;
        if (sound != null) sound.StartRoll();
        Narration.SpeakKey("ball_away", "Ball away. Power " + Mathf.RoundToInt(power) + ".");
        UpdateHUD();
    }

    // --- Rolling / resolving ---

    private void OnBallHitPin(float impactSpeed)
    {
        if (state != GameState.Rolling) return;
        if (!crashPlayed)
        {
            crashPlayed = true;
            if (sound != null) sound.PlayCrash(Mathf.Clamp(impactSpeed / 8f, 0.3f, 1f));
        }
    }

    private void HandleRolling()
    {
        if (ball.InGutter) gutterThisRoll = true;

        float z = ball.Position.z;
        if (!crashPlayed && z > 17.5f)
        {
            crashPlayed = true;
            if (gutterThisRoll && !gutterPlayed)
            {
                gutterPlayed = true;
                if (sound != null) sound.PlayGutter();
            }
            else if (!gutterThisRoll)
            {
                // Reached the pins with no reported pin hit (rare): play a
                // soft crash so the throw still sounds right.
                if (sound != null) sound.PlayCrash(0.4f);
            }
        }

        if (sound != null) sound.SetRollVolume(Mathf.Clamp01(ball.CurrentSpeed / 8f) * 0.8f);

        bool slow = ball.CurrentSpeed < 0.15f;
        if (slow) settleTimer += Time.deltaTime;
        else settleTimer = 0f;

        bool fellOut = ball.Position.y < -1f;
        if (settleTimer > 0.6f || z > 20.3f || fellOut || Time.time - rollStartTime > 8f)
        {
            if (sound != null) sound.StopRoll();
            state = GameState.Resolving;
            lastResult = "Counting pins…";
            StartCoroutine(ResolveRollAfterDelay());
        }
    }

    private IEnumerator ResolveRollAfterDelay()
    {
        yield return new WaitForSeconds(0.7f);
        ResolveRoll();
    }

    private void ResolveRoll()
    {
        int fallen = pins.CountFallen();
        int standing = pins.CountStanding();
        List<int> standingNums = pins.StandingNumbers();
        string result;
        bool isStrike = false;
        bool isSpare = false;
        bool isGutter = false;

        // v1.2.0: pins FIRST. The old code checked the sticky gutter flag
        // before the strike branch, so a ball that clipped the gutter
        // after knocking the pins down was announced as a gutter ball.
        if (mode == BowlingMode.SparePractice)
        {
            result = fallen == 10
                ? "All pins down!"
                : fallen + (fallen == 1 ? " pin down, " : " pins down, ") + standing + " standing.";
        }
        else if (fallen == 10 && currentBall == 1)
        {
            isStrike = true;
            consecutiveStrikes[currentPlayer]++;
            result = consecutiveStrikes[currentPlayer] >= 3
                ? "Turkey! Three strikes in a row!"
                : "Strike!";
            if (sound != null) sound.PlayCheer();
        }
        else
        {
            if (currentBall == 1) consecutiveStrikes[currentPlayer] = 0;
            bool spare = false;
            if (mode != BowlingMode.Practice && currentBall == 2)
            {
                int first = scores[currentPlayer].RollInFrame(currentFrame, 0);
                spare = first >= 0 && first + fallen == 10;
            }
            if (spare)
            {
                isSpare = true;
                result = "Spare!";
                if (sound != null) sound.PlaySpare();
            }
            else if (fallen == 0)
            {
                // A gutter result only when NO pins fell — never from the
                // sticky gutter flag alone.
                isGutter = true;
                result = "Gutter ball. No pins down.";
            }
            else
            {
                result = fallen + (fallen == 1 ? " pin down, " : " pins down, ")
                    + standing + " standing.";
            }
        }

        lastResult = result;
        gamePinsTotal += fallen;
        if (isGutter) gameHadGutter = true;

        // Coins (scored games only, so Practice cannot be farmed) and
        // missions / achievements.
        if (IsScoredMode() && fallen > 0) GameProgression.AddCoins(fallen);
        foreach (string m in GameProgression.NotePins(fallen)) AnnounceMission(m);
        if (isStrike)
        {
            foreach (string m in GameProgression.NoteStrike()) AnnounceMission(m);
            GameProgression.UnlockAchievement("first_strike");
            if (consecutiveStrikes[currentPlayer] >= 3) GameProgression.UnlockAchievement("turkey");
        }
        if (isSpare)
        {
            foreach (string m in GameProgression.NoteSpare()) AnnounceMission(m);
            GameProgression.UnlockAchievement("first_spare");
        }

        if (mode == BowlingMode.Practice)
        {
            practiceThrows++;
            SpeakResultLine(result, " Practice throw " + practiceThrows + ".", fallen, standingNums);
            pins.RackAll();
            currentBall = 1;
            DrainAchievements();
            BeginBall();
            return;
        }

        if (mode == BowlingMode.SparePractice)
        {
            if (standing == 0)
            {
                if (voiceBank != null) voiceBank.Play("down_10");
                Narration.SpeakKey("spare_made", "Spare made! Nice shot.");
                if (sound != null) sound.PlaySpare();
            }
            else
            {
                SpeakResultLine(result, " " + standing + " left. Try again.", fallen, standingNums);
            }
            pins.RackPins(BowlingConfig.SpareSetups[spareIndex].Pins);
            currentBall = 1;
            DrainAchievements();
            BeginBall();
            return;
        }

        // Full game / 2 players / Quick Match: record the roll, then advance.
        scores[currentPlayer].AddRoll(fallen);
        SpeakResultLine(result, " Score " + scores[currentPlayer].Total() + ".", fallen, standingNums);
        DrainAchievements();

        bool frameOver;
        if (currentFrame < TotalFrames)
        {
            frameOver = (currentBall == 1 && fallen == 10) || currentBall == 2;
        }
        else
        {
            frameOver = scores[currentPlayer].IsComplete();
        }

        if (!frameOver)
        {
            currentBall++;
            // Last frame: a fresh rack after a strike (ball 1 or 2) or a
            // spare (balls 1+2); otherwise shoot at the pins left standing.
            if (currentFrame == TotalFrames && NeedsFreshRack())
            {
                pins.RackAll();
            }
            else
            {
                pins.RemoveFallen();
            }
            BeginBall();
            return;
        }

        if (currentFrame == TotalFrames)
        {
            // Two players: player 1 still has to bowl the last frame.
            if (mode == BowlingMode.TwoPlayer && currentPlayer == 0)
            {
                currentPlayer = 1;
                currentBall = 1;
                BeginFrame();
                return;
            }
            GameOver();
            return;
        }

        if (mode == BowlingMode.TwoPlayer && currentPlayer == 0)
        {
            currentPlayer = 1;
        }
        else
        {
            currentPlayer = 0;
            currentFrame++;
        }
        currentBall = 1;
        BeginFrame();
    }

    // Last frame only: does the next ball get a fresh full rack?
    // Ball 2: yes if ball 1 was a strike. Ball 3: yes if ball 2 was a
    // strike, or balls 1+2 made a spare.
    private bool NeedsFreshRack()
    {
        if (currentFrame != TotalFrames) return false;
        if (currentBall == 2)
        {
            return scores[currentPlayer].RollInFrame(TotalFrames, 0) == 10;
        }
        int b1 = scores[currentPlayer].RollInFrame(TotalFrames, 0);
        int b2 = scores[currentPlayer].RollInFrame(TotalFrames, 1);
        if (b2 == 10) return true;
        if (b1 >= 0 && b2 >= 0 && b1 + b2 == 10) return true;
        return false;
    }

    // Speaks a roll result. The ball-cam comes first: the pin-count clip
    // ("Seven pins down.") plays, then the result clip (strike, spare,
    // gutter, turkey, all down). For a plain count the pins left standing
    // are dynamic, so they are spoken by TTS after the queued count clip
    // finishes. The HUD always shows the full line.
    private void SpeakResultLine(string result, string suffix, int fallen, List<int> standingNums)
    {
        string line = result + suffix;
        if (voiceBank != null) voiceBank.Play("down_" + fallen);
        if (result == "Strike!") Narration.SpeakKey("strike", line);
        else if (result.StartsWith("Turkey!")) Narration.SpeakKey("turkey", line);
        else if (result == "Spare!") Narration.SpeakKey("spare", line);
        else if (result == "Gutter ball. No pins down.") Narration.SpeakKey("gutter", line);
        else if (result == "All pins down!") Narration.SpeakKey("all_down", line);
        else
        {
            string tts = fallen + (fallen == 1 ? " pin down. Pins " : " pins down. Pins ")
                + JoinPinNumbers(standingNums) + " standing." + suffix;
            float delay = voiceBank != null ? voiceBank.QueuedSeconds() + 0.1f : 0.5f;
            StartCoroutine(SpeakDelayed(tts, delay));
        }
    }

    private string JoinPinNumbers(List<int> numbers)
    {
        if (numbers.Count == 0) return "";
        if (numbers.Count == 1) return numbers[0].ToString();
        string s = "";
        for (int i = 0; i < numbers.Count; i++)
        {
            if (i > 0) s += (i == numbers.Count - 1) ? " and " : ", ";
            s += numbers[i].ToString();
        }
        return s;
    }

    // --- Spoken announcements ---

    // Plays a baked clip, then speaks dynamic text (TTS) once the queued
    // clips have finished — so a clip and a TTS line never talk over each
    // other. Falls back to plain TTS when the clip does not exist.
    private void SpeakClipThenTTS(string clipKey, string clipFallback, string ttsText)
    {
        if (voiceBank != null && voiceBank.HasClip(clipKey))
        {
            Narration.SpeakKey(clipKey, clipFallback);
            float delay = voiceBank.QueuedSeconds() + 0.1f;
            StartCoroutine(SpeakDelayed(ttsText, delay));
        }
        else
        {
            Narration.Speak(ttsText);
        }
    }

    private IEnumerator SpeakDelayed(string text, float delay)
    {
        yield return new WaitForSeconds(delay);
        Narration.Speak(text);
    }

    private void AnnounceMission(string message)
    {
        SpeakClipThenTTS("mission_done", "Mission complete!", message);
    }

    // Announces every achievement unlocked since the last drain.
    private void DrainAchievements()
    {
        string name;
        while ((name = GameProgression.TakePendingAchievement()) != null)
        {
            SpeakClipThenTTS("achievement_unlocked", "Achievement unlocked!", name + " unlocked.");
        }
    }

    // --- HUD ---

    private string TimerText()
    {
        if (!GameProgression.TimerOn) return "";
        return "   •   " + Mathf.CeilToInt(Mathf.Max(0f, turnTimer)) + "s left";
    }

    private string StatusLine()
    {
        switch (state)
        {
            case GameState.StepPosition:
                return "Step 1 — Position: " + AimText() + "   •   OK to lock" + TimerText();
            case GameState.StepPower:
                return "Step 2 — Power " + Mathf.RoundToInt(power) + " — release to lock" + TimerText();
            case GameState.StepHook:
                return "Step 3 — Hook: " + HookText() + "   •   OK to throw" + TimerText();
            case GameState.Rolling:
                return "Ball rolling…";
            default:
                return lastResult;
        }
    }

    private void UpdateHUD()
    {
        string playerLabel;
        if (mode == BowlingMode.TwoPlayer) playerLabel = "Player " + (currentPlayer + 1);
        else if (mode == BowlingMode.Practice) playerLabel = "Practice";
        else if (mode == BowlingMode.SparePractice) playerLabel = "Spare Practice";
        else if (mode == BowlingMode.QuickMatch) playerLabel = "Quick Match";
        else playerLabel = "Lynxos Bowling";

        int frames = IsScoredMode() ? TotalFrames : 0;
        ui.UpdateHUD(playerLabel, currentFrame, currentBall, scores[currentPlayer].Total(),
            pins.CountStanding(), StatusLine(), power / 100f, state == GameState.StepPower, frames);
        ui.UpdateScoreboard(scores[currentPlayer], currentFrame, TotalFrames);
        ui.SetScoreboardVisible(IsScoredMode());
    }
}
