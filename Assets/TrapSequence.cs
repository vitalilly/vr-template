using UnityEngine;

public class AcousticTrapSector : MonoBehaviour
{
    [Header("Sonar & Terrain FX")]
    [SerializeField] private Material sonarShaderMaterial;
    [SerializeField] private GameObject[] mineBlips2D;
    
    [Header("Events")]
    [SerializeField] private SubmarineLightGroup lightGroup;
    [SerializeField] private AudioSource instructorRadio;
    [SerializeField] private AudioClip panicRadioClip;

    private bool trapTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Submarine2DProxy") && !trapTriggered)
        {
            // Player entered the trench lip volume
            Debug.Log("[Terrain] Entering Trench Lip: Arming Trap...");
        }
    }

    // Called when player activates active sonar ping inside the trigger zone
    public void TriggerTrap()
    {
        if (trapTriggered) return;
        trapTriggered = true;

        // 1. Play instructor panic audio
        instructorRadio.PlayOneShot(panicRadioClip);

        // 2. Trigger cockpit lighting brownout
        lightGroup.TriggerPowerFailure();

        // 3. Reveal hidden mine contacts on the 2D nav screen
        foreach (var mine in mineBlips2D)
        {
            mine.SetActive(true);
        }

        // 4. Inject shader distortion into 2D Sonar Render Texture
        if (sonarShaderMaterial != null)
        {
            sonarShaderMaterial.SetFloat("_SonarDistortion", 1.0f);
        }
    }
}