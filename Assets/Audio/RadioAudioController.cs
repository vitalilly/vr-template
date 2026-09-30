using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class RadioDialogueController : MonoBehaviour
{
    [Header("Audio Tracks (.wav)")]
    [Tooltip("Drag your dialogue .wav files into this list.")]
    [SerializeField] private List<AudioClip> dialogueClips = new List<AudioClip>();

    [Header("Optional Radio Effects")]
    [Tooltip("Optional static/squelch sound played before/after dialogue.")]
    [SerializeField] private AudioClip radioStaticSquelch;

    [Header("Proximity Settings")]
    [Tooltip("Tag on the VR Camera / Player transform (e.g., 'MainCamera' or 'Player').")]
    [SerializeField] private string playerTag = "MainCamera";
    [Tooltip("If true, dialogue stops when player leaves the cockpit seat trigger area.")]
    [SerializeField] private bool stopWhenLeavingSeat = false;

    [Header("Playback Options")]
    [SerializeField] private bool loopPlaylist = false;
    [SerializeField] private bool playRandomOrder = false;

    private AudioSource audioSource;
    private int currentClipIndex = 0;
    private bool isPlayingSequence = false;
    private bool isPlayerInSeat = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        // Ensure Spatial Blend is 3D and non-zero min/max ranges
        audioSource.spatialBlend = 1.0f; 
        audioSource.playOnAwake = false;
    }

    // Triggered automatically when player enters CockpitSeatZone collider
    private void OnTriggerEnter(Collider other)
    {
        // 1. Diagnostic log to see WHAT is hitting the trigger
        Debug.Log($"[Radio Trigger] Something entered: {other.gameObject.name} with Tag: {other.tag}");

        if (other.CompareTag(playerTag) || (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(playerTag)))
        {
            Debug.Log("[Radio Trigger] Verified Player entered cockpit seat zone!");
            isPlayerInSeat = true;

            if (!isPlayingSequence && !audioSource.isPlaying)
            {
                PlayNextDialogue();
            }
        }
    }

    // Triggered when player leaves the zone
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag) || (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(playerTag)))
        {
            isPlayerInSeat = false;

            if (stopWhenLeavingSeat)
            {
                StopRadio();
            }
        }
    }

    public void PlayNextDialogue()
    {
        if (dialogueClips.Count == 0)
        {
            Debug.LogWarning("[Radio] No dialogue clips assigned in inspector!", this);
            return;
        }

        if (playRandomOrder)
        {
            currentClipIndex = Random.Range(0, dialogueClips.Count);
        }

        AudioClip selectedClip = dialogueClips[currentClipIndex];

        if (selectedClip != null)
        {
            StartCoroutine(PlayDialogueWithEffects(selectedClip));
        }

        currentClipIndex = (currentClipIndex + 1) % dialogueClips.Count;
    }

    private IEnumerator PlayDialogueWithEffects(AudioClip clip)
    {
        isPlayingSequence = true;

        // 1. Static intro squelch
        if (radioStaticSquelch != null)
        {
            audioSource.PlayOneShot(radioStaticSquelch);
            yield return new WaitForSeconds(radioStaticSquelch.length * 0.8f);
        }

        // 2. Main dialogue clip
        audioSource.clip = clip;
        audioSource.Play();

        yield return new WaitWhile(() => audioSource.isPlaying);

        // 3. Static outro squelch
        if (radioStaticSquelch != null)
        {
            audioSource.PlayOneShot(radioStaticSquelch);
            yield return new WaitForSeconds(radioStaticSquelch.length);
        }

        isPlayingSequence = false;

        // Auto-advance if player remains seated and loop is active
        if (isPlayerInSeat && loopPlaylist)
        {
            PlayNextDialogue();
        }
    }

    public void StopRadio()
    {
        StopAllCoroutines();
        audioSource.Stop();
        isPlayingSequence = false;
    }
    
}