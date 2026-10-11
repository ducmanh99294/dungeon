using UnityEngine;
using UnityEngine.Tilemaps;

public class WaterSparkle : MonoBehaviour
{
    [Header("Sparkle Settings")]
    public Color colorA = new Color(0.3f, 0.6f, 0.9f, 1f);   // xanh đậm
    public Color colorB = new Color(0.6f, 0.85f, 1f, 1f);    // xanh nhạt sáng
    public float speed = 1.5f;

    private Tilemap tilemap;

    void Start()
    {
        tilemap = GetComponent<Tilemap>();
    }

    void Update()
    {
        if (tilemap == null) return;

        float t = (Mathf.Sin(Time.time * speed) + 1f) / 2f;
        tilemap.color = Color.Lerp(colorA, colorB, t);
    }
}