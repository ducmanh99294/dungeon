// ScenePortal.cs — gắn vào DoorTrigger object
using UnityEngine;

public class ScenePortal : MonoBehaviour
{
    [Header("Destination")]
    public string sceneName;      // tên scene muốn chuyển đến
    public string spawnPointID;   // ID điểm spawn trong scene mới
    public string locationName;   // tên hiện lên màn hình "Dark Forest"

    [Header("Settings")]
    public bool requireInteract = false; // true = nhấn E, false = tự động

    private bool playerInside = false;

    void Update()
    {
        if (requireInteract && playerInside && Input.GetKeyDown(KeyCode.E))
            Transition();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[Portal] Trigger: {other.gameObject.name} tag:{other.tag}");
        if (!other.CompareTag("Player")) return;
        playerInside = true;
        if (!requireInteract) Transition();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerInside = false;
    }

    void Transition()
    {
        Debug.Log($"[Portal] Chuyển đến: {sceneName} | SpawnID: {spawnPointID}");
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[Portal] sceneName trống!");
            return;
        }
        SceneTransitionManager.Instance?.TransitionTo(sceneName, spawnPointID, locationName);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        var col = GetComponent<BoxCollider2D>();
        if (col != null)
            Gizmos.DrawCube(transform.position + (Vector3)col.offset, col.size);

        // Hiện tên scene
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up, $"→ {sceneName}");
#endif
    }
}