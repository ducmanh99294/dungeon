// DroppedItem.cs — gắn vào item prefab, xử lý nhặt đồ
using NUnit.Framework.Interfaces;
using UnityEngine;

public class DroppedItem : MonoBehaviour
{
    [Header("Item Info")]
    public string itemName;
    public int amount = 1;
    public LootEntry.Rarity rarity;
    public ItemData itemData;

    [Header("Settings")]
    public float pickupRadius = 0.5f;  // tầm nhặt
    public float floatSpeed = 2f;    // tốc độ bay lên nhẹ
    public float floatHeight = 0.3f;  // độ cao bay
    public float lifetime = 30f;   // tự mất sau bao lâu

    private Transform player;
    private Vector3 startPos;
    private float floatTimer;
    private bool isPickable = false;

    // Màu theo rarity
    static readonly Color[] rarityColors =
    {
        Color.white,                        // Common
        new Color(0.3f, 1f, 0.3f),         // Uncommon
        new Color(0.3f, 0.5f, 1f),         // Rare
        new Color(0.8f, 0.3f, 1f)          // Epic
    };

    void Start()
    {
        startPos = transform.position;
        floatTimer = 0f;

        // Tìm player
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;

            Collider2D playerCol = p.GetComponent<Collider2D>();
            Collider2D itemCol = GetComponent<Collider2D>();
            if (playerCol != null && itemCol != null)
                Physics2D.IgnoreCollision(itemCol, playerCol);
        }

        // Đổi màu sprite theo rarity
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = rarityColors[(int)rarity];

        // Delay nhặt 0.5s để tránh nhặt ngay lúc drop
        Invoke(nameof(EnablePickup), 0.5f);

        // Tự hủy sau lifetime
        Destroy(gameObject, lifetime);
    }

    void EnablePickup() => isPickable = true;

    void Update()
    {
        // Float animation
        floatTimer += Time.deltaTime * floatSpeed;
        float y = startPos.y + Mathf.Sin(floatTimer) * floatHeight;
        transform.position = new Vector3(transform.position.x, y, transform.position.z);

        // Kiểm tra nhặt
        if (!isPickable || player == null) return;
        float dist = Vector2.Distance(transform.position, player.position);
        if (dist <= pickupRadius)
            Pickup();
    }

    void Pickup()
    {
        // TODO: thêm vào inventory
        Debug.Log($"[Loot] Nhặt: {itemName} x{amount} ({rarity})");

        // Hiệu ứng nhặt (optional)
        // Instantiate(pickupEffectPrefab, transform.position, Quaternion.identity);
        InventoryManager.Instance?.AddItem(itemData, amount);
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}