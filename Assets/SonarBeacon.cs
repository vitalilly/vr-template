using UnityEngine;

public class SonarBeacon : MonoBehaviour
{
    [Header("Beacon Identity & Signal")]
    [SerializeField] private string beaconID = "NAV_BEACON_01";
    [SerializeField] private float signalRadius = 500f; // Detection range in meters
    [SerializeField] private float pulseInterval = 3.0f; // Seconds between acoustic pings
    [SerializeField] private AudioClip acousticPingSound; // Optional distinct sound clip for this beacon
    [SerializeField] private float scaleFactor = 200f;

    [Header("Visual Styling")]
    [SerializeField] private Color beaconColor = Color.green; // Display color on CRT

    [Header("HUD & Edge Tracking")]
    [SerializeField] private SonarEdgeIndicator edgeIndicator; // Reference to UI edge arrow indicator for this beacon

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

    /// <summary>
    /// Processes beacon tracking logic relative to the submarine.
    /// Updates edge indicator position and returns Vector4 shader data if within signal range.
    /// </summary>

public bool ProcessBeacon(Transform subTransform, out Vector4 shaderContactData)
{
    shaderContactData = Vector4.zero;

    if (subTransform == null)
    {
        if (edgeIndicator != null) edgeIndicator.HideIndicator();
        return false;
    }

    Vector3 beaconWorldPos = transform.position;
    Vector3 directionToBeacon = beaconWorldPos - subTransform.position;
    float distance = directionToBeacon.magnitude;

    if (distance <= signalRadius)
    {

        // Get local position relative to submarine orientation
        Vector3 localPos = subTransform.InverseTransformPoint(beaconWorldPos);
        Vector3 scaledPos = localPos * scaleFactor;
        float scaledDist = distance * scaleFactor;
        // Debug.Log($"Raw dist {distance}: Scaled dist {scaledDist}: pos {scaledPos}");
        // Pack X and Z into vector (normalized later in shader or here)
        // x = local x, y = local y, z = raw distance, w = signal strength
        float signalNormalized = Mathf.Clamp01(1f - (distance / signalRadius));
        shaderContactData = new Vector4(scaledPos.x, scaledPos.y, scaledDist, signalNormalized);
        return true;
    }

    return false;
}
    private void OnDisable()
    {
        if (edgeIndicator != null)
        {
            edgeIndicator.HideIndicator();
        }
    }

    public float GetSignalRadius() => signalRadius;
    public string GetBeaconID() => beaconID;
    public Color GetBeaconColor() => beaconColor;
    public AudioClip GetPingSound() => acousticPingSound;
}