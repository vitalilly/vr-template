using UnityEngine;
using UnityEngine.UI;

public class SonarTerrain : MonoBehaviour
{
    [Header("Target Display Reference")]
    [SerializeField] private Renderer sonarMeshRenderer;
    [SerializeField] private RawImage sonarUIRawImage;

    [Header("Tracking Targets")]
    [SerializeField] private Transform submarine2DProxy;

    [Header("Sonar Visual Controls")]
    [ColorUsage(true, true)]
    [SerializeField] private Color sonarColor = new Color(0f, 1f, 0.3f, 1f);
    [Tooltip("Arc width of the illuminated trail in degrees (e.g. 60).")]
    [Range(10f, 180f)]
    [SerializeField] private float sweepWidthDegrees = 60f;
    [SerializeField] private float sweepSpeedDegreesPerSec = 180f;

    [Header("Sonar & Map Dimensions")]
    [SerializeField] private Texture terrainMapTexture;
    [Tooltip("World size of the full terrain map texture (e.g. 1000 x 1000). Must NOT be 0!")]
    [SerializeField] private Vector2 mapSizeWorld = new Vector2(1000f, 1000f);
    [Tooltip("Maximum distance viewable on screen in world units. Must NOT be 0!")]
    [SerializeField] private float maxDistance = 1000f;

    [Header("CRT Shader Settings")]
    [Range(0f, 0.5f)] [SerializeField] private float curvature = 0.15f;
    [Range(50f, 300f)] [SerializeField] private float scanlineCount = 150f;
    [Range(0f, 1f)] [SerializeField] private float scanlineIntensity = 0.35f;
    [Range(1f, 5f)] [SerializeField] private float glowMultiplier = 2.5f;

    private Material sonarMaterial;
    private float currentSweepAngle;

    // Contact Array Storage for Shader
    private Vector4[] contactsDataArray = new Vector4[16];
    [SerializeField] private SonarBeacon[] activeBeacons;
    private Vector4[] contactBuffer = new Vector4[16];
    private int activeContactCount = 0;

    // Property IDs
    private static readonly int SonarColorID = Shader.PropertyToID("_SonarColor");
    private static readonly int SweepAngleID = Shader.PropertyToID("_SweepAngle");
    private static readonly int SweepWidthID = Shader.PropertyToID("_SweepWidth");
    private static readonly int SubmarinePosID = Shader.PropertyToID("_SubmarinePos");
    private static readonly int SubmarineHeadingID = Shader.PropertyToID("_SubmarineHeading");
    private static readonly int TerrainMapID = Shader.PropertyToID("_TerrainMap");
    private static readonly int MapSizeWorldID = Shader.PropertyToID("_MapSizeWorld");
    private static readonly int MaxDistanceID = Shader.PropertyToID("_MaxDistance");
    private static readonly int CurvatureID = Shader.PropertyToID("_Curvature");
    private static readonly int ScanlineCountID = Shader.PropertyToID("_ScanlineCount");
    private static readonly int ScanlineIntensityID = Shader.PropertyToID("_ScanlineIntensity");
    private static readonly int GlowMultiplierID = Shader.PropertyToID("_GlowMultiplier");
    private static readonly int ContactsID = Shader.PropertyToID("_Contacts");
    private static readonly int ContactCountID = Shader.PropertyToID("_ContactCount");

    private void Awake()
    {
        if (sonarMeshRenderer != null)
        {
            sonarMaterial = sonarMeshRenderer.material;
        }
        else if (sonarUIRawImage != null)
        {
            sonarMaterial = new Material(sonarUIRawImage.material);
            sonarUIRawImage.material = sonarMaterial;
        }

        if (sonarMaterial == null)
        {
            Debug.LogError("[SonarTerrain] No MeshRenderer or RawImage assigned!", this);
            return;
        }

        if (terrainMapTexture != null)
        {
            sonarMaterial.SetTexture(TerrainMapID, terrainMapTexture);
        }

        UpdateMaterialProperties();
    }

    private void LateUpdate()
    {
        if (sonarMaterial == null) return;

        // Advance sweep rotation
        currentSweepAngle += sweepSpeedDegreesPerSec * Time.deltaTime;
        currentSweepAngle %= 360f;

        // Track submarine 2D position and heading
        if (submarine2DProxy != null)
        {
            Vector3 pos = submarine2DProxy.position;
            sonarMaterial.SetVector(SubmarinePosID, new Vector4(pos.x, pos.y, 0f, 0f));

            float headingRad = submarine2DProxy.eulerAngles.z * Mathf.Deg2Rad;
            sonarMaterial.SetFloat(SubmarineHeadingID, headingRad);
        }

        // Process & Transmit Target Contacts
        int contactCount = 0;

        for (int i = 0; i < activeBeacons.Length; i++)
        {
            if (activeBeacons[i] == null) continue;

            // Process distance, update edge indicators, and retrieve shader data
            if (activeBeacons[i].ProcessBeacon(submarine2DProxy, out Vector4 contactData))
            {
                if (contactCount < 16)
                {
                    contactBuffer[contactCount] = contactData;
                    contactCount++;
                }
            }
        }
        UpdateShaderContacts(contactBuffer, contactCount);

        UpdateMaterialProperties();
    }

    private void UpdateMaterialProperties()
    {
        // Sonar & Sweep Controls
        sonarMaterial.SetColor(SonarColorID, sonarColor);
        sonarMaterial.SetFloat(SweepAngleID, currentSweepAngle);
        sonarMaterial.SetFloat(SweepWidthID, sweepWidthDegrees);

        // Dimensions & Scaling
        sonarMaterial.SetVector(MapSizeWorldID, new Vector4(mapSizeWorld.x, mapSizeWorld.y, 0f, 0f));
        sonarMaterial.SetFloat(MaxDistanceID, maxDistance > 0f ? maxDistance : 1000f);

        // CRT Post-Effects
        sonarMaterial.SetFloat(CurvatureID, curvature);
        sonarMaterial.SetFloat(ScanlineCountID, scanlineCount);
        sonarMaterial.SetFloat(ScanlineIntensityID, scanlineIntensity);
        sonarMaterial.SetFloat(GlowMultiplierID, glowMultiplier);
    }

    /// <summary>
    /// Call this from SonarBeaconTracker to pass active target blips directly into the shader.
    /// </summary>
    public void UpdateShaderContacts(Vector4[] contacts, int count)
    {
        activeContactCount = Mathf.Min(count, 16);
        for (int i = 0; i < activeContactCount; i++)
        {
            contactsDataArray[i] = contacts[i];
        }
        //push data to the shader
        if (sonarMaterial != null)
        {
            sonarMaterial.SetVectorArray("_Contacts", contactsDataArray);
            sonarMaterial.SetInt("_ContactCount", activeContactCount);
        }
    }

    public void SetMaxDistance(float newRange) => maxDistance = Mathf.Max(10f, newRange);
    public void SetSweepWidth(float newWidth) => sweepWidthDegrees = Mathf.Clamp(newWidth, 10f, 180f);
    public void SetSonarColor(Color newColor) => sonarColor = newColor;

    private void OnDestroy()
    {
        if (sonarMaterial != null && sonarUIRawImage != null)
        {
            Destroy(sonarMaterial);
        }
    }
}