// SpawnPoint.cs
using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    public string spawnID = "default"; // đặt tên tùy ý

    void Start()
    {
        // Nếu ID khớp → spawn player tại đây
        if (SceneTransitionManager.SpawnPointID == spawnID)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                player.transform.position = transform.position;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, spawnID);
#endif
    }
}