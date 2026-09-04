using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Mana")]
    public float maxMana = 100f;
    public float currentMana = 100f;
    public float manaRegen = 2f; // h?i mana/giây

    [Header("Energy / Stamina")]
    public float maxEnergy = 100f;
    public float currentEnergy = 100f;
    public float energyDrain = 20f; // hao khi ch?y/giây
    public float energyRegen = 10f; // h?i khi không ch?y/giây
    public float energyRegenDelay = 1f; // delay tr??c khi h?i
    private float energyRegenTimer = 0f;

    [Header("Temperature")]
    public float maxTemp = 100f;
    public float currentTemp = 50f; // 0=l?nh, 100=nóng, 50=bình th??ng

    [Header("EXP")]
    public float maxExp = 100f;
    public float currentExp = 0f;
    public int level = 1;

    [Header("References")]
    public PlayerHUD hud;
    public PlayerMovement playerMovement; // ?? bi?t ?ang ch?y không

    public AttributeSystem attributeSystem;
    void Start()
    {
        UpdateAllHUD();
    }

    void Update()
    {
        HandleEnergyDrain();
        HandleManaRegen();
    }

    // ?? ENERGY ??????????????????????????????
    void HandleEnergyDrain()
    {
        bool isRunning = playerMovement != null && playerMovement.IsRunning
                         && IsMoving();

        if (isRunning)
        {
            currentEnergy -= energyDrain * Time.deltaTime;
            currentEnergy = Mathf.Max(currentEnergy, 0f);
            energyRegenTimer = energyRegenDelay;
        }
        else
        {
            if (energyRegenTimer > 0f)
                energyRegenTimer -= Time.deltaTime;
            else
            {
                currentEnergy += energyRegen * Time.deltaTime;
                currentEnergy = Mathf.Min(currentEnergy, maxEnergy);
            }
        }

        hud?.UpdateEnergy(currentEnergy, maxEnergy);

        // H?t energy ? ép v? walk
        if (currentEnergy <= 0f && playerMovement != null)
            playerMovement.ForceWalk();
    }

    bool IsMoving()
    {
        return Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0;
    }

    // ?? MANA ????????????????????????????????
    void HandleManaRegen()
    {
        if (currentMana >= maxMana) return;
        currentMana += manaRegen * Time.deltaTime;
        currentMana = Mathf.Min(currentMana, maxMana);
        hud?.UpdateMana(currentMana, maxMana);
    }

    public bool UseMana(float amount)
    {
        if (currentMana < amount) return false;
        currentMana -= amount;
        hud?.UpdateMana(currentMana, maxMana);
        return true;
    }

    // ?? TEMPERATURE ?????????????????????????
    public void ChangeTemperature(float delta)
    {
        currentTemp += delta;
        currentTemp = Mathf.Clamp(currentTemp, 0f, maxTemp);
        hud?.UpdateTemperature(currentTemp, maxTemp);
    }

    // ?? EXP ?????????????????????????????????
    public void GainEXP(float amount)
    {
        currentExp += amount;
        while (currentExp >= maxExp)
        {
            currentExp -= maxExp;
            LevelUp();
        }
        hud?.UpdateEXP(currentExp, maxExp);
    }

    void LevelUp()
    {
        level++;
        attributeSystem?.OnLevelUp(5);
        Debug.Log($"[Stats] Level Up! Level {level}");
        //UpdateAllHUD();
    }

    // ?? HELPERS ?????????????????????????????
    void UpdateAllHUD()
    {
        hud?.UpdateMana(currentMana, maxMana);
        hud?.UpdateEnergy(currentEnergy, maxEnergy);
        hud?.UpdateTemperature(currentTemp, maxTemp);
        hud?.UpdateEXP(currentExp, maxExp);
    }
}