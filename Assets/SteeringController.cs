using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(HingeJoint))]
public class SteeringWheelController : MonoBehaviour
{
    [Header("Steering Limits")]
    [Tooltip("Maximum steering angle in degrees (e.g. 90 for +/-90 deg, or 180 for +/-180 deg).")]
    [SerializeField] private float maxSteeringAngle = 90f;
    [SerializeField] private float deadzone = 0.05f;

    [Header("Events")]
    [Tooltip("Fired when normalized steering input changes (-1 = Left, +1 = Right).")]
    public UnityEvent<float> onSteeringInputChanged;

    [Header("Output Readouts")]
    [Range(-1f, 1f)] public float normalizedSteeringInput;

    private HingeJoint hinge;
    private float lastReportedInput;

    private void Awake()
    {
        hinge = GetComponent<HingeJoint>();
    }

    private void Update()
    {
        // HingeJoint.angle gives exact rotation in degrees relative to the joint rest angle (-180 to +180)
        float currentAngle = hinge.angle;

        // Normalize between -1.0 (Full Left) and +1.0 (Full Right)
        float rawInput = Mathf.Clamp(currentAngle / maxSteeringAngle, -1f, 1f);

        // Apply Deadzone
        float deadzoneInput = Mathf.Abs(rawInput) < deadzone ? 0f : rawInput;

        if (!Mathf.Approximately(deadzoneInput, lastReportedInput))
        {
            normalizedSteeringInput = deadzoneInput;
            lastReportedInput = deadzoneInput;

            // Broadcast new steering value
            onSteeringInputChanged?.Invoke(normalizedSteeringInput);
        }
    }
}