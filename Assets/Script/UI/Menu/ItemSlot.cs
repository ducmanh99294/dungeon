// ItemSlot.cs — g?n vào m?i Slot trong ItemGrid
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ItemSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI")]
    public Image iconImage;
    public Image borderImage;
    public TMP_Text amountText;
    public Image bgImage;

    [Header("Sprites")]
    public Sprite normalBg;
    public Sprite selectedBg;
    public Sprite hoverBg;

    [Header("Rarity Sprites")]
    public Sprite[] raritySprites;
    public bool IsSelected { get; private set; }
    public ItemStack CurrentStack { get; private set; }

    private static ItemSlot currentSelected;

    public void SetItem(ItemStack stack)
    {
        CurrentStack = stack;

        iconImage.sprite = stack.data.icon;
        iconImage.enabled = stack.data.icon != null;
        if (borderImage != null && raritySprites != null)
        {
            int index = (int)stack.data.rarity;
            if (index < raritySprites.Length)
                borderImage.sprite = raritySprites[index];
        }

        // Hi?n s? l??ng n?u stack > 1
        if (stack.amount > 1)
        {
            amountText.text = stack.amount.ToString();
            amountText.enabled = true;
        }
        else
            amountText.enabled = false;
    }

    public void ClearSlot()
    {
        CurrentStack = null;
        iconImage.enabled = false;
        amountText.enabled = false;
        borderImage.color = Color.clear;
        IsSelected = false;
        if (bgImage != null) bgImage.sprite = normalBg;
    }

    // ?? EVENTS ??????????????????????????????????
    public void OnPointerClick(PointerEventData eventData)
    {
        if (CurrentStack == null) return;

        // Deselect slot c?
        if (currentSelected != null && currentSelected != this)
        {
            currentSelected.IsSelected = false;
            if (currentSelected.bgImage != null)
                currentSelected.bgImage.sprite = currentSelected.normalBg;
        }

        // Select slot này
        IsSelected = true;
        currentSelected = this;
        if (bgImage != null) bgImage.sprite = selectedBg;

        // Hi?n detail
        InventoryManager.Instance?.ShowDetail(CurrentStack);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsSelected && bgImage != null)
            bgImage.sprite = hoverBg;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!IsSelected && bgImage != null)
            bgImage.sprite = normalBg;
    }
}