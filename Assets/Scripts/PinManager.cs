// Lynxos Bowling — the 10 pins. Each pin is a rigidbody with a capsule
// collider, so pins fall and scatter with real Unity physics. A pin counts
// as fallen when it tilts more than 35 degrees or slides more than 0.3 m
// from its rack position. Fallen pins ("dead wood") are removed between
// balls; the rack resets at the start of a frame.
using UnityEngine;
using System.Collections.Generic;

public class PinManager : MonoBehaviour
{
    public class Pin
    {
        public int Number;
        public GameObject Obj;
        public Rigidbody Rb;
        public Vector3 Home;
        public bool Active;
    }

    private List<Pin> pins = new List<Pin>();
    private Material pinMat;
    private Material stripeMat;

    public void Build()
    {
        pinMat = MakeMaterial(Color.white, 0.35f);
        stripeMat = MakeMaterial(new Color(0.9f, 0.12f, 0.12f), 0.35f);
        for (int i = 1; i <= 10; i++)
        {
            pins.Add(CreatePin(i));
        }
        RackAll();
    }

    // v1.1.4: shader via BowlingConfig.FindGameShader (pipeline-aware).
    // Returns null (instead of throwing) when no shader can be found, so a
    // missing shader can never kill the start. Callers keep the primitive's
    // own default material when this returns null.
    private Material MakeMaterial(Color color, float smoothness)
    {
        try
        {
            Shader shader = BowlingConfig.FindGameShader();
            if (shader == null)
            {
                Debug.LogWarning("[PinManager] No shader found for material " + color + "; using default.");
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
            Debug.LogWarning("[PinManager] Material failed (" + color + "): " + e.Message);
            return null;
        }
    }

    private Pin CreatePin(int number)
    {
        // Root carries the physics; the capsule mesh and red neck stripe
        // are visual children at unit scale so their proportions stay true.
        GameObject root = new GameObject("Pin" + number);
        root.tag = "Pin";

        CapsuleCollider col = root.AddComponent<CapsuleCollider>();
        col.radius = BowlingConfig.PinRadius;
        col.height = BowlingConfig.PinHeight;
        PhysicsMaterial phys = new PhysicsMaterial("Pin");
        phys.dynamicFriction = 0.3f;
        phys.staticFriction = 0.3f;
        phys.bounciness = 0.1f;
        col.material = phys;

        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.mass = 1.5f;
        rb.linearDamping = 0.05f;
        rb.angularDamping = 0.08f;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Body: capsule primitive mesh only (its collider is removed).
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(root.transform, false);
        // Primitive capsule is 2 units tall, 1 unit across: scale to
        // 0.38 m tall, 0.12 m across.
        body.transform.localScale = new Vector3(0.12f, 0.19f, 0.12f);
        if (pinMat != null) body.GetComponent<MeshRenderer>().material = pinMat;
        Object.Destroy(body.GetComponent<Collider>());

        // Red neck stripe near the top of the pin. (v1.2.0: slightly
        // wider and brighter red, so it reads from the camera.)
        GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stripe.name = "Stripe";
        stripe.transform.SetParent(root.transform, false);
        // Primitive cylinder is 2 units tall, 1 unit across.
        stripe.transform.localScale = new Vector3(0.13f, 0.02f, 0.13f);
        stripe.transform.localPosition = new Vector3(0f, 0.11f, 0f);
        if (stripeMat != null) stripe.GetComponent<MeshRenderer>().material = stripeMat;
        Object.Destroy(stripe.GetComponent<Collider>());

        Pin pin = new Pin();
        pin.Number = number;
        pin.Obj = root;
        pin.Rb = rb;
        pin.Home = RackPosition(number);
        pin.Active = true;
        return pin;
    }

    private Vector3 RackPosition(int number)
    {
        int i = number - 1;
        float x = BowlingConfig.PinOffsets[i, 0];
        float z = BowlingConfig.HeadPinZ + BowlingConfig.PinOffsets[i, 1];
        return new Vector3(x, BowlingConfig.PinHeight / 2f, z);
    }

    public void RackAll()
    {
        int[] all = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        RackPins(all);
    }

    // Rack only the listed pins (used by Spare Practice); the rest stay off.
    public void RackPins(int[] numbers)
    {
        HashSet<int> wanted = new HashSet<int>(numbers);
        foreach (Pin pin in pins)
        {
            if (wanted.Contains(pin.Number))
            {
                ResetPin(pin);
            }
            else
            {
                pin.Obj.SetActive(false);
                pin.Active = false;
            }
        }
    }

    private void ResetPin(Pin pin)
    {
        pin.Obj.SetActive(true);
        pin.Obj.transform.position = pin.Home;
        pin.Obj.transform.rotation = Quaternion.identity;
        pin.Rb.linearVelocity = Vector3.zero;
        pin.Rb.angularVelocity = Vector3.zero;
        pin.Active = true;
    }

    public bool IsFallen(Pin pin)
    {
        if (!pin.Active) return false;
        float tilt = Vector3.Angle(pin.Obj.transform.up, Vector3.up);
        if (tilt > BowlingConfig.PinFallTiltDegrees) return true;
        Vector3 flat = pin.Obj.transform.position - pin.Home;
        flat.y = 0f;
        return flat.magnitude > BowlingConfig.PinFallDistance;
    }

    public int CountFallen()
    {
        int count = 0;
        foreach (Pin pin in pins)
        {
            if (pin.Active && IsFallen(pin)) count++;
        }
        return count;
    }

    public int CountStanding()
    {
        int count = 0;
        foreach (Pin pin in pins)
        {
            if (pin.Active && !IsFallen(pin)) count++;
        }
        return count;
    }

    // The pin NUMBERS still standing (1–10, standard layout), for the
    // spoken ball-cam: "Pins 4 and 7 standing."
    public List<int> StandingNumbers()
    {
        List<int> numbers = new List<int>();
        foreach (Pin pin in pins)
        {
            if (pin.Active && !IsFallen(pin)) numbers.Add(pin.Number);
        }
        numbers.Sort();
        return numbers;
    }

    // Remove the fallen pins (dead wood) between balls in the same frame.
    public void RemoveFallen()
    {
        foreach (Pin pin in pins)
        {
            if (pin.Active && IsFallen(pin))
            {
                pin.Obj.SetActive(false);
                pin.Active = false;
            }
        }
    }
}
