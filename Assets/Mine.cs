using UnityEngine;

public class SonarMine : MonoBehaviour
{
    [Header("Mine Settings")]
    [SerializeField] private float detectionRadius = 400f;
    [SerializeField] private float driftSpeed = 0.8f;

    [Header("Audio")]
    [SerializeField] private AudioClip pingSound;
    [SerializeField] private float pulseInterval = 2.0f;
    private float pulseTimer;

    private Transform targetSubmarine;
    private bool isInitialized = false;

    public void Initialize(Transform submarine)
    {
        targetSubmarine = submarine;
        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized || targetSubmarine == null) return;

        // Slowly drift or home toward the submarine in the 2D XY plane
        Vector3 drift = new Vector3(Mathf.Sin(Random.Range(1000,-1000)),Mathf.Cos(Random.Range(1000,-1000)),0.0f);
        transform.position = transform.position + drift;
        Vector3 direction = (targetSubmarine.position - transform.position);
        float distance = direction.magnitude;

        if (distance <= detectionRadius)
        {
            // Drift toward the sub to crowd the tactical space around the lure approach
            transform.position += direction.normalized * driftSpeed * Time.deltaTime;
        }

        // Handle acoustic pulse timing
        pulseTimer += Time.deltaTime;
        if (pulseTimer >= pulseInterval)
        {
            pulseTimer = 0f;
            
            if (pingSound != null)
            {
                AudioSource.PlayClipAtPoint(pingSound, transform.position, 0.4f);
            }
        }
    }
}