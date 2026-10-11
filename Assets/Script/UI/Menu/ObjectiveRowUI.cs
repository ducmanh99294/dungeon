// ObjectiveRowUI.cs — gắn vào prefab ObjectiveRow
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ObjectiveRowUI : MonoBehaviour
{
    public Image checkbox;
    public TMP_Text objText;
    public TMP_Text objCount;

    public Sprite checkedSprite;
    public Sprite uncheckedSprite;

    public void Setup(QuestObjective obj)
    {
        objText.text = obj.objectiveText;
        UpdateCount(obj);
    }

    public void UpdateCount(QuestObjective obj)
    {
        objCount.text = $"{obj.currentCount} / {obj.targetCount}";
        bool done = obj.currentCount >= obj.targetCount;
        if (checkbox != null)
            checkbox.sprite = done ? checkedSprite : uncheckedSprite;
        objCount.color = done ? Color.green : Color.white;
    }
}