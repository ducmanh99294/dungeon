// MultiplayerManager.cs — gắn vào DontDestroyOnLoad object
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MultiplayerManager : MonoBehaviour
{
    public static MultiplayerManager Instance;

    [Header("Player Prefab")]
    public GameObject otherPlayerPrefab; // prefab hiển thị player khác

    [Header("Username Label")]
    public GameObject usernameLabelPrefab; // prefab TMP floating text

    // Track các player khác đang trong zone
    private Dictionary<string, OtherPlayerController> otherPlayers = new Dictionary<string, OtherPlayerController>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    void OnEnable()
    {
        if (NetworkManager.Instance == null) return;
        NetworkManager.Instance.OnPlayerJoined   += HandlePlayerJoined;
        NetworkManager.Instance.OnPlayerLeft      += HandlePlayerLeft;
        NetworkManager.Instance.OnPlayerMoved     += HandlePlayerMoved;
    }

    void OnDisable()
    {
        if (NetworkManager.Instance == null) return;
        NetworkManager.Instance.OnPlayerJoined   -= HandlePlayerJoined;
        NetworkManager.Instance.OnPlayerLeft      -= HandlePlayerLeft;
        NetworkManager.Instance.OnPlayerMoved     -= HandlePlayerMoved;
    }

    // ── SYNC ZONE PLAYERS ───────────────────────────────────────────────────
    // Gọi khi vào zone mới — sync toàn bộ player đang có

    public void SyncZonePlayers(PlayerData[] players)
    {
        // Xóa tất cả player cũ
        ClearAllPlayers();

        if (players == null) return;

        foreach (var player in players)
        {
            // Bỏ qua chính mình
            if (player.playerId == NetworkManager.Instance?.PlayerId) continue;
            SpawnOtherPlayer(player);
        }

        Debug.Log($"[Multiplayer] Synced {players.Length} players in zone");
    }

    // ── HANDLE EVENTS ───────────────────────────────────────────────────────

    void HandlePlayerJoined(PlayerData data)
    {
        // Bỏ qua chính mình
        if (data.playerId == NetworkManager.Instance?.PlayerId) return;

        Debug.Log($"[Multiplayer] Player joined: {data.username}");
        SpawnOtherPlayer(data);
    }

    void HandlePlayerLeft(string playerId)
    {
        Debug.Log($"[Multiplayer] Player left: {playerId}");
        RemoveOtherPlayer(playerId);
    }

    void HandlePlayerMoved(PlayerMoveData data)
    {
        if (otherPlayers.TryGetValue(data.playerId, out var controller))
            controller.MoveTo(data.x, data.y);
    }

    // ── SPAWN / REMOVE ──────────────────────────────────────────────────────

    void SpawnOtherPlayer(PlayerData data)
    {
        if (otherPlayers.ContainsKey(data.playerId)) return;
        if (otherPlayerPrefab == null)
        {
            Debug.LogWarning("[Multiplayer] otherPlayerPrefab chưa được gắn!");
            return;
        }

        var pos = new Vector3(data.x, data.y, 0f);
        var go  = Instantiate(otherPlayerPrefab, pos, Quaternion.identity);

        var controller = go.GetComponent<OtherPlayerController>();
        if (controller == null)
            controller = go.AddComponent<OtherPlayerController>();

        controller.Initialize(data.playerId, data.username, usernameLabelPrefab);
        otherPlayers[data.playerId] = controller;
    }

    void RemoveOtherPlayer(string playerId)
    {
        if (!otherPlayers.TryGetValue(playerId, out var controller)) return;
        if (controller != null) Destroy(controller.gameObject);
        otherPlayers.Remove(playerId);
    }

    void ClearAllPlayers()
    {
        foreach (var controller in otherPlayers.Values)
            if (controller != null) Destroy(controller.gameObject);
        otherPlayers.Clear();
    }

    // ── SCENE CHANGED ───────────────────────────────────────────────────────
    // Gọi từ ScenePortal khi đổi scene

    public void OnSceneChanged(string newScene)
    {
        ClearAllPlayers();
        NetworkManager.Instance?.SendSceneChanged(newScene);
    }

    public void ClearOnly() => ClearAllPlayers();
}
