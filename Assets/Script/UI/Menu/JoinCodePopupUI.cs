// JoinCodePopupUI.cs — gắn vào JoinCodePopup panel
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class JoinCodePopupUI : MonoBehaviour
{
    public static JoinCodePopupUI Instance;

    [Header("UI References")]
    public GameObject popupPanel;
    public TMP_InputField codeInput;
    public Button btnSubmit;
    public Button btnCancel;
    public TMP_Text errorText;

    private System.Action<bool> onResultCallback;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        popupPanel.SetActive(false);
        btnSubmit.onClick.AddListener(OnSubmit);
        btnCancel.onClick.AddListener(ClosePopup);
        HideError();
    }

    public void ShowPopup(System.Action<bool> onResult = null)
    {
        onResultCallback = onResult;
        codeInput.text = "";
        HideError();
        popupPanel.SetActive(true);
        codeInput.Select();
        codeInput.ActivateInputField();
        Time.timeScale = 0f; // pause game trong lúc nhập
    }

    void OnSubmit()
    {
        string code = codeInput.text.Trim().ToUpper();

        if (string.IsNullOrEmpty(code))
        {
            ShowError("Vui lòng nhập mã!");
            return;
        }

        btnSubmit.interactable = false;
        HideError();

        NetworkManager.Instance?.JoinWorldByCode(code, (success, reason) =>
        {
            btnSubmit.interactable = true;

            if (success)
            {
                ClosePopup();
                onResultCallback?.Invoke(true);
            }
            else
            {
                ShowError(GetErrorMessage(reason));
                onResultCallback?.Invoke(false);
            }
        });
    }

    string GetErrorMessage(string reason)
    {
        return reason switch
        {
            "CODE_REQUIRED" => "Vui lòng nhập mã!",
            "INVALID_CODE" => "Mã không hợp lệ!",
            "CANNOT_JOIN_OWN_WORLD" => "Không thể vào thế giới của chính mình!",
            "ZONE_FULL" => "Thế giới đã đầy!",
            _ => "Tham gia thất bại, thử lại!",
        };
    }

    void ShowError(string msg)
    {
        errorText.text = msg;
        errorText.enabled = true;
    }

    void HideError()
    {
        errorText.enabled = false;
    }

    public void ClosePopup()
    {
        popupPanel.SetActive(false);
        Time.timeScale = 1f;
    }
}