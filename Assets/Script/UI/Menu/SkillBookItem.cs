// SkillBookItem.cs — gắn vào prefab Skill Book
using UnityEngine;

public class SkillBookItem : MonoBehaviour
{
    [Header("Item Info")]
    public ItemData itemData; // kéo ItemData (isSkillBook=true, có skillData) vào đây

    [Header("Settings")]
    public float pickupRadius = 0.5f;
    public float floatSpeed = 2f;
    public float floatHeight = 0.3f;
    public float lifetime = 30f;

    private Transform player;
    private Vector3 startPos;
    private float floatTimer;
    private bool isPickable = false;

    void Start()
    {
        startPos = transform.position;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            Collider2D playerCol = p.GetComponent<Collider2D>();
            Collider2D itemCol = GetComponent<Collider2D>();
            if (playerCol != null && itemCol != null)
                Physics2D.IgnoreCollision(itemCol, playerCol);
        }

        Invoke(nameof(EnablePickup), 0.5f);
        Destroy(gameObject, lifetime);
    }

    void EnablePickup() => isPickable = true;

    void Update()
    {
        floatTimer += Time.deltaTime * floatSpeed;
        float y = startPos.y + Mathf.Sin(floatTimer) * floatHeight;
        transform.position = new Vector3(transform.position.x, y, transform.position.z);

        if (!isPickable || player == null) return;
        if (Vector2.Distance(transform.position, player.position) <= pickupRadius)
            Pickup();
    }

    void Pickup()
    {
        if (itemData == null)
        {
            Debug.LogWarning("[SkillBook] itemData chưa được gắn!");
            Destroy(gameObject);
            return;
        }

        Debug.Log($"[SkillBook] Nhặt sách: {itemData.itemName}");
        InventoryManager.Instance?.AddItem(itemData, 1);
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}