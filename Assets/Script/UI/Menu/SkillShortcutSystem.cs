// SkillShortcutSystem.cs — gắn vào Canvas hoặc PlayerHUD
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class SkillShortcutSystem : MonoBehaviour
{
    public static SkillShortcutSystem Instance;

    [System.Serializable]
    public class ShortcutSlot
    {
        public Image skillIcon;
        public Image slotBg;
        public Sprite emptySprite;  // sprite khi slot trống
        public Sprite activeSprite; // sprite khi đang dùng
        [HideInInspector] public LearnedSkill assignedSkill;
    }

    [Header("Shortcut Slots")]
    public List<ShortcutSlot> slots = new List<ShortcutSlot>();

    [Header("Slot Select Popup")]
    public GameObject slotSelectPopup;
    public List<Button> slotSelectButtons = new List<Button>();
    public TMP_Text popupTitle;

    [Header("References")]
    public PlayerStats playerStats;

    private LearnedSkill pendingSkill; // skill đang chờ gán

    [Header("Notification")]
    public GameObject notificationPanel; // kéo cả panel (bg + text) vào
    public TMP_Text notificationText;
    //[Header("Shortcut Bar")]
    //public GameObject shortcutBar;

    // Phím tắt tương ứng
    static readonly KeyCode[] hotkeys = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3,
                                          KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6 };

    private float[] cooldownTimers; // đếm ngược cooldown từng slot

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        cooldownTimers = new float[6]; // tối đa 6 slot
    }

    void Start()
    {
        slotSelectPopup.SetActive(false);
        if (notificationPanel != null) notificationPanel.SetActive(false); // ẩn lúc đầu

        // Gắn event cho từng button chọn slot
        for (int i = 0; i < slotSelectButtons.Count; i++)
        {
            int index = i;
            slotSelectButtons[i].onClick.AddListener(() => AssignToSlot(index));
        }

        RefreshAllSlots();
    }

    void Update()
    {
        // Giảm cooldown timer
        for (int i = 0; i < cooldownTimers.Length; i++)
            if (cooldownTimers[i] > 0) cooldownTimers[i] -= Time.deltaTime;

        // Bấm phím 1/2/3 để dùng skill
        for (int i = 0; i < slots.Count && i < hotkeys.Length; i++)
            if (Input.GetKeyDown(hotkeys[i]))
                UseSkill(i);

        if (Input.GetMouseButtonDown(0) && notificationPanel != null && notificationPanel.activeSelf)
        {
            StopCoroutine(nameof(HideNotification));
            notificationPanel.SetActive(false);
        }
    }

    // Gọi từ SkillSlotUI khi click skill trong SkillPanel
    public void OpenSlotSelect(LearnedSkill skill)
    {
        pendingSkill = skill;
        slotSelectPopup.SetActive(true);

        if (popupTitle != null)
            popupTitle.text = $"Gán \"{skill.data.skillName}\" vào slot nào?";

        // Ẩn slot button nếu vượt quá số slot được mở
        int unlockedSlots = GetUnlockedSlotCount();
        for (int i = 0; i < slotSelectButtons.Count; i++)
            slotSelectButtons[i].gameObject.SetActive(i < unlockedSlots);

        // Pause game khi chọn slot
        Time.timeScale = 0f;
    }

    void AssignToSlot(int index)
    {
        if (pendingSkill == null || index >= slots.Count) return;

        slots[index].assignedSkill = pendingSkill;
        RefreshSlot(index);

        slotSelectPopup.SetActive(false);
        Time.timeScale = 1f;
        pendingSkill = null;

        Debug.Log($"[Skill] Gán {slots[index].assignedSkill.data.skillName} vào slot {index + 1}");
    }

    void UseSkill(int index)
    {
        Debug.Log($"[Skill] UseSkill called frame:{Time.frameCount}");

        if (index >= slots.Count) return;
        var skill = slots[index].assignedSkill;
        if (skill == null || skill.data == null)
        {
            ShowNotification($"Slot {index + 1} chưa gán skill!");
            return;
        }

        // Kiểm tra cooldown
        if (cooldownTimers[index] > 0)
        {
            Debug.Log($"[Skill] Đang hồi chiêu: {cooldownTimers[index]:F1}s");
            return;
        }

        // Kiểm tra mana
        if (playerStats != null && !playerStats.UseMana(skill.data.manaCost))
        {
            Debug.Log("[Skill] Không đủ mana!");
            return;
        }

        // Set cooldown
        cooldownTimers[index] = skill.data.cooldown;

        // 1. Chạy animation 1 lần (không dùng StartCombo)
        var anim = FindFirstObjectByType<ComboAttackController>()?.GetComponent<Animator>();
        if (anim != null && !string.IsNullOrEmpty(skill.data.animTrigger))
        {
            anim.Play(skill.data.animTrigger); // play thẳng clip, không qua combo
        }

        // 2. Spawn slash effect
        var playerAnim = FindFirstObjectByType<PlayerAnimation>();
        if (skill.data.effectPrefab != null && playerAnim != null)
        {
            Vector2 dir = playerAnim.LastDirection;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            Vector3 spawnPos = playerAnim.transform.position + (Vector3)(dir * 0.5f);
            var fx = Instantiate(skill.data.effectPrefab, spawnPos, Quaternion.Euler(0, 0, angle));
            Destroy(fx, skill.data.effectDuration);
        }

        Debug.Log($"[Skill] Dùng: {skill.data.skillName} | Cooldown: {skill.data.cooldown}s");
    }

    // Số slot mở theo level
    int GetUnlockedSlotCount()
    {
        if (playerStats == null) return 3;
        int level = playerStats.level;
        if (level >= 20) return 6;
        if (level >= 10) return 4;
        return 3;
    }

    void RefreshSlot(int index)
    {
        if (index >= slots.Count) return;
        var slot = slots[index];
        if (slot == null || slot.skillIcon == null) return;

        if (slot.assignedSkill != null && slot.assignedSkill.data != null && slot.assignedSkill.data.icon != null)
        {
            slot.skillIcon.sprite = slot.assignedSkill.data.icon;
            slot.skillIcon.enabled = true;
            if (slot.slotBg != null && slot.activeSprite != null)
                slot.slotBg.sprite = slot.activeSprite;
        }
        else
        {
            if (slot.skillIcon != null) slot.skillIcon.enabled = false;
            if (slot.slotBg != null && slot.emptySprite != null)
                slot.slotBg.sprite = slot.emptySprite;
        }
    }

    void RefreshAllSlots()
    {
        for (int i = 0; i < slots.Count; i++)
            RefreshSlot(i);
    }

    void ShowNotification(string msg)
    {
        if (notificationText != null) notificationText.text = msg;
        if (notificationPanel != null) notificationPanel.SetActive(true);
        StopCoroutine(nameof(HideNotification));
        StartCoroutine(nameof(HideNotification));
    }

    IEnumerator HideNotification()
    {
        yield return new WaitForSeconds(2f);
        if (notificationPanel != null) notificationPanel.SetActive(false);
    }

    public void ClosePopup()
    {
        slotSelectPopup.SetActive(false);
        Time.timeScale = 1f;
        pendingSkill = null;
    }
}