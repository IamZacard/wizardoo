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
    private Dictionary<string, SpellData> spellCache = new Dictionary<string, SpellData>();
    private bool isInitialized;

    //public static event Action<string, int, int> OnSpellChargesUpdated;
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
        if (isInitialized && GameBoard.Instance?.Actions != null)
        {
            GameBoard.Instance.Actions.OnCellRevealed.AddListener(HandleCellRevealed);
            GameBoard.Instance.Actions.OnTrapExploded.AddListener(HandleTrapExploded);
        }
    }

    // In CharacterSpellManager.cs - OnDisable should match OnEnable
    private void OnDisable()
    {
        if (GameBoard.Instance?.Actions != null)
        {
            GameBoard.Instance.Actions.OnCellRevealed.RemoveListener(HandleCellRevealed);
            GameBoard.Instance.Actions.OnTrapExploded.RemoveListener(HandleTrapExploded);
        }

        // Add this cleanup for static events
        //OnSpellChargesUpdated = null;
        OnSpellCastAttempted = null;
        OnSpellCastSuccess = null;
        OnSpellCastFailed = null;
    }

    public void Initialize(CharacterData data)
    {
        characterData = data;
        isInitialized = true;
        spellCharges.Clear();

        if (characterData != null)
        {
            foreach (var entry in characterData.characterSpells)
            {
                if (entry.spellData.resourceType == ResourceType.Charges)
                {
                    spellCharges[entry.spellData] = entry.spellData.maxCharges;
                }
            }
        }

        if (GameBoard.Instance?.Actions != null)
        {
            GameBoard.Instance.Actions.OnCellRevealed.AddListener(HandleCellRevealed);
            GameBoard.Instance.Actions.OnTrapExploded.AddListener(HandleTrapExploded);
        }

        spellCache.Clear();
        foreach (var entry in characterData.characterSpells)
        {
            spellCache[entry.spellData.spellName] = entry.spellData;
        }
    }

    public void TryCastActiveSpell()
    {
        if (characterData == null) return;

        SpellData activeSpell = characterData.characterSpells
            .FirstOrDefault(entry => entry.spellData.spellType == SpellType.Active)?.spellData;

        if (activeSpell == null) return;
        if (!CanCastSpell(activeSpell)) return;

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
                if (characterBase.CharacterData.characterName == "Gale" && CharacterManager.Instance != null)
                {
                    return CharacterManager.Instance.CurrentMagicShardCount >= spell.resourceCost;
                }
                return false;
            case ResourceType.Mana:
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

        SpellData spell = characterData.characterSpells
            .FirstOrDefault(entry => entry.spellData.spellName == spellName)?.spellData;

        return false;
    }

    public void DecrementSpellCharges(string spellName, int amount = 1)
    {
        if (characterData == null) return;

        //SpellData spell = characterData.characterSpells
        //    .FirstOrDefault(entry => entry.spellData.spellName == spellName)?.spellData;

        if (spellCache.TryGetValue(spellName, out SpellData spell) &&
       spellCharges.ContainsKey(spell))
        {
            spellCharges[spell] -= amount;
            Debug.Log($"{spell.spellName} charges decremented by {amount}. Remaining: {spellCharges[spell]}");
            if (spellCharges[spell] <= 0)
            {
                Debug.Log($"{spell.spellName} charges depleted.");
            }
        }
    }

    private void HandleCellRevealed(Cell cell)
    {
        if (!isInitialized || characterBase == null || characterData == null) return;

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

                        if (characterBase.CharacterData.characterName == "Gale" && spell.spellName == "Trap Whisper")
                        {
                            if (cell.type != Cell.CellType.Trap && UnityEngine.Random.value < 0.7f)
                            {
                                CharacterManager.Instance?.AddMagicShardCount(1);
                            }
                        }
                    }
                }
            }
        }
    }

    private void HandleTrapExploded(Cell cell)
    {
        if (!isInitialized || characterBase == null || characterData == null) return;

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

    public static void RaiseSpellCastAttempted(SpellData spell, CharacterBase caster)
    {
        OnSpellCastAttempted?.Invoke(spell, caster);
    }

    public static void RaiseSpellCastSuccess(SpellData spell, CharacterBase caster)
    {
        OnSpellCastSuccess?.Invoke(spell, caster);
    }

    public static void RaiseSpellCastFailed(SpellData spell, CharacterBase caster, string reason)
    {
        OnSpellCastFailed?.Invoke(spell, caster, reason);
    }

}
