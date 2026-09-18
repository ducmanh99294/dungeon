// TeleportAltar.cs — gắn vào object bàn tế
using UnityEngine;
using System.Collections;

public class TeleportAltar : MonoBehaviour
{
    [Header("Destination")]
    public string sceneName;
    public string spawnPointID;
    public string locationName;

    [Header("Altar Animation")]
    public Animator altarAnimator;
    public string altarClipName = "altar-activate";

    [Header("Player Animation")]
    public string playerTeleportClip = "teleport"; // tên clip trong Animator player

    private bool playerInside = false;
    private bool isTeleporting = false;

    void Update()
    {
        if (playerInside && !isTeleporting && Input.GetKeyDown(KeyCode.E))
            StartCoroutine(TeleportRoutine());
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInside = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInside = false;
    }

    IEnumerator TeleportRoutine()
    {
        isTeleporting = true;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Animator playerAnim = player?.GetComponent<Animator>();

        // 1. Play anim bàn tế
        float altarClipLength = 0f;
        if (altarAnimator != null)
        {
            altarAnimator.Play(altarClipName);
            altarClipLength = GetClipLength(altarAnimator, altarClipName);
        }

        // 2. Play anim tele player (khoá input di chuyển nếu cần)
        float playerClipLength = 0f;
        if (playerAnim != null)
        {
            var pm = player.GetComponent<PlayerMovement>();
            if (pm != null) pm.enabled = false; // khoá di chuyển lúc tele

            playerAnim.Play(playerTeleportClip);
            playerClipLength = GetClipLength(playerAnim, playerTeleportClip);
        }

        // 3. Chờ clip dài hơn chạy xong
        float wait = Mathf.Max(altarClipLength, playerClipLength);
        yield return new WaitForSeconds(wait);

        // 4. Chuyển scene
        SceneTransitionManager.Instance?.TransitionTo(sceneName, spawnPointID, locationName);

        isTeleporting = false;
    }

    float GetClipLength(Animator anim, string clipName)
    {
        if (anim == null || anim.runtimeAnimatorController == null) return 0.3f;
        foreach (var clip in anim.runtimeAnimatorController.animationClips)
            if (clip.name == clipName) return clip.length;
        return 0.3f;
    }
}