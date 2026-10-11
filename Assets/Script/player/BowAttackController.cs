using UnityEngine;
using UnityEngine.EventSystems;

public class BowAttackController : MonoBehaviour
{
    public PlayerMovement playerMovement;
    public PlayerAnimation playerAnimation;
    public SpriteRenderer playerRenderer;
    [Header("Bow")]
    public Transform bowPivot;
    public Transform firePoint;
    public SpriteRenderer bowRenderer;
    public Animator bowAnimator;          // param: bool Drawing, trigger Shoot
    public Arrow arrowPrefab;

    [Header("Stats")]
    public float minDrawTime = 0.25f;
    public float shotCooldown = 0.4f;     // >= cooldown server (400ms)
    public float arrowSpeed = 14f;
    public float arrowRange = 12f;
    public string attackId = "bow_shot";
    public bool IsDrawing => drawing;

    bool drawing; float drawTimer, nextShot;
    Vector2 aimDir = Vector2.down;
    Camera cam;

    void Awake() { cam = Camera.main; }
    void OnEnable() { bowRenderer.enabled = false; }
    void OnDisable() { EndDraw(); }

    void Update()
    {
        if (Time.timeScale == 0f) { EndDraw(); return; }

        if (Input.GetMouseButtonDown(0) && !drawing && Time.time >= nextShot
            && !(EventSystem.current && EventSystem.current.IsPointerOverGameObject()))
            StartDraw();

        if (!drawing) return;

        UpdateAim();
        drawTimer += Time.deltaTime;

        if (Input.GetMouseButtonUp(0))
        {
            if (drawTimer >= minDrawTime) Fire();
            EndDraw();
        }
    }

    void StartDraw()
    {
        drawing = true; drawTimer = 0f;
        playerMovement.SetAiming(true);
        bowRenderer.enabled = true;
        bowAnimator?.SetBool("Drawing", true);
        UpdateAim();
    }

    void EndDraw()
    {
        if (!drawing) return;
        drawing = false;
        playerMovement.SetAiming(false);
        playerAnimation.SetAiming(false, aimDir);
        bowRenderer.enabled = false;
        bowAnimator?.SetBool("Drawing", false);
    }

    void UpdateAim()
    {
        Vector2 m = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 d = m - (Vector2)transform.position;
        if (d.sqrMagnitude > 0.01f) aimDir = d.normalized;

        float angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
        bowPivot.rotation = Quaternion.Euler(0, 0, angle);
        // ngắm lên thì cung nằm sau nhân vật
        bowRenderer.sortingOrder = playerRenderer.sortingOrder + (aimDir.y > 0.5f ? -1 : 1);

        playerAnimation.SetAiming(true, aimDir);
    }

    void Fire()
    {
        nextShot = Time.time + shotCooldown;
        float angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
        var arrow = Instantiate(arrowPrefab, firePoint.position, Quaternion.Euler(0, 0, angle));
        arrow.Init(aimDir, arrowSpeed, arrowRange, attackId);
        bowAnimator?.SetTrigger("Shoot");
    }
}