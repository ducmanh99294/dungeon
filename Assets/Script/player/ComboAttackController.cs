// ComboAttackController.cs — gắn vào Player
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class ComboAttackController : MonoBehaviour
{
    [Header("Combo Settings")]
    public int maxCombo = 3;
    public float attackCooldown = 0.1f;
    public bool IsAttacking => isAttacking;

    [Header("References")]
    public GameObject swordHitboxObject;
    public PlayerAnimation playerAnimation;
    public PlayerMovement playerMovement;

    [Header("Weapon Visual")]
    public Animator weaponAnimator; // Animator riêng của vũ khí

    private Animator anim;
    private int comboStep = 0;
    private bool isAttacking = false;
    private bool inputQueued = false;
    private Coroutine comboRoutine;

    static readonly int HashComboStep = Animator.StringToHash("ComboStep");
    static readonly int HashAttack = Animator.StringToHash("Attack");
    static readonly int HashDirection = Animator.StringToHash("AttackDir");
    static readonly int HashIsMoving = Animator.StringToHash("IsMoving");
    static readonly int HashIsRunning = Animator.StringToHash("IsRunning");

    void Awake()
    {
        anim = GetComponent<Animator>();
    }

    void Start()
    {
        if (anim.runtimeAnimatorController == null)
            Debug.LogError("[Combo] Animator chưa gắn Controller!", this);
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;
        if (Input.GetButtonDown("Fire1"))
        {
            if (EventSystem.current && EventSystem.current.IsPointerOverGameObject()) return;
            if (!isAttacking) StartCombo();
            else if (comboStep < maxCombo) inputQueued = true;
        }
    }

    public void StartCombo()
    {
        if (comboRoutine != null) StopCoroutine(comboRoutine);
        comboStep = 1;
        comboRoutine = StartCoroutine(ComboRoutine());
    }

    IEnumerator ComboRoutine()
    {
        isAttacking = true;

        // Snapshot hướng đánh lúc bắt đầu combo (giữ nguyên suốt combo)
        int dir = GetAttackDirection();
        anim.SetInteger(HashDirection, dir);
        if (weaponAnimator != null)
            weaponAnimator.SetInteger(HashDirection, dir);

        if (playerAnimation != null)
        {
            playerAnimation.UnlockFlip();
            playerAnimation.LockMovementBools();
        }

        Vector2 rawDir = GetRawDirection();
        if (playerAnimation != null)
            playerAnimation.LockFlip(dir == 0 && rawDir.x > 0);

        bool isMoving = GetIsMoving();
        bool isRunning = isMoving && playerMovement != null && playerMovement.IsRunning;
        anim.SetBool(HashIsMoving, isMoving);
        anim.SetBool(HashIsRunning, isRunning);
        if (weaponAnimator != null)
        {
            weaponAnimator.SetBool(HashIsMoving, isMoving);
            weaponAnimator.SetBool(HashIsRunning, isRunning);
            weaponAnimator.SetInteger(HashDirection, dir);
        }

        while (comboStep <= maxCombo)
        {
            inputQueued = false;

            anim.SetInteger(HashComboStep, comboStep);
            anim.SetInteger(HashDirection, dir);
            if (weaponAnimator != null)
            {
                weaponAnimator.SetInteger(HashComboStep, comboStep);
                weaponAnimator.SetInteger(HashDirection, dir);
            }

            anim.SetTrigger(HashAttack);
            if (weaponAnimator != null)
                weaponAnimator.SetTrigger(HashAttack);

            // Chờ Player vào state Attack (tag "Attack")
            yield return null;
            float waited = 0f;
            while (!anim.GetCurrentAnimatorStateInfo(0).IsTag("Attack") && waited < 0.5f)
            {
                waited += Time.deltaTime;
                yield return null;
            }
            if (!anim.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
            {
                Debug.LogWarning("[COMBO] Attack state không bắt đầu! Kiểm tra tag \"Attack\" trong Animator.");
                break;
            }

            // Chờ một khoảng ngắn trước khi cho phép chuyển combo
            yield return new WaitForSeconds(attackCooldown);

            // Chờ animation hiện tại kết thúc
            while (anim.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
                yield return null;

            // Sang combo tiếp theo nếu có input queue
            if (inputQueued && comboStep < maxCombo)
                comboStep++;
            else
                break;
        }

        // Kết thúc combo: reset về trạng thái thường
        anim.ResetTrigger(HashAttack);
        if (weaponAnimator != null)
            weaponAnimator.ResetTrigger(HashAttack);

        anim.SetInteger(HashComboStep, 0);
        if (weaponAnimator != null)
            weaponAnimator.SetInteger(HashComboStep, 0);

        bool movingAfterAttack = GetIsMoving();
        bool runningAfterAttack = playerMovement != null && playerMovement.IsRunning;
        anim.SetBool(HashIsMoving, movingAfterAttack);
        anim.SetBool(HashIsRunning, runningAfterAttack);
        if (weaponAnimator != null)
        {
            weaponAnimator.SetBool(HashIsMoving, movingAfterAttack);
            weaponAnimator.SetBool(HashIsRunning, runningAfterAttack);
        }

        isAttacking = false;
        comboStep = 0;

        DisableHitbox();

        if (playerAnimation != null)
        {
            playerAnimation.UnlockFlip();
            playerAnimation.UnlockMovementBools();
        }
    }

    int GetAttackDirection()
    {
        Vector2 d = GetRawDirection();
        if (Mathf.Abs(d.y) > Mathf.Abs(d.x))
            return d.y > 0 ? 1 : 2;
        return 0;
    }

    Vector2 GetRawDirection()
    {
        if (playerAnimation != null) return playerAnimation.LastDirection;
        return Vector2.down;
    }

    bool GetIsMoving()
    {
        if (playerMovement == null) return false;
        return playerMovement.CurrentMovement != Vector2.zero;
    }

    // Gọi từ Animation Event để bật/tắt hitbox đúng nhịp chém
    public void EnableHitbox() => swordHitboxObject?.SetActive(true);
    public void DisableHitbox() => swordHitboxObject?.SetActive(false);
}
