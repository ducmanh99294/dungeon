// ItemData.cs — ScriptableObject định nghĩa item
using UnityEngine;

public enum ItemType { Weapon, Armor, Helmet, Ring, Consumable, Material }
public enum ItemRarity { Common, Uncommon, Rare, Epic, Legendary }

[CreateAssetMenu(fileName = "ItemData", menuName = "Terra Reclaim/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Basic Info")]
    public string itemName;
    public Sprite icon;
    public ItemType type;
    public ItemRarity rarity;

    [Header("Description")]
    [TextArea(2, 4)]
    public string description;

    [Header("Stats")]
    public int attackBonus;
    public int defenseBonus;
    public int hpBonus;
    public int manaBonus;

    [Header("Skill Book (chỉ dùng nếu là sách skill)")]
    public bool isSkillBook = false;
    public SkillData skillData;  // kéo SkillData vào nếu là sách skill
    public int sellValue;
    public float weight;
    public int maxStack = 1; // >1 cho consumable/material

    [Header("Weapon Animation")]
    public RuntimeAnimatorController weaponAnimatorController;
    public GameObject slashEffectPrefab;

    public Color GetRarityColor()
    {
        return rarity switch
        {
            ItemRarity.Common => Color.white,
            ItemRarity.Uncommon => new Color(0.3f, 1f, 0.3f),
            ItemRarity.Rare => new Color(0.3f, 0.5f, 1f),
            ItemRarity.Epic => new Color(0.8f, 0.3f, 1f),
            ItemRarity.Legendary => new Color(1f, 0.6f, 0.1f),
            _ => Color.white
        };
    }
}