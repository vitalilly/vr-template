using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AbyssalFloorSequenceManager : MonoBehaviour
{
    [Header("Player Control Lockout")]
    [SerializeField] private MonoBehaviour submarineMovementController; // e.g. Submarine2DNavProxy or Physics Controller
    [SerializeField] private Rigidbody2D submarineRigidbody;
    [SerializeField] private CameraDepthUpdate depthController;

    [Header("Aftermath Audio Sequence")]
    [SerializeField] private AudioSource radioAudioSource;
    [SerializeField] private List<AudioClip> postCrashDialogueSequence = new List<AudioClip>();
    [SerializeField] private float initialDelayBeforeDialogue = 3.0f; // Silence after crash impact
    [SerializeField] private float delayBetweenLines = 0.8f;

    [Header("Post-Crash State Events")]
    [SerializeField] private UnityEvent OnFloorSequenceStarted;
    [SerializeField] private UnityEvent OnFloorSequenceCompleted;

    private void OnEnable()
    {
        EncounterManager.SubmarineSignals.OnSubmarineCrashedAtFloor += HandleAbyssalFloorArrival;
    }

    private void OnDisable()
    {
        EncounterManager.SubmarineSignals.OnSubmarineCrashedAtFloor -= HandleAbyssalFloorArrival;
    }

    private void HandleAbyssalFloorArrival()
    {
        Debug.Log("<color=magenta>[AbyssalFloorManager] Lockout state initiated. Player control disabled.</color>");

        // 1. Permanently disable pilot input/movement controller
        if (submarineMovementController != null)
        {
            submarineMovementController.enabled = false;
        }

        // 2. Lock submarine physics velocity
        if (submarineRigidbody != null)
        {
            submarineRigidbody.linearVelocity = Vector2.zero;
            submarineRigidbody.angularVelocity = 0f;
            submarineRigidbody.isKinematic = true;
        }

        // 3. Ensure depth remains hard-clamped to maximum trench depth
        if (depthController != null)
        {
            depthController.SetCurrentDepth(depthController.GetMaxDepth());
        }

        // 4. Trigger visual state changes (emergency red lighting, sparking UI)
        OnFloorSequenceStarted?.Invoke();

        // 5. Begin post-crash dialogue sequence
        StartCoroutine(RunPostCrashDialogueRoutine());
    }

    private IEnumerator RunPostCrashDialogueRoutine()
    {
        // Moment of heavy silence on the ocean floor
        if (initialDelayBeforeDialogue > 0f)
        {
            yield return new WaitForSeconds(initialDelayBeforeDialogue);
        }

        // Play post-crash dialogue line-by-line
        if (radioAudioSource != null && postCrashDialogueSequence != null)
        {
            foreach (AudioClip clip in postCrashDialogueSequence)
            {
                if (clip != null)
                {
                    radioAudioSource.Stop();
                    radioAudioSource.PlayOneShot(clip);

                    yield return new WaitForSeconds(clip.length);

                    if (delayBetweenLines > 0f)
                    {
                        yield return new WaitForSeconds(delayBetweenLines);
                    }
                }
            }
        }

        Debug.Log("<color=magenta>[AbyssalFloorManager] Post-crash sequence finished.</color>");
        OnFloorSequenceCompleted?.Invoke();
    }
}