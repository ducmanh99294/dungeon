// TorchFlicker.cs — gắn vào prefab đèn tường (wallLightPrefab).
// Làm Point Light 2D nhấp nháy nhẹ như lửa đuốc (dùng Perlin noise cho mượt).
// Mỗi đèn có seed ngẫu nhiên riêng nên không nhấp nháy đồng bộ.
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class TorchFlicker : MonoBehaviour
{
    [Header("Nhấp nháy")]
    [Tooltip("Cường độ gốc của đèn")]
    public float baseIntensity = 1f;
    [Tooltip("Biên độ nhấp nháy (cộng/trừ quanh baseIntensity)")]
    public float flickerAmount = 0.25f;
    [Tooltip("Tốc độ nhấp nháy — càng cao càng chập chờn")]
    public float flickerSpeed = 6f;

    [Header("Đu đưa ánh sáng (tùy chọn)")]
    [Tooltip("Bán kính đu đưa vị trí đèn (0 = đứng yên)")]
    public float swayRadius = 0.05f;

    Light2D light2D;
    Vector3 basePos;
    float seed;

    void Awake()
    {
        light2D = GetComponent<Light2D>();
        basePos = transform.position;
        seed = Random.Range(0f, 100f);
        if (light2D != null && baseIntensity <= 0f)
            baseIntensity = light2D.intensity;
    }

    void Update()
    {
        float n = Mathf.PerlinNoise(seed, Time.time * flickerSpeed);
        float n2 = Mathf.PerlinNoise(seed + 50f, Time.time * flickerSpeed * 0.7f);

        if (light2D != null)
            light2D.intensity = Mathf.Max(0f, baseIntensity + (n - 0.5f) * 2f * flickerAmount);

        if (swayRadius > 0f)
            transform.position = basePos + new Vector3((n - 0.5f) * 2f, (n2 - 0.5f) * 2f, 0f) * swayRadius;
    }
}
