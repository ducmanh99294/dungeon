// EnemyHealthBar.cs — gắn vào Enemy prefab
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [Header("UI References")]
    public GameObject healthBarRoot; // object chứa cả thanh máu
    public Image hpFill;        // Image fill horizontal
    public Image hpDelayFill;   // thanh vàng delay (optional, trông đẹp hơn)

    [Header("Settings")]
    public Vector3 offset = new Vector3(0f, 0.8f, 0f); // vị trí trên đầu enemy
    public float hideDelay = 3f;  // ẩn sau bao lâu không bị hit
    public float delaySpeed = 2f;  // tốc độ thanh vàng đuổi theo thanh đỏ

    private Health health;
    private Camera cam;
    private Canvas canvas;
    private float hideTimer = 0f;
    private bool isVisible = false;

    void Awake()
    {
        health = GetComponent<Health>();
        cam = Camera.main;

        // Tìm canvas trong scene
        canvas = FindFirstObjectByType<Canvas>();
    }

    void Start()
    {
        if (health != null)
            health.OnHealthChanged += OnHealthChanged;

        // Ẩn thanh máu lúc đầu
        if (healthBarRoot != null)
            healthBarRoot.SetActive(false);

        UpdateBar(1f);
    }

    void Update()
    {
        if (!isVisible) return;

        // Di chuyển thanh máu theo enemy
        UpdatePosition();

        // Delay fill đuổi theo fill chính
        if (hpDelayFill != null)
            hpDelayFill.fillAmount = Mathf.Lerp(hpDelayFill.fillAmount, hpFill.fillAmount, delaySpeed * Time.deltaTime);

        // Tự ẩn sau hideDelay giây
        hideTimer -= Time.deltaTime;
        if (hideTimer <= 0f)
            HideBar();
    }

    void OnHealthChanged(int current, int max)
    {
        float ratio = (float)current / max;
        UpdateBar(ratio);
        ShowBar();
    }

    void UpdateBar(float ratio)
    {
        if (hpFill != null) hpFill.fillAmount = ratio;
    }

    void ShowBar()
    {
        if (healthBarRoot != null) healthBarRoot.SetActive(true);
        isVisible = true;
        hideTimer = hideDelay;
    }

    void HideBar()
    {
        if (healthBarRoot != null) healthBarRoot.SetActive(false);
        isVisible = false;
    }

    void UpdatePosition()
    {
        if (canvas == null || cam == null) return;

        Vector3 worldPos = transform.position + offset;
        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

        // Nếu enemy ở sau camera thì ẩn
        if (screenPos.z < 0)
        {
            healthBarRoot.SetActive(false);
            return;
        }

        healthBarRoot.GetComponent<RectTransform>().position = screenPos;
    }

    void OnDestroy()
    {
        if (health != null)
            health.OnHealthChanged -= OnHealthChanged;

        // Xóa UI khi enemy chết
        if (healthBarRoot != null)
            Destroy(healthBarRoot);
    }
}