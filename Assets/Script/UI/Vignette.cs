// Vignette.cs — viền màn hình tối dần, tăng chiều sâu cho dungeon.
// Kéo script vào một GameObject bất kỳ trong scene Dungeon (vd Main Camera).
// Tự tạo Canvas overlay + texture gradient bằng code (không cần art, không cần post-processing).
using UnityEngine;
using UnityEngine.UI;

public class Vignette : MonoBehaviour
{
    [Header("Vignette")]
    [Tooltip("Độ tối ở rìa (0-1)")]
    [Range(0f, 1f)]
    public float intensity = 0.55f;
    [Tooltip("Rìa bắt đầu tối từ bao xa (0 = tâm, 1 = góc màn hình)")]
    [Range(0.2f, 0.9f)]
    public float innerRadius = 0.55f;
    [Tooltip("Màu viền (đen hoặc xanh tím cho ma mị)")]
    public Color vignetteColor = Color.black;
    [Tooltip("Nhịp 'thở' nhẹ của dungeon (0 = tắt)")]
    [Range(0f, 0.3f)]
    public float breatheAmount = 0.05f;
    [Tooltip("Tốc độ thở")]
    public float breatheSpeed = 0.5f;

    Image img;
    float baseAlpha = 1f;

    void Start()
    {
        // Canvas overlay
        GameObject canvasGO = new GameObject("VignetteCanvas");
        canvasGO.transform.SetParent(transform, false);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // trên hết

        // Image fullscreen
        GameObject imgGO = new GameObject("VignetteImage");
        imgGO.transform.SetParent(canvasGO.transform, false);
        img = imgGO.AddComponent<Image>();
        img.raycastTarget = false;

        Texture2D tex = MakeVignetteTexture(256);
        img.sprite = Sprite.Create(tex, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f));

        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        ApplyColor();
    }

    void Update()
    {
        if (breatheAmount > 0f && img != null)
        {
            float b = 1f + Mathf.Sin(Time.time * breatheSpeed) * breatheAmount;
            var c = img.color;
            c.a = baseAlpha * b;
            img.color = c;
        }
    }

    void ApplyColor()
    {
        if (img == null) return;
        Color c = vignetteColor;
        c.a = intensity;
        baseAlpha = intensity;
        img.color = c;
    }

    // Cho phép đổi runtime (vd theo tầng)
    public void SetIntensity(float value)
    {
        intensity = Mathf.Clamp01(value);
        ApplyColor();
    }

    // Texture: trong suốt ở tâm, tối dần ra rìa
    Texture2D MakeVignetteTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        float center = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / 1.4142f; // 0 tâm → 1 góc
                float t = Mathf.Clamp01((d - innerRadius) / (1f - innerRadius));
                t = t * t * (3f - 2f * t); // smoothstep
                pixels[y * size + x] = new Color(1f, 1f, 1f, t);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
}
