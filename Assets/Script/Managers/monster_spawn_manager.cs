void ClearAllMonsters()
{
    foreach (var go in activeMonsters.Values)
        if (go != null) Destroy(go);
    activeMonsters.Clear();
}

// ── DATA CLASSES ─────────────────────────────────────────────────────────────

[System.Serializable]
public class MonsterTemplateData
{
    public string monsterId;
    public string type;
    public string displayName;
    public int    maxHp;
    public int    damage;
    public float  speed;
    public float  detectRange;
    public float  attackRange;
    public int    expReward;
    public float  spawnX;
    public float  spawnY;
}

[System.Serializable]
public class MonsterListResponse
{
    public MonsterTemplateData[] monsters;
}
