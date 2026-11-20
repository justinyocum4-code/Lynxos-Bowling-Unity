// Lynxos Bowling — builds the 3D lane, gutters, pit, bumpers, camera and
// lights entirely at runtime. Real metre scale: lane 18.3 m long, 1.06 m
// wide; foul line at z = 0, head pin at z = 18.3, ball starts at z = -0.6.
using UnityEngine;

public class LaneBuilder : MonoBehaviour
{
    public GameObject BumpersLeft { get; private set; }
    public GameObject BumpersRight { get; private set; }

    private Material laneMat;
    private Material gutterMat;
    private Material wallMat;
    private Material goldMat;
    private Material darkDotMat;
    private Material arrowMat;
    private Material boardLightMat;
    private Material boardDarkMat;

    // First failure recorded here, so the manager can show + speak it
    // instead of dying silent on a black screen.
    public string BuildError { get; private set; }

    public void Build()
    {
        // Camera + lights FIRST: if any step below throws, the player still
        // renders a scene instead of a black screen, and the manager can
        // report the failure on screen. (v1.1 built the camera last, so a
        // throw in the materials/lane left no camera at all.)
        BuildLightsAndCamera();
        TryStep("materials", CreateMaterials);
        TryStep("lane", BuildLane);
        TryStep("gutters", BuildGutters);
        TryStep("pit", BuildPit);
        TryStep("bumpers", BuildBumpers);
        TryStep("decor", BuildDecor);
        TryStep("alley", BuildAlley);
    }

    private void TryStep(string name, System.Action step)
    {
        try
        {
            step();
        }
        catch (System.Exception e)
        {
            if (BuildError == null) BuildError = "Lane " + name + ": " + e.GetType().Name + " " + e.Message;
            Debug.LogError("[LaneBuilder] " + name + " failed: " + e);
        }
    }

    public void SetBumpers(bool on)
    {
        if (BumpersLeft != null) BumpersLeft.SetActive(on);
        if (BumpersRight != null) BumpersRight.SetActive(on);
    }

    // v1.2.0: recolour the gold accent (foul line, gutter trim, pin mask
    // trim, alley trim) for an equipped alley theme. The saved theme is
    // applied by the manager right after Build().
    public void SetAccentColor(Color color)
    {
        if (goldMat != null) goldMat.color = color;
    }

    private void CreateMaterials()
    {
        laneMat = MakeMaterial(new Color(0.55f, 0.38f, 0.18f), 0.4f); // wood
        gutterMat = MakeMaterial(new Color(0.08f, 0.08f, 0.08f), 0.2f);
        wallMat = MakeMaterial(new Color(0.05f, 0.05f, 0.06f), 0.2f);
        goldMat = MakeMaterial(GameProgression.AccentColor(), 0.6f);
        darkDotMat = MakeMaterial(new Color(0.15f, 0.1f, 0.05f), 0.3f);
        arrowMat = MakeMaterial(new Color(0.95f, 0.85f, 0.45f), 0.4f); // bright gold arrows
        boardLightMat = MakeMaterial(new Color(0.62f, 0.45f, 0.22f), 0.4f);
        boardDarkMat = MakeMaterial(new Color(0.48f, 0.32f, 0.15f), 0.4f);
    }

    // v1.1.4: shader via BowlingConfig.FindGameShader (pipeline-aware).
    // Returns null (instead of throwing) when no shader can be found, so a
    // missing shader can never kill the start — but it records BuildError,
    // so the failure shows and speaks on the menu instead of rendering
    // the whole scene pink silently (v1.1.3). Callers keep the primitive's
    // own default material when this returns null.
    private Material MakeMaterial(Color color, float smoothness)
    {
        try
        {
            Shader shader = BowlingConfig.FindGameShader();
            if (shader == null)
            {
                Debug.LogWarning("[LaneBuilder] No shader found for material " + color + "; using default.");
                if (BuildError == null) BuildError = "No 3D shader found — the scene may render pink.";
                return null;
            }
            Material mat = new Material(shader);
            mat.color = color;
            if (shader.name.Contains("Universal Render Pipeline"))
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
            Debug.LogWarning("[LaneBuilder] Material failed (" + color + "): " + e.Message);
            return null;
        }
    }

    private PhysicsMaterial MakePhysicsMaterial(string name, float friction, float bounciness)
    {
        PhysicsMaterial mat = new PhysicsMaterial(name);
        mat.dynamicFriction = friction;
        mat.staticFriction = friction;
        mat.bounciness = bounciness;
        mat.frictionCombine = PhysicsMaterialCombine.Minimum;
        mat.bounceCombine = PhysicsMaterialCombine.Minimum;
        return mat;
    }

    // Helper: a box with a mesh renderer; collider optional.
    private GameObject CreateBox(string name, Vector3 center, Vector3 size, Material mat,
        bool withCollider, PhysicsMaterial physMat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.position = center;
        go.transform.localScale = size;
        if (mat != null) go.GetComponent<MeshRenderer>().material = mat;
        Collider col = go.GetComponent<Collider>();
        if (!withCollider)
        {
            Object.Destroy(col);
        }
        else if (physMat != null && col is BoxCollider box)
        {
            box.material = physMat;
        }
        return go;
    }

    private void BuildLane()
    {
        float totalLength = BowlingConfig.ApproachLength + BowlingConfig.LaneLength + BowlingConfig.PitLength;
        float centerZ = -BowlingConfig.ApproachLength + totalLength / 2f; // -2 + 11.25 = 9.25
        PhysicsMaterial lanePhys = MakePhysicsMaterial("Lane", 0.02f, 0f);
        // Lane surface: top face at y = 0.
        CreateBox("Lane", new Vector3(0f, -0.05f, centerZ),
            new Vector3(BowlingConfig.LaneWidth, 0.1f, totalLength), laneMat, true, lanePhys);

        // Foul line: thin gold strip across the lane at z = 0, visual only.
        // y = 0.004 so it sits above the v1.2.0 board strips (tops at 0.002).
        GameObject line = GameObject.CreatePrimitive(PrimitiveType.Quad);
        line.name = "FoulLine";
        line.transform.position = new Vector3(0f, 0.004f, 0f);
        line.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        line.transform.localScale = new Vector3(BowlingConfig.LaneWidth, 0.06f, 1f);
        if (goldMat != null) line.GetComponent<MeshRenderer>().material = goldMat;
        Object.Destroy(line.GetComponent<Collider>());
    }

    private void BuildGutters()
    {
        float totalLength = BowlingConfig.ApproachLength + BowlingConfig.LaneLength + BowlingConfig.PitLength;
        float centerZ = -BowlingConfig.ApproachLength + totalLength / 2f;
        float laneHalf = BowlingConfig.LaneWidth / 2f;
        PhysicsMaterial gutterPhys = MakePhysicsMaterial("Gutter", 0.02f, 0f);
        for (int s = -1; s <= 1; s += 2)
        {
            // Gutter floor: 0.06 m below the lane surface.
            CreateBox("GutterFloor" + (s < 0 ? "L" : "R"),
                new Vector3(s * (laneHalf + BowlingConfig.GutterWidth / 2f), -0.05f - BowlingConfig.GutterDrop, centerZ),
                new Vector3(BowlingConfig.GutterWidth, 0.1f, totalLength), gutterMat, true, gutterPhys);
            // Outer wall keeps the ball in the channel.
            CreateBox("GutterWall" + (s < 0 ? "L" : "R"),
                new Vector3(s * (laneHalf + BowlingConfig.GutterWidth + 0.01f), 0.1f, centerZ),
                new Vector3(0.02f, 0.4f, totalLength), wallMat, true, null);
            // v1.2.0: gold trim along the top of each gutter wall.
            CreateBox("GutterTrim" + (s < 0 ? "L" : "R"),
                new Vector3(s * (laneHalf + BowlingConfig.GutterWidth + 0.01f), 0.31f, centerZ),
                new Vector3(0.03f, 0.02f, totalLength), goldMat, false, null);
        }
    }

    private void BuildPit()
    {
        float backZ = BowlingConfig.HeadPinZ + 2.15f; // 20.45
        // Back wall: stops the ball and the pins. Wide + tall: it is the
        // pin-deck backdrop of the alley, not a floating slab.
        CreateBox("PitBackWall", new Vector3(0f, 2.0f, backZ),
            new Vector3(5.0f, 4.2f, 0.15f), wallMat, true, null);
        // Pit floor: catches anything that flies past the lane end instead
        // of letting it fall out of the world.
        CreateBox("PitFloor", new Vector3(0f, -0.05f - BowlingConfig.GutterDrop, 19.9f),
            new Vector3(2.0f, 0.1f, 3.0f), gutterMat, true, null);
        // Masking unit above the pins: dark panel with a gold lower edge,
        // the way a real alley hides the pit machinery.
        CreateBox("PinMask", new Vector3(0f, 1.7f, backZ - 0.35f),
            new Vector3(2.4f, 1.1f, 0.08f), wallMat, false, null);
        CreateBox("PinMaskTrim", new Vector3(0f, 1.12f, backZ - 0.35f),
            new Vector3(2.4f, 0.07f, 0.09f), goldMat, false, null);
    }

    private void BuildBumpers()
    {
        // Invisible walls along the lane edges. Off by default; the manager
        // turns them on in Bumpers mode so the ball can never gutter.
        PhysicsMaterial bumperPhys = MakePhysicsMaterial("Bumper", 0f, 0.25f);
        float laneHalf = BowlingConfig.LaneWidth / 2f;
        float length = BowlingConfig.ApproachLength + BowlingConfig.LaneLength;
        float centerZ = -BowlingConfig.ApproachLength + length / 2f;
        BumpersLeft = CreateBox("BumperL", new Vector3(-(laneHalf + 0.015f), 0.175f, centerZ),
            new Vector3(0.03f, 0.35f, length), wallMat, true, bumperPhys);
        BumpersRight = CreateBox("BumperR", new Vector3(laneHalf + 0.015f, 0.175f, centerZ),
            new Vector3(0.03f, 0.35f, length), wallMat, true, bumperPhys);
        // Invisible: keep the collider, drop the renderer.
        Object.Destroy(BumpersLeft.GetComponent<MeshRenderer>());
        Object.Destroy(BumpersRight.GetComponent<MeshRenderer>());
        BumpersLeft.SetActive(false);
        BumpersRight.SetActive(false);
    }

    private void BuildDecor()
    {
        // v1.2.0: board striping — a real lane is 39 boards. Alternating
        // light/dark thin strips down the whole lane, so the wood reads as
        // boards instead of one flat slab. Visual only, no colliders.
        float totalLength = BowlingConfig.ApproachLength + BowlingConfig.LaneLength + BowlingConfig.PitLength;
        float centerZ = -BowlingConfig.ApproachLength + totalLength / 2f;
        for (int i = 0; i < 39; i++)
        {
            float x = -BowlingConfig.LaneWidth / 2f + (i + 0.5f) * BowlingConfig.BoardWidth;
            CreateBox("Board" + i, new Vector3(x, 0.001f, centerZ),
                new Vector3(BowlingConfig.BoardWidth * 0.9f, 0.002f, totalLength),
                (i % 2 == 0) ? boardLightMat : boardDarkMat, false, null);
        }

        // Range dots and arrows on the lane (visual only, no colliders).
        float[] dotX = new float[] { -0.36f, -0.24f, -0.12f, 0f, 0.12f, 0.24f, 0.36f };
        for (int i = 0; i < dotX.Length; i++)
        {
            CreateBox("LaneDot" + i, new Vector3(dotX[i], 0.002f, 4.2f),
                new Vector3(0.05f, 0.004f, 0.05f), darkDotMat, false, null);
        }
        for (int i = 0; i < 5; i++)
        {
            CreateBox("LaneArrow" + i, new Vector3(-0.24f + i * 0.12f, 0.002f, 5.0f),
                new Vector3(0.04f, 0.004f, 0.14f), arrowMat, false, null);
        }
    }

    // The room itself: floor, side walls with gold trim, ceiling with
    // light panels, neighbouring lanes, and a ball return. Before v1.1.3
    // the lane floated in a near-black void, which read as a distracting
    // empty background at the far end. Now it looks like the inside of a
    // bowling alley. Visual only (no colliders) except where noted.
    private void BuildAlley()
    {
        Material floorMat = MakeMaterial(new Color(0.10f, 0.08f, 0.06f), 0.15f);
        Material woodMat = MakeMaterial(new Color(0.22f, 0.14f, 0.07f), 0.35f);
        Material ceilMat = MakeMaterial(new Color(0.06f, 0.06f, 0.07f), 0.1f);
        Material neighMat = MakeMaterial(new Color(0.35f, 0.24f, 0.12f), 0.35f);
        // v1.2.0: warmer light panels (more amber, less cold white).
        Material panelMat = MakeMaterial(new Color(1f, 0.88f, 0.62f), 0.1f);

        // Floor under everything, so there is no void below the lane.
        CreateBox("AlleyFloor", new Vector3(0f, -0.16f, 8f),
            new Vector3(14f, 0.1f, 30f), floorMat, false, null);

        // Side walls + gold trim.
        for (int s = -1; s <= 1; s += 2)
        {
            CreateBox("AlleyWall" + (s < 0 ? "L" : "R"), new Vector3(s * 2.6f, 2.05f, 8f),
                new Vector3(0.15f, 4.1f, 28f), woodMat, false, null);
            CreateBox("AlleyTrim" + (s < 0 ? "L" : "R"), new Vector3(s * 2.52f, 2.8f, 8f),
                new Vector3(0.06f, 0.07f, 28f), goldMat, false, null);
        }

        // Ceiling.
        CreateBox("AlleyCeiling", new Vector3(0f, 4.15f, 8f),
            new Vector3(14f, 0.1f, 30f), ceilMat, false, null);

        // Light panels on the ceiling, down the lane. v1.2.0: each panel
        // gets a thin gold frame, so the ceiling reads as fitted lights
        // instead of flat white slabs.
        float[] panelZ = new float[] { 1f, 7f, 13f, 18f };
        for (int i = 0; i < panelZ.Length; i++)
        {
            CreateBox("LightPanel" + i, new Vector3(0f, 4.08f, panelZ[i]),
                new Vector3(1.6f, 0.04f, 0.9f), panelMat, false, null);
            CreateBox("LightFrameA" + i, new Vector3(0f, 4.06f, panelZ[i] - 0.47f),
                new Vector3(1.7f, 0.02f, 0.06f), goldMat, false, null);
            CreateBox("LightFrameB" + i, new Vector3(0f, 4.06f, panelZ[i] + 0.47f),
                new Vector3(1.7f, 0.02f, 0.06f), goldMat, false, null);
        }

        // Neighbouring lanes, one each side, slightly darker wood.
        for (int s = -1; s <= 1; s += 2)
        {
            CreateBox("NeighbourLane" + (s < 0 ? "L" : "R"),
                new Vector3(s * 1.62f, -0.05f, 8.15f),
                new Vector3(BowlingConfig.LaneWidth, 0.1f, 20.3f), neighMat, false, null);
        }

        // Ball return rail along the right side, with a spare gold ball
        // resting on it near the approach.
        CreateBox("BallReturn", new Vector3(1.18f, 0.32f, 8f),
            new Vector3(0.18f, 0.08f, 20f), gutterMat, false, null);
        GameObject spare = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        spare.name = "ReturnBall";
        spare.transform.position = new Vector3(1.18f, 0.45f, -1.2f);
        spare.transform.localScale = Vector3.one * (BowlingConfig.BallRadius * 2f);
        if (goldMat != null) spare.GetComponent<MeshRenderer>().material = goldMat;
        Object.Destroy(spare.GetComponent<Collider>());
    }

    private void BuildLightsAndCamera()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.38f, 0.38f, 0.42f);

        GameObject sun = new GameObject("Sun");
        Light dir = sun.AddComponent<Light>();
        dir.type = LightType.Directional;
        dir.intensity = 1.1f;
        dir.color = new Color(1f, 0.95f, 0.85f);
        sun.transform.rotation = Quaternion.Euler(50f, -25f, 0f);

        // Soft light over the pins so they read clearly from the camera.
        GameObject pinLight = new GameObject("PinLight");
        Light point = pinLight.AddComponent<Light>();
        point.type = LightType.Point;
        point.intensity = 0.6f;
        point.range = 8f;
        point.color = Color.white;
        pinLight.transform.position = new Vector3(0f, 3f, 19f);

        // Warm ceiling lights down the lane.
        GameObject midLight = new GameObject("MidLight");
        Light mid = midLight.AddComponent<Light>();
        mid.type = LightType.Point;
        mid.intensity = 0.55f;
        mid.range = 11f;
        mid.color = new Color(1f, 0.95f, 0.85f);
        midLight.transform.position = new Vector3(0f, 3.6f, 7f);

        GameObject camObj = new GameObject("Main Camera");
        Camera cam = camObj.AddComponent<Camera>();
        // v1.1.4: Solid Color, never Skybox — the template scene is
        // destroyed at load, so there is no skybox to render.
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.04f, 0.03f);
        cam.fieldOfView = 55f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 100f;
        // v1.1.3 framing: pulled back and lower than v1.1 (0, 2.8, -3.6).
        // The old framing put the ball 32.9 deg off the view centre against
        // a 27.5 deg half-FOV, so the ball sat below the bottom edge of the
        // screen and was never visible. This framing puts the ball ~17 deg
        // off centre with the pins near the centre — verified numerically.
        camObj.transform.position = new Vector3(0f, 1.7f, -4.6f);
        camObj.transform.LookAt(new Vector3(0f, 0.4f, 12f));
        // The listener is what lets Unity play ANY sound. v1.1–v1.1.2 had
        // none: GameBootstrap destroys the template scene (whose camera
        // carried the listener) and the rebuilt camera never added one, so
        // every game sound was muted by design.
        camObj.AddComponent<AudioListener>();
    }
}
