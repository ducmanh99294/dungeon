using UnityEngine;

public class DroppedItem : MonoBehaviour
{
    [Header("Item Info")]
    public string itemName;
    public int amount = 1;
    public LootEntry.Rarity rarity;
    public ItemData itemData;

    [Header("Settings")]
    public float pickupRadius = 0.5f;
    public float floatSpeed = 2f;
    public float floatHeight = 0.3f;
    public float lifetime = 30f;

    private Transform player;
    private Vector3 startPos;
    private float floatTimer;
    private bool isPickable = false;

    static readonly Color[] rarityColors =
    {
        Color.white,
        new Color(0.3f, 1f, 0.3f),
        new Color(0.3f, 0.5f, 1f),
        new Color(0.8f, 0.3f, 1f)
    };

    void Start()
    {
        startPos = transform.position;
        floatTimer = 0f;

        GameObject p = GameObject.FindGameObjectWithTag("Player");

        if (p != null)
        {
            player = p.transform;

            Collider2D playerCol = p.GetComponent<Collider2D>();
            Collider2D itemCol = GetComponent<Collider2D>();

            if (playerCol != null && itemCol != null)
            {
                Physics2D.IgnoreCollision(itemCol, playerCol);
            }
        }

        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        int rarityIndex = (int)rarity;

        if (sr != null &&
            rarityIndex >= 0 &&
            rarityIndex < rarityColors.Length)
        {
            sr.color = rarityColors[rarityIndex];
        }

        Invoke(nameof(EnablePickup), 0.5f);

        Destroy(gameObject, lifetime);
    }

    void EnablePickup()
    {
        isPickable = true;
    }

    void Update()
    {
        floatTimer += Time.deltaTime * floatSpeed;

        float y =
            startPos.y +
            Mathf.Sin(floatTimer) * floatHeight;

        transform.position = new Vector3(
            transform.position.x,
            y,
            transform.position.z
        );

        if (!isPickable || player == null)
            return;

        float dist = Vector2.Distance(
            transform.position,
            player.position
        );

        if (dist <= pickupRadius)
        {
            Pickup();
        }
    }

    void Pickup()
    {
        Debug.Log(
            $"[Loot] Nhặt: {itemName} x{amount} ({rarity})"
        );

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem(
                itemData,
                amount
            );
        }
        else
        {
            Debug.LogWarning(
                "[Loot] InventoryManager.Instance is NULL!"
            );
        }

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            pickupRadius
        );
    }
}