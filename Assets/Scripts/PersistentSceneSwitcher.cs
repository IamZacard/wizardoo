using UnityEngine;
using UnityEngine.SceneManagement;

public class PersistentSceneSwitcher : MonoBehaviour
{
    // This ensures that this object persists across scene loads
    private void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }

    // This method is triggered to check and load the next scene
    public void ProceedToNextScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int totalSceneCount = SceneManager.sceneCountInBuildSettings;

        // Check if there is a next scene
        if (currentSceneIndex < totalSceneCount - 1)
        {
            // If there is, load the next scene
            SceneManager.LoadScene(currentSceneIndex + 1);
        }
        else
        {
            // Do nothing if it's the last scene
            Debug.Log("No more scenes to load.");
        }
    }
}
