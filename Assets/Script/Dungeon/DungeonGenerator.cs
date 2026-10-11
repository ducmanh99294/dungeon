// DungeonGenerator.cs — sinh layout dungeon bằng thuật toán (không phải AI).
// Cách làm: đặt phòng ngẫu nhiên không đè nhau → nối các phòng liên tiếp
// bằng hành lang chữ L (đảm bảo 100% thông nhau) → thêm vài hành lang chéo
// cho bớt tuyến tính. Seed cố định => layout giống hệt nhau (debug dễ).
using System.Collections.Generic;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    [Header("Kích thước map (ô tile)")]
    public int width = 64;
    public int height = 64;

    [Header("Phòng")]
    public int roomAttempts = 40;   // số lần thử đặt phòng
    [Tooltip("Số phòng tối đa được tạo. Để 0 = không giới hạn.")]
    public int maxRooms = 8;
    public int minRoomSize = 5;
    public int maxRoomSize = 11;

    [Header("Phòng bí mật")]
    [Tooltip("Số phòng bí mật (không nối hành lang — mở bằng câu đố)")]
    public int secretRoomCount = 1;
    public int minSecretRoomSize = 5;
    public int maxSecretRoomSize = 8;
    [Tooltip("Loại câu đố cho từng cửa (xoay vòng nếu nhiều cửa hơn)")]
    public PuzzleType[] secretPuzzlePool = { PuzzleType.KillAll, PuzzleType.LightTorches };

    [Header("Hành lang")]
    [Tooltip("Độ rộng hành lang (ô tile). Khuyên dùng số lẻ: 1, 3, 5.")]
    public int corridorWidth = 3;

    [Header("Spawn quái")]
    public int maxMonsterSpawns = 14;
    public float minSpawnDistFromPlayer = 8f; // ô tile

    [Header("Điểm spawn người chơi")]
    [Tooltip("Bật: spawn ở PHÒNG gần giữa map nhất. Tắt: spawn ở phòng đầu tiên.")]
    public bool spawnAtMapCenter = true;

    public DungeonData Generate(int seed)
    {
        var rng = new System.Random(seed);
        int[] tiles = new int[width * height]; // mặc định 0 = tường

        // 1. Đặt phòng ngẫu nhiên, không đè nhau (chừa 1 ô đệm)
        var rooms = new List<RectInt>();
        for (int i = 0; i < roomAttempts; i++)
        {
            // Đủ số phòng tối đa thì dừng
            if (maxRooms > 0 && rooms.Count >= maxRooms) break;

            int w = rng.Next(minRoomSize, maxRoomSize + 1);
            int h = rng.Next(minRoomSize, maxRoomSize + 1);
            int x = rng.Next(1, width - w - 1);
            int y = rng.Next(1, height - h - 1);
            var r = new RectInt(x, y, w, h);

            bool overlap = false;
            foreach (var o in rooms)
            {
                var padded = new RectInt(o.x - 1, o.y - 1, o.width + 2, o.height + 2);
                if (r.Overlaps(padded)) { overlap = true; break; }
            }
            if (overlap) continue;

            rooms.Add(r);
            for (int ix = r.xMin; ix < r.xMax; ix++)
                for (int iy = r.yMin; iy < r.yMax; iy++)
                    tiles[iy * width + ix] = DungeonData.FLOOR;
        }

        // 2. Nối các phòng liên tiếp bằng hành lang chữ L => đảm bảo thông nhau
        for (int i = 1; i < rooms.Count; i++)
            CarveCorridor(tiles, Center(rooms[i - 1]), Center(rooms[i]), rng);

        // 3. Thêm vài đường nối chéo cho map bớt tuyến tính
        int extra = rooms.Count / 3;
        for (int i = 0; i < extra && rooms.Count > 2; i++)
        {
            int a = rng.Next(rooms.Count);
            int b = rng.Next(rooms.Count);
            if (a != b) CarveCorridor(tiles, Center(rooms[a]), Center(rooms[b]), rng);
        }

        var data = new DungeonData
        {
            width = width,
            height = height,
            seed = seed,
            tiles = tiles,
            // Phòng gần giữa map nhất -> spawn ở tâm phòng đó
            playerSpawn = spawnAtMapCenter
                ? FindCenterRoomSpawn(rooms, tiles)
                : (rooms.Count > 0 ? Center(rooms[0]) : new Vector2Int(width / 2, height / 2)),
            exitPos = rooms.Count > 1 ? Center(rooms[rooms.Count - 1]) : new Vector2Int(width / 2, height / 2),
        };

        // 3b. Đặt phòng bí mật (SAU khi đào hành lang — để kiểm tra ô cửa còn là tường)
        PlaceSecretRooms(rng, rooms, tiles, data);

        // 4. Điểm spawn quái: ô sàn ngẫu nhiên, cách xa điểm spawn người chơi,
        //    KHÔNG spawn trong phòng bí mật (tránh kẹt logic diệt-hết-quái)
        var spawns = new List<Vector2>();
        int guard = 0;
        while (spawns.Count < maxMonsterSpawns && guard < 2000)
        {
            guard++;
            int x = rng.Next(1, width - 1);
            int y = rng.Next(1, height - 1);
            if (tiles[y * width + x] != DungeonData.FLOOR) continue;
            if (data.IsInSecretRoom(x, y)) continue;

            var cell = new Vector2Int(x, y);
            if (Vector2Int.Distance(cell, data.playerSpawn) < minSpawnDistFromPlayer) continue;

            bool tooClose = false;
            foreach (var s in spawns)
            {
                if (Vector2.Distance(s, new Vector2(x + 0.5f, y + 0.5f)) < 3f)
                { tooClose = true; break; }
            }
            if (tooClose) continue;

            spawns.Add(new Vector2(x + 0.5f, y + 0.5f)); // tâm ô tile
        }
        data.monsterSpawns = spawns.ToArray();

        Debug.Log($"[Dungeon] Seed={seed} | Phòng={rooms.Count} | Phòng bí mật={data.secretRooms.Count} | Sàn={data.FloorCount()} ô | Spawn quái={spawns.Count}");
        return data;
    }

    Vector2Int Center(RectInt r)
    {
        return new Vector2Int(r.x + r.width / 2, r.y + r.height / 2);
    }

    // Đặt phòng bí mật: KHÔNG nối hành lang, nhưng luôn sát 1 phòng thường
    // (chung 1 bức tường dày 1 ô — ô tường chung đó chính là cửa).
    // Gọi SAU khi đào hành lang để chắc chắn ô cửa còn là tường.
    void PlaceSecretRooms(System.Random rng, List<RectInt> rooms, int[] tiles, DungeonData data)
    {
        if (secretRoomCount <= 0 || rooms.Count == 0) return;

        for (int s = 0; s < secretRoomCount; s++)
        {
            bool placed = false;
            for (int attempt = 0; attempt < 40 && !placed; attempt++)
            {
                var anchor = rooms[rng.Next(rooms.Count)];
                int sw = rng.Next(minSecretRoomSize, maxSecretRoomSize + 1);
                int sh = rng.Next(minSecretRoomSize, maxSecretRoomSize + 1);
                int dir = rng.Next(4); // 0=trên, 1=dưới, 2=trái, 3=phải

                RectInt sr;
                Vector2Int doorCell;
                switch (dir)
                {
                    case 0: // phòng bí mật ở TRÊN anchor (y lớn hơn)
                        {
                            int sx = anchor.x + rng.Next(-(sw - 2), anchor.width - 1);
                            int sy = anchor.y + anchor.height + 1; // cách 1 ô tường
                            sr = new RectInt(sx, sy, sw, sh);
                            if (!TryFindDoorCell(rng, tiles, anchor, sr, true, out doorCell)) continue;
                            break;
                        }
                    case 1: // DƯỚI anchor
                        {
                            int sx = anchor.x + rng.Next(-(sw - 2), anchor.width - 1);
                            int sy = anchor.y - sh - 1;
                            sr = new RectInt(sx, sy, sw, sh);
                            if (!TryFindDoorCell(rng, tiles, anchor, sr, true, out doorCell)) continue;
                            break;
                        }
                    case 2: // TRÁI anchor
                        {
                            int sx = anchor.x - sw - 1;
                            int sy = anchor.y + rng.Next(-(sh - 2), anchor.height - 1);
                            sr = new RectInt(sx, sy, sw, sh);
                            if (!TryFindDoorCell(rng, tiles, anchor, sr, false, out doorCell)) continue;
                            break;
                        }
                    default: // PHẢI anchor
                        {
                            int sx = anchor.x + anchor.width + 1;
                            int sy = anchor.y + rng.Next(-(sh - 2), anchor.height - 1);
                            sr = new RectInt(sx, sy, sw, sh);
                            if (!TryFindDoorCell(rng, tiles, anchor, sr, false, out doorCell)) continue;
                            break;
                        }
                }

                // Trong biên map?
                if (sr.xMin < 1 || sr.yMin < 1 || sr.xMax > width - 1 || sr.yMax > height - 1)
                    continue;

                // Không đè phòng khác (kể cả phòng bí mật đã đặt)
                bool overlap = false;
                foreach (var o in rooms)
                {
                    var padded = new RectInt(o.x - 1, o.y - 1, o.width + 2, o.height + 2);
                    if (sr.Overlaps(padded)) { overlap = true; break; }
                }
                if (!overlap)
                {
                    foreach (var o in data.secretRooms)
                    {
                        var padded = new RectInt(o.x - 1, o.y - 1, o.width + 2, o.height + 2);
                        if (sr.Overlaps(padded)) { overlap = true; break; }
                    }
                }
                if (overlap) continue;

                // Ô cửa phải còn là tường (hành lang có thể đã đào xuyên qua)
                if (tiles[doorCell.y * width + doorCell.x] != DungeonData.WALL)
                    continue;

                // Toàn bộ vùng phòng bí mật phải còn là tường
                // (không có hành lang nào đào xuyên qua — nếu không phòng bị lộ)
                bool carved = false;
                for (int ix = sr.xMin; ix < sr.xMax && !carved; ix++)
                    for (int iy = sr.yMin; iy < sr.yMax && !carved; iy++)
                        if (tiles[iy * width + ix] != DungeonData.WALL)
                            carved = true;
                if (carved) continue;

                // Khắc phòng bí mật
                for (int ix = sr.xMin; ix < sr.xMax; ix++)
                    for (int iy = sr.yMin; iy < sr.yMax; iy++)
                        tiles[iy * width + ix] = DungeonData.FLOOR;

                data.secretRooms.Add(sr);
                data.secretDoors.Add(new SecretDoorData
                {
                    puzzleId = $"secret_{s}",
                    puzzleType = secretPuzzlePool.Length > 0
                        ? secretPuzzlePool[s % secretPuzzlePool.Length]
                        : PuzzleType.KillAll,
                    doorCells = new List<Vector2Int> { doorCell },
                    roomCenter = Center(sr),
                    roomRect = sr,
                });
                placed = true;
                Debug.Log($"[Dungeon] Phòng bí mật {s} tại ({sr.x},{sr.y}) {sr.width}x{sr.height}, cửa tại ({doorCell.x},{doorCell.y})");
            }

            if (!placed)
                Debug.LogWarning($"[Dungeon] Không đặt được phòng bí mật {s} sau 40 lần thử.");
        }
    }

    // Tìm ô cửa trong đoạn tường chung giữa anchor và phòng bí mật.
    // horizontal=true: tường chung nằm ngang (phòng trên/dưới nhau) -> cửa có dạng (x, yWall).
    // Trả về false nếu không có đoạn chung nào còn là tường.
    bool TryFindDoorCell(System.Random rng, int[] tiles, RectInt anchor, RectInt sr, bool horizontal, out Vector2Int doorCell)
    {
        doorCell = default;
        if (horizontal)
        {
            int x0 = Mathf.Max(anchor.xMin, sr.xMin);
            int x1 = Mathf.Min(anchor.xMax, sr.xMax) - 1;
            if (x1 < x0) return false; // không có đoạn chung
            // Ô tường chung nằm ở hàng giữa 2 phòng
            int yWall = anchor.yMax < sr.yMin ? anchor.yMax : sr.yMax;
            // Thử giữa trước, rồi quét cả đoạn để tìm ô còn là tường
            int mid = (x0 + x1) / 2;
            for (int off = 0; off <= (x1 - x0); off++)
            {
                foreach (int x in new int[] { mid + off, mid - off })
                {
                    if (x < x0 || x > x1) continue;
                    if (tiles[yWall * width + x] == DungeonData.WALL)
                    {
                        doorCell = new Vector2Int(x, yWall);
                        return true;
                    }
                }
            }
            return false;
        }
        else
        {
            int y0 = Mathf.Max(anchor.yMin, sr.yMin);
            int y1 = Mathf.Min(anchor.yMax, sr.yMax) - 1;
            if (y1 < y0) return false;
            int xWall = anchor.xMax < sr.xMin ? anchor.xMax : sr.xMax;
            int mid = (y0 + y1) / 2;
            for (int off = 0; off <= (y1 - y0); off++)
            {
                foreach (int y in new int[] { mid + off, mid - off })
                {
                    if (y < y0 || y > y1) continue;
                    if (tiles[y * width + xWall] == DungeonData.WALL)
                    {
                        doorCell = new Vector2Int(xWall, y);
                        return true;
                    }
                }
            }
            return false;
        }
    }

    // Tìm phòng gần giữa map nhất, spawn ở tâm phòng đó.
    // Không có phòng nào thì tìm ô sàn gần tâm nhất (fallback).
    Vector2Int FindCenterRoomSpawn(List<RectInt> rooms, int[] tiles)
    {
        if (rooms.Count > 0)
        {
            Vector2 mapCenter = new Vector2(width / 2f, height / 2f);
            RectInt best = rooms[0];
            float bestDist = float.MaxValue;
            foreach (var r in rooms)
            {
                Vector2 c = new Vector2(r.x + r.width / 2f, r.y + r.height / 2f);
                float d = Vector2.Distance(c, mapCenter);
                if (d < bestDist) { bestDist = d; best = r; }
            }
            return Center(best);
        }
        return FindNearestFloor(tiles, width / 2, height / 2);
    }

    // Tìm ô sàn gần (cx, cy) nhất bằng cách loang vòng tròn.
    // Đảm bảo người chơi luôn spawn trên sàn, không kẹt trong tường.
    Vector2Int FindNearestFloor(int[] tiles, int cx, int cy)
    {
        if (tiles[cy * width + cx] == DungeonData.FLOOR)
            return new Vector2Int(cx, cy);

        int maxR = Mathf.Max(width, height);
        for (int r = 1; r < maxR; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    // Chỉ xét viền ngoài của hình vuông (vòng ring)
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r) continue;

                    int x = cx + dx;
                    int y = cy + dy;
                    if (x < 0 || y < 0 || x >= width || y >= height) continue;
                    if (tiles[y * width + x] == DungeonData.FLOOR)
                        return new Vector2Int(x, y);
                }
            }
        }
        return new Vector2Int(cx, cy); // fallback, hầu như không xảy ra
    }

    void CarveCorridor(int[] tiles, Vector2Int a, Vector2Int b, System.Random rng)
    {
        // Hành lang chữ L: đi ngang trước hoặc đi dọc trước (ngẫu nhiên)
        if (rng.Next(2) == 0)
        {
            CarveH(tiles, a.x, b.x, a.y);
            CarveV(tiles, a.y, b.y, b.x);
        }
        else
        {
            CarveV(tiles, a.y, b.y, a.x);
            CarveH(tiles, a.x, b.x, b.y);
        }
    }

    void CarveH(int[] tiles, int x0, int x1, int y)
    {
        for (int x = Mathf.Min(x0, x1); x <= Mathf.Max(x0, x1); x++)
            for (int d = -(corridorWidth - 1) / 2; d <= corridorWidth / 2; d++)
                SetFloor(tiles, x, y + d);
    }

    void CarveV(int[] tiles, int y0, int y1, int x)
    {
        for (int y = Mathf.Min(y0, y1); y <= Mathf.Max(y0, y1); y++)
            for (int d = -(corridorWidth - 1) / 2; d <= corridorWidth / 2; d++)
                SetFloor(tiles, x + d, y);
    }

    // Đặt sàn có kiểm tra biên (hành lang rộng gần mép map không bị tràn mảng)
    void SetFloor(int[] tiles, int x, int y)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return;
        tiles[y * width + x] = DungeonData.FLOOR;
    }
}
