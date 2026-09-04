// LootTable.cs — định nghĩa item và tỉ lệ drop
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class LootEntry
{
    public GameObject itemPrefab;   // prefab item sẽ spawn
    public string itemName;
    [Range(0f, 100f)]
    public float dropChance;   // % rơi ra (0-100)
    public int minAmount = 1;
    public int maxAmount = 1;

    public enum Rarity { Common, Uncommon, Rare, Epic }
    public Rarity rarity = Rarity.Common;
}

[CreateAssetMenu(fileName = "LootTable", menuName = "Terra Reclaim/Loot Table")]
public class LootTable : ScriptableObject
{
    public List<LootEntry> entries = new List<LootEntry>();

    public List<LootEntry> Roll()
    {
        var dropped = new List<LootEntry>();
        foreach (var entry in entries)
        {
            float roll = Random.Range(0f, 100f);
            if (roll <= entry.dropChance)
                dropped.Add(entry);
        }
        return dropped;
    }
}