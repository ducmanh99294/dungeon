// InventoryManager.cs — gắn vào MenuPanel
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("UI References")]
    public GameObject menuPanel;
    public GameObject inventoryPanel;

    [Header("Grid")]
    public Transform itemGridParent;  // kéo ItemGrid vào
    public GameObject slotPrefab;      // prefab 1 slot

    [Header("Item Detail")]
    public Image itemIcon;
    public TMP_Text itemName;
    public TMP_Text itemRarity;
    public TMP_Text itemDesc;
    public TMP_Text itemType;
    public TMP_Text itemValue;
    public Image rarityBorder;

    [Header("Stats")]
    public TMP_Text goldText;
    public TMP_Text weightText;

    [Header("Player Stats")]
    public float maxWeight = 100f;
    public int gold = 0;

    public Button btnUse;
    // Data
    private List<ItemStack> items = new List<ItemStack>();
    private List<ItemSlot> slots = new List<ItemSlot>();
    private float currentWeight = 0f;
    private bool isOpen = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        btnUse.onClick.AddListener(OnUseButton);

        // Xóa slot cũ nếu có
        foreach (Transform child in itemGridParent)
            Destroy(child.gameObject);

        // Tạo đúng 30 slot
        for (int i = 0; i < 30; i++)
        {
            var obj = Instantiate(slotPrefab, itemGridParent);
            var slot = obj.GetComponent<ItemSlot>();
            if (slot != null) slots.Add(slot);
        }

        menuPanel.SetActive(false);
        ClearDetail();
        UpdateStatsUI();
    }

    void Update()
    {
        // Toggle menu bằng phím I hoặc Tab
        if (Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.Tab))
            ToggleMenu();
    }

    public void ToggleMenu()
    {
        isOpen = !isOpen;
        menuPanel.SetActive(isOpen);
        if (isOpen) RefreshGrid();

        // Pause game khi mở menu
        Time.timeScale = isOpen ? 0f : 1f;
    }

    // ── THÊM ITEM ───────────────────────────────
    public bool AddItem(ItemData data, int amount = 1)
    {
        // Kiểm tra cân nặng
        if (currentWeight + data.weight * amount > maxWeight)
        {
            Debug.Log("[Inventory] Quá nặng!");
            return false;
        }

        // Stack nếu có thể
        if (data.maxStack > 1)
        {
            var existing = items.Find(i => i.data == data && i.amount < data.maxStack);
            if (existing != null)
            {
                existing.amount += amount;
                currentWeight += data.weight * amount;
                RefreshGrid();
                UpdateStatsUI();
                return true;
            }
        }

        // Thêm slot mới
        if (items.Count >= slots.Count)
        {
            Debug.Log("[Inventory] Túi đầy!");
            return false;
        }

        items.Add(new ItemStack(data, amount));
        currentWeight += data.weight * amount;
        RefreshGrid();
        UpdateStatsUI();
        return true;
    }

    // ── XÓA ITEM ────────────────────────────────
    public void RemoveItem(ItemStack stack)
    {
        currentWeight -= stack.data.weight * stack.amount;
        items.Remove(stack);
        RefreshGrid();
        UpdateStatsUI();
        ClearDetail();
    }

    // ── HIỂN THỊ GRID ───────────────────────────
    void RefreshGrid()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (i < items.Count)
                slots[i].SetItem(items[i]);
            else
                slots[i].ClearSlot();
        }
    }

    // ── HIỂN THỊ DETAIL ─────────────────────────
    public void ShowDetail(ItemStack stack)
    {
        if (stack == null) { ClearDetail(); return; }
        var d = stack.data;

        itemIcon.sprite = d.icon;
        itemIcon.enabled = d.icon != null;
        itemName.text = d.itemName;
        itemRarity.text = $"Rarity: {d.rarity}";
        itemRarity.color = d.GetRarityColor();
        itemDesc.text = d.description;
        itemType.text = $"Type: {d.type}";
        itemValue.text = $"Sell Value: {d.sellValue} G\nStack Size: {stack.amount}";
        rarityBorder.color = d.GetRarityColor();
    }

    void ClearDetail()
    {
        if (itemName != null) itemName.text = "";
        if (itemRarity != null) itemRarity.text = "";
        if (itemDesc != null) itemDesc.text = "";
        if (itemType != null) itemType.text = "";
        if (itemValue != null) itemValue.text = "";
        if (itemIcon != null) itemIcon.enabled = false;
    }

    void UpdateStatsUI()
    {
        if (goldText != null) goldText.text = $"{gold} G";
        if (weightText != null) weightText.text = $"{currentWeight:F1} / {maxWeight} kg";
    }

    // ── BUTTONS ─────────────────────────────────
    public void OnUseButton()
    {
        var selected = GetSelectedSlot();
        if (selected == null) return;

        var item = selected.data;

        // Nếu là Skill Book → trigger gacha
        if (item.isSkillBook && item.skillData != null)
        {
            RemoveItem(selected);
            GachaAnimationUI.Instance?.PlayGacha(item.skillData, 1);
            return;
        }

        // TODO: xử lý các loại consumable khác (potion...)
        Debug.Log($"[Inventory] Dùng: {item.itemName}");
    }

    public void OnDropButton()
    {
        var selected = GetSelectedSlot();
        if (selected == null) return;
        RemoveItem(selected);
    }

    public void OnTrashButton()
    {
        var selected = GetSelectedSlot();
        if (selected == null) return;
        RemoveItem(selected);
    }

    public void OnSortButton()
    {
        items.Sort((a, b) => a.data.type.CompareTo(b.data.type));
        RefreshGrid();
    }

    ItemStack GetSelectedSlot()
    {
        var selected = slots.Find(s => s.IsSelected);
        return selected?.CurrentStack;
    }

    public void AddGold(int amount)
    {
        gold += amount;
        UpdateStatsUI();
    }
}

[System.Serializable]
public class ItemStack
{
    public ItemData data;
    public int amount;
    public ItemStack(ItemData d, int a) { data = d; amount = a; }
}