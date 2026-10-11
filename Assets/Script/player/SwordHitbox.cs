// SwordHitbox.cs — gắn vào collider của sword animation
using UnityEngine;

public class SwordHitbox : MonoBehaviour
{
    public HitSparkSpawner sparkSpawner;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;

        var netMonster = other.GetComponent<NetworkMonster>();
        if (netMonster == null)
        {
            Debug.LogWarning($"[Hitbox] {other.gameObject.name} thiếu NetworkMonster!");
            return;
        }

        // Server-authoritative: chỉ báo server, damage do server tính
        netMonster.ReportDamage("basic_attack");

        // Hiệu ứng thị giác local (không ảnh hưởng logic)
        Vector2 contact = other.ClosestPoint(transform.position);
        Vector2 direction = (other.transform.position - transform.position).normalized;

        if (sparkSpawner != null)
            sparkSpawner.SpawnSpark(contact, direction);

        other.GetComponent<HitStunEffect>()?.TriggerHitStun();
    }
}
