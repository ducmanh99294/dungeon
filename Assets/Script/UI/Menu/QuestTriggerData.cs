// QuestTriggerData.cs — ScriptableObject định nghĩa điều kiện trigger quest
using UnityEngine;

public enum TriggerType { KillCount, RandomChance, Both }

[CreateAssetMenu(fileName = "QuestTrigger", menuName = "Terra Reclaim/Quest Trigger")]
public class QuestTriggerData : ScriptableObject
{
    [Header("Quest để trigger")]
    public QuestData questToTrigger;

    [Header("Trigger Type")]
    public TriggerType triggerType;

    [Header("Kill Count Trigger")]
    public string enemyType = "Slime"; // tag hoặc tên enemy
    public int killCountNeeded = 50;      // giết đủ bao nhiêu con

    [Header("Random Chance Trigger")]
    [Range(0f, 100f)]
    public float triggerChance = 5f; // % mỗi lần giết

    [Header("Settings")]
    public bool canTriggerOnce = true; // chỉ trigger 1 lần duy nhất
}