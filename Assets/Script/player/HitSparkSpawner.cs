// HitSparkSpawner.cs — gắn vào Sword / Attack Hitbox
using UnityEngine;

public class HitSparkSpawner : MonoBehaviour
{
    public GameObject hitSparkPrefab;

    public void SpawnSpark(Vector2 contactPoint, Vector2 attackDirection)
    {
        if (hitSparkPrefab == null) return;
        GameObject fx = Instantiate(hitSparkPrefab, contactPoint, Quaternion.identity);

        // Xoay effect theo hướng tấn công
        float angle = Mathf.Atan2(attackDirection.y, attackDirection.x) * Mathf.Rad2Deg;
        fx.transform.rotation = Quaternion.Euler(0, 0, angle);

        Destroy(fx, 0.4f); // tự hủy sau 0.4s
    }
}