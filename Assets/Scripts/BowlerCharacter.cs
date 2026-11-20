// Lynxos Bowling — the menu bowler (v1.3.0).
// A full 3D bowler built from primitives, standing front and centre on
// the main menu with the menu buttons off to the side. Menu-only: the
// bowler NEVER appears in the game itself (Justin's call).
// The character MOVES: a living idle animation — gentle sway, weight
// shifting foot to foot, the ball turning in one hand, looking around,
// and a slow practice swing every few seconds. All procedural in code
// (sine-based, smoothly looped), so it can never freeze mid-pose.
// Wearable gear (shirts, shoes, one accessory, ball) is applied here
// from GameProgression, so the makeover shows on the character at once.
using UnityEngine;

public class BowlerCharacter : MonoBehaviour
{
    // Stands on the approach, right of centre so the menu buttons on
    // the left never cover him. Faces the camera (rotation y = 180, so
    // local +z points toward the camera at z = -4.2).
    private const float BaseX = 0.45f;
    private const float BaseZ = -1.2f;

    private GameObject torsoObj;
    private GameObject headPivot;
    private GameObject ballArmPivot;
    private GameObject otherArmPivot;
    private GameObject heldBall;
    private GameObject headbandObj;
    private GameObject capObj;
    private GameObject wristbandObj;

    private Material shirtMat;
    private Material shoeMat;
    private Material ballMat;
    private Material accessoryMat;

    private Vector3 torsoBaseScale;
    private float animTime;

    public void Build()
    {
        transform.position = new Vector3(BaseX, 0f, BaseZ);
        transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        Material skinMat = MakeMaterial(new Color(0.85f, 0.65f, 0.50f), 0.4f);
        Material pantsMat = MakeMaterial(new Color(0.12f, 0.12f, 0.16f), 0.4f);
        Material hairMat = MakeMaterial(new Color(0.15f, 0.10f, 0.06f), 0.3f);
        Material darkMat = MakeMaterial(new Color(0.02f, 0.02f, 0.02f), 0.2f);
        shirtMat = MakeMaterial(ShirtColor(), 0.5f);
        shoeMat = MakeMaterial(ShoeColor(), 0.5f);
        ballMat = MakeMaterial(HeldBallColor(), 0.8f);
        accessoryMat = MakeMaterial(AccessoryColor(), 0.5f);

        // Shoes: flat boxes, toes toward the camera (local +z).
        MakePart("ShoeL", PrimitiveType.Cube, new Vector3(-0.13f, 0.045f, 0.03f),
            new Vector3(0.17f, 0.09f, 0.32f), shoeMat, transform);
        MakePart("ShoeR", PrimitiveType.Cube, new Vector3(0.13f, 0.045f, 0.03f),
            new Vector3(0.17f, 0.09f, 0.32f), shoeMat, transform);

        // Legs: capsules, dark trousers.
        MakePart("LegL", PrimitiveType.Capsule, new Vector3(-0.13f, 0.47f, 0f),
            new Vector3(0.16f, 0.375f, 0.16f), pantsMat, transform);
        MakePart("LegR", PrimitiveType.Capsule, new Vector3(0.13f, 0.47f, 0f),
            new Vector3(0.16f, 0.375f, 0.16f), pantsMat, transform);

        // Torso: the shirt. A capsule reads as shoulders + chest.
        torsoObj = MakePart("Torso", PrimitiveType.Capsule, new Vector3(0f, 1.17f, 0f),
            new Vector3(0.52f, 0.34f, 0.30f), shirtMat, transform);
        torsoBaseScale = torsoObj.transform.localScale;

        // Arms hang from shoulder pivots, so they can swing.
        ballArmPivot = MakePivot("BallArm", new Vector3(0.30f, 1.46f, 0f));
        MakePart("BallArmMesh", PrimitiveType.Capsule, new Vector3(0f, -0.28f, 0f),
            new Vector3(0.12f, 0.29f, 0.12f), skinMat, ballArmPivot.transform);
        MakePart("BallHand", PrimitiveType.Sphere, new Vector3(0f, -0.60f, 0f),
            new Vector3(0.11f, 0.11f, 0.11f), skinMat, ballArmPivot.transform);
        // The ball itself, turning in the ball hand.
        heldBall = MakePart("HeldBall", PrimitiveType.Sphere, new Vector3(0f, -0.64f, 0.06f),
            new Vector3(0.216f, 0.216f, 0.216f), ballMat, ballArmPivot.transform);

        otherArmPivot = MakePivot("OtherArm", new Vector3(-0.30f, 1.46f, 0f));
        MakePart("OtherArmMesh", PrimitiveType.Capsule, new Vector3(0f, -0.28f, 0f),
            new Vector3(0.12f, 0.29f, 0.12f), skinMat, otherArmPivot.transform);
        MakePart("OtherHand", PrimitiveType.Sphere, new Vector3(0f, -0.60f, 0f),
            new Vector3(0.11f, 0.11f, 0.11f), skinMat, otherArmPivot.transform);

        // Head on its own pivot at the neck, so it can look around.
        headPivot = MakePivot("Head", new Vector3(0f, 1.52f, 0f));
        MakePart("HeadMesh", PrimitiveType.Sphere, new Vector3(0f, 0.16f, 0f),
            new Vector3(0.26f, 0.26f, 0.26f), skinMat, headPivot.transform);
        // Hair cap on top/back of the head.
        MakePart("Hair", PrimitiveType.Sphere, new Vector3(0f, 0.25f, -0.01f),
            new Vector3(0.27f, 0.13f, 0.27f), hairMat, headPivot.transform);
        // Eyes on the face (local +z = toward the camera).
        MakePart("EyeL", PrimitiveType.Sphere, new Vector3(-0.05f, 0.17f, 0.115f),
            new Vector3(0.035f, 0.035f, 0.035f), darkMat, headPivot.transform);
        MakePart("EyeR", PrimitiveType.Sphere, new Vector3(0.05f, 0.17f, 0.115f),
            new Vector3(0.035f, 0.035f, 0.035f), darkMat, headPivot.transform);

        // Accessories (all start hidden; ApplyGear shows the equipped one).
        headbandObj = MakePart("Headband", PrimitiveType.Cylinder, new Vector3(0f, 0.22f, 0f),
            new Vector3(0.27f, 0.025f, 0.27f), accessoryMat, headPivot.transform);
        capObj = new GameObject("Cap");
        capObj.transform.SetParent(headPivot.transform, false);
        capObj.transform.localPosition = Vector3.zero;
        capObj.transform.localRotation = Quaternion.identity;
        MakePart("CapTop", PrimitiveType.Cylinder, new Vector3(0f, 0.27f, 0f),
            new Vector3(0.30f, 0.05f, 0.30f), accessoryMat, capObj.transform);
        MakePart("CapBrim", PrimitiveType.Cube, new Vector3(0f, 0.23f, 0.16f),
            new Vector3(0.28f, 0.02f, 0.16f), accessoryMat, capObj.transform);
        wristbandObj = MakePart("Wristband", PrimitiveType.Cylinder, new Vector3(0f, -0.45f, 0f),
            new Vector3(0.13f, 0.03f, 0.13f), accessoryMat, ballArmPivot.transform);

        ApplyGear();
    }

    // --- Gear ---

    private static Color ShirtColor()
    {
        int i = GameProgression.EquippedShirt;
        if (i < 0 || i >= BowlingConfig.Shirts.Length) i = 0;
        return BowlingConfig.Shirts[i].Color;
    }

    private static Color ShoeColor()
    {
        int i = GameProgression.EquippedShoe;
        if (i < 0 || i >= BowlingConfig.Shoes.Length) i = 0;
        return BowlingConfig.Shoes[i].Color;
    }

    private static Color AccessoryColor()
    {
        int i = GameProgression.EquippedAccessory;
        if (i < 0 || i >= BowlingConfig.Accessories.Length) return BowlingConfig.Gold;
        return BowlingConfig.Accessories[i].Color;
    }

    private static Color HeldBallColor()
    {
        int ball = GameProgression.EquippedBall;
        if (ball < 0 || ball >= BowlingConfig.Balls.Length) ball = 0;
        int skin = GameProgression.EquippedSkin;
        if (skin >= 0 && skin < BowlingConfig.Skins.Length) return BowlingConfig.Skins[skin].Color;
        return BowlingConfig.Balls[ball].Color;
    }

    // Recolours the shirt/shoes/ball and shows the equipped accessory.
    // Called when the menu opens and after any gear purchase, so the
    // makeover shows on the character at once.
    public void ApplyGear()
    {
        if (shirtMat != null) shirtMat.color = ShirtColor();
        if (shoeMat != null) shoeMat.color = ShoeColor();
        if (ballMat != null) ballMat.color = HeldBallColor();
        if (accessoryMat != null) accessoryMat.color = AccessoryColor();
        int acc = GameProgression.EquippedAccessory;
        if (headbandObj != null) headbandObj.SetActive(acc == 0);
        if (wristbandObj != null) wristbandObj.SetActive(acc == 1);
        if (capObj != null) capObj.SetActive(acc == 2);
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    // --- Idle animation ---

    private void Update()
    {
        animTime += Time.deltaTime;
        float t = animTime;

        // Weight shifting foot to foot + a gentle lean: never still.
        transform.position = new Vector3(BaseX + Mathf.Sin(t * 0.5f) * 0.02f, 0f, BaseZ);
        transform.rotation = Quaternion.Euler(0f, 180f, Mathf.Sin(t * 0.8f) * 1.5f);

        // Breathing: the chest rises and falls a touch.
        if (torsoObj != null)
        {
            Vector3 s = torsoBaseScale;
            s.y = torsoBaseScale.y * (1f + Mathf.Sin(t * 1.5f) * 0.015f);
            torsoObj.transform.localScale = s;
        }

        // Looking around: slow turns, a small nod.
        if (headPivot != null)
        {
            headPivot.transform.localRotation = Quaternion.Euler(
                Mathf.Sin(t * 0.6f) * 4f, Mathf.Sin(t * 0.35f) * 25f, 0f);
        }

        // A slow practice swing every 7 seconds: the ball arm swings
        // forward like a real throw (1.4 s), then settles back to the
        // gentle pendulum. The torso leans into it a little.
        float cycle = t % 7f;
        float w = cycle < 1.4f ? Mathf.Sin(cycle / 1.4f * Mathf.PI) : 0f;
        if (ballArmPivot != null)
        {
            ballArmPivot.transform.localRotation = Quaternion.Euler(
                Mathf.Sin(t * 1.1f) * 12f + w * 55f, 0f, 0f);
        }
        if (otherArmPivot != null)
        {
            otherArmPivot.transform.localRotation = Quaternion.Euler(
                Mathf.Sin(t * 1.1f + Mathf.PI) * 9f, 0f, 0f);
        }
        if (torsoObj != null)
        {
            torsoObj.transform.localRotation = Quaternion.Euler(w * 6f, 0f, 0f);
        }

        // The ball turns slowly in the ball hand.
        if (heldBall != null)
        {
            heldBall.transform.Rotate(0f, 40f * Time.deltaTime, 0f);
        }
    }

    // --- Builders ---

    private GameObject MakePivot(string name, Vector3 localPos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;
        return go;
    }

    private GameObject MakePart(string name, PrimitiveType type, Vector3 localPos,
        Vector3 localScale, Material mat, Transform parent)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        go.transform.localRotation = Quaternion.identity;
        if (mat != null) go.GetComponent<MeshRenderer>().material = mat;
        Object.Destroy(go.GetComponent<Collider>());
        return go;
    }

    // Same guarded pattern as the other builders: null shader keeps the
    // primitive's default material instead of throwing.
    private Material MakeMaterial(Color color, float smoothness)
    {
        try
        {
            Shader shader = BowlingConfig.FindGameShader();
            if (shader == null) return null;
            Material mat = new Material(shader);
            mat.color = color;
            if (shader.name.Contains("Universal Render Pipeline") || shader.name.Contains("GameLit"))
            {
                mat.SetFloat("_Smoothness", smoothness);
            }
            else
            {
                mat.SetFloat("_Glossiness", smoothness);
            }
            return mat;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[BowlerCharacter] Material failed (" + color + "): " + e.Message);
            return null;
        }
    }
}
