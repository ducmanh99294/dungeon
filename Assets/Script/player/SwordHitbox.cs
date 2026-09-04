// SwordHitbox.cs — gắn vào collider của sword animation
using UnityEngine;

public class SwordHitbox : MonoBehaviour
{
    public HitSparkSpawner sparkSpawner;
    public GameObject hitSparkPrefab; // kéo prefab trực tiếp vào đây nếu không dùng spawner
    public int damageAmount = 10;

    void OnEnable()
    {
        Debug.Log($"[Hitbox] ENABLED  at {Time.time:F3}");
    }

    void OnDisable()
    {
        Debug.Log($"[Hitbox] DISABLED at {Time.time:F3}");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[Hitbox] HIT at {Time.time:F3}");
        if (!other.CompareTag("Enemy"))
        {
            Debug.Log("[Hitbox] Không phải Enemy, bỏ qua");
            return;
        }

        // 1. Damage
        var health = other.GetComponent<Health>();
        if (health != null)
        {
            Debug.Log($"[Hitbox] Gọi TakeDamage({damageAmount}) lên {other.gameObject.name}");
            health.TakeDamage(damageAmount, transform.position);
        }
        else
        {
            Debug.LogWarning($"[Hitbox] {other.gameObject.name} không có component Health!");
        }

        // 2. Spark
        Vector2 contact = other.ClosestPoint(transform.position);
        Vector2 direction = (other.transform.position - transform.position).normalized;
        if (sparkSpawner != null)
            sparkSpawner.SpawnSpark(contact, direction);
        else if (hitSparkPrefab != null)
        {
            // Spawn trực tiếp không cần spawner
            var fx = Instantiate(hitSparkPrefab, contact, Quaternion.identity);
            Destroy(fx, 0.15f);
        }
        else
            Debug.LogWarning("[Hitbox] SparkSpawner và hitSparkPrefab đều chưa gắn!");

        // 3. Hit stun
        var stun = other.GetComponent<HitStunEffect>();
        if (stun != null) stun.TriggerHitStun();
    }
}