// QuestPopupUI.cs — gắn vào QuestPopup
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestPopupUI : MonoBehaviour
{
    public static QuestPopupUI Instance;

    [Header("UI References")]
    public GameObject popupPanel;
    public TMP_Text questTitle;
    public TMP_Text questDesc;
    public TMP_Text timeLimitText;
    public TMP_Text penaltyText;
    public Button btnAccept;
    public Button btnDecline;

    [Header("Objectives")]
    public Transform objectivesContent; // Content của scroll objectives
    public GameObject objectiveRowPrefab;

    [Header("Rewards")]
    public Transform rewardsContent;
    public GameObject rewardItemPrefab;
    public Sprite goldSprite;
    public Sprite expSprite;

    private QuestData pendingQuest;
    private List<ObjectiveRowUI> objRows = new List<ObjectiveRowUI>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        popupPanel.SetActive(false);
        btnAccept.onClick.AddListener(OnAccept);
        btnDecline.onClick.AddListener(OnDecline);
    }

    public void ShowPopup(QuestData quest)
    {
        pendingQuest = quest;

        // Basic info
        questTitle.text = quest.questName;
        questDesc.text = quest.description;

        // Time limit
        float t = quest.timeLimitSeconds;
        int min = (int)(t / 60);
        int sec = (int)(t % 60);
        timeLimitText.text = $"⏱ Giới hạn: {min:00}:{sec:00}";

        // Penalty
        penaltyText.text = $"❌ Phạt: -{quest.penaltyGold}G  -{quest.penaltyEXP}EXP\n{quest.penaltyDescription}";

        // Spawn objectives
        SpawnObjectives(quest);

        // Xóa reward cũ
        foreach (Transform child in rewardsContent) Destroy(child.gameObject);

        // Gold + EXP tự động thêm vào đầu
        if (quest.rewardGold > 0)
        {
            var go = Instantiate(rewardItemPrefab, rewardsContent);
            go.GetComponent<RewardItemUI>()?.Setup(new QuestReward { icon = goldSprite, label = $"{quest.rewardGold}" });
        }
        if (quest.rewardEXP > 0)
        {
            var go = Instantiate(rewardItemPrefab, rewardsContent);
            go.GetComponent<RewardItemUI>()?.Setup(new QuestReward { icon = expSprite, label = $"+{quest.rewardEXP}" });
        }

        // Custom rewards từ list
        foreach (var reward in quest.rewardItems)
        {
            var go = Instantiate(rewardItemPrefab, rewardsContent);
            go.GetComponent<RewardItemUI>()?.Setup(reward);
        }

        popupPanel.SetActive(true);
        // Không dùng timeScale = 0 vì sẽ làm Scroll View bị đơ
        // Thay vào đó pause bằng cách block input game
        // Time.timeScale = 0f;
    }

    void SpawnObjectives(QuestData quest)
    {
        // Xóa cũ
        foreach (Transform child in objectivesContent) Destroy(child.gameObject);
        objRows.Clear();

        foreach (var obj in quest.objectives)
        {
            var go = Instantiate(objectiveRowPrefab, objectivesContent);
            var row = go.GetComponent<ObjectiveRowUI>();
            row.Setup(obj);
            objRows.Add(row);
        }
    }

    void OnAccept()
    {
        if (pendingQuest == null) return;
        QuestManager.Instance?.AddQuest(pendingQuest);
        ClosePopup();
    }

    void OnDecline() => ClosePopup();

    void ClosePopup()
    {
        popupPanel.SetActive(false);
        pendingQuest = null;
    }
}