using UnityEngine;

public class LevelCompleteSpawner : MonoBehaviour
{
    public GameObject objectToSpawn; // The object to be spawned
    public Transform spawnPoint; // The point where the object will be spawned
    public float objectLifetime = 5f; // Lifetime of the spawned object in seconds

    private Game gameRules; // Reference to the Game script
    private bool hasSpawned = false; // Flag to ensure the object spawns only once
    private GameObject spawnedObject; // Reference to the spawned object

    private void Start()
    {
        // Find the Game object with the Game script attached
        GameObject gameRulesObject = GameObject.FindGameObjectWithTag("GameRules");

        if (gameRulesObject != null)
        {
            gameRules = gameRulesObject.GetComponent<Game>();
        }
        else
        {
            Debug.LogWarning("No GameObject with tag 'GameRules' found!");
        }
    }

    private void Update()
    {
        // Check if the level is complete and the object has not been spawned yet
        if (gameRules != null && gameRules.levelComplete && !hasSpawned)
        {
            // Spawn the object
            SpawnObject();

            // Set the flag to true to prevent further spawns
            hasSpawned = true;
        }

        if (Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            DestroySpawnedObject();
        }
    }

    private void SpawnObject()
    {
        if (objectToSpawn != null && spawnPoint != null)
        {
            spawnedObject = Instantiate(objectToSpawn, spawnPoint.position, spawnPoint.rotation);
            ScreenShake.Instance.TriggerShake(1f, 3f);
            Debug.Log("Object spawned!");

            // Destroy the object after the specified lifetime
            Destroy(spawnedObject, objectLifetime);
        }
        else
        {
            Debug.LogWarning("Object to spawn or spawn point is not set!");
        }
    }

    private void DestroySpawnedObject()
    {
        if (spawnedObject != null)
        {
            Destroy(spawnedObject);
            Debug.Log("Spawned object destroyed!");
        }
        else
        {
            Debug.LogWarning("No spawned object to destroy!");
        }
    }
}
