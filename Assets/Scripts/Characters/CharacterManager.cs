using UnityEngine;
using System;

public class CharacterManager : MonoBehaviour
{
    public static CharacterManager Instance { get; private set; }

    [Header("References")]
    public Transform spawnPoint;

    [Header("Current Character")]
    public CharacterData selectedCharacterData;

    // Current character instance
    public CharacterBase ActiveCharacter { get; private set; }

    // Global stats
    public int CurrentGold { get; private set; }

    // Events
    public static event Action<CharacterState> OnCharacterStateChanged;
    public static event Action<int> OnGoldChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (selectedCharacterData != null)
        {
            SpawnCharacter(selectedCharacterData);
        }
    }

    public void SpawnCharacter(CharacterData characterData)
    {
        // Destroy existing character if any
        if (ActiveCharacter != null)
        {
            Destroy(ActiveCharacter.gameObject);
        }

        // Spawn new character
        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        GameObject characterGO = Instantiate(characterData.prefab, spawnPos, Quaternion.identity);

        ActiveCharacter = characterGO.GetComponent<CharacterBase>();
        if (ActiveCharacter != null)
        {
            ActiveCharacter.Initialize(characterData);
            CurrentGold = characterData.startingGold;
            OnGoldChanged?.Invoke(CurrentGold);
        }
        else
        {
            Debug.LogError($"Character prefab {characterData.name} doesn't have CharacterBase component!");
        }
    }

    public void NotifyStateChanged(CharacterState newState)
    {
        OnCharacterStateChanged?.Invoke(newState);
    }

    public void AddGold(int amount)
    {
        CurrentGold += amount;
        OnGoldChanged?.Invoke(CurrentGold);
    }

    public bool SpendGold(int amount)
    {
        if (CurrentGold >= amount)
        {
            CurrentGold -= amount;
            OnGoldChanged?.Invoke(CurrentGold);
            return true;
        }
        return false;
    }

    public CharacterState GetCharacterState()
    {
        return ActiveCharacter != null ? ActiveCharacter.CurrentState : CharacterState.Dead;
    }
}