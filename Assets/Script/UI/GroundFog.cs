// GroundFog.cs — sương mù mặt đất trôi là là trên sàn dungeon.
// Kéo script vào Main Camera trong scene Dungeon.
// Tự tạo ParticleSystem + texture mềm bằng code (không cần art).
// Màu sương tự đổi theo tầng dungeon (đọc từ DungeonManager nếu có).
using UnityEngine;

public class GroundFog : MonoBehaviour
{
    [Header("Sương")]
    [Tooltip("Số hạt sương")]
    public int puffCount = 18;
    [Tooltip("Kích thước hạt (unit)")]
    public float minSize = 3f;
    public float maxSize = 6f;
    [Tooltip("Độ mờ (0-1), càng thấp càng tinh tế")]
    [Range(0.02f, 0.5f)]
    public float opacity = 0.10f;
    [Tooltip("Tốc độ trôi")]
    public float driftSpeed = 0.35f;

    [Header("Màu sương theo tầng")]
    public Color[] tierFogColors = new Color[]
    {
        new Color(0.78f, 0.75f, 0.70f), // tầng 1: xám ấm
        new Color(0.62f, 0.72f, 0.84f), // tầng 2: xám băng
        new Color(0.60f, 0.52f, 0.70f), // tầng 3: xám tím
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

        // Texture mềm vẽ bằng code
        Texture2D tex = MakePuffTexture(128);
        Shader shader = Shader.Find("Sprites/Default");
        Material mat = shader != null ? new Material(shader) : new Material(psr.sharedMaterial);
        mat.mainTexture = tex;
        psr.material = mat;

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.duration = 20f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(10f, 18f);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, driftSpeed);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 48;

        var emission = ps.emission;
        emission.rateOverTime = puffCount / 12f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        UpdateEmissionBox();

        // Mờ dần vào/ra
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.25f),
                new GradientAlphaKey(1f, 0.75f),
                new GradientAlphaKey(0f, 1f) }
        );
        col.color = new ParticleSystem.MinMaxGradient(g);

        // Trôi ngang nhẹ + bốc lên rất chậm
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-driftSpeed, driftSpeed);
        vel.y = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);

        ApplyTierColor();
        ps.Play();
    }

    void Update()
    {
        // Kiểm tra tầng mỗi 3s (phòng khi reuse scene)
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
        Color c = (tierFogColors != null && tier >= 1 && tier <= tierFogColors.Length)
            ? tierFogColors[tier - 1]
            : new Color(0.7f, 0.7f, 0.7f);
        c.a = opacity;
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
        shape.scale = new Vector3(w * 1.3f, h * 1.3f, 1f);
    }

    // Texture tròn mềm (trắng, alpha mờ dần từ tâm)
    Texture2D MakePuffTexture(int size)
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
                a = a * a; // mềm hơn
                pixels[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
}