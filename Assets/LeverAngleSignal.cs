using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(ConfigurableJoint))]
public class JoystickVelocityController : MonoBehaviour
{
    [Header("Joint Mapping Configuration")]
    [Tooltip("Maximum tilt angle in degrees configured on the joint (e.g. 30 for +/-30 deg).")]
    [SerializeField] private float maxTiltAngle = 30f;
    [SerializeField] private float deadzone = 0.05f;

    [Header("Motor Audio Configuration")]
    [SerializeField] private AudioSource motorAudioSource;
    [SerializeField] private AudioSource engineKillSolenoidSource; // Optional mechanical relay/click sample
    [SerializeField] private float minVolume = 0.05f;  // Idle rumble volume
    [SerializeField] private float maxVolume = 0.9f;   // Full throttle volume
    [SerializeField] private float minPitch = 0.65f;   // Low idle pitch
    [SerializeField] private float maxPitch = 1.4f;    // Full throttle pitch
    [SerializeField] private float audioFadeSpeed = 3.0f; // Engine inertia response speed

    [Header("Engine State")]
    [SerializeField] private bool isEngineAlive = true;

    [Header("Events")]
    [Tooltip("Fired when the joystick's primary X-axis normalized value updates.")]
    public UnityEvent<float> onVelocityInputChanged;

    [Header("Output Readouts")]
    [Range(-1f, 1f)] public float normalizedVelocityInput;

    private ConfigurableJoint joint;
    private Quaternion initialLocalRotation;
    private float lastReportedInput;

    private float currentVolumeLevel;
    private float currentPitchLevel = 0.65f;

    private void Awake()
    {
        joint = GetComponent<ConfigurableJoint>();
        initialLocalRotation = transform.localRotation;
    }

    private void Start()
    {
        if (motorAudioSource != null)
        {
            motorAudioSource.loop = true;
            motorAudioSource.volume = 0f;
            motorAudioSource.pitch = minPitch;
        }
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

        // Process motor audio scaling based on velocity magnitude
        UpdateMotorAudio(Mathf.Abs(normalizedVelocityInput));
    }

    private void UpdateMotorAudio(float speedMagnitude)
    {
        if (motorAudioSource == null) return;

        // If the engine is cut (e.g. power failure/trap sequence)
        if (!isEngineAlive)
        {
            currentVolumeLevel = Mathf.MoveTowards(currentVolumeLevel, 0f, audioFadeSpeed * 2f * Time.deltaTime);
            motorAudioSource.volume = currentVolumeLevel;

            if (currentVolumeLevel <= 0.001f && motorAudioSource.isPlaying)
            {
                motorAudioSource.Stop();
            }
            return;
        }

        // Ensure playback loop is active when alive
        if (!motorAudioSource.isPlaying)
        {
            motorAudioSource.Play();
        }

        // Calculate target volume and pitch from speed magnitude (0.0 to 1.0)
        float targetVolume = (speedMagnitude < 0.02f) ? minVolume : Mathf.Lerp(minVolume, maxVolume, speedMagnitude);
        float targetPitch = Mathf.Lerp(minPitch, maxPitch, speedMagnitude);

        // Interpolate smoothly to simulate engine mass/wind-up delay
        currentVolumeLevel = Mathf.Lerp(currentVolumeLevel, targetVolume, Time.deltaTime * audioFadeSpeed);
        currentPitchLevel = Mathf.Lerp(currentPitchLevel, targetPitch, Time.deltaTime * audioFadeSpeed);

        motorAudioSource.volume = currentVolumeLevel;
        motorAudioSource.pitch = currentPitchLevel;
    }

    /// <summary>
    /// Call this to shut down or restart engine thruster audio (e.g., during brownouts)
    /// </summary>
    public void SetEnginePowerState(bool alive)
    {
        isEngineAlive = alive;

        if (!alive && engineKillSolenoidSource != null)
        {
            engineKillSolenoidSource.Play();
        }
    }

    public bool IsEngineAlive() => isEngineAlive;

    private float WrapAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f)
            angle -= 360f;
        return angle;
    }
}