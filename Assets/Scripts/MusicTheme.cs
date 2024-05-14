using UnityEngine;

public class MusicTheme : MonoBehaviour
{
    private static MusicTheme instance;

    private void Awake()
    {
        // Check if an instance of MusicTheme already exists
        if (instance == null)
        {
            // If not, set the instance to this object
            instance = this;

            // Make this object persistent between scene changes
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // If an instance already exists and it's not this object, destroy this object
            if (instance != this)
            {
                Destroy(gameObject);
            }
        }
    }

    // Additional music-related methods can be added here
}
