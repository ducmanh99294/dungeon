// NetworkMonster.cs — gắn vào mỗi Monster prefab
using UnityEngine;

public class NetworkMonster : MonoBehaviour
{
    [Header("Network")]
    public string monsterId; // ID từ server

    private Health health;

    void Awake()
    {
        health = GetComponent<Health>();
    }

    void Start()
    {
        // Khi monster bị damage → báo server
        if (health != null)
            health.OnHealthChanged += OnHealthChanged;
    }

    void OnHealthChanged(int current, int max)
    {
        // Chỉ báo server nếu mình là người đánh
        // (tránh loop: server sync → unity update → unity báo server lại)
    }

    // Gọi từ SwordHitbox khi đánh trúng
    public void ReportDamage(int damage)
    {
        if (string.IsNullOrEmpty(monsterId)) return;
        NetworkManager.Instance?.SendMonsterAttack(monsterId, damage);
    }

    void OnDestroy()
    {
        if (health != null)
            health.OnHealthChanged -= OnHealthChanged;
    }
}
