using UnityEngine;

public class Arrow : MonoBehaviour
{
    Rigidbody2D rb;
    Vector2 startPos;
    float maxDist;
    string attackId;
    bool hit;

    public void Init(Vector2 dir, float speed, float range, string attack)
    {
        rb = GetComponent<Rigidbody2D>();
        startPos = transform.position;
        maxDist = range;
        attackId = attack;
        rb.linearVelocity = dir * speed;
    }

    void Update()
    {
        if (Vector2.Distance(startPos, transform.position) >= maxDist)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hit || !other.CompareTag("Enemy")) return;

        var nm = other.GetComponent<NetworkMonster>();
        if (nm == null || string.IsNullOrEmpty(nm.monsterId)) return;

        hit = true;
        NetworkManager.Instance?.SendMonsterAttack(nm.monsterId, attackId); // server tính damage
        other.GetComponent<HitStunEffect>()?.TriggerHitStun();
        Destroy(gameObject);
    }
}