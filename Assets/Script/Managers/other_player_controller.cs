// OtherPlayerController.cs — gắn vào prefab OtherPlayer
using UnityEngine;
using TMPro;

public class OtherPlayerController : MonoBehaviour
{
    [Header("Settings")]
    public float moveSpeed = 10f; // tốc độ lerp di chuyển

    // Data
    public string PlayerId  { get; private set; }
    public string Username  { get; private set; }

    private Vector3        targetPos;
    private SpriteRenderer sr;
    private Animator       anim;
    private GameObject     usernameLabel;
    private TMP_Text       usernameText;

    static readonly int HashSpeed = Animator.StringToHash("Speed");

    void Awake()
    {
        sr   = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        // Lerp về vị trí mới
        if (Vector3.Distance(transform.position, targetPos) > 0.01f)
        {
            transform.position = Vector3.Lerp(
                transform.position,
                targetPos,
                moveSpeed * Time.deltaTime
            );

            // Flip sprite theo hướng di chuyển
            float dx = targetPos.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.01f && sr != null)
                sr.flipX = dx < 0;

            // Anim chạy
            anim?.SetFloat(HashSpeed, 1f);
        }
        else
        {
            // Anim idle
            anim?.SetFloat(HashSpeed, 0f);
        }

        // Username label follow
        UpdateLabelPosition();
    }

    public void Initialize(string playerId, string username, GameObject labelPrefab)
    {
        PlayerId   = playerId;
        Username   = username;
        targetPos  = transform.position;

        gameObject.name = $"OtherPlayer_{username}";

        // Spawn username label
        if (labelPrefab != null)
        {
            usernameLabel = Instantiate(labelPrefab, transform.position, Quaternion.identity);
            usernameText  = usernameLabel.GetComponentInChildren<TMP_Text>();
            if (usernameText != null) usernameText.text = username;
        }
    }

    public void MoveTo(float x, float y)
    {
        targetPos = new Vector3(x, y, transform.position.z);
    }

    void UpdateLabelPosition()
    {
        if (usernameLabel == null) return;
        usernameLabel.transform.position = transform.position + Vector3.up * 0.8f;
    }

    void OnDestroy()
    {
        if (usernameLabel != null)
            Destroy(usernameLabel);
    }
}
