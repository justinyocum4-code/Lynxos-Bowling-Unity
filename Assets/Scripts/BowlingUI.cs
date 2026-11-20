// Lynxos Bowling — all uGUI menus and HUD, built at runtime.
// Panels: Main Menu, Ball Select, Spare Practice select, More, Gear Shop,
// Achievements, HUD (with a compact scoreboard strip), Game Over.
// Gold #F5C518 on dark #0A0A0A, big text, explicit D-pad navigation with
// wrap-around, and every button speaks its label when focused so the game
// needs no TalkBack. (v1.2.0: under TalkBack the game stops speaking UI
// labels — TalkBack reads them — via Narration.SpeakUIKey.)
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class BowlingUI : MonoBehaviour
{
    // Callbacks wired by BowlingGameManager.
    public System.Action OnAnyButton;
    public System.Action OnFullGame;
    public System.Action OnQuickMatch;
    public System.Action OnPractice;
    public System.Action OnSparePracticeMenu;
    public System.Action OnTwoPlayers;
    public System.Action OnOpenBallSelect;
    public System.Action<int> OnBallSelected;
    public System.Action<int> OnSpareSelected;
    public System.Action OnToggleBumpers;
    public System.Action OnToggleVoice;
    public System.Action OnPlayAgain;
    public System.Action OnQuitToMenu;
    public System.Action OnOpenMore;
    public System.Action OnOpenGear;
    public System.Action OnOpenAchievements;
    public System.Action OnToggleTimer;
    public System.Action<int, int> OnGearSelected; // (kind, index): 0 ball, 1 skin, 2 theme, 3 shirt, 4 shoe, 5 accessory

    // Touch controls (tablet): shown only when the device has a touchscreen.
    public System.Action OnTouchAimLeft;
    public System.Action OnTouchAimRight;
    public System.Action OnTouchHookLeft;
    public System.Action OnTouchHookRight;
    public System.Action OnTouchThrowDown;
    public System.Action OnTouchThrowUp;

    private Canvas canvas;
    private Font font;
    private Text errorText;

    // True when no font could be found at all: menu text cannot render.
    // The manager reports this as a startup error.
    public bool FontMissing { get; private set; }

    private GameObject menuPanel;
    private GameObject ballPanel;
    private GameObject sparePanel;
    private GameObject morePanel;
    private GameObject gearPanel;
    private GameObject achPanel;
    private GameObject hudPanel;
    private GameObject gameOverPanel;
    private GameObject touchPanel;

    private ButtonSpeaker bumpersSpeaker;
    private ButtonSpeaker voiceSpeaker;
    private ButtonSpeaker timerSpeaker;

    private List<Button> menuButtons = new List<Button>();
    private List<Button> ballButtons = new List<Button>();
    private List<Button> spareButtons = new List<Button>();
    private List<Button> moreButtons = new List<Button>();
    private List<Button> gearButtons = new List<Button>();
    private List<Button> achButtons = new List<Button>();
    private List<Button> gameOverButtons = new List<Button>();

    private Button ballButton;
    private Button bumpersButton;
    private Button voiceButton;
    private Button timerButton;
    private Text menuBest;
    private Text gearCoins;
    private Text[] achLines = new Text[10];
    private Text[] missionLines = new Text[3];

    private Text hudPlayer;
    private Text hudFrame;
    private Text hudScore;
    private Text hudPins;
    private Text hudLine;
    private Text hudPower;
    private Image powerFill;
    private GameObject scoreboardStrip;
    private Image[] cellImages = new Image[10];
    private Text[] frameMarkTexts = new Text[10];
    private Text[] frameScoreTexts = new Text[10];

    private Text gameOverTitle;
    private Text gameOverDetail;

    // v1.3.0: the Gear Shop has grown (balls, colours, alleys, shirts,
    // shoes, accessories = 24 buttons) — too many for one screen. It is
    // now two levels: a category list, then the items in that category.
    // -1 = showing categories.
    private int gearCategory = -1;

    public void Build()
    {
        // The font ships INSIDE the game (Assets/Resources/DejaVuSans.ttf):
        // v1.1 relied on the device having a font, and on the TV both the
        // built-in lookup and the OS lookup came back empty, so no menu
        // text rendered at all. Built-in + OS fonts stay as fallbacks.
        font = Resources.Load<Font>("DejaVuSans");
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font == null) font = Font.CreateDynamicFontFromOSFont(new string[] { "Arial", "sans-serif", "Roboto", "Droid Sans" }, 32);
        FontMissing = font == null;
        if (FontMissing) Debug.LogWarning("[BowlingUI] No font found anywhere; menu text cannot render.");

        GameObject canvasObj = new GameObject("Canvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject esObj = new GameObject("EventSystem");
        esObj.AddComponent<EventSystem>();
        esObj.AddComponent<StandaloneInputModule>();

        BuildMenuPanel();
        BuildBallPanel();
        BuildSparePanel();
        BuildMorePanel();
        BuildGearPanel();
        BuildAchPanel();
        BuildHUDPanel();
        BuildTouchPanel();
        BuildGameOverPanel();
        BuildErrorLabel();
    }

    // Persistent startup-error line, created last so it draws above every
    // panel. Hidden until ShowStartupError fills it in.
    private void BuildErrorLabel()
    {
        errorText = CreateText(canvas.gameObject, "", 40, new Color(1f, 0.4f, 0.3f),
            new Vector2(0.5f, 1f), new Vector2(0f, -210f), new Vector2(1700f, 120f), TextAnchor.MiddleCenter);
        errorText.gameObject.SetActive(false);
    }

    // Shows a startup failure in big text (and the manager also speaks it),
    // so a broken start can never again fail silent on a black screen.
    public void ShowStartupError(string message)
    {
        try
        {
            if (errorText == null)
            {
                // Build() died before the label existed: make a bare one.
                if (canvas == null)
                {
                    GameObject canvasObj = new GameObject("ErrorCanvas");
                    canvas = canvasObj.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                }
                BuildErrorLabel();
            }
            if (errorText.gameObject.activeSelf)
            {
                // A second problem (e.g. the voice failing after startup):
                // append instead of replacing the first one.
                errorText.text += " | " + message;
                return;
            }
            errorText.text = "STARTUP PROBLEM: " + message;
            errorText.gameObject.SetActive(true);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[BowlingUI] Cannot show startup error: " + e.Message);
        }
    }

    // --- Panels ---

    private GameObject CreatePanel(string name, bool withBackground)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(canvas.transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        if (withBackground)
        {
            // Semi-transparent on purpose (v1.1 was 0.97): the 3D lane
            // stays visible behind the menu, so even if the text ever fails
            // again the screen is never just black.
            Image bg = go.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.04f, 0.04f, 0.55f);
        }
        go.SetActive(false);
        return go;
    }

    private void BuildMenuPanel()
    {
        menuPanel = CreatePanel("MainMenu", true);
        // v1.3.0: the bowler stands centre-screen behind the menu, so
        // the full-screen dim is lighter than other panels (0.30, not
        // 0.55) and the buttons get their own darker strip on the left.
        Image menuBg = menuPanel.GetComponent<Image>();
        if (menuBg != null) menuBg.color = new Color(0.04f, 0.04f, 0.04f, 0.30f);
        GameObject strip = new GameObject("MenuStrip");
        strip.transform.SetParent(menuPanel.transform, false);
        RectTransform srt = strip.AddComponent<RectTransform>();
        srt.anchorMin = new Vector2(0f, 0f);
        srt.anchorMax = new Vector2(0f, 1f);
        srt.offsetMin = Vector2.zero;
        srt.offsetMax = new Vector2(860f, 0f);
        Image stripImg = strip.AddComponent<Image>();
        stripImg.color = new Color(0.04f, 0.04f, 0.04f, 0.55f);
        stripImg.raycastTarget = false;

        // v1.3.0: title, subtitle and score line move to the left, over
        // the button strip, so the bowler owns the centre of the screen.
        CreateText(menuPanel, "LYNXOS BOWLING", 72, BowlingConfig.Gold,
            new Vector2(0f, 1f), new Vector2(60f, -70f), new Vector2(1100f, 100f), TextAnchor.MiddleLeft);
        CreateText(menuPanel, "Real 3D bowling", 30, Color.white,
            new Vector2(0f, 1f), new Vector2(60f, -160f), new Vector2(1100f, 50f), TextAnchor.MiddleLeft);

        // v1.2.0: nine buttons, so they are shorter and tighter than the
        // old seven. Best score and coins share one line above them.
        // v1.3.0: the buttons live on the LEFT side (x = -560), so the
        // bowler stands front and centre.
        menuBest = CreateText(menuPanel, "Best score: 0   •   Coins: 0", 30, BowlingConfig.Gold,
            new Vector2(0f, 1f), new Vector2(60f, -215f), new Vector2(1100f, 50f), TextAnchor.MiddleLeft);
        float y = -300f;
        menuButtons.Add(CreateButton(menuPanel, "Full Game", y, 760f, 76f, () => { if (OnFullGame != null) OnFullGame(); }, "full_game", 34, -560f));
        y -= 88f;
        menuButtons.Add(CreateButton(menuPanel, "Quick Match", y, 760f, 76f, () => { if (OnQuickMatch != null) OnQuickMatch(); }, "quick_match", 34, -560f));
        y -= 88f;
        menuButtons.Add(CreateButton(menuPanel, "Practice", y, 760f, 76f, () => { if (OnPractice != null) OnPractice(); }, "practice", 34, -560f));
        y -= 88f;
        menuButtons.Add(CreateButton(menuPanel, "Spare Practice", y, 760f, 76f, () => { if (OnSparePracticeMenu != null) OnSparePracticeMenu(); }, "spare_practice", 34, -560f));
        y -= 88f;
        menuButtons.Add(CreateButton(menuPanel, "2 Players", y, 760f, 76f, () => { if (OnTwoPlayers != null) OnTwoPlayers(); }, "two_players", 34, -560f));
        y -= 88f;
        ballButton = CreateButton(menuPanel, "Ball: Heavy 16 lb", y, 760f, 76f, () => { if (OnOpenBallSelect != null) OnOpenBallSelect(); }, "ball_select", 34, -560f);
        menuButtons.Add(ballButton);
        y -= 88f;
        bumpersButton = CreateButton(menuPanel, "Bumpers: Off", y, 760f, 76f, () => { if (OnToggleBumpers != null) OnToggleBumpers(); }, "bumpers_off", 34, -560f);
        menuButtons.Add(bumpersButton);
        bumpersSpeaker = bumpersButton.GetComponent<ButtonSpeaker>();
        y -= 88f;
        voiceButton = CreateButton(menuPanel, "Voice: On", y, 760f, 76f, () => { if (OnToggleVoice != null) OnToggleVoice(); }, "voice_on", 34, -560f);
        menuButtons.Add(voiceButton);
        voiceSpeaker = voiceButton.GetComponent<ButtonSpeaker>();
        y -= 88f;
        menuButtons.Add(CreateButton(menuPanel, "More", y, 760f, 76f, () => { if (OnOpenMore != null) OnOpenMore(); }, "more_btn", 34, -560f));
        WireVertical(menuButtons);
    }

    private void BuildBallPanel()
    {
        ballPanel = CreatePanel("BallSelect", true);
        CreateText(ballPanel, "Choose your ball", 56, BowlingConfig.Gold,
            new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(1200f, 80f), TextAnchor.MiddleCenter);
        // Buttons are rebuilt in ShowBallSelect: only owned balls are
        // listed (starters + whatever the player unlocked in the Gear Shop).
    }

    private void RebuildBallButtons()
    {
        foreach (Button b in ballButtons)
        {
            if (b != null) Object.Destroy(b.gameObject);
        }
        ballButtons.Clear();
        // 76 px buttons, 88 px apart: even with all 8 balls owned the
        // Back button still fits on a 1080 screen (last at -984).
        float y = -280f;
        for (int i = 0; i < BowlingConfig.Balls.Length; i++)
        {
            if (!GameProgression.OwnsBall(i)) continue;
            int index = i;
            string label = BowlingConfig.Balls[i].Name + " — " + BowlingConfig.Balls[i].Desc
                + (i == GameProgression.EquippedBall ? " — Equipped" : "");
            ballButtons.Add(CreateButton(ballPanel, label, y, 900f, 76f,
                () => { if (OnBallSelected != null) OnBallSelected(index); }, BowlingConfig.BallKeys[i]));
            y -= 88f;
        }
        ballButtons.Add(CreateButton(ballPanel, "Back", y, 760f, 76f,
            () => { if (OnQuitToMenu != null) OnQuitToMenu(); }, "back"));
        WireVertical(ballButtons);
    }

    private void BuildSparePanel()
    {
        sparePanel = CreatePanel("SpareSelect", true);
        CreateText(sparePanel, "Spare practice — choose a setup", 48, BowlingConfig.Gold,
            new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(1400f, 70f), TextAnchor.MiddleCenter);
        float y = -280f;
        string[] spareKeys = new string[] { "spare_710", "spare_47", "spare_610", "spare_310", "spare_245", "spare_124", "spare_bucket" };
        for (int i = 0; i < BowlingConfig.SpareSetups.Length; i++)
        {
            int index = i;
            spareButtons.Add(CreateButton(sparePanel, BowlingConfig.SpareSetups[i].Name, y, 760f, 84f,
                () => { if (OnSpareSelected != null) OnSpareSelected(index); }, spareKeys[i]));
            y -= 102f;
        }
        spareButtons.Add(CreateButton(sparePanel, "Back", y, 760f, 84f,
            () => { if (OnQuitToMenu != null) OnQuitToMenu(); }, "back"));
        WireVertical(spareButtons);
    }

    // v1.2.0: Gear Shop, Achievements and the turn-timer toggle live here,
    // one tap off the main menu.
    private void BuildMorePanel()
    {
        morePanel = CreatePanel("More", true);
        CreateText(morePanel, "More", 56, BowlingConfig.Gold,
            new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(1200f, 80f), TextAnchor.MiddleCenter);
        float y = -280f;
        moreButtons.Add(CreateButton(morePanel, "Gear Shop", y, 760f, 84f,
            () => { if (OnOpenGear != null) OnOpenGear(); }, "gear_shop"));
        y -= 102f;
        moreButtons.Add(CreateButton(morePanel, "Achievements", y, 760f, 84f,
            () => { if (OnOpenAchievements != null) OnOpenAchievements(); }, "achievements_btn"));
        y -= 102f;
        timerButton = CreateButton(morePanel, "Turn Timer: Off", y, 760f, 84f,
            () => { if (OnToggleTimer != null) OnToggleTimer(); }, "timer_off");
        moreButtons.Add(timerButton);
        timerSpeaker = timerButton.GetComponent<ButtonSpeaker>();
        y -= 102f;
        moreButtons.Add(CreateButton(morePanel, "Back", y, 760f, 84f,
            () => { if (OnQuitToMenu != null) OnQuitToMenu(); }, "back"));
        WireVertical(moreButtons);

        // Today's 3 missions, with live progress (read-only lines).
        float my = -700f;
        for (int i = 0; i < 3; i++)
        {
            missionLines[i] = CreateText(morePanel, "", 26, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0f, my), new Vector2(1500f, 44f), TextAnchor.MiddleCenter);
            my -= 52f;
        }
    }

    private void RefreshMissions()
    {
        GameProgression.MissionDef[] defs = GameProgression.MissionsToday();
        for (int i = 0; i < 3; i++)
        {
            if (missionLines[i] == null) continue;
            bool done = GameProgression.MissionDone(i);
            int prog = GameProgression.MissionProgress(i);
            missionLines[i].text = (done ? "Mission done: " : "Mission: ") + defs[i].Desc
                + (done ? "" : " — " + prog + "/" + defs[i].Target);
            missionLines[i].color = done ? BowlingConfig.Gold : Color.white;
        }
    }

    private void BuildGearPanel()
    {
        gearPanel = CreatePanel("GearShop", true);
        CreateText(gearPanel, "Gear Shop — earn coins by playing", 44, BowlingConfig.Gold,
            new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(1500f, 70f), TextAnchor.MiddleCenter);
        gearCoins = CreateText(gearPanel, "Coins: 0", 32, Color.white,
            new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(900f, 50f), TextAnchor.MiddleCenter);
        // Buttons are rebuilt in ShowGear: unlockable balls, ball colours
        // and alley looks, each showing its price or Owned / Equipped.
    }

    private void RebuildGearButtons()
    {
        foreach (Button b in gearButtons)
        {
            if (b != null) Object.Destroy(b.gameObject);
        }
        gearButtons.Clear();
        if (gearCoins != null) gearCoins.text = "Coins: " + GameProgression.Coins;

        // v1.3.0: two levels. At the top level the shop lists its six
        // categories; inside a category it lists that category's items.
        // One screen never holds more than 7 buttons, so everything
        // fits on a 1080 screen.
        if (gearCategory < 0)
        {
            float y = -250f;
            string[] cats = new string[] { "Balls", "Ball Colours", "Alley Looks", "Shirts", "Shoes", "Accessories" };
            string[] catKeys = new string[] { "ball_select", "skin_ruby", "theme_midnight", "shirt_classic", "shoes_classic", "acc_headband" };
            for (int i = 0; i < cats.Length; i++)
            {
                int index = i;
                gearButtons.Add(CreateButton(gearPanel, cats[i], y, 900f, 76f,
                    () => { gearCategory = index; RebuildGearButtons(); SelectFirstGear(); }, catKeys[i], 30));
                y -= 88f;
            }
            gearButtons.Add(CreateButton(gearPanel, "Back", y, 760f, 76f,
                () => { if (OnQuitToMenu != null) OnQuitToMenu(); }, "back", 30));
            WireVertical(gearButtons);
            return;
        }

        float iy = -250f;
        if (gearCategory == 0)
        {
            for (int i = 0; i < BowlingConfig.Balls.Length; i++)
            {
                if (BowlingConfig.Balls[i].Cost <= 0) continue; // starters are not sold here
                int index = i;
                string state = GameProgression.OwnsBall(i)
                    ? (i == GameProgression.EquippedBall ? " — Equipped" : " — Owned")
                    : " — " + BowlingConfig.Balls[i].Cost + " coins";
                gearButtons.Add(CreateButton(gearPanel, "Ball: " + BowlingConfig.Balls[i].Name + state, iy, 900f, 56f,
                    () => { if (OnGearSelected != null) OnGearSelected(0, index); }, BowlingConfig.BallKeys[i], 26));
                iy -= 64f;
            }
        }
        else if (gearCategory == 1)
        {
            for (int i = 0; i < BowlingConfig.Skins.Length; i++)
            {
                int index = i;
                string state = GameProgression.OwnsSkin(i)
                    ? (i == GameProgression.EquippedSkin ? " — Equipped" : " — Owned")
                    : " — " + BowlingConfig.Skins[i].Cost + " coins";
                gearButtons.Add(CreateButton(gearPanel, "Colour: " + BowlingConfig.Skins[i].Name + state, iy, 900f, 56f,
                    () => { if (OnGearSelected != null) OnGearSelected(1, index); }, BowlingConfig.Skins[i].Key, 26));
                iy -= 64f;
            }
        }
        else if (gearCategory == 2)
        {
            for (int i = 0; i < BowlingConfig.Themes.Length; i++)
            {
                int index = i;
                string state = GameProgression.OwnsTheme(i)
                    ? (i == GameProgression.EquippedTheme ? " — Equipped" : " — Owned")
                    : " — " + BowlingConfig.Themes[i].Cost + " coins";
                gearButtons.Add(CreateButton(gearPanel, "Alley: " + BowlingConfig.Themes[i].Name + state, iy, 900f, 56f,
                    () => { if (OnGearSelected != null) OnGearSelected(2, index); }, BowlingConfig.Themes[i].Key, 26));
                iy -= 64f;
            }
        }
        else if (gearCategory == 3)
        {
            for (int i = 0; i < BowlingConfig.Shirts.Length; i++)
            {
                int index = i;
                string state = GameProgression.OwnsShirt(i)
                    ? (i == GameProgression.EquippedShirt ? " — Equipped" : " — Owned")
                    : " — " + BowlingConfig.Shirts[i].Cost + " coins";
                gearButtons.Add(CreateButton(gearPanel, "Shirt: " + BowlingConfig.Shirts[i].Name + state, iy, 900f, 56f,
                    () => { if (OnGearSelected != null) OnGearSelected(3, index); }, BowlingConfig.Shirts[i].Key, 26));
                iy -= 64f;
            }
        }
        else if (gearCategory == 4)
        {
            for (int i = 0; i < BowlingConfig.Shoes.Length; i++)
            {
                int index = i;
                string state = GameProgression.OwnsShoe(i)
                    ? (i == GameProgression.EquippedShoe ? " — Equipped" : " — Owned")
                    : " — " + BowlingConfig.Shoes[i].Cost + " coins";
                gearButtons.Add(CreateButton(gearPanel, "Shoes: " + BowlingConfig.Shoes[i].Name + state, iy, 900f, 56f,
                    () => { if (OnGearSelected != null) OnGearSelected(4, index); }, BowlingConfig.Shoes[i].Key, 26));
                iy -= 64f;
            }
        }
        else if (gearCategory == 5)
        {
            for (int i = 0; i < BowlingConfig.Accessories.Length; i++)
            {
                int index = i;
                string state = GameProgression.OwnsAccessory(i)
                    ? (i == GameProgression.EquippedAccessory ? " — Equipped" : " — Owned")
                    : " — " + BowlingConfig.Accessories[i].Cost + " coins";
                gearButtons.Add(CreateButton(gearPanel, "Accessory: " + BowlingConfig.Accessories[i].Name + state, iy, 900f, 56f,
                    () => { if (OnGearSelected != null) OnGearSelected(5, index); }, BowlingConfig.Accessories[i].Key, 26));
                iy -= 64f;
            }
        }
        gearButtons.Add(CreateButton(gearPanel, "Back", iy, 760f, 64f,
            () => { GearBack(); }, "back", 28));
        WireVertical(gearButtons);
    }

    // Back inside a gear category returns to the category list; Back at
    // the top level leaves the shop for the main menu.
    private void GearBack()
    {
        if (gearCategory >= 0)
        {
            gearCategory = -1;
            RebuildGearButtons();
            SelectFirstGear();
        }
        else if (OnQuitToMenu != null)
        {
            OnQuitToMenu();
        }
    }

    private void SelectFirstGear()
    {
        if (gearButtons.Count > 0 && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(gearButtons[0].gameObject);
    }

    private void BuildAchPanel()
    {
        achPanel = CreatePanel("Achievements", true);
        CreateText(achPanel, "Achievements", 56, BowlingConfig.Gold,
            new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(1200f, 80f), TextAnchor.MiddleCenter);
        float y = -220f;
        for (int i = 0; i < GameProgression.Achievements.Length; i++)
        {
            achLines[i] = CreateText(achPanel, "", 26, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(1500f, 44f), TextAnchor.MiddleCenter);
            y -= 62f;
        }
        achButtons.Add(CreateButton(achPanel, "Back", y, 760f, 76f,
            () => { if (OnQuitToMenu != null) OnQuitToMenu(); }, "back"));
        WireVertical(achButtons);
    }

    private void RefreshAchievements()
    {
        for (int i = 0; i < GameProgression.Achievements.Length; i++)
        {
            if (achLines[i] == null) continue;
            GameProgression.Achievement a = GameProgression.Achievements[i];
            bool done = GameProgression.IsAchievementUnlocked(a.Id);
            achLines[i].text = (done ? "Unlocked: " : "Locked: ") + a.Name + " — " + a.Desc;
            achLines[i].color = done ? BowlingConfig.Gold : Color.white;
        }
    }

    private void BuildHUDPanel()
    {
        hudPanel = CreatePanel("HUD", false);

        hudPlayer = CreateText(hudPanel, "Lynxos Bowling", 30, Color.white,
            new Vector2(0f, 1f), new Vector2(24f, -16f), new Vector2(500f, 44f), TextAnchor.MiddleLeft);
        hudFrame = CreateText(hudPanel, "Frame 1  •  Ball 1", 30, Color.white,
            new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(700f, 44f), TextAnchor.MiddleCenter);
        hudScore = CreateText(hudPanel, "Score 0", 30, BowlingConfig.Gold,
            new Vector2(1f, 1f), new Vector2(-24f, -16f), new Vector2(500f, 44f), TextAnchor.MiddleRight);
        hudPins = CreateText(hudPanel, "Pins standing: 10", 28, Color.white,
            new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(700f, 40f), TextAnchor.MiddleCenter);

        // Compact scoreboard strip (frames in the current match).
        scoreboardStrip = new GameObject("Scoreboard");
        scoreboardStrip.transform.SetParent(hudPanel.transform, false);
        RectTransform stripRt = scoreboardStrip.AddComponent<RectTransform>();
        stripRt.anchorMin = new Vector2(0.5f, 1f);
        stripRt.anchorMax = new Vector2(0.5f, 1f);
        stripRt.anchoredPosition = new Vector2(0f, -118f);
        stripRt.sizeDelta = new Vector2(1740f, 96f);
        for (int i = 0; i < 10; i++)
        {
            GameObject cell = new GameObject("Frame" + (i + 1));
            cell.transform.SetParent(scoreboardStrip.transform, false);
            RectTransform crt = cell.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = new Vector2(-783f + i * 174f, 0f);
            crt.sizeDelta = new Vector2(170f, 96f);
            Image cellImg = cell.AddComponent<Image>();
            cellImg.color = new Color(0.12f, 0.12f, 0.12f);
            cellImages[i] = cellImg;
            frameMarkTexts[i] = CreateText(cell, "", 26, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(160f, 44f), TextAnchor.MiddleCenter);
            frameScoreTexts[i] = CreateText(cell, "", 24, BowlingConfig.Gold,
                new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(160f, 44f), TextAnchor.MiddleCenter);
        }

        // Big spoken-line mirror at the bottom, above the power bar.
        // v1.3.0: moved up to clear the taller power meter.
        hudLine = CreateText(hudPanel, "", 40, BowlingConfig.Gold,
            new Vector2(0.5f, 0f), new Vector2(0f, 215f), new Vector2(1700f, 60f), TextAnchor.MiddleCenter);

        // v1.3.0: the power meter is rebuilt big and unmissable. The old
        // one (900x36, dark grey, no border) read as missing on the TV.
        // The bar is now 1200x90 with a bright gold border, the fill
        // changes colour as it climbs (green -> gold -> red), and a big
        // percent number sits beside it. It fills through the whole
        // power step, from the moment OK is held to the moment it locks.
        GameObject barBorder = new GameObject("PowerBarBorder");
        barBorder.transform.SetParent(hudPanel.transform, false);
        RectTransform borderRt = barBorder.AddComponent<RectTransform>();
        borderRt.anchorMin = new Vector2(0.5f, 0f);
        borderRt.anchorMax = new Vector2(0.5f, 0f);
        borderRt.anchoredPosition = new Vector2(0f, 100f);
        borderRt.sizeDelta = new Vector2(1224f, 114f);
        Image borderImg = barBorder.AddComponent<Image>();
        borderImg.color = BowlingConfig.Gold;

        GameObject barBg = new GameObject("PowerBarBg");
        barBg.transform.SetParent(barBorder.transform, false);
        RectTransform barRt = barBg.AddComponent<RectTransform>();
        barRt.anchorMin = new Vector2(0.5f, 0.5f);
        barRt.anchorMax = new Vector2(0.5f, 0.5f);
        barRt.anchoredPosition = Vector2.zero;
        barRt.sizeDelta = new Vector2(1200f, 90f);
        Image barBgImg = barBg.AddComponent<Image>();
        barBgImg.color = new Color(0.08f, 0.08f, 0.08f);

        GameObject fillObj = new GameObject("PowerBarFill");
        fillObj.transform.SetParent(barBg.transform, false);
        RectTransform fillRt = fillObj.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        powerFill = fillObj.AddComponent<Image>();
        powerFill.type = Image.Type.Filled;
        powerFill.fillMethod = Image.FillMethod.Horizontal;
        powerFill.fillOrigin = 0;
        powerFill.fillAmount = 0f;
        powerFill.color = BowlingConfig.Gold;

        hudPower = CreateText(hudPanel, "", 48, BowlingConfig.Gold,
            new Vector2(0.5f, 0f), new Vector2(640f, 100f), new Vector2(280f, 90f), TextAnchor.MiddleLeft);
    }

    // Touch controls for tablet play: aim + hook buttons and a big hold
    // to throw button. Lives inside the HUD panel, so it only shows while
    // playing, and only on devices with a touchscreen — the TV layout is
    // untouched. Menu buttons already accept taps through the
    // StandaloneInputModule.
    // True on Android TV devices (Onn 4K Pro). UiModeManager reports
    // UI_MODE_TYPE_TELEVISION (4) on TVs, NORMAL (1) on phones/tablets.
    // False on any failure, so a broken check never hides touch controls
    // from a real touchscreen.
    private static bool IsTelevision()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject uiModeManager = activity.Call<AndroidJavaObject>("getSystemService", "uimode"))
            {
                return uiModeManager.Call<int>("getCurrentModeType") == 4;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[BowlingUI] TV check failed: " + e.Message);
            return false;
        }
#else
        return false;
#endif
    }

    private void BuildTouchPanel()
    {
        touchPanel = new GameObject("TouchControls");
        touchPanel.transform.SetParent(hudPanel.transform, false);
        RectTransform trt = touchPanel.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        CreateTouchButton("◀ Aim", new Vector2(-620f, 300f), new Vector2(300f, 110f), 30,
            () => { if (OnTouchAimLeft != null) OnTouchAimLeft(); });
        CreateTouchButton("Aim ▶", new Vector2(-300f, 300f), new Vector2(300f, 110f), 30,
            () => { if (OnTouchAimRight != null) OnTouchAimRight(); });
        CreateTouchButton("◀ Hook", new Vector2(-620f, 430f), new Vector2(300f, 110f), 30,
            () => { if (OnTouchHookLeft != null) OnTouchHookLeft(); });
        CreateTouchButton("Hook ▶", new Vector2(-300f, 430f), new Vector2(300f, 110f), 30,
            () => { if (OnTouchHookRight != null) OnTouchHookRight(); });

        GameObject throwBtn = CreateTouchButton("HOLD TO THROW", new Vector2(620f, 300f),
            new Vector2(440f, 160f), 40, null);
        AddHoldHandlers(throwBtn,
            () => { if (OnAnyButton != null) OnAnyButton(); if (OnTouchThrowDown != null) OnTouchThrowDown(); },
            () => { if (OnTouchThrowUp != null) OnTouchThrowUp(); });

        // v1.1.4: the Onn TV reports touch support though it has no
        // touchscreen, so the touch buttons showed on TV and stole the
        // D-pad from aiming. TV devices never get the touch panel.
        // (Build #14 failed: Unity's DeviceType has no Television value.
        // Detect TV through Android's UiModeManager instead — see
        // IsTelevision below.)
        touchPanel.SetActive(Input.touchSupported && !IsTelevision());
    }

    private GameObject CreateTouchButton(string label, Vector2 pos, Vector2 size, int fontSize,
        System.Action onClick)
    {
        GameObject go = new GameObject("Touch_" + label);
        go.transform.SetParent(touchPanel.transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image img = go.AddComponent<Image>();
        img.color = new Color(0.28f, 0.28f, 0.28f);

        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = new Color(0.28f, 0.28f, 0.28f);
        cb.highlightedColor = BowlingConfig.Gold;
        cb.pressedColor = new Color(0.7f, 0.55f, 0.05f);
        cb.selectedColor = BowlingConfig.Gold;
        cb.colorMultiplier = 1f;
        btn.colors = cb;

        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(go.transform, false);
        RectTransform lrt = labelObj.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
        Text txt = labelObj.AddComponent<Text>();
        txt.font = font;
        txt.text = label;
        txt.fontSize = fontSize;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;

        if (onClick != null)
        {
            btn.onClick.AddListener(() =>
            {
                if (OnAnyButton != null) OnAnyButton();
                onClick();
            });
        }
        return go;
    }

    private void AddHoldHandlers(GameObject go, System.Action down, System.Action up)
    {
        EventTrigger trigger = go.AddComponent<EventTrigger>();
        EventTrigger.Entry downEntry = new EventTrigger.Entry();
        downEntry.eventID = EventTriggerType.PointerDown;
        downEntry.callback.AddListener((data) => { if (down != null) down(); });
        trigger.triggers.Add(downEntry);
        EventTrigger.Entry upEntry = new EventTrigger.Entry();
        upEntry.eventID = EventTriggerType.PointerUp;
        upEntry.callback.AddListener((data) => { if (up != null) up(); });
        trigger.triggers.Add(upEntry);
    }

    private void BuildGameOverPanel()
    {
        gameOverPanel = CreatePanel("GameOver", true);
        gameOverTitle = CreateText(gameOverPanel, "Game Over", 64, BowlingConfig.Gold,
            new Vector2(0.5f, 1f), new Vector2(0f, -200f), new Vector2(1200f, 90f), TextAnchor.MiddleCenter);
        gameOverDetail = CreateText(gameOverPanel, "", 36, Color.white,
            new Vector2(0.5f, 1f), new Vector2(0f, -330f), new Vector2(1200f, 200f), TextAnchor.MiddleCenter);
        gameOverButtons.Add(CreateButton(gameOverPanel, "Play Again", -580f, 760f, 84f,
            () => { if (OnPlayAgain != null) OnPlayAgain(); }, "play_again"));
        gameOverButtons.Add(CreateButton(gameOverPanel, "Main Menu", -682f, 760f, 84f,
            () => { if (OnQuitToMenu != null) OnQuitToMenu(); }, "main_menu_btn"));
        WireVertical(gameOverButtons);
    }

    // --- Widgets ---

    private Text CreateText(GameObject parent, string text, int size, Color color,
        Vector2 anchor, Vector2 pos, Vector2 sizeDelta, TextAnchor align)
    {
        GameObject go = new GameObject("Text");
        go.transform.SetParent(parent.transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;
        Text t = go.AddComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        return t;
    }

    private Button CreateButton(GameObject parent, string label, float y, float width, float height,
        System.Action onClick, string speakKey, int fontSize = 34, float x = 0f)
    {
        GameObject go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent.transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        // v1.3.0: x lets the menu buttons sit on the left side (x = -560)
        // instead of centred, so the bowler owns the middle of the screen.
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(width, height);
        Image img = go.AddComponent<Image>();
        img.color = new Color(0.28f, 0.28f, 0.28f);

        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = new Color(0.28f, 0.28f, 0.28f);
        cb.highlightedColor = BowlingConfig.Gold;
        cb.pressedColor = new Color(0.7f, 0.55f, 0.05f);
        cb.selectedColor = BowlingConfig.Gold;
        cb.colorMultiplier = 1f;
        btn.colors = cb;

        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(go.transform, false);
        RectTransform lrt = labelObj.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
        Text txt = labelObj.AddComponent<Text>();
        txt.font = font;
        txt.text = label;
        txt.fontSize = fontSize;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;

        btn.onClick.AddListener(() =>
        {
            if (OnAnyButton != null) OnAnyButton();
            if (onClick != null) onClick();
        });

        ButtonSpeaker speaker = go.AddComponent<ButtonSpeaker>();
        speaker.SpeakText = label;
        speaker.SpeakKey = speakKey;
        speaker.Label = txt;
        return btn;
    }

    // Speaks the button label when it gains focus; swaps the label to black
    // on the gold focused background for contrast. Uses the baked voice
    // clip (SpeakKey) when one exists, so buttons speak even on a TV
    // whose Android TTS is silent; falls back to live TTS otherwise.
    // v1.2.0: SpeakUIKey — under TalkBack the game stays quiet here, since
    // TalkBack reads the focused button itself.
    private class ButtonSpeaker : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        public string SpeakText;
        public string SpeakKey;
        public Text Label;

        public void OnSelect(BaseEventData eventData)
        {
            if (Label != null) Label.color = Color.black;
            if (!string.IsNullOrEmpty(SpeakKey)) Narration.SpeakUIKey(SpeakKey, SpeakText);
            else if (!string.IsNullOrEmpty(SpeakText)) Narration.Speak(SpeakText);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (Label != null) Label.color = Color.white;
        }
    }

    private void WireVertical(List<Button> buttons)
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            Navigation nav = new Navigation();
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnUp = buttons[(i - 1 + buttons.Count) % buttons.Count];
            nav.selectOnDown = buttons[(i + 1) % buttons.Count];
            buttons[i].navigation = nav;
        }
    }

    // --- Show / update ---

    private void SetOnly(GameObject panel)
    {
        // Null-safe: if a panel failed to build, the others still toggle.
        if (menuPanel != null) menuPanel.SetActive(panel == menuPanel);
        if (ballPanel != null) ballPanel.SetActive(panel == ballPanel);
        if (sparePanel != null) sparePanel.SetActive(panel == sparePanel);
        if (morePanel != null) morePanel.SetActive(panel == morePanel);
        if (gearPanel != null) gearPanel.SetActive(panel == gearPanel);
        if (achPanel != null) achPanel.SetActive(panel == achPanel);
        if (hudPanel != null) hudPanel.SetActive(panel == hudPanel);
        if (gameOverPanel != null) gameOverPanel.SetActive(panel == gameOverPanel);
    }

    public void ShowMainMenu(int bestScore, string ballName, bool bumpersOn, bool voiceOn)
    {
        SetOnly(menuPanel);
        RefreshMenuLabels(bestScore, ballName, bumpersOn, voiceOn);
        if (menuButtons.Count > 0 && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(menuButtons[0].gameObject);
    }

    public void RefreshMenuLabels(int bestScore, string ballName, bool bumpersOn, bool voiceOn)
    {
        if (menuBest != null) menuBest.text = "Best score: " + bestScore + "   •   Coins: " + GameProgression.Coins;
        if (ballButton != null) ballButton.GetComponentInChildren<Text>().text = "Ball: " + ballName;
        if (bumpersButton != null) bumpersButton.GetComponentInChildren<Text>().text = "Bumpers: " + (bumpersOn ? "On" : "Off");
        if (voiceButton != null) voiceButton.GetComponentInChildren<Text>().text = "Voice: " + (voiceOn ? "On" : "Off");
        // The toggle buttons speak their CURRENT state when focused.
        if (bumpersSpeaker != null) bumpersSpeaker.SpeakKey = bumpersOn ? "bumpers_on" : "bumpers_off";
        if (voiceSpeaker != null) voiceSpeaker.SpeakKey = voiceOn ? "voice_on" : "voice_off";
    }

    public void ShowBallSelect()
    {
        RebuildBallButtons();
        SetOnly(ballPanel);
        if (ballButtons.Count > 0 && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(ballButtons[0].gameObject);
    }

    public void ShowSpareSelect()
    {
        SetOnly(sparePanel);
        if (spareButtons.Count > 0 && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(spareButtons[0].gameObject);
    }

    public void ShowMore(bool timerOn)
    {
        SetOnly(morePanel);
        if (timerButton != null) timerButton.GetComponentInChildren<Text>().text = "Turn Timer: " + (timerOn ? "On" : "Off");
        if (timerSpeaker != null) timerSpeaker.SpeakKey = timerOn ? "timer_on" : "timer_off";
        RefreshMissions();
        if (moreButtons.Count > 0 && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(moreButtons[0].gameObject);
    }

    public void ShowGear()
    {
        gearCategory = -1; // v1.3.0: the shop always opens at the categories
        RebuildGearButtons();
        SetOnly(gearPanel);
        if (gearButtons.Count > 0 && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(gearButtons[0].gameObject);
    }

    public void ShowAchievements()
    {
        RefreshAchievements();
        SetOnly(achPanel);
        if (achButtons.Count > 0 && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(achButtons[0].gameObject);
    }

    public void ShowHUD()
    {
        SetOnly(hudPanel);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    public void ShowGameOver(string title, string detail)
    {
        SetOnly(gameOverPanel);
        if (gameOverTitle != null) gameOverTitle.text = title;
        if (gameOverDetail != null) gameOverDetail.text = detail;
        if (gameOverButtons.Count > 0 && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(gameOverButtons[0].gameObject);
    }

    public void UpdateHUD(string player, int frame, int ball, int score, int pinsStanding,
        string line, float power01, bool showPower, int totalFrames)
    {
        if (hudPlayer != null) hudPlayer.text = player;
        if (hudFrame != null)
        {
            hudFrame.text = totalFrames > 0
                ? "Frame " + frame + " of " + totalFrames + "  •  Ball " + ball
                : "Frame " + frame + "  •  Ball " + ball;
        }
        if (hudScore != null) hudScore.text = "Score " + score;
        if (hudPins != null) hudPins.text = "Pins standing: " + pinsStanding;
        if (hudLine != null) hudLine.text = line;
        if (powerFill != null)
        {
            powerFill.fillAmount = power01;
            // v1.3.0: the fill changes colour as it climbs — green, then
            // gold, then red-orange at full power.
            powerFill.color = power01 < 0.5f ? new Color(0.20f, 0.80f, 0.25f)
                : power01 < 0.75f ? BowlingConfig.Gold
                : new Color(1f, 0.30f, 0.08f);
        }
        if (hudPower != null) hudPower.text = showPower ? Mathf.RoundToInt(power01 * 100f) + "%" : "";
    }

    public void UpdateScoreboard(BowlingScore score, int currentFrame, int totalFrames)
    {
        for (int f = 1; f <= 10; f++)
        {
            bool inMatch = f <= totalFrames;
            if (cellImages[f - 1] != null) cellImages[f - 1].gameObject.SetActive(inMatch);
            if (!inMatch) continue;
            if (frameMarkTexts[f - 1] != null) frameMarkTexts[f - 1].text = score.FrameRolls(f);
            int cum = score.FrameCumulativeScore(f);
            if (frameScoreTexts[f - 1] != null) frameScoreTexts[f - 1].text = cum < 0 ? "" : cum.ToString();
            if (cellImages[f - 1] != null) cellImages[f - 1].color = (f == currentFrame)
                ? new Color(0.35f, 0.28f, 0.05f)
                : new Color(0.12f, 0.12f, 0.12f);
        }
    }

    public void SetScoreboardVisible(bool visible)
    {
        if (scoreboardStrip != null) scoreboardStrip.SetActive(visible);
    }
}
