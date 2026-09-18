// NetworkManager.cs — gắn vào DontDestroyOnLoad object
using System;
using System.Collections;
using UnityEngine;
using SocketIOClient;
using Newtonsoft.Json;
using UnityEngine.Networking;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance;

    [Header("Server")]
    public string serverUrl = "http://localhost:3000";

    [Header("Debug")]
    public bool showLogs = true;

    // Socket
    private SocketIOUnity socket;

    // State
    public bool IsConnected { get; private set; }
    public string PlayerId { get; private set; }
    public string Token { get; private set; }

    // Events
    public event Action<MonsterSpawnedData> OnMonsterSpawned;
    public event Action<MonsterKilledData> OnMonsterKilled;
    public event Action<ZoneMonstersData> OnZoneMonsters;
    public event Action OnConnected;
    public event Action OnDisconnected;
    public event Action<PlayerData> OnPlayerJoined;
    public event Action<string> OnPlayerLeft;
    public event Action<PlayerMoveData> OnPlayerMoved;
    public event Action<MonsterDamagedData> OnMonsterDamaged;
    public event Action<WorldStateData> OnWorldSync;
    public event Action<LootData> OnLootSpawned;
    public event Action<string> OnLootRemoved;
    public event Action<ChatData> OnChatMessage;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    // ── CONNECT ─────────────────────────────────────────────────────────────

    void OnEnable()
    {
        OnConnected += HandleConnected;
    }

    void HandleConnected()
    {
        // Lấy vị trí player từ PlayerPrefs hoặc default
        JoinWorld("MainMap", 0f, 0f);
    }

    public void Connect(string token, string playerId)
    {
        Token = token;
        PlayerId = playerId;

        var uri = new Uri(serverUrl);
        socket = new SocketIOUnity(uri, new SocketIOOptions
        {
            Auth = new { token },
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket,
            ReconnectionAttempts = 5,
        });

        RegisterSocketEvents();
        socket.Connect();
    }

    public void Disconnect()
    {
        socket?.Disconnect();
    }

    // ── REGISTER EVENTS ─────────────────────────────────────────────────────

    void RegisterSocketEvents()
    {
        socket.OnConnected += (sender, e) =>
        {
            IsConnected = true;
            Log("[Network] Connected!");
            OnConnected?.Invoke();
        };

        socket.OnDisconnected += (sender, e) =>
        {
            IsConnected = false;
            Log("[Network] Disconnected!");
            OnDisconnected?.Invoke();
        };

        // Player joined zone
        socket.On("player_joined", res =>
        {
            var data = res.GetValue<PlayerData>();
            Log($"[Network] Player joined: {data.username}");
            OnPlayerJoined?.Invoke(data);
        });

        // Player left zone
        socket.On("player_left", res =>
        {
            var data = res.GetValue<PlayerLeftData>();
            Log($"[Network] Player left: {data.playerId}");
            OnPlayerLeft?.Invoke(data.playerId);
        });

        // Player moved
        socket.On("player_moved", res =>
        {
            var data = res.GetValue<PlayerMoveData>();
            OnPlayerMoved?.Invoke(data);
        });
        // Trong RegisterSocketEvents()
        socket.On("monster_spawned", res =>
        {
            var data = res.GetValue<MonsterSpawnedData>();
            OnMonsterSpawned?.Invoke(data);
        });

        socket.On("monster_killed", res =>
        {
            var data = res.GetValue<MonsterKilledData>();
            OnMonsterKilled?.Invoke(data);
        });

        socket.On("zone_monsters", res =>
        {
            var data = res.GetValue<ZoneMonstersData>();
            OnZoneMonsters?.Invoke(data);
        });
        // Monster damaged
        socket.On("monster_damaged", res =>
        {
            var data = res.GetValue<MonsterDamagedData>();
            Log($"[Network] Monster {data.monsterId} HP: {data.hp}/{data.maxHp}");
            OnMonsterDamaged?.Invoke(data);
        });

        // World sync
        socket.On("world_sync", res =>
        {
            var data = res.GetValue<WorldStateData>();
            OnWorldSync?.Invoke(data);
        });

        // Zone players (khi vào zone mới)
        socket.On("zone_players", res =>
        {
            var data = res.GetValue<ZonePlayersData>();
            Log($"[Network] Zone players: {data.players?.Length}");
            MultiplayerManager.Instance?.SyncZonePlayers(data.players);
        });

        // Zone monsters
        socket.On("zone_monsters", res =>
        {
            var data = res.GetValue<ZoneMonstersData>();
            Log($"[Network] Zone monsters: {data.monsters?.Length}");
            // TODO: sync monsters
        });

        // Loot
        socket.On("loot_spawned", res =>
        {
            var data = res.GetValue<LootData>();
            OnLootSpawned?.Invoke(data);
        });

        socket.On("loot_removed", res =>
        {
            var data = res.GetValue<LootRemovedData>();
            OnLootRemoved?.Invoke(data.lootId);
        });

        // Chat
        socket.On("chat_message", res =>
        {
            var data = res.GetValue<ChatData>();
            OnChatMessage?.Invoke(data);
        });
    }

    // ── EMIT EVENTS ─────────────────────────────────────────────────────────

    public void JoinWorld(string scene, float x, float y)
    {
        socket?.Emit("join_world", new { scene, x, y });
        Log($"[Network] join_world: {scene} ({x},{y})");
    }

    public void SendMove(float x, float y)
    {
        if (!IsConnected) return;
        socket?.Emit("player_move", new { x, y });
    }

    public void SendSceneChanged(string scene)
    {
        socket?.Emit("scene_changed", new { scene });
        Log($"[Network] scene_changed: {scene}");
    }

    public void SendMonsterAttack(string monsterId, string attackId = "basic_attack")
    {
        socket?.Emit("monster_attack", new { monsterId, attackId });
    }

    public void SendLootDropped(string lootId, string itemId, string itemName, string rarity, float x, float y)
    {
        socket?.Emit("loot_dropped", new { lootId, itemId, itemName, rarity, x, y });
    }

    public void SendLootPicked(string lootId)
    {
        socket?.Emit("loot_picked", new { lootId });
    }

    public void SendChatMessage(string message)
    {
        socket?.Emit("chat_message", new { message });
    }

    public void SavePlayer(PlayerSaveData data)
    {
        socket?.Emit("save_player", data);
        Log("[Network] save_player sent");
    }

    // ── HELPERS ─────────────────────────────────────────────────────────────

    void Log(string msg)
    {
        if (showLogs) Debug.Log(msg);
    }

    void OnDestroy()
    {
        socket?.Disconnect();
    }
}

// ── DATA CLASSES ────────────────────────────────────────────────────────────

[Serializable]
public class PlayerData
{
    public string playerId;
    public string username;
    public string scene;
    public string zoneId;
    public float level;
    public float x;
    public float y;
}

[Serializable] public class PlayerLeftData { public string playerId; }

[Serializable]
public class PlayerMoveData
{
    public string playerId;
    public float x;
    public float y;
}

[Serializable]
public class MonsterDamagedData
{
    public string monsterId;
    public int hp;
    public int maxHp;
    public bool killed;
    public string attackerId;
}

[Serializable]
public class WorldStateData
{
    public string time;
    public int day;
    public string weather;
    public string moon;
}

[Serializable]
public class ZonePlayersData
{
    public string zoneId;
    public PlayerData[] players;
}

[Serializable]
public class ZoneMonstersData
{
    public string zoneId;
    public MonsterData[] monsters;
}

[Serializable]
public class MonsterData
{
    public string monsterId;
    public string type;
    public int hp;
    public int maxHp;
    public float x;
    public float y;
}

[Serializable]
public class LootData
{
    public string lootId;
    public string itemId;
    public string itemName;
    public string rarity;
    public float x;
    public float y;
}

[Serializable] public class LootRemovedData { public string lootId; }

[Serializable]
public class ChatData
{
    public string playerId;
    public string username;
    public string message;
    public string zoneId;
}

[Serializable]
public class PlayerSaveData
{
    public float x;
    public float y;
    public string scene;
    public int level;
    public float exp;
    public int gold;
}

// Data classes mới
[Serializable]
public class MonsterSpawnedData
{
    public string monsterId;
    public string type;
    public string displayName;
    public int hp;
    public int maxHp;
    public PositionData position;
    public string zoneId;
}

[Serializable] public class PositionData { public float x; public float y; }

[Serializable]
public class MonsterKilledData
{
    public string monsterId;
    public string killerId;
    public int expReward;
}
