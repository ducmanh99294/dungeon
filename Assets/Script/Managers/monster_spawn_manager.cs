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
    // Nhận từ server khi player khác đánh monster

    void HandleMonsterDamaged(MonsterDamagedData data)
    {
        if (!activeMonsters.TryGetValue(data.monsterId, out var go)) return;
        if (go == null) return;

        var health = go.GetComponent<Health>();
        if (health != null)
        {
            // Sync HP từ server
            health.currentHP = data.hp;
            //          health.OnHealthChanged?.Invoke(data.hp, data.maxHp);
        }

        // Monster chết
        if (data.killed)
        {
            activeMonsters.Remove(data.monsterId);
            Destroy(go, 1f); // delay để chạy death anim
        }
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