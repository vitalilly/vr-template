using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Submarine2DNavProxy : MonoBehaviour
{
    [Header("Engine & Steering Settings")]
    [SerializeField] private float moveSpeed = 500f;
    [SerializeField] private float turnSpeed = 150f;
    [SerializeField] private float accelerationRate = 2.0f;

    private Rigidbody2D rb2d;
    private float targetThrottle;
    private float targetSteering;
    
    private float currentThrottle;
    private float currentSteering;

    private void Awake()
    {
        rb2d = GetComponent<Rigidbody2D>();
    }

    // Connect to Joystick's onVelocityInputChanged event
    public void SetThrottleInput(float value)
    {
        targetThrottle = value;
        //Debug.Log($"2D Proxy Received Throttle: {value}");
    }

    // Connect to Steering Wheel's onSteeringInputChanged event
    public void SetSteeringInput(float value)
    {
        targetSteering = value;
        //Debug.Log($"2D Proxy Received Steering: {value}");
    }


    private void FixedUpdate()
    {
        // Smooth input
        currentThrottle = Mathf.MoveTowards(currentThrottle, targetThrottle, accelerationRate * Time.fixedDeltaTime);
        currentSteering = Mathf.MoveTowards(currentSteering, targetSteering, accelerationRate * Time.fixedDeltaTime);

        // Apply 2D Rotation (Z-Axis Yaw)
        float rotationAmount = -currentSteering * turnSpeed * Time.fixedDeltaTime;
        rb2d.MoveRotation(rb2d.rotation + rotationAmount);

        // Apply 2D Forward Movement (up vector in 2D space)
        Vector2 forward2D = transform.up; 
        Vector2 force = forward2D * (currentThrottle * moveSpeed * Time.fixedDeltaTime);
        rb2d.AddForce(force, ForceMode2D.Force);
    }

    // Optional: Detect impact events to trigger audio or hull damage logic
    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"Submarine collided with terrain hazard: {collision.gameObject.name}");
        // Trigger hull crunch sound effect or VR haptic rumble here
    }
}