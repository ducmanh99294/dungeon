// LightningSystem.cs — gắn vào WeatherSystem object
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LightningSystem : MonoBehaviour
{
    public static LightningSystem Instance;

    [Header("References")]
    public GameObject lightningPrefab;  // prefab chớp
    public GameObject firePrefab;       // prefab lửa cháy
    public Image lightningFlash;   // Image trắng full screen

    [Header("Lightning Settings")]
    [Range(0f, 1f)]
    public float lightningChanceRain = 0.1f;  // 10% khi mưa
    [Range(0f, 1f)]
    public float lightningChanceStorm = 0.4f;  // 40% khi bão
    public float lightningIntervalMin = 5f;    // thời gian tối thiểu giữa 2 lần sét
    public float lightningIntervalMax = 15f;   // tối đa

    [Header("Damage")]
    public int playerDamage = 30;
    public float damageRadius = 1f;   // bán kính gây damage

    [Header("Fire")]
    public float fireDuration = 10f;  // lửa cháy bao lâu
    [Range(0f, 1f)]
    public float fireChance = 0.6f; // % cây bắt lửa khi bị sét

    private float lightningTimer = 0f;
    private bool isActive = false;
    private Vector3 lastStrikePos;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (lightningFlash != null)
            lightningFlash.color = Color.clear;

        ResetTimer();

        // Lắng nghe weather change
        // Gọi SetActive từ WeatherSystem
    }

    void Update()
    {
        if (!isActive) return;

        lightningTimer -= Time.deltaTime;
        if (lightningTimer <= 0f)
        {
            TriggerLightning();
            ResetTimer();
        }
    }

    public void SetActive(bool active) => isActive = active;

    void ResetTimer()
    {
        lightningTimer = Random.Range(lightningIntervalMin, lightningIntervalMax);
    }

    void TriggerLightning()
    {
        // Tính % theo thời tiết
        float chance = WeatherSystem.Instance?.CurrentWeather == WeatherType.Storm
            ? lightningChanceStorm : lightningChanceRain;

        if (Random.value > chance) return;

        // Random vị trí trong tầm nhìn camera
        Vector3 strikePos = GetRandomCameraPosition();

        StartCoroutine(LightningRoutine(strikePos));
    }

    Vector3 GetRandomCameraPosition()
    {
        Camera cam = Camera.main;
        float h = cam.orthographicSize;
        float w = h * cam.aspect;

        // Random trong phạm vi camera
        float x = Random.Range(cam.transform.position.x - w, cam.transform.position.x + w);
        float y = cam.transform.position.y + h; // luôn bắt đầu từ top

        return new Vector3(x, y, 0f);
    }

    // Tính điểm kết thúc của tia sét (random trong camera)
    Vector3 GetRandomStrikeEnd()
    {
        Camera cam = Camera.main;
        float h = cam.orthographicSize;
        float w = h * cam.aspect;

        float x = Random.Range(cam.transform.position.x - w, cam.transform.position.x + w);
        float y = Random.Range(cam.transform.position.y - h, cam.transform.position.y);

        return new Vector3(x, y, 0f);
    }

    IEnumerator LightningRoutine(Vector3 startPos)
    {
        Vector3 endPos = GetRandomStrikeEnd();
        Debug.Log($"[Lightning] startPos:{startPos} | endPos:{endPos} | lastStrikePos sẽ set:{endPos}");

        yield return StartCoroutine(FlashRoutine());

        if (lightningPrefab != null)
        {
            var spawnPos = new Vector3(startPos.x, startPos.y, lightningPrefab.transform.position.z);
            Vector3 dir = (endPos - startPos).normalized;
            float angle = Mathf.Atan2(dir.x, -dir.y) * Mathf.Rad2Deg;

            var fx = Instantiate(lightningPrefab, spawnPos, Quaternion.Euler(0f, 0f, angle));

            // Scale Y theo khoảng cách startPos → endPos
            float dist = Vector3.Distance(startPos, endPos);
            float prefabHeight = lightningPrefab.GetComponent<SpriteRenderer>()?.bounds.size.y ?? 1f;
            float scaleY = dist / prefabHeight;
            fx.transform.localScale = new Vector3(fx.transform.localScale.x, scaleY, 1f);

            float clipLength = GetClipLength(fx.GetComponent<Animator>());
            Destroy(fx, clipLength);
        }

        // Truyền đúng endPos
        CheckLightningHit(endPos);
        Debug.Log($"[Lightning] ⚡ Sét từ {startPos} → {endPos}");
    }

    IEnumerator FlashRoutine()
    {
        if (lightningFlash == null) yield break;

        // Flash trắng nhanh
        lightningFlash.color = new Color(1f, 1f, 1f, 0.8f);
        yield return new WaitForSeconds(0.05f);
        lightningFlash.color = Color.clear;
        yield return new WaitForSeconds(0.05f);
        lightningFlash.color = new Color(1f, 1f, 1f, 0.4f);
        yield return new WaitForSeconds(0.05f);
        lightningFlash.color = Color.clear;
    }

    void CheckLightningHit(Vector3 pos)
    {
        lastStrikePos = pos;

        // Vòng vàng — check player và cây gần nhất
        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, damageRadius);

        // Nếu không thấy gì → mở rộng ra vòng đỏ
        if (hits.Length == 0)
            hits = Physics2D.OverlapCircleAll(pos, damageRadius * 3f);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                hit.GetComponent<Health>()?.TakeDamage(playerDamage, pos);
                Debug.Log("[Lightning] ⚡ Player bị sét đánh!");
            }
        }

        // Check tile cây trong vòng vàng trước
        if (IsTreeTile(pos))
        {
            if (Random.value <= fireChance) SpawnFire(pos);
        }

        // Không có cây trong vòng vàng → tìm trong vòng đỏ
        for (float r = damageRadius; r <= damageRadius * 3f; r += 0.5f)
        {
            // Check 8 hướng xung quanh
            for (int angle = 0; angle < 360; angle += 45)
            {
                float rad = angle * Mathf.Deg2Rad;
                Vector3 checkPos = pos + new Vector3(Mathf.Cos(rad) * r, Mathf.Sin(rad) * r, 0f);
                if (IsTreeTile(checkPos))
                {
                    if (Random.value <= fireChance) SpawnFire(checkPos);
                    return;
                }
            }
        }
    }
    bool IsTreeTile(Vector3 pos)
    {
        // Tìm Tilemap có tên chứa "tree" hoặc "Tree"
        var tilemaps = FindObjectsByType<UnityEngine.Tilemaps.Tilemap>(FindObjectsSortMode.None);
        foreach (var tilemap in tilemaps)
        {
            if (!tilemap.name.ToLower().Contains("tree")) continue;
            Vector3Int cellPos = tilemap.WorldToCell(pos);
            if (tilemap.HasTile(cellPos))
            {
                Debug.Log($"[Lightning] Trúng tile cây: {tilemap.name}");
                return true;
            }
        }
        return false;
    }

    void SpawnFire(Vector3 treePos)
    {
        if (firePrefab == null) return;

        // Spawn 2-4 điểm lửa random trên thân cây
        int count = Random.Range(2, 5);
        for (int i = 0; i < count; i++)
        {
            Vector2 offset = Random.insideUnitCircle * 0.5f;
            Vector3 spawnPos = new Vector3(
                treePos.x + offset.x,
                treePos.y + offset.y + Random.Range(0f, 1f),
                firePrefab.transform.position.z  // ← lấy Z từ prefab
            );
            var fire = Instantiate(firePrefab, spawnPos, Quaternion.identity);
            Destroy(fire, fireDuration);
        }

        Debug.Log($"[Lightning] Cây bắt lửa tại {treePos}");
    }

    float GetClipLength(Animator anim)
    {
        if (anim.runtimeAnimatorController == null) return 0.5f;
        foreach (var clip in anim.runtimeAnimatorController.animationClips)
            return clip.length;
        return 0.5f;
    }


    void OnDrawGizmos()
    {
        if (lastStrikePos == Vector3.zero) return;

        Vector3 strikePoint = lastStrikePos + new Vector3(0f, 1f, 0f); // lùi lên 2 unit

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(strikePoint, damageRadius * 3f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(strikePoint, 1f);
    }
}