// Lynxos Bowling — the ball. A sphere rigidbody (ContinuousDynamic so it
// cannot tunnel through pins at full speed). Throw speed comes from the
// windup power; hook is a lateral acceleration applied while the ball rolls,
// scaled by the spin setting and the chosen ball's hook factor.
using UnityEngine;

public class BallController : MonoBehaviour
{
    public System.Action<float> OnPinHit; // first pin contact, impact speed

    public Vector3 Position
    {
        get { return ballObj != null ? ballObj.transform.position : Vector3.zero; }
    }

    public float CurrentSpeed
    {
        get { return rb != null ? rb.linearVelocity.magnitude : 0f; }
    }

    public bool IsRolling { get; private set; }
    public bool InGutter { get; private set; }

    private GameObject ballObj;
    private GameObject aimGuide;
    private Material ballMat;
    private Rigidbody rb;
    private float spin;        // -3..+3
    private float hookFactor = 1f;
    private float speedBonus;
    private bool crashReported;

    public void Build()
    {
        ballObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ballObj.name = "BowlingBall";
        ballObj.transform.localScale = Vector3.one * (BowlingConfig.BallRadius * 2f);

        // v1.1.4: pipeline-aware lookup (see BowlingConfig.FindGameShader).
        // The old URP/Lit-then-Standard lookup came back empty in the
        // player build, so the ball kept the built-in default material and
        // rendered magenta — and the aim guide was never created, because
        // it only exists inside the shader!=null branch below.
        Shader shader = null;
        try
        {
            shader = BowlingConfig.FindGameShader();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[BallController] Shader lookup failed: " + e.Message);
        }
        if (shader != null)
        {
            Material mat = new Material(shader);
            // v1.1.3: bright gold, not near-black. The old ball (0.08 grey)
            // was invisible against the dark lane even when in frame.
            mat.color = BowlingConfig.Gold;
            if (shader.name.Contains("Universal Render Pipeline")) mat.SetFloat("_Smoothness", 0.8f);
            else mat.SetFloat("_Glossiness", 0.8f);
            ballObj.GetComponent<MeshRenderer>().material = mat;
            ballMat = mat;

            // v1.2.0: a small white shine on the ball, so it reads as a
            // glossy ball instead of a flat coloured circle. It rides on
            // the ball, so it rolls with it. (The ball is scaled to 0.216
            // diameter, so the local offset/scale are in that space: the
            // offset magnitude 0.499 x 0.216 = the 0.108 m ball radius, so
            // the shine sits half-proud of the surface.)
            GameObject shine = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shine.name = "Shine";
            shine.transform.SetParent(ballObj.transform, false);
            shine.transform.localPosition = new Vector3(0.22f, 0.35f, 0.28f);
            shine.transform.localScale = new Vector3(0.23f, 0.23f, 0.23f);
            Material shineMat = new Material(shader);
            shineMat.color = new Color(1f, 1f, 1f);
            shine.GetComponent<MeshRenderer>().material = shineMat;
            Object.Destroy(shine.GetComponent<Collider>());

            // Gold aim guide on the lane: a thin strip from the foul line
            // toward the pins at the current aim position, so the aim is
            // visible as well as spoken.
            aimGuide = GameObject.CreatePrimitive(PrimitiveType.Quad);
            aimGuide.name = "AimGuide";
            aimGuide.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            aimGuide.transform.localScale = new Vector3(0.07f, 6f, 1f);
            aimGuide.GetComponent<MeshRenderer>().material = mat;
            Object.Destroy(aimGuide.GetComponent<Collider>());
        }
        // (shader null = keep the primitive's own default material.)

        rb = ballObj.AddComponent<Rigidbody>();
        rb.mass = 6.35f;
        rb.linearDamping = 0.03f;
        rb.angularDamping = 0.05f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        SphereCollider col = ballObj.GetComponent<SphereCollider>();
        PhysicsMaterial phys = new PhysicsMaterial("Ball");
        phys.dynamicFriction = 0.35f;
        phys.staticFriction = 0.35f;
        phys.bounciness = 0.05f;
        col.material = phys;

        ballObj.AddComponent<BallHitReporter>().Init(this);
        ResetBall(0f);
    }

    public void SetBallSpec(BowlingConfig.BallSpec spec)
    {
        if (rb != null) rb.mass = spec.WeightKg;
        hookFactor = spec.HookFactor;
        speedBonus = spec.SpeedBonus;
    }

    // Recolours the ball (ball's own colour, or an equipped skin colour).
    public void SetBallColor(Color color)
    {
        if (ballMat != null) ballMat.color = color;
    }

    public void ResetBall(float aimX)
    {
        if (ballObj == null) return;
        ballObj.transform.position = new Vector3(aimX, BowlingConfig.BallRadius, -0.6f);
        ballObj.transform.rotation = Quaternion.identity;
        if (aimGuide != null) aimGuide.transform.position = new Vector3(aimX, 0.004f, 3.0f);
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        IsRolling = false;
        InGutter = false;
        crashReported = false;
        spin = 0f;
    }

    // power01: 0..1 from the windup meter. spinSetting: -3..+3.
    public void ThrowBall(float power01, float spinSetting)
    {
        if (rb == null) return;
        float speed = BowlingConfig.MinThrowSpeed
            + power01 * (BowlingConfig.MaxThrowSpeed - BowlingConfig.MinThrowSpeed)
            + speedBonus;
        speed = Mathf.Clamp(speed, 3f, 11f);
        spin = spinSetting;
        rb.linearVelocity = new Vector3(0f, 0f, speed);
        // Rolling rotation: forward roll around the x axis.
        rb.angularVelocity = new Vector3(speed / BowlingConfig.BallRadius, spin * 2f, 0f);
        IsRolling = true;
        crashReported = false;
    }

    private void FixedUpdate()
    {
        if (!IsRolling || rb == null) return;

        // Hook: lateral curve while the ball is on the lane.
        float z = ballObj.transform.position.z;
        if (z > 0f && z < 17f && spin != 0f)
        {
            rb.AddForce(new Vector3(spin * BowlingConfig.HookAccelPerStep * hookFactor, 0f, 0f),
                ForceMode.Acceleration);
        }

        // Gutter: centre past the lane edge and dropped below lane level.
        if (!InGutter && Mathf.Abs(ballObj.transform.position.x) > 0.56f
            && ballObj.transform.position.y < 0.09f)
        {
            InGutter = true;
        }
    }

    // Called by BallHitReporter on the first pin contact of a roll.
    public void NotifyPinHit(float impactSpeed)
    {
        if (crashReported) return;
        crashReported = true;
        if (OnPinHit != null) OnPinHit(impactSpeed);
    }
}

// Tiny helper on the ball object: reports the first pin collision per roll.
// Kept separate so BallController stays free of Unity collision callbacks.
public class BallHitReporter : MonoBehaviour
{
    private BallController controller;

    public void Init(BallController owner)
    {
        controller = owner;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (controller == null) return;
        if (collision.gameObject.CompareTag("Pin"))
        {
            controller.NotifyPinHit(collision.relativeVelocity.magnitude);
        }
    }
}
