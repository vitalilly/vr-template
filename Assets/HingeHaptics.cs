using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(HingeJoint))]
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public class HingeHaptics : MonoBehaviour
{
    [Header("Haptic Settings")]
    [Range(0f, 1f)] [SerializeField] private float limitAmplitude = 0.8f;
    [SerializeField] private float limitDuration = 0.15f;

    [Header("Center Notch")]
    [SerializeField] private bool enableCenterNotch = true;
    [Range(0f, 1f)] [SerializeField] private float notchAmplitude = 0.3f;
    [SerializeField] private float notchDuration = 0.05f;

    private HingeJoint hinge;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;

    private bool hitMinLimit;
    private bool hitMaxLimit;
    private float previousAngle;

    private void Awake()
    {
        hinge = GetComponent<HingeJoint>();
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
    }

    private void Update()
    {
        if (grabInteractable == null || !grabInteractable.isSelected)
        {
            hitMinLimit = false;
            hitMaxLimit = false;
            return;
        }

        float currentAngle = hinge.angle;
        JointLimits limits = hinge.limits;
        float threshold = 2.0f;

        // Min Limit
        if (hinge.useLimits && currentAngle <= (limits.min + threshold))
        {
            if (!hitMinLimit)
            {
                TriggerHaptics(limitAmplitude, limitDuration);
                hitMinLimit = true;
            }
        }
        else hitMinLimit = false;

        // Max Limit
        if (hinge.useLimits && currentAngle >= (limits.max - threshold))
        {
            if (!hitMaxLimit)
            {
                TriggerHaptics(limitAmplitude, limitDuration);
                hitMaxLimit = true;
            }
        }
        else hitMaxLimit = false;

        // Center Notch
        if (enableCenterNotch)
        {
            if ((previousAngle < 0f && currentAngle >= 0f) || (previousAngle > 0f && currentAngle <= 0f))
            {
                TriggerHaptics(notchAmplitude, notchDuration);
            }
        }

        previousAngle = currentAngle;
    }

    private void TriggerHaptics(float amplitude, float duration)
    {
        foreach (var interactor in grabInteractable.interactorsSelecting)
        {
            if (interactor is UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor controllerInteractor)
            {
                controllerInteractor.SendHapticImpulse(amplitude, duration);
            }
        }
    }
}