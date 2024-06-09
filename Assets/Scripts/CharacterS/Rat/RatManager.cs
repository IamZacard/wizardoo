using System.Collections;
using UnityEngine;

public class RatManager : MonoBehaviour
{
    public GameObject ratPrefab; // Prefab of the rat
    public Transform[] spawnPoints; // Array of spawn points for rats
    public Transform[] targetPoints; // Array of target points for rats
    public float spawnInterval = 10f; // Interval between rat spawns

    private void Start()
    {
        // Check for null or empty arrays to prevent runtime errors
        if (ratPrefab == null || spawnPoints.Length == 0 || targetPoints.Length == 0)
        {
            Debug.LogError("RatManager is not properly configured.");
            return;
        }

        // Start spawning rats
        StartCoroutine(SpawnRatsRoutine());
    }

    private IEnumerator SpawnRatsRoutine()
    {
        // Infinite loop to continuously spawn rats
        while (true)
        {
            // Wait for the specified interval before spawning the next rat
            yield return new WaitForSeconds(spawnInterval);

            // Spawn a rat at a random spawn point
            SpawnRat(Random.Range(0, spawnPoints.Length));
        }
    }

    private void SpawnRat(int spawnPointIndex)
    {
        // Instantiate a rat at the specified spawn point
        GameObject newRat = Instantiate(ratPrefab, spawnPoints[spawnPointIndex].position, Quaternion.identity);

        // Check if the new rat has the Rat component
        Rat ratComponent = newRat.GetComponent<Rat>();
        if (ratComponent != null)
        {
            // Get a random target point for the rat to move towards
            Transform randomTarget = targetPoints[Random.Range(0, targetPoints.Length)];

            // Set the target point for the rat
            ratComponent.SetTarget(randomTarget.position);
        }
        else
        {
            Debug.LogError("Spawned rat does not have a Rat component.");
        }
    }
}
