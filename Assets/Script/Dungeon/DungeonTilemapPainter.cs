// DungeonTilemapPainter.cs — vẽ DungeonData lên Tilemap.
// floorMap: vẽ ô sàn, mỗi ô chọn 1 trong 3 biến thể tile sàn theo tỉ lệ
//   trọng số (mặc định 60/25/15).
// wallMap: vẽ tường theo kiểu KHUNG VIỀN 1 ô có hướng quanh vùng sàn
// (không vẽ khối đặc như trước):
//   - wallTopTile:    viền TRÊN — ô tường có sàn ngay phía dưới (mặt sau, tối)
//   - wallBottomTile: viền DƯỚI — ô tường có sàn ngay phía trên (mặt trước, sáng)
//   - wallVLeftTile:  viền TRÁI (bạn cung cấp; dự phòng wallVTile)
//   - wallVRightTile: viền PHẢI (bạn cung cấp; dự phòng wallVTile)
//   - wallHTile / wallVTile: dự phòng chung
// - 4 tile góc NGOÀI (wallCornerTopLeft/...): vá ô trống ở góc lồi
// - 4 tile góc TRONG (wallCornerInnerTopLeft/...): chỗ hành lang nối phòng
// Xen kẽ: cứ mỗi lightTileInterval ô tường thì chèn 1 tile đèn
// (biến thể theo đúng hướng tường: trên/dưới/trái/phải).
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
    [Tooltip("Tường dọc TRÁI (bạn cung cấp) — dự phòng: wallVTile rồi wallHTile")]
    public TileBase wallVLeftTile;
    [Tooltip("Tường dọc PHẢI (bạn cung cấp) — dự phòng: wallVTile rồi wallHTile")]
    public TileBase wallVRightTile;

    [Header("Tile góc NGOÀI (lồi) — vá ô trống ở 4 góc khung tường")]
    [Tooltip("Góc ngoài TRÊN-TRÁI (bạn cung cấp) — dự phòng: wallHTile")]
    public TileBase wallCornerTopLeft;
    [Tooltip("Góc ngoài TRÊN-PHẢI (bạn cung cấp) — dự phòng: wallHTile")]
    public TileBase wallCornerTopRight;
    [Tooltip("Góc ngoài DƯỚI-TRÁI (bạn cung cấp) — dự phòng: wallHTile")]
    public TileBase wallCornerBottomLeft;
    [Tooltip("Góc ngoài DƯỚI-PHẢI (bạn cung cấp) — dự phòng: wallHTile")]
    public TileBase wallCornerBottomRight;

    [Header("Tile góc TRONG (lõm) — chỗ hành lang nối phòng")]
    [Tooltip("Góc trong TRÊN-TRÁI (bạn cung cấp) — dự phòng: tile tường theo hướng")]
    public TileBase wallCornerInnerTopLeft;
    [Tooltip("Góc trong TRÊN-PHẢI (bạn cung cấp) — dự phòng: tile tường theo hướng")]
    public TileBase wallCornerInnerTopRight;
    [Tooltip("Góc trong DƯỚI-TRÁI (bạn cung cấp) — dự phòng: tile tường theo hướng")]
    public TileBase wallCornerInnerBottomLeft;
    [Tooltip("Góc trong DƯỚI-PHẢI (bạn cung cấp) — dự phòng: tile tường theo hướng")]
    public TileBase wallCornerInnerBottomRight;
    [Tooltip("Tile tường nứt (tùy chọn) — vẽ ở ô cửa phòng bí mật khi còn khóa, để người tinh mắt phát hiện")]
    public TileBase lockedDoorTile;

    // Vị trí các ô tile đèn đã vẽ (dùng để đặt đuốc câu đố) — tọa độ tile
    [HideInInspector]
    public List<Vector2Int> lightTileCells = new();

    [Header("Tile đèn tường (tùy chọn)")]
    [Tooltip("Tile đèn cho tường NGANG (dự phòng chung)")]
    [FormerlySerializedAs("wallLightTile")]
    public TileBase wallLightHTile;
    [Tooltip("Tile đèn cho tường DỌC (dự phòng chung)")]
    public TileBase wallLightVTile;
    [Tooltip("Tile đèn cho tường TRÊN (bạn cung cấp) — dự phòng: wallLightHTile")]
    public TileBase wallLightTopTile;
    [Tooltip("Tile đèn cho tường DƯỚI (bạn cung cấp) — dự phòng: wallLightHTile")]
    public TileBase wallLightBottomTile;
    [Tooltip("Tile đèn cho tường TRÁI (bạn cung cấp) — dự phòng: wallLightVTile")]
    public TileBase wallLightLeftTile;
    [Tooltip("Tile đèn cho tường PHẢI (bạn cung cấp) — dự phòng: wallLightVTile")]
    public TileBase wallLightRightTile;
    [Tooltip("Cứ mỗi N ô tường thì chèn 1 tile đèn. Để 0 hoặc không gán tile = tắt.")]
    public int lightTileInterval = 5;
    [Tooltip("Prefab có Point Light 2D — nếu gán, mỗi tile đèn sẽ phát sáng thật. Không gán thì chỉ vẽ tile.")]
    public GameObject wallLightPrefab;

    // Chứa các đèn đã spawn (để xóa khi vẽ lại map)
    Transform lightContainer;

    // Tile tường theo hướng của lần Paint hiện tại (để ResolveWallTile dùng)
    TileBase _topTile, _bottomTile, _leftTile, _rightTile;
    // Thứ tự các ô tường đã vẽ (vị trí + hướng) — dùng để đặt đèn cách đều
    readonly List<(Vector2Int cell, int dir)> _wallDrawOrder = new();
    // Các ô góc (ngoài) — đặt đèn sẽ né các ô này
    readonly HashSet<Vector2Int> _cornerCells = new();

    public void Paint(DungeonData data)
    {
        bool hasVTile = wallVTile != null || wallVLeftTile != null || wallVRightTile != null;
        if (floorMap == null || wallMap == null || floorTileA == null ||
            wallHTile == null || !hasVTile)
        {
            Debug.LogError("[Dungeon] Chưa gán đủ floorMap/wallMap/floorTileA/wallHTile + 1 tile tường dọc trong Inspector!");
            return;
        }

        floorMap.ClearAllTiles();
        wallMap.ClearAllTiles();
        lightTileCells.Clear();
        _wallDrawOrder.Clear();
        _cornerCells.Clear();

        // RNG riêng cho biến thể sàn — dùng seed của dungeon nên cùng seed = cùng map y hệt
        var floorRng = new System.Random(data.seed);

        // Xóa đèn của lần vẽ trước (nếu vẽ lại map)
        if (lightContainer != null) Destroy(lightContainer.gameObject);
        lightContainer = new GameObject("WallLights").transform;
        lightContainer.SetParent(transform, false);

        bool useLightTiles = lightTileInterval > 0 &&
            (wallLightHTile != null || wallLightVTile != null ||
             wallLightTopTile != null || wallLightBottomTile != null ||
             wallLightLeftTile != null || wallLightRightTile != null);

        // Pass 1: vẽ sàn (mỗi ô chọn 1 trong 3 biến thể theo tỉ lệ trọng số)
        for (int y = 0; y < data.height; y++)
            for (int x = 0; x < data.width; x++)
                if (data.IsFloor(x, y))
                    floorMap.SetTile(new Vector3Int(x, y, 0), PickFloorTile(floorRng));

        // Pass 2: vẽ tường theo kiểu KHUNG VIỀN 1 ô có hướng.
        // Với mỗi ô sàn, 4 ô kề (trên/dưới/trái/phải) nếu không phải sàn
        // thì vẽ tile tường tương ứng. Tường ngang (trên/dưới) vẽ trước,
        // tường dọc (trái/phải) không đè lên ô đã có tường.
        var doorCellSet = new HashSet<Vector2Int>();
        if (data.secretDoors != null)
            foreach (var door in data.secretDoors)
                if (door.doorCells != null)
                    foreach (var dc in door.doorCells)
                        doorCellSet.Add(dc);

        _topTile = wallTopTile != null ? wallTopTile : wallHTile;
        _bottomTile = wallBottomTile != null ? wallBottomTile : wallHTile;
        _leftTile = wallVLeftTile != null ? wallVLeftTile
            : (wallVTile != null ? wallVTile : wallHTile);
        _rightTile = wallVRightTile != null ? wallVRightTile
            : (wallVTile != null ? wallVTile : wallHTile);

        // Viền ngang (trên/dưới) trước
        for (int y = 0; y < data.height; y++)
        {
            for (int x = 0; x < data.width; x++)
            {
                if (!data.IsFloor(x, y)) continue;
                TryDrawWallCell(data, x, y + 1,
                    0, floorRng); // 0 = tường TRÊN
                TryDrawWallCell(data, x, y - 1,
                    1, floorRng); // 1 = tường DƯỚI
            }
        }
        // Viền dọc (trái/phải) sau — không đè lên tường ngang ở góc
        for (int y = 0; y < data.height; y++)
        {
            for (int x = 0; x < data.width; x++)
            {
                if (!data.IsFloor(x, y)) continue;
                TryDrawWallCell(data, x - 1, y, 2, floorRng); // 2 = tường TRÁI
                TryDrawWallCell(data, x + 1, y, 3, floorRng); // 3 = tường PHẢI
            }
        }

        // Pass 3: lấp góc NGOÀI (ô chéo của sàn).
        // Mỗi ô sàn xét 4 ô chéo: nếu ô chéo chưa có tường và 2 ô kề
        // trực tiếp đều không phải sàn → đây là góc lồi, vẽ tile góc.
        for (int y = 0; y < data.height; y++)
        {
            for (int x = 0; x < data.width; x++)
            {
                if (!data.IsFloor(x, y)) continue;
                TryDrawCorner(data, x + 1, y + 1, x, y, 0, floorRng); // góc trên-phải
                TryDrawCorner(data, x - 1, y + 1, x, y, 1, floorRng); // góc trên-trái
                TryDrawCorner(data, x + 1, y - 1, x, y, 2, floorRng); // góc dưới-phải
                TryDrawCorner(data, x - 1, y - 1, x, y, 3, floorRng); // góc dưới-trái
            }
        }

        // Pass 4: đặt đèn tường (sau khi tường + góc đã vẽ xong)
        if (useLightTiles)
            PlaceWallLights(data, doorCellSet);

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
    // Vẽ 1 ô tường của khung viền (nếu hợp lệ và chưa có tile).
    // - Bỏ qua ngoài biên / ô sàn / ô đã vẽ (tường ngang vẽ trước nên giữ ở góc).
    // - Sàn được lót DƯỚI tường (chung 1 ô trên floorMap); tường ở wallMap
    //   có Order in Layer cao hơn nên phủ lên trên.
    // - Cứ mỗi N ô tường thì chèn 1 tile đèn đúng hướng (trừ ô cửa bí mật).
    // Chọn tile đèn theo đúng hướng tường.
    // Ưu tiên biến thể riêng của hướng đó, rồi đến tile ngang/dọc chung.
    // wallDir: 0 = trên, 1 = dưới, 2 = trái, 3 = phải.
    TileBase PickLightTile(int wallDir)
    {
        TileBase specific = wallDir switch
        {
            0 => wallLightTopTile,
            1 => wallLightBottomTile,
            2 => wallLightLeftTile,
            _ => wallLightRightTile,
        };
        if (specific != null) return specific;
        bool isV = wallDir >= 2;
        TileBase t = isV ? wallLightVTile : wallLightHTile;
        if (t == null) t = isV ? wallLightHTile : wallLightVTile; // fallback chéo
        return t;
    }

    // Xác định tile tường theo hình học thực tế của ô:
    // - Góc TRONG (lõm, sàn ở 2 hướng kề nhau) → tile góc trong (nếu có)
    // - Còn lại → tile tường thẳng theo hướng.
    // wallDir: 0 = trên, 1 = dưới, 2 = trái, 3 = phải.
    TileBase ResolveWallTile(DungeonData data, int x, int y, int wallDir)
    {
        TileBase dirTile = wallDir switch
        {
            0 => _topTile,
            1 => _bottomTile,
            2 => _leftTile,
            _ => _rightTile,
        };

        bool fN = data.IsFloor(x, y + 1);
        bool fS = data.IsFloor(x, y - 1);
        bool fE = data.IsFloor(x + 1, y);
        bool fW = data.IsFloor(x - 1, y);

        // Góc trong (lõm): sàn bao 2 hướng kề nhau
        if (fN && fW && !fS && !fE && wallCornerInnerTopLeft != null)
            return wallCornerInnerTopLeft;
        if (fN && fE && !fS && !fW && wallCornerInnerTopRight != null)
            return wallCornerInnerTopRight;
        if (fS && fW && !fN && !fE && wallCornerInnerBottomLeft != null)
            return wallCornerInnerBottomLeft;
        if (fS && fE && !fN && !fW && wallCornerInnerBottomRight != null)
            return wallCornerInnerBottomRight;

        return dirTile;
    }

    void TryDrawWallCell(DungeonData data, int x, int y, int wallDir,
        System.Random floorRng)
    {
        if (x < 0 || y < 0 || x >= data.width || y >= data.height) return;
        if (data.IsFloor(x, y)) return;
        var cell = new Vector3Int(x, y, 0);
        if (wallMap.GetTile(cell) != null) return; // đã vẽ (ô gọi trước thắng)
        TileBase tile = ResolveWallTile(data, x, y, wallDir);
        if (tile == null) return;

        // Lót sàn dưới tường
        floorMap.SetTile(cell, PickFloorTile(floorRng));
        wallMap.SetTile(cell, tile);
        _wallDrawOrder.Add((new Vector2Int(x, y), wallDir));
    }

    // Vẽ tile góc NGOÀI (lồi) tại ô chéo (cx,cy) của ô sàn (fx,fy).
    // Chỉ vẽ khi: ô chéo chưa có tile VÀ 2 ô kề trực tiếp đều không phải sàn.
    // cornerDir: 0 = trên-phải, 1 = trên-trái, 2 = dưới-phải, 3 = dưới-trái.
    void TryDrawCorner(DungeonData data, int cx, int cy, int fx, int fy,
        int cornerDir, System.Random floorRng)
    {
        if (cx < 0 || cy < 0 || cx >= data.width || cy >= data.height) return;
        if (data.IsFloor(cx, cy)) return;
        var cell = new Vector3Int(cx, cy, 0);
        if (wallMap.GetTile(cell) != null) return;
        // 2 ô kề trực tiếp giữa sàn và góc
        if (data.IsFloor(cx, fy) || data.IsFloor(fx, cy)) return;

        TileBase tile = cornerDir switch
        {
            0 => wallCornerTopRight,
            1 => wallCornerTopLeft,
            2 => wallCornerBottomRight,
            _ => wallCornerBottomLeft,
        };
        if (tile == null) tile = wallHTile;
        if (tile == null) return;

        floorMap.SetTile(cell, PickFloorTile(floorRng)); // lót sàn dưới góc
        wallMap.SetTile(cell, tile);
        _cornerCells.Add(new Vector2Int(cx, cy));
    }

    // Pass 4: đặt đèn tường — mỗi lightTileInterval ô tường thì 1 đèn,
    // đếm theo đúng thứ tự vẽ. Ô cửa bí mật thì bỏ qua.
    // Nếu vị trí đèn rơi vào ô GÓC → lùi lại 1 ô (y-1) nếu ô đó là tường
    // hợp lệ (không phải góc/cửa), tránh đuốc nằm chình ình ở góc.
    void PlaceWallLights(DungeonData data, HashSet<Vector2Int> doorCellSet)
    {
        int counter = 0;
        int placed = 0;
        foreach (var (cellPos, wallDir) in _wallDrawOrder)
        {
            counter++;
            if (counter % lightTileInterval != 0) continue;
            if (doorCellSet.Contains(cellPos)) continue;

            Vector2Int lightPos = cellPos;
            if (_cornerCells.Contains(cellPos))
            {
                var back = new Vector2Int(cellPos.x, cellPos.y - 1);
                if (back.y >= 0 &&
                    wallMap.GetTile(new Vector3Int(back.x, back.y, 0)) != null &&
                    !_cornerCells.Contains(back) &&
                    !doorCellSet.Contains(back))
                {
                    lightPos = back; // lùi đuốc 1 ô tránh góc
                }
            }

            TileBase lightTile = PickLightTile(wallDir);
            if (lightTile == null) continue;

            var cell = new Vector3Int(lightPos.x, lightPos.y, 0);
            wallMap.SetTile(cell, lightTile);
            if (!lightTileCells.Contains(lightPos))
                lightTileCells.Add(lightPos);
            SpawnWallLight(cell);
            placed++;
        }
        Debug.Log($"[Dungeon] Đặt {placed} đèn tường (mỗi {lightTileInterval} ô).");
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

    // Đổi tọa độ tile -> world (tâm ô). Dùng transform thật của tilemap
    // nên đúng ngay cả khi Grid bị dời khỏi gốc tọa độ.
    public Vector3 CellToWorld(Vector2Int cell)
    {
        if (floorMap != null)
            return floorMap.CellToWorld(new Vector3Int(cell.x, cell.y, 0))
                 + floorMap.cellSize * 0.5f;
        // Fallback nếu chưa gán floorMap: giả định tilemap ở gốc, mỗi ô = 1 unit
        return new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
    }

    // Mở cửa phòng bí mật: đổi các ô tường của cửa thành sàn.
    // TilemapCollider2D + CompositeCollider2D tự cập nhật khi tile đổi.
    public void OpenDoor(List<Vector2Int> doorCells)
    {
        if (doorCells == null || wallMap == null) return;
        foreach (var c in doorCells)
        {
            // Sàn đã được lót sẵn dưới tường lúc Paint — chỉ cần xóa tile tường
            // là sàn hiện ra (không cần vẽ đè sàn lên wallMap như trước).
            wallMap.SetTile(new Vector3Int(c.x, c.y, 0), null);
        }
        Debug.Log($"[Dungeon] Đã mở cửa bí mật ({doorCells.Count} ô).");
    }
}
