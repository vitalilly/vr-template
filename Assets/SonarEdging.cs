using UnityEngine;
using UnityEngine.UI;

public class SonarEdgeIndicator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera orthographicSonarCamera; // Camera rendering the tilemap/world to RenderTexture
    [SerializeField] private RectTransform crtMonitorCanvasRect; // RectTransform of the CRT display UI Panel
    [SerializeField] private RectTransform indicatorUI;         // RectTransform of the Edge Arrow icon

    [Header("Edge Padding")]
    [SerializeField] private float edgeMargin = 20f; // Distance from the border of the CRT screen UI

    private Image indicatorImage;

    private void Awake()
    {
        if (indicatorUI == null) indicatorUI = GetComponent<RectTransform>();
        if (indicatorUI != null) indicatorImage = indicatorUI.GetComponent<Image>();
    }

    public void UpdateIndicatorPosition(Vector3 beaconWorldPos, Transform subTransform)
    {
        if (orthographicSonarCamera == null || crtMonitorCanvasRect == null || indicatorUI == null) return;

        // 1. Convert Beacon world position to Sonar Camera Viewport Space (0.0 to 1.0)
        Vector3 viewPos = orthographicSonarCamera.WorldToViewportPoint(beaconWorldPos);

        // Check if the beacon is outside the orthographic camera's view bounds
        bool isOffScreen = viewPos.x < 0f || viewPos.x > 1f || viewPos.y < 0f || viewPos.y > 1f || viewPos.z < 0f;

        if (isOffScreen)
        {
            if (!indicatorUI.gameObject.activeSelf) indicatorUI.gameObject.SetActive(true);

            // 2. Map Viewport space (-0.5 to +0.5) to the CRT Canvas dimensions
            Vector2 viewportCentered = new Vector2(viewPos.x - 0.5f, viewPos.y - 0.5f);

            // Handle objects behind camera plane
            if (viewPos.z < 0f) viewportCentered *= -1f;

            // Get local dimensions of the CRT Monitor UI RectTransform
            Vector2 canvasSize = crtMonitorCanvasRect.rect.size;
            Vector2 bounds = new Vector2((canvasSize.x * 0.5f) - edgeMargin, (canvasSize.y * 0.5f) - edgeMargin);

            // 3. Ray-cast direction vector to clamp to the CRT UI rectangle edges
            float angle = Mathf.Atan2(viewportCentered.y, viewportCentered.x);
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            float slope = sin / cos;
            Vector2 clampedAnchoredPos;

            if (Mathf.Abs(slope) < bounds.y / bounds.x)
            {
                clampedAnchoredPos = new Vector2(Mathf.Sign(cos) * bounds.x, Mathf.Sign(cos) * bounds.x * slope);
            }
            else
            {
                clampedAnchoredPos = new Vector2(Mathf.Sign(sin) * bounds.y / slope, Mathf.Sign(sin) * bounds.y);
            }

            // 4. Update UI Transform position on Canvas and rotate arrow pointing outward
            indicatorUI.anchoredPosition = clampedAnchoredPos;
            indicatorUI.localRotation = Quaternion.Euler(0f, 0f, (angle * Mathf.Rad2Deg) - 90f);
        }
        else
        {
            // Beacon is visible on the main CRT map, hide the edge guide
            HideIndicator();
        }
    }

    public void HideIndicator()
    {
        if (indicatorUI != null && indicatorUI.gameObject.activeSelf)
        {
            indicatorUI.gameObject.SetActive(false);
        }
    }
}