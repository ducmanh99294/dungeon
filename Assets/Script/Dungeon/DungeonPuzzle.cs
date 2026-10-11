// DungeonPuzzle.cs — class cha cho mọi câu đố trong dungeon.
// Muốn thêm loại câu đố mới: tạo class kế thừa DungeonPuzzle,
// override logic, gọi Solve() khi người chơi giải xong.
using UnityEngine;

public abstract class DungeonPuzzle : MonoBehaviour
{
    [Header("Puzzle")]
    [Tooltip("ID nối câu đố với cửa (khớp với SecretDoorData.puzzleId)")]
    public string puzzleId;

    public bool isSolved { get; protected set; }

    // Gọi khi người chơi giải xong ở client này
    public virtual void Solve()
    {
        if (isSolved) return;
        isSolved = true;
        Debug.Log($"[Puzzle] Đã giải xong: {puzzleId} ({GetType().Name})");
        PuzzleManager.Instance?.NotifySolved(this);
    }

    // Đánh dấu đã giải mà KHÔNG báo lại (dùng khi nhận từ server — tránh loop)
    public void ForceSolved()
    {
        isSolved = true;
    }

    public virtual void ResetPuzzle()
    {
        isSolved = false;
    }
}
