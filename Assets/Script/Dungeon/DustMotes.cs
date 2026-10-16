using UnityEngine;

// Hạt bụi / tàn lửa trôi nhẹ trong khung hình — tăng cảm giác "không khí" cho dungeon.
// Gắn vào Main Camera (kéo script vào camera là chạy). Không cần sprite/art:
// texture tròn mờ được tạo tự động lúc chạy. Dùng API ổn định, tương thích mọi bản Unity.
// Hiệu ứng rất nhẹ (alpha thấp), hạt bám theo camera khi di chuyển.
[RequireComponent(typeof(Camera))]
public class DustMotes : MonoBehaviour
{
    [Header("Mật độ")]
    [Tooltip("Số hạt hiển thị cùng lúc (ổn định). Tăng nếu muốn dày hơn.")]
    public int moteCount = 40;

    [Header("Kích thước")]
    [Tooltip("Hạt nhỏ nhất / lớn nhất (đơn vị world)")]
    public Vector2 sizeRange = new Vector2(0.06f, 0.18f);

    [Header("Chuyển động")]
    [Tooltip("Tốc độ trôi ngang")]
    public float driftSpeed = 0.25f;
    [Tooltip("Lực nâng lên nhẹ (âm = chìm xuống). Bụi thường hơi bay lên.")]
    public float riseForce = 0.05f;
    [Tooltip("Hạt sống bao lâu (giây) trước khi biến mất")]
    public float lifetime = 8f;

    [Header("Hiển thị")]
    [Tooltip("Màu hạt")]
    public Color tint = new Color(1f, 0.93f, 0.8f); // trắng ấm
    [Range(0f, 1f)]
    [Tooltip("Độ mờ (0.35 = rất nhẹ)")]
    public float opacity = 0.35f;

    ParticleSystem ps;
    Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();

        ps = gameObject.AddComponent<ParticleSystem>();

        // --- Vật liệu: soft circle tự tạo, không cần texture ngoài ---
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        var mat = new Material(Shader.Find("Sprites/Default")); // shader có sẵn mọi pipeline
        mat.mainTexture = MakeSoftCircle();
        renderer.material = mat;

        // --- Thông số hạt ---
        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = lifetime;
        main.startSpeed = driftSpeed;
        main.startColor = new Color(tint.r, tint.g, tint.b, opacity);
        main.gravityModifier = -riseForce; // âm gravity = bay lên
        main.maxParticles = moteCount * 2;

        // Kích thước hạt ngẫu nhiên trong khoảng sizeRange
        main.startSize = new ParticleSystem.MinMaxCurve(sizeRange.x, sizeRange.y);

        // --- Phát hạt: mật độ đều, vùng phát = khung camera + lề ---
        var emission = ps.emission;
        emission.rateOverTime = moteCount / lifetime;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        float halfH = cam.orthographicSize + 2f;
        float halfW = cam.orthographicSize * cam.aspect + 2f;
        shape.scale = new Vector3(halfW * 2f, halfH * 2f, 1f);

        ps.Play();

        // Không cần di chuyển gì thêm trong Update: ParticleSystem nằm ngay
        // trên GameObject của camera nên vùng phát hạt tự bám theo camera;
        // hạt đã phát ở lại trong world (simulationSpace = World) và trôi tự nhiên.
    }

    Texture2D MakeSoftCircle()
    {
        const int S = 64;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float dx = (x - S / 2f) / (S / 2f);
                float dy = (y - S / 2f) / (S / 2f);
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - d);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
        }
        tex.Apply();
        return tex;
    }
}
