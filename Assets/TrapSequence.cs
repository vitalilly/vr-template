using System.Collections.Generic;
using UnityEngine;

public class SonarMineManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SonarTerrain sonarTerrain;
    [SerializeField] private Transform submarine2DProxy;
    [SerializeField] private GameObject minePrefab;

    [Header("Swarm Parameters")]
    [SerializeField] private int totalMinesInSector = 40;
    [SerializeField] private float spawnRadius = 350f;

    private List<SonarMine> activeMines = new List<SonarMine>();
    private Vector4[] contactBuffer = new Vector4[16];

    private void Start()
    {
        SpawnSwarm();
    }

    private void SpawnSwarm()
    {
        for (int i = 0; i < totalMinesInSector; i++)
        {
            // Spawn mines randomly around the starting area in the 2D XY plane
            Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPos = submarine2DProxy != null ? submarine2DProxy.position + (Vector3)randomOffset : (Vector3)randomOffset;

            GameObject mineObj = Instantiate(minePrefab, spawnPos, Quaternion.identity, transform);
            SonarMine mine = mineObj.GetComponent<SonarMine>();
            
            if (mine != null)
            {
                mine.Initialize(submarine2DProxy);
                activeMines.Add(mine);
            }
        }
    }

    private void LateUpdate()
    {
        if (sonarTerrain == null || submarine2DProxy == null || activeMines.Count == 0) return;

        // Clean up null/destroyed mines from the list
        activeMines.RemoveAll(m => m == null);

        // Sort active mines by distance to the submarine proxy (closest first)
        activeMines.Sort((a, b) => 
        {
            float distA = Vector3.SqrMagnitude(a.transform.position - submarine2DProxy.position);
            float distB = Vector3.SqrMagnitude(b.transform.position - submarine2DProxy.position);
            return distA.CompareTo(distB);
        });

        int contactCount = 0;

        // Process up to the 16 closest mines for the shader buffer
        for (int i = 0; i < activeMines.Count && contactCount < 16; i++)
        {
            SonarMine mine = activeMines[i];
            Vector3 beaconWorldPos = mine.transform.position;
            Vector3 directionToBeacon = beaconWorldPos - submarine2DProxy.position;
            float distance = directionToBeacon.magnitude;

            // Get local position relative to the submarine in 2D XY space
            Vector3 localPos = submarine2DProxy.InverseTransformPoint(beaconWorldPos);

            // Apply your display scale factor (matching what you used previously, e.g., 200 or 1000)
            float displayScaleFactor = 200f; 
            float scaledDistance = distance * displayScaleFactor;
            Vector3 scaledLocalPos = localPos * displayScaleFactor;

            float signalNormalized = Mathf.Clamp01(1f - (distance / 400f));

            // Pack data into Vector4: x = local X, y = local Y, z = scaled distance, w = signal strength
            contactBuffer[contactCount] = new Vector4(scaledLocalPos.x, scaledLocalPos.y, scaledDistance, signalNormalized);
            
            contactCount++;
        }

        // Send the filtered 16 closest contacts to the SonarTerrain display
        sonarTerrain.UpdateShaderContacts(contactBuffer, contactCount);
    }
}