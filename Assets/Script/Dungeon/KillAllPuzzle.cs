// KillAllPuzzle.cs — câu đố "diệt hết quái trong dungeon".
// Khi số quái (tag Enemy) về 0 sau khi đã từng thấy quái -> Solve().
// Phòng bí mật KHÔNG spawn quái từ đầu nên không bao giờ kẹt.
using UnityEngine;

public class KillAllPuzzle : DungeonPuzzle
{
    [Header("Diệt hết quái")]
    [Tooltip("Kiểm tra mỗi X giây")]
    public float checkInterval = 1f;

    bool hasSeenMonsters = false; // tránh solve nhầm lúc quái chưa spawn
    float timer = 0f;

    void Update()
    {
        if (isSolved) return;

        timer += Time.deltaTime;
        if (timer < checkInterval) return;
        timer = 0f;

        int count = GameObject.FindGameObjectsWithTag("Enemy").Length;
        if (count > 0)
        {
            hasSeenMonsters = true;
        }
        else if (hasSeenMonsters)
        {
            Debug.Log("[Puzzle] Đã diệt hết quái!");
            Solve();
        }
    }
}
