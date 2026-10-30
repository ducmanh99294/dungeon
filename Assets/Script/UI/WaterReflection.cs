// Phản chiếu nhân vật trên mặt nước (kiểu gương lật ngược)
// Gắn vào GameObject Player. Khi player đứng trên hoặc sát nước (sông/ao),
// hiện bóng phản chiếu lật ngược bên dưới, mờ dần.

using UnityEngine;

public class WaterReflection : MonoBehaviour
{
    [Header("Phản chiếu")]
    [Tooltip("Độ mờ của bóng (0 = trong suốt, 1 = rõ như thật)")]
    [Range(0f, 1f)] public float reflectionAlpha = 0.35f;
    [Tooltip("Bóng lệch xuống dưới nhân vật bao nhiêu unit")]
    public float reflectionOffsetY = -0.9f;
    [Tooltip("Màu nhuộm bóng (hơi xanh nước)")]
    public Color reflectionTint = new Color(0.7f, 0.85f, 1f, 1f);
    [Tooltip("Order của bóng — thấp hơn nhân vật để không đè lên")]
    public int reflectionOrder = 1;

    SpriteRenderer playerSr;
    SpriteRenderer reflectSr;
    Transform reflectTf;

    void Start()
    {
        playerSr = GetComponentInChildren<SpriteRenderer>();
        if (playerSr == null)
        {
            Debug.LogWarning("[WaterReflection] Không tìm thấy SpriteRenderer trên Player");
            enabled = false;
            return;
        }

        // Tạo object bóng con của player
        var go = new GameObject("Reflection");
        reflectTf = go.transform;
        reflectTf.SetParent(transform, false);
        reflectTf.localPosition = new Vector3(0, reflectionOffsetY, 0);
        reflectTf.localScale = new Vector3(1, -1, 1); // lật ngược dọc

        reflectSr = go.AddComponent<SpriteRenderer>();
        reflectSr.sortingOrder = reflectionOrder;
        reflectSr.flipX = playerSr.flipX;
    }

    void LateUpdate()
    {
        if (playerSr == null || reflectSr == null) return;

        // Chỉ hiện bóng khi đứng trên/sát nước
        bool nearWater = false;
        if (FarmMapGenerator.Instance != null)
        {
            float ts = FarmMapGenerator.Instance.tileSize;
            if (ts <= 0) ts = 1f;
            int gx = Mathf.RoundToInt(transform.position.x / ts);
            int gy = Mathf.RoundToInt(transform.position.y / ts);
            nearWater = FarmMapGenerator.Instance.IsNearWater(gx, gy);
        }
        reflectSr.enabled = nearWater;
        if (!nearWater) return;

        // Bóng copy sprite + hướng lật của nhân vật theo từng frame
        reflectSr.sprite = playerSr.sprite;
        reflectSr.flipX = playerSr.flipX;
        Color c = reflectionTint;
        c.a = reflectionAlpha;
        reflectSr.color = c;
    }
}
