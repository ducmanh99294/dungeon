// PuzzleManager.cs — điều phối câu đố và cửa phòng bí mật trong dungeon.
// - Câu đố giải xong (ở client này) -> mở cửa + báo server cho cả phòng cùng thấy.
// - Nhận puzzle_solved từ server (người khác giải) -> mở cửa, không báo lại.
using System.Collections.Generic;
using UnityEngine;

public class PuzzleManager : MonoBehaviour
{
    public static PuzzleManager Instance;

    [Header("Refs")]
    [Tooltip("Tự tìm nếu để trống")]
    public DungeonTilemapPainter painter;

    readonly Dictionary<string, List<SecretRoomDoor>> doorsByPuzzle = new();
    readonly Dictionary<string, DungeonPuzzle> puzzles = new();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (painter == null)
            painter = FindFirstObjectByType<DungeonTilemapPainter>();
    }

    void Start()
    {
        // Nghe người khác giải đố (multiplayer)
        var nm = NetworkManager.Instance;
        if (nm != null)
            nm.OnPuzzleSolved += OnRemoteSolved;
    }

    void OnDestroy()
    {
        var nm = NetworkManager.Instance;
        if (nm != null)
            nm.OnPuzzleSolved -= OnRemoteSolved;
        if (Instance == this) Instance = null;
    }

    public void RegisterDoor(SecretRoomDoor door)
    {
        if (door == null || string.IsNullOrEmpty(door.requiredPuzzleId)) return;
        if (!doorsByPuzzle.TryGetValue(door.requiredPuzzleId, out var list))
            doorsByPuzzle[door.requiredPuzzleId] = list = new List<SecretRoomDoor>();
        if (!list.Contains(door)) list.Add(door);
    }

    public void RegisterPuzzle(DungeonPuzzle puzzle)
    {
        if (puzzle == null || string.IsNullOrEmpty(puzzle.puzzleId)) return;
        puzzles[puzzle.puzzleId] = puzzle;
    }

    // Câu đố được giải ở client này
    public void NotifySolved(DungeonPuzzle puzzle)
    {
        OpenDoorsFor(puzzle.puzzleId);

        var nm = NetworkManager.Instance;
        if (nm != null && nm.IsConnected)
            nm.SendPuzzleSolved(puzzle.puzzleId);
    }

    // Người khác giải (nhận từ server) — mở cửa nhưng không báo lại
    void OnRemoteSolved(string puzzleId)
    {
        if (string.IsNullOrEmpty(puzzleId)) return;
        if (puzzles.TryGetValue(puzzleId, out var p))
            p.ForceSolved();
        OpenDoorsFor(puzzleId);
        Debug.Log($"[Puzzle] Người khác đã giải: {puzzleId} — mở cửa.");
    }

    void OpenDoorsFor(string puzzleId)
    {
        if (!doorsByPuzzle.TryGetValue(puzzleId, out var list)) return;
        foreach (var d in list)
            d.Open(painter);
    }
}
