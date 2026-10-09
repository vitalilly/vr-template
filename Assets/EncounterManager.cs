using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class EncounterEvent
{
    public string encounterName = "SOS Signal";
    
    [Header("Trigger Conditions")]
    public TriggerType triggerType = TriggerType.DistanceTraveled;
    [Tooltip("Target distance in world units (if DistanceTraveled) or index.")]
    public float triggerThreshold = 500f; 
    public Transform wayPointLocation; // For WaypointProximity trigger type

    [Header("Encounter Deliverables")]
    [Tooltip("Sequential list of dialogue clips to play in order.")]
    public List<AudioClip> dialogueSequence = new List<AudioClip>();
    public float delayBetweenLines = 0.5f;
    public float delayBeforeLightsOff = 1.0f;
    public Vector2 sonarContactPosition; // Relative position for dynamic CRT blip
    public float headingLenience; 
    [Tooltip("Maximum angle the submarine can be turned away from waypoint")]
    public float expectedDepth; 
    [Tooltip("Maximum depth at which waypoint triggers")]

    [Header("Climax / Sinking Trigger")]
    public bool triggersSinkingSequence = false;
    [Tooltip("CHECK THIS ONLY FOR THE FINAL ENCOUNTER! Standard encounters should leave this unticked.")]

    [Header("Chaos / Sinking Parameters (Only if triggersSinkingSequence = true)")]
    public List<AudioClip> chaosAudioClips = new List<AudioClip>();
    [Tooltip("Audio clips to fire simultaneously when lights cut out (sirens, explosions, hull creaks).")]
    public float rapidSinkingSpeed = 45f;      // Downward rate in meters/second
    public float forwardPropulsionSpeed = 15f; // Forward velocity into the trench
    public float sinkingDuration = 8.0f;       // Seconds the uncontrolled dive lasts
    
    [Header("Unity Events")]
    [Tooltip("Triggers immediately when the encounter condition is met.")]
    public UnityEvent onEncounterTriggered;
    [Tooltip("Triggers after dialogue completes (e.g. for blackout event on final encounter).")]
    public UnityEvent onLightsOffTriggered;

    [HideInInspector] public bool hasTriggered = false;
}

public enum TriggerType
{
    DistanceTraveled,
    WaypointProximity,
    ManualTrigger,
    Depth,
    Heading,
    Vector
}

public class EncounterManager : MonoBehaviour
{
    [SerializeField] private CameraDepthUpdate depthController;
    private bool isEncountersActive = false;

    [Header("Tracking Target")]
    [SerializeField] private Transform submarineProxy;
    
    [Header("Connected Cockpit Systems")]
    [SerializeField] private AudioSource radioAudioSource;
    [SerializeField] private AudioSource ambientChaosAudioSource;
    [SerializeField] private SonarTerrain sonarDriver;

    [Header("Scripted Encounter List")]
    [SerializeField] private List<EncounterEvent> encounterList = new List<EncounterEvent>();

    private Vector3 lastPosition;
    private float totalDistanceTraveled = 0f;
    private float currentDepth = 50f;

    private void OnEnable()
    {
        CameraDepthUpdate.OnIntroSequenceCompleted += HandleIntroCompleted;
    }

    private void OnDisable()
    {
        CameraDepthUpdate.OnIntroSequenceCompleted -= HandleIntroCompleted;
    }

    private void Start()
    {
        if (submarineProxy != null)
        {
            lastPosition = submarineProxy.position;
        }
        
        if (depthController != null && !depthController.IsIntroActive())
        {
            ActivateEncounters();
        }
    }

    private void HandleIntroCompleted()
    {
        ActivateEncounters();
    }

    private void ActivateEncounters()
    {
        if (submarineProxy != null)
        {
            lastPosition = submarineProxy.position;
        }
        
        totalDistanceTraveled = 0f;
        isEncountersActive = true;
        Debug.Log("[EncounterManager] Encounters activated safely.");
    }

    private void Update()
    {
        if (submarineProxy == null || !isEncountersActive) return;

        if (depthController != null)
        {
            currentDepth = depthController.GetCurrentDepth();
        }

        // 1. Calculate total distance traveled by submarine
        float stepDistance = Vector3.Distance(submarineProxy.position, lastPosition);
        float currentHeadingDeg = WrapAngle(submarineProxy.localEulerAngles.z);
        totalDistanceTraveled += stepDistance;
        lastPosition = submarineProxy.position;

        // 2. Evaluate un-triggered encounters
        foreach (var encounter in encounterList)
        {
            if (encounter.hasTriggered) continue;

            bool shouldTrigger = false;

            switch (encounter.triggerType)
            {
                case TriggerType.DistanceTraveled:
                    if (totalDistanceTraveled >= encounter.triggerThreshold)
                    {
                        shouldTrigger = true;
                    }
                    break;

                case TriggerType.WaypointProximity:
                    if (encounter.wayPointLocation != null)
                    {
                        float distToWaypoint = Vector3.Distance(submarineProxy.position, encounter.wayPointLocation.position);
                        if (distToWaypoint <= encounter.triggerThreshold)
                        {
                            shouldTrigger = true;
                        }
                    }
                    break;
                
                case TriggerType.Depth:
                    if (currentDepth < encounter.expectedDepth && totalDistanceTraveled < 4f)
                    {
                        shouldTrigger = true;
                    }  
                    break;

                case TriggerType.Heading:
                    if (Math.Abs(currentHeadingDeg) > encounter.headingLenience && totalDistanceTraveled < 4f)
                    {
                        shouldTrigger = true;
                    }  
                    break;
                
                case TriggerType.Vector:
                    if (encounter.wayPointLocation != null)
                    {
                        float distToWaypoint = Vector3.Distance(submarineProxy.position, encounter.wayPointLocation.position);
                        if (distToWaypoint > encounter.triggerThreshold && totalDistanceTraveled < 4f)
                        {
                            shouldTrigger = true;
                        }
                    }
                    break;
            }

            if (shouldTrigger)
            {
                ExecuteEncounter(encounter);
            }
        }
    }

    private void ExecuteEncounter(EncounterEvent encounter)
    {
        encounter.hasTriggered = true;
        Debug.Log($"<color=cyan>[Encounter System] Executing Special Event: {encounter.encounterName}</color>");

        encounter.onEncounterTriggered?.Invoke();
        StartCoroutine(RunEncounterSequence(encounter));
    }

    private IEnumerator RunEncounterSequence(EncounterEvent encounter)
    {
        // 1. Play dialogue sequence
        if (radioAudioSource != null && encounter.dialogueSequence != null && encounter.dialogueSequence.Count > 0)
        {
            for (int i = 0; i < encounter.dialogueSequence.Count; i++)
            {
                AudioClip clip = encounter.dialogueSequence[i];
                if (clip != null)
                {
                    radioAudioSource.Stop();
                    radioAudioSource.PlayOneShot(clip);

                    yield return new WaitForSeconds(clip.length);

                    if (encounter.delayBetweenLines > 0f)
                    {
                        yield return new WaitForSeconds(encounter.delayBetweenLines);
                    }
                }
            }
        }

        // 2. IF this encounter is configured to sink the sub, execute the blackout/sinking routine
        if (encounter.triggersSinkingSequence)
        {
            if (encounter.delayBeforeLightsOff > 0f)
            {
                yield return new WaitForSeconds(encounter.delayBeforeLightsOff);
            }

            ExecuteLightsOffAndStartSinking(encounter);
        }
    }

    private void ExecuteLightsOffAndStartSinking(EncounterEvent encounter)
    {
        Debug.Log($"<color=red>[Encounter System] Power Cut! Initiating uncontrolled plunge for: {encounter.encounterName}</color>");

        encounter.onLightsOffTriggered?.Invoke();

        if (encounter.chaosAudioClips != null)
        {
            foreach (AudioClip chaosClip in encounter.chaosAudioClips)
            {
                if (chaosClip != null)
                {
                    if (ambientChaosAudioSource != null)
                    {
                        ambientChaosAudioSource.PlayOneShot(chaosClip);
                    }
                    else if (radioAudioSource != null)
                    {
                        radioAudioSource.PlayOneShot(chaosClip);
                    }
                }
            }
        }

        StartCoroutine(RapidSinkingRoutine(encounter));
    }

    public static class SubmarineSignals
    {
        public static System.Action OnSubmarineCrashedAtFloor;
    }

    private IEnumerator RapidSinkingRoutine(EncounterEvent encounter)
    {
        float timer = 0f;

        while (timer < encounter.sinkingDuration)
        {
            timer += Time.deltaTime;

            if (depthController != null)
            {
                float newDepth = depthController.GetCurrentDepth() + (encounter.rapidSinkingSpeed * Time.deltaTime);
                depthController.SetCurrentDepth(newDepth);
            }

            if (submarineProxy != null)
            {
                submarineProxy.position += submarineProxy.up * (encounter.forwardPropulsionSpeed * Time.deltaTime);
            }

            yield return null;
        }

        if (depthController != null)
        {
            depthController.SetCurrentDepth(depthController.GetMaxDepth());
        }

        Debug.Log("<color=red>[SubmarineSignals] Submarine reached trench floor! Firing OnSubmarineCrashedAtFloor event.</color>");

        SubmarineSignals.OnSubmarineCrashedAtFloor?.Invoke();
    }

    public void TriggerEncounterByName(string name)
    {
        var encounter = encounterList.Find(e => e.encounterName == name && !e.hasTriggered);
        if (encounter != null)
        {
            ExecuteEncounter(encounter);
        }
    }

    public void UpdateDepth(float depth)
    {
        currentDepth = depth;
    }

    private float WrapAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f)
            angle -= 360f;
        return angle;
    }
}