// HitStunEffect.cs — gắn vào mỗi Enemy
using System.Collections;
using UnityEngine;

public class HitStunEffect : MonoBehaviour
{
    [Header("Stun Settings")]
    public float stunDuration = 0.18f;   // freeze bao lâu (giây)
    public int flashCount = 4;       // số lần flash
    public Color flashColor = new Color(1f, 0.2f, 0.2f, 1f); // đỏ

    private SpriteRenderer sr;
    private Color originalColor;
    private Rigidbody2D rb;
    private Animator anim;
    private bool isStunned;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        originalColor = sr.color;
    }

    public void TriggerHitStun()
    {
        if (!isStunned)
            StartCoroutine(HitStunRoutine());
    }

    IEnumerator HitStunRoutine()
    {
        isStunned = true;

        // --- Freeze ---
        float savedSpeed = 1f;
        if (anim) { savedSpeed = anim.speed; anim.speed = 0f; }
        Vector2 savedVel = Vector2.zero;
        if (rb) { savedVel = rb.linearVelocity; rb.linearVelocity = Vector2.zero; }

        // --- Flash loop ---
        float flashInterval = stunDuration / (flashCount * 2);
        for (int i = 0; i < flashCount; i++)
        {
            sr.color = flashColor;
            yield return new WaitForSeconds(flashInterval);
            sr.color = originalColor;
            yield return new WaitForSeconds(flashInterval);
        }

        // --- Unfreeze ---
        if (anim) anim.speed = savedSpeed;
        if (rb) rb.linearVelocity = savedVel;
        sr.color = originalColor;
        isStunned = false;
    }
}