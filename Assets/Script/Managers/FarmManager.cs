// FarmManager.cs — client: hiển thị ruộng + tương tác trồng/thu hoạch.
// Gắn vào 1 GameObject trong scene. Mỗi player thấy ruộng của mình.
// Click ô trống → trồng (loại cây đang chọn). Click ô chín → thu hoạch.
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlotData
{
    public string plotId;
    public string ownerId;
    public int gridX;
    public int gridY;
    public string state; // empty | growing | ready
    public string cropType;
    public int readyInSec;
}

[Serializable]
public class FarmPlotsData { public PlotData[] plots; }
[Serializable]
public class PlotUpdatedData { public PlotData plot; }

public class FarmManager : MonoBehaviour
{
    public static FarmManager Instance;

    [Header("Ruộng")]
    [Tooltip("Khoảng cách giữa các ô (unit)")]
    public float plotSpacing = 1.2f;
    [Tooltip("Vị trí góc ruộng (tính từ player)")]
    public Vector2 farmOffset = new Vector2(-2f, -2f);

    [Header("Đặt ô đất tay (tùy chọn)")]
    [Tooltip("Kéo 6 GameObject vào đây theo thứ tự ô 0→5 để tự đặt vị trí. Để trống = tự tạo lưới quanh player.")]
    public Transform[] manualPlots = new Transform[6];

    [Header("Màu ô đất (nếu không gán sprite)")]
    public Color emptyColor = new Color(0.45f, 0.30f, 0.18f);
    public Color growingColor = new Color(0.30f, 0.55f, 0.25f);
    public Color readyColor = new Color(0.95f, 0.80f, 0.20f);

    [Header("Sprite tùy chọn (để trống = dùng màu)")]
    public Sprite emptySprite;
    public Sprite growingSprite;
    public Sprite readySprite;

    // Loại cây đang chọn để trồng (UI gọi SetSelectedCrop để đổi)
    public string selectedCropType = "carrot";

    Dictionary<string, PlotData> plots = new Dictionary<string, PlotData>();
    Dictionary<string, GameObject> plotObjects = new Dictionary<string, GameObject>();
    Dictionary<string, TextMesh> plotLabels = new Dictionary<string, TextMesh>();
    Dictionary<string, float> plotCountdown = new Dictionary<string, float>();
    Transform playerTransform;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnFarmPlots += HandleFarmPlots;
            NetworkManager.Instance.OnPlotUpdated += HandlePlotUpdated;
            NetworkManager.Instance.OnConnected += RequestFarmData;
            RequestFarmData(); // thử ngay (nếu đã kết nối)
        }
    }

    void OnDestroy()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnFarmPlots -= HandleFarmPlots;
            NetworkManager.Instance.OnPlotUpdated -= HandlePlotUpdated;
            NetworkManager.Instance.OnConnected -= RequestFarmData;
        }
    }

    // Gửi lại khi mới kết nối (tránh bị drop lúc socket chưa sẵn sàng)
    void RequestFarmData()
    {
        if (NetworkManager.Instance != null && NetworkManager.Instance.IsConnected)
        {
            NetworkManager.Instance.Emit("request_farm", "{}");
            Debug.Log("[Farm] Đã gửi request_farm");
        }
    }

    // UI gọi để đổi loại cây
    public void SetSelectedCrop(string cropType)
    {
        selectedCropType = cropType;
        Debug.Log($"[Farm] Chọn cây: {cropType}");
    }

    // ── NETWORK ───────────────────────────────────────────────────────

    void HandleFarmPlots(FarmPlotsData data)
    {
        foreach (var plot in data.plots)
        {
            plots[plot.plotId] = plot;
            CreateOrUpdatePlotVisual(plot);
        }
    }

    void HandlePlotUpdated(PlotUpdatedData data)
    {
        plots[data.plot.plotId] = data.plot;
        CreateOrUpdatePlotVisual(data.plot);
    }

    // Lấy số thứ tự ô từ plotId (dạng "..._plot_3" → 3)
    int GetPlotIndex(string plotId)
    {
        if (string.IsNullOrEmpty(plotId)) return -1;
        int last = plotId.LastIndexOf('_');
        if (last < 0 || last + 1 >= plotId.Length) return -1;
        if (int.TryParse(plotId.Substring(last + 1), out int idx)) return idx;
        return -1;
    }

    // ── VISUAL ────────────────────────────────────────────────────────

    void CreateOrUpdatePlotVisual(PlotData plot)
    {
        if (!plotObjects.TryGetValue(plot.plotId, out GameObject go))
        {
            go = new GameObject($"Plot_{plot.plotId}");
            go.transform.SetParent(transform);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 1;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one * 0.9f;

            var click = go.AddComponent<PlotClickHandler>();
            click.plotId = plot.plotId;
            click.manager = this;

            // Label hiển thị trạng thái (đếm ngược / tên cây)
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            labelGO.transform.localPosition = new Vector3(0, 0.7f, 0);
            var tm = labelGO.AddComponent<TextMesh>();
            tm.fontSize = 28;
            tm.characterSize = 0.08f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = Color.white;
            // Viền đen cho dễ đọc
            try { tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch { try { tm.font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
            plotLabels[plot.plotId] = tm;

            plotObjects[plot.plotId] = go;
        }

        // Vị trí: ưu tiên ô đặt tay, không có thì lưới quanh farmOffset (theo player nếu có)
        int plotIndex = GetPlotIndex(plot.plotId);
        Vector3 plotPos;
        if (manualPlots != null && plotIndex >= 0 && plotIndex < manualPlots.Length
            && manualPlots[plotIndex] != null)
        {
            plotPos = manualPlots[plotIndex].position;
        }
        else
        {
            Vector3 basePos = playerTransform != null
                ? playerTransform.position
                : transform.position;
            plotPos = basePos + new Vector3(
                farmOffset.x + plot.gridX * plotSpacing,
                farmOffset.y + plot.gridY * plotSpacing,
                0f);
        }
        go.transform.position = plotPos;

        RefreshPlotLabel(plot);

        var spriteRenderer = go.GetComponent<SpriteRenderer>();
        switch (plot.state)
        {
            case "growing":
                SetPlotAppearance(spriteRenderer, growingSprite, growingColor);
                break;
            case "ready":
                SetPlotAppearance(spriteRenderer, readySprite, readyColor);
                break;
            default:
                SetPlotAppearance(spriteRenderer, emptySprite, emptyColor);
                break;
        }
    }

    void Update()
    {
        // Đếm ngược mượt ở client (server chỉ gửi khi đổi trạng thái)
        var keys = new System.Collections.Generic.List<string>(plotCountdown.Keys);
        foreach (var plotId in keys)
        {
            plotCountdown[plotId] -= Time.deltaTime;
            if (plotLabels.TryGetValue(plotId, out TextMesh tm))
            {
                int sec = Mathf.Max(0, Mathf.CeilToInt(plotCountdown[plotId]));
                tm.text = sec > 0 ? sec + "s" : "...";
            }
            if (plotCountdown[plotId] <= -2f) plotCountdown.Remove(plotId);
        }
    }

    void RefreshPlotLabel(PlotData plot)
    {
        if (!plotLabels.TryGetValue(plot.plotId, out TextMesh tm)) return;
        // Đồng bộ countdown với server
        if (plot.state == "growing") plotCountdown[plot.plotId] = plot.readyInSec;
        else plotCountdown.Remove(plot.plotId);
        switch (plot.state)
        {
            case "growing":
                tm.text = plot.readyInSec > 0 ? plot.readyInSec + "s" : "...";
                tm.color = Color.white;
                break;
            case "ready":
                tm.text = (plot.cropType ?? "?") + "!";
                tm.color = Color.yellow;
                break;
            default:
                tm.text = "";
                break;
        }
        // Giảm readyInSec mỗi giây ở client để đếm ngược mượt
        // (server sẽ gửi cập nhật chính xác)
    }

    void SetPlotAppearance(SpriteRenderer sr, Sprite sprite, Color color)
    {
        if (sprite != null)
        {
            sr.sprite = sprite;
            sr.color = Color.white;
        }
        else
        {
            sr.sprite = MakeSquareSprite();
            sr.color = color;
        }
    }

    static Sprite squareSprite;
    static Sprite MakeSquareSprite()
    {
        if (squareSprite != null) return squareSprite;
        var tex = new Texture2D(32, 32);
        var pixels = new Color[32 * 32];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        squareSprite = Sprite.Create(tex, new Rect(0, 0, 32, 32),
            new Vector2(0.5f, 0.5f), 32f);
        return squareSprite;
    }

    // ── INTERACTION ───────────────────────────────────────────────────

    public void OnPlotClicked(string plotId)
    {
        if (!plots.TryGetValue(plotId, out PlotData plot)) return;

        if (plot.state == "empty")
        {
            // Trồng
            var payload = JsonUtility.ToJson(new PlantPayload
            {
                plotId = plotId,
                cropType = selectedCropType
            });
            NetworkManager.Instance.Emit("plant_crop", payload);
            ShowToast($"🌱 Đã trồng {selectedCropType}", new Color(0.5f, 0.9f, 0.5f));
        }
        else if (plot.state == "ready")
        {
            // Thu hoạch
            var payload = JsonUtility.ToJson(new HarvestPayload { plotId = plotId });
            NetworkManager.Instance.Emit("harvest_crop", payload);
            ShowToast($"🧺 Thu hoạch {plot.cropType}!", new Color(1f, 0.85f, 0.3f));
        }
        // growing: chưa làm gì được
    }

    [Serializable] class PlantPayload { public string plotId; public string cropType; }
    [Serializable] class HarvestPayload { public string plotId; }

    void ShowToast(string msg, Color? color = null)
    {
        var setup = FindFirstObjectByType<FarmGameSetup>();
        if (setup != null) setup.Toast(msg, color);
    }
}

// Gắn vào mỗi ô đất để bắt click
public class PlotClickHandler : MonoBehaviour
{
    public string plotId;
    public FarmManager manager;

    void OnMouseDown()
    {
        manager?.OnPlotClicked(plotId);
    }
}
