using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager instance;
    public AudioClip characterSelectionMusic;
    public AudioClip levelMusic;
    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Level1") // Replace with your level scene name
        {
            audioSource.clip = levelMusic;
            audioSource.Play();
        }
        else if (scene.name == "SelectionMenu") // Replace with your character selection scene name
        {
            audioSource.clip = characterSelectionMusic;
            audioSource.Play();
        }
    }
}
