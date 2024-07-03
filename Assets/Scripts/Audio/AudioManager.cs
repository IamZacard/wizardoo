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
        Success,
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

    // Make sure to plug these in in the inspector.
    public AudioSource musicPlayer;
    public AudioSource effectsPlayer;

    public TextMeshProUGUI musicPlayerNumber;
    public TextMeshProUGUI effectsPlayerNumber;

    private static float volumeVar = 100f;

    public GameObject OptionsPanel;
    public GameObject HTPPanel;

    //public static AudioManager instance;

    [Range(0f, 1f)] // This attribute limits the range of volume between 0 and 1
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
            Destroy(this.gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(this.gameObject);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        //AudioManager.Instance.Volume = 0.1f; // Set volume to 50%

        musicVolumeSlider.onValueChanged.AddListener(SetMusicListenerVolume);
        effectsVolumeSlider.onValueChanged.AddListener(SetEffectsListenerVolume);

        musicPlayer.volume = .5f;
        effectsPlayer.volume = .5f;
    }

    private void Update()
    {
        musicPlayerNumber.text = (musicPlayer.volume * 100f).ToString();
        effectsPlayerNumber.text = (effectsPlayer.volume * 100f).ToString();


        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OptionsPanel.SetActive(false);
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            if (!OptionsPanel.activeInHierarchy)
            {
                OptionsPanel.SetActive(true);
            }
            else
            {
                OptionsPanel.SetActive(false);
            }
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            if (!HTPPanel.activeInHierarchy)
            {
                HTPPanel.SetActive(true);
            }
            else
            {
                HTPPanel.SetActive(false);
            }
        }
    }

    private void SetMusicListenerVolume(float volume)
    {
        musicPlayer.volume = volume / volumeVar;
    }

    private void SetEffectsListenerVolume(float volume)
    {
        effectsPlayer.volume = volume / volumeVar;
    }

    public void ApplyMusicSettings()
    {
        // The volume updates itself through the slider's events, so no need to update 
        // the volume in this function.
        SetMusicListenerVolume(volumeVar);
    }

    public void ApplyEffectsSettings()
    {
        // Or this one.
        SetEffectsListenerVolume(volumeVar);
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
    public void PlaySounds(SoundType soundType, float pitch)
    {
        int index = (int)soundType;

        /*if (index >= 0 && index < soundClips.Count)
        {
            audioSource.pitch = pitch;
            audioSource.PlayOneShot(soundClips[index]);
        }
        else
        {
            Debug.LogError("Invalid sound type!");
        }*/
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
    public float Volume
    {
        get { return volume; }
        set
        {
            volume = Mathf.Clamp01(value); // Ensure volume is within range 0 to 1
            audioSource.volume = volume; // Update the volume of the AudioSource
        }
    }

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume); // Ensure volume is within range 0 to 1
        audioSource.volume = volume; // Set the volume of the AudioSource
    }


    public void Options()
    {
        if (OptionsPanel.activeSelf)
        {
            // If it is active, set it to false
            OptionsPanel.SetActive(false);
        }
        else
        {
            // If it is not active, set it to true
            OptionsPanel.SetActive(true);
        }
    }

    public void HowToPlay()
    {
        if (HTPPanel.activeSelf)
        {
            // If it is active, set it to false
            HTPPanel.SetActive(false);
        }
        else
        {
            // If it is not active, set it to true
            HTPPanel.SetActive(true);
        }
    }
}
