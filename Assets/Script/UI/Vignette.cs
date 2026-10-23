// Vignette.cs — viền màn hình tối dần, ôm khít theo camera.
// Kéo script vào Main Camera. Script tự tạo object con "VignetteOverlay"
// (SpriteRenderer) — bạn click vào nó trong lúc Play để thấy/thử.
// Mọi thông số chỉnh live trong Inspector được (Intensity áp dụng ngay,
// Inner Radius thì chuột phải vào component → Rebuild Vignette).
// Tự tạo texture ellipse khớp đúng tỉ lệ màn hình bằng code (không cần art).
using UnityEngine;

public class Vignette : MonoBehaviour
{
    [Header("Vignette (Intensity chỉnh live được lúc Play)")]
    [Tooltip("Độ tối ở rìa (0-1) — tăng lên nếu thấy nhạt quá")]
    [Range(0f, 1f)]
    public float intensity = 0.6f;
    [Tooltip("Rìa bắt đầu tối từ bao xa (0 = tâm, 1 = góc). Đổi xong chuột phải → Rebuild Vignette")]
    [Range(0.2f, 0.9f)]
    public float innerRadius = 0.55f;
    [Tooltip("Màu viền (đen hoặc xanh tím cho ma mị)")]
    public Color vignetteColor = Color.black;
    [Tooltip("Nhịp 'thở' nhẹ của dungeon (0 = tắt)")]
    [Range(0f, 0.3f)]
    public float breatheAmount = 0.05f;
    [Tooltip("Tốc độ thở")]
    public float breatheSpeed = 0.5f;

    GameObject overlayGO;
    SpriteRenderer sr;
    Camera cam;
    float baseAlpha = 1f;

    void Start()
    {
        Build();
    }

    [ContextMenu("Rebuild Vignette")]
    public void Rebuild()
    {
        if (overlayGO != null) Destroy(overlayGO);
        Build();
    }

    void Build()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("[Vignette] Không tìm thấy Camera nào!");
            return;
        }

        float aspect = cam.aspect;
        Texture2D tex = MakeVignetteTexture(256, aspect);
        // PPU 100 → sprite có kích thước đúng tỉ lệ màn hình
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f), 100f);

        overlayGO = new GameObject("VignetteOverlay");
        overlayGO.transform.SetParent(cam.transform, false);
        overlayGO.transform.localPosition = new Vector3(0f, 0f, 1f);
        overlayGO.transform.localRotation = Quaternion.identity;

        sr = overlayGO.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 999;

        FitToCamera(tex);
        ApplyColor();
        Debug.Log($"[Vignette] đã tạo {overlayGO.name} trên camera {cam.gameObject.name} (aspect {aspect:F2})");
    }

    // Chỉnh Intensity lúc Play → tối/sáng ngay không cần rebuild
    void OnValidate()
    {
        if (sr != null && Application.isPlaying)
            ApplyColor();
    }

    void Update()
    {
        if (breatheAmount > 0f && sr != null)
        {
            float b = 1f + Mathf.Sin(Time.time * breatheSpeed) * breatheAmount;
            var c = sr.color;
            c.a = baseAlpha * b;
            sr.color = c;
        }
    }

    void FitToCamera(Texture2D tex)
    {
        if (cam == null || sr == null) return;
        float h = cam.orthographicSize * 2f;
        float w = h * cam.aspect;
        // Sprite đã đúng tỉ lệ màn hình → scale cho vừa khít view
        float sx = w / (tex.width / 100f);
        float sy = h / (tex.height / 100f);
        sr.transform.localScale = new Vector3(sx, sy, 1f);
    }

    void ApplyColor()
    {
        if (sr == null) return;
        Color c = vignetteColor;
        c.a = intensity;
        baseAlpha = intensity;
        sr.color = c;
    }

    public void SetIntensity(float value)
    {
        intensity = Mathf.Clamp01(value);
        ApplyColor();
    }

    // Texture ellipse: trong suốt ở tâm, tối dần ra đúng 4 góc màn hình
    Texture2D MakeVignetteTexture(int size, float aspect)
    {
        int w = size;
        int h = Mathf.Max(8, Mathf.RoundToInt(size / aspect));
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[w * h];
        float cx = w / 2f, cy = h / 2f;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = (x - cx) / cx; // [-1, 1]
                float dy = (y - cy) / cy; // [-1, 1]
                float d = Mathf.Sqrt(dx * dx + dy * dy) / 1.4142f; // 0 tâm → 1 góc
                float t = Mathf.Clamp01((d - innerRadius) / (1f - innerRadius));
                t = t * t * (3f - 2f * t); // smoothstep
                pixels[y * w + x] = new Color(1f, 1f, 1f, t);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
}
