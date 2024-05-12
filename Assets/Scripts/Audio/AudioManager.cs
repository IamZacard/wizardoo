using System.Collections.Generic;
using UnityEngine;

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

    [Range(0f, 1f)] // This attribute limits the range of volume between 0 and 1
    public float volume = .2f; // Default volume value is 1 (maximum)

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

        AudioManager.Instance.Volume = 0.1f; // Set volume to 50%
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


    
}
