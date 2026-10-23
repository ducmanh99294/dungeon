// RaidManager.cs — client: trộm cắp, đồ mang theo, PK.
// - Tạo marker kho cho mỗi nhà (từ dữ liệu tunnel)
// - Click kho nhà khác (ban đêm) → trộm
// - Click kho nhà mình → cất đồ mang theo vào kho
// - Click người chơi khác → đánh (ban đêm + trong dungeon)
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RaidManager : MonoBehaviour
{
    public static RaidManager Instance;

    [Header("Marker kho")]
    public Color ownWarehouseColor = new Color(0.3f, 0.7f, 0.3f);
    public Color enemyWarehouseColor = new Color(0.9f, 0.3f, 0.3f);

    [Header("UI (tùy chọn)")]
    public Text carriedText; // "Mang theo: carrot x2"

    Dictionary<string, GameObject> warehouseMarkers = new Dictionary<string, GameObject>();
    Dictionary<string, int> carried = new Dictionary<string, int>();
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
            NetworkManager.Instance.OnTunnelList += CreateWarehouseMarkers;
            NetworkManager.Instance.OnCarriedUpdated += HandleCarried;
            NetworkManager.Instance.OnStealAlert += HandleStealAlert;
            NetworkManager.Instance.OnPlayerKilled += HandlePlayerKilled;
            NetworkManager.Instance.OnConnected += RequestCarried;
            RequestCarried();
        }
    }

    void OnDestroy()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnTunnelList -= CreateWarehouseMarkers;
            NetworkManager.Instance.OnCarriedUpdated -= HandleCarried;
            NetworkManager.Instance.OnStealAlert -= HandleStealAlert;
            NetworkManager.Instance.OnPlayerKilled -= HandlePlayerKilled;
            NetworkManager.Instance.OnConnected -= RequestCarried;
        }
    }

    void RequestCarried()
    {
        if (NetworkManager.Instance != null && NetworkManager.Instance.IsConnected)
        {
            NetworkManager.Instance.Emit("request_carried", "{}");
            Debug.Log("[Raid] Đã gửi request_carried");
        }
    }

    // ── WAREHOUSE MARKERS ─────────────────────────────────────────────

    void CreateWarehouseMarkers(TunnelListData data)
    {
        if (data.tunnels == null) return;
        foreach (var t in data.tunnels)
        {
            string key = $"wh_{t.houseOwnerId}";
            if (warehouseMarkers.ContainsKey(key)) continue;

            var go = new GameObject($"Warehouse_{t.houseOwnerId}");
            go.transform.position = new Vector3(t.housePos.x + 2f, t.housePos.y, 0f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = TunnelManager.GetSquareSpritePublic();
            bool isMine = t.houseOwnerId == myPlayerId;
            sr.color = isMine ? ownWarehouseColor : enemyWarehouseColor;
            sr.sortingOrder = 1;
            go.transform.localScale = Vector3.one * 1.2f;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            var click = go.AddComponent<WarehouseClickHandler>();
            click.houseOwnerId = t.houseOwnerId;
            click.manager = this;

            warehouseMarkers[key] = go;
        }
    }

    public void OnWarehouseClicked(string houseOwnerId)
    {
        if (houseOwnerId == myPlayerId)
        {
            // Kho mình: cất đồ mang theo
            NetworkManager.Instance?.Emit("deposit_loot", "{}");
            Debug.Log("[Raid] Cất đồ mang theo vào kho");
        }
        else
        {
            // Kho người khác: trộm (chỉ ban đêm)
            if (DayNightManager.Instance != null && !DayNightManager.Instance.IsNight())
            {
                Debug.Log("[Raid] Chỉ trộm được ban đêm!");
                return;
            }
            var payload = JsonUtility.ToJson(new StealPayload { targetPlayerId = houseOwnerId });
            NetworkManager.Instance?.Emit("steal", payload);
            Debug.Log($"[Raid] Trộm kho của {houseOwnerId}");
        }
    }

    // ── PK ────────────────────────────────────────────────────────────

    public void AttackPlayer(string targetId)
    {
        var payload = JsonUtility.ToJson(new AttackPayload { targetId = targetId });
        NetworkManager.Instance?.Emit("attack_player", payload);
        Debug.Log($"[Raid] Tấn công {targetId}");
    }

    // ── NETWORK HANDLERS ──────────────────────────────────────────────

    void HandleCarried(string json)
    {
        try
        {
            var dict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            if (dict.TryGetValue("carried", out object c) && dict["playerId"].ToString() == myPlayerId)
            {
                carried = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, int>>(c.ToString());
                RefreshCarriedUI();
            }
        }
        catch (Exception e) { Debug.LogWarning($"[Raid] Parse carried lỗi: {e.Message}"); }
    }

    void HandleStealAlert(string json)
    {
        Debug.Log($"[Raid] CẢNH BÁO TRỘM: {json}");
        // TODO: hiện UI báo động cho nạn nhân
    }

    void HandlePlayerKilled(string json)
    {
        Debug.Log($"[Raid] Có người bị giết: {json}");
    }

    void RefreshCarriedUI()
    {
        if (carriedText == null) return;
        if (carried.Count == 0) { carriedText.text = "Mang theo: không có"; return; }
        var sb = new System.Text.StringBuilder("Mang theo: ");
        foreach (var kv in carried) sb.Append($"{kv.Key} x{kv.Value}  ");
        carriedText.text = sb.ToString();
    }

    [Serializable] class StealPayload { public string targetPlayerId; }
    [Serializable] class AttackPayload { public string targetId; }
}

public class WarehouseClickHandler : MonoBehaviour
{
    public string houseOwnerId;
    public RaidManager manager;
    void OnMouseDown() { manager?.OnWarehouseClicked(houseOwnerId); }
}

// Click vào người chơi khác để PK (gắn vào prefab player)
public class PlayerClickHandler : MonoBehaviour
{
    public string playerId;
    void OnMouseDown()
    {
        if (RaidManager.Instance != null)
            RaidManager.Instance.AttackPlayer(playerId);
    }
}
