using UnityEngine;

using Unity.XR.CoreUtils;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationAnchor))]
public class SeatedTeleportAnchor : MonoBehaviour
{
    [Header("Seated Offset Settings")]
    [Tooltip("Target eye height above seat cushion surface (in meters).")]
    [SerializeField] private float seatedEyeHeight = 0.7f; 

    [Header("XR Origin Reference")]
    [SerializeField] private XROrigin xrOrigin;

    private UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationAnchor teleportAnchor;

    private void Awake()
    {
        teleportAnchor = GetComponent<UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationAnchor>();

        if (xrOrigin == null)
        {
            xrOrigin = FindObjectOfType<XROrigin>();
        }
    }

    private void OnEnable()
    {
        teleportAnchor.teleporting.AddListener(OnTeleporting);
    }

    private void OnDisable()
    {
        teleportAnchor.teleporting.RemoveListener(OnTeleporting);
    }

    private void OnTeleporting(UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportingEventArgs args)
    {
        if (xrOrigin == null) return;

        // ONLY handle vertical height offset here to prevent double-rotating
        float physicalHmdHeight = xrOrigin.Camera.transform.localPosition.y;
        float targetYOffset = seatedEyeHeight;// - physicalHmdHeight;

        Vector3 currentOffset = xrOrigin.CameraFloorOffsetObject.transform.localPosition;
        xrOrigin.CameraFloorOffsetObject.transform.localPosition = new Vector3(
            currentOffset.x,
            targetYOffset,
            currentOffset.z
        );
    }
}