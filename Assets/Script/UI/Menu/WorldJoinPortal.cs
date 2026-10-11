// WorldJoinPortal.cs — gắn vào bàn tế dùng để join world bạn bè
using UnityEngine;
using System.Collections;

public class WorldJoinPortal : MonoBehaviour
{
    [Header("Altar Animation")]
    public Animator altarAnimator;
    public string altarClipName = "altar-activate";

    [Header("Player Animation")]
    public string playerTeleportClip = "teleport";

    [Header("Guest Spawn")]
    public string guestSpawnPointID = "guestEntry"; // SpawnPoint.spawnID tương ứng

    private bool playerInside = false;
    private bool isBusy = false;

    void Update()
    {
        if (playerInside && !isBusy && Input.GetKeyDown(KeyCode.E))
        {
            isBusy = true;
            JoinCodePopupUI.Instance?.ShowPopup(OnJoinResult);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInside = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInside = false;
    }

    void OnJoinResult(bool success)
    {
        if (success)
            StartCoroutine(TeleportRoutine());
        else
            isBusy = false; // cho phép nhập lại
    }

    IEnumerator TeleportRoutine()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Animator playerAnim = player?.GetComponent<Animator>();

        float altarClipLength = 0f;
        if (altarAnimator != null)
        {
            altarAnimator.Play(altarClipName);
            altarClipLength = GetClipLength(altarAnimator, altarClipName);
        }

        float playerClipLength = 0f;
        PlayerMovement pm = player?.GetComponent<PlayerMovement>();
        if (pm != null) pm.enabled = false;

        if (playerAnim != null)
        {
            playerAnim.Play(playerTeleportClip);
            playerClipLength = GetClipLength(playerAnim, playerTeleportClip);
        }

        yield return new WaitForSeconds(Mathf.Max(altarClipLength, playerClipLength));

        // Xóa player khác của zone cũ trước khi vào zone mới
        MultiplayerManager.Instance?.OnSceneChanged("MainMap");
        // lưu ý: OnSceneChanged ở trên chỉ ClearAllPlayers + gọi lại SendSceneChanged,
        // ở đây ta CHỈ cần ClearAllPlayers vì server đã tự chuyển zone qua join_world_by_code.
        // Nếu muốn tránh gọi nhầm SendSceneChanged, dùng thẳng:
        // MultiplayerManager.Instance?.SendMessage("ClearAllPlayers"); (hoặc public wrapper riêng)

        RepositionAtGuestSpawn();

        if (pm != null) pm.enabled = true;
        isBusy = false;
    }

    void RepositionAtGuestSpawn()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        foreach (var sp in FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None))
        {
            if (sp.spawnID == guestSpawnPointID)
            {
                player.transform.position = sp.transform.position;
                return;
            }
        }

        Debug.LogWarning($"[WorldJoinPortal] Không tìm thấy SpawnPoint id: {guestSpawnPointID}");
    }

    float GetClipLength(Animator anim, string clipName)
    {
        if (anim == null || anim.runtimeAnimatorController == null) return 0.3f;
        foreach (var clip in anim.runtimeAnimatorController.animationClips)
            if (clip.name == clipName) return clip.length;
        return 0.3f;
    }
}