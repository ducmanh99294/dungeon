// SkillSlotUI.cs — gắn vào prefab skill slot
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class SkillSlotUI : MonoBehaviour, IPointerClickHandler
{
    public Image iconImage;
    public TMP_Text levelText;
    public Image bgImage;
    public Sprite normalBg;
    public Sprite selectedBg;

    private LearnedSkill skill;
    private SkillManager manager;
    private static SkillSlotUI currentSelected;

    private float lastClickTime = 0f;
    private float doubleClickGap = 0.3f; // khoảng thời gian tính là double click

    public void Setup(LearnedSkill s, SkillManager m)
    {
        skill = s;
        manager = m;

        iconImage.sprite = s.data.icon;
        iconImage.enabled = s.data.icon != null;
        levelText.text = $"Lv{s.level}";
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Deselect cũ
        if (currentSelected != null && currentSelected != this)
            currentSelected.bgImage.sprite = currentSelected.normalBg;

        currentSelected = this;
        bgImage.sprite = selectedBg;

        // Kiểm tra double click
        if (Time.unscaledTime - lastClickTime < doubleClickGap)
        {
            // Double click → mở popup gán slot
            SkillShortcutSystem.Instance?.OpenSlotSelect(skill);
            lastClickTime = 0f; // reset
        }
        else
        {
            // Single click → hiện detail
            manager.ShowDetail(skill);
            lastClickTime = Time.unscaledTime;
        }
    }
}