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
    private ItemData equippedWeapon;
    public ItemData EquippedWeapon => equippedWeapon;
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Nếu chưa gán nút Use trong Inspector thì tự tạo 1 nút trong panel detail
        EnsureUseButton();
        btnUse.onClick.AddListener(OnUseButton);
        equippedWeapon = WeaponController.Instance?.Current;
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
        if (equippedWeapon == stack.data && !items.Exists(i => i != stack && i.data == stack.data))
        {
            WeaponController.Instance?.Equip(null);
            equippedWeapon = null;
        }

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
        UpdateUseButtonLabel(d);
    }

    void ClearDetail()
    {
        if (itemName != null) itemName.text = "";
        if (itemRarity != null) itemRarity.text = "";
        if (itemDesc != null) itemDesc.text = "";
        if (itemType != null) itemType.text = "";
        if (itemValue != null) itemValue.text = "";
        if (itemIcon != null) itemIcon.enabled = false;
        UpdateUseButtonLabel(null);
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

        if (item.type == ItemType.Weapon)
        {
            ToggleEquip(item);
            return;
        }
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
    // chance weapon
    void ToggleEquip(ItemData item)
    {
        var wc = WeaponController.Instance;
        if (wc == null) return;

        bool unequip = equippedWeapon == item;
        if (wc.Equip(unequip ? null : item))
        {
            equippedWeapon = unequip ? null : item;
            RefreshGrid();
            UpdateUseButtonLabel(item);
        }
    }

    // ── NÚT USE TRONG PANEL DETAIL ────────────────────
    // Nếu chưa gán btnUse trong Inspector thì tự tạo 1 nút ở đáy panel detail.
    // Nút tự đổi nhãn: EQUIP / UNEQUIP / USE tùy loại đồ đang chọn.
    void EnsureUseButton()
    {
        if (btnUse != null) return;

        Transform detailParent = itemName != null ? itemName.transform.parent : transform;

        var go = new GameObject("UseButton (Auto)",
            typeof(RectTransform), typeof(Button), typeof(Image));
        go.transform.SetParent(detailParent, false);
        go.transform.SetAsLastSibling();

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 12f);
        rt.sizeDelta = new Vector2(180f, 46f);

        var img = go.GetComponent<Image>();
        img.color = new Color(0.85f, 0.62f, 0.18f); // vàng đồng cho dễ thấy

        var txtGo = new GameObject("Label", typeof(RectTransform));
        txtGo.transform.SetParent(go.transform, false);
        var txtRt = txtGo.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;

        var txt = txtGo.AddComponent<TextMeshProUGUI>();
        // Mượn font của text có sẵn trong scene cho chắc ăn
        if (itemName != null)
        {
            txt.font = itemName.font;
            txt.fontSharedMaterial = itemName.fontSharedMaterial;
        }
        txt.text = "USE";
        txt.fontSize = 22;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.black;

        btnUse = go.GetComponent<Button>();
        Debug.Log("[Inventory] Đã tự tạo nút USE trong panel detail (hoặc double-click vào ô đồ).");
    }

    void UpdateUseButtonLabel(ItemData d)
    {
        if (btnUse == null) return;
        var txt = btnUse.GetComponentInChildren<TextMeshProUGUI>();
        if (txt == null) return;

        if (d != null && d.type == ItemType.Weapon)
            txt.text = (equippedWeapon == d) ? "UNEQUIP" : "EQUIP";
        else
            txt.text = "USE";
    }
}

[System.Serializable]
public class ItemStack
{
    public ItemData data;
    public int amount;
    public ItemStack(ItemData d, int a) { data = d; amount = a; }
}