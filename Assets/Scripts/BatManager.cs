using System.Collections;
using UnityEngine;

public class BatManager : MonoBehaviour
{
    public GameObject batPrefab; // Prefab of the bat
    public Transform[] spawnPoints; // Array of spawn points for bats
    public Transform[] targetPoints; // Array of target points for bats
    public float spawnInterval = 10f; // Interval between bat spawns

    private void Start()
    {
        // Check for null or empty arrays to prevent runtime errors
        if (batPrefab == null || spawnPoints.Length == 0 || targetPoints.Length == 0)
        {
            Debug.LogError("BatManager is not properly configured.");
            return;
        }

        // Start spawning bats
        StartCoroutine(SpawnBatsRoutine());
    }

    private IEnumerator SpawnBatsRoutine()
    {
        // Infinite loop to continuously spawn bats
        while (true)
        {
            // Wait for the specified interval before spawning the next bat
            yield return new WaitForSeconds(spawnInterval);

            // Spawn a bat at a random spawn point
            SpawnBat(Random.Range(0, spawnPoints.Length));
        }
    }

    private void SpawnBat(int spawnPointIndex)
    {
        // Instantiate a bat at the specified spawn point
        GameObject newBat = Instantiate(batPrefab, spawnPoints[spawnPointIndex].position, Quaternion.identity);

        // Check if the new bat has the Bat component
        Bat batComponent = newBat.GetComponent<Bat>();
        if (batComponent != null)
        {
            // Get a random target point for the bat to fly towards
            Transform randomTarget = targetPoints[Random.Range(0, targetPoints.Length)];

            // Set the target point for the bat
            batComponent.SetTarget(randomTarget.position);
        }
        else
        {
            Debug.LogError("Spawned bat does not have a Bat component.");
        }
    }
}
