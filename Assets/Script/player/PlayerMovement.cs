using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public bool IsRunning => isRunning;

    [Header("Toggle Key")]
    public KeyCode toggleKey = KeyCode.LeftShift; // đổi key tùy bạn

    private Rigidbody2D rb;
    private Vector2 movement;
    private PlayerAnimation playerAnimation;
    private bool isRunning = true;

    public Vector2 CurrentMovement => movement;
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerAnimation = GetComponent<PlayerAnimation>();
    }

    void Update()
    {
        // Nhận input di chuyển
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");
        movement = movement.normalized;

        // Toggle run/walk khi nhấn phím
        if (Input.GetKeyDown(toggleKey))
        {
            isRunning = !isRunning;
            playerAnimation.isRunning = isRunning;
            Debug.Log(isRunning ? "Chế độ: RUN" : "Chế độ: WALK");
        }

        // Gửi movement sang animation
        if (playerAnimation != null)
            playerAnimation.SetMovement(movement);
    }

    void FixedUpdate()
    {
        float currentSpeed = isRunning ? runSpeed : walkSpeed;
        rb.MovePosition(rb.position + movement * currentSpeed * Time.fixedDeltaTime);
    }

    public void ForceWalk()
    {
        if (!isRunning) return;
        isRunning = false;
        playerAnimation.isRunning = false;
        Debug.Log("[Movement] Hết Energy — chuyển sang Walk");
    }
}