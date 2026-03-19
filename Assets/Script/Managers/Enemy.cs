using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float health = 30f;    // Máu của quái
    public float speed = 2f;     // Tốc độ đuổi theo

    private Transform player;    // Mục tiêu để đuổi
    private Rigidbody2D rb;
    private SpriteRenderer sr;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        // Tìm nhân vật chính bằng Tag "Player" (Mặc định Unity có sẵn tag này)
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
        }
    }

    void FixedUpdate()
    {
        // Nếu tìm thấy player, hãy đuổi theo
        if (player != null)
        {
            // Tính hướng đi từ Quái đến Player
            Vector2 direction = (player.position - transform.position).normalized;

            // Di chuyển
            rb.linearVelocity = direction * speed;

            // Xoay mặt quái sang trái/phải theo hướng đi
            if (direction.x > 0) sr.flipX = true; // Quay phải
            else if (direction.x < 0) sr.flipX = false; // Quay trái
        }
    }

    // Hàm nhận sát thương (sẽ được gọi từ code Player)
    public void TakeDamage(float damage)
    {
        health -= damage;
        Debug.Log("Quái trúng đòn! Máu còn: " + health);

        // Hiệu ứng nháy đỏ khi trúng đòn (tùy chọn nhưng nên có)
        StartCoroutine(HitEffect());

        if (health <= 0)
        {
            Die();
        }
    }

    System.Collections.IEnumerator HitEffect()
    {
        sr.color = Color.red; // Chuyển sang màu đỏ rực
        yield return new WaitForSeconds(0.1f); // Chờ 0.1 giây
        sr.color = Color.white; // Trở lại màu bình thường
    }

    void Die()
    {
        Debug.Log("Quái đã chết!");
        // Tương lai: Chạy animation chết, rớt tiền...
        Destroy(gameObject); // Xóa con quái khỏi màn hình
    }
}