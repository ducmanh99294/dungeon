// MonsterSpawnManager.cs — gắn vào GameManager object
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class MonsterSpawnManager : MonoBehaviour
{
    public static MonsterSpawnManager Instance;

    [Header("Settings")]
    public string currentZone = "MainMap";

    // Track monsters đang có trong scene
    private Dictionary<string, GameObject> activeMonsters = new Dictionary<string, GameObject>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void OnEnable()
    {
        if (NetworkManager.Instance == null) return;
        NetworkManager.Instance.OnMonsterDamaged += HandleMonsterDamaged;
    }

    void OnDisable()
    {
        if (NetworkManager.Instance == null) return;
        NetworkManager.Instance.OnMonsterDamaged -= HandleMonsterDamaged;
    }

    // ── LOAD MONSTERS FROM BACKEND ──────────────────────────────────────────

    public void LoadMonstersForZone(string zone)
    {
        currentZone = zone;
        StartCoroutine(LoadMonstersRoutine(zone));
    }

    IEnumerator LoadMonstersRoutine(string zone)
    {
        string url = $"{NetworkManager.Instance?.serverUrl}/api/monsters?zone={zone}";

        using var req = UnityWebRequest.Get(url);
        req.SetRequestHeader(
            "Authorization",
            $"Bearer {NetworkManager.Instance?.Token}"
        );

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[MonsterSpawn] Load failed: {req.error}");
            yield break;
        }

        var res = JsonUtility.FromJson<MonsterListResponse>(
            req.downloadHandler.text
        );

        if (res?.monsters == null) yield break;

        // Xóa monster cũ
        ClearAllMonsters();

        // Spawn monster mới
        foreach (var template in res.monsters)
            SpawnMonster(template);

        Debug.Log($"[MonsterSpawn] Spawned {res.monsters.Length} monsters in {zone}");
    }

    // ── SPAWN ────────────────────────────────────────────────────────────────

    void SpawnMonster(MonsterTemplateData template)
    {
        var prefab = MonsterPrefabRegistry.Instance?.GetPrefab(template.type);
        if (prefab == null) return;

        var pos = new Vector3(template.spawnX, template.spawnY, 0f);
        var go = Instantiate(prefab, pos, Quaternion.identity);

        // Apply stats từ backend
        MonsterPrefabRegistry.Instance?.ApplyTemplate(go, template);

        // Gắn NetworkMonster nếu chưa có
        var netMonster = go.GetComponent<NetworkMonster>();
        if (netMonster == null) netMonster = go.AddComponent<NetworkMonster>();
        netMonster.monsterId = template.monsterId;

        activeMonsters[template.monsterId] = go;
    }

    // ── HANDLE MONSTER DAMAGED ───────────────────────────────────────────────
    // Nhận từ server khi monster bị đánh (kể cả đòn của mình, server echo lại)

    void HandleMonsterDamaged(MonsterDamagedData data)
    {
        if (!activeMonsters.TryGetValue(data.monsterId, out var go))
            return;

        if (go == null)
        {
            activeMonsters.Remove(data.monsterId);
            return;
        }

        var health = go.GetComponent<Health>();

        // Đi qua Health.TakeDamage thay vì gán HP trực tiếp, để fire đầy đủ:
        // OnHealthChanged (thanh máu, hurt state, flash đỏ) và
        // OnDeath (death anim, rớt loot, cộng EXP)
        // Lưu ý: enemy phải để useInvincibility = false trong Inspector
        if (health != null && !health.IsDead && data.hp < health.currentHP)
        {
            int damage = health.currentHP - data.hp;
            health.TakeDamage(damage);
        }

        if (data.killed)
        {
            activeMonsters.Remove(data.monsterId);

            // Đảm bảo chạy death flow nếu TakeDamage chưa giết được
            // (vd. useInvincibility bị bật nhầm trên enemy)
            if (health != null && !health.IsDead)
                health.TakeDamage(health.currentHP);

            // Destroy sau delay để death anim kịp chạy.
            // SlimeEnemy.Die cũng tự Destroy sau 1.5s — gọi trùng vẫn an toàn.
            Destroy(go, 1.5f);
        }
    }

    // ── SPAWN TỪ DUNGEON ──────────────────────────────────────────────────
    // Dùng cho procedural dungeon: server đã validate + chọn quái,
    // client chỉ việc spawn theo danh sách server trả về.

    public void SpawnFromTemplates(System.Collections.Generic.List<MonsterTemplateData> templates)
    {
        if (templates == null) return;

        ClearAllMonsters();

        foreach (var template in templates)
            SpawnMonster(template);

        Debug.Log($"[MonsterSpawn] Spawned {templates.Count} dungeon monsters");
    }

    // Spawn THÊM quái mà không xóa quái cũ.
    // Dùng cho quái canh phòng bí mật (xuất hiện khi cửa mở).
    public void SpawnExtra(System.Collections.Generic.List<MonsterTemplateData> templates)
    {
        if (templates == null) return;

        foreach (var template in templates)
            SpawnMonster(template);

        Debug.Log($"[MonsterSpawn] Spawned {templates.Count} extra monsters");
    }

    // ── CLEAR ────────────────────────────────────────────────────────────────

    void ClearAllMonsters()
    {
        foreach (var go in activeMonsters.Values)
            if (go != null) Destroy(go);
        activeMonsters.Clear();
    }
}

// ── DATA CLASSES ─────────────────────────────────────────────────────────────

[System.Serializable]
public class MonsterTemplateData
{
    public string monsterId;
    public string type;
    public string displayName;
    public int maxHp;
    public int damage;
    public float speed;
    public float detectRange;
    public float attackRange;
    public int expReward;
    public float spawnX;
    public float spawnY;
}

[System.Serializable]
public class MonsterListResponse
{
    public MonsterTemplateData[] monsters;
}
