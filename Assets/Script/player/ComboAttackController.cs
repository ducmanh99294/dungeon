// ComboAttackController.cs — gắn vào Player
using System.Collections;
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
    public Transform weaponPivot; // kéo WeaponPivot vào đây

    private Animator anim;
    private int comboStep = 0;
    private bool isAttacking = false;
    private bool comboWindowOpen = false;
    private bool inputQueued = false;
    private Coroutine comboRoutine;

    static readonly int HashComboStep = Animator.StringToHash("ComboStep");
    static readonly int HashAttack = Animator.StringToHash("Attack");
    static readonly int HashDirection = Animator.StringToHash("AttackDir");

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

        if (playerAnimation != null) playerAnimation.UnlockFlip();

        int dir = GetAttackDirection();
        Vector2 rawDir = GetRawDirection();

        anim.SetInteger(HashDirection, dir);
        RotateWeaponPivot(dir, rawDir);

        if (playerAnimation != null)
            playerAnimation.LockFlip(dir == 0 && rawDir.x > 0);

        while (comboStep <= maxCombo)
        {
            inputQueued = false;
            comboWindowOpen = false;

            anim.SetInteger(HashComboStep, comboStep);
            anim.SetTrigger(HashAttack);

            yield return null;
            float waited = 0f;
            while (!anim.GetCurrentAnimatorStateInfo(0).IsTag("Attack") && waited < 0.25f)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            float clipLength = anim.GetCurrentAnimatorStateInfo(0).length;

            yield return new WaitForSeconds(Mathf.Min(attackCooldown, clipLength * 0.3f));

            comboWindowOpen = true;
            float windowElapsed = 0f;
            while (windowElapsed < comboWindowDuration)
            {
                if (inputQueued && comboStep < maxCombo) break;
                windowElapsed += Time.deltaTime;
                yield return null;
            }
            comboWindowOpen = false;

            if (!inputQueued || comboStep >= maxCombo)
            {
                float remaining = clipLength - attackCooldown - comboWindowDuration;
                if (remaining > 0) yield return new WaitForSeconds(remaining);
            }

            if (inputQueued && comboStep < maxCombo)
                comboStep++;
            else
                break;
        }

        yield return new WaitForSeconds(0.15f);
        anim.SetInteger(HashComboStep, 0);
        isAttacking = false;
        comboStep = 0;
        DisableHitbox();
        if (playerAnimation != null) playerAnimation.UnlockFlip();
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
        Debug.Log($"[Combo] RotatePivot dir:{dir} angle:{angle}");
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

    public void EnableHitbox() => swordHitboxObject?.SetActive(true);
    public void DisableHitbox() => swordHitboxObject?.SetActive(false);

    public void OnAttackAnimEnd()
    {
        inputQueued = false;
        comboWindowOpen = false;
    }
}