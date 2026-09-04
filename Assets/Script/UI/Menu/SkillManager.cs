// SkillManager.cs — gắn vào SkillsPanel
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance;

    [Header("Column Skills")]
    public GameObject emptyText;   // "Chưa có skill nào"
    public GameObject skillGrid;   // Grid chứa các skill slot
    public GameObject slotPrefab;  // prefab 1 skill slot
    public Transform gridParent;  // parent của các slot

    [Header("Column Detail")]
    public GameObject detailPanel;
    public Image skillIcon;
    public TMP_Text skillName;
    public TMP_Text skillLevel;
    public TMP_Text cooldownText;
    public TMP_Text manaCostText;
    public TMP_Text descText;

    [Header("Assign Button")]
    public Button btnAssign;
    // Data
    private List<LearnedSkill> learnedSkills = new List<LearnedSkill>();
    private List<SkillSlotUI> slots = new List<SkillSlotUI>();
    private LearnedSkill selectedSkill;
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        detailPanel.SetActive(false);
        if (btnAssign != null)
            btnAssign.onClick.AddListener(() => {
                if (selectedSkill != null)
                    SkillShortcutSystem.Instance?.OpenSlotSelect(selectedSkill);
            });
        RefreshSkillGrid();
    }

    // Gọi khi nhặt Skill Book
    public void LearnSkill(SkillData data)
    {
        var existing = learnedSkills.Find(s => s.data == data);
        if (existing != null)
        {
            // Tăng level skill nếu đã có
            if (existing.level < data.maxLevel)
            {
                existing.level++;
                Debug.Log($"[Skill] {data.skillName} lên level {existing.level}!");
            }
            else
                Debug.Log($"[Skill] {data.skillName} đã đạt max level!");
        }
        else
        {
            learnedSkills.Add(new LearnedSkill(data, 1));
            Debug.Log($"[Skill] Học skill mới: {data.skillName}!");
        }

        RefreshSkillGrid();
    }

    void RefreshSkillGrid()
    {
        // Xóa slot cũ
        foreach (Transform child in gridParent)
            Destroy(child.gameObject);
        slots.Clear();

        if (learnedSkills.Count == 0)
        {
            emptyText.SetActive(true);
            skillGrid.SetActive(false);
            return;
        }

        emptyText.SetActive(false);
        skillGrid.SetActive(true);

        // Tạo slot cho mỗi skill
        foreach (var skill in learnedSkills)
        {
            var obj = Instantiate(slotPrefab, gridParent);
            var slot = obj.GetComponent<SkillSlotUI>();
            slot.Setup(skill, this);
            slots.Add(slot);
        }
    }

    public void ShowDetail(LearnedSkill skill)
    {
        selectedSkill = skill; // lưu skill đang chọn
        detailPanel.SetActive(true);
        var d = skill.data;

        skillIcon.sprite = d.icon;
        skillName.text = d.skillName;
        skillLevel.text = $"Level: {skill.level} / {d.maxLevel}";
        cooldownText.text = $"Cooldown: {d.cooldown}s";
        manaCostText.text = $"Mana Cost: {d.manaCost}";
        descText.text = d.description;
    }

    public void HideDetail() => detailPanel.SetActive(false);
}

[System.Serializable]
public class LearnedSkill
{
    public SkillData data;
    public int level;
    public LearnedSkill(SkillData d, int l) { data = d; level = l; }
}