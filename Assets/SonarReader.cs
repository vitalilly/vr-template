using UnityEngine;

public class SonarDisplayDriver : MonoBehaviour
{
    [Header("Material Reference")]
    [SerializeField] private Renderer sonarScreenRenderer;
    // Alternatively, use Image if rendering on a UI Canvas:
    // [SerializeField] private UnityEngine.UI.Image sonarCanvasImage;

    [Header("2D Map Boundaries")]
    [Tooltip("The size of your 2D map in world units, used to normalize coordinates (0.0 to 1.0) for the shader.")]
    [SerializeField] private Vector2 mapWorldSize = new Vector2(100f, 100f);
    
    [Tooltip("The offset origin of your 2D map (e.g. bottom-left corner or center position).")]
    [SerializeField] private Vector2 mapWorldOrigin = Vector2.zero;

    [Header("Shader Property Names")]
    [SerializeField] private string positionPropertyName = "_SubmarinePos";
    [SerializeField] private string headingPropertyName = "_SubmarineHeading";
    [SerializeField] private string sweepSpeedPropertyName = "_SweepSpeed";

    private Material sonarMaterial;
    private int posPropertyID;
    private int headingPropertyID;

    private void Awake()
    {
        if (sonarScreenRenderer != null)
        {
            // Fetch material instance so changes don't overwrite source asset disk files
            sonarMaterial = sonarScreenRenderer.material;
        }

        // Cache property IDs for performance (faster than string lookups every frame)
        posPropertyID = Shader.PropertyToID(positionPropertyName);
        headingPropertyID = Shader.PropertyToID(headingPropertyName);
    }

    private void Update()
    {
        if (sonarMaterial == null) return;

        // 1. Get current 2D Proxy Position (X, Y)
        Vector2 currentPos = transform.position;

        // 2. Normalize position to [0.0, 1.0] UV space for texture-based sonar maps
        Vector2 normalizedPos = new Vector2(
            (currentPos.x - mapWorldOrigin.x) / mapWorldSize.x,
            (currentPos.y - mapWorldOrigin.y) / mapWorldSize.y
        );

        // 3. Get Submarine Heading Angle (Z-rotation in 2D space)
        float headingDegrees = transform.eulerAngles.z;
        float headingRadians = headingDegrees * Mathf.Deg2Rad;

        // 4. Pass coordinates to HLSL Shader
        // Option A: Raw World Position (Vector4)
        sonarMaterial.SetVector(posPropertyID, new Vector4(currentPos.x, currentPos.y, 0f, 0f));

        // Option B: Normalized UV Position (Uncomment if your shader uses 0..1 map UVs)
        // sonarMaterial.SetVector(posPropertyID, new Vector4(normalizedPos.x, normalizedPos.y, 0f, 0f));

        // Pass Heading Angle (in radians or degrees depending on your HLSL math)
        sonarMaterial.SetFloat(headingPropertyID, headingRadians);
    }

    private void OnDestroy()
    {
        // Clean up instantiated material to prevent memory leaks in the editor
        if (sonarMaterial != null)
        {
            Destroy(sonarMaterial);
        }
    }
}