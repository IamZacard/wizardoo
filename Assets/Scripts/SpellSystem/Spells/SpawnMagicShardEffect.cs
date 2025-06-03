using System;
using UnityEngine;

[Serializable]
public class SpawnMagicShardEffect : SpellEffectBase
{
    public SpawnMagicShardEffect() {  }

    public override void Execute(CharacterBase caster, Cell targetCell)
    {
        // This might need a reference to a CharacterManager or similar to add shards
        // Assuming CharacterManager.Instance.AddMagicShards() exists
        if (CharacterManager.Instance != null)
        {
            CharacterManager.Instance.SpendMagicShards(1);
            Debug.Log("Spawned a magic shard.");
        }
        else
        {
            Debug.LogWarning("SpawnMagicShardEffect: CharacterManager.Instance not found to add shards.");
        }
    }
}