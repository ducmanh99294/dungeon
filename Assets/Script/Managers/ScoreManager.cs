// ScoreManager.cs — client: điểm, kho, xuất hàng, bảng xếp hạng.
// Gắn vào 1 GameObject trong scene. Kéo UI Text vào các field (để trống vẫn chạy).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class WarehouseData { public string playerId; public Dictionary<string, int> warehouse; }
// Unity JsonUtility không parse Dictionary — dùng class trung gian
[Serializable]
public class WarehouseRaw
{
    public string playerId;
    public string json; // server gửi object, parse tay
}

[Serializable]
public class ScoreUpdatedData
{
    public string playerId;
    public int points;
    public int gained;
    public string reason;
}

[Serializable]
public class LeaderboardEntry
{
    public string playerId;
    public string username;
    public int points;
}
[Serializable]
public class LeaderboardData { public LeaderboardEntry[] entries; }

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [Header("UI (tùy chọn)")]
    public Text scoreText;       // "Điểm: 120"
    public Text warehouseText;   // "Kho: carrot x3"
    public Text leaderboardText; // bảng xếp hạng
    public Text matchResultText; // "THẮNG!" / "Hạng 2"
    public Button shipButton;    // nút Xuất hàng

    public int MyPoints { get; private set; }
    public Dictionary<string, int> MyWarehouse { get; private set; } = new Dictionary<string, int>();

    string myPlayerId;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (NetworkManager.Instance != null)
        {
            myPlayerId = NetworkManager.Instance.PlayerId;
            NetworkManager.Instance.OnWarehouseUpdated += HandleWarehouse;
            NetworkManager.Instance.OnScoreUpdated += HandleScore;
            NetworkManager.Instance.OnLeaderboardUpdate += HandleLeaderboard;
            NetworkManager.Instance.OnMatchEnded += HandleMatchEnded;
            NetworkManager.Instance.OnConnected += RequestScoreData;
            RequestScoreData();
        }

        if (shipButton != null)
            shipButton.onClick.AddListener(ShipGoods);

        if (matchResultText != null)
            matchResultText.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnWarehouseUpdated -= HandleWarehouse;
            NetworkManager.Instance.OnScoreUpdated -= HandleScore;
            NetworkManager.Instance.OnLeaderboardUpdate -= HandleLeaderboard;
            NetworkManager.Instance.OnMatchEnded -= HandleMatchEnded;
            NetworkManager.Instance.OnConnected -= RequestScoreData;
        }
    }

    void RequestScoreData()
    {
        if (NetworkManager.Instance != null && NetworkManager.Instance.IsConnected)
        {
            NetworkManager.Instance.Emit("request_score", "{}");
            Debug.Log("[Score] Đã gửi request_score");
        }
    }

    // Nút Xuất hàng gọi
    public void ShipGoods()
    {
        // Chỉ ban ngày mới xuất được (server cũng check)
        if (DayNightManager.Instance != null && !DayNightManager.Instance.IsDay())
        {
            Debug.Log("[Score] Chỉ xuất hàng ban ngày!");
            return;
        }
        NetworkManager.Instance?.Emit("ship_goods", "{}");
        Debug.Log("[Score] Gửi yêu cầu xuất hàng");
    }

    // ── NETWORK ───────────────────────────────────────────────────────

    void HandleWarehouse(string json)
    {
        // Parse tay vì warehouse là object động {carrot: 3, corn: 1}
        try
        {
            var dict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            if (dict.TryGetValue("warehouse", out object wh))
            {
                var whDict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, int>>(wh.ToString());
                string pid = dict["playerId"].ToString();
                if (pid == myPlayerId)
                {
                    MyWarehouse = whDict;
                    RefreshWarehouseUI();
                }
            }
        }
        catch (Exception e) { Debug.LogWarning($"[Score] Parse warehouse lỗi: {e.Message}"); }
    }

    void HandleScore(ScoreUpdatedData data)
    {
        if (data.playerId == myPlayerId)
        {
            MyPoints = data.points;
            RefreshScoreUI();
            if (data.gained > 0)
                Debug.Log($"[Score] +{data.gained} điểm! (Tổng: {data.points})");
        }
    }

    void HandleLeaderboard(LeaderboardData data)
    {
        if (leaderboardText == null || data.entries == null) return;
        var sb = new System.Text.StringBuilder("BXH\n");
        for (int i = 0; i < data.entries.Length; i++)
        {
            var e = data.entries[i];
            string mark = e.playerId == myPlayerId ? " ← bạn" : "";
            sb.AppendLine($"{i + 1}. {e.username}: {e.points}{mark}");
        }
        leaderboardText.text = sb.ToString();
    }

    void HandleMatchEnded()
    {
        // Server đã tính winner — client tự check mình hạng mấy từ BXH cuối
        // (đơn giản: hiện điểm của mình, chi tiết hơn làm sau)
        if (matchResultText != null)
        {
            matchResultText.gameObject.SetActive(true);
            matchResultText.text = $"Hết trận!\nĐiểm của bạn: {MyPoints}";
        }
        Debug.Log($"[Score] Match kết thúc. Điểm của bạn: {MyPoints}");
    }

    // ── UI ────────────────────────────────────────────────────────────

    void RefreshScoreUI()
    {
        if (scoreText != null)
            scoreText.text = $"Điểm: {MyPoints}";
    }

    void RefreshWarehouseUI()
    {
        if (warehouseText == null) return;
        if (MyWarehouse.Count == 0)
        {
            warehouseText.text = "Kho: trống";
            return;
        }
        var sb = new System.Text.StringBuilder("Kho: ");
        foreach (var kv in MyWarehouse)
            sb.Append($"{kv.Key} x{kv.Value}  ");
        warehouseText.text = sb.ToString();
    }
}
