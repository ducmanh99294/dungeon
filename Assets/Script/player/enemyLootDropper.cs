// EnemyLootDropper.cs — gắn vào Slime
using UnityEngine;

public class EnemyLootDropper : MonoBehaviour
{
    [Header("Loot")]
    public LootTable lootTable;  // kéo ScriptableObject vào đây
    public float expReward = 20f;
    public float dropRadius = 0.5f; // item rơi ngẫu nhiên trong vòng này

    private Health health;
    private PlayerStats playerStats;

    void Awake()
    {
        health = GetComponent<Health>();
    }

    void Start()
    {
        if (health != null)
            health.OnDeath += OnDeath;

        // Tìm PlayerStats
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerStats = p.GetComponent<PlayerStats>();
    }

    void OnDeath()
    {
        DropLoot();
        GiveEXP();
    }

    void DropLoot()
    {
        if (lootTable == null) return;

        var dropped = lootTable.Roll();
        foreach (var entry in dropped)
        {
            if (entry.itemPrefab == null) continue;

            int amount = Random.Range(entry.minAmount, entry.maxAmount + 1);
            for (int i = 0; i < amount; i++)
            {
                // Rơi ngẫu nhiên quanh vị trí enemy
                Vector2 offset = Random.insideUnitCircle * dropRadius;
                Vector3 spawnPos = transform.position + new Vector3(offset.x, offset.y, 0f);

                GameObject item = Instantiate(entry.itemPrefab, spawnPos, Quaternion.identity);
                var droppedItem = item.GetComponent<DroppedItem>();
                if (droppedItem != null)
                {
                    droppedItem.itemName = entry.itemName;
                    droppedItem.amount = amount;
                    droppedItem.rarity = entry.rarity;
                }

                Debug.Log($"[Loot] Drop: {entry.itemName} ({entry.rarity}) {entry.dropChance}%");
            }
        }
    }

    void GiveEXP()
    {
        if (playerStats != null)
        {
            playerStats.GainEXP(expReward);
            Debug.Log($"[EXP] +{expReward} EXP");
        }
    }

    void OnDestroy()
    {
        if (health != null)
            health.OnDeath -= OnDeath;
    }
}