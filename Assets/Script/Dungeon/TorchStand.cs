// TorchStand.cs — chân đuốc tương tác được (nhấn E để thắp).
// Được LightTorchPuzzle tạo lúc chạy — không cần prefab.
using UnityEngine;

public class TorchStand : MonoBehaviour
{
    LightTorchPuzzle puzzle;
    SpriteRenderer sr;
    Sprite unlitSprite;
    Sprite litSprite;
    bool isLit = false;
    bool playerInside = false;

    // Tạo chân đuốc hoàn chỉnh tại vị trí world cho trước
    public static TorchStand Create(LightTorchPuzzle puzzle, Sprite unlit, Sprite lit, Vector3 pos)
    {
        var go = new GameObject("TorchStand");
        go.transform.position = pos;

        var t = go.AddComponent<TorchStand>();
        t.puzzle = puzzle;
        t.unlitSprite = unlit;
        t.litSprite = lit;

        t.sr = go.AddComponent<SpriteRenderer>();
        t.sr.sprite = unlit;
        t.sr.sortingOrder = 6; // trên tilemap

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 1.2f;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        return t;
    }

    public void SetLit(bool lit)
    {
        isLit = lit;
        if (sr != null)
            sr.sprite = lit ? (litSprite != null ? litSprite : unlitSprite) : unlitSprite;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInside = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInside = false;
    }

    void Update()
    {
        if (!isLit && playerInside && Input.GetKeyDown(KeyCode.E))
        {
            SetLit(true);
            Debug.Log("[Puzzle] Đã thắp 1 ngọn đuốc.");
            puzzle?.OnTorchLit(this);
        }
    }
}
