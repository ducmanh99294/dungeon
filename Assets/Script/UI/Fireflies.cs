// Fireflies.cs — đom đóm / spore bay lơ lửng trong dungeon.
// Kéo script vào Main Camera trong scene Dungeon.
// Tự tạo ParticleSystem + texture phát sáng bằng code (không cần art).
// Màu tự đổi theo tầng: tầng 1 đom đóm vàng ấm, tầng 2 xanh băng, tầng 3 spore tím.
using UnityEngine;

public class Fireflies : MonoBehaviour
{
    [Header("Đom đóm")]
    [Tooltip("Số hạt")]
    public int fireflyCount = 24;
    [Tooltip("Kích thước hạt")]
    public float minSize = 0.12f;
    public float maxSize = 0.28f;
    [Tooltip("Độ sáng")]
    [Range(0.2f, 1.5f)]
    public float brightness = 0.9f;

    [Header("Màu theo tầng")]
    public Color[] tierFireflyColors = new Color[]
    {
        new Color(1f, 0.80f, 0.42f), // tầng 1: vàng ấm (đom đóm)
        new Color(0.55f, 0.88f, 1f), // tầng 2: xanh băng
        new Color(0.82f, 0.55f, 1f), // tầng 3: tím (spore ma)
    };

    ParticleSystem ps;
    int currentTier = -1;
    float tierCheckTimer;
    DungeonManager dungeonManager;

    void Start()
    {
        dungeonManager = FindFirstObjectByType<DungeonManager>();

        ps = gameObject.AddComponent<ParticleSystem>();
        var psr = gameObject.GetComponent<ParticleSystemRenderer>();

        // Texture chấm sáng (tâm trắng, hào quang mềm)
        Texture2D tex = MakeGlowTexture(64);
        Shader shader = Shader.Find("Sprites/Default");
        Material mat = shader != null ? new Material(shader) : new Material(psr.sharedMaterial);
        mat.mainTexture = tex;
        psr.material = mat;

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.duration = 12f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 12f);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.5f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 48;

        var emission = ps.emission;
        emission.rateOverTime = fireflyCount / 8f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        UpdateEmissionBox();

        // Bay lượn tự nhiên bằng Noise
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = new ParticleSystem.MinMaxCurve(0.4f, 1f);
        noise.frequency = 0.6f;
        noise.scrollSpeed = 0.4f;
        noise.quality = ParticleSystemNoiseQuality.Medium;

        // Nhấp nháy: mờ dần vào/ra (như chớp sáng)
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.3f),
                new GradientAlphaKey(0.9f, 0.7f),
                new GradientAlphaKey(0f, 1f) }
        );
        col.color = new ParticleSystem.MinMaxGradient(g);

        ApplyTierColor();
        ps.Play();
    }

    void Update()
    {
        tierCheckTimer += Time.deltaTime;
        if (tierCheckTimer > 3f)
        {
            tierCheckTimer = 0f;
            if (dungeonManager == null)
                dungeonManager = FindFirstObjectByType<DungeonManager>();
            int tier = dungeonManager != null ? dungeonManager.dungeonTier : 1;
            if (tier != currentTier)
                ApplyTierColor();
        }
    }

    void ApplyTierColor()
    {
        int tier = dungeonManager != null ? dungeonManager.dungeonTier : 1;
        currentTier = tier;
        Color c = (tierFireflyColors != null && tier >= 1 && tier <= tierFireflyColors.Length)
            ? tierFireflyColors[tier - 1]
            : new Color(1f, 0.8f, 0.4f);
        c *= brightness;
        c.a = 1f;
        var main = ps.main;
        main.startColor = new ParticleSystem.MinMaxGradient(c);
    }

    void UpdateEmissionBox()
    {
        Camera cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        if (cam == null) return;
        float h = cam.orthographicSize * 2f;
        float w = h * cam.aspect;
        var shape = ps.shape;
        shape.scale = new Vector3(w * 1.2f, h * 1.2f, 1f);
    }

    // Texture chấm phát sáng: tâm đặc, hào quang mờ
    Texture2D MakeGlowTexture(int size)
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
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - d);
                a = Mathf.Pow(a, 2.5f); // hào quang gọn
                pixels[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
}