// DungeonTilemapPainter.cs — vẽ DungeonData lên Tilemap.
// floorMap: vẽ ô sàn, mỗi ô chọn 1 trong 3 biến thể tile sàn theo tỉ lệ
//   trọng số (mặc định 60/25/15).
// wallMap: chỉ vẽ tường KỀ sàn (không vẽ cả vùng void), dùng 2 tile theo
// hướng tường:
//   - wallHTile: tường ngang chung (dự phòng)
//   - wallTopTile: tường TRÊN — tường ngang có sàn ngay phía dưới (tối)
//   - wallBottomTile: tường DƯỚI — tường ngang có sàn ngay phía trên (sáng)
//   - wallVTile: tường DỌC 2 bên
// Xen kẽ: cứ mỗi lightTileInterval ô tường thì chèn 1 tile đèn
// (wallLightHTile / wallLightVTile theo đúng hướng tường).
// Kèm TilemapCollider2D + CompositeCollider2D để nhân vật không đi xuyên tường.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Serialization;

public class DungeonTilemapPainter : MonoBehaviour
{
    [Header("Tilemaps (tạo trong scene Dungeon)")]
    public Tilemap floorMap;
    public Tilemap wallMap;

    [Header("Tiles (kéo từ Tile Palette vào)")]
    [Tooltip("Tile sàn chính (tỉ lệ cao nhất)")]
    [FormerlySerializedAs("floorTile")]
    public TileBase floorTileA;
    [Tooltip("Tile sàn biến thể 2")]
    public TileBase floorTileB;
    [Tooltip("Tile sàn biến thể 3")]
    public TileBase floorTileC;

    [Header("Tỉ lệ tile sàn (%)")]
    [Range(0, 100)] public float floorWeightA = 60f;
    [Range(0, 100)] public float floorWeightB = 25f;
    [Range(0, 100)] public float floorWeightC = 15f;
    [Tooltip("Tường ngang chung (dự phòng khi không xác định được trên/dưới)")]
    [FormerlySerializedAs("wallTile")]
    [FormerlySerializedAs("wallTopTile")]
    public TileBase wallHTile;
    [Tooltip("Tường TRÊN: tường ngang có sàn ngay phía dưới (mặt sau, tối)")]
    [FormerlySerializedAs("wallHBottomTile")]
    public TileBase wallTopTile;
    [Tooltip("Tường DƯỚI: tường ngang có sàn ngay phía trên (mặt trước, sáng)")]
    public TileBase wallBottomTile;
    [Tooltip("Tường dọc 2 bên")]
    [FormerlySerializedAs("wallFrontTile")]
    public TileBase wallVTile;
    [Tooltip("Tile tường nứt (tùy chọn) — vẽ ở ô cửa phòng bí mật khi còn khóa, để người tinh mắt phát hiện")]
    public TileBase lockedDoorTile;

    // Vị trí các ô tile đèn đã vẽ (dùng để đặt đuốc câu đố) — tọa độ tile
    [HideInInspector]
    public List<Vector2Int> lightTileCells = new();

    [Header("Tile đèn tường (tùy chọn)")]
    [Tooltip("Tile đuốc/đèn cho tường NGANG — bạn tự vẽ/cung cấp")]
    [FormerlySerializedAs("wallLightTile")]
    public TileBase wallLightHTile;
    [Tooltip("Tile đuốc/đèn cho tường DỌC — bạn tự vẽ/cung cấp")]
    public TileBase wallLightVTile;
    [Tooltip("Cứ mỗi N ô tường thì chèn 1 tile đèn. Để 0 hoặc không gán tile = tắt.")]
    public int lightTileInterval = 5;
    [Tooltip("Prefab có Point Light 2D — nếu gán, mỗi tile đèn sẽ phát sáng thật. Không gán thì chỉ vẽ tile.")]
    public GameObject wallLightPrefab;

    // Chứa các đèn đã spawn (để xóa khi vẽ lại map)
    Transform lightContainer;

    public void Paint(DungeonData data)
    {
        if (floorMap == null || wallMap == null || floorTileA == null ||
            wallHTile == null || wallVTile == null)
        {
            Debug.LogError("[Dungeon] Chưa gán đủ floorMap/wallMap/floorTileA/wallHTile/wallVTile trong Inspector!");
            return;
        }

        floorMap.ClearAllTiles();
        wallMap.ClearAllTiles();
        lightTileCells.Clear();

        // RNG riêng cho biến thể sàn — dùng seed của dungeon nên cùng seed = cùng map y hệt
        var floorRng = new System.Random(data.seed);

        // Xóa đèn của lần vẽ trước (nếu vẽ lại map)
        if (lightContainer != null) Destroy(lightContainer.gameObject);
        lightContainer = new GameObject("WallLights").transform;
        lightContainer.SetParent(transform, false);

        bool useLightTiles = lightTileInterval > 0 &&
                               (wallLightHTile != null || wallLightVTile != null);
        int wallCount = 0;

        // Tập ô cửa phòng bí mật — không vẽ tile đèn ở đây
        var doorCellSet = new HashSet<Vector2Int>();
        if (data.secretDoors != null)
        {
            foreach (var door in data.secretDoors)
                foreach (var dc in door.doorCells)
                    doorCellSet.Add(dc);
        }

        for (int y = 0; y < data.height; y++)
        {
            for (int x = 0; x < data.width; x++)
            {
                var cell = new Vector3Int(x, y, 0);
                if (data.IsFloor(x, y))
                {
                    // Sàn: chọn 1 trong 3 biến thể theo tỉ lệ trọng số
                    floorMap.SetTile(cell, PickFloorTile(floorRng));
                }
                else if (BordersFloor(data, x, y))
                {
                    wallCount++;
                    // Cứ mỗi N ô tường thì chèn 1 tile đèn (đúng hướng tường),
                    // trừ ô cửa phòng bí mật
                    TileBase lightTile = (useLightTiles && wallCount % lightTileInterval == 0 &&
                                          !doorCellSet.Contains(new Vector2Int(x, y)))
                        ? PickLightTile(data, x, y)
                        : null;
                    if (lightTile != null)
                    {
                        wallMap.SetTile(cell, lightTile);
                        lightTileCells.Add(new Vector2Int(x, y));
                        SpawnWallLight(cell);
                    }
                    else
                    {
                        wallMap.SetTile(cell, PickWallTile(data, x, y));
                    }
                }
            }
        }

        // Vẽ tile cửa khóa ở các ô cửa phòng bí mật (nếu có gán lockedDoorTile)
        if (lockedDoorTile != null)
        {
            foreach (var dc in doorCellSet)
                wallMap.SetTile(new Vector3Int(dc.x, dc.y, 0), lockedDoorTile);
        }

        floorMap.CompressBounds();
        wallMap.CompressBounds();
        EnsureWallCollider();

        Debug.Log("[Dungeon] Đã vẽ tilemap xong.");
    }

    // Chọn tile sàn theo tỉ lệ trọng số (vd 60/25/15).
    // Tile nào chưa gán thì tỉ lệ của nó = 0 (bỏ qua).
    TileBase PickFloorTile(System.Random rng)
    {
        float wA = floorTileA != null ? floorWeightA : 0f;
        float wB = floorTileB != null ? floorWeightB : 0f;
        float wC = floorTileC != null ? floorWeightC : 0f;
        float total = wA + wB + wC;
        if (total <= 0f) return floorTileA; // fallback, không bao giờ null (đã check ở Paint)

        float r = (float)(rng.NextDouble() * total);
        if (r < wA) return floorTileA;
        if (r < wA + wB) return floorTileB;
        return floorTileC;
    }

    // Chọn tile tường theo vị trí tương đối với sàn:
    // - Tường kề sàn 2 bên đông/tây (chạy dọc) -> tile dọc
    // - Tường ngang có sàn ngay phía dưới (tường TRÊN của phòng) -> tile trên (tối)
    // - Tường ngang có sàn ngay phía trên (tường DƯỚI của phòng) -> tile dưới (sáng)
    // - Còn lại -> tile ngang chung
    TileBase PickWallTile(DungeonData data, int x, int y)
    {
        if (IsVerticalWall(data, x, y))
            return wallVTile;

        bool floorBelow = data.IsFloor(x, y - 1);
        bool floorAbove = data.IsFloor(x, y + 1);

        if (floorBelow && wallTopTile != null)
            return wallTopTile;
        if (floorAbove && wallBottomTile != null)
            return wallBottomTile;
        return wallHTile;
    }

    // Chọn tile đèn theo hướng tường. Nếu chỉ gán 1 trong 2 thì dùng cái đã gán.
    // Trả về null khi chưa gán tile đèn nào.
    TileBase PickLightTile(DungeonData data, int x, int y)
    {
        bool isV = IsVerticalWall(data, x, y);
        TileBase t = isV ? wallLightVTile : wallLightHTile;
        if (t == null) t = isV ? wallLightHTile : wallLightVTile; // fallback
        return t;
    }

    bool IsVerticalWall(DungeonData data, int x, int y)
    {
        bool floorE = data.IsFloor(x + 1, y);
        bool floorW = data.IsFloor(x - 1, y);
        bool floorN = data.IsFloor(x, y + 1);
        bool floorS = data.IsFloor(x, y - 1);

        return (floorE || floorW) && !(floorN || floorS);
    }

    // Spawn prefab đèn (Point Light 2D) tại ô tile đèn — chỉ khi có gán prefab.
    // Đèn là con của WallLights nên vẽ lại map sẽ tự xóa đèn cũ.
    void SpawnWallLight(Vector3Int cell)
    {
        if (wallLightPrefab == null || lightContainer == null) return;
        // Tâm ô tile (tôn trọng vị trí thật của Grid)
        Vector3 pos = wallMap.CellToWorld(cell) + wallMap.cellSize * 0.5f;
        pos.z = 0f;
        var light = Instantiate(wallLightPrefab, pos, Quaternion.identity);
        light.transform.SetParent(lightContainer, true);
    }

    // Chỉ vẽ tường khi kề ô sàn (8 hướng) — tránh vẽ kín cả vùng void
    bool BordersFloor(DungeonData data, int x, int y)
    {
        for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                if (data.IsFloor(x + dx, y + dy)) return true;
            }
        return false;
    }

    // Đảm bảo tường có collider. Gắn tay trong Inspector cũng được —
    // hàm này chỉ thêm những gì còn thiếu, không tạo trùng.
    // Sau khi gọi sẽ log xác nhận để bạn kiểm tra.
    void EnsureWallCollider()
    {
        var rb = wallMap.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = wallMap.gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        var tileCol = wallMap.GetComponent<TilemapCollider2D>();
        if (tileCol == null)
            tileCol = wallMap.gameObject.AddComponent<TilemapCollider2D>();

        var comp = wallMap.GetComponent<CompositeCollider2D>();
        if (comp == null)
            comp = wallMap.gameObject.AddComponent<CompositeCollider2D>();

        tileCol.usedByComposite = true;

        // Xác nhận để dễ debug: nếu thiếu cái nào log sẽ báo ngay
        Debug.Log($"[Dungeon] Collider tường: Rigidbody2D={rb != null}, " +
                  $"TilemapCollider2D={tileCol != null}, " +
                  $"CompositeCollider2D={comp != null}");
    }

    // Đổi tọa độ tile -> world (tâm ô)
    public Vector3 CellToWorld(Vector2Int cell)
    {
        // Giả định tilemap đặt tại gốc, mỗi ô = 1 unit, tâm ô lệch 0.5
        return new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
    }

    // Mở cửa phòng bí mật: đổi các ô tường của cửa thành sàn.
    // TilemapCollider2D + CompositeCollider2D tự cập nhật khi tile đổi.
    public void OpenDoor(List<Vector2Int> doorCells)
    {
        if (doorCells == null || wallMap == null) return;
        foreach (var c in doorCells)
        {
            // RNG ổn định theo vị trí ô để biến thể sàn nhất quán
            var rng = new System.Random(c.x * 73856093 ^ c.y * 19349663);
            wallMap.SetTile(new Vector3Int(c.x, c.y, 0), PickFloorTile(rng));
        }
        Debug.Log($"[Dungeon] Đã mở cửa bí mật ({doorCells.Count} ô).");
    }
}
