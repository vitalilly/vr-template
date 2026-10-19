using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class XRSceneRebinder : MonoBehaviour
{
    [Header("Input Action Assets")]
    [SerializeField] private InputActionAsset xriInputActions;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[XR Rebind] Scene loaded: '{scene.name}'. Re-enabling XR input actions...");

        // 1. Force re-enable all XRI input action maps
        if (xriInputActions != null)
        {
            xriInputActions.Disable();
            xriInputActions.Enable();
        }

        // 2. Force re-center / update tracked pose drivers
        var poseDrivers = FindObjectsOfType<UnityEngine.SpatialTracking.TrackedPoseDriver>();
        foreach (var driver in poseDrivers)
        {
            driver.enabled = false;
            driver.enabled = true;
        }
    }
}