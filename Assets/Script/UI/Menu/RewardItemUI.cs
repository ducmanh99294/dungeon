// RewardItemUI.cs — gắn vào prefab RewardItem
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RewardItemUI : MonoBehaviour
{
    public Image icon;
    public TMP_Text countText;

    public void Setup(QuestReward reward)
    {
        if (icon != null)
        {
            icon.sprite = reward.icon;
            icon.enabled = reward.icon != null;
        }
        if (countText != null)
            countText.text = reward.label;
    }
}