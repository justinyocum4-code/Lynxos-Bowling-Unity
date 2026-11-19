// Lynxos Bowling — game orchestration: modes, turns, input, narration.
// Input (Onn remote D-pad, matching the old v1.0): Up/Down = aim position
// (in boards), Left/Right = hook/spin, hold OK = wind up the power meter,
// release OK = throw. Escape/Back returns to the menu. While a menu is open
// the D-pad drives the buttons instead (EventSystem); while aiming, no
// button is selected so the arrows reach the game.
using UnityEngine;
using System.Collections;

public enum BowlingMode { FullGame, Practice, SparePractice, TwoPlayer }

public class BowlingGameManager : MonoBehaviour
{
    private enum GameState { Menu, BallSelect, SpareSelect, Aiming, Windup, Rolling, Resolving, GameOver }

    private GameState state;
    private BowlingMode mode;
    private LaneBuilder lane;
    private PinManager pins;
    private BallController ball;
    private BowlingUI ui;
    private SoundSynth sound;

    private BowlingScore[] scores = new BowlingScore[2];
    private int[] consecutiveStrikes = new int[2];
    private int currentPlayer;
    private int currentFrame = 1;
    private int currentBall = 1;
    private int ballIndex = 1; // Standard 14 lb
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

    private void Awake()
    {
        scores[0] = new BowlingScore();
        scores[1] = new BowlingScore();
        bestScore = PlayerPrefs.GetInt("LynxosBowlingBest", 0);

        GameObject laneObj = new GameObject("Lane");
        laneObj.transform.SetParent(transform, false);
        lane = laneObj.AddComponent<LaneBuilder>();
        lane.Build();

        GameObject pinObj = new GameObject("Pins");
        pinObj.transform.SetParent(transform, false);
        pins = pinObj.AddComponent<PinManager>();
        pins.Build();

        GameObject ballObj = new GameObject("Ball");
        ballObj.transform.SetParent(transform, false);
        ball = ballObj.AddComponent<BallController>();
        ball.Build();
        ball.OnPinHit += OnBallHitPin;

        GameObject soundObj = new GameObject("Sound");
        soundObj.transform.SetParent(transform, false);
        sound = soundObj.AddComponent<SoundSynth>();

        GameObject uiObj = new GameObject("UI");
        uiObj.transform.SetParent(transform, false);
        ui = uiObj.AddComponent<BowlingUI>();
        ui.Build();
        WireUI();

        ApplyBallSpec();
        lane.SetBumpers(bumpersOn);
        Narration.Enabled = voiceOn;
        ShowMainMenu();
    }

    private void WireUI()
    {
        ui.OnAnyButton = () => { if (sound != null) sound.PlayClick(); };
        ui.OnFullGame = () => StartGame(BowlingMode.FullGame);
        ui.OnPractice = () => StartGame(BowlingMode.Practice);
        ui.OnSparePracticeMenu = () => { state = GameState.SpareSelect; ui.ShowSpareSelect(); };
        ui.OnTwoPlayers = () => StartGame(BowlingMode.TwoPlayer);
        ui.OnOpenBallSelect = () => { state = GameState.BallSelect; ui.ShowBallSelect(); };
        ui.OnBallSelected = (i) =>
        {
            ballIndex = i;
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
            ui.RefreshMenuLabels(bestScore, BowlingConfig.Balls[ballIndex].Name, bumpersOn, voiceOn);
            Narration.Speak(bumpersOn ? "Bumpers on." : "Bumpers off.");
        };
        ui.OnToggleVoice = () =>
        {
            voiceOn = !voiceOn;
            Narration.Enabled = voiceOn;
            ui.RefreshMenuLabels(bestScore, BowlingConfig.Balls[ballIndex].Name, bumpersOn, voiceOn);
            if (voiceOn) Narration.Speak("Voice on.");
        };
        ui.OnPlayAgain = () => StartGame(mode);
        ui.OnQuitToMenu = () => ShowMainMenu();
    }

    private void ApplyBallSpec()
    {
        if (ball != null) ball.SetBallSpec(BowlingConfig.Balls[ballIndex]);
    }

    // --- Game flow ---

    private void StartGame(BowlingMode newMode)
    {
        mode = newMode;
        scores[0].Reset();
        scores[1].Reset();
        consecutiveStrikes[0] = 0;
        consecutiveStrikes[1] = 0;
        currentPlayer = 0;
        currentFrame = 1;
        currentBall = 1;
        practiceThrows = 0;
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
        state = GameState.Aiming;
        okWasReleased = false;
        inputLockout = 0.25f;
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
        else
        {
            string who = mode == BowlingMode.TwoPlayer ? "Player " + (currentPlayer + 1) + ". " : "";
            line = who + "Frame " + currentFrame + ", ball " + currentBall + ". "
                + standing + " pins standing.";
        }
        lastResult = line;
        Narration.Speak(line);
        UpdateHUD();
    }

    private void ShowMainMenu()
    {
        state = GameState.Menu;
        if (sound != null) sound.StopRoll();
        ui.ShowMainMenu(bestScore, BowlingConfig.Balls[ballIndex].Name, bumpersOn, voiceOn);
        Narration.Speak("Lynxos Bowling. Main menu.");
    }

    private void GameOver()
    {
        state = GameState.GameOver;
        sound.StopRoll();
        int s0 = scores[0].Total();
        int s1 = scores[1].Total();
        string detail;
        if (mode == BowlingMode.TwoPlayer)
        {
            string winner = s0 == s1 ? "It is a tie!" : (s0 > s1 ? "Player 1 wins!" : "Player 2 wins!");
            detail = "Player 1: " + s0 + "\nPlayer 2: " + s1 + "\n" + winner;
            Narration.Speak("Game over. Player 1 scored " + s0 + ". Player 2 scored " + s1 + ". " + winner);
        }
        else
        {
            bool newBest = s0 > bestScore;
            if (newBest)
            {
                bestScore = s0;
                PlayerPrefs.SetInt("LynxosBowlingBest", bestScore);
                PlayerPrefs.Save();
            }
            detail = "Final score: " + s0 + (newBest ? "\nNew best score!" : "\nBest score: " + bestScore);
            Narration.Speak("Game over. Final score " + s0 + "." + (newBest ? " New best score!" : ""));
        }
        ui.ShowGameOver("Game Over", detail);
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

        switch (state)
        {
            case GameState.Aiming:
                HandleAimingInput();
                break;
            case GameState.Windup:
                HandleWindupInput();
                break;
            case GameState.Rolling:
                HandleRolling();
                break;
            case GameState.Menu:
            case GameState.BallSelect:
            case GameState.SpareSelect:
                if (IsBackDown() && state != GameState.Menu) ShowMainMenu();
                break;
        }
    }

    private void HandleAimingInput()
    {
        if (inputLockout > 0f) return;

        if (IsBackDown())
        {
            ShowMainMenu();
            return;
        }

        bool okHeld = IsOkHeld();
        if (!okHeld) okWasReleased = true;

        // Aim: Up = right, Down = left, one board per press, auto-repeat held.
        int aimDir = 0;
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            aimDir = 1;
            aimHeldDir = 1;
            aimRepeatTimer = 0.4f;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            aimDir = -1;
            aimHeldDir = -1;
            aimRepeatTimer = 0.4f;
        }
        else if (Input.GetKeyUp(KeyCode.UpArrow) && aimHeldDir == 1)
        {
            aimHeldDir = 0;
        }
        else if (Input.GetKeyUp(KeyCode.DownArrow) && aimHeldDir == -1)
        {
            aimHeldDir = 0;
        }
        else if (aimHeldDir != 0 && (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow)))
        {
            aimRepeatTimer -= Time.deltaTime;
            if (aimRepeatTimer <= 0f)
            {
                aimDir = aimHeldDir;
                aimRepeatTimer = 0.12f;
            }
        }
        if (aimDir != 0) ChangeAim(aimDir);

        // Hook: Left = hook left, Right = hook right.
        if (Input.GetKeyDown(KeyCode.LeftArrow)) ChangeSpin(-1);
        else if (Input.GetKeyDown(KeyCode.RightArrow)) ChangeSpin(1);

        // Hold OK to wind up (only on a fresh press, after the key was released).
        if (okWasReleased && IsOkDown())
        {
            state = GameState.Windup;
            windupTime = 0f;
            power = 0f;
            Narration.Speak("Winding up. Release to throw.");
            UpdateHUD();
        }
    }

    private void HandleWindupInput()
    {
        windupTime += Time.deltaTime;
        power = Mathf.PingPong(windupTime * 110f, 100f); // ~0.9 s up, ~0.9 s down
        if (IsBackDown())
        {
            state = GameState.Aiming;
            power = 0f;
            UpdateHUD();
            return;
        }
        if (IsOkUp())
        {
            ThrowBall();
            return;
        }
        UpdateHUD();
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
        ball.SetBallSpec(BowlingConfig.Balls[ballIndex]);
        ball.ThrowBall(power / 100f, spin);
        state = GameState.Rolling;
        rollStartTime = Time.time;
        settleTimer = 0f;
        crashPlayed = false;
        gutterPlayed = false;
        gutterThisRoll = false;
        sound.StartRoll();
        Narration.Speak("Ball away. Power " + Mathf.RoundToInt(power) + ".");
        UpdateHUD();
    }

    // --- Rolling / resolving ---

    private void OnBallHitPin(float impactSpeed)
    {
        if (state != GameState.Rolling) return;
        if (!crashPlayed)
        {
            crashPlayed = true;
            sound.PlayCrash(Mathf.Clamp(impactSpeed / 8f, 0.3f, 1f));
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
                sound.PlayGutter();
            }
            else if (!gutterThisRoll)
            {
                // Reached the pins with no reported pin hit (rare): play a
                // soft crash so the throw still sounds right.
                sound.PlayCrash(0.4f);
            }
        }

        sound.SetRollVolume(Mathf.Clamp01(ball.CurrentSpeed / 8f) * 0.3f);

        bool slow = ball.CurrentSpeed < 0.15f;
        if (slow) settleTimer += Time.deltaTime;
        else settleTimer = 0f;

        bool fellOut = ball.Position.y < -1f;
        if (settleTimer > 0.6f || z > 20.3f || fellOut || Time.time - rollStartTime > 8f)
        {
            sound.StopRoll();
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
        string result;

        if (mode == BowlingMode.SparePractice)
        {
            result = fallen == 10
                ? "All pins down!"
                : fallen + (fallen == 1 ? " pin down, " : " pins down, ") + standing + " standing.";
        }
        else if (gutterThisRoll)
        {
            result = "Gutter ball. No pins down.";
            consecutiveStrikes[currentPlayer] = 0;
        }
        else if (fallen == 10 && currentBall == 1)
        {
            consecutiveStrikes[currentPlayer]++;
            result = consecutiveStrikes[currentPlayer] >= 3
                ? "Turkey! Three strikes in a row!"
                : "Strike!";
            sound.PlayCheer();
        }
        else
        {
            if (currentBall == 1) consecutiveStrikes[currentPlayer] = 0;
            bool isSpare = false;
            if (mode != BowlingMode.Practice && currentBall == 2)
            {
                int first = scores[currentPlayer].RollInFrame(currentFrame, 0);
                isSpare = first >= 0 && first + fallen == 10;
            }
            if (isSpare)
            {
                result = "Spare!";
                sound.PlaySpare();
            }
            else
            {
                result = fallen + (fallen == 1 ? " pin down, " : " pins down, ")
                    + standing + " standing.";
            }
        }

        lastResult = result;

        if (mode == BowlingMode.Practice)
        {
            practiceThrows++;
            Narration.Speak(result + " Practice throw " + practiceThrows + ".");
            pins.RackAll();
            currentBall = 1;
            BeginBall();
            return;
        }

        if (mode == BowlingMode.SparePractice)
        {
            if (standing == 0)
            {
                Narration.Speak("Spare made! Nice shot.");
                sound.PlaySpare();
            }
            else
            {
                Narration.Speak(result + " " + standing + " left. Try again.");
            }
            pins.RackPins(BowlingConfig.SpareSetups[spareIndex].Pins);
            currentBall = 1;
            BeginBall();
            return;
        }

        // Full game / 2 players: record the roll, then advance.
        scores[currentPlayer].AddRoll(fallen);
        Narration.Speak(result + " Score " + scores[currentPlayer].Total() + ".");

        bool frameOver;
        if (currentFrame <= 9)
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
            // 10th frame: a fresh rack after a strike (ball 1 or 2) or a
            // spare (balls 1+2); otherwise shoot at the pins left standing.
            if (currentFrame == 10 && NeedsFreshRack())
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

        if (currentFrame == 10)
        {
            // Two players: player 1 still has to bowl the 10th frame.
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

    // 10th frame only: does the next ball get a fresh full rack?
    // Ball 2: yes if ball 1 was a strike. Ball 3: yes if ball 2 was a
    // strike, or balls 1+2 made a spare.
    private bool NeedsFreshRack()
    {
        if (currentFrame != 10) return false;
        if (currentBall == 2)
        {
            return scores[currentPlayer].RollInFrame(10, 0) == 10;
        }
        int b1 = scores[currentPlayer].RollInFrame(10, 0);
        int b2 = scores[currentPlayer].RollInFrame(10, 1);
        if (b2 == 10) return true;
        if (b1 >= 0 && b2 >= 0 && b1 + b2 == 10) return true;
        return false;
    }

    // --- HUD ---

    private string StatusLine()
    {
        switch (state)
        {
            case GameState.Aiming:
                return "Aim: " + AimText() + "   •   Hook: " + HookText() + "   •   Hold OK to wind up";
            case GameState.Windup:
                return "Power " + Mathf.RoundToInt(power) + " — release to throw!";
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
        else playerLabel = "Lynxos Bowling";

        ui.UpdateHUD(playerLabel, currentFrame, currentBall, scores[currentPlayer].Total(),
            pins.CountStanding(), StatusLine(), power / 100f, state == GameState.Windup);
        ui.UpdateScoreboard(scores[currentPlayer], currentFrame);
        ui.SetScoreboardVisible(mode == BowlingMode.FullGame || mode == BowlingMode.TwoPlayer);
    }
}
