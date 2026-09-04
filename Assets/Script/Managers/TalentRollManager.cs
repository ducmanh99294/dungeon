using UnityEngine;
using UnityEngine.UI;
using TMPro; // Bắt buộc phải có dòng này để dùng TextMeshPro
using UnityEngine.SceneManagement;

public class TalentRollManager : MonoBehaviour
{
    // Cắm các UI Element vào đây giống như getElementById
    public TextMeshProUGUI resultText;
    public Button rollButton;
    public Button continueButton;

    // Mảng dữ liệu chứa các Thiên Phú
    private string[] danhSachThienPhu = {
        "Sức mạnh vô song (+10 ATK)",
        "Thân thủ du long (+5 Speed)",
        "Mình đồng da sắt (+20 HP)",
        "Kẻ được chọn (X2 EXP)"
    };

    void Start()
    {
        // Khi mới vào, hiện nút Rút và ẩn nút Đi tiếp
        rollButton.gameObject.SetActive(true);
        continueButton.gameObject.SetActive(false);
    }

    // Hàm gắn vào sự kiện onClick của nút Rút Thiên Phú
    public void RollTalent()
    {
        // Random từ 0 đến chỉ số cuối của mảng
        int randomIndex = Random.Range(0, danhSachThienPhu.Length);
        string thienPhuDaChon = danhSachThienPhu[randomIndex];

        // Đổi chữ trên giao diện
        resultText.text = thienPhuDaChon;

        // Thêm hiệu ứng màu sắc (tùy chọn)
        resultText.color = Color.yellow;

        // Lưu ID thiên phú vào Local Storage của Unity
        PlayerPrefs.SetInt("TalentID", randomIndex);
        PlayerPrefs.Save();

        // Ẩn nút Rút đi, hiện nút Bắt đầu lên
        rollButton.gameObject.SetActive(false);
        continueButton.gameObject.SetActive(true);
    }

    // Hàm gắn vào sự kiện onClick của nút Bắt đầu
    public void GoToTutorial()
    {
        // Chuyển scene (Nhớ add TutorialScene vào Build Settings)
        SceneManager.LoadScene("TutorialScene");
    }
}