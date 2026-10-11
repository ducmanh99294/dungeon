// QuestManager.cs — gắn vào QuestsPanel
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    [Header("Quest List")]
    public Transform questListContent;
    public GameObject questItemPrefab;

    [Header("Quest Detail")]
    public GameObject detailPanel;
    public TMP_Text questTitle;
    public TMP_Text questDesc;
    public TMP_Text timeRemaining;
    public TMP_Text objectivesList;
    public TMP_Text penaltyText;

    [Header("Quest Detail Rewards")]
    public Transform detailRewardsContent; // kéo RewardsContent vào
    public GameObject rewardItemPrefab;     // kéo prefab RewardItem vào
    public Sprite goldSprite;
    public Sprite expSprite;

    [Header("References")]
    public PlayerStats playerStats;
    public InventoryManager inventoryManager;

    private List<ActiveQuest> activeQuests = new List<ActiveQuest>();
    private ActiveQuest selectedQuest;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (detailPanel != null) detailPanel.SetActive(false);
    }

    void Update()
    {
        foreach (var quest in activeQuests)
        {
            if (quest.status != QuestStatus.Active) continue;
            quest.timeLeft -= Time.deltaTime;
            if (quest.timeLeft <= 0f) FailQuest(quest);
        }

        if (selectedQuest != null && selectedQuest.status == QuestStatus.Active)
            UpdateTimeUI();
    }

    // ── THÊM QUEST ──────────────────────────────
    public void AddQuest(QuestData data)
    {
        if (activeQuests.Exists(q => q.data == data))
        {
            Debug.Log($"[Quest] Đã có quest: {data.questName}");
            return;
        }
        activeQuests.Add(new ActiveQuest(data));
        Debug.Log($"[Quest] Nhận quest: {data.questName}");
        RefreshQuestList();
    }

    // ── HOÀN THÀNH QUEST ────────────────────────
    public void CompleteQuest(QuestData data)
    {
        var quest = activeQuests.Find(q => q.data == data);
        if (quest == null || quest.status != QuestStatus.Active) return;

        quest.status = QuestStatus.Completed;
        if (playerStats != null)
        {
            playerStats.GainEXP(data.rewardEXP);
            inventoryManager?.AddGold(data.rewardGold);
        }

        Debug.Log($"[Quest] Hoàn thành: {data.questName} → +{data.rewardGold}G +{data.rewardEXP}EXP");
        RefreshQuestList();
    }

    // ── THẤT BẠI QUEST ──────────────────────────
    void FailQuest(ActiveQuest quest)
    {
        quest.status = QuestStatus.Failed;
        quest.timeLeft = 0f;

        if (playerStats != null)
        {
            playerStats.GainEXP(-quest.data.penaltyEXP);
            inventoryManager?.AddGold(-quest.data.penaltyGold);
        }

        Debug.Log($"[Quest] Thất bại: {quest.data.questName} → -{quest.data.penaltyGold}G -{quest.data.penaltyEXP}EXP");
        RefreshQuestList();
    }

    // ── HIỂN THỊ DANH SÁCH ──────────────────────
    void RefreshQuestList()
    {
        foreach (Transform child in questListContent)
            Destroy(child.gameObject);

        foreach (var quest in activeQuests)
        {
            var obj = Instantiate(questItemPrefab, questListContent);
            var item = obj.GetComponent<QuestItemUI>();
            item.Setup(quest, this);
        }
    }

    // ── HIỂN THỊ DETAIL ─────────────────────────
    public void ShowDetail(ActiveQuest quest)
    {
        selectedQuest = quest;
        detailPanel.SetActive(true);

        var d = quest.data;
        questTitle.text = d.questName;
        questDesc.text = d.description;

        // Objectives
        string objText = "";
        foreach (var obj in d.objectives)
            objText += $"• {obj.objectiveText}: {obj.currentCount}/{obj.targetCount}\n";
        objectivesList.text = objText;

        // Rewards — spawn động giống popup
        if (detailRewardsContent != null && rewardItemPrefab != null)
        {
            foreach (Transform child in detailRewardsContent)
                Destroy(child.gameObject);

            if (d.rewardGold > 0)
            {
                var go = Instantiate(rewardItemPrefab, detailRewardsContent);
                go.GetComponent<RewardItemUI>()?.Setup(new QuestReward { icon = goldSprite, label = $"{d.rewardGold}" });
            }
            if (d.rewardEXP > 0)
            {
                var go = Instantiate(rewardItemPrefab, detailRewardsContent);
                go.GetComponent<RewardItemUI>()?.Setup(new QuestReward { icon = expSprite, label = $"+{d.rewardEXP}" });
            }
            foreach (var reward in d.rewardItems)
            {
                var go = Instantiate(rewardItemPrefab, detailRewardsContent);
                go.GetComponent<RewardItemUI>()?.Setup(reward);
            }
        }

        // Penalty
        penaltyText.text = $"-{d.penaltyGold} G  -{d.penaltyEXP} EXP\n{d.penaltyDescription}";

        UpdateTimeUI();
    }

    void UpdateTimeUI()
    {
        if (selectedQuest == null || timeRemaining == null) return;

        if (selectedQuest.status == QuestStatus.Failed)
        {
            timeRemaining.text = "⏱ Hết giờ!";
            timeRemaining.color = Color.red;
            return;
        }
        if (selectedQuest.status == QuestStatus.Completed)
        {
            timeRemaining.text = "✓ Hoàn thành!";
            timeRemaining.color = Color.green;
            return;
        }

        float t = selectedQuest.timeLeft;
        int min = (int)(t / 60);
        int sec = (int)(t % 60);
        timeRemaining.text = $"⏱ {min:00}:{sec:00}";
        timeRemaining.color = t < 60f ? Color.red : Color.white;
    }
}

[System.Serializable]
public class ActiveQuest
{
    public QuestData data;
    public QuestStatus status;
    public float timeLeft;

    public ActiveQuest(QuestData d)
    {
        data = d;
        status = QuestStatus.Active;
        timeLeft = d.timeLimitSeconds;
    }
}