// Thêm vào PlayerMovement.cs hoặc tạo script PlayerPersist.cs
using UnityEngine;

public class PlayerPersist : MonoBehaviour
{
    void Awake()
    {
        // Nếu đã có Player khác → destroy cái mới (từ scene)
        var existing = FindObjectsByType<PlayerPersist>(FindObjectsSortMode.None);
        if (existing.Length > 1)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
    }
}