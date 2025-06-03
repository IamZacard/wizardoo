// CharacterSpellManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class CharacterSpellManager : MonoBehaviour
{
    private CharacterBase characterBase;
    private CharacterData characterData;
    private Dictionary<SpellData, int> spellCharges = new Dictionary<SpellData, int>();
    private bool isInitialized = false;

    // Events for UI updates
    public static event Action<string, int, int> OnSpellChargesUpdated;
    public static event Action<SpellData, CharacterBase> OnSpellCastAttempted;
    public static event Action<SpellData, CharacterBase> OnSpellCastSuccess;
    public static event Action<SpellData, CharacterBase, string> OnSpellCastFailed;

    private void Awake()
    {
        characterBase = GetComponent<CharacterBase>();
        if (characterBase == null)
        {
            Debug.LogError("CharacterSpellManager requires CharacterBase component on the same GameObject.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        // Only subscribe to events after we're properly initialized
        if (isInitialized && GameBoard.Instance != null && GameBoard.Instance.Actions != null)
        {
            GameBoard.Instance.Actions.OnCellRevealed.AddListener(HandleCellRevealed);
            GameBoard.Instance.Actions.OnTrapExploded.AddListener(HandleTrapExploded);
        }
    }

    private void OnDisable()
    {
        if (GameBoard.Instance != null && GameBoard.Instance.Actions != null)
        {
            GameBoard.Instance.Actions.OnCellRevealed.RemoveListener(HandleCellRevealed);
            GameBoard.Instance.Actions.OnTrapExploded.RemoveListener(HandleTrapExploded);
        }
    }

    private void Start()
    {
        // Don't initialize here - wait for Initialize() to be called
        if (isInitialized)
        {
            InitializeSpellCharges();
        }
    }

    public void Initialize(CharacterData data)
    {
        characterData = data;
        isInitialized = true;

        InitializeSpellCharges();

        // Now it's safe to subscribe to events
        if (GameBoard.Instance != null && GameBoard.Instance.Actions != null)
        {
            GameBoard.Instance.Actions.OnCellRevealed.AddListener(HandleCellRevealed);
            GameBoard.Instance.Actions.OnTrapExploded.AddListener(HandleTrapExploded);
        }
    }

    private void InitializeSpellCharges()
    {
        spellCharges.Clear();
        if (characterData == null) return;

        foreach (var entry in characterData.characterSpells)
        {
            if (entry.spellData.resourceType == ResourceType.Charges)
            {
                spellCharges[entry.spellData] = entry.spellData.maxCharges;
            }
        }
    }

    public void TryCastActiveSpell()
    {
        if (characterData == null)
        {
            Debug.LogWarning("CharacterData not initialized for spell casting.");
            return;
        }

        SpellData activeSpell = characterData.characterSpells
                                            .FirstOrDefault(entry => entry.spellData.spellType == SpellType.Active)?.spellData;

        if (activeSpell == null)
        {
            Debug.Log($"No active spell configured for {characterData.characterName}.", this);
            return;
        }

        if (!CanCastSpell(activeSpell))
        {
            Debug.Log($"Cannot cast {activeSpell.spellName} - requirements not met.", this);
            return;
        }

        ConsumeSpellCost(activeSpell);
        SpellExecutor.ExecuteSpell(activeSpell, characterBase);
    }

    private bool CanCastSpell(SpellData spell)
    {
        switch (spell.resourceType)
        {
            case ResourceType.None:
                return true;
            case ResourceType.MagicShards:
                // Check CharacterManager for Magic Shards
                if (characterBase.CharacterData.characterName == "Gale" && CharacterManager.Instance != null)
                {
                    return CharacterManager.Instance.CurrentMagicShardCount >= spell.resourceCost;
                }
                Debug.LogWarning($"Resource type {spell.resourceType} not handled globally for {spell.spellName}.");
                return false;
            case ResourceType.Mana:
                Debug.LogWarning($"Mana resource type not implemented yet for {spell.spellName}.");
                return false;
            case ResourceType.Charges:
                if (spellCharges.TryGetValue(spell, out int currentCharges))
                {
                    return currentCharges >= spell.resourceCost;
                }
                return false;
            default:
                return false;
        }
    }

    private void ConsumeSpellCost(SpellData spell)
    {
        switch (spell.resourceType)
        {
            case ResourceType.MagicShards:
                // Consume Magic Shards via CharacterManager
                if (characterBase.CharacterData.characterName == "Gale" && CharacterManager.Instance != null)
                {
                    CharacterManager.Instance.SpendMagicShards(spell.resourceCost);
                }
                break;
            case ResourceType.Mana:
                break;
            case ResourceType.Charges:
                if (spell.consumesCharges && spellCharges.ContainsKey(spell))
                {
                    spellCharges[spell] -= spell.resourceCost;
                    Debug.Log($"Spell {spell.spellName} consumed {spell.resourceCost} charge(s). Remaining: {spellCharges[spell]}");
                }
                break;
            case ResourceType.None:
                break;
        }
    }

    public bool IsSpellActive(string spellName)
    {
        if (characterData == null) return false;

        SpellData mysticShield = characterData.characterSpells
                                             .FirstOrDefault(entry => entry.spellData.spellName == spellName)?.spellData;
        return false;
    }

    public void DecrementSpellCharges(string spellName, int amount = 1)
    {
        if (characterData == null) return;

        SpellData spellToDecrement = characterData.characterSpells
                                                 .FirstOrDefault(entry => entry.spellData.spellName == spellName)?.spellData;

        if (spellToDecrement != null && spellCharges.ContainsKey(spellToDecrement))
        {
            spellCharges[spellToDecrement] -= amount;
            Debug.Log($"{spellToDecrement.spellName} charges decremented by {amount}. Remaining: {spellCharges[spellToDecrement]}");
            if (spellCharges[spellToDecrement] <= 0)
            {
                Debug.Log($"{spellToDecrement.spellName} charges depleted.");
            }
        }
    }

    // --- Event Handlers for Triggered Spells ---

    private void HandleCellRevealed(Cell cell)
    {
        // Add null checks at the beginning
        if (!isInitialized || characterBase == null || characterData == null)
        {
            Debug.LogWarning("CharacterSpellManager not properly initialized, skipping cell reveal handling");
            return;
        }

        foreach (var entry in characterData.characterSpells)
        {
            SpellData spell = entry.spellData;

            if ((spell.spellType == SpellType.Passive || spell.spellType == SpellType.Triggered) && spell.triggers != null)
            {
                foreach (SpellTriggerBase trigger in spell.triggers)
                {
                    if (trigger != null && trigger.Evaluate(characterBase, cell))
                    {
                        Debug.Log($"Triggered spell {spell.spellName} via {trigger.description} on cell {cell.position}");
                        SpellExecutor.ExecuteSpell(spell, characterBase);

                        // Gale's Trap Whisper - Add Magic Shards via CharacterManager
                        if (characterBase.CharacterData.characterName == "Gale" && spell.spellName == "Trap Whisper")
                        {
                            if (cell.type != Cell.CellType.Trap && UnityEngine.Random.value < 0.7f)
                            {
                                if (CharacterManager.Instance != null)
                                {
                                    CharacterManager.Instance.AddMagicShardCount(1); // Add shard via CharacterManager
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private void HandleTrapExploded(Cell cell)
    {
        // Add null checks at the beginning
        if (!isInitialized || characterBase == null || characterData == null)
        {
            Debug.LogWarning("CharacterSpellManager not properly initialized, skipping trap explosion handling");
            return;
        }

        foreach (var entry in characterData.characterSpells)
        {
            SpellData spell = entry.spellData;
            if ((spell.spellType == SpellType.Passive || spell.spellType == SpellType.Triggered) && spell.triggers != null)
            {
                foreach (SpellTriggerBase trigger in spell.triggers)
                {
                    if (trigger != null && trigger.Evaluate(characterBase, cell))
                    {
                        Debug.Log($"Triggered spell {spell.spellName} via {trigger.description} on exploded trap {cell.position}");
                        SpellExecutor.ExecuteSpell(spell, characterBase);
                    }
                }
            }
        }
    }
}