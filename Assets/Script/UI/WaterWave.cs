using UnityEngine;

public class WaterWave : MonoBehaviour
{
    [Header("Wave Settings")]
    public float waveSpeed = 2f;
    public float waveHeight = 0.05f;

    private Vector3 originalPosition;

    void Start()
    {
        originalPosition = transform.position;
    }

    void Update()
    {
        float wave = Mathf.Sin(Time.time * waveSpeed) * waveHeight;
        transform.position = new Vector3(
            originalPosition.x,
            originalPosition.y + wave,
            originalPosition.z
        );
    }
}