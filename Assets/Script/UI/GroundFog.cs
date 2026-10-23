// GroundFog.cs v2 — sương mù mặt đất DẠNG LỚP TRÔI (đẹp hơn puff).
// 2 lớp texture noise liền mạch (vẽ bằng code) trôi ngược chiều nhau,
// tạo sương liên tục như thật thay vì từng bóng tròn.
// Kéo script vào Main Camera. Mọi thông số chỉnh live trong Inspector.
// Màu sương tự đổi theo tầng dungeon.
using UnityEngine;

public class GroundFog : MonoBehaviour
{
    [Header("Texture sương (tùy chọn)")]
    [Tooltip("Kéo ảnh fog PNG vào đây (nhớ tick Read/Write Enabled trong import settings). Để trống = dùng noise vẽ bằng code.")]
    public Texture2D fogTexture;
    [Tooltip("Ngưỡng đen→trong suốt (0-1). Tăng lên nếu nền đen còn lộ.")]
    [Range(0f, 0.5f)]
    public float fogThreshold = 0.06f;
    [Tooltip("Độ to của vân sương: 1 = bình thường, 2 = to gấp đôi, 0.5 = nhỏ một nửa. Chỉnh live được lúc Play.")]
    [Range(0.25f, 4f)]
    public float fogPatternScale = 1f;

    [Header("Vị trí sương")]
    [Tooltip("BẬT = sương đứng yên trong thế giới, không đi theo camera/nhân vật. TẮT = sương đi theo camera như hiện tại. Đổi xong chuột phải → Rebuild Fog.")]
    public bool lockToWorld = false;
    [Tooltip("Tâm vùng sương trong thế giới (chỉ dùng khi Lock To World bật)")]
    public Vector2 fogWorldCenter = Vector2.zero;
    [Tooltip("Kích thước vùng sương phủ (chỉ dùng khi Lock To World bật)")]
    public Vector2 fogWorldSize = new Vector2(80f, 80f);

    [Header("Lớp sương 1 (chỉnh live được)")]
    [Tooltip("Độ đậm lớp 1")]
    [Range(0f, 0.6f)] public float layer1Opacity = 0.12f;
    [Tooltip("Tốc độ trôi lớp 1 (unit/giây)")]
    public float layer1Speed = 0.45f;
    [Tooltip("Hướng trôi lớp 1")]
    public Vector2 layer1Dir = new Vector2(1f, 0.25f);

    [Header("Lớp sương 2 (chỉnh live được)")]
    [Tooltip("Độ đậm lớp 2")]
    [Range(0f, 0.6f)] public float layer2Opacity = 0.08f;
    [Tooltip("Tốc độ trôi lớp 2 (unit/giây, nên khác lớp 1)")]
    public float layer2Speed = 0.28f;
    [Tooltip("Hướng trôi lớp 2 (nên ngược lớp 1)")]
    public Vector2 layer2Dir = new Vector2(-0.7f, 0.4f);

    [Header("Màu sương theo tầng")]
    public Color[] tierFogColors = new Color[]
    {
        new Color(0.80f, 0.77f, 0.72f), // tầng 1: xám ấm
        new Color(0.64f, 0.74f, 0.86f), // tầng 2: xám băng
        new Color(0.62f, 0.54f, 0.72f), // tầng 3: xám tím
    };

    GameObject layer1GO, layer2GO;
    GameObject fogRoot; // root khi lockToWorld (đứng yên trong thế giới)
    SpriteRenderer sr1, sr2;
    Material mat1, mat2;
    float tileWorldSize = 2.56f; // 1 tile texture = bao nhiêu unit (đã nhân scale)
    float baseTileWorldSize = 2.56f; // kích thước tile gốc (chưa nhân scale)
    int currentTier = -1;
    float tierCheckTimer;
    DungeonManager dungeonManager;

    void Start()
    {
        Build();
    }

    [ContextMenu("Rebuild Fog")]
    public void Rebuild()
    {
        if (fogRoot != null) Destroy(fogRoot);
        if (layer1GO != null) Destroy(layer1GO);
        if (layer2GO != null) Destroy(layer2GO);
        fogRoot = null;
        Build();
    }

    void Build()
    {
        dungeonManager = FindFirstObjectByType<DungeonManager>();

        Transform parent;
        Vector2 coverSize;
        if (lockToWorld)
        {
            // Sương đứng yên trong thế giới — không phải con của camera
            fogRoot = new GameObject("WorldFog");
            fogRoot.transform.position = new Vector3(fogWorldCenter.x, fogWorldCenter.y, -5f);
            parent = fogRoot.transform;
            coverSize = fogWorldSize;
        }
        else
        {
            Camera cam = GetComponent<Camera>();
            if (cam == null) cam = Camera.main;
            if (cam == null) { Debug.LogError("[GroundFog] Không tìm thấy Camera!"); return; }
            parent = cam.transform;
            // Kích thước phủ 2x màn hình (để trôi không bao giờ lộ viền)
            float h = cam.orthographicSize * 2f;
            float w = h * cam.aspect;
            coverSize = new Vector2(w * 2f, h * 2f);
        }

        Texture2D fogTex;
        if (fogTexture != null)
        {
            // Convert ảnh nền đen → sương trong suốt (độ sáng thành alpha)
            fogTex = ConvertToFogTexture(fogTexture, fogThreshold);
            Debug.Log($"[GroundFog] dùng texture người dùng: {fogTexture.name}");
        }
        else
        {
            fogTex = MakeSeamlessFogTexture(256);
        }
        fogTex.wrapMode = TextureWrapMode.Mirror; // mirror: không bao giờ lộ mối nối
        baseTileWorldSize = fogTex.width / 100f; // khớp PPU=100 lúc Sprite.Create

        // Tự động sửa giá trị speed cũ (bản trước dùng đơn vị UV/giây quá chậm)
        if (layer1Speed < 0.05f)
        {
            layer1Speed = 0.45f;
            Debug.Log("[GroundFog] Tự động cập nhật Layer 1 Speed → 0.45");
        }
        if (layer2Speed < 0.05f)
        {
            layer2Speed = 0.28f;
            Debug.Log("[GroundFog] Tự động cập nhật Layer 2 Speed → 0.28");
        }

        CreateLayer("FogLayer1", parent, fogTex, coverSize, 4,
            out layer1GO, out sr1, out mat1);
        CreateLayer("FogLayer2", parent, fogTex, coverSize * 1.4f, 5,
            out layer2GO, out sr2, out mat2);
        // World mode: layer đứng yên tại tâm vùng (local 0,0,0)
        if (lockToWorld)
        {
            layer1GO.transform.localPosition = Vector3.zero;
            layer2GO.transform.localPosition = Vector3.zero;
        }
        ApplyFogScale();

        ApplySettings();
        ApplyTierColor();
        string where = lockToWorld ? $"thế giới tại {fogWorldCenter}" : $"camera {parent.gameObject.name}";
        Debug.Log($"[GroundFog] đã tạo 2 lớp sương trên {where}");
    }

    void CreateLayer(
        string name, Transform parent, Texture2D tex, Vector2 size, int order,
        out GameObject go, out SpriteRenderer sr, out Material mat)
    {
        go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, 0f, 2f);
        go.transform.localRotation = Quaternion.identity;

        // Mỗi lớp 1 material riêng để scroll offset độc lập
        Shader shader = Shader.Find("Sprites/Default");
        mat = shader != null ? new Material(shader) : new Material(Shader.Find("Standard"));
        mat.mainTexture = tex;

        sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        sr.drawMode = SpriteDrawMode.Tiled; // lặp texture liền mạch
        sr.size = size;
        sr.sortingOrder = order;
        sr.material = mat;
    }

    void Update()
    {
        // Trôi 2 lớp bằng cách di chuyển GameObject (chắc chắn hoạt động mọi pipeline).
        // Bọc vòng theo đúng 1 tile texture → lặp liền mạch, không giật.
        MoveLayer(layer1GO, layer1Dir, layer1Speed);
        MoveLayer(layer2GO, layer2Dir, layer2Speed);

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

    // Phóng to/thu nhỏ vân sương (scale GameObject) — bọc vòng vẫn liền mạch
    void ApplyFogScale()
    {
        Vector3 sc = new Vector3(fogPatternScale, fogPatternScale, 1f);
        if (layer1GO != null) layer1GO.transform.localScale = sc;
        if (layer2GO != null) layer2GO.transform.localScale = sc;
        tileWorldSize = baseTileWorldSize * fogPatternScale;
    }

    void MoveLayer(GameObject go, Vector2 dir, float speed)
    {
        if (go == null || speed <= 0f) return;
        Vector2 d = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
        Vector3 p = go.transform.localPosition;
        p.x += d.x * speed * Time.deltaTime;
        p.y += d.y * speed * Time.deltaTime;
        p.x = WrapValue(p.x, tileWorldSize);
        p.y = WrapValue(p.y, tileWorldSize);
        // giữ nguyên z lúc tạo (camera mode z=2, world mode z=0)
        go.transform.localPosition = p;
    }

    // Giữ giá trị trong [-period/2, period/2] để bọc vòng liền mạch
    float WrapValue(float v, float period)
    {
        if (period <= 0f) return v;
        v = v % period;
        if (v > period * 0.5f) v -= period;
        else if (v < -period * 0.5f) v += period;
        return v;
    }

    // Chỉnh Inspector lúc Play → áp dụng ngay
    void OnValidate()
    {
        if (!Application.isPlaying) return;
        ApplySettings();
        ApplyFogScale();
        // Đổi màu thì cần qua tầng; ở đây chỉ refresh opacity
        if (sr1 != null && sr2 != null)
        {
            var c1 = sr1.color; c1.a = layer1Opacity; sr1.color = c1;
            var c2 = sr2.color; c2.a = layer2Opacity; sr2.color = c2;
        }
    }

    void ApplySettings()
    {
        // (tốc độ/hướng đọc trực tiếp trong Update nên không cần làm gì thêm)
    }

    void ApplyTierColor()
    {
        int tier = dungeonManager != null ? dungeonManager.dungeonTier : 1;
        currentTier = tier;
        Color c = (tierFogColors != null && tier >= 1 && tier <= tierFogColors.Length)
            ? tierFogColors[tier - 1]
            : new Color(0.75f, 0.75f, 0.75f);
        if (sr1 != null)
        {
            var cc = c; cc.a = layer1Opacity;
            sr1.color = cc;
        }
        if (sr2 != null)
        {
            var cc = c; cc.a = layer2Opacity;
            sr2.color = cc;
        }
    }

    // Convert ảnh sương nền đen → texture trong suốt (độ sáng = alpha).
    // Yêu cầu: tick Read/Write Enabled trong import settings của ảnh.
    Texture2D ConvertToFogTexture(Texture2D src, float threshold)
    {
        int w = src.width, h = src.height;
        Color[] srcPixels;
        try { srcPixels = src.GetPixels(); }
        catch
        {
            Debug.LogError("[GroundFog] Ảnh chưa bật Read/Write Enabled! Click ảnh → Inspector → tick Read/Write Enabled → Apply.");
            return src;
        }
        Color[] outPixels = new Color[srcPixels.Length];
        for (int i = 0; i < srcPixels.Length; i++)
        {
            float lum = srcPixels[i].grayscale;
            float a = Mathf.Clamp01((lum - threshold) / (1f - threshold));
            outPixels[i] = new Color(1f, 1f, 1f, a);
        }
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.SetPixels(outPixels);
        tex.Apply();
        return tex;
    }

    // Noise liền mạch (tileable): value noise nhiều octave, lưới bọc vòng
    Texture2D MakeSeamlessFogTexture(int size)
    {
        System.Random rng = new System.Random(777);
        float[,] height = new float[size, size];

        int[] cellsList = { 3, 6, 12 };
        float[] amps = { 0.60f, 0.30f, 0.10f };

        for (int o = 0; o < cellsList.Length; o++)
        {
            int cells = cellsList[o];
            float amp = amps[o];
            float[,] grid = new float[cells, cells];
            for (int i = 0; i < cells; i++)
                for (int j = 0; j < cells; j++)
                    grid[i, j] = (float)rng.NextDouble();

            float cellSize = (float)size / cells;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float gx = x / cellSize;
                    float gy = y / cellSize;
                    int x0 = ((int)gx) % cells;
                    int y0 = ((int)gy) % cells;
                    int x1 = (x0 + 1) % cells; // bọc vòng → liền mạch
                    int y1 = (y0 + 1) % cells;
                    float fx = gx - (int)gx;
                    float fy = gy - (int)gy;
                    float sx = fx * fx * (3f - 2f * fx);
                    float sy = fy * fy * (3f - 2f * fy);
                    float v00 = grid[x0, y0], v10 = grid[x1, y0];
                    float v01 = grid[x0, y1], v11 = grid[x1, y1];
                    float v = v00 + (v10 - v00) * sx
                                 + (v01 - v00) * sy
                                 + (v00 - v10 - v01 + v11) * sx * sy;
                    height[x, y] += v * amp;
                }
            }
        }

        // Thành dải sương mỏng (wispy): chỉ giữ phần giá trị cao
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float v = Mathf.Clamp01(height[x, y]);
                float a = Mathf.Clamp01((v - 0.55f) / 0.45f);
                a = a * a * a; // cubic: mềm, chỉ đỉnh sương hiện rõ
                pixels[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
}
