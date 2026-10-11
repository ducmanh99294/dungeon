// Thêm vào PlayerMovement.cs hoặc tạo script PlayerPersist.cs
using UnityEngine;

public class PlayerPersist : MonoBehaviour
{
    private static PlayerPersist instance;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning($"[PlayerPersist] Trùng — DESTROY {gameObject.name}");
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}