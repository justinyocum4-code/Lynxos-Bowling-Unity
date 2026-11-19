// Lynxos Bowling — all uGUI menus and HUD, built at runtime.
// Panels: Main Menu, Ball Select, Spare Practice select, HUD (with a
// compact 10-frame scoreboard strip), Game Over. Gold #F5C518 on dark
// #0A0A0A, big text, explicit D-pad navigation with wrap-around, and every
// button speaks its label when focused so the game needs no TalkBack.
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class BowlingUI : MonoBehaviour
{
    // Callbacks wired by BowlingGameManager.
    public System.Action OnAnyButton;
    public System.Action OnFullGame;
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

    private Canvas canvas;
    private Font font;
    private Text errorText;

    // True when no font could be found at all: menu text cannot render.
    // The manager reports this as a startup error.
    public bool FontMissing { get; private set; }

    private GameObject menuPanel;
    private GameObject ballPanel;
    private GameObject sparePanel;
    private GameObject hudPanel;
    private GameObject gameOverPanel;

    private List<Button> menuButtons = new List<Button>();
    private List<Button> ballButtons = new List<Button>();
    private List<Button> spareButtons = new List<Button>();
    private List<Button> gameOverButtons = new List<Button>();

    private Button ballButton;
    private Button bumpersButton;
    private Button voiceButton;
    private Text menuBest;

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
        BuildHUDPanel();
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
        CreateText(menuPanel, "LYNXOS BOWLING", 72, BowlingConfig.Gold,
            new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(1400f, 100f), TextAnchor.MiddleCenter);
        CreateText(menuPanel, "Real 3D bowling", 30, Color.white,
            new Vector2(0.5f, 1f), new Vector2(0f, -175f), new Vector2(1200f, 50f), TextAnchor.MiddleCenter);

        float y = -280f;
        menuButtons.Add(CreateButton(menuPanel, "Full Game", y, 760f, 84f, () => { if (OnFullGame != null) OnFullGame(); }));
        y -= 102f;
        menuButtons.Add(CreateButton(menuPanel, "Practice", y, 760f, 84f, () => { if (OnPractice != null) OnPractice(); }));
        y -= 102f;
        menuButtons.Add(CreateButton(menuPanel, "Spare Practice", y, 760f, 84f, () => { if (OnSparePracticeMenu != null) OnSparePracticeMenu(); }));
        y -= 102f;
        menuButtons.Add(CreateButton(menuPanel, "2 Players", y, 760f, 84f, () => { if (OnTwoPlayers != null) OnTwoPlayers(); }));
        y -= 102f;
        ballButton = CreateButton(menuPanel, "Ball: Standard 14 lb", y, 760f, 84f, () => { if (OnOpenBallSelect != null) OnOpenBallSelect(); });
        menuButtons.Add(ballButton);
        y -= 102f;
        bumpersButton = CreateButton(menuPanel, "Bumpers: Off", y, 760f, 84f, () => { if (OnToggleBumpers != null) OnToggleBumpers(); });
        menuButtons.Add(bumpersButton);
        y -= 102f;
        voiceButton = CreateButton(menuPanel, "Voice: On", y, 760f, 84f, () => { if (OnToggleVoice != null) OnToggleVoice(); });
        menuButtons.Add(voiceButton);
        y -= 102f;
        menuBest = CreateText(menuPanel, "Best score: 0", 30, BowlingConfig.Gold,
            new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(760f, 50f), TextAnchor.MiddleCenter);
        WireVertical(menuButtons);
    }

    private void BuildBallPanel()
    {
        ballPanel = CreatePanel("BallSelect", true);
        CreateText(ballPanel, "Choose your ball", 56, BowlingConfig.Gold,
            new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(1200f, 80f), TextAnchor.MiddleCenter);
        float y = -280f;
        for (int i = 0; i < BowlingConfig.Balls.Length; i++)
        {
            int index = i;
            string label = BowlingConfig.Balls[i].Name + BallHint(i);
            ballButtons.Add(CreateButton(ballPanel, label, y, 760f, 84f,
                () => { if (OnBallSelected != null) OnBallSelected(index); }));
            y -= 102f;
        }
        ballButtons.Add(CreateButton(ballPanel, "Back", y, 760f, 84f,
            () => { if (OnQuitToMenu != null) OnQuitToMenu(); }));
        WireVertical(ballButtons);
    }

    private string BallHint(int index)
    {
        if (index == 0) return " — more control";
        if (index == 2) return " — more power";
        return "";
    }

    private void BuildSparePanel()
    {
        sparePanel = CreatePanel("SpareSelect", true);
        CreateText(sparePanel, "Spare practice — choose a setup", 48, BowlingConfig.Gold,
            new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(1400f, 70f), TextAnchor.MiddleCenter);
        float y = -280f;
        for (int i = 0; i < BowlingConfig.SpareSetups.Length; i++)
        {
            int index = i;
            spareButtons.Add(CreateButton(sparePanel, BowlingConfig.SpareSetups[i].Name, y, 760f, 84f,
                () => { if (OnSpareSelected != null) OnSpareSelected(index); }));
            y -= 102f;
        }
        spareButtons.Add(CreateButton(sparePanel, "Back", y, 760f, 84f,
            () => { if (OnQuitToMenu != null) OnQuitToMenu(); }));
        WireVertical(spareButtons);
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

        // Compact 10-frame scoreboard strip.
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
        hudLine = CreateText(hudPanel, "", 40, BowlingConfig.Gold,
            new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(1700f, 60f), TextAnchor.MiddleCenter);

        GameObject barBg = new GameObject("PowerBarBg");
        barBg.transform.SetParent(hudPanel.transform, false);
        RectTransform barRt = barBg.AddComponent<RectTransform>();
        barRt.anchorMin = new Vector2(0.5f, 0f);
        barRt.anchorMax = new Vector2(0.5f, 0f);
        barRt.anchoredPosition = new Vector2(0f, 92f);
        barRt.sizeDelta = new Vector2(900f, 36f);
        Image barBgImg = barBg.AddComponent<Image>();
        barBgImg.color = new Color(0.2f, 0.2f, 0.2f);

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

        hudPower = CreateText(hudPanel, "", 30, Color.white,
            new Vector2(0.5f, 0f), new Vector2(0f, 42f), new Vector2(400f, 40f), TextAnchor.MiddleCenter);
    }

    private void BuildGameOverPanel()
    {
        gameOverPanel = CreatePanel("GameOver", true);
        gameOverTitle = CreateText(gameOverPanel, "Game Over", 64, BowlingConfig.Gold,
            new Vector2(0.5f, 1f), new Vector2(0f, -200f), new Vector2(1200f, 90f), TextAnchor.MiddleCenter);
        gameOverDetail = CreateText(gameOverPanel, "", 36, Color.white,
            new Vector2(0.5f, 1f), new Vector2(0f, -330f), new Vector2(1200f, 200f), TextAnchor.MiddleCenter);
        gameOverButtons.Add(CreateButton(gameOverPanel, "Play Again", -580f, 760f, 84f,
            () => { if (OnPlayAgain != null) OnPlayAgain(); }));
        gameOverButtons.Add(CreateButton(gameOverPanel, "Main Menu", -682f, 760f, 84f,
            () => { if (OnQuitToMenu != null) OnQuitToMenu(); }));
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
        System.Action onClick)
    {
        GameObject go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent.transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, y);
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
        txt.fontSize = 34;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;

        btn.onClick.AddListener(() =>
        {
            if (OnAnyButton != null) OnAnyButton();
            if (onClick != null) onClick();
        });

        ButtonSpeaker speaker = go.AddComponent<ButtonSpeaker>();
        speaker.SpeakText = label;
        speaker.Label = txt;
        return btn;
    }

    // Speaks the button label when it gains focus; swaps the label to black
    // on the gold focused background for contrast.
    private class ButtonSpeaker : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        public string SpeakText;
        public Text Label;

        public void OnSelect(BaseEventData eventData)
        {
            if (Label != null) Label.color = Color.black;
            if (!string.IsNullOrEmpty(SpeakText)) Narration.Speak(SpeakText);
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
        if (menuBest != null) menuBest.text = "Best score: " + bestScore;
        if (ballButton != null) ballButton.GetComponentInChildren<Text>().text = "Ball: " + ballName;
        if (bumpersButton != null) bumpersButton.GetComponentInChildren<Text>().text = "Bumpers: " + (bumpersOn ? "On" : "Off");
        if (voiceButton != null) voiceButton.GetComponentInChildren<Text>().text = "Voice: " + (voiceOn ? "On" : "Off");
    }

    public void ShowBallSelect()
    {
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
        string line, float power01, bool showPower)
    {
        if (hudPlayer != null) hudPlayer.text = player;
        if (hudFrame != null) hudFrame.text = "Frame " + frame + "  •  Ball " + ball;
        if (hudScore != null) hudScore.text = "Score " + score;
        if (hudPins != null) hudPins.text = "Pins standing: " + pinsStanding;
        if (hudLine != null) hudLine.text = line;
        if (powerFill != null) powerFill.fillAmount = power01;
        if (hudPower != null) hudPower.text = showPower ? "Power " + Mathf.RoundToInt(power01 * 100f) : "";
    }

    public void UpdateScoreboard(BowlingScore score, int currentFrame)
    {
        for (int f = 1; f <= 10; f++)
        {
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
