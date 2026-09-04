using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Vector2 lastDirection = Vector2.down;
    public Vector2 LastDirection => lastDirection; // cho ComboAttackController đọc
    [HideInInspector] public bool isRunning = true;
    private bool flipLocked = false; // khi đang attack, giữ nguyên flip

    public void LockFlip(bool flipRight)
    {
        flipLocked = true;
        spriteRenderer.flipX = flipRight;
    }

    public void UnlockFlip() => flipLocked = false;

    void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        flipLocked = false; // đảm bảo không bị lock từ đầu
        // FIX: báo lỗi rõ ràng thay vì crash âm thầm
        if (animator == null)
            Debug.LogError("[PlayerAnimation] Không tìm thấy Animator component!", this);
        else if (animator.runtimeAnimatorController == null)
            Debug.LogError("[PlayerAnimation] Animator chưa gắn Controller! Vào Inspector → Animator → Controller để gắn file .controller", this);
    }

    public void SetMovement(Vector2 movement)
    {
        if (!IsAnimatorReady()) return;
        if (flipLocked) return; // đang attack, không đổi flip

        bool isMoving = movement != Vector2.zero;
        if (isMoving)
        {
            if (!flipLocked) lastDirection = movement; // chỉ update khi không attack
            string prefix = isRunning ? "run" : "walk";

            if (Mathf.Abs(movement.x) >= Mathf.Abs(movement.y))
            {
                PlayAnimation(prefix + "-l");
                spriteRenderer.flipX = movement.x > 0;
            }
            else
            {
                spriteRenderer.flipX = false;
                PlayAnimation(movement.y > 0 ? prefix + "-u" : prefix + "-d");
            }
        }
        else
        {
            if (Mathf.Abs(lastDirection.x) >= Mathf.Abs(lastDirection.y))
            {
                spriteRenderer.flipX = lastDirection.x > 0;
                PlayAnimation("idle-l");
            }
            else
            {
                spriteRenderer.flipX = false;
                PlayAnimation(lastDirection.y > 0 ? "idle-up" : "idle-d");
            }
        }
    }

    private void PlayAnimation(string clipName)
    {
        if (!IsAnimatorReady()) return;
        if (animator.GetCurrentAnimatorStateInfo(0).IsName(clipName)) return;
        animator.Play(clipName);
    }

    private bool IsAnimatorReady()
    {
        return animator != null && animator.runtimeAnimatorController != null;
    }
}