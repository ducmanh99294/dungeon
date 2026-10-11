// DungeonData.cs — dữ liệu layout của 1 dungeon đã gen.
// tile: 0 = tường, 1 = sàn. Mảng 1 chiều row-major: tiles[y * width + x].
using System;
using System.Collections.Generic;
using UnityEngine;

// Loại câu đố dùng để mở phòng bí mật (thêm loại mới không cần sửa code cũ)
public enum PuzzleType
{
    KillAll,      // diệt hết quái trong dungeon
    LightTorches, // thắp sáng N ngọn đuốc
}

[Serializable]
public class SecretDoorData
{
    public string puzzleId;              // id nối cửa <-> câu đố
    public PuzzleType puzzleType;        // loại câu đố canh cửa này
    public List<Vector2Int> doorCells = new(); // các ô tile của cửa (tọa độ tile)
    public Vector2Int roomCenter;        // tâm phòng bí mật (tọa độ tile)
    public RectInt roomRect;             // vùng phòng bí mật
}

[Serializable]
public class DungeonData
{
    public int width;
    public int height;
    public int seed;
    public int[] tiles;

    public Vector2Int playerSpawn;   // ô spawn người chơi (tọa độ tile)
    public Vector2Int exitPos;       // ô đặt cổng ra (tọa độ tile)
    public Vector2[] monsterSpawns;  // tọa độ world của các điểm spawn quái

    public List<RectInt> secretRooms = new();       // các phòng bí mật
    public List<SecretDoorData> secretDoors = new(); // cửa + câu đố của chúng

    public const int WALL = 0;
    public const int FLOOR = 1;

    public bool InBounds(int x, int y)
    {
        return x >= 0 && y >= 0 && x < width && y < height;
    }

    public bool IsFloor(int x, int y)
    {
        return InBounds(x, y) && tiles[y * width + x] == FLOOR;
    }

    // true nếu ô (x,y) nằm trong 1 phòng bí mật
    public bool IsInSecretRoom(int x, int y)
    {
        foreach (var r in secretRooms)
        {
            if (x >= r.xMin && x < r.xMax && y >= r.yMin && y < r.yMax)
                return true;
        }
        return false;
    }

    public int FloorCount()
    {
        int n = 0;
        foreach (var t in tiles) if (t == FLOOR) n++;
        return n;
    }
}
