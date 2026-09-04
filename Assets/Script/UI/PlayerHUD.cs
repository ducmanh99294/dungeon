using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    [Header("References")]
    public Health playerHealth;
    public PlayerStats playerStats; // script ch?a Mana, Energy, Temp, EXP — t?o sau

    [Header("HP")]
    public Image hpFill; // Radial 360

    [Header("Mana")]
    public Image manaFill;

    [Header("Energy / Stamina")]
    public Image energyFill;

    [Header("Temperature")]
    public Image tempFill;
    public Gradient tempGradient; // xanh l?nh ? ?? nóng

    [Header("EXP")]
    public Image expFill;

    void Start()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += UpdateHP;
            UpdateHP(playerHealth.currentHP, playerHealth.maxHP);
        }
    }

    void UpdateHP(int current, int max)
    {
        if (hpFill == null) return;
        hpFill.fillAmount = (float)current / max;
    }

    // G?i t? PlayerStats khi mana thay ??i
    public void UpdateMana(float current, float max)
    {
        if (manaFill == null) return;
        manaFill.fillAmount = current / max;
    }

    // G?i t? PlayerStats khi energy thay ??i
    public void UpdateEnergy(float current, float max)
    {
        if (energyFill == null) return;
        float fill = current / max;
        energyFill.fillAmount = current / max;
    }

    // G?i t? PlayerStats khi nhi?t ?? thay ??i
    public void UpdateTemperature(float current, float max)
    {
        if (tempFill == null) return;
        float t = current / max;
        tempFill.fillAmount = t;
        // ??i màu theo nhi?t ??: l?nh=xanh, nóng=??
        if (tempGradient != null)
            tempFill.color = tempGradient.Evaluate(t);
    }

    // G?i t? PlayerStats khi EXP thay ??i
    public void UpdateEXP(float current, float max)
    {
        if (expFill == null) return;
        expFill.fillAmount = current / max;
    }
}