using UnityEngine;

public class SonarCameraManager : MonoBehaviour
{
    public static SonarCameraManager Instance { get; private set; }

    [Header("Sonar Orthographic Camera")]
    [SerializeField] private Camera sonarOrthographicCamera;
    [SerializeField] private float minDepthSize = 10f;  // Zoomed-in trench view
    [SerializeField] private float maxDepthSize = 1200f; // Open water view

    [Header("Sonar CRT Material")]
    [SerializeField] private Material sonarShaderMaterial;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Updates orthographic camera size and shader uniforms based on depth (0.0 to 1.0)
    /// </summary>
    public void UpdateCameraDepthView(float normalizedDepth)
    {
        // 1. Adjust Orthographic Camera Zoom
        if (sonarOrthographicCamera != null)
        {
            sonarOrthographicCamera.orthographicSize = Mathf.Lerp(maxDepthSize, minDepthSize, normalizedDepth);
        }

        // 2. Drive Shader Depth Uniforms
        if (sonarShaderMaterial != null)
        {
            // Maps normalized depth (0..1) directly to shader parameters
            float currentMeters = Mathf.Lerp(10f, 1200f, normalizedDepth);
            sonarShaderMaterial.SetFloat("_SubDepth", currentMeters);
        }
    }
}