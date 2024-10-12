using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    public enum SoundType
    {
        CharacterPick,
        ButtonClick,
        ErrorSound,
        FootStepSound,
        FlagSpell,
        GaleBlast,
        GalePickUp,
        VioletTeleport,
        MysticInvincible,
        SageReveal,
        ShuffProc,
        ShuffExplotion,
        LevelComplete,
        LoseStepOnTrap,
        BoardRevealSound,
        PlateRevealSound,
        Success,
        PortalSound,
        TalkSound,
        CatMeowSound,
        MagicBlockReleaseSound,
        DialogStart,
        LevelStartSound,
        EnterWordSound,
        // Add more sound types as needed
    }

    [System.Serializable]
    public class SoundEntry
    {
        public SoundType soundType;
        public AudioClip audioClip;
    }

    public List<SoundEntry> soundEntries;

    private static AudioManager instance;
    private AudioSource audioSource;

    public Slider musicVolumeSlider;
    public Slider effectsVolumeSlider;

    // Ensure these are assigned in the inspector.
    public AudioSource musicPlayer;
    public AudioSource effectsPlayer;

    public TextMeshProUGUI musicPlayerNumber;
    public TextMeshProUGUI effectsPlayerNumber;

    private static float volumeVar = 100f;

    public GameObject OptionsPanel;
    public GameObject HTPPanel;

    [Range(0f, 1f)]
    public float volume = 1f; // Default volume value is 1 (maximum)

    public static AudioManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<AudioManager>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("AudioManager");
                    instance = obj.AddComponent<AudioManager>();
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        effectsVolumeSlider.onValueChanged.AddListener(SetEffectsVolume);

        musicPlayer.volume = 0.5f;
        effectsPlayer.volume = 0.5f;
    }

    private void Update()
    {
        musicPlayerNumber.text = (musicPlayer.volume * 100f).ToString("F0");
        effectsPlayerNumber.text = (effectsPlayer.volume * 100f).ToString("F0");

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OptionsPanel.SetActive(false);
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            OptionsPanel.SetActive(!OptionsPanel.activeSelf);
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            HTPPanel.SetActive(!HTPPanel.activeSelf);
        }
    }

    private void SetMusicVolume(float volume)
    {
        musicPlayer.volume = volume / 100f; // Convert 0-100 range to 0-1 for AudioSource
        musicPlayerNumber.text = volume.ToString("F0") + "%"; // Show the actual 0-100 value for the UI
    }

    private void SetEffectsVolume(float volume)
    {
        effectsPlayer.volume = volume / 100f; // Convert 0-100 range to 0-1 for AudioSource
        effectsPlayerNumber.text = volume.ToString("F0") + "%"; // Show the actual 0-100 value for the UI
    }

    /*private void SetMusicVolume(float volume)
    {
        musicPlayer.volume = volume;
    }

    private void SetEffectsVolume(float volume)
    {
        effectsPlayer.volume = volume;
    }*/

    public void ApplyMusicSettings()
    {
        musicPlayer.volume = volumeVar / 100f; // Adjust based on volumeVar if needed
    }

    public void ApplyEffectsSettings()
    {
        effectsPlayer.volume = volumeVar / 100f; // Adjust based on volumeVar if needed
    }

    public void PlaySound(SoundType soundType, float pitch)
    {
        AudioClip clip = GetAudioClip(soundType);
        if (clip != null)
        {
            audioSource.pitch = pitch;
            audioSource.PlayOneShot(clip);
        }
    }

    public void PlaySound(AudioClip clip, float pitch)
    {
        if (clip != null)
        {
            audioSource.pitch = pitch;
            audioSource.PlayOneShot(clip);
        }
    }

    private AudioClip GetAudioClip(SoundType soundType)
    {
        foreach (var entry in soundEntries)
        {
            if (entry.soundType == soundType)
                return entry.audioClip;
        }
        Debug.LogError("Sound entry not found for " + soundType);
        return null;
    }

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume); // Ensure volume is within range 0 to 1
        audioSource.volume = volume; // Set the volume of the AudioSource
    }

    public void ToggleOptionsPanel()
    {
        OptionsPanel.SetActive(!OptionsPanel.activeSelf);
    }

    public void ToggleHTPPanel()
    {
        HTPPanel.SetActive(!HTPPanel.activeSelf);
    }
}
