using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(ConfigurableJoint))]
public class JoystickVelocityController : MonoBehaviour
{
    [Header("Joint Mapping Configuration")]
    [Tooltip("Maximum tilt angle in degrees configured on the joint (e.g. 30 for +/-30 deg).")]
    [SerializeField] private float maxTiltAngle = 30f;
    [SerializeField] private float deadzone = 0.05f;

    [Header("Events")]
    [Tooltip("Fired when the joystick's primary X-axis normalized value updates.")]
    public UnityEvent<float> onVelocityInputChanged;

    [Header("Output Readouts")]
    [Range(-1f, 1f)] public float normalizedVelocityInput;

    private ConfigurableJoint joint;
    private Quaternion initialLocalRotation;
    private float lastReportedInput;

    private void Awake()
    {
        joint = GetComponent<ConfigurableJoint>();
        initialLocalRotation = transform.localRotation;
    }

    private void Update()
    {
        Quaternion deltaRotation = Quaternion.Inverse(initialLocalRotation) * transform.localRotation;
        float rollZ = WrapAngle(deltaRotation.eulerAngles.z);

        // Raw input mapping (-1 to +1)
        float rawInput = Mathf.Clamp(rollZ / maxTiltAngle, -1f, 1f);

        // Apply Deadzone
        float deadzoneInput = -(Mathf.Abs(rawInput) < deadzone ? 0f : rawInput);

        if (!Mathf.Approximately(deadzoneInput, lastReportedInput))
        {
            normalizedVelocityInput = deadzoneInput;
            lastReportedInput = deadzoneInput;

            // Broadcast new value to listening scripts
            onVelocityInputChanged?.Invoke(normalizedVelocityInput);
        }
    }

    private float WrapAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f)
            angle -= 360f;
        return angle;
    }
}