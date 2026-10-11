// SecretRoomDoor.cs — cửa phòng bí mật.
// Cửa là 1 đoạn tường (1 ô tile); khi câu đố tương ứng được giải,
// các ô tường đó đổi thành sàn -> đi qua được.
// (Không cần sprite riêng: lúc khóa trông như tường thường — đúng chất "bí mật".)
using System.Collections.Generic;
using UnityEngine;

public class SecretRoomDoor : MonoBehaviour
{
    [Header("Cửa phòng bí mật")]
    [Tooltip("ID câu đố mở cửa này (khớp DungeonPuzzle.puzzleId)")]
    public string requiredPuzzleId;

    [Tooltip("Các ô tile của cửa")]
    public List<Vector2Int> doorCells = new();

    [Tooltip("Prefab hiệu ứng khi mở (tùy chọn: particle, âm thanh...)")]
    public GameObject openEffectPrefab;

    public bool isOpen { get; private set; }

    // Sự kiện cho DungeonManager spawn thưởng (quái tinh anh...) khi cửa mở
    public event System.Action<SecretRoomDoor> onOpened;

    void Start()
    {
        PuzzleManager.Instance?.RegisterDoor(this);
    }

    public void Open(DungeonTilemapPainter painter)
    {
        if (isOpen) return;
        isOpen = true;

        if (painter != null)
            painter.OpenDoor(doorCells);
        else
            Debug.LogWarning("[Door] Mở cửa nhưng thiếu painter!");

        // Hiệu ứng mở cửa (nếu có)
        if (openEffectPrefab != null && doorCells.Count > 0)
        {
            var c = doorCells[doorCells.Count / 2];
            Vector3 pos = painter != null
                ? painter.CellToWorld(c)
                : new Vector3(c.x + 0.5f, c.y + 0.5f, 0f);
            Instantiate(openEffectPrefab, pos, Quaternion.identity);
        }

        Debug.Log($"[Door] PHÒNG BÍ MẬT ĐÃ MỞ! (câu đố: {requiredPuzzleId})");
        onOpened?.Invoke(this);
    }
}
