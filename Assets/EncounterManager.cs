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
    public AudioClip dialogueAudioClip;
    public Vector2 sonarContactPosition; // Relative position for dynamic CRT blip
    public float headingLenience; //
    [Tooltip("Maximum angle the submarine can be turned away from waypoint")]
    public float expectedDepth; 
    [Tooltip("Maximum depth at which waypoint triggers")]
    
    [Header("Unity Events (Hook up Radio/CRT/Haptics)")]
    public UnityEvent onEncounterTriggered;

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
        // Fallback: If no intro is playing (e.g. testing directly in editor)
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
        isEncountersActive = true;
        Debug.Log("EncounterManager Active: Starting deep-sea encounters.");
        // Begin mine spawning, creature pathing, or hazard logic here
    }

    private void Update()
    {
        if (submarineProxy == null) return;
        if (!isEncountersActive) return;

        // 1. Calculate total distance traveled by submarine
        float stepDistance = Vector3.Distance(submarineProxy.position, lastPosition);
        float currentHeadingDeg = submarineProxy.eulerAngles.z;
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
                    if (currentDepth <= encounter.expectedDepth)
                    {
                        shouldTrigger = true;
                    }  
                    break;

                case TriggerType.Heading:
                    if (currentHeadingDeg > encounter.headingLenience)
                    {
                        shouldTrigger = true;
                    }  
                    break;
                
                case TriggerType.Vector:
                    if (encounter.wayPointLocation != null)
                    {
                        float distToWaypoint = Vector3.Distance(submarineProxy.position, encounter.wayPointLocation.position);
                        if (distToWaypoint > encounter.triggerThreshold)
                        {
                            shouldTrigger=true;
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

        // 1. Play dialogue clip through radio if assigned
        if (radioAudioSource != null && encounter.dialogueAudioClip != null)
        {
            // You can add a PlaySpecificClip method to RadioDialogueController or call directly
            PlayRadioClip(encounter.dialogueAudioClip);
        }

        // 2. Trigger custom UnityEvents (e.g. sound alarms, flicker cockpit lights)
        encounter.onEncounterTriggered?.Invoke();
    }

    // Public method to trigger encounters manually via levers/triggers
    public void TriggerEncounterByName(string name)
    {
        var encounter = encounterList.Find(e => e.encounterName == name && !e.hasTriggered);
        if (encounter != null)
        {
            ExecuteEncounter(encounter);
        }
    }

    //Method to play audio clip for encounters
    private void PlayRadioClip(AudioClip clip)
    {
        if (radioAudioSource != null && clip != null)
        {
            // radioAudioSource.Stop();
            radioAudioSource.PlayOneShot(clip);
        }
    }

    public void UpdateDepth(float depth)
    {
        currentDepth = depth;
    }

}