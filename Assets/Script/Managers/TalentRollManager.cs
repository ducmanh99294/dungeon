// TalentRollManager.cs — Thiên phú do SERVER roll và lưu.
// Client chỉ gọi API và hiển thị kết quả, không tự random, không tự cộng chỉ số.
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

public class TalentRollManager : MonoBehaviour
{
    public TextMeshProUGUI resultText;
    public Button rollButton;
    public Button continueButton;

    [System.Serializable]
    private class TalentDto
    {
        public string id;
        public string name;
        public string desc;
    }

    [System.Serializable]
    private class RollResponse
    {
        public bool success;
        public bool alreadyRolled;
        public TalentDto talent;
    }

    void Start()
    {
        rollButton.gameObject.SetActive(true);
        continueButton.gameObject.SetActive(false);
        rollButton.onClick.AddListener(RollTalent);
        continueButton.onClick.AddListener(GoToTutorial);
    }

    // Gắn vào sự kiện onClick của nút Rút Thiên Phú
    public void RollTalent()
    {
        rollButton.interactable = false;
        resultText.text = "Đang rút...";
        StartCoroutine(RollTalentRoutine());
    }

    IEnumerator RollTalentRoutine()
    {
        var nm = NetworkManager.Instance;
        if (nm == null || string.IsNullOrEmpty(nm.Token))
        {
            resultText.text = "Chưa đăng nhập!";
            rollButton.interactable = true;
            yield break;
        }

        string url = $"{nm.serverUrl}/api/player/talent/roll";
        using var req = UnityWebRequest.PostWwwForm(url, "");
        req.SetRequestHeader("Authorization", $"Bearer {nm.Token}");

        yield return req.SendWebRequest();

        rollButton.interactable = true;

        if (req.result != UnityWebRequest.Result.Success)
        {
            resultText.text = "Lỗi kết nối server!";
            Debug.LogError($"[Talent] Roll failed: {req.error}");
            yield break;
        }

        var res = JsonUtility.FromJson<RollResponse>(req.downloadHandler.text);
        if (res == null || !res.success || res.talent == null)
        {
            resultText.text = "Roll thất bại, thử lại!";
            yield break;
        }

        resultText.text = $"{res.talent.name}\n({res.talent.desc})";
        resultText.color = Color.yellow;

        rollButton.gameObject.SetActive(false);
        continueButton.gameObject.SetActive(true);
    }

    // Gắn vào sự kiện onClick của nút Bắt đầu
    public void GoToTutorial()
    {
        SceneManager.LoadScene("TutorialScene");
    }
}
