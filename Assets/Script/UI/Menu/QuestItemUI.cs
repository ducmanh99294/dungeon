// QuestItemUI.cs — gắn vào prefab QuestItem
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class QuestItemUI : MonoBehaviour, IPointerClickHandler
{
    public TMP_Text questName;
    public TMP_Text timeLeft;
    public Image statusIcon;

    public Sprite iconActive;
    public Sprite iconCompleted;
    public Sprite iconFailed;

    private ActiveQuest quest;
    private QuestManager manager;

    public void Setup(ActiveQuest q, QuestManager m)
    {
        quest = q;
        manager = m;

        questName.text = q.data.questName;
        UpdateStatus();
    }

    void Update()
    {
        if (quest == null) return;
        UpdateStatus();
    }

    void UpdateStatus()
    {
        switch (quest.status)
        {
            case QuestStatus.Active:
                float t = quest.timeLeft;
                int min = (int)(t / 60);
                int sec = (int)(t % 60);
                timeLeft.text = $"⏱ {min:00}:{sec:00}";
                timeLeft.color = t < 60f ? Color.red : Color.white;
                if (statusIcon != null && iconActive != null)
                    statusIcon.sprite = iconActive;
                break;

            case QuestStatus.Completed:
                timeLeft.text = "✓ Hoàn thành";
                timeLeft.color = Color.green;
                if (statusIcon != null && iconCompleted != null)
                    statusIcon.sprite = iconCompleted;
                break;

            case QuestStatus.Failed:
                timeLeft.text = "✗ Thất bại";
                timeLeft.color = Color.red;
                if (statusIcon != null && iconFailed != null)
                    statusIcon.sprite = iconFailed;
                break;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        manager.ShowDetail(quest);
    }
}