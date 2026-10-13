using UnityEngine;
using UnityEngine.Rendering; // Required namespace for Volumes
// using UnityEngine.Rendering.Universal; // Only if you are accessing specific effects via script (like Lens Distortion intensity)

public class CabinFloodingController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform waterPlane; 
    [SerializeField] private Transform playerCamera; 
    [SerializeField] private AudioSource floodingAudioSource; 
    [SerializeField] private AudioLowPassFilter worldLowPassFilter; 

    // === NEW: Post-Processing Integration ===
    [Header("Underwater Post-Processing (URP Volume)")]
    [SerializeField] private Volume underwaterVolume; // The specific Volume object for underwater effects
    [SerializeField] private float visualTransitionSpeed = 2.0f; // Speed of the blending lerp
    // ===========================================

    [Header("Flooding Parameters")]
    [SerializeField] private float floorY = -1.5f;   
    [SerializeField] private float ceilingY = 1.0f;  
    [SerializeField] private float totalFloodTime = 45f; 

    [Header("Acoustics & Air Cavity Pitch")]
    [SerializeField] private float minPitch = 1.0f;  
    [SerializeField] private float maxPitch = 2.2f;  

    [Header("Audio Low-Pass Frequencies")]
    [SerializeField] private float normalCutoff = 22000f; 
    [SerializeField] private float underwaterCutoff = 600f; 
    [SerializeField] private float filterTransitionSpeed = 5.0f;

    private bool isFlooding = false;
    private float floodTimer = 0f;
    private bool playerIsSubmerged = false;
    private float targetCutoff;

    // === NEW: Weight Target ===
    private float targetVolumeWeight = 0f; // Target weight for the lerp
    // ============================

    public void TriggerFloodingSequence()
    {
        isFlooding = true;
        if (floodingAudioSource != null)
        {
            floodingAudioSource.pitch = minPitch;
            floodingAudioSource.Play();
        }

        if (worldLowPassFilter != null)
        {
            worldLowPassFilter.cutoffFrequency = normalCutoff;
            targetCutoff = normalCutoff;
        }

        // --- NEW: Initialize volume weight to 0 ---
        targetVolumeWeight = 0f;
        if (underwaterVolume != null) underwaterVolume.weight = 0f;
        // ------------------------------------------
    }

    private void Update()
    {
        if (!isFlooding) return;

        // 1. Progress water rising level
        if (floodTimer < totalFloodTime)
        {
            floodTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(floodTimer / totalFloodTime);
            
            float currentY = Mathf.Lerp(floorY, ceilingY, progress);
            Vector3 pos = waterPlane.localPosition;
            pos.y = currentY;
            waterPlane.localPosition = pos;

            if (floodingAudioSource != null && floodingAudioSource.isPlaying)
            {
                floodingAudioSource.pitch = Mathf.Lerp(minPitch, maxPitch, progress);
            }
        }

        // 2. Head Submergence Check
        if (playerCamera != null)
        {
            bool submergedNow = playerCamera.position.y < waterPlane.position.y;
            if (submergedNow != playerIsSubmerged)
            {
                playerIsSubmerged = submergedNow;
                ToggleUnderwaterState(playerIsSubmerged);
            }
        }

        // 3. Smooth Audio Filter Interpolation
        if (worldLowPassFilter != null)
        {
            worldLowPassFilter.cutoffFrequency = Mathf.Lerp(
                worldLowPassFilter.cutoffFrequency, 
                targetCutoff, 
                Time.deltaTime * filterTransitionSpeed
            );
        }

        // === NEW: 4. Smooth Post-Processing Volume Weight Interpolation ===
        if (underwaterVolume != null)
        {
            // Use MoveTowards for a precise, smooth linear blend for weight
            underwaterVolume.weight = Mathf.MoveTowards(
                underwaterVolume.weight, 
                targetVolumeWeight, 
                Time.deltaTime * visualTransitionSpeed
            );
        }
        // ====================================================================
    }

    private void ToggleUnderwaterState(bool submerged)
    {
        if (submerged)
        {
            Debug.Log("[Flooding] Head underwater. Engaging muffling and POST-EFFECTS.");
            targetCutoff = underwaterCutoff;
            if (floodingAudioSource != null) floodingAudioSource.pitch = 0.75f; 

            // --- NEW: Set Target for Post-Processing Volume ---
            targetVolumeWeight = 1.0f; // Fully engage the underwater effect volume
        }
        else
        {
            Debug.Log("[Flooding] Head above water. Restoring normal state.");
            targetCutoff = normalCutoff;

            // --- NEW: Set Target for Post-Processing Volume ---
            targetVolumeWeight = 0.0f; // Smoothly disengage the underwater effect volume
        }
    }
}