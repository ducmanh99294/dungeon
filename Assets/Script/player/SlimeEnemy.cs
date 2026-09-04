using UnityEngine;

public class SlimeEnemy : MonoBehaviour
{
    [Header("Stats")]
    public int damage = 10;
    private Health health;

    [Header("Movement")]
    public float wanderSpeed = 1f;
    public float chaseSpeed = 2.5f;
    public float wanderRadius = 3f;
    public float wanderInterval = 2f;

    [Header("Detection")]
    public float detectRange = 4f;
    public float attackRange = 0.8f;
    public float loseRange = 6f;

    [Header("Attack")]
    public float attackCooldown = 1.5f;
    private float attackTimer = 0f;

    [Header("Hurt")]
    public float hurtDuration = 0.2f; // bao lâu ở trạng thái hurt

    [Header("References")]
    public Transform player;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private Vector2 wanderTarget;
    private float wanderTimer;
    private Vector2 startPosition;
    private Vector2 lastDirection = Vector2.down;

    private enum State { Wander, Chase, Attack, Hurt, Die }
    private State currentState = State.Wander;

    private bool isDead = false;
    private float hurtTimer = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        health = GetComponent<Health>();
    }

    void Start()
    {
        startPosition = transform.position;
        PickNewWanderTarget();

        if (health != null)
        {
            health.OnDeath += Die;
            health.OnHealthChanged += OnHealthChanged; // lắng nghe khi bị hit
        }

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    // Gọi mỗi khi HP thay đổi (bị hit)
    void OnHealthChanged(int current, int max)
    {
        if (isDead) return;
        if (current < max) // bị trừ máu
            TriggerHurt();
    }

    public void TriggerHurt()
    {
        if (isDead) return;
        currentState = State.Hurt;
        hurtTimer = hurtDuration;
        rb.linearVelocity = Vector2.zero;
        PlayDirectionalAnimation("hurt", lastDirection);
    }

    void Update()
    {
        if (isDead || player == null || health == null) return;

        float dist = Vector2.Distance(transform.position, player.position);
        attackTimer -= Time.deltaTime;

        // Hurt state — tunggu selesai
        if (currentState == State.Hurt)
        {
            hurtTimer -= Time.deltaTime;
            if (hurtTimer <= 0f)
                currentState = dist <= attackRange ? State.Attack : dist <= detectRange ? State.Chase : State.Wander;
            return;
        }

        // Chuyển state
        switch (currentState)
        {
            case State.Wander:
                if (dist <= detectRange) currentState = State.Chase;
                break;
            case State.Chase:
                if (dist <= attackRange) currentState = State.Attack;
                else if (dist > loseRange) currentState = State.Wander;
                break;
            case State.Attack:
                if (dist > attackRange) currentState = State.Chase;
                break;
        }

        // Thực thi state
        switch (currentState)
        {
            case State.Wander: DoWander(); break;
            case State.Chase: DoChase(); break;
            case State.Attack: DoAttack(); break;
        }
    }

    void DoWander()
    {
        wanderTimer -= Time.deltaTime;
        if (wanderTimer <= 0f) PickNewWanderTarget();

        Vector2 dir = wanderTarget - (Vector2)transform.position;
        if (dir.magnitude > 0.1f)
        {
            dir.Normalize();
            MoveTowards(dir, wanderSpeed);
            PlayDirectionalAnimation("move", dir);
        }
        else
            PlayDirectionalAnimation("idle", lastDirection);
    }

    void PickNewWanderTarget()
    {
        wanderTarget = startPosition + Random.insideUnitCircle * wanderRadius;
        wanderTimer = wanderInterval;
    }

    void DoChase()
    {
        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        MoveTowards(dir, chaseSpeed);
        PlayDirectionalAnimation("move", dir);
    }

    void DoAttack()
    {
        rb.linearVelocity = Vector2.zero;
        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        PlayDirectionalAnimation("attack", dir);

        if (attackTimer <= 0f)
        {
            attackTimer = attackCooldown;
            DealDamageToPlayer();
        }
    }

    void DealDamageToPlayer()
    {
        Health playerHealth = player.GetComponent<Health>();
        if (playerHealth != null)
            playerHealth.TakeDamage(damage, transform.position);
    }

    void MoveTowards(Vector2 direction, float speed)
    {
        rb.linearVelocity = direction * speed;
    }

    void PlayDirectionalAnimation(string prefix, Vector2 direction)
    {
        if (direction != Vector2.zero) lastDirection = direction;

        string clipName;
        if (Mathf.Abs(lastDirection.x) >= Mathf.Abs(lastDirection.y))
        {
            clipName = prefix + "-l";
            if (spriteRenderer != null) spriteRenderer.flipX = lastDirection.x > 0;
        }
        else
        {
            if (spriteRenderer != null) spriteRenderer.flipX = false;
            clipName = lastDirection.y > 0 ? prefix + "-u" : prefix + "-d";
        }

        SetAnimation(clipName);
    }

    void SetAnimation(string clipName)
    {
        if (animator == null) return;
        if (animator.GetCurrentAnimatorStateInfo(0).IsName(clipName)) return;
        animator.Play(clipName);
    }

    void Die()
    {
        isDead = true;
        currentState = State.Die;
        rb.linearVelocity = Vector2.zero;
        PlayDirectionalAnimation("die", lastDirection);

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Destroy(gameObject, 1.5f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, loseRange);
    }
}