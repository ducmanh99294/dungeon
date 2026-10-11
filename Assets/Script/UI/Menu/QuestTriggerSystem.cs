// QuestTriggerSystem.cs — gắn vào Player hoặc GameManager
using System.Collections.Generic;
using UnityEngine;

public class QuestTriggerSystem : MonoBehaviour
{
    public static QuestTriggerSystem Instance;

    [Header("Trigger List")]
    public List<QuestTriggerData> triggers = new List<QuestTriggerData>();

    // Đếm số lần giết theo từng loại enemy
    private Dictionary<string, int> killCounts = new Dictionary<string, int>();
    // Các trigger đã kích hoạt
    private HashSet<QuestTriggerData> triggered = new HashSet<QuestTriggerData>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Gọi từ SlimeEnemy khi chết
    public void OnEnemyKilled(string enemyType)
    {
        // Cộng kill count
        if (!killCounts.ContainsKey(enemyType))
            killCounts[enemyType] = 0;
        killCounts[enemyType]++;

        Debug.Log($"[Quest Trigger] {enemyType} killed: {killCounts[enemyType]}");

        // Kiểm tra từng trigger
        foreach (var trigger in triggers)
        {
            if (trigger.canTriggerOnce && triggered.Contains(trigger)) continue;
            if (trigger.questToTrigger == null) continue;
            if (trigger.enemyType != enemyType) continue;

            bool shouldTrigger = false;

            switch (trigger.triggerType)
            {
                case TriggerType.KillCount:
                    shouldTrigger = killCounts[enemyType] >= trigger.killCountNeeded;
                    break;

                case TriggerType.RandomChance:
                    shouldTrigger = Random.Range(0f, 100f) <= trigger.triggerChance;
                    break;

                case TriggerType.Both:
                    bool killOk = killCounts[enemyType] >= trigger.killCountNeeded;
                    bool randomOk = Random.Range(0f, 100f) <= trigger.triggerChance;
                    shouldTrigger = killOk || randomOk;
                    break;
            }

            if (shouldTrigger)
            {
                if (trigger.canTriggerOnce) triggered.Add(trigger);
                QuestPopupUI.Instance?.ShowPopup(trigger.questToTrigger);
            }
        }
    }

    public int GetKillCount(string enemyType)
    {
        return killCounts.ContainsKey(enemyType) ? killCounts[enemyType] : 0;
    }
}