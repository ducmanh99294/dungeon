// AttributeSystem.cs — gắn vào SkillsPanel
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class AttributeSystem : MonoBehaviour
{
    [System.Serializable]
    public class StatRow
    {
        public string statName;
        public TMP_Text valueText;
        public Button plusButton;
        public int baseValue = 1;
        public int bonusValue = 0;  // từ equipment
        public int Value => baseValue + bonusValue;
    }

    [Header("Attribute Points")]
    public int availablePoints = 0;
    public TMP_Text pointsText;

    [Header("Stat Rows")]
    public StatRow STR; // Strength  — tăng ATK
    public StatRow VIT; // Vitality  — tăng HP
    public StatRow AGI; // Agility   — tăng SPD
    public StatRow INT; // Intelligence — tăng Mana
    public StatRow END; // Endurance — tăng Energy
    public StatRow LUK; // Luck      — tăng drop rate

    [Header("References")]
    public PlayerStats playerStats;
    public Health playerHealth;

    private List<StatRow> allStats;

    void Start()
    {
        allStats = new List<StatRow> { STR, VIT, AGI, INT, END, LUK };

        // Gắn event cho từng nút +
        STR.plusButton.onClick.AddListener(() => AddPoint(STR));
        VIT.plusButton.onClick.AddListener(() => AddPoint(VIT));
        AGI.plusButton.onClick.AddListener(() => AddPoint(AGI));
        INT.plusButton.onClick.AddListener(() => AddPoint(INT));
        END.plusButton.onClick.AddListener(() => AddPoint(END));
        LUK.plusButton.onClick.AddListener(() => AddPoint(LUK));

        RefreshUI();
    }

    // Gọi từ PlayerStats khi level up
    public void OnLevelUp(int bonusPoints = 3)
    {
        availablePoints += bonusPoints;
        RefreshUI();
    }

    void AddPoint(StatRow stat)
    {
        if (availablePoints <= 0)
        {
            Debug.Log("[Attributes] Không còn điểm!");
            return;
        }

        availablePoints--;
        stat.baseValue++;
        ApplyStatEffect(stat);
        RefreshUI();
    }

    void ApplyStatEffect(StatRow stat)
    {
        if (playerHealth == null || playerStats == null) return;

        switch (stat.statName)
        {
            case "STR": // tăng ATK — áp dụng trong combat
                Debug.Log($"[Attributes] ATK tăng: {stat.Value}");
                break;
            case "VIT": // tăng max HP
                playerHealth.maxHP += 5;
                playerHealth.currentHP = Mathf.Min(playerHealth.currentHP + 5, playerHealth.maxHP);
                break;
            case "AGI": // tăng tốc độ
                // playerMovement.runSpeed += 0.2f;
                break;
            case "INT": // tăng max Mana
                playerStats.maxMana += 5;
                break;
            case "END": // tăng max Energy
                playerStats.maxEnergy += 5;
                break;
            case "LUK": // tăng drop rate — xử lý trong LootDropper
                Debug.Log($"[Attributes] LUK: {stat.Value}");
                break;
        }
    }

    void RefreshUI()
    {
        pointsText.text = $"Available Points: {availablePoints}";

        foreach (var stat in allStats)
        {
            stat.valueText.text = stat.Value.ToString();
            // Tắt nút + nếu hết điểm
            stat.plusButton.interactable = availablePoints > 0;
        }
    }

    // Cộng bonus từ equipment
    public void ApplyEquipmentBonus(StatRow stat, int bonus)
    {
        stat.bonusValue += bonus;
        RefreshUI();
    }

    public void RemoveEquipmentBonus(StatRow stat, int bonus)
    {
        stat.bonusValue -= bonus;
        RefreshUI();
    }

    // Getter cho các system khác
    public int GetSTR() => STR.Value;
    public int GetVIT() => VIT.Value;
    public int GetAGI() => AGI.Value;
    public int GetINT() => INT.Value;
    public int GetEND() => END.Value;
    public int GetLUK() => LUK.Value;
}