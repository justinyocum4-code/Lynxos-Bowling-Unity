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

    public void Build()
    {
        CreateMaterials();
        BuildLane();
        BuildGutters();
        BuildPit();
        BuildBumpers();
        BuildDecor();
        BuildLightsAndCamera();
    }

    public void SetBumpers(bool on)
    {
        if (BumpersLeft != null) BumpersLeft.SetActive(on);
        if (BumpersRight != null) BumpersRight.SetActive(on);
    }

    private void CreateMaterials()
    {
        laneMat = MakeMaterial(new Color(0.55f, 0.38f, 0.18f), 0.4f); // wood
        gutterMat = MakeMaterial(new Color(0.08f, 0.08f, 0.08f), 0.2f);
        wallMat = MakeMaterial(new Color(0.05f, 0.05f, 0.06f), 0.2f);
        goldMat = MakeMaterial(BowlingConfig.Gold, 0.6f);
        darkDotMat = MakeMaterial(new Color(0.15f, 0.1f, 0.05f), 0.3f);
    }

    private Material MakeMaterial(Color color, float smoothness)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
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

    private PhysicMaterial MakePhysicMaterial(string name, float friction, float bounciness)
    {
        PhysicMaterial mat = new PhysicMaterial(name);
        mat.dynamicFriction = friction;
        mat.staticFriction = friction;
        mat.bounciness = bounciness;
        mat.frictionCombine = PhysicMaterialCombine.Minimum;
        mat.bounceCombine = PhysicMaterialCombine.Minimum;
        return mat;
    }

    // Helper: a box with a mesh renderer; collider optional.
    private GameObject CreateBox(string name, Vector3 center, Vector3 size, Material mat,
        bool withCollider, PhysicMaterial physMat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.position = center;
        go.transform.localScale = size;
        go.GetComponent<MeshRenderer>().material = mat;
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
        PhysicMaterial lanePhys = MakePhysicMaterial("Lane", 0.02f, 0f);
        // Lane surface: top face at y = 0.
        CreateBox("Lane", new Vector3(0f, -0.05f, centerZ),
            new Vector3(BowlingConfig.LaneWidth, 0.1f, totalLength), laneMat, true, lanePhys);

        // Foul line: thin gold strip across the lane at z = 0, visual only.
        GameObject line = GameObject.CreatePrimitive(PrimitiveType.Quad);
        line.name = "FoulLine";
        line.transform.position = new Vector3(0f, 0.002f, 0f);
        line.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        line.transform.localScale = new Vector3(BowlingConfig.LaneWidth, 0.06f, 1f);
        line.GetComponent<MeshRenderer>().material = goldMat;
        Object.Destroy(line.GetComponent<Collider>());
    }

    private void BuildGutters()
    {
        float totalLength = BowlingConfig.ApproachLength + BowlingConfig.LaneLength + BowlingConfig.PitLength;
        float centerZ = -BowlingConfig.ApproachLength + totalLength / 2f;
        float laneHalf = BowlingConfig.LaneWidth / 2f;
        PhysicMaterial gutterPhys = MakePhysicMaterial("Gutter", 0.02f, 0f);
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
        }
    }

    private void BuildPit()
    {
        float backZ = BowlingConfig.HeadPinZ + 2.15f; // 20.45
        // Back wall: stops the ball and the pins.
        CreateBox("PitBackWall", new Vector3(0f, 0.5f, backZ),
            new Vector3(2.0f, 1.0f, 0.1f), wallMat, true, null);
        // Pit floor: catches anything that flies past the lane end instead
        // of letting it fall out of the world.
        CreateBox("PitFloor", new Vector3(0f, -0.05f - BowlingConfig.GutterDrop, 19.9f),
            new Vector3(2.0f, 0.1f, 3.0f), gutterMat, true, null);
    }

    private void BuildBumpers()
    {
        // Invisible walls along the lane edges. Off by default; the manager
        // turns them on in Bumpers mode so the ball can never gutter.
        PhysicMaterial bumperPhys = MakePhysicMaterial("Bumper", 0f, 0.25f);
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
                new Vector3(0.04f, 0.004f, 0.14f), darkDotMat, false, null);
        }
    }

    private void BuildLightsAndCamera()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.32f, 0.32f, 0.36f);

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

        GameObject camObj = new GameObject("Main Camera");
        Camera cam = camObj.AddComponent<Camera>();
        cam.clearColor = new Color(0.02f, 0.02f, 0.03f);
        cam.fieldOfView = 55f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 100f;
        camObj.transform.position = new Vector3(0f, 2.8f, -3.6f);
        camObj.transform.LookAt(new Vector3(0f, 0f, 14f));
    }
}
