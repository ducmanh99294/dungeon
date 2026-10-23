// DayNightManager.cs — gắn vào GameObject DontDestroyOnLoad (cùng object với NetworkManager).
// Nhận trạng thái ngày/đêm từ server, đổi ánh sáng + bắn event cho các system khác (farm, UI...).
// Mọi thông số visual chỉnh trong Inspector, không cần đụng code.
using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[Serializable]
public class DayNightStateData
{
    public string phase;       // "day" | "night"
    public int cycle;
    public int totalCycles;
    public int timeLeftSec;
    public int dayDurationSec;
    public int nightDurationSec;
    public bool running;
}

public class DayNightManager : MonoBehaviour
{
    public static DayNightManager Instance;

    [Header("Ánh sáng ngày")]
    [Tooltip("Kéo Global Light 2D vào đây (để trống = tự tìm)")]
    public Light2D globalLight;
    public Color dayColor = new Color(1f, 0.96f, 0.88f);
    [Range(0f, 2f)] public float dayIntensity = 1f;

    [Header("Ánh sáng đêm")]
    public Color nightColor = new Color(0.35f, 0.45f, 0.75f);
    [Range(0f, 2f)] public float nightIntensity = 0.55f;

    [Header("Chuyển cảnh")]
    [Tooltip("Mất bao lâu để sáng/tối dần (giây)")]
    public float transitionDuration = 5f;

    // State hiện tại (server-authoritative)
    public string CurrentPhase { get; private set; } = "day";
    public int Cycle { get; private set; } = 1;
    public int TotalCycles { get; private set; } = 6;
    public int TimeLeftSec { get; private set; } = 0;
    public bool IsRunning { get; private set; } = false;

    // Events cho các system khác
    public event Action<DayNightStateData> OnStateUpdated;
    public event Action<string> OnPhaseChanged; // "day" | "night"
    public event Action OnMatchEnded;
    public event Action<DayNightStateData> OnDayNightUpdate;

    public event Action<FarmPlotsData> OnFarmPlots;
    public event Action<PlotUpdatedData> OnPlotUpdated;
    public event Action<string> OnWarehouseUpdated;
    public event Action<ScoreUpdatedData> OnScoreUpdated;
    public event Action<LeaderboardData> OnLeaderboardUpdate;
    Color targetColor;
    float targetIntensity;
    Color currentColor;
    float currentIntensity;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    void Start()
    {
        if (globalLight == null)
            globalLight = FindFirstObjectByType<Light2D>();

        currentColor = dayColor;
        currentIntensity = dayIntensity;
        targetColor = dayColor;
        targetIntensity = dayIntensity;

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnDayNightUpdate += HandleStateUpdate;
            NetworkManager.Instance.OnDayNightPhaseChanged += HandlePhaseChanged;
            NetworkManager.Instance.OnMatchEnded += HandleMatchEnded;
        }
    }

    void OnDestroy()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnDayNightUpdate -= HandleStateUpdate;
            NetworkManager.Instance.OnDayNightPhaseChanged -= HandlePhaseChanged;
            NetworkManager.Instance.OnMatchEnded -= HandleMatchEnded;
        }
    }

    void Update()
    {
        // Chuyển sáng/tối mượt
        if (globalLight != null)
        {
            float t = Mathf.Clamp01(Time.deltaTime / Mathf.Max(0.01f, transitionDuration));
            currentColor = Color.Lerp(currentColor, targetColor, t);
            currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, t);
            globalLight.color = currentColor;
            globalLight.intensity = currentIntensity;
        }
    }

    // ── PUBLIC API (các system khác gọi) ──────────────────────────────

    public bool IsDay() => CurrentPhase == "day";
    public bool IsNight() => CurrentPhase == "night";

    public string GetTimeString()
    {
        int m = TimeLeftSec / 60;
        int s = TimeLeftSec % 60;
        return $"{m:00}:{s:00}";
    }

    // ── SOCKET HANDLERS ───────────────────────────────────────────────

    void HandleStateUpdate(DayNightStateData data)
    {
        CurrentPhase = data.phase;
        Cycle = data.cycle;
        TotalCycles = data.totalCycles;
        TimeLeftSec = data.timeLeftSec;
        IsRunning = data.running;
        OnStateUpdated?.Invoke(data);
    }

    void HandlePhaseChanged(DayNightStateData data)
    {
        HandleStateUpdate(data);
        if (data.phase == "day")
        {
            targetColor = dayColor;
            targetIntensity = dayIntensity;
        }
        else
        {
            targetColor = nightColor;
            targetIntensity = nightIntensity;
        }
        Debug.Log($"[DayNight] Chuyển sang {(data.phase == "day" ? "NGÀY" : "ĐÊM")} — chu kỳ {data.cycle}/{data.totalCycles}");
        OnPhaseChanged?.Invoke(data.phase);
    }

    void HandleMatchEnded()
    {
        IsRunning = false;
        Debug.Log("[DayNight] Match kết thúc!");
        OnMatchEnded?.Invoke();
    }
}
