using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement; // Required if reloading the scene on game over

public class CabinFloodingController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform waterPlane; 
    [SerializeField] private Transform playerCamera; 
    [SerializeField] private AudioSource floodingAudioSource; 
    [SerializeField] private AudioLowPassFilter worldLowPassFilter; 

    [Header("Underwater Post-Processing")]
    [SerializeField] private Volume underwaterVolume; 
    [SerializeField] private float visualTransitionSpeed = 2.0f; 

    [Header("Oxygen & Drowning Settings")]
    [SerializeField] private float maxOxygenTime = 10f; // 10 seconds underwater before game over
    private float currentOxygenTimer = 0f;
    private bool isGameOverTriggered = false;

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

    [Header("Submarine Control Integration")]
    [SerializeField] private Submarine2DNavProxy submarineNavProxy;
    [SerializeField] private CameraDepthUpdate cameraDepthUpdate;

    private bool isFlooding = false;
    private float floodTimer = 0f;
    private bool playerIsSubmerged = false;
    private float targetCutoff;
    private float targetVolumeWeight = 0f; 

    public void TriggerFloodingSequence()
    {
        isFlooding = true;
        isGameOverTriggered = false;
        currentOxygenTimer = maxOxygenTime;
        if (submarineNavProxy != null)
        {
            submarineNavProxy.SetControlsEnabled(false);
        }
        if (cameraDepthUpdate != null)
        {
            cameraDepthUpdate.SetControlsEnabled(false);
        }
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

        targetVolumeWeight = 0f;
        if (underwaterVolume != null) underwaterVolume.weight = 0f;
    }

    private void Update()
    {
        if (!isFlooding || isGameOverTriggered) return;

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

            // === OXYGEN COUNTDOWN LOGIC ===
            if (playerIsSubmerged)
            {
                currentOxygenTimer -= Time.deltaTime;
                // Optional: Debug.Log($"Oxygen remaining: {currentOxygenTimer:F1}s");

                if (currentOxygenTimer <= 0f)
                {
                    TriggerGameOver();
                }
            }
            else
            {
                // Replenish oxygen quickly when above water
                currentOxygenTimer = Mathf.MoveTowards(currentOxygenTimer, maxOxygenTime, Time.deltaTime * 5f);
            }
            // ================================
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

        // 4. Smooth Post-Processing Volume Weight Interpolation
        if (underwaterVolume != null)
        {
            underwaterVolume.weight = Mathf.MoveTowards(
                underwaterVolume.weight, 
                targetVolumeWeight, 
                Time.deltaTime * visualTransitionSpeed
            );
        }
    }

    private void ToggleUnderwaterState(bool submerged)
    {
        if (submerged)
        {
            targetCutoff = underwaterCutoff;
            if (floodingAudioSource != null) floodingAudioSource.pitch = 0.75f; 
            targetVolumeWeight = 1.0f; 
        }
        else
        {
            targetCutoff = normalCutoff;
            targetVolumeWeight = 0.0f; 
        }
    }

    private void TriggerGameOver()
    {
        isGameOverTriggered = true;
        Debug.Log("[Flooding] Oxygen depleted. Experience ending.");

        // Implement your end-game sequence here. 
        // For example: fade to black, trigger a UI death screen, or reload the scene:
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}