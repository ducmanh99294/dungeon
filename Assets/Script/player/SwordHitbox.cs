// SwordHitbox.cs — gắn vào collider của sword animation
using System.Diagnostics;
using UnityEngine;

using UnityDebug = UnityEngine.Debug;

public class SwordHitbox : MonoBehaviour
{
    public HitSparkSpawner sparkSpawner;
    public GameObject hitSparkPrefab;
    public int damageAmount = 10;

    void OnEnable()
    {
        UnityDebug.Log($"[Hitbox] ENABLED  at {Time.time:F3}");
    }

    void OnDisable()
    {
        UnityDebug.Log($"[Hitbox] DISABLED at {Time.time:F3}");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;

        var netMonster = other.GetComponent<NetworkMonster>();

        if (netMonster != null && !string.IsNullOrEmpty(netMonster.monsterId))
        {
            // Server-authoritative — không tự TakeDamage local nữa
            NetworkManager.Instance?.SendMonsterAttack(netMonster.monsterId);
        }
        else
        {
            UnityDebug.LogWarning(
                $"[Hitbox] {other.gameObject.name} không có NetworkMonster/monsterId!"
            );
        }

        // Spark + hit stun vẫn giữ (hiệu ứng thị giác local, không ảnh hưởng logic)
        Vector2 contact = other.ClosestPoint(transform.position);
        Vector2 direction =
            (other.transform.position - transform.position).normalized;

        if (sparkSpawner != null)
            sparkSpawner.SpawnSpark(contact, direction);

        var stun = other.GetComponent<HitStunEffect>();

        if (stun != null)
            stun.TriggerHitStun();
    }
}