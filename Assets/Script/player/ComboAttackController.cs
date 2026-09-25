// ComboAttackController.cs — gắn vào Player
using System.Collections;
//using System.Diagnostics;
using UnityEngine;

public class ComboAttackController : MonoBehaviour
{
    [Header("Combo Settings")]
    public int maxCombo = 3;
    public float attackCooldown = 0.1f;
    public float comboWindowDuration = 0.5f;

    [Header("References")]
    public GameObject swordHitboxObject;
    public PlayerAnimation playerAnimation;
    public PlayerMovement playerMovement;
    public Transform weaponPivot; // kéo WeaponPivot vào đây

    [Header("Weapon Visual")]
    public Animator weaponAnimator; // Animator riêng của vũ khí (con của weaponPivot)

    private Animator anim;
    private int comboStep = 0;
    private bool isAttacking = false;
    private bool comboWindowOpen = false;
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
        if (Input.GetButtonDown("Fire1"))
        {
            if (!isAttacking)
                StartCombo();
            else if (comboWindowOpen && comboStep < maxCombo)
                inputQueued = true;
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

         
        // 1. Snapshot hướng tấn công
         

        int dir = GetAttackDirection();
        Vector2 rawDir = GetRawDirection();

        anim.SetInteger(HashDirection, dir);

        if (playerAnimation != null)
        {
            // Cho phép cập nhật hướng trước khi khóa
            playerAnimation.UnlockFlip();

            // Khóa movement bool trong toàn bộ combo
            playerAnimation.LockMovementBools();
        }

        // Xoay weapon theo hướng attack
        RotateWeaponPivot(dir, rawDir);

        // Khóa hướng nhân vật trong lúc attack
        if (playerAnimation != null)
        {
            playerAnimation.LockFlip(
                dir == 0 && rawDir.x > 0
            );
        }

         
        // 2. Snapshot movement
         

        bool isMoving = GetIsMoving();

        bool isRunning =
            playerMovement != null &&
            playerMovement.IsRunning;

        anim.SetBool(HashIsMoving, isMoving);
        anim.SetBool(HashIsRunning, isRunning);

        if (weaponAnimator != null)
        {
            weaponAnimator.SetBool(HashIsMoving, isMoving);
            weaponAnimator.SetBool(HashIsRunning, isRunning);
        }

         
        // 3. Combo
         

        while (comboStep <= maxCombo)
        {
            inputQueued = false;
            comboWindowOpen = false;

            // Set combo step
            anim.SetInteger(
                HashComboStep,
                comboStep
            );

            anim.SetTrigger(HashAttack);

            // Weapon animator
            if (weaponAnimator != null)
            {
                weaponAnimator.SetInteger(
                    HashComboStep,
                    comboStep
                );

                weaponAnimator.SetTrigger(HashAttack);
            }

             
            // Chờ Attack state
             

            yield return null;

            float waited = 0f;

            while (
                !anim.GetCurrentAnimatorStateInfo(0).IsTag("Attack") &&
                waited < 0.25f
            )
            {
                waited += Time.deltaTime;
                yield return null;
            }

            // Attack không start
            if (!anim.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
            {
                Debug.LogWarning(
                    "Attack animation did not start."
                );

                break;
            }

            float clipLength =
                anim.GetCurrentAnimatorStateInfo(0).length;

             
            // Attack cooldown
             

            yield return new WaitForSeconds(
                Mathf.Min(
                    attackCooldown,
                    clipLength * 0.3f
                )
            );

             
            // Combo window
         

            comboWindowOpen = true;

            float windowElapsed = 0f;

            while (
                windowElapsed < comboWindowDuration
            )
            {
                if (
                    inputQueued &&
                    comboStep < maxCombo
                )
                {
                    break;
                }

                windowElapsed += Time.deltaTime;

                yield return null;
            }

            comboWindowOpen = false;

             
            // Chờ animation kết thúc
             

            if (
                !inputQueued ||
                comboStep >= maxCombo
            )
            {
                float remaining =
                    clipLength -
                    attackCooldown -
                    comboWindowDuration;

                if (remaining > 0f)
                {
                    yield return new WaitForSeconds(
                        remaining
                    );
                }
            }

             
            // Sang combo tiếp theo
             

            if (
                inputQueued &&
                comboStep < maxCombo
            )
            {
                comboStep++;
            }
            else
            {
                break;
            }
        }

         
        // 4. Kết thúc combo
         

        yield return new WaitForSeconds(0.15f);

        anim.SetInteger(
            HashComboStep,
            0
        );

        if (weaponAnimator != null)
        {
            weaponAnimator.SetInteger(
                HashComboStep,
                0
            );
        }

        anim.SetBool(
            HashIsMoving,
            false
        );

        anim.SetBool(
            HashIsRunning,
            false
        );

        if (weaponAnimator != null)
        {
            weaponAnimator.SetBool(
                HashIsMoving,
                false
            );

            weaponAnimator.SetBool(
                HashIsRunning,
                false
            );
        }

         
        // 5. Reset state
         

        isAttacking = false;
        comboStep = 0;

        DisableHitbox();

        if (playerAnimation != null)
        {
            playerAnimation.UnlockFlip();
            playerAnimation.UnlockMovementBools();
        }
    }

    void RotateWeaponPivot(int dir, Vector2 rawDir)
    {
        if (weaponPivot == null)
        {
            Debug.LogWarning("[Combo] WeaponPivot chưa được gắn!");
            return;
        }
        float angle = dir switch
        {
            1 => 90f,
            2 => 270f,
            _ => rawDir.x < 0 ? 180f : 0f
        };
        weaponPivot.localRotation = Quaternion.Euler(0f, 0f, angle);
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

    public void EnableHitbox() => swordHitboxObject?.SetActive(true);
    public void DisableHitbox() => swordHitboxObject?.SetActive(false);

    public void OnAttackAnimEnd()
    {
        inputQueued = false;
        comboWindowOpen = false;
    }
}