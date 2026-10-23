// FarmGameSetup.cs v2 — UI hoàn chỉnh cho game farm.
// Gắn vào 1 GameObject trống, bấm Play là tự setup hết.
// Gồm: managers + UI polished (toast, banner, HP bar, nút cây chi tiết).
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FarmGameSetup : MonoBehaviour
{
    [Header("Màu chủ đạo")]
    public Color panelBg = new Color(0.08f, 0.10f, 0.16f, 0.85f);
    public Color accentColor = new Color(0.95f, 0.75f, 0.20f); // vàng gold
    public Color textColor = Color.white;
    public Color dimTextColor = new Color(0.7f, 0.7f, 0.75f);
    public int fontSize = 15;

    Font defaultFont;
    GameObject canvasGO;

    // UI refs
    Text dayNightText;
    Text scoreText;
    Text warehouseText;
    Text carriedText;
    Text leaderboardText;
    Text matchResultText;
    Slider hpBar;
    Text hpText;
    Transform toastContainer;
    Text phaseBannerText;
    GameObject phaseBannerGO;

    Dictionary<string, Button> cropButtons = new Dictionary<string, Button>();
    Color cropBtnNormal = new Color(0.16f, 0.35f, 0.65f);
    Color cropBtnSelected = new Color(0.85f, 0.55f, 0.10f);

    // Thông tin cây để hiện trên nút
    readonly Dictionary<string, (string name, int secs, int value)> cropInfo =
        new Dictionary<string, (string, int, int)>
    {
        { "carrot", ("Cà rốt", 60, 2) },
        { "corn", ("Bắp", 120, 5) },
        { "pumpkin", ("Bí ngô", 180, 10) },
    };

    void Start() { StartCoroutine(SetupRoutine()); }
    [ContextMenu("Setup Ngay")] public void SetupNow() { StartCoroutine(SetupRoutine()); }

    IEnumerator SetupRoutine()
    {
        defaultFont = GetBuiltinFont();
        float timeout = 10f;
        while (NetworkManager.Instance == null && timeout > 0)
        {
            yield return null;
            timeout -= Time.deltaTime;
        }
        SetupManagers();
        SetupUI();
        Debug.Log("[Setup] Hoàn tất — game sẵn sàng");
    }

    static Font GetBuiltinFont()
    {
        try { return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
        catch { }
        try { return Resources.GetBuiltinResource<Font>("Arial.ttf"); }
        catch { }
        return null;
    }

    // ── MANAGERS ──────────────────────────────────────────────────────

    void SetupManagers()
    {
        EnsureManager<DayNightManager>("DayNightManager");
        EnsureManager<FarmManager>("FarmManager");
        EnsureManager<ScoreManager>("ScoreManager");
        EnsureManager<TunnelManager>("TunnelManager");
        EnsureManager<RaidManager>("RaidManager");
    }

    T EnsureManager<T>(string name) where T : Component
    {
        var existing = FindFirstObjectByType<T>();
        if (existing != null) return existing;
        var go = new GameObject(name);
        return go.AddComponent<T>();
    }

    // ── UI ────────────────────────────────────────────────────────────

    void SetupUI()
    {
        if (canvasGO != null) return;

        canvasGO = new GameObject("GameCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        CreateTopBar();
        CreatePlayerPanel();
        CreateLeaderboardPanel();
        CreateInventoryPanel();
        CreateActionBar();
        CreateToastSystem();
        CreatePhaseBanner();
        CreateMatchResult();

        WireUI();
    }

    // --- Thanh trên: ngày/đêm ---
    void CreateTopBar()
    {
        var panel = CreatePanel("TopBar", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(420, 54), new Vector2(0, -35));
        AddTitleBar(panel, "THỜI GIAN");
        dayNightText = CreateText(panel, "DayNightText", 18, TextAnchor.MiddleCenter);
        // Đẩy text xuống dưới title
        var rt = dayNightText.GetComponent<RectTransform>();
        rt.offsetMin = new Vector2(10, 8); rt.offsetMax = new Vector2(-10, -22);
    }

    // --- Trái trên: điểm + HP ---
    void CreatePlayerPanel()
    {
        var panel = CreatePanel("PlayerPanel", new Vector2(0, 1f), new Vector2(0, 1f),
            new Vector2(280, 130), new Vector2(150, -120));
        AddTitleBar(panel, "NHÂN VẬT");
        var v = CreateVerticalLayout(panel, 26); // chừa chỗ title
        scoreText = CreateText(v, "ScoreText", fontSize, TextAnchor.MiddleLeft);

        // Thanh HP
        var hpGO = new GameObject("HPBar");
        hpGO.transform.SetParent(v.transform, false);
        var hpRt = hpGO.AddComponent<RectTransform>();
        hpRt.sizeDelta = new Vector2(0, 22);
        var hpBg = hpGO.AddComponent<Image>();
        hpBg.color = new Color(0.25f, 0.05f, 0.05f);
        hpBar = hpGO.AddComponent<Slider>();
        hpBar.minValue = 0; hpBar.maxValue = 100; hpBar.value = 100;
        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(hpGO.transform, false);
        var fillRt = fillGO.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero; fillRt.offsetMax = Vector2.zero;
        var fillImg = fillGO.AddComponent<Image>();
        fillImg.color = new Color(0.85f, 0.20f, 0.20f);
        hpBar.fillRect = fillRt;
        hpBar.targetGraphic = fillImg;
        // Text HP đè lên
        hpText = CreateText(hpGO, "HPText", 13, TextAnchor.MiddleCenter);
    }

    // --- Phải trên: BXH ---
    void CreateLeaderboardPanel()
    {
        var panel = CreatePanel("LeaderboardPanel", new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(280, 320), new Vector2(-150, -200));
        AddTitleBar(panel, "BẢNG XẾP HẠNG");
        leaderboardText = CreateText(panel, "LeaderboardText", fontSize, TextAnchor.UpperLeft);
        var rt = leaderboardText.GetComponent<RectTransform>();
        rt.offsetMin = new Vector2(12, 10); rt.offsetMax = new Vector2(-12, -26);
    }

    // --- Trái dưới: kho + mang theo ---
    void CreateInventoryPanel()
    {
        var panel = CreatePanel("InventoryPanel", new Vector2(0, 0f), new Vector2(0, 0f),
            new Vector2(280, 120), new Vector2(150, 90));
        AddTitleBar(panel, "KHO ĐỒ");
        var v = CreateVerticalLayout(panel, 26);
        warehouseText = CreateText(v, "WarehouseText", fontSize, TextAnchor.MiddleLeft);
        carriedText = CreateText(v, "CarriedText", fontSize, TextAnchor.MiddleLeft);
    }

    // --- Dưới giữa: xuất hàng + chọn cây ---
    void CreateActionBar()
    {
        var panel = CreatePanel("ActionBar", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(640, 96), new Vector2(0, 62));
        AddTitleBar(panel, "HÀNH ĐỘNG");
        var h = CreateHorizontalLayout(panel, 30);
        // Nút xuất hàng
        CreateActionButton(h, "ShipButton", "📦 Xuất hàng", new Color(0.15f, 0.55f, 0.25f), () =>
        {
            ScoreManager.Instance?.ShipGoods();
        });
        // Nút cây (có thông tin chi tiết)
        foreach (var kv in cropInfo)
            CreateCropButton(h, kv.Key, kv.Value.name, kv.Value.secs, kv.Value.value);
        HighlightCropButton("carrot");
    }

    // --- Toast notifications (góc phải dưới) ---
    void CreateToastSystem()
    {
        var go = new GameObject("ToastContainer");
        go.transform.SetParent(canvasGO.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f); rt.anchorMax = new Vector2(1f, 0f);
        rt.sizeDelta = new Vector2(340, 300);
        rt.anchoredPosition = new Vector2(-190, 220);
        var v = go.AddComponent<VerticalLayoutGroup>();
        v.spacing = 6;
        v.childAlignment = TextAnchor.LowerRight;
        v.childControlWidth = true; v.childControlHeight = false;
        toastContainer = go.transform;
    }

    // --- Banner chuyển phase (giữa màn hình) ---
    void CreatePhaseBanner()
    {
        phaseBannerGO = new GameObject("PhaseBanner");
        phaseBannerGO.transform.SetParent(canvasGO.transform, false);
        var rt = phaseBannerGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.6f); rt.anchorMax = new Vector2(0.5f, 0.6f);
        rt.sizeDelta = new Vector2(600, 100);
        rt.anchoredPosition = Vector2.zero;
        phaseBannerText = phaseBannerGO.AddComponent<Text>();
        phaseBannerText.font = defaultFont;
        phaseBannerText.fontSize = 48;
        phaseBannerText.fontStyle = FontStyle.Bold;
        phaseBannerText.color = accentColor;
        phaseBannerText.alignment = TextAnchor.MiddleCenter;
        // Viền đen
        var outline = phaseBannerGO.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, 2);
        phaseBannerGO.SetActive(false);
    }

    // --- Kết quả trận ---
    void CreateMatchResult()
    {
        var panel = CreatePanel("ResultPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(480, 220), Vector2.zero);
        matchResultText = CreateText(panel, "MatchResultText", 26, TextAnchor.MiddleCenter);
        panel.SetActive(false);
    }

    void WireUI()
    {
        var scoreMgr = FindFirstObjectByType<ScoreManager>();
        if (scoreMgr != null)
        {
            scoreMgr.scoreText = scoreText;
            scoreMgr.warehouseText = warehouseText;
            scoreMgr.leaderboardText = leaderboardText;
            scoreMgr.matchResultText = matchResultText;
        }
        var raidMgr = FindFirstObjectByType<RaidManager>();
        if (raidMgr != null) raidMgr.carriedText = carriedText;

        var updater = canvasGO.AddComponent<GameUIUpdater>();
        updater.dayNightText = dayNightText;
        updater.hpBar = hpBar;
        updater.hpText = hpText;
        updater.matchResultText = matchResultText;
        updater.setup = this;
    }

    // ── PUBLIC API (managers gọi) ─────────────────────────────────────

    public void Toast(string message, Color? color = null)
    {
        if (toastContainer == null) return;
        var go = new GameObject("Toast");
        go.transform.SetParent(toastContainer, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.1f, 0.12f, 0.18f, 0.92f);
        var txt = go.AddComponent<Text>();
        txt.font = defaultFont;
        txt.fontSize = 14;
        txt.color = color ?? textColor;
        txt.text = message;
        txt.alignment = TextAnchor.MiddleLeft;
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 36; le.preferredWidth = 320;
        // Padding trong
        var rt = txt.GetComponent<RectTransform>();
        // Tự hủy sau 3s với fade
        StartCoroutine(FadeToast(go, 3f));
    }

    IEnumerator FadeToast(GameObject go, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (go == null) yield break;
        var img = go.GetComponent<Image>();
        var txt = go.GetComponent<Text>();
        float t = 0;
        while (t < 0.5f && go != null)
        {
            t += Time.deltaTime;
            float a = 1f - t / 0.5f;
            if (img != null) { var c = img.color; c.a = 0.92f * a; img.color = c; }
            if (txt != null) { var c = txt.color; c.a = a; txt.color = c; }
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    public void ShowPhaseBanner(string text, Color color)
    {
        if (phaseBannerGO == null) return;
        phaseBannerText.text = text;
        phaseBannerText.color = color;
        phaseBannerGO.SetActive(true);
        StopCoroutine("HideBanner");
        StartCoroutine("HideBanner");
    }

    IEnumerator HideBanner()
    {
        yield return new WaitForSeconds(2.5f);
        if (phaseBannerGO != null) phaseBannerGO.SetActive(false);
    }

    // ── HELPERS ───────────────────────────────────────────────────────

    GameObject CreatePanel(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 anchoredPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(canvasGO.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;
        var img = go.AddComponent<Image>();
        img.color = panelBg;
        // Viền mỏng
        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(1, 1, 1, 0.12f);
        outline.effectDistance = new Vector2(1, 1);
        return go;
    }

    void AddTitleBar(GameObject panel, string title)
    {
        var go = new GameObject("Title");
        go.transform.SetParent(panel.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
        rt.sizeDelta = new Vector2(0, 24);
        rt.anchoredPosition = new Vector2(0, -12);
        var txt = go.AddComponent<Text>();
        txt.font = defaultFont;
        txt.fontSize = 12;
        txt.fontStyle = FontStyle.Bold;
        txt.color = accentColor;
        txt.text = title;
        txt.alignment = TextAnchor.MiddleCenter;
    }

    GameObject CreateVerticalLayout(GameObject parent, float topPad = 10)
    {
        var go = new GameObject("Vertical");
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(12, 10); rt.offsetMax = new Vector2(-12, -topPad);
        var layout = go.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 6;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlHeight = false;
        return go;
    }

    GameObject CreateHorizontalLayout(GameObject parent, float topPad = 10)
    {
        var go = new GameObject("Horizontal");
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(12, 10); rt.offsetMax = new Vector2(-12, -topPad);
        var layout = go.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.MiddleCenter;
        return go;
    }

    Text CreateText(GameObject parent, string name, int size, TextAnchor align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var txt = go.AddComponent<Text>();
        txt.font = defaultFont;
        txt.fontSize = size;
        txt.color = textColor;
        txt.text = "";
        txt.alignment = align;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = size + 8;
        return txt;
    }

    void CreateActionButton(GameObject parent, string name, string label, Color bg, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = bg;
        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(onClick);
        // Hiệu ứng bấm
        var colors = btn.colors;
        colors.pressedColor = new Color(bg.r * 0.7f, bg.g * 0.7f, bg.b * 0.7f);
        btn.colors = colors;

        var txt = CreateText(go, "Label", 15, TextAnchor.MiddleCenter);
        // Ghi đè layout vì nằm trong button
        var le = txt.GetComponent<LayoutElement>();
        if (le != null) Destroy(le);

        var le2 = go.AddComponent<LayoutElement>();
        le2.minWidth = 120; le2.minHeight = 48;
        // Set text sau vì CreateText tạo mới
        txt.text = label;
    }

    void CreateCropButton(GameObject parent, string cropType, string displayName, int secs, int value)
    {
        var go = new GameObject(cropType + "Btn");
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.color = cropBtnNormal;
        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(() =>
        {
            FarmManager.Instance?.SetSelectedCrop(cropType);
            HighlightCropButton(cropType);
        });
        var colors = btn.colors;
        colors.pressedColor = new Color(0.1f, 0.3f, 0.5f);
        btn.colors = colors;

        // Text 2 dòng: tên + thông tin
        var txtGO = new GameObject("Label");
        txtGO.transform.SetParent(go.transform, false);
        var txt = txtGO.AddComponent<Text>();
        txt.font = defaultFont;
        txt.fontSize = 13;
        txt.color = Color.white;
        txt.text = $"{displayName}\n{secs}s • {value}đ";
        txt.alignment = TextAnchor.MiddleCenter;
        var rt = txtGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        var le = go.AddComponent<LayoutElement>();
        le.minWidth = 110; le.minHeight = 56;
        cropButtons[cropType] = btn;
    }

    void HighlightCropButton(string selected)
    {
        foreach (var kv in cropButtons)
        {
            var img = kv.Value.GetComponent<Image>();
            if (img != null) img.color = kv.Key == selected ? cropBtnSelected : cropBtnNormal;
        }
    }
}

// Cập nhật UI mỗi frame
public class GameUIUpdater : MonoBehaviour
{
    public Text dayNightText;
    public Slider hpBar;
    public Text hpText;
    public Text matchResultText;
    public FarmGameSetup setup;

    void Update()
    {
        // Ngày/đêm
        if (dayNightText != null && DayNightManager.Instance != null)
        {
            var dm = DayNightManager.Instance;
            string icon = dm.IsDay() ? "☀" : "🌙";
            string phase = dm.IsDay() ? "NGÀY" : "ĐÊM";
            dayNightText.text = $"{icon} {phase}  {dm.GetTimeString()}   •   Chu kỳ {dm.Cycle}/{dm.TotalCycles}";
        }

        // HP (tìm player)
        if (hpBar != null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                // Thử lấy HP từ component Health nếu có
                var health = playerObj.GetComponent("Health");
                if (health != null)
                {
                    var hpProp = health.GetType().GetProperty("CurrentHP") ?? health.GetType().GetProperty("currentHP");
                    var maxProp = health.GetType().GetProperty("MaxHP") ?? health.GetType().GetProperty("maxHP");
                    // Fallback: dùng field
                    if (hpProp == null)
                    {
                        var hpField = health.GetType().GetField("currentHP") ?? health.GetType().GetField("CurrentHP");
                        var maxField = health.GetType().GetField("maxHP") ?? health.GetType().GetField("MaxHP");
                        if (hpField != null && maxField != null)
                        {
                            float cur = System.Convert.ToSingle(hpField.GetValue(health));
                            float max = System.Convert.ToSingle(maxField.GetValue(health));
                            hpBar.maxValue = max;
                            hpBar.value = cur;
                            if (hpText != null) hpText.text = $"{cur}/{max}";
                        }
                    }
                }
            }
        }
    }
}
