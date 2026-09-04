// SkillData.cs — ScriptableObject định nghĩa skill
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillData", menuName = "Terra Reclaim/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("Basic Info")]
    public string skillName;
    public string skillType;
    public Sprite icon;
    [TextArea(2, 4)]
    public string description;

    [Header("Stats")]
    public float cooldown;
    public float manaCost;
    public int maxLevel = 5;

    [Header("Unlock")]
    public string unlockRequirement; // mô tả yêu cầu mở khóa

    [Header("Animation")]
    public string animTrigger;

    [Header("Effect")]
    public GameObject effectPrefab;
    public float effectDuration = 1f;
}