using UnityEngine;

public class Player : MonoBehaviour
{
    public float speed = 5f;
    public float runSpeed = 8f;

    private Rigidbody2D rb;
    private Animator anim;

    private float moveX;
    private float moveY;
    private float currentSpeed;

    private bool isAttacking;

    // --- Các biến cho chiến đấu ---
    public Transform attackPoint;      // Điểm trung tâm của nhát chém (tạo một Empty Object trước mặt Player)
    public float attackRange = 0.5f;  // Độ rộng của nhát chém
    public float attackDamage = 10f; // Sát thương mỗi nhát chém
    public LayerMask enemyLayers;     // Layer để code biết chỉ chém kẻ địch (để không chém nhầm vào tường)
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        rb.gravityScale = 0f;
    }

    void Update()
    {
        moveX = Input.GetAxis("Horizontal");
        moveY = Input.GetAxisRaw("Vertical");

        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        currentSpeed = isRunning ? runSpeed : speed;

        if (moveX > 0)
            transform.localScale = new Vector3(1, 1, 1);
        else if (moveX < 0)
            transform.localScale = new Vector3(-1, 1, 1);

        // TẤN CÔNG
        if (Input.GetMouseButtonDown(0) && !isAttacking)
        {
            anim.SetTrigger("attack");
            isAttacking = true;

            PerformAttack();
        }

        // di chuyen
        float moveMagnitude = new Vector2(moveX, moveY).magnitude;
        anim.SetFloat("speed", Mathf.Abs(moveMagnitude * currentSpeed));
    }

    void FixedUpdate()
    {
        if (!isAttacking)
        {
            // .normalized giúp nhân vật đi chéo không bị nhanh gấp đôi
            Vector2 moveDirection = new Vector2(moveX, moveY).normalized;
            rb.linearVelocity = moveDirection * currentSpeed;
        }
        else
        {
            // Đứng im khi đang chém
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void EndAttack()
    {
        isAttacking = false;
    }

    void PerformAttack()
    {
        // 1. Vẽ ra một vòng tròn tàng hình và lấy danh sách kẻ địch trúng đòn
        // Cần truyền vào: (Vị trí tâm, Bán kính, Lớp đối tượng cần kiểm tra)
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);

        // 2. Duyệt qua từng kẻ địch và trừ máu chúng
        foreach (Collider2D enemy in hitEnemies)
        {
            // Kiểm tra xem nó có Tag là Enemy không (cho chắc chắn)
            if (enemy.CompareTag("Enemy"))
            {
                // Lấy component Enemy ra và gọi hàm TakeDamage
                Enemy enemyScript = enemy.GetComponent<Enemy>();
                if (enemyScript != null)
                {
                    enemyScript.TakeDamage(attackDamage);
                }
            }
        }
    }

    // Hàm này giúp vẽ vòng tròn tấn công ra màn hình Scene để bạn dễ canh chỉnh (không hiện khi chơi)
    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}