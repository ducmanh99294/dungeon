// DayNightSystem.cs — gắn vào GameManager hoặc Canvas
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DayNightSystem : MonoBehaviour
{
    public static DayNightSystem Instance;

    [Header("Time Settings")]
    public float dayDurationMinutes = 10f; // 1 ngày = bao nhiêu phút thực
    public float startHour = 6f;  // bắt đầu lúc 6:00 sáng

    [Header("UI")]
    public Image dayNightOverlay;
    public TMP_Text timeText;
    public Image moonIcon;
    public TMP_Text fullMoonText;

    [Header("Full Moon")]
    public int fullMoonCycle = 5; // trăng tròn mỗi 5 ngày

    [Header("Overlay Colors")]
    public Color dayColor = new Color(1f, 0.95f, 0.8f, 0f);    // ban ngày — trong suốt
    public Color nightColor = new Color(0.05f, 0.05f, 0.2f, 0.75f); // đêm — xanh tối
    public Color fullMoonColor = new Color(0.1f, 0f, 0.3f, 0.85f); // trăng tròn — tím đậm

    [Header("Gameplay Modifiers")]
    public float nightEnemyDamageMultiplier = 1.5f;
    public float nightDropRateMultiplier = 1.3f;
    public float fullMoonDamageMultiplier = 2f;
    public float fullMoonDropRateMultiplier = 2f;

    // State
    private float currentHour; // 0-24
    private int currentDay = 1;
    private bool isFullMoon = false;
    private bool isNight = false;

    // Events
    public System.Action OnDayStart;
    public System.Action OnNightStart;
    public System.Action<int> OnNewDay;      // int = day number
    public System.Action OnFullMoon;
    public System.Action OnFullMoonEnd;

    // Getters
    public bool IsNight => isNight;
    public bool IsFullMoon => isFullMoon;
    public int CurrentDay => currentDay;
    public float EnemyDamageMultiplier => isFullMoon ? fullMoonDamageMultiplier :
                                          isNight ? nightEnemyDamageMultiplier : 1f;
    public float DropRateMultiplier => isFullMoon ? fullMoonDropRateMultiplier :
                                          isNight ? nightDropRateMultiplier : 1f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        currentHour = startHour;
        UpdateUI();
        if (fullMoonText != null) fullMoonText.gameObject.SetActive(false);
        if (moonIcon != null) moonIcon.gameObject.SetActive(false);
    }

    void Update()
    {
        // Tăng giờ theo thời gian thực
        float hoursPerSecond = 24f / (dayDurationMinutes * 60f);
        currentHour += hoursPerSecond * Time.deltaTime;

        // Qua ngày mới
        if (currentHour >= 24f)
        {
            currentHour -= 24f;
            currentDay++;
            OnNewDay?.Invoke(currentDay);
            CheckFullMoon();
        }

        UpdateOverlay();
        UpdateDayNightState();
        UpdateUI();
    }

    void CheckFullMoon()
    {
        bool wasFullMoon = isFullMoon;
        isFullMoon = (currentDay % fullMoonCycle == 0);

        if (isFullMoon && !wasFullMoon)
        {
            OnFullMoon?.Invoke();
            Debug.Log($"[DayNight] 🌕 Trăng tròn! Ngày {currentDay}");
        }
        else if (!isFullMoon && wasFullMoon)
        {
            OnFullMoonEnd?.Invoke();
        }
    }

    void UpdateDayNightState()
    {
        bool wasNight = isNight;
        isNight = currentHour >= 20f || currentHour < 6f;

        if (isNight && !wasNight) OnNightStart?.Invoke();
        if (!isNight && wasNight) OnDayStart?.Invoke();
    }

    void UpdateOverlay()
    {
        if (dayNightOverlay == null) return;

        Color targetColor;

        if (isFullMoon && isNight)
            targetColor = fullMoonColor;
        else if (isNight)
            targetColor = nightColor;
        else
            targetColor = dayColor;

        dayNightOverlay.color = Color.Lerp(dayNightOverlay.color, targetColor, Time.deltaTime * 0.8f);
    }

    void UpdateUI()
    {
        // Hiện giờ "06:30"
        if (timeText != null)
        {
            int h = (int)currentHour;
            int m = (int)((currentHour - h) * 60f);
            timeText.text = $"{h:00}:{m:00}";
        }

        // Moon icon — hiện ban đêm
        if (moonIcon != null)
            moonIcon.gameObject.SetActive(isNight);

        // Full moon text
        if (fullMoonText != null)
            fullMoonText.gameObject.SetActive(isFullMoon && isNight);
    }

    // Gọi từ bên ngoài để skip sang ban đêm (debug)
    public void SetTime(float hour) => currentHour = Mathf.Clamp(hour, 0f, 24f);
}