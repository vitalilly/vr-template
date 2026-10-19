using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class ArcKinematicJoystick : MonoBehaviour
{
    [Header("Joystick Settings")]
    [SerializeField] private float maxTiltAngle = 45f;
    [SerializeField] private float smoothSpeed = 20f;

    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;
    private Quaternion initialLocalRotation;
    private Transform pivotParent;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        rb.isKinematic = true; // Lock physics forces to prevent flopping
        initialLocalRotation = transform.localRotation;
        pivotParent = transform.parent != null ? transform.parent : transform;

        grabInteractable.trackPosition = false;
        grabInteractable.trackRotation = false;
        grabInteractable.movementType = XRBaseInteractable.MovementType.Instantaneous;
    }

    private void Update()
    {
        float targetAngle = 0f;

        if (grabInteractable.isSelected)
        {
            Transform handTransform = grabInteractable.interactorsSelecting[0].transform;
            Vector3 localHandPos = pivotParent.InverseTransformPoint(handTransform.position);
            targetAngle = Mathf.Atan2(localHandPos.z, localHandPos.y) * Mathf.Rad2Deg;
            targetAngle = Mathf.Clamp(targetAngle, -maxTiltAngle, maxTiltAngle);
        }

        // Smoothly rotate kinematic transform along X axis
        Quaternion targetRotation = initialLocalRotation * Quaternion.Euler(targetAngle, 0f, 0f);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * smoothSpeed);
    }

    // Public getter for Submarine movement scripts
    public float NormalizedPitch => (transform.localEulerAngles.x > 180f ? transform.localEulerAngles.x - 360f : transform.localEulerAngles.x) / maxTiltAngle;
}