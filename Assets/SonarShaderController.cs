using UnityEngine;

[ExecuteInEditMode]
public class SonarShaderController : MonoBehaviour
{
    [Header("Display Material Reference")]
    [SerializeField] private Material sonarMaterial;

    [Header("Sonar Configuration")]
    [SerializeField] private float sweepSpeedDegreesPerSec = 90f;
    [SerializeField] private float maxRange = 500f;
    [SerializeField] private float sweepWidthDegrees = 60f;

    [Header("CRT Shader Effects")]
    [Range(0f, 0.5f)] [SerializeField] private float screenCurvature = 0.15f;
    [SerializeField] private float scanlineCount = 150f;
    [SerializeField] private float scanlineSpeed = 2f;
    [Range(0f, 1f)] [SerializeField] private float scanlineIntensity = 0.35f;
    [Range(0f, 5f)] [SerializeField] private float vignettePower = 2f;
    [Range(1f, 5f)] [SerializeField] private float bloomGlowMultiplier = 2.5f;
    [SerializeField] private Color sonarColor = new Color(0f, 1f, 0.3f, 1f);

    [Header("Target Tracking")]
    [SerializeField] private Transform submarineTransform;
    [SerializeField] private Transform[] targetEntities;

    private float currentSweepAngle = 0f;
    private Vector4[] contactData = new Vector4[16];

    private void Start()
    {
        if (submarineTransform == null)
        {
            submarineTransform = transform;
        }

        // Ensure default fallback texture exists to prevent null material errors
        if (sonarMaterial != null && sonarMaterial.GetTexture("_MainTex") == null)
        {
            sonarMaterial.SetTexture("_MainTex", Texture2D.whiteTexture);
        }
    }

    private void Update()
    {
        if (sonarMaterial == null) return;

        // 1. Advance Sweep Angle
        currentSweepAngle = (currentSweepAngle + sweepSpeedDegreesPerSec * Time.deltaTime) % 360f;

        // 2. Transmit Core Sonar Variables
        sonarMaterial.SetFloat("_SweepAngle", currentSweepAngle);
        sonarMaterial.SetFloat("_MaxDistance", maxRange > 0f ? maxRange : 500f);
        sonarMaterial.SetFloat("_SweepWidth", sweepWidthDegrees);
        sonarMaterial.SetColor("_SonarColor", sonarColor);

        // 3. Transmit CRT Shader Properties
        sonarMaterial.SetFloat("_Curvature", screenCurvature);
        sonarMaterial.SetFloat("_ScanlineCount", scanlineCount);
        sonarMaterial.SetFloat("_ScanlineSpeed", scanlineSpeed);
        sonarMaterial.SetFloat("_ScanlineIntensity", scanlineIntensity);
        sonarMaterial.SetFloat("_VignettePower", vignettePower);
        sonarMaterial.SetFloat("_GlowMultiplier", bloomGlowMultiplier);

        // 4. Process & Transmit Target Contacts
        if (submarineTransform != null && targetEntities != null && targetEntities.Length > 0)
        {
            int count = Mathf.Min(targetEntities.Length, 16);
            for (int i = 0; i < count; i++)
            {
                if (targetEntities[i] == null) continue;

                Vector3 relativePos = targetEntities[i].position - submarineTransform.position;
                
                // Distance (X)
                float distance = relativePos.magnitude;

                // Bearing in degrees relative to world orientation (Y)
                float bearing = Mathf.Atan2(relativePos.y, relativePos.x) * Mathf.Rad2Deg;
                if (bearing < 0f) bearing += 360f;

                contactData[i] = new Vector4(distance, bearing, 1.0f, 0f);
            }

            sonarMaterial.SetVectorArray("_Contacts", contactData);
            sonarMaterial.SetInt("_ContactCount", count);
        }
    }
}