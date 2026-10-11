using UnityEngine;

public class TreeSway : MonoBehaviour
{
    [Header("Sway Settings")]
    public float swayAmount = 2f;      // ?? nghiêng (??)
    public float swaySpeed = 1.2f;     // t?c ?? ?ung ??a
    public float randomOffset = 0f;    // ?? các cây không ?ung ??a cùng lúc

    private Quaternion originalRotation;

    void Start()
    {
        originalRotation = transform.rotation;
        // Random offset ?? m?i cây ?ung ??a l?ch pha nhau
        randomOffset = Random.Range(0f, 2f * Mathf.PI);
    }

    void Update()
    {
        float angle = Mathf.Sin(Time.time * swaySpeed + randomOffset) * swayAmount;
        transform.rotation = originalRotation * Quaternion.Euler(0, 0, angle);
    }
}