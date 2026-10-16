// DungeonManager.cs — điều phối toàn bộ luồng vào dungeon:
// 1. Sinh layout (seed) -> 2. Vẽ tilemap -> 3. Đặt người chơi + cổng ra
// -> 4. Báo scene mới cho server -> 5. Xin quái từ server -> 6. Spawn quái.
//
// LUỒNG ANTI-CHEAT: client chỉ gửi {tier, seed, spawnPoints}.
// Loại quái, chỉ số, EXP đều do server quyết định (xem /api/dungeon/enter).
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class DungeonManager : MonoBehaviour
{
    public static DungeonManager Instance;

    [Header("Dungeon")]
    public int dungeonTier = 1;
    public bool randomSeed = true;
    public int fixedSeed = 12345; // dùng khi randomSeed = false (debug)

    [Header("References")]
    public DungeonGenerator generator;
    public DungeonTilemapPainter painter;

    [Header("Màu đèn tường theo tầng")]
    [Tooltip("Màu Point Light 2D của đuốc theo tầng (index 0 = tầng 1). Để trống = dùng màu mặc định của painter.")]
    public Color[] tierLightColors = new Color[]
    {
        new Color(1f, 0.71f, 0.42f), // tầng 1: cam lửa
        new Color(0.55f, 0.85f, 1f), // tầng 2: xanh băng
        new Color(0.70f, 0.55f, 1f), // tầng 3: tím ma trơi
    };

    [Header("Cổng ra")]
    public GameObject exitPortalPrefab; // prefab có ScenePortal/trigger về MainMap
    public string exitSceneName = "MainMap";

    [Header("Hình ảnh")]
    [Tooltip("Màu nền cho vùng void ngoài dungeon (mặc định đen)")]
    public Color voidBackgroundColor = Color.black;

    [Header("Phòng bí mật & câu đố")]
    [Tooltip("Số đuốc cần thắp cho câu đố LightTorches")]
    public int torchesPerPuzzle = 4;
    [Tooltip("Kéo sprite đuốc TẮT vào (dùng cho câu đố thắp đuốc)")]
    public Sprite torchUnlitSprite;
    [Tooltip("Kéo sprite đuốc CHÁY vào (dùng cho câu đố thắp đuốc)")]
    public Sprite torchLitSprite;
    [Tooltip("Số quái tinh anh canh trong phòng bí mật (spawn khi cửa mở)")]
    public int secretGuardians = 3;

    [Header("Server")]
    public string dungeonSceneName = "Dungeon"; // phải khớp scene_changed
    public float sceneSyncDelay = 0.5f; // đợi server tạo zone xong

    // Lưu lại để dùng khi mở phòng bí mật (spawn quái canh)
    List<MonsterTemplateData> dungeonTemplates;
    DungeonData currentData;

    [Serializable]
    class SpawnPointDto { public float x; public float y; }
    [Serializable]
    class EnterRequest
    {
        public int tier;
        public int seed;
        public List<SpawnPointDto> spawnPoints;
    }
    [Serializable]
    class EnterResponse
    {
        public bool success;
        public string reason;
        public MonsterTemplateData[] monsters;
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        StartCoroutine(RunDungeonFlow());
    }

    IEnumerator RunDungeonFlow()
    {
        if (generator == null || painter == null)
        {
            Debug.LogError("[Dungeon] Chưa gán generator/painter trong Inspector!");
            yield break;
        }

        // Nền đen cho vùng void ngoài dungeon (camera của scene này)
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.backgroundColor = voidBackgroundColor;

            // Đảm bảo camera thấy được layer Player (camera tạo mới trong
            // scene Dungeon có thể thiếu tick layer này)
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0 && (mainCam.cullingMask & (1 << playerLayer)) == 0)
            {
                mainCam.cullingMask |= 1 << playerLayer;
                Debug.Log("[Dungeon] Camera chưa tick layer Player — đã bật lại.");
            }
        }

        // 1-2. Sinh layout + vẽ
        int seed = randomSeed ? UnityEngine.Random.Range(int.MinValue, int.MaxValue) : fixedSeed;
        DungeonData data = generator.Generate(seed);
        currentData = data;
        // Màu đèn theo tầng (nếu có cấu hình)
        if (tierLightColors != null && dungeonTier >= 1 && dungeonTier <= tierLightColors.Length)
            painter.wallLightColor = tierLightColors[dungeonTier - 1];
        painter.Paint(data);

        // 2b. Dựng phòng bí mật + câu đố (không cần đợi server)
        SetupSecretRooms(data);

        // 3. Đặt người chơi vào điểm spawn
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("[Dungeon] Không tìm thấy Player trong scene!");
            yield break;
        }
        // Đặt người chơi vào điểm spawn — giữ nguyên z của nhân vật
        // (không ép về 0, một số setup camera/sort sẽ ẩn sprite nếu z bị đổi)
        Vector3 spawnWorld = painter.CellToWorld(data.playerSpawn);
        spawnWorld.z = player.transform.position.z;
        player.transform.position = spawnWorld;
        Debug.Log($"[Dungeon] Player spawn: tile={data.playerSpawn} -> world={spawnWorld} | GridPos={painter.floorMap.transform.position}");

        // 3b. Camera bám theo player.
        // (Player là DontDestroyOnLoad nên không kéo gán tay trong Inspector được)
        var camFollow = FindFirstObjectByType<CameraFollow>();
        if (camFollow != null)
        {
            camFollow.target = player.transform;
            // Giới hạn camera trong phạm vi map dungeon
            camFollow.useBounds = true;
            camFollow.minX = 0f; camFollow.maxX = data.width;
            camFollow.minY = 0f; camFollow.maxY = data.height;
        }

        // 3c. Đảm bảo có đèn 2D — sprite dùng material Lit mà không có đèn sẽ đen thui.
        // Nếu bạn đã đặt đèn riêng trong scene thì dòng này không làm gì cả.
        if (FindFirstObjectByType<UnityEngine.Rendering.Universal.Light2D>() == null)
        {
            var lightObj = new GameObject("Dungeon Global Light");
            var light2D = lightObj.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
            light2D.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Global;
            light2D.intensity = 1f;
            Debug.Log("[Dungeon] Scene chưa có đèn 2D — đã tự tạo Global Light 2D.");
        }

        // Đặt cổng ra
        if (exitPortalPrefab != null)
        {
            var portal = Instantiate(exitPortalPrefab, painter.CellToWorld(data.exitPos), Quaternion.identity);
            // Nếu prefab dùng ScenePortal thì gán scene đích tại đây
            var sp = portal.GetComponent<ScenePortal>();
            // (ScenePortal của bạn tự xử lý scene đích — kiểm tra lại prefab)
        }

        // 4. Báo server đổi scene để vào zone dungeon
        var nm = NetworkManager.Instance;
        if (nm == null)
        {
            Debug.LogError("[Dungeon] Chưa đăng nhập / không có NetworkManager — quái sẽ không spawn!");
            yield break;
        }
        nm.SendSceneChanged(dungeonSceneName);
        yield return new WaitForSeconds(sceneSyncDelay);

        // 5-6. Xin quái từ server + spawn
        yield return RequestMonsters(data);
    }

    IEnumerator RequestMonsters(DungeonData data)
    {
        var nm = NetworkManager.Instance;

        var reqBody = new EnterRequest
        {
            tier = dungeonTier,
            seed = data.seed,
            spawnPoints = new List<SpawnPointDto>()
        };
        foreach (var s in data.monsterSpawns)
            reqBody.spawnPoints.Add(new SpawnPointDto { x = s.x, y = s.y });

        string json = JsonUtility.ToJson(reqBody);
        string url = $"{nm.serverUrl}/api/dungeon/enter";

        using var req = new UnityWebRequest(url, "POST");
        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Authorization", $"Bearer {nm.Token}");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            // Đọc reason server trả về (vd INVALID_SPAWN_POINTS) để biết chính xác lỗi gì
            string reason = "";
            try
            {
                var errRes = JsonUtility.FromJson<EnterResponse>(req.downloadHandler.text);
                reason = errRes != null ? errRes.reason : "";
            }
            catch { /* body không phải JSON */ }
            Debug.LogError($"[Dungeon] Xin quái thất bại: {req.error} | reason={reason} | đã gửi {reqBody.spawnPoints.Count} điểm");
            yield break;
        }

        var res = JsonUtility.FromJson<EnterResponse>(req.downloadHandler.text);
        if (res == null || !res.success || res.monsters == null)
        {
            Debug.LogError($"[Dungeon] Server từ chối: {res?.reason}");
            yield break;
        }

        var mgr = MonsterSpawnManager.Instance;
        if (mgr == null)
        {
            Debug.LogError("[Dungeon] Thiếu MonsterSpawnManager trong scene!");
            yield break;
        }
        dungeonTemplates = new List<MonsterTemplateData>(res.monsters);
        mgr.SpawnFromTemplates(dungeonTemplates);
    }

    // ── PHÒNG BÍ MẬT & CÂU ĐỐ ──────────────────────────────────────────

    void SetupSecretRooms(DungeonData data)
    {
        if (data.secretDoors == null || data.secretDoors.Count == 0)
        {
            Debug.Log("[Dungeon] Không có phòng bí mật nào được đặt.");
            return;
        }

        var pmObj = new GameObject("PuzzleManager");
        var pm = pmObj.AddComponent<PuzzleManager>();
        pm.painter = painter;

        foreach (var sd in data.secretDoors)
        {
            // 1. Cửa
            var doorObj = new GameObject($"SecretDoor_{sd.puzzleId}");
            var door = doorObj.AddComponent<SecretRoomDoor>();
            door.requiredPuzzleId = sd.puzzleId;
            door.doorCells = new List<Vector2Int>(sd.doorCells);
            door.onOpened += d => OnSecretRoomOpened(d, data);

            // 2. Câu đố canh cửa này
            var puzzle = CreatePuzzle(sd, data);
            pm.RegisterPuzzle(puzzle);

            Debug.Log($"[Dungeon] Cửa {sd.puzzleId}: câu đố {sd.puzzleType}, {sd.doorCells.Count} ô cửa.");
        }
    }

    DungeonPuzzle CreatePuzzle(SecretDoorData sd, DungeonData data)
    {
        var go = new GameObject($"Puzzle_{sd.puzzleId}");
        DungeonPuzzle puzzle;

        switch (sd.puzzleType)
        {
            case PuzzleType.LightTorches:
                var ltp = go.AddComponent<LightTorchPuzzle>();
                ltp.torchUnlitSprite = torchUnlitSprite;
                ltp.torchLitSprite = torchLitSprite;
                ltp.torchesToLight = torchesPerPuzzle;
                ltp.puzzleId = sd.puzzleId;
                ltp.SpawnTorches(PickTorchPositions(data, torchesPerPuzzle));
                puzzle = ltp;
                break;

            case PuzzleType.KillAll:
            default:
                var kap = go.AddComponent<KillAllPuzzle>();
                kap.puzzleId = sd.puzzleId;
                puzzle = kap;
                break;
        }
        return puzzle;
    }

    // Chọn vị trí đặt đuốc: ưu tiên ô sàn kề tile đèn tường đã vẽ,
    // fallback quét toàn map tìm ô sàn kề tường.
    List<Vector3> PickTorchPositions(DungeonData data, int count)
    {
        var result = new List<Vector3>();
        var rng = new System.Random(data.seed + 777);

        var wallCells = new List<Vector2Int>(painter.lightTileCells);
        if (wallCells.Count == 0)
        {
            // Fallback: mọi ô tường kề sàn đều dùng được
            for (int y = 1; y < data.height - 1; y++)
                for (int x = 1; x < data.width - 1; x++)
                    if (!data.IsFloor(x, y) && data.IsFloor(x + 1, y))
                        wallCells.Add(new Vector2Int(x, y));
        }

        // Xáo trộn để đuốc rải đều
        for (int i = wallCells.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            var tmp = wallCells[i];
            wallCells[i] = wallCells[j];
            wallCells[j] = tmp;
        }

        var dirs = new Vector2Int[]
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(0, 1), new Vector2Int(0, -1),
        };

        foreach (var c in wallCells)
        {
            if (result.Count >= count) break;
            // Đuốc đứng trên ô sàn kề tường (không đứng trong phòng bí mật)
            foreach (var d in dirs)
            {
                int fx = c.x + d.x, fy = c.y + d.y;
                if (data.IsFloor(fx, fy) && !data.IsInSecretRoom(fx, fy))
                {
                    var pos = painter.CellToWorld(new Vector2Int(fx, fy));
                    pos.z = 0f;
                    result.Add(pos);
                    break;
                }
            }
        }

        if (result.Count == 0)
            Debug.LogError("[Dungeon] Không tìm được chỗ đặt đuốc! Kiểm tra lightTileCells.");
        return result;
    }

    // Khi cửa mở: spawn quái tinh anh canh trong phòng bí mật
    void OnSecretRoomOpened(SecretRoomDoor door, DungeonData data)
    {
        var sd = data.secretDoors.Find(d => d.puzzleId == door.requiredPuzzleId);
        if (sd == null) return;

        var mgr = MonsterSpawnManager.Instance;
        if (mgr == null || dungeonTemplates == null || dungeonTemplates.Count == 0)
        {
            Debug.LogWarning("[Dungeon] Mở phòng bí mật nhưng không spawn được quái canh.");
            return;
        }

        var rng = new System.Random(data.seed + sd.puzzleId.GetHashCode());
        var guardians = new List<MonsterTemplateData>();
        for (int i = 0; i < secretGuardians; i++)
        {
            var src = dungeonTemplates[rng.Next(dungeonTemplates.Count)];
            int gx = rng.Next(sd.roomRect.xMin + 1, sd.roomRect.xMax - 1);
            int gy = rng.Next(sd.roomRect.yMin + 1, sd.roomRect.yMax - 1);
            guardians.Add(new MonsterTemplateData
            {
                monsterId = $"guardian_{sd.puzzleId}_{i}_{Guid.NewGuid().ToString("N").Substring(0, 6)}",
                type = src.type,
                displayName = src.displayName,
                maxHp = Mathf.RoundToInt(src.maxHp * 1.5f),   // tinh anh: trâu hơn
                damage = Mathf.RoundToInt(src.damage * 1.5f),  // đánh đau hơn
                speed = src.speed,
                detectRange = src.detectRange,
                attackRange = src.attackRange,
                expReward = src.expReward * 2,                 // EXP gấp đôi
                spawnX = gx + 0.5f,
                spawnY = gy + 0.5f,
            });
        }
        mgr.SpawnExtra(guardians);
        Debug.Log($"[Dungeon] Đã spawn {guardians.Count} quái tinh anh trong phòng bí mật!");
    }

    // Gọi từ cổng ra khi muốn về
    public void ExitDungeon()
    {
        SceneManager.LoadScene(exitSceneName);
    }
}
