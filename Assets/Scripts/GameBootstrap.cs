// Lynxos Bowling — bootstrap + shared configuration.
// No scene editing is needed: the bootstrap destroys the template scene's
// contents at load and the GameManager builds the whole game at runtime.
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BowlingConfig
{
    // Real-world bowling dimensions, in metres.
    public const float LaneLength = 18.3f;       // foul line to head pin
    public const float LaneWidth = 1.06f;
    public const float ApproachLength = 2.0f;    // behind the foul line
    public const float PitLength = 2.2f;         // beyond the head pin
    public const float GutterWidth = 0.22f;
    public const float GutterDrop = 0.06f;       // gutter floor below lane surface
    public const float BallRadius = 0.108f;
    public const float PinHeight = 0.38f;
    public const float PinRadius = 0.06f;
    public const float HeadPinZ = 18.3f;
    public const float PinRowSpacing = 0.3048f;  // 12 inches
    public const float PinLateralSpacing = 0.3048f;

    // Aiming: a lane has 39 boards; board width = lane width / 39.
    public const float BoardWidth = 0.0272f;
    public const float AimMaxBoards = 14f;

    // Throw tuning (on-device tuning may be needed).
    public const float MinThrowSpeed = 4f;       // m/s at power 0
    public const float MaxThrowSpeed = 10f;      // m/s at power 100
    public const float HookAccelPerStep = 0.10f; // lateral m/s^2 per spin step
    public const int MaxSpin = 3;

    public const float PinFallTiltDegrees = 35f; // tilt past this = fallen
    public const float PinFallDistance = 0.3f;   // slid this far = fallen

    // Pin offsets [pinIndex, 0 = x lateral, 1 = z depth] relative to head pin.
    // Standard triangle: pin 1 head, 2-3 second row, 4-6 third row, 7-10 back row.
    public static readonly float[,] PinOffsets = new float[,]
    {
        {  0f,       0f      }, // 1
        { -0.1524f,  0.3048f }, // 2
        {  0.1524f,  0.3048f }, // 3
        { -0.3048f,  0.6096f }, // 4
        {  0f,       0.6096f }, // 5
        {  0.3048f,  0.6096f }, // 6
        { -0.4572f,  0.9144f }, // 7
        { -0.1524f,  0.9144f }, // 8
        {  0.1524f,  0.9144f }, // 9
        {  0.4572f,  0.9144f }, // 10
    };

    // v1.2.0: balls are side-grades, never pay-to-win. Each starter and
    // each unlockable ball trades weight, speed and hook against each
    // other — none is stronger overall. Cost 0 = starter, owned from the
    // start; anything with a Cost is bought with coins earned by playing.
    public struct BallSpec
    {
        public string Name;
        public float WeightKg;
        public float SpeedBonus;  // m/s added to the throw
        public float HookFactor;  // hook strength multiplier
        public Color Color;       // the ball's own colour
        public int Cost;          // coins, 0 = starter
        public string Desc;       // short speakable hint
        public BallSpec(string name, float weightKg, float speedBonus, float hookFactor,
            Color color, int cost, string desc)
        {
            Name = name;
            WeightKg = weightKg;
            SpeedBonus = speedBonus;
            HookFactor = hookFactor;
            Color = color;
            Cost = cost;
            Desc = desc;
        }
    }

    public static readonly BallSpec[] Balls = new BallSpec[]
    {
        // Starters (owned from the start).
        new BallSpec("Heavy 16 lb",    7.26f, -0.5f, 0.8f, new Color(0.96f, 0.77f, 0.09f), 0, "More power"),
        new BallSpec("Bouncy 13 lb",   5.90f,  0.4f, 1.4f, new Color(0.95f, 0.50f, 0.10f), 0, "More hook"),
        new BallSpec("Accurate 14 lb", 6.35f,  0.1f, 0.6f, new Color(0.85f, 0.85f, 0.90f), 0, "Straighter"),
        // Unlockable with earned coins — still side-grades.
        new BallSpec("Comet",    6.80f,  0.2f, 1.1f, new Color(0.90f, 0.25f, 0.15f),  60, "Fast and hooky"),
        new BallSpec("Midnight", 6.80f,  0.0f, 1.0f, new Color(0.15f, 0.25f, 0.70f), 120, "Steady all-rounder"),
        new BallSpec("Thunder",  7.26f, -0.2f, 1.2f, new Color(0.55f, 0.30f, 0.85f), 200, "Heavy hitter"),
        new BallSpec("Feather",  5.44f,  0.6f, 1.5f, new Color(0.50f, 0.85f, 0.90f), 200, "Light and lively"),
        new BallSpec("Gold Pro", 6.35f,  0.3f, 0.9f, new Color(1.00f, 0.85f, 0.20f), 350, "Smooth and quick"),
    };

    // Spoken clip key for each ball, in Balls[] order (used by the ball
    // select panel so every button speaks on a TV with no TTS voice).
    public static readonly string[] BallKeys = new string[]
    {
        "ball_heavy", "ball_bouncy", "ball_accurate", "ball_comet",
        "ball_midnight", "ball_thunder", "ball_feather", "ball_goldpro",
    };

    // Ball colours (skins): recolour any ball. Earned coins only.
    public struct GearSkin
    {
        public string Name;
        public Color Color;
        public int Cost;
        public string Key;
        public GearSkin(string name, Color color, int cost, string key)
        {
            Name = name;
            Color = color;
            Cost = cost;
            Key = key;
        }
    }

    public static readonly GearSkin[] Skins = new GearSkin[]
    {
        new GearSkin("Ruby",    new Color(0.85f, 0.10f, 0.15f),  40, "skin_ruby"),
        new GearSkin("Ocean",   new Color(0.10f, 0.45f, 0.85f),  40, "skin_ocean"),
        new GearSkin("Emerald", new Color(0.10f, 0.70f, 0.35f),  60, "skin_emerald"),
        new GearSkin("Violet",  new Color(0.60f, 0.30f, 0.90f), 100, "skin_violet"),
    };

    // Alley looks: recolour the gold accent trim (foul line, gutter trim,
    // pin mask trim) of the whole alley. Earned coins only.
    public struct AlleyTheme
    {
        public string Name;
        public Color Accent;
        public int Cost;
        public string Key;
        public AlleyTheme(string name, Color accent, int cost, string key)
        {
            Name = name;
            Accent = accent;
            Cost = cost;
            Key = key;
        }
    }

    public static readonly AlleyTheme[] Themes = new AlleyTheme[]
    {
        new AlleyTheme("Midnight Alley", new Color(0.30f, 0.50f, 1.00f),  80, "theme_midnight"),
        new AlleyTheme("Sunset Alley",   new Color(1.00f, 0.45f, 0.15f), 100, "theme_sunset"),
        new AlleyTheme("Emerald Alley",  new Color(0.15f, 0.85f, 0.40f), 120, "theme_emerald"),
    };

    public struct SpareSetup
    {
        public string Name;
        public int[] Pins;
        public SpareSetup(string name, int[] pins)
        {
            Name = name;
            Pins = pins;
        }
    }

    public static readonly SpareSetup[] SpareSetups = new SpareSetup[]
    {
        new SpareSetup("7-10 split",       new int[] { 7, 10 }),
        new SpareSetup("4-7",              new int[] { 4, 7 }),
        new SpareSetup("6-10",             new int[] { 6, 10 }),
        new SpareSetup("3-10",             new int[] { 3, 10 }),
        new SpareSetup("2-4-5",            new int[] { 2, 4, 5 }),
        new SpareSetup("1-2-4",            new int[] { 1, 2, 4 }),
        new SpareSetup("Bucket 2-4-5-8",   new int[] { 2, 4, 5, 8 }),
    };

    public static readonly Color Gold = new Color(0.96f, 0.77f, 0.09f); // #F5C518
    public static readonly Color Dark = new Color(0.04f, 0.04f, 0.04f); // #0A0A0A

    // v1.1.4: pipeline-aware shader lookup. The project is URP, but the
    // template ships no material using "Universal Render Pipeline/Lit", so
    // that shader is not in the player build and a bare Shader.Find for it
    // comes back empty — every 3D object then kept the built-in default
    // material, which URP paints solid magenta (the pink screen in
    // v1.1.3). GraphicsSettings.currentRenderPipeline is non-null exactly
    // when a scriptable pipeline (URP here) is active. Never assign a
    // built-in shader in a URP build or vice versa: the wrong pipeline's
    // shader renders as solid magenta.
    // v1.1.5: the v1.1.4 chain came back EMPTY in the player build — not
    // just Lit, every name. The template ships no materials at all, so
    // Unity strips every URP/built-in shader out of the build, and
    // Shader.Find can only find shaders that are in the build. So the
    // game now ships its OWN shader (Assets/Resources/Shaders/
    // GameUnlit.shader): a shader asset in a Resources folder is always
    // included in the build, and Resources.Load finds it. The Find chain
    // stays as a fallback. Returns null when nothing is found.
    public static Shader FindGameShader()
    {
        Shader shipped = Resources.Load<Shader>("Shaders/GameUnlit");
        if (shipped != null)
        {
            Debug.Log("[BowlingConfig] Using shipped shader Lynxos/GameUnlit");
            return shipped;
        }
        Debug.LogWarning("[BowlingConfig] Shipped shader not found in Resources; trying Shader.Find.");
        bool urp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
        string[] names = urp
            ? new string[] { "Universal Render Pipeline/Lit", "Universal Render Pipeline/Simple Lit", "Universal Render Pipeline/Unlit" }
            : new string[] { "Standard", "Unlit/Color", "Legacy Shaders/Diffuse" };
        foreach (string n in names)
        {
            Shader s = Shader.Find(n);
            if (s != null)
            {
                Debug.Log("[BowlingConfig] Using shader " + n);
                return s;
            }
        }
        Debug.LogWarning("[BowlingConfig] No usable 3D shader found.");
        return null;
    }
}

public static class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Start()
    {
        // Clean slate: remove everything the template scene shipped with
        // (camera, lights, ground plane) so the game owns the whole scene.
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            Object.Destroy(root);
        }

        GameObject rootObj = new GameObject("LynxosBowling");
        Object.DontDestroyOnLoad(rootObj);
        rootObj.AddComponent<BowlingGameManager>();
    }
}
