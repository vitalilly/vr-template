using UnityEngine;

[RequireComponent(typeof(Camera))]
public class SonarMapUpdater : MonoBehaviour
{
    [Header("Refresh Settings")]
    [Tooltip("Target render frames per second for the sonar map update.")]
    [Range(0.5f, 30f)]
    public float targetFPS = 5.0f; // 5 FPS is great for tactical screens

    [Header("Position Tracking (Optional)")]
    [Tooltip("Drag your Submarine Transform here if the map camera follows the player.")]
    public Transform submarineTransform;
    [Tooltip("Only re-render if the submarine moves more than this distance (in world units).")]
    public float minimumMoveDistance = 0.5f;

    private Camera mapCamera;
    private float timer;
    private Vector3 lastRenderPosition;

    void Awake()
    {
        mapCamera = GetComponent<Camera>();
        
        // Ensure camera does NOT render automatically every frame
        mapCamera.enabled = false;
    }

    void Start()
    {
        // Initial render on scene start
        RenderMap();
    }

    void Update()
    {
        timer += Time.deltaTime;

        float interval = 1.0f / targetFPS;

        if (timer >= interval)
        {
            timer = 0f;

            // Optional Optimization: Check if submarine moved enough to justify a re-render
            if (submarineTransform != null)
            {
                // Follow submarine position overhead
                Vector3 newPos = submarineTransform.position;
                newPos.z = transform.position.z; // Preserve orthographic Z depth
                transform.position = newPos;

               // if (Vector3.Distance(transform.position, lastRenderPosition) >= minimumMoveDistance)
                {
                    RenderMap();
                }
            }
            else
            {
                // Fixed overhead map update at target FPS
                RenderMap();
            }
        }
    }

    public void RenderMap()
    {
        if (mapCamera != null)
        {
            mapCamera.Render();
            lastRenderPosition = transform.position;
        }
    }
}