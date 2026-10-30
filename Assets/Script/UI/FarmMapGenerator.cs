// FarmMapGenerator.cs — Client: dựng map farm từ data server gửi về
// Gắn vào GameObject gameSetup (cùng các manager khác)

using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.Tilemaps;

// 1 tile + trọng số (tỉ lệ xuất hiện khi gen)
[System.Serializable]
public class WeightedTile
{
    public TileBase tile;
    [Tooltip("Tỉ lệ xuất hiện (càng cao càng nhiều)")]
    public float weight = 1f;
}

[System.Serializable]
public class WeightedSprite
{
    public Sprite sprite;
    [Tooltip("Tỉ lệ xuất hiện (càng cao càng nhiều)")]
    public float weight = 1f;
}

public class FarmMapGenerator : MonoBehaviour
{
    public static FarmMapGenerator Instance;

    [Header("Map")]
    [Tooltip("Kích thước 1 ô (unit). Map 128x128 ô.")]
    public float tileSize = 1f;
    [Tooltip("Seed cố định để test (0 = random mỗi lần Play)")]
    public int testSeed = 0;
    [Header("🏠 KÍCH THƯỚC KHU")]
    public int homeWidth = 18;
    public int homeHeight = 14;
    public int dungeonWidth = 10;
    public int dungeonHeight = 10;

    [Header("Màu sắc (nếu không gán sprite)")]
    public Color grassColorA = new Color(0.32f, 0.62f, 0.28f);
    public Color grassColorB = new Color(0.28f, 0.57f, 0.25f);
    public Color grassTuftColor = new Color(0.25f, 0.55f, 0.22f);
    public Color portalColor = new Color(0.5f, 0.2f, 0.7f);
    public Color treeColor = new Color(0.16f, 0.42f, 0.18f);
    public Color rockColor = new Color(0.48f, 0.48f, 0.52f);

    [Header("Sprite tùy chọn")]
    public Sprite treeSprite;
    [Header("🌳 CÂY 2 LỚP (thân + tán)")]
    [Tooltip("Sprite thân cây (không có tán) — gắn collider")]
    public Sprite treeTrunkSprite;
    [Tooltip("Sprite tán cây (chỉ phần lá trên) — order cao hơn player, không collider")]
    public Sprite treeCanopySprite;
    [Tooltip("Nhiều mẫu tán cây, vẽ theo tỉ lệ (như grass) — để trống thì dùng Tree Canopy Sprite")]
    public WeightedSprite[] treeCanopyTiles;
    public int treeTrunkOrder = 2;
    public int treeCanopyOrder = 5;
    [Tooltip("Tán lệch lên trên thân bao nhiêu unit")]
    public float canopyOffsetY = 0.6f;
    [Header("🌊 SÔNG / NƯỚC")]
    [Tooltip("Số sông uốn lượn trên map")]
    public int riverCount = 1;
    [Tooltip("Số hồ vẽ bằng tile (thay cho pond tròn)")]
    public int lakeCount = 3;
    [Tooltip("Tile nước vẽ sông (để trống thì dùng màu xanh mặc định)")]
    public TileBase waterTile;
    public int waterOrder = -3;
    [Header("💧 TILE BO GÓC NƯỚC (như grass)")]
    [Tooltip("Tile viền nước — vẽ ĐÈ lên ô đất kề bờ")]
    public TileBase waterEdgeTop, waterEdgeBottom, waterEdgeLeft, waterEdgeRight;
    public TileBase waterCornerOuterTL, waterCornerOuterTR, waterCornerOuterBL, waterCornerOuterBR;
    public TileBase waterCornerInnerTL, waterCornerInnerTR, waterCornerInnerBL, waterCornerInnerBR;
    [Tooltip("Order lớp phủ viền nước (cao hơn cỏ để đè lên bờ)")]
    public int waterEdgeOrder = -2;
    [Header("🌲 MẬT ĐỘ CÂY THEO KHOẢNG CÁCH")]
    [Tooltip("Số cây tối đa trên map")]
    public int maxTrees = 150;
    [Tooltip("Tỉ lệ cây gần nhà (0 = không có cây sát nhà, 1 = nhiều như xa nhà)")]
    [Range(0f, 1f)] public float nearHomeDensity = 0.1f;
    [Tooltip("Bán kính quanh nhà không mọc cây dày (tile) — ngoài bán kính này cây đều nhau")]
    public float homeClearRadius = 15f;
    [Tooltip("Kích thước collider gốc cây")]
    public Vector2 treeColliderSize = new Vector2(0.5f, 0.4f);
    [Tooltip("Vị trí collider so với tâm cây")]
    public Vector2 treeColliderOffset = new Vector2(0, -0.3f);
    public Sprite rockSprite;
    [Header("🪨 ĐÁ NHIỀU TILE (như tán cây)")]
    [Tooltip("Nhiều mẫu đá, vẽ theo tỉ lệ (như tán cây) — để trống thì dùng Rock Sprite")]
    public WeightedSprite[] rockTiles;
    [Tooltip("Kích thước collider đá (mặc định 0.9 như cũ)")]
    public Vector2 rockColliderSize = new Vector2(0.9f, 0.9f);
    [Tooltip("Vị trí collider so với tâm đá")]
    public Vector2 rockColliderOffset = new Vector2(0, 0);
    public Sprite grassSprite;
    public Sprite portalSprite;

    [Header("🌱 GRASS TILES — tile nền cỏ")]
    [Tooltip("Tile cỏ + tỉ lệ xuất hiện. VD: cỏ thường 60, cỏ đậm 25, cỏ hoa 15")]
    public WeightedTile[] grassTiles;

    [Header("🟤 GROUND TILES — tile đường đất")]
    [Tooltip("Tile đường đất + tỉ lệ xuất hiện")]
    public WeightedTile[] pathTiles;

    [Header("🔲 BO GÓC GRASS")]
    [Tooltip("Viền cỏ (1 phía không phải cỏ)")]
    public TileBase grassEdgeTop;
    public TileBase grassEdgeBottom;
    public TileBase grassEdgeLeft;
    public TileBase grassEdgeRight;
    [Tooltip("Góc ngoài cỏ")]
    public TileBase grassCornerOuterTL;
    public TileBase grassCornerOuterTR;
    public TileBase grassCornerOuterBL;
    public TileBase grassCornerOuterBR;
    [Tooltip("Góc trong cỏ")]
    public TileBase grassCornerInnerTL;
    public TileBase grassCornerInnerTR;
    public TileBase grassCornerInnerBL;
    public TileBase grassCornerInnerBR;

    [Header("Tilemap có sẵn (tùy chọn)")]
    [Tooltip("Tilemap FLOOR — lót đất bên dưới")]
    public Tilemap floorTilemap;
    [Tooltip("Tilemap GRASS OVERLAY — phủ cỏ lên trên, có bo góc")]
    public Tilemap grassTilemap;
    [Tooltip("Tilemap NƯỚC (sông + hồ) tự tạo trong Grid — gán để tự chỉnh Order in Layer, không gán = code tự tạo (order = waterOrder)")]
    public Tilemap waterTilemap;
    [Tooltip("Tilemap VIỀN NƯỚC (đè lên bờ hồ) tự tạo trong Grid — gán để tự chỉnh Order, không gán = code tự tạo (order = waterEdgeOrder)")]
    public Tilemap waterEdgeTilemap;
    [Header("Tilemap lấy Order cho cây/đá (tùy chọn)")]
    [Tooltip("Tilemap của cây trong Grid — gán để thân/tán lấy Order in Layer từ tilemap này thay vì số treeTrunkOrder/treeCanopyOrder (tán = thân + khoảng cách cũ)")]
    public Tilemap treeTilemap;
    [Tooltip("Tilemap của đá trong Grid — gán để đá lấy Order in Layer từ tilemap này thay vì resourceOrder")]
    public Tilemap rockTilemap;

    [Header("Order in Layer")]
    public int floorOrder = -10;
    public int tuftOrder = 0;
    public int resourceOrder = 2;
    public int portalOrder = 3;
    public int grassOrder = -5;

    [Header("Grid")]
    [Tooltip("Tự tạo Grid để snap vị trí xây dựng theo ô")]
    public bool createGrid = true;
    [Tooltip("Hiện lưới ô vuông (để căn lúc xây)")]
    public bool showGridLines = false;
    public Color gridLineColor = new Color(1, 1, 1, 0.15f);

    GameObject mapRoot;
    public Grid Grid { get; private set; }
    public Tilemap FloorTilemap { get; private set; }
    public Tilemap GrassTilemap { get; private set; }
    public Tilemap WaterTilemap { get; private set; }
    public Tilemap WaterEdgeTilemap { get; private set; }
    // Tập hợp ô nước (sông + hồ) — dùng cho phản chiếu
    // Tập hợp riêng ô hồ — viền nước chỉ vẽ quanh hồ
    public HashSet<Vector2Int> waterSet = new HashSet<Vector2Int>();
    public HashSet<Vector2Int> lakeSet = new HashSet<Vector2Int>();
    // Vị trí spawn player (manager khác đọc)
    public Vector2 SpawnPosition { get; private set; }
    public int MapWidth { get; private set; } = 128;
    public int MapHeight { get; private set; } = 128;

    // Danh sách tài nguyên (để sau này chặt cây/đập đá)
    public Dictionary<Vector2Int, ResourceNode> resources = new Dictionary<Vector2Int, ResourceNode>();
    Dictionary<string, int> rockVariantCounts = new Dictionary<string, int>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Kiểu dungeon: gen ngay từ seed, không chờ server
        // Nếu có seed từ server (multiplayer) thì gen lại bằng seed đó
        if (NetworkManager.Instance != null)
        {
            try
            {
                // Thử đăng ký nhận seed từ server (nếu NetworkManager có event này)
                var nmType = NetworkManager.Instance.GetType();
                var seedEvent = nmType.GetEvent("OnMapSeed");
                if (seedEvent != null)
                {
                    System.Action<int> handler = (seed) => GenerateFromSeed(seed);
                    seedEvent.AddEventHandler(NetworkManager.Instance, handler);
                    NetworkManager.Instance.Emit("request_map_seed", "{}");
                    Debug.Log("[FarmMap] Đã xin seed từ server, gen tạm bằng seed local...");
                }
            }
            catch { /* NetworkManager chưa có OnMapSeed — dùng seed local */ }
        }

        // Gen ngay bằng seed local (chơi đơn hoặc chờ seed server)
        int startSeed = testSeed != 0 ? testSeed : Random.Range(0, 1000000);
        GenerateFromSeed(startSeed);
    }

    // API public: gen map từ seed (server gọi khi có seed multiplayer)
    int currentSeed = 0;
    public void GenerateFromSeed(int seed)
    {
        currentSeed = seed;
        Debug.Log($"[FarmMap] Gen map từ seed {seed}");
        BuildMap(GenerateLocal(seed));
    }

    void BuildMap(MapData data)
    {
        // Xóa map cũ nếu có
        if (mapRoot != null) Destroy(mapRoot);
        mapRoot = new GameObject("FarmMap");
        resources.Clear();
        rockVariantCounts.Clear();

        MapWidth = data.width;
        MapHeight = data.height;
        SpawnPosition = new Vector2(data.spawn.x * tileSize, data.spawn.y * tileSize);

        if (createGrid) CreateGrid(data);

        BuildGround(data);
        BuildPaths(data);
        BuildWater(data);
        foreach (var g in data.grass) SpawnGrassTuft(g.x, g.y, g.v);
        foreach (var t in data.trees) SpawnResource(t.x, t.y, ResourceType.Tree, t.wood);
        foreach (var r in data.rocks) SpawnResource(r.x, r.y, ResourceType.Rock, r.stone);
        if (data.dungeonPortal != null) SpawnDungeonPortal(data.dungeonPortal.x, data.dungeonPortal.y);

        // Đặt player vào điểm spawn
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) player.transform.position = new Vector3(SpawnPosition.x, SpawnPosition.y, 0);

        string rockBreakdown = "";
        foreach (var kv in rockVariantCounts) rockBreakdown += $"{kv.Key}={kv.Value} ";
        Debug.Log($"[FarmMap] Dựng xong: {data.trees.Count} cây, {data.rocks.Count} đá, {data.lakes.Count} ô hồ, {data.grass.Count} cỏ | Đá theo mẫu: {rockBreakdown}");
    }

    // Bụi cỏ trang trí (nhỏ, không chặn)
    void SpawnGrassTuft(int gx, int gy, int variant)
    {
        var go = new GameObject($"Grass_{gx}_{gy}");
        go.transform.SetParent(mapRoot.transform, false);
        go.transform.position = new Vector3(gx * tileSize, gy * tileSize, 0);
        // Xoay/scale ngẫu nhiên theo biến thể cho đỡ đơn điệu
        go.transform.rotation = Quaternion.Euler(0, 0, variant * 45f);
        float s = 0.5f + variant * 0.15f;
        go.transform.localScale = Vector3.one * s;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = grassSprite != null ? grassSprite : MakeGrassSprite();
        sr.sortingOrder = tuftOrder;
    }

    Sprite grassCache;
    Sprite MakeGrassSprite()
    {
        if (grassCache != null) return grassCache;
        // Hình 3 vạch chéo như bụi cỏ
        int sz = 16;
        var tex = new Texture2D(sz, sz);
        var px = new Color[sz * sz];
        for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
        // Vẽ 3 đường chéo
        for (int i = 0; i < sz; i++)
        {
            SetPx(px, sz, i, sz - 1 - i / 2, grassTuftColor);           // trái
            SetPx(px, sz, sz / 2, sz - 1 - i * 2 / 3, grassTuftColor);  // giữa
            SetPx(px, sz, sz - 1 - i, sz - 1 - i / 2, grassTuftColor);  // phải
        }
        tex.SetPixels(px); tex.Apply();
        tex.filterMode = FilterMode.Point;
        grassCache = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0f), sz);
        return grassCache;
    }

    void SetPx(Color[] px, int sz, int x, int y, Color c)
    {
        if (x >= 0 && x < sz && y >= 0 && y < sz) px[y * sz + x] = c;
    }

    // Cổng vào dungeon — click để vào
    void SpawnDungeonPortal(int gx, int gy)
    {
        var go = new GameObject("DungeonPortal");
        go.transform.SetParent(mapRoot.transform, false);
        go.transform.position = new Vector3(gx * tileSize, gy * tileSize, 0);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = portalSprite != null ? portalSprite : MakeCircleSprite(portalColor);
        go.transform.localScale = Vector3.one * 2f;
        sr.sortingOrder = portalOrder;

        // Vòng sáng ngoài
        var glowGO = new GameObject("Glow");
        glowGO.transform.SetParent(go.transform, false);
        var glowSr = glowGO.AddComponent<SpriteRenderer>();
        glowSr.sprite = MakeCircleSprite(new Color(portalColor.r, portalColor.g, portalColor.b, 0.35f));
        glowGO.transform.localScale = Vector3.one * 1.6f;
        glowSr.sortingOrder = portalOrder - 1;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 1f;
        col.isTrigger = true;

        var portal = go.AddComponent<DungeonPortal>();
        Debug.Log($"[FarmMap] Cổng dungeon tại ({gx}, {gy})");
    }

    void CreateGrid(MapData data)
    {
        // Đồng bộ tileSize TRƯỚC khi tạo Grid (để BuildGrid.cellSize đúng ngay từ đầu)
        if (floorTilemap != null && floorTilemap.transform.parent != null)
        {
            var pg = floorTilemap.transform.parent.GetComponent<Grid>();
            if (pg != null && Mathf.Abs(pg.cellSize.x - tileSize) > 0.01f)
            {
                tileSize = pg.cellSize.x;
                Debug.Log($"[FarmMap] Đã đồng bộ tileSize = {tileSize} theo Grid có sẵn");
            }
        }

        var gridGO = new GameObject("BuildGrid");
        gridGO.transform.SetParent(mapRoot.transform, false);
        Grid = gridGO.AddComponent<Grid>();
        Grid.cellSize = new Vector3(tileSize, tileSize, 0);
        Grid.cellLayout = GridLayout.CellLayout.Rectangle;

        // Tilemap nền cỏ: dùng cái có sẵn nếu được gán
        if (floorTilemap != null)
        {
            FloorTilemap = floorTilemap;
            Debug.Log("[FarmMap] Dùng Tilemap floor có sẵn");
        }
        else
        {
            var tmGO = new GameObject("FloorTilemap");
            tmGO.transform.SetParent(gridGO.transform, false);
            FloorTilemap = tmGO.AddComponent<Tilemap>();
            var tmr = tmGO.AddComponent<TilemapRenderer>();
            tmr.sortingOrder = floorOrder;
        }

        // Tilemap cỏ phủ: dùng cái có sẵn nếu được gán
        if (grassTilemap != null)
        {
            GrassTilemap = grassTilemap;
            Debug.Log("[FarmMap] Dùng Tilemap grass có sẵn");
        }
        else
        {
            var pathGO = new GameObject("GrassTilemap");
            pathGO.transform.SetParent(gridGO.transform, false);
            GrassTilemap = pathGO.AddComponent<Tilemap>();
            var pathR = pathGO.AddComponent<TilemapRenderer>();
            pathR.sortingOrder = grassOrder;
        }

        // Tilemap nước (sông + hồ): dùng cái có sẵn nếu được gán (giữ nguyên Order in Layer trong scene)
        if (waterTilemap != null)
        {
            WaterTilemap = waterTilemap;
            Debug.Log("[FarmMap] Dùng Tilemap nước có sẵn");
        }
        else
        {
            var waterGO = new GameObject("WaterTilemap");
            waterGO.transform.SetParent(gridGO.transform, false);
            WaterTilemap = waterGO.AddComponent<Tilemap>();
            var waterR = waterGO.AddComponent<TilemapRenderer>();
            waterR.sortingOrder = waterOrder;
        }

        // Tilemap phủ viền nước (đè lên ô đất kề bờ): dùng cái có sẵn nếu được gán
        if (waterEdgeTilemap != null)
        {
            WaterEdgeTilemap = waterEdgeTilemap;
            Debug.Log("[FarmMap] Dùng Tilemap viền nước có sẵn");
        }
        else
        {
            var waterEdgeGO = new GameObject("WaterEdgeTilemap");
            waterEdgeGO.transform.SetParent(gridGO.transform, false);
            WaterEdgeTilemap = waterEdgeGO.AddComponent<Tilemap>();
            var waterEdgeR = waterEdgeGO.AddComponent<TilemapRenderer>();
            waterEdgeR.sortingOrder = waterEdgeOrder;
        }
        // Collider chặn ở bờ (chỉ thêm nếu tilemap chưa có — để bờ vẫn chặn khi dùng tilemap tự tạo)
        if (WaterEdgeTilemap.GetComponent<TilemapCollider2D>() == null)
            WaterEdgeTilemap.gameObject.AddComponent<TilemapCollider2D>();

        if (treeTilemap != null)
            Debug.Log($"[FarmMap] Cây lấy Order từ tilemap: thân={TilemapOrderOr(treeTilemap, treeTrunkOrder)}, tán={TilemapOrderOr(treeTilemap, treeTrunkOrder) + (treeCanopyOrder - treeTrunkOrder)}");
        if (rockTilemap != null)
            Debug.Log($"[FarmMap] Đá lấy Order từ tilemap: {TilemapOrderOr(rockTilemap, resourceOrder)}");

        if (showGridLines) DrawGridLines(data, gridGO.transform);
        Debug.Log("[FarmMap] Đã tạo Grid " + data.width + "x" + data.height);
    }

    // Vẽ lưới bằng LineRenderer (nhẹ, 1 object)
    void DrawGridLines(MapData data, Transform parent)
    {
        var go = new GameObject("GridLines");
        go.transform.SetParent(parent, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = gridLineColor;
        lr.startWidth = lr.endWidth = 0.03f;
        lr.sortingOrder = 5;
        lr.useWorldSpace = false;

        var pts = new List<Vector3>();
        float w = data.width * tileSize, h = data.height * tileSize;
        // Dọc
        for (int x = 0; x <= data.width; x++)
        {
            pts.Add(new Vector3(x * tileSize, 0, 0));
            pts.Add(new Vector3(x * tileSize, h, 0));
        }
        // Ngang
        for (int y = 0; y <= data.height; y++)
        {
            pts.Add(new Vector3(0, y * tileSize, 0));
            pts.Add(new Vector3(w, y * tileSize, 0));
        }
        lr.positionCount = pts.Count;
        lr.SetPositions(pts.ToArray());
    }

    // Gen map local (port từ server generateStructured) — chỉ dùng khi offline test
    MapData GenerateLocal(int seed)
    {
        var rand = new System.Random(seed);
        var occupied = new HashSet<Vector2Int>();
        var paths = new List<PathData>();

        System.Func<int, int, int, bool> isFree = (x, y, margin) =>
        {
            for (int dx = -margin; dx <= margin; dx++)
                for (int dy = -margin; dy <= margin; dy++)
                    if (occupied.Contains(new Vector2Int(x + dx, y + dy))) return false;
            return true;
        };
        System.Action<int, int, int> mark = (x, y, r) =>
        {
            for (int dx = -r; dx <= r; dx++)
                for (int dy = -r; dy <= r; dy++)
                    occupied.Add(new Vector2Int(x + dx, y + dy));
        };
        System.Action<int, int, int, int> markRect = (rx, ry, rw, rh) =>
        {
            for (int x = rx - 1; x < rx + rw + 1; x++)
                for (int y = ry - 1; y < ry + rh + 1; y++)
                    occupied.Add(new Vector2Int(x, y));
        };
        System.Func<int, int> randPos = (margin) => margin + rand.Next(128 - margin * 2);

        // 3 khu
        // Chỉ 2 khu, đặt xa nhau: home 1 góc, dungeon góc đối diện
        var home = new ZoneRect { x = 8 + rand.Next(20), y = 8 + rand.Next(20), w = homeWidth, h = homeHeight };
        markRect(home.x, home.y, home.w, home.h);
        // Dungeon ở góc đối diện (cách home tối thiểu nửa map)
        int dx = (home.x < 64) ? 90 + rand.Next(20) : 8 + rand.Next(20);
        int dy = (home.y < 64) ? 90 + rand.Next(20) : 8 + rand.Next(20);
        var dungeon = new ZoneRect { x = dx, y = dy, w = dungeonWidth, h = dungeonHeight };
        markRect(dungeon.x, dungeon.y, dungeon.w, dungeon.h);

        // Tâm 2 khu (dùng cho spawn, cổng dungeon, mật độ cây)
        int hcx = home.x + home.w / 2, hcy = home.y + home.h / 2;
        int dcx = dungeon.x + dungeon.w / 2, dcy = dungeon.y + dungeon.h / 2;

        // Sông uốn lượn (random walk, tránh khu home/dungeon)
        var rivers = new List<Pos>();
        for (int r = 0; r < riverCount; r++)
        {
            bool horizontal = rand.Next(2) == 0;
            int main = 2;
            int perp = 10 + rand.Next(108);
            int perpVel = 0;
            while (main < 126)
            {
                for (int w = -1; w <= 1; w++)
                {
                    int wx = horizontal ? main : perp + w;
                    int wy = horizontal ? perp + w : main;
                    if (wx < 0 || wx >= 128 || wy < 0 || wy >= 128) continue;
                    if (occupied.Contains(new Vector2Int(wx, wy))) continue; // tránh khu
                    rivers.Add(new Pos { x = wx, y = wy });
                    occupied.Add(new Vector2Int(wx, wy));
                }
                main++;
                if (rand.NextDouble() < 0.5) perpVel += rand.Next(3) - 1;
                perpVel = Mathf.Clamp(perpVel, -2, 2);
                perp += perpVel;
                perp = Mathf.Clamp(perp, 4, 123);
            }
        }

        // Đường chữ L rộng 3


        // Hồ vẽ bằng tile: hình tròn thuần
        var lakes = new List<Pos>();
        for (int i = 0; i < lakeCount; i++)
            for (int a = 0; a < 60; a++)
            {
                int cx = randPos(10), cy = randPos(10);
                if (!isFree(cx, cy, 5)) continue;
                int r = 2 + rand.Next(3); // bán kính 2-4
                for (int lx = -r; lx <= r; lx++)
                    for (int ly = -r; ly <= r; ly++)
                    {
                        float d = Mathf.Sqrt(lx * lx + ly * ly);
                        if (d > r + 0.5f) continue;
                        int wx = cx + lx, wy = cy + ly;
                        if (wx < 0 || wx >= 128 || wy < 0 || wy >= 128) continue;
                        if (occupied.Contains(new Vector2Int(wx, wy))) continue;
                        lakes.Add(new Pos { x = wx, y = wy });
                        occupied.Add(new Vector2Int(wx, wy));
                    }
                break;
            }

        // Cây
        // Cây: chia map thành sector 16x16, mỗi sector rải đều số cây như nhau
        // (ép phân bố đều, không dồn góc dù random lệch)
        var trees = new List<TreeData>();
        int sector = 16;
        int sectorsX = 128 / sector, sectorsY = 128 / sector;
        int perSector = Mathf.Max(1, maxTrees / (sectorsX * sectorsY));
        for (int sx = 0; sx < sectorsX; sx++)
            for (int sy = 0; sy < sectorsY; sy++)
            {
                int placedInSector = 0;
                for (int t = 0; t < perSector * 4 && placedInSector < perSector; t++)
                {
                    if (trees.Count >= maxTrees) break;
                    int x = sx * sector + 3 + rand.Next(sector - 6);
                    int y = sy * sector + 3 + rand.Next(sector - 6);
                    if (!isFree(x, y, 1)) continue;
                    // Trong bán kính quanh nhà thì xác suất thấp, ngoài thì 100%
                    float ddx = x - hcx, ddy = y - hcy;
                    float distTiles = Mathf.Sqrt(ddx * ddx + ddy * ddy);
                    float chance = distTiles < homeClearRadius ? nearHomeDensity : 1f;
                    if (rand.NextDouble() > chance) continue;
                    trees.Add(new TreeData { x = x, y = y, wood = 3 + rand.Next(3) });
                    mark(x, y, 1);
                    placedInSector++;
                }
            }

        // Đá
        var rocks = new List<RockData>();
        for (int i = 0; i < 30; i++)
            for (int a = 0; a < 40; a++)
            {
                int x = randPos(3), y = randPos(3);
                if (isFree(x, y, 1)) { rocks.Add(new RockData { x = x, y = y, stone = 2 + rand.Next(3) }); mark(x, y, 1); break; }
            }

        // Cỏ
        var grass = new List<GrassData>();
        for (int i = 0; i < 150; i++)
            for (int a = 0; a < 20; a++)
            {
                int x = randPos(2), y = randPos(2);
                if (isFree(x, y, 0)) { grass.Add(new GrassData { x = x, y = y, v = rand.Next(3) }); mark(x, y, 0); break; }
            }

        return new MapData
        {
            seed = seed, width = 128, height = 128,
            spawn = new Pos { x = hcx, y = hcy },
            trees = trees, rocks = rocks, lakes = lakes, grass = grass,
            dungeonPortal = new Pos { x = dcx, y = dcy },
            zones = new ZonesData { home = home, dungeon = dungeon },
            paths = paths,
            rivers = rivers,
        };
    }

    // Chọn tile theo trọng số (deterministic theo seed)
    TileBase PickWeightedTile(WeightedTile[] tiles, System.Random rand)
    {
        float total = 0;
        foreach (var wt in tiles)
            if (wt.tile != null) total += Mathf.Max(0, wt.weight);
        if (total <= 0) return null;

        float roll = (float)rand.NextDouble() * total;
        foreach (var wt in tiles)
        {
            if (wt.tile == null) continue;
            roll -= Mathf.Max(0, wt.weight);
            if (roll <= 0) return wt.tile;
        }
        return tiles[tiles.Length - 1].tile;
    }

    // Chọn sprite theo trọng số (như PickWeightedTile nhưng cho Sprite)
    Sprite PickWeightedSprite(WeightedSprite[] sprites, System.Random rand)
    {
        float total = 0;
        foreach (var ws in sprites)
            if (ws.sprite != null) total += Mathf.Max(0, ws.weight);
        if (total <= 0) return null;

        float roll = (float)rand.NextDouble() * total;
        foreach (var ws in sprites)
        {
            if (ws.sprite == null) continue;
            roll -= Mathf.Max(0, ws.weight);
            if (roll <= 0) return ws.sprite;
        }
        return sprites[sprites.Length - 1].sprite;
    }

    // Mở rộng ground hẹp: tile đất nào bị cỏ kẹp 2 bên (chỉ rộng 1) thì nới thành 3
    void WidenNarrowGround(MapData data)
    {
        if (GrassTilemap == null || pathTiles == null || pathTiles.Length == 0) return;
        var rand = new System.Random(data.seed + 777);
        var toAdd = new List<Vector3Int>();

        for (int x = 1; x < data.width - 1; x++)
            for (int y = 1; y < data.height - 1; y++)
            {
                var pos = new Vector3Int(x, y, 0);
                if (!GrassTilemap.HasTile(pos)) continue;

                // Kẹp dọc: cỏ trên + cỏ dưới, ground chỉ 1 tile ở giữa
                bool upIsGround = GrassTilemap.HasTile(new Vector3Int(x, y + 1, 0));
                bool downIsGround = GrassTilemap.HasTile(new Vector3Int(x, y - 1, 0));
                bool leftIsGround = GrassTilemap.HasTile(new Vector3Int(x - 1, y, 0));
                bool rightIsGround = GrassTilemap.HasTile(new Vector3Int(x + 1, y, 0));

                // Hẹp ngang: ground 1 tile, cỏ 2 bên trái/phải
                if (!leftIsGround && !rightIsGround && upIsGround && downIsGround)
                {
                    // Đường dọc hẹp — mở rộng ngang thành 3
                    toAdd.Add(new Vector3Int(x - 1, y, 0));
                    toAdd.Add(new Vector3Int(x + 1, y, 0));
                }
                // Hẹp dọc: ground 1 tile, cỏ 2 bên trên/dưới
                if (!upIsGround && !downIsGround && leftIsGround && rightIsGround)
                {
                    toAdd.Add(new Vector3Int(x, y - 1, 0));
                    toAdd.Add(new Vector3Int(x, y + 1, 0));
                }
            }

        foreach (var pos in toAdd)
        {
            if (GrassTilemap.HasTile(pos)) continue;
            var tile = PickWeightedTile(pathTiles, rand);
            if (tile != null) GrassTilemap.SetTile(pos, tile);
        }
        if (toAdd.Count > 0)
            Debug.Log($"[FarmMap] Đã mở rộng {toAdd.Count} ô ground hẹp thành 3");
    }

    // WALL kiểu dungeon: phủ GRASS lên trên floor, có bo góc
    // (grass chỉ vẽ ở vùng cỏ, không vẽ lên đường/khu/ao)
    void BuildPaths(MapData data)
    {
        if (GrassTilemap == null) return;
        if (grassTiles == null || grassTiles.Length == 0)
        {
            Debug.LogWarning("[FarmMap] Chưa gán Grass Tiles — cỏ sẽ không hiện");
            return;
        }
        GrassTilemap.ClearAllTiles();

        // Tập hợp đường + khu + ao (không vẽ cỏ lên đó)
        var noGrass = new HashSet<Vector2Int>();
        if (data.paths != null)
            foreach (var pt in data.paths) noGrass.Add(new Vector2Int(pt.x, pt.y));
        if (data.zones != null)
        {
            foreach (var z in new[] { data.zones.home, data.zones.dungeon })
            {
                if (z == null) continue;
                for (int zx = z.x; zx < z.x + z.w; zx++)
                    for (int zy = z.y; zy < z.y + z.h; zy++)
                        noGrass.Add(new Vector2Int(zx, zy));
            }
        }
        if (data.lakes != null)
            foreach (var lk in data.lakes) noGrass.Add(new Vector2Int(lk.x, lk.y));
        if (data.rivers != null)
            foreach (var rv in data.rivers) noGrass.Add(new Vector2Int(rv.x, rv.y));

        var rand = new System.Random(data.seed);
        for (int x = 0; x < data.width; x++)
            for (int y = 0; y < data.height; y++)
            {
                if (noGrass.Contains(new Vector2Int(x, y))) continue;
                // Cỏ phủ lên floor, bo góc ở ranh giới với đường/khu/ao
                var tile = PickGrassTileWithCorners(x, y, data.width, data.height, noGrass, new HashSet<Vector2Int>(), rand);
                if (tile != null)
                    GrassTilemap.SetTile(new Vector3Int(x, y, 0), tile);
            }
        Debug.Log("[FarmMap] Đã phủ grass (wall) lên trên floor");
    }

    // Chọn tile cỏ theo neighbor (autotile bo góc — cỏ gặp đường/nước)
    TileBase PickGrassTileWithCorners(int x, int y, int w, int h, HashSet<Vector2Int> pathSet, HashSet<Vector2Int> noGrassSet, System.Random rand)
    {
        // Cỏ được bo góc khi kề đường đất hoặc ao
        bool IsGrass(int nx, int ny)
        {
            if (nx < 0 || nx >= w || ny < 0 || ny >= h) return true; // ngoài map = coi như cỏ (rìa map không bo góc)
            if (pathSet.Contains(new Vector2Int(nx, ny))) return false; // đường đất
            if (noGrassSet.Contains(new Vector2Int(nx, ny))) return false; // sông/hồ/khu
            return true;
        }

        bool N = IsGrass(x, y + 1);
        bool S = IsGrass(x, y - 1);
        bool W = IsGrass(x - 1, y);
        bool E = IsGrass(x + 1, y);
        bool NE = IsGrass(x + 1, y + 1);
        bool NW = IsGrass(x - 1, y + 1);
        bool SE = IsGrass(x + 1, y - 1);
        bool SW = IsGrass(x - 1, y - 1);

        // Góc ngoài
        if (!N && !E && grassCornerOuterTR != null) return grassCornerOuterTR;
        if (!N && !W && grassCornerOuterTL != null) return grassCornerOuterTL;
        if (!S && !E && grassCornerOuterBR != null) return grassCornerOuterBR;
        if (!S && !W && grassCornerOuterBL != null) return grassCornerOuterBL;

        // Viền
        if (!N && grassEdgeTop != null) return grassEdgeTop;
        if (!S && grassEdgeBottom != null) return grassEdgeBottom;
        if (!W && grassEdgeLeft != null) return grassEdgeLeft;
        if (!E && grassEdgeRight != null) return grassEdgeRight;

        // Góc trong
        if (N && E && !NE && grassCornerInnerTR != null) return grassCornerInnerTR;
        if (N && W && !NW && grassCornerInnerTL != null) return grassCornerInnerTL;
        if (S && E && !SE && grassCornerInnerBR != null) return grassCornerInnerBR;
        if (S && W && !SW && grassCornerInnerBL != null) return grassCornerInnerBL;

        return PickWeightedTile(grassTiles, rand);
    }


    // Vẽ sông + lưu tập hợp ô nước (phục vụ phản chiếu)
    void BuildWater(MapData data)
    {
        waterSet.Clear();
        lakeSet.Clear();
        if (data.rivers != null)
            foreach (var rv in data.rivers) waterSet.Add(new Vector2Int(rv.x, rv.y));
        if (data.lakes != null)
            foreach (var lk in data.lakes) { waterSet.Add(new Vector2Int(lk.x, lk.y)); lakeSet.Add(new Vector2Int(lk.x, lk.y)); }

        if (WaterTilemap == null) return;
        WaterTilemap.ClearAllTiles();
        int waterCount = 0;
        foreach (var w in waterSet)
        {
            if (waterTile != null)
                WaterTilemap.SetTile(new Vector3Int(w.x, w.y, 0), waterTile);
            else
            {
                var t = PickWeightedTile(pathTiles, new System.Random(w.x * 31 + w.y));
                if (t != null) WaterTilemap.SetTile(new Vector3Int(w.x, w.y, 0), t);
            }
            waterCount++;
        }
        Debug.Log($"[FarmMap] Đã vẽ {waterCount} ô nước (sông + hồ)");

        // Viền nước v40: luật chọn tile của v35, DỊCH RA 1 Ô đặt lên đất (overlay).
        // Nước giữ nguyên tile giữa (hồ không bị thụt 1 ô như v35).
        if (WaterEdgeTilemap != null)
        {
            WaterEdgeTilemap.ClearAllTiles();
            var skipEdge = new HashSet<Vector2Int>();
            if (data.paths != null)
                foreach (var pt in data.paths) skipEdge.Add(new Vector2Int(pt.x, pt.y));
            if (data.zones != null)
            {
                foreach (var z in new[] { data.zones.home, data.zones.dungeon })
                {
                    if (z == null) continue;
                    for (int zx = z.x; zx < z.x + z.w; zx++)
                        for (int zy = z.y; zy < z.y + z.h; zy++)
                            skipEdge.Add(new Vector2Int(zx, zy));
                }
            }
            if (waterCornerInnerTL == null || waterCornerInnerTR == null || waterCornerInnerBL == null || waterCornerInnerBR == null)
                Debug.LogWarning("[FarmMap] Chưa gán đủ 4 tile inner (TL/TR/BL/BR) — góc inner sẽ không xuất hiện, kiểm tra Inspector");
            if (waterCornerOuterTL == null || waterCornerOuterTR == null || waterCornerOuterBL == null || waterCornerOuterBR == null)
                Debug.LogWarning("[FarmMap] Chưa gán đủ 4 tile outer (TL/TR/BL/BR) — góc outer sẽ thiếu, kiểm tra Inspector");

            // Pass 1: gom tile viền + góc (góc đè lên viền khi trùng ô)
            var edgeTiles = new Dictionary<Vector2Int, TileBase>();
            var cornerTiles = new Dictionary<Vector2Int, TileBase>();
            foreach (var p in lakeSet)
            {
                if (!TryGetShiftedEdge(p.x, p.y, data.width, data.height, out var t, out var lp)) continue;
                if (lp.x < 0 || lp.x >= data.width || lp.y < 0 || lp.y >= data.height) continue;
                if (waterSet.Contains(lp)) continue; // không vẽ lên sông
                if (skipEdge.Contains(lp)) continue; // đường đi/khu nhà không vẽ viền
                bool isCorner = t == waterCornerOuterTL || t == waterCornerOuterTR || t == waterCornerOuterBL || t == waterCornerOuterBR
                    || t == waterCornerInnerTL || t == waterCornerInnerTR || t == waterCornerInnerBL || t == waterCornerInnerBR;
                if (isCorner) cornerTiles[lp] = t;
                else edgeTiles[lp] = t;
            }
            foreach (var kv in edgeTiles) WaterEdgeTilemap.SetTile(new Vector3Int(kv.Key.x, kv.Key.y, 0), kv.Value);
            foreach (var kv in cornerTiles) WaterEdgeTilemap.SetTile(new Vector3Int(kv.Key.x, kv.Key.y, 0), kv.Value);

            // Pass 2 fallback: ô đất kề hồ chưa có tile (thường chỉ kề chéo) -> luật đất cũ
            var placed = new HashSet<Vector2Int>(edgeTiles.Keys);
            placed.UnionWith(cornerTiles.Keys);
            int fallbackCount = 0;
            for (int x = 0; x < data.width; x++)
                for (int y = 0; y < data.height; y++)
                {
                    var pos = new Vector2Int(x, y);
                    if (waterSet.Contains(pos)) continue;
                    if (skipEdge.Contains(pos)) continue;
                    if (placed.Contains(pos)) continue;
                    var edge = PickWaterEdgeTile(x, y, data.width, data.height);
                    if (edge != null)
                    {
                        WaterEdgeTilemap.SetTile(new Vector3Int(x, y, 0), edge);
                        placed.Add(pos);
                        fallbackCount++;
                    }
                }

            int innerCount = 0, outerCount = 0;
            foreach (var kv in cornerTiles)
            {
                if (kv.Value == waterCornerInnerTL || kv.Value == waterCornerInnerTR || kv.Value == waterCornerInnerBL || kv.Value == waterCornerInnerBR) innerCount++;
                else outerCount++;
            }
            Debug.Log($"[FarmMap] Viền nước v40 (luật v35 dịch ra đất): {placed.Count} ô (inner={innerCount}, outer={outerCount}, viền={edgeTiles.Count}, fallback={fallbackCount}, hồ={lakeSet.Count} ô, seed={data.seed})");
        }
        else
        {
            Debug.LogWarning("[FarmMap] WaterEdgeTilemap chưa gán — không vẽ được viền nước, kiểm tra Inspector");
        }
    }

    // Kiểm tra ô có phải nước không (sông/ao)
    public bool IsWater(int gx, int gy) => waterSet.Contains(new Vector2Int(gx, gy));

    // Kiểm tra có nước kề bên không (để hiện phản chiếu khi đứng sát bờ)
    public bool IsNearWater(int gx, int gy)
    {
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                if (waterSet.Contains(new Vector2Int(gx + dx, gy + dy))) return true;
        return false;
    }

    // Luật chọn tile của v35 (outer -> viền -> inner) nhưng DỊCH RA 1 Ô đặt lên đất.
    // v35 vẽ thẳng lên ô nước nên hồ bị thụt 1 ô; bản này giữ nguyên loại tile của v35
    // mà đặt lên ô đất kề (overlay), nước giữ nguyên diện tích.
    // Trả về false nếu ô nước này là giữa hồ (không cần viền).
    bool TryGetShiftedEdge(int x, int y, int w, int h, out TileBase tile, out Vector2Int landPos)
    {
        tile = null;
        landPos = new Vector2Int(x, y);
        bool IsWater(int nx, int ny)
        {
            if (nx < 0 || nx >= w || ny < 0 || ny >= h) return true; // ngoài map = coi như nước
            return waterSet.Contains(new Vector2Int(nx, ny)); // giống v35: sông cũng tính là nước
        }

        bool N = IsWater(x, y + 1);
        bool S = IsWater(x, y - 1);
        bool W = IsWater(x - 1, y);
        bool E = IsWater(x + 1, y);
        bool NE = IsWater(x + 1, y + 1);
        bool NW = IsWater(x - 1, y + 1);
        bool SE = IsWater(x + 1, y - 1);
        bool SW = IsWater(x - 1, y - 1);

        // Góc outer (thiếu 2 phía) -> ô đất chéo
        if (!N && !E && waterCornerOuterTR != null) { tile = waterCornerOuterTR; landPos = new Vector2Int(x + 1, y + 1); return true; }
        if (!N && !W && waterCornerOuterTL != null) { tile = waterCornerOuterTL; landPos = new Vector2Int(x - 1, y + 1); return true; }
        if (!S && !E && waterCornerOuterBR != null) { tile = waterCornerOuterBR; landPos = new Vector2Int(x + 1, y - 1); return true; }
        if (!S && !W && waterCornerOuterBL != null) { tile = waterCornerOuterBL; landPos = new Vector2Int(x - 1, y - 1); return true; }
        // Viền (thiếu 1 phía) -> ô đất vuông góc
        if (!N && waterEdgeTop != null) { tile = waterEdgeTop; landPos = new Vector2Int(x, y + 1); return true; }
        if (!S && waterEdgeBottom != null) { tile = waterEdgeBottom; landPos = new Vector2Int(x, y - 1); return true; }
        if (!W && waterEdgeLeft != null) { tile = waterEdgeLeft; landPos = new Vector2Int(x - 1, y); return true; }
        if (!E && waterEdgeRight != null) { tile = waterEdgeRight; landPos = new Vector2Int(x + 1, y); return true; }
        // Góc inner (bao 2 phía, khuyết chéo) -> ô đất chéo chỗ khuyết
        if (N && E && !NE && waterCornerInnerTR != null) { tile = waterCornerInnerTR; landPos = new Vector2Int(x + 1, y + 1); return true; }
        if (N && W && !NW && waterCornerInnerTL != null) { tile = waterCornerInnerTL; landPos = new Vector2Int(x - 1, y + 1); return true; }
        if (S && E && !SE && waterCornerInnerBR != null) { tile = waterCornerInnerBR; landPos = new Vector2Int(x + 1, y - 1); return true; }
        if (S && W && !SW && waterCornerInnerBL != null) { tile = waterCornerInnerBL; landPos = new Vector2Int(x - 1, y - 1); return true; }
        return false;
    }

    TileBase PickWaterEdgeTile(int x, int y, int w, int h)
    {
        bool IsWater(int nx, int ny)
        {
            if (nx < 0 || nx >= w || ny < 0 || ny >= h) return true; // ngoài map = coi như nước (rìa map không bo góc)
            return lakeSet.Contains(new Vector2Int(nx, ny));
        }

        bool N = IsWater(x, y + 1);
        bool S = IsWater(x, y - 1);
        bool W = IsWater(x - 1, y);
        bool E = IsWater(x + 1, y);
        bool NE = IsWater(x + 1, y + 1);
        bool NW = IsWater(x - 1, y + 1);
        bool SE = IsWater(x + 1, y - 1);
        bool SW = IsWater(x - 1, y - 1);

        // Ô đất không kề nước thì không vẽ viền
        if (!N && !S && !W && !E && !NE && !NW && !SE && !SW) return null;

        // Góc trong (nước bao 2 phía nhưng khuyết chéo) — PHẢI trước góc outer,
        // vì điều kiện inner là tập con của outer (outer không có điều kiện chéo sẽ cướp hết)
        if (N && E && !NE && waterCornerInnerTR != null) return waterCornerInnerTR;
        if (N && W && !NW && waterCornerInnerTL != null) return waterCornerInnerTL;
        if (S && E && !SE && waterCornerInnerBR != null) return waterCornerInnerBR;
        if (S && W && !SW && waterCornerInnerBL != null) return waterCornerInnerBL;

        // Góc vuông thật (nước ở 2 phía vuông góc)
        if (S && W && waterCornerOuterTR != null) return waterCornerOuterTR;
        if (S && E && waterCornerOuterTL != null) return waterCornerOuterTL;
        if (N && W && waterCornerOuterBR != null) return waterCornerOuterBR;
        if (N && E && waterCornerOuterBL != null) return waterCornerOuterBL;

        // Bước chéo: nước ở 1 phía + ĐÚNG 1 chéo cùng phía -> góc
        // (bờ thẳng có 2 chéo thì không vào đây, để xuống viền)
        if (S && SW && !SE && waterCornerOuterTR != null) return waterCornerOuterTR;
        if (S && SE && !SW && waterCornerOuterTL != null) return waterCornerOuterTL;
        if (N && NW && !NE && waterCornerOuterBR != null) return waterCornerOuterBR;
        if (N && NE && !NW && waterCornerOuterBL != null) return waterCornerOuterBL;
        if (W && SW && !NW && waterCornerOuterTR != null) return waterCornerOuterTR;
        if (W && NW && !SW && waterCornerOuterBR != null) return waterCornerOuterBR;
        if (E && SE && !NE && waterCornerOuterTL != null) return waterCornerOuterTL;
        if (E && NE && !SE && waterCornerOuterBL != null) return waterCornerOuterBL;

        // Viền (nước ở 1 phía) — tile L/R/D/U dùng ở đây, kể cả bờ thẳng có 2 chéo
        if (S && waterEdgeTop != null) return waterEdgeTop;
        if (N && waterEdgeBottom != null) return waterEdgeBottom;
        if (W && waterEdgeRight != null) return waterEdgeRight;
        if (E && waterEdgeLeft != null) return waterEdgeLeft;

        // Góc chỉ kề chéo (hồ tròn: ô bờ chỉ chạm nước ở góc chéo)
        if (SW && waterCornerOuterTR != null) return waterCornerOuterTR;
        if (SE && waterCornerOuterTL != null) return waterCornerOuterTL;
        if (NW && waterCornerOuterBR != null) return waterCornerOuterBR;
        if (NE && waterCornerOuterBL != null) return waterCornerOuterBL;

        return null;
    }

    // FLOOR kiểu dungeon: lát ĐẤT kín map làm nền
    void BuildGround(MapData data)
    {
        // Lát đất kín map bằng tile ground (floor)
        if (pathTiles != null && pathTiles.Length > 0 && FloorTilemap != null)
        {
            FloorTilemap.ClearAllTiles();
            var rand = new System.Random(data.seed);
            for (int x = 0; x < data.width; x++)
                for (int y = 0; y < data.height; y++)
                {
                    var tile = PickWeightedTile(pathTiles, rand);
                    if (tile != null)
                        FloorTilemap.SetTile(new Vector3Int(x, y, 0), tile);
                }
            Debug.Log("[FarmMap] Đã lát floor (đất) kín map");
            return;
        }

        // Không có tiles → texture gen sẵn (fallback)
        int texSize = 256;
        var tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point; // giữ pixel rõ
        var randFallback = new System.Random(data.seed);

        var pixels = new Color[texSize * texSize];
        for (int i = 0; i < pixels.Length; i++)
        {
            // Trộn 2 màu cỏ + nhiễu nhẹ
            float t = (float)randFallback.NextDouble();
            Color c = Color.Lerp(grassColorA, grassColorB, t);
            float n = (float)(randFallback.NextDouble() - 0.5) * 0.06f;
            c.r += n; c.g += n; c.b += n;
            pixels[i] = c;
        }
        tex.SetPixels(pixels);
        tex.Apply();

        var go = new GameObject("Ground");
        go.transform.SetParent(mapRoot.transform, false);
        // Đặt giữa map
        go.transform.position = new Vector3(data.width * tileSize / 2f, data.height * tileSize / 2f, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, texSize, texSize),
            new Vector2(0.5f, 0.5f), texSize / (data.width * tileSize));
        sr.sortingOrder = floorOrder;
    }

    // Lấy Order in Layer từ tilemap được gán (nếu có), không thì dùng số mặc định
    int TilemapOrderOr(Tilemap tm, int fallback)
    {
        if (tm == null) return fallback;
        var r = tm.GetComponent<TilemapRenderer>();
        return r != null ? r.sortingOrder : fallback;
    }

    void SpawnResource(int gx, int gy, ResourceType type, int amount)
    {
        var go = new GameObject(type == ResourceType.Tree ? $"Tree_{gx}_{gy}" : $"Rock_{gx}_{gy}");
        go.transform.SetParent(mapRoot.transform, false);
        go.transform.position = new Vector3(gx * tileSize, gy * tileSize, 0);

        // CÂY 2 LỚP: thân (collider) + tán (che player)
        if (type == ResourceType.Tree && (treeCanopySprite != null || (treeCanopyTiles != null && treeCanopyTiles.Length > 0)))
        {
            // Lớp thân: order ngang player, có collider ở gốc
            var trunk = new GameObject("Trunk");
            trunk.transform.SetParent(go.transform, false);
            var trunkSr = trunk.AddComponent<SpriteRenderer>();
            trunkSr.sprite = treeTrunkSprite != null ? treeTrunkSprite : treeSprite;
            int trunkOrder = TilemapOrderOr(treeTilemap, treeTrunkOrder);
            trunkSr.sortingOrder = trunkOrder;
            var col = trunk.AddComponent<BoxCollider2D>();
            col.size = treeColliderSize;
            col.offset = treeColliderOffset; // chỉ chặn ở gốc thân

            // Lớp tán: order cao hơn player, KHÔNG collider
            var canopy = new GameObject("Canopy");
            canopy.transform.SetParent(go.transform, false);
            canopy.transform.localPosition = new Vector3(0, canopyOffsetY, 0);
            var canopySr = canopy.AddComponent<SpriteRenderer>();
            // Nhiều mẫu tán theo tỉ lệ (deterministic theo seed + tọa độ)
            Sprite canopySprite = treeCanopySprite;
            if (treeCanopyTiles != null && treeCanopyTiles.Length > 0)
            {
                var canopyRand = new System.Random(currentSeed + gx * 73856093 + gy * 19349663);
                var picked = PickWeightedSprite(treeCanopyTiles, canopyRand);
                if (picked != null) canopySprite = picked;
            }
            canopySr.sprite = canopySprite;
            canopySr.sortingOrder = trunkOrder + (treeCanopyOrder - treeTrunkOrder);
        }
        else if (type == ResourceType.Rock)
        {
            // Đá nhiều tile: chọn deterministic theo seed + tọa độ (giống tán cây)
            var sr = go.AddComponent<SpriteRenderer>();
            Sprite rockPicked = rockSprite;
            if (rockTiles != null && rockTiles.Length > 0)
            {
                var rockRand = new System.Random(currentSeed + gx * 73856093 + gy * 19349663);
                var picked = PickWeightedSprite(rockTiles, rockRand);
                if (picked != null) rockPicked = picked;
            }
            if (rockPicked != null) sr.sprite = rockPicked;
            else
            {
                sr.sprite = MakeSquareSprite(rockColor);
                go.transform.localScale = Vector3.one * 0.9f;
            }
            sr.sortingOrder = TilemapOrderOr(rockTilemap, resourceOrder);

            var col = go.AddComponent<BoxCollider2D>();
            col.size = rockColliderSize;
            col.offset = rockColliderOffset;

            string key = rockPicked != null ? rockPicked.name : "vuông";
            if (!rockVariantCounts.ContainsKey(key)) rockVariantCounts[key] = 0;
            rockVariantCounts[key]++;
        }
        else
        {
            // Cây chưa tách tán: 1 sprite như cũ
            var sr = go.AddComponent<SpriteRenderer>();
            if (treeSprite != null) sr.sprite = treeSprite;
            else
            {
                sr.sprite = MakeSquareSprite(treeColor);
                go.transform.localScale = Vector3.one * 0.9f;
            }
            sr.sortingOrder = TilemapOrderOr(treeTilemap, resourceOrder);

            var col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one * 0.9f;
        }

        var node = go.AddComponent<ResourceNode>();
        node.type = type;
        node.amount = amount;
        node.gridPos = new Vector2Int(gx, gy);
        resources[node.gridPos] = node;
    }


    // Tạo sprite vuông 1 màu (placeholder)
    Sprite MakeSquareSprite(Color c)
    {
        var tex = new Texture2D(16, 16);
        var p = new Color[256];
        for (int i = 0; i < p.Length; i++) p[i] = c;
        tex.SetPixels(p); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
    }

    Sprite MakeCircleSprite(Color c)
    {
        int s = 32;
        var tex = new Texture2D(s, s);
        var p = new Color[s * s];
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(s / 2f, s / 2f));
                Color col = c; col.a = d <= s / 2f ? 1f : 0f;
                p[y * s + x] = col;
            }
        tex.SetPixels(p); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
    }

    // ── DATA ──────────────────────────────────────────
    [System.Serializable] class MapData
    {
        public int seed; public int width; public int height;
        public Pos spawn;
        public List<TreeData> trees; public List<RockData> rocks; public List<Pos> lakes;
        public List<GrassData> grass; public Pos dungeonPortal;
        public List<PathData> paths;
        public List<Pos> rivers;
        public ZonesData zones;
    }
    [System.Serializable] class Pos { public int x; public int y; }
    [System.Serializable] class TreeData { public int x; public int y; public int wood; }
    [System.Serializable] class RockData { public int x; public int y; public int stone; }
    [System.Serializable] class GrassData { public int x; public int y; public int v; }
    [System.Serializable] class PathData { public int x; public int y; }
    [System.Serializable] class ZonesData { public ZoneRect home; public ZoneRect dungeon; public ZoneRect field; }
    [System.Serializable] class ZoneRect { public int x; public int y; public int w; public int h; }
}

// Click cổng → xin vào dungeon
public class DungeonPortal : MonoBehaviour
{
    void OnMouseDown()
    {
        Debug.Log("[FarmMap] Click cổng dungeon");
        if (NetworkManager.Instance != null && NetworkManager.Instance.IsConnected)
            NetworkManager.Instance.Emit("enter_dungeon", "{}");
    }
}

public enum ResourceType { Tree, Rock }

// Gắn trên mỗi cây/đá — sau này dùng để chặt/đập
public class ResourceNode : MonoBehaviour
{
    public ResourceType type;
    public int amount; // số gỗ/đá còn lại
    public Vector2Int gridPos;
}
