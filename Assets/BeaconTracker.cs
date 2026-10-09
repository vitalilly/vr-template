using System.Collections.Generic;
using UnityEngine;

public class SonarBeaconTracker : MonoBehaviour
{
    [Header("Tracking Settings")]
    [SerializeField] private Transform submarineTransform;
    [SerializeField] private AudioSource sonarAudioSource;
    [SerializeField] private float scanRadius = 800f;
    [SerializeField] private float pingInterval = 2.5f;

    [Header("Pitch & Volume Scaling (Option A)")]
    [SerializeField] private float minPitch = 0.8f;
    [SerializeField] private float maxPitch = 1.6f;
    [SerializeField] private float minDistance = 20f;
    [SerializeField] private float maxDistance = 500f;

    [Header("UI Reference (Option B)")]
    [SerializeField] private SonarEdgeIndicator edgeIndicator;

    [Header("Shader Reference (CRT Blips)")]
    [SerializeField] private SonarTerrain sonarTerrain;

    private List<SonarBeacon> activeBeacons = new List<SonarBeacon>();
    private Vector4[] contactBuffer = new Vector4[16];
    private float pingTimer;

    private void Start()
    {
        // Cache all active beacons in the scene
        activeBeacons.AddRange(FindObjectsByType<SonarBeacon>(FindObjectsSortMode.None));
    }

    private void Update()
    {
        if (submarineTransform == null) return;

        SonarBeacon closestBeacon = null;
        float closestDistance = float.MaxValue;
        int contactIndex = 0;

        // Reset contact buffer memory
        System.Array.Clear(contactBuffer, 0, contactBuffer.Length);

        // Scan scene beacons
        foreach (var beacon in activeBeacons)
        {
            if (beacon == null || !beacon.gameObject.activeInHierarchy) continue;

            Vector2 relativePos = beacon.transform.position - submarineTransform.position;
            float distance = relativePos.magnitude;

            if (distance <= beacon.GetSignalRadius() && distance <= scanRadius)
            {
                // Track nearest beacon for directional audio and edge guide UI
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestBeacon = beacon;
                }

                // Pack data into contact array for CRT Shader pass (Max 16 blips)
                if (contactIndex < 16)
                {
                    // Calculate World Bearing Angle (0° = North/Up, 90° = East/Right)
                    float bearing = Mathf.Atan2(relativePos.x, relativePos.y) * Mathf.Rad2Deg;
                    if (bearing < 0f) bearing += 360f;

                    // X = World Distance, Y = World Bearing Angle (Deg), Z = Active Flag (1.0)
                    contactBuffer[contactIndex] = new Vector4(distance, bearing, 1f, 0f);
                    contactIndex++;
                }
            }
        }

        // --- 1. SHADER BRIDGE (Render Blips on CRT Display) ---
        if (sonarTerrain != null)
        {
            sonarTerrain.UpdateShaderContacts(contactBuffer, contactIndex);
        }

        // --- 2. SINGLE CLOSEST BEACON GUIDANCE (Audio & UI) ---
        if (closestBeacon != null)
        {
            Vector2 relativePos = closestBeacon.transform.position - submarineTransform.position;

            // Option A: Dynamic Audio Ping
            pingTimer += Time.deltaTime;
            if (pingTimer >= pingInterval)
            {
                pingTimer = 0f;
                PlaySpatializedPing(relativePos, closestDistance, closestBeacon.GetPingSound());
            }

            // Option B: Canvas Edge Clamped Arrow
            if (edgeIndicator != null)
            {
                edgeIndicator.UpdateIndicatorPosition(closestBeacon.transform.position, submarineTransform);
            }
        }
        else
        {
            if (edgeIndicator != null)
            {
                edgeIndicator.HideIndicator();
            }
        }
    }

    private void PlaySpatializedPing(Vector2 relativePos, float distance, AudioClip clip)
    {
        if (sonarAudioSource == null || clip == null) return;

        // 1. Stereo Panning based on submarine local orientation (-1.0 = Left, +1.0 = Right)
        float dotRight = Vector2.Dot(submarineTransform.right, relativePos.normalized);
        sonarAudioSource.panStereo = Mathf.Clamp(dotRight, -1f, 1f);

        // 2. Pitch Scaling based on Distance (Closer = Higher Pitch)
        float distNormalized = Mathf.InverseLerp(maxDistance, minDistance, distance);
        sonarAudioSource.pitch = Mathf.Lerp(minPitch, maxPitch, distNormalized);

        // 3. Trigger Ping
        sonarAudioSource.PlayOneShot(clip);
    }

    /// <summary>
    /// Call this when spawning new dynamic beacons at runtime.
    /// </summary>
    public void RegisterBeacon(SonarBeacon newBeacon)
    {
        if (newBeacon != null && !activeBeacons.Contains(newBeacon))
        {
            activeBeacons.Add(newBeacon);
        }
    }
}