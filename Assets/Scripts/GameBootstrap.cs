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

    public struct BallSpec
    {
        public string Name;
        public float WeightKg;
        public float SpeedBonus;  // m/s added to the throw
        public float HookFactor;  // hook strength multiplier
        public BallSpec(string name, float weightKg, float speedBonus, float hookFactor)
        {
            Name = name;
            WeightKg = weightKg;
            SpeedBonus = speedBonus;
            HookFactor = hookFactor;
        }
    }

    public static readonly BallSpec[] Balls = new BallSpec[]
    {
        new BallSpec("Light 12 lb",    5.44f,  0.5f, 1.2f),
        new BallSpec("Standard 14 lb", 6.35f,  0.0f, 1.0f),
        new BallSpec("Heavy 16 lb",    7.26f, -0.5f, 0.8f),
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
