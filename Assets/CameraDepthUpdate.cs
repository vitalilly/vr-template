using System.Collections;
using UnityEngine;

public class CameraDepthUpdate : MonoBehaviour
{
    [Header("Joint Settings")]
    [SerializeField] private ConfigurableJoint depthJoint;
    [SerializeField] private float maxAngularLimit = 45f; // Maximum pitch limit in degrees

    [Header("Intro Sequence Configuration")]
    [SerializeField] private bool playIntroSequence = true;
    [SerializeField] private float surfaceStartDepth = 0f;          // Initial drop depth (Surface)
    [SerializeField] private float targetDeploymentDepth = 400f;    // Point of no return
    [SerializeField] private float introDescentDuration = 12.0f;     // Sequence duration in seconds
    [SerializeField] private AnimationCurve descentEaseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Intro Radio Dialogue Clips")]
    [SerializeField] private AudioSource radioAudioSource;           // Dedicated 2D/3D Radio AudioSource
    [SerializeField] private AudioClip startDescentRadioClip;        // e.g. "Control to Sub, adjust heading 0-4-0..."
    [SerializeField] private AudioClip endDescentRadioClip;          // e.g. "Point of no return reached. You're on your own..."
    [SerializeField] [Range(0f, 1f)] private float startTriggerProgress = 0.05f; // Trigger near start (~5%)
    [SerializeField] [Range(0f, 1f)] private float endTriggerProgress = 0.85f;   // Trigger near bottom (~85%)

    [Header("Depth Range Configuration")]
    [SerializeField] private float minDepthMeters = 10f;   // Surface depth (Overwritten post-intro)
    [SerializeField] private float maxDepthMeters = 1200f; // Trench depth
    [SerializeField] private float depthChangeSpeed = 50f; // Meters per second at full tilt

    [Header("System References")]
    [SerializeField] private SonarCameraManager depthCamera;  
    [SerializeField] private EncounterManager encounterMngr;
    [SerializeField] private Material sonarShaderMaterial;

    [Header("Audio Dynamic Modulators")]
    [SerializeField] private AudioSource seaAmbienceSource;
    [SerializeField] private AudioLowPassFilter seaLowPassFilter;
    [SerializeField] private AudioSource engineAmbienceSource;
    [SerializeField] private AudioLowPassFilter engineLowPassFilter;

    [Header("Runtime State (Read Only)")]
    [SerializeField] private float currentDepthMeters = 50f;
    [SerializeField] private float normalizedDepth = 0.033f; // 0.0 (Surface) to 1.0 (Abyss)
    [SerializeField] private bool isIntroActive = false;

    private Quaternion initialLocalRotation;
    private Rigidbody jointRigidbody;

    private void Start()
    {
        if (depthJoint != null)
        {
            initialLocalRotation = depthJoint.transform.localRotation;
            depthJoint.TryGetComponent<Rigidbody>(out jointRigidbody);
        }

        if (playIntroSequence)
        {
            StartCoroutine(RunIntroSequence());
        }
        else
        {
            isIntroActive = false;
        }
    }
    // C# event fired when the intro sequence finishes
    public static event System.Action OnIntroSequenceCompleted;

    private IEnumerator RunIntroSequence()
    {
        isIntroActive = true;
        currentDepthMeters = surfaceStartDepth;

        // Kinematically lock joint physics during drop cutscene
        if (jointRigidbody != null)
        {
            jointRigidbody.isKinematic = true;
        }

        float elapsedTime = 0f;
        bool hasPlayedStartRadio = false;
        bool hasPlayedEndRadio = false;

        while (elapsedTime < introDescentDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / introDescentDuration);
            float curveT = descentEaseCurve.Evaluate(t);

            // Interpolate depth programmatically
            currentDepthMeters = Mathf.Lerp(surfaceStartDepth, targetDeploymentDepth, curveT);
            UpdateOutputs();

            // 1. Trigger Start Dialogue (e.g. "Adjust heading 0-4-0 towards the trench...")
            if (!hasPlayedStartRadio && t >= startTriggerProgress)
            {
                hasPlayedStartRadio = true;
                PlayRadioClip(startDescentRadioClip);
            }

            // 2. Trigger End Dialogue (e.g. "Operational depth reached. Good luck down there...")
            if (!hasPlayedEndRadio && t >= endTriggerProgress)
            {
                hasPlayedEndRadio = true;
                PlayRadioClip(endDescentRadioClip);
            }
            // Notify subscribers (EncounterManager) that the intro is complete

            yield return null;
        }

        // --- POINT OF NO RETURN REACHED ---
        // minDepthMeters = targetDeploymentDepth;
        currentDepthMeters = targetDeploymentDepth;

        // Unlock joint physics for player control
        if (jointRigidbody != null)
        {
            jointRigidbody.isKinematic = false;
        }
        OnIntroSequenceCompleted?.Invoke();
        isIntroActive = false;
    }

    private void PlayRadioClip(AudioClip clip)
    {
        if (radioAudioSource != null && clip != null)
        {
            radioAudioSource.Stop();
            radioAudioSource.PlayOneShot(clip);
        }
    }

    private void Update()
    {
        // Block player control during intro sequence
        if (isIntroActive || depthJoint == null) return;

        Quaternion relativeRotation = Quaternion.Inverse(initialLocalRotation) * depthJoint.transform.localRotation;
        float pitchAngle = WrapAngle(relativeRotation.eulerAngles.z);

        float inputPitch = Mathf.Clamp(pitchAngle / maxAngularLimit, -1f, 1f);
        
        if (Mathf.Abs(inputPitch) < 0.05f)
        {
            inputPitch = 0f;
        }

        if (inputPitch != 0f)
        {
            currentDepthMeters -= inputPitch * depthChangeSpeed * Time.deltaTime;
            currentDepthMeters = Mathf.Clamp(currentDepthMeters, minDepthMeters, maxDepthMeters);
        }

        UpdateOutputs();
    }

    private void UpdateOutputs()
    {
        normalizedDepth = Mathf.InverseLerp(minDepthMeters, maxDepthMeters, currentDepthMeters);

        // Update Sonar Camera Zoom
        if (depthCamera != null)
        {
            depthCamera.UpdateCameraDepthView(normalizedDepth);
        }

        if (encounterMngr != null)
        {
            encounterMngr.UpdateDepth(currentDepthMeters);
            // Debug.Log("Sending depth"+normalizedDepth);
        }

        // Pass updated depth parameters to Sonar CRT Shader
        if (sonarShaderMaterial != null)
        {
            sonarShaderMaterial.SetFloat("_SubDepth", currentDepthMeters);
            sonarShaderMaterial.SetFloat("_MaxDepth", maxDepthMeters);
        }

        // Modulate Ambient Audio Filtering based on depth
        ApplyDepthAudioModulation(normalizedDepth);
    }

    private void ApplyDepthAudioModulation(float normDepth)
    {
        if (seaLowPassFilter != null)
        {
            seaLowPassFilter.cutoffFrequency = Mathf.Lerp(5000f, 400f, normDepth);
        }

        if (seaAmbienceSource != null)
        {
            seaAmbienceSource.pitch = Mathf.Lerp(1.0f, 0.6f, normDepth);
            seaAmbienceSource.volume = Mathf.Lerp(0.10f, 0.45f, normDepth);
        }

        if (engineLowPassFilter != null)
        {
            engineLowPassFilter.cutoffFrequency = Mathf.Lerp(22000f, 1200f, normDepth);
        }

        if (engineAmbienceSource != null)
        {
            engineAmbienceSource.pitch = Mathf.Lerp(0.95f, 0.70f, normDepth);
        }
    }

    public float GetCurrentDepth() => currentDepthMeters;
    public void SetCurrentDepth(float depth) {currentDepthMeters = depth;} 
    public float GetNormalizedDepth() => normalizedDepth;
    public float GetMaxDepth() => maxDepthMeters;
    public bool IsIntroActive() => isIntroActive;
    
    private float WrapAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f)
            angle -= 360f;
        return angle;
    }
}