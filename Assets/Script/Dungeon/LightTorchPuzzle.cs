// LightTorchPuzzle.cs — câu đố "thắp sáng N ngọn đuốc".
// DungeonManager spawn các chân đuốc (TorchStand) tại vị trí do mình chọn;
// người chơi nhấn E từng cái. Đủ số lượng -> Solve().
using System.Collections.Generic;
using UnityEngine;

public class LightTorchPuzzle : DungeonPuzzle
{
    [Header("Thắp đuốc")]
    [Tooltip("Số đuốc cần thắp")]
    public int torchesToLight = 4;

    [Tooltip("Kéo sprite đuốc TẮT vào")]
    public Sprite torchUnlitSprite;

    [Tooltip("Kéo sprite đuốc CHÁY vào")]
    public Sprite torchLitSprite;

    int litCount = 0;
    readonly List<TorchStand> torches = new();

    // DungeonManager gọi sau khi gen map xong
    public void SpawnTorches(List<Vector3> worldPositions)
    {
        foreach (var pos in worldPositions)
        {
            var torch = TorchStand.Create(this, torchUnlitSprite, torchLitSprite, pos);
            torches.Add(torch);
        }
        // Không đòi nhiều hơn số đuốc thực tế có
        torchesToLight = Mathf.Min(torchesToLight, torches.Count);
        Debug.Log($"[Puzzle] Đã đặt {torches.Count} chân đuốc, cần thắp {torchesToLight}.");
    }

    // TorchStand gọi khi được thắp
    public void OnTorchLit(TorchStand torch)
    {
        if (isSolved) return;
        litCount++;
        Debug.Log($"[Puzzle] Đuốc: {litCount}/{torchesToLight}");
        if (litCount >= torchesToLight)
            Solve();
    }

    public override void ResetPuzzle()
    {
        base.ResetPuzzle();
        litCount = 0;
        foreach (var t in torches)
            if (t != null) t.SetLit(false);
    }
}
