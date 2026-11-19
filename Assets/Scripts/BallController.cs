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
        get { return rb != null ? rb.velocity.magnitude : 0f; }
    }

    public bool IsRolling { get; private set; }
    public bool InGutter { get; private set; }

    private GameObject ballObj;
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

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material mat = new Material(shader);
        mat.color = new Color(0.08f, 0.08f, 0.10f);
        if (shader.name.Contains("Universal Render Pipeline")) mat.SetFloat("_Smoothness", 0.6f);
        else mat.SetFloat("_Glossiness", 0.6f);
        ballObj.GetComponent<MeshRenderer>().material = mat;

        rb = ballObj.AddComponent<Rigidbody>();
        rb.mass = 6.35f;
        rb.drag = 0.03f;
        rb.angularDrag = 0.05f;
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

    public void ResetBall(float aimX)
    {
        if (ballObj == null) return;
        ballObj.transform.position = new Vector3(aimX, BowlingConfig.BallRadius, -0.6f);
        ballObj.transform.rotation = Quaternion.identity;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        IsRolling = false;
        InGutter = false;
        crashReported = false;
        spin = 0f;
    }

    // power01: 0..1 from the windup meter. spinSetting: -3..+3.
    public void ThrowBall(float power01, float spinSetting)
    {
        float speed = BowlingConfig.MinThrowSpeed
            + power01 * (BowlingConfig.MaxThrowSpeed - BowlingConfig.MinThrowSpeed)
            + speedBonus;
        speed = Mathf.Clamp(speed, 3f, 11f);
        spin = spinSetting;
        rb.velocity = new Vector3(0f, 0f, speed);
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
