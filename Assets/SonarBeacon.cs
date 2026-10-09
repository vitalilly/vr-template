using UnityEngine;

public class SonarBeacon : MonoBehaviour
{
    [Header("Beacon Identity & Signal")]
    [SerializeField] private string beaconID = "NAV_BEACON_01";
    [SerializeField] private float signalRadius = 500f; // Detection range in meters
    [SerializeField] private float pulseInterval = 3.0f; // Seconds between acoustic pings
    [SerializeField] private AudioClip acousticPingSound; // Optional distinct sound clip for this beacon

    [Header("Visual Styling")]
    [SerializeField] private Color beaconColor = Color.green; // Display color on CRT

    private float pulseTimer;

    private void Update()
    {
        pulseTimer += Time.deltaTime;
        if (pulseTimer >= pulseInterval)
        {
            pulseTimer = 0f;
            // Pulse logic can trigger local visual ripple FX
        }
    }

    public float GetSignalRadius() => signalRadius;
    public string GetBeaconID() => beaconID;
    public Color GetBeaconColor() => beaconColor;
    public AudioClip GetPingSound() => acousticPingSound;
    // Inside SonarBeaconTracker.cs

}   