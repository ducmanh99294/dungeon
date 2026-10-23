// TunnelManager.cs — client: hiển thị cửa hầm + click để dịch chuyển.
// Tự tạo marker cho mỗi đường hầm từ dữ liệu server.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class TunnelInfo
{
    public string tunnelId;
    public string houseOwnerId;
    public TunnelPositionData housePos;
    public TunnelPositionData dungeonPos;
}
[Serializable]
public class TunnelPositionData { public float x; public float y; public string scene; }
[Serializable]
public class TunnelListData { public TunnelInfo[] tunnels; }
[Serializable]
public class TeleportData
{
    public string playerId;
    public float x; public float y; public string scene;
}

public class TunnelManager : MonoBehaviour
{
    public static TunnelManager Instance;

    [Header("Marker cửa hầm")]
    public Color tunnelColor = new Color(0.6f, 0.4f, 0.9f);
    public float markerSize = 1f;
    [Tooltip("Chỉ hiện marker cùng scene hiện tại (tắt = hiện tất cả, tiện test)")]
    public bool filterByScene = false;

    public List<TunnelInfo> Tunnels { get; private set; } = new List<TunnelInfo>();

    Dictionary<string, GameObject> markers = new Dictionary<string, GameObject>();
    static Sprite squareSprite;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnTunnelList += HandleTunnelList;
            NetworkManager.Instance.OnPlayerTeleported += HandleTeleported;
            NetworkManager.Instance.OnConnected += RequestTunnels;
            RequestTunnels();
        }
    }

    void OnDestroy()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnTunnelList -= HandleTunnelList;
            NetworkManager.Instance.OnPlayerTeleported -= HandleTeleported;
            NetworkManager.Instance.OnConnected -= RequestTunnels;
        }
    }

    void RequestTunnels()
    {
        if (NetworkManager.Instance != null && NetworkManager.Instance.IsConnected)
        {
            NetworkManager.Instance.Emit("request_tunnels", "{}");
            Debug.Log("[Tunnel] Đã gửi request_tunnels");
        }
    }

    void HandleTunnelList(TunnelListData data)
    {
        if (data.tunnels == null) return;
        Tunnels = new List<TunnelInfo>(data.tunnels);
        foreach (var t in Tunnels)
        {
            CreateMarker(t.tunnelId + "_house", t.housePos, t.tunnelId);
            CreateMarker(t.tunnelId + "_dungeon", t.dungeonPos, t.tunnelId);
        }
        Debug.Log($"[Tunnel] Nhận {Tunnels.Count} đường hầm");
    }

    void CreateMarker(string key, TunnelPositionData pos, string tunnelId)
    {
        if (markers.ContainsKey(key)) return;
        if (filterByScene && pos.scene != SceneManager.GetActiveScene().name) return;

        var go = new GameObject($"Tunnel_{key}");
        // Đặt tại vị trí (nếu khác scene thì vẫn tạo, user tự sắp xếp khi test)
        go.transform.position = new Vector3(pos.x, pos.y, 0f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetSquareSpritePublic();
        sr.color = tunnelColor;
        sr.sortingOrder = 1;
        go.transform.localScale = Vector3.one * markerSize;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;

        var click = go.AddComponent<TunnelClickHandler>();
        click.tunnelId = tunnelId;
        click.manager = this;

        markers[key] = go;
    }

    public void UseTunnel(string tunnelId)
    {
        var payload = JsonUtility.ToJson(new TunnelUsePayload { tunnelId = tunnelId });
        NetworkManager.Instance?.Emit("use_tunnel", payload);
        Debug.Log($"[Tunnel] Dùng đường hầm {tunnelId}");
    }

    void HandleTeleported(TeleportData data)
    {
        // Nếu là mình: di chuyển player tới vị trí mới
        if (NetworkManager.Instance != null && data.playerId == NetworkManager.Instance.PlayerId)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                if (data.scene != SceneManager.GetActiveScene().name)
                {
                    Debug.Log($"[Tunnel] Cần chuyển scene sang {data.scene} (tự xử lý theo hệ thống scene của bạn)");
                    // TODO: gọi hệ thống chuyển scene của bạn ở đây, rồi đặt vị trí
                }
                playerObj.transform.position = new Vector3(data.x, data.y, playerObj.transform.position.z);
            }
        }
    }

    public static Sprite GetSquareSpritePublic()
    {
        if (squareSprite != null) return squareSprite;
        var tex = new Texture2D(32, 32);
        var pixels = new Color[32 * 32];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        squareSprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
        return squareSprite;
    }

    [Serializable] class TunnelUsePayload { public string tunnelId; }
}

public class TunnelClickHandler : MonoBehaviour
{
    public string tunnelId;
    public TunnelManager manager;
    void OnMouseDown() { manager?.UseTunnel(tunnelId); }
}
