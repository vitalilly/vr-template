using UnityEngine;
using System.Collections;

public class RadioManager : MonoBehaviour
{
    public static RadioManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private AudioSource radioAudioSource;
    [SerializeField] private AudioLowPassFilter radioLowPassFilter;

    [Header("Radio Atmosphere")]
    [SerializeField] private AudioClip staticBurstClip; // Static click/squelch effect
    [Range(0f, 1f)] [SerializeField] private float staticVolume = 0.6f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Centralized method called by EncounterManager to play radio dialogue with full radio effects.
    /// </summary>
    public void PlayRadioTransmission(AudioClip dialogueClip)
    {
    if (dialogueClip == null)
    {
        Debug.LogWarning("[RadioManager] Dialogue clip passed is NULL!");
        return;
    }

    if (radioAudioSource == null)
    {
        Debug.LogError("[RadioManager] radioAudioSource reference is missing in the Inspector!");
        return;
    }

    Debug.Log($"[RadioManager] Playing transmission for clip: {dialogueClip.name}");
    StopAllCoroutines();
    StartCoroutine(TransmissionRoutine(dialogueClip));
    }

    private IEnumerator TransmissionRoutine(AudioClip clip)
    {
        // 1. Play opening static squelch/burst
        if (staticBurstClip != null)
        {
            radioAudioSource.PlayOneShot(staticBurstClip, staticVolume);
            // Brief pause to let the static cut through before the voice starts
            yield return new WaitForSeconds(0.15f); 
        }

        // 2. Play the actual dialogue clip
        radioAudioSource.clip = clip;
        radioAudioSource.Play();

        // 3. Wait until the dialogue finishes playing
        yield return new WaitForSeconds(clip.length);

        // 4. Play closing static burst
        if (staticBurstClip != null)
        {
            radioAudioSource.PlayOneShot(staticBurstClip, staticVolume);
        }
    }
}