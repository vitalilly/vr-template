using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SubmarineLightGroup : MonoBehaviour
{
    [Header("Group Components")]
    [SerializeField] private List<Light> targetLights = new List<Light>();
    [SerializeField] private List<Renderer> emissiveRenderers = new List<Renderer>();

    [Header("Flicker Profile Settings")]
    [Tooltip("Lower values (2-5) create slow, heavy power surges. Higher values (10+) speed it up.")]
    [SerializeField] private float flickerFrequency = 4.0f;
    
    [Tooltip("Cutoff threshold for Perlin noise. Values below this drop to pitch black.")]
    [Range(0.1f, 0.6f)]
    [SerializeField] private float dropOutThreshold = 0.35f;

    [Header("Emergency Backup Glow")]
    [SerializeField] private bool enableRedEmergencyBackup = true;
    [ColorUsage(true, true)]
    [SerializeField] private Color emergencyColor = Color.red;

    private Dictionary<Light, float> originalIntensities = new Dictionary<Light, float>();
    private Coroutine activeFlickerRoutine;
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        // Store baseline light intensities for accurate restoration
        foreach (Light l in targetLights)
        {
            if (l != null) originalIntensities[l] = l.intensity;
        }
    }

    /// <summary>
    /// Instantly cuts off or restores power to this light group.
    /// </summary>
    public void SetPower(bool isOn)
    {
        if (activeFlickerRoutine != null)
        {
            StopCoroutine(activeFlickerRoutine);
            activeFlickerRoutine = null;
        }

        ApplyLightState(isOn ? 1.0f : 0.0f, isOn ? Color.white : (enableRedEmergencyBackup ? emergencyColor : Color.black));
    }

    /// <summary>
    /// Triggers a smooth, heavy brownout flicker before cutting main power.
    /// </summary>
    public void TriggerPowerFailure(float flickerDuration = 2.5f)
    {
        if (activeFlickerRoutine != null) StopCoroutine(activeFlickerRoutine);
        activeFlickerRoutine = StartCoroutine(Routine_PowerFailure(flickerDuration));
    }

    private IEnumerator Routine_PowerFailure(float duration)
    {
        float timer = 0f;
        float noiseOffset = Random.Range(0f, 100f); 

        while (timer < duration)
        {
            timer += Time.deltaTime;

            // 1. Compute smooth Perlin noise wave
            float noise = Mathf.PerlinNoise(noiseOffset, Time.time * flickerFrequency);

            // 2. Apply threshold so light drops out during wave troughs
            float intensityFactor = noise > dropOutThreshold ? noise : 0.0f;

            // 3. Drive lighting and emissive materials
            ApplyLightState(intensityFactor, intensityFactor > 0.05f ? Color.white : Color.black);

            yield return null; // Evaluates smoothly frame-by-frame
        }

        // Final State: Shut off main power and transition to emergency red backup
        ApplyLightState(0.0f, enableRedEmergencyBackup ? emergencyColor : Color.black);
        activeFlickerRoutine = null;
    }

    private void ApplyLightState(float intensityMultiplier, Color glowColor)
    {
        // Drive real-time point/spot lights
        foreach (Light l in targetLights)
        {
            if (l != null && originalIntensities.ContainsKey(l))
            {
                l.intensity = originalIntensities[l] * intensityMultiplier;
                l.enabled = intensityMultiplier > 0.01f;
            }
        }

        // Drive emissive panel/gauge materials
        foreach (Renderer rend in emissiveRenderers)
        {
            if (rend != null)
            {
                Material mat = rend.material;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor(EmissionColorID, glowColor * intensityMultiplier);
            }
        }
    }
}