// MonsterPrefabRegistry.cs — gắn vào GameManager object
using UnityEngine;

public class MonsterPrefabRegistry : MonoBehaviour
{
    public static MonsterPrefabRegistry Instance;

    [System.Serializable]
    public class MonsterEntry
    {
        public string     type;   // "slime", "goblin"... khớp với backend
        public GameObject prefab; // kéo prefab vào
    }

    [Header("Monster Prefabs")]
    public MonsterEntry[] monsters;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Lấy prefab theo type
    public GameObject GetPrefab(string type)
    {
        foreach (var m in monsters)
            if (m.type == type) return m.prefab;

        Debug.LogWarning($"[MonsterRegistry] Không tìm thấy prefab: {type}");
        return null;
    }

    // Apply template data lên monster vừa spawn
    public void ApplyTemplate(GameObject monsterObj, MonsterTemplateData template)
    {
        if (monsterObj == null || template == null) return;

        // Apply Health
        var health = monsterObj.GetComponent<Health>();
        if (health != null)
        {
            health.maxHP     = template.maxHp;
            health.currentHP = template.maxHp;
        }

        // Apply SlimeEnemy stats
        var slime = monsterObj.GetComponent<SlimeEnemy>();
        if (slime != null)
        {
            slime.damage      = template.damage;
            slime.chaseSpeed  = template.speed;
            slime.detectRange = template.detectRange;
            slime.attackRange = template.attackRange;
        }

        // Gán monsterId từ server
        var netMonster = monsterObj.GetComponent<NetworkMonster>();
        if (netMonster != null)
            netMonster.monsterId = template.monsterId;
    }
}
