// QuestData.cs — ScriptableObject định nghĩa quest
using System.Collections.Generic;
using UnityEngine;

public enum QuestStatus { Active, Completed, Failed }

[System.Serializable]
public class QuestObjective
{
    public string objectiveText;  // "Defeat Slimes"
    public int targetCount;    // 10
    [HideInInspector]
    public int currentCount;   // 0 — tự cập nhật khi chơi
}

[System.Serializable]
public class QuestReward
{
    public Sprite icon;   // kéo sprite vào
    public string label;  // "100" hoặc "x5"
}

[CreateAssetMenu(fileName = "QuestData", menuName = "Terra Reclaim/Quest Data")]
public class QuestData : ScriptableObject
{
    [Header("Basic Info")]
    public string questName;
    [TextArea(2, 4)]
    public string description;

    [Header("Objectives")]
    public List<QuestObjective> objectives = new List<QuestObjective>();

    [Header("Time Limit")]
    public float timeLimitSeconds = 300f;

    [Header("Rewards (hoàn thành đúng hạn)")]
    public int rewardGold;
    public int rewardEXP;
    public List<QuestReward> rewardItems = new List<QuestReward>(); // tùy chỉnh thêm bao nhiêu cũng được

    [Header("Penalty")]
    public int penaltyGold;
    public int penaltyEXP;
    public string penaltyDescription;

    [Header("Info")]
    public string npcGiver;
    public string location;
    public string difficulty;
}