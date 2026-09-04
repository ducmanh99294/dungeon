using UnityEngine;
using System;

public class Health : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHP = 100;
    public int currentHP;

    [Header("Invincibility")]
    public bool useInvincibility = true; // tắt cho enemy, bật cho player
    public float invincibleDuration = 0.5f;
    private bool isInvincible = false;

    [Header("Knockback")]
    public bool useKnockback = true;
    public float knockbackForce = 3f;

    [Header("Effects")]
    public bool flashOnHit = true;
    public float flashDuration = 0.1f;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Animator animator;

    public bool IsDead { get; private set; } = false;

    public event Action<int, int> OnHealthChanged;
    public event Action OnDeath;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        currentHP = maxHP;
        OnHealthChanged?.Invoke(currentHP, maxHP);
    }

    public void TakeDamage(int amount, Vector2 attackerPosition = default)
    {
        if (IsDead || isInvincible) return;

        currentHP -= amount;
        currentHP = Mathf.Max(currentHP, 0);
        OnHealthChanged?.Invoke(currentHP, maxHP);

        if (flashOnHit) StartCoroutine(FlashEffect());
        if (useKnockback && attackerPosition != default)
            ApplyKnockback(attackerPosition);

        if (currentHP <= 0)
            Die();
        else if (useInvincibility) // chỉ trigger invincibility nếu bật
            StartCoroutine(InvincibilityFrame());
    }

    public void Heal(int amount)
    {
        if (IsDead) return;
        currentHP += amount;
        currentHP = Mathf.Min(currentHP, maxHP);
        OnHealthChanged?.Invoke(currentHP, maxHP);
    }

    void ApplyKnockback(Vector2 attackerPosition)
    {
        if (rb == null) return;
        Vector2 dir = ((Vector2)transform.position - attackerPosition).normalized;
        rb.AddForce(dir * knockbackForce, ForceMode2D.Impulse);
    }

    System.Collections.IEnumerator FlashEffect()
    {
        if (spriteRenderer == null) yield break;
        Color original = spriteRenderer.color;
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.color = original;
    }

    System.Collections.IEnumerator InvincibilityFrame()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibleDuration);
        isInvincible = false;
    }

    void Die()
    {
        IsDead = true;
        OnDeath?.Invoke();
        if (animator != null && HasState(animator, "die"))
            animator.Play("die");
    }

    bool HasState(Animator anim, string stateName)
    {
        return anim.HasState(0, Animator.StringToHash(stateName));
    }
}