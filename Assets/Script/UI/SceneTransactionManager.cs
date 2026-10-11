// SceneTransitionManager.cs — gắn vào SceneTransition object (luôn active)
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance;

    [Header("UI")]
    public Image fadeOverlay;    // Image đen full screen
    public TMP_Text locationText;   // tên địa điểm mới

    [Header("Settings")]
    public float fadeDuration = 0.5f;
    public float locationShowTime = 1.5f; // hiện tên bao lâu

    // Spawn point khi load scene mới
    public static string SpawnPointID = "default";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    void Start()
    {
        if (fadeOverlay != null) fadeOverlay.color = Color.clear;
        if (locationText != null) locationText.alpha = 0f;
    }

    // Gọi từ ScenePortal
    public void TransitionTo(string sceneName, string spawnID, string locationName)
    {
        SpawnPointID = spawnID;
        StartCoroutine(TransitionRoutine(sceneName, locationName));
    }

    IEnumerator TransitionRoutine(string sceneName, string locationName)
    {
        // 1. Fade đen
        yield return StartCoroutine(Fade(0f, 1f));

        // 2. Hiện tên địa điểm
        if (locationText != null)
        {
            locationText.text = locationName;
            locationText.alpha = 1f;
        }

        yield return new WaitForSeconds(locationShowTime);

        // 3. Load scene
        yield return SceneManager.LoadSceneAsync(sceneName);

        // 4. Fade sáng
        yield return StartCoroutine(Fade(1f, 0f));

        // 5. Ẩn tên địa điểm
        if (locationText != null) locationText.alpha = 0f;
    }

    IEnumerator Fade(float from, float to)
    {
        if (fadeOverlay == null) yield break;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            fadeOverlay.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }
        fadeOverlay.color = new Color(0f, 0f, 0f, to);
    }
}