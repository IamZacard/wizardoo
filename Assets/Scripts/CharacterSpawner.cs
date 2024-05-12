using UnityEngine;

public class CharacterSpawner : MonoBehaviour
{
    public GameObject spawnPoint; // Spawn point for the selected character

    private int characterIndex; // Index of the selected character prefab
    void Awake()
    {
        SpawnSelectedCharacter();
    }

    void SpawnSelectedCharacter()
    {
        // Check if a character prefab is selected
        if (CharacterManager.SelectedCharacterPrefab != null)
        {
            // Instantiate the selected character prefab at the spawn point
            Instantiate(CharacterManager.SelectedCharacterPrefab, spawnPoint.transform.position, Quaternion.identity);

            // Display the characterIndex value
            Debug.Log("Character Index: " + characterIndex);
        }
        else
        {
            Debug.LogWarning("No character prefab selected!");
        }
    }
}
