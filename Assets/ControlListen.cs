using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Submarine2DNavProxy : MonoBehaviour
{
    [Header("Engine & Steering Settings")]
    [SerializeField] private float moveSpeed = 500f;
    [SerializeField] private float turnSpeed = 150f;
    [SerializeField] private float accelerationRate = 2.0f;

    [Header("Hydrodynamics & Inertia")]
    [SerializeField] private float lateralDrag = 4.0f; 

    // === NEW: Control Lock State ===
    private bool controlsEnabled = true;
    // ===============================

    private Rigidbody2D rb2d;
    private float targetThrottle;
    private float targetSteering;
    
    private float currentThrottle;
    private float currentSteering;

    private void Awake()
    {
        rb2d = GetComponent<Rigidbody2D>();
        rb2d.linearDamping = 0.5f; 
    }

    // === NEW: External method to enable/disable controls ===
    public void SetControlsEnabled(bool enabled)
    {
        controlsEnabled = enabled;
        if (!enabled)
        {
            // Clear inputs immediately so it doesn't hold last-known throttle/steering
            targetThrottle = 0f;
            targetSteering = 0f;
            currentThrottle = 0f;
            currentSteering = 0f;
        }
    }
    // =======================================================

    public void SetThrottleInput(float value)
    {
        if (!controlsEnabled) return;
        targetThrottle = value;
    }

    public void SetSteeringInput(float value)
    {
        if (!controlsEnabled) return;
        targetSteering = value;
    }

    private void FixedUpdate()
    {
        // === NEW: Skip movement calculations if controls are locked ===
        if (!controlsEnabled) return;
        // ==============================================================

        // 1. Smooth input
        currentThrottle = Mathf.MoveTowards(currentThrottle, targetThrottle, accelerationRate * Time.fixedDeltaTime);
        currentSteering = Mathf.MoveTowards(currentSteering, targetSteering, accelerationRate * Time.fixedDeltaTime);

        // 2. Apply 2D Rotation (Z-Axis Yaw)
        float rotationAmount = -currentSteering * turnSpeed * Time.fixedDeltaTime;
        rb2d.MoveRotation(rb2d.rotation + rotationAmount);

        // 3. Apply 2D Forward Movement force
        Vector2 forward2D = transform.up; 
        Vector2 force = forward2D * (currentThrottle * moveSpeed * Time.fixedDeltaTime);
        rb2d.AddForce(force, ForceMode2D.Force);

        // 4. HYDRODYNAMIC LATERAL DAMPING
        Vector2 right2D = transform.right;
        float sidewaysSpeed = Vector2.Dot(rb2d.linearVelocity, right2D);
        Vector2 sidewaysVelocity = right2D * sidewaysSpeed;
        rb2d.linearVelocity -= sidewaysVelocity * Mathf.Clamp01(lateralDrag * Time.fixedDeltaTime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"Submarine collided with terrain hazard: {collision.gameObject.name}");
    }
}