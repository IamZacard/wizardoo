// CharacterManager.cs
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
    public int CurrentMagicShardCount { get; private set; } // This is the official count!

    // Events
    public static event Action<CharacterState> OnCharacterStateChanged;
    public static event Action<int> OnGoldChanged;
    public static event Action<int> OnMagicShardChanged; // Event for UI updates

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
            CurrentMagicShardCount = 0; // Reset magic shards on spawn

            OnGoldChanged?.Invoke(CurrentGold);
            OnMagicShardChanged?.Invoke(CurrentMagicShardCount);
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

    // --- Magic Shard Management ---
    public void AddMagicShardCount(int amount)
    {
        CurrentMagicShardCount += amount;
        OnMagicShardChanged?.Invoke(CurrentMagicShardCount);
        Debug.Log($"Magic Shards added: {amount}. Total: {CurrentMagicShardCount}");
    }

    public bool SpendMagicShards(int amount)
    {
        if (CurrentMagicShardCount >= amount)
        {
            CurrentMagicShardCount -= amount;
            OnMagicShardChanged?.Invoke(CurrentMagicShardCount);
            Debug.Log($"Magic Shards spent: {amount}. Remaining: {CurrentMagicShardCount}");
            return true;
        }
        Debug.Log($"Not enough Magic Shards to spend {amount}. Have: {CurrentMagicShardCount}");
        return false;
    }

    public CharacterState GetCharacterState()
    {
        return ActiveCharacter != null ? ActiveCharacter.CurrentState : CharacterState.Dead;
    }
}