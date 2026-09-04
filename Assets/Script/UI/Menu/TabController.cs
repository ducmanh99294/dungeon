// TabController.cs — gắn vào TabGroup
using UnityEngine;
using UnityEngine.UI;

public class TabController : MonoBehaviour
{
    [System.Serializable]
    public class Tab
    {
        public Button button;
        public Image tabImage;
        public GameObject panel;     // panel tương ứng (InventoryPanel, SkillsPanel, QuestsPanel)
        public Sprite activeSprite;
        public Sprite inactiveSprite;
    }

    public Tab[] tabs;
    private int currentTab = 0;

    void Start()
    {
        // Gắn event cho từng tab
        for (int i = 0; i < tabs.Length; i++)
        {
            int index = i; // tránh closure bug
            tabs[i].button.onClick.AddListener(() => SelectTab(index));
        }

        // Mặc định chọn tab đầu
        SelectTab(0);
    }

    public void SelectTab(int index)
    {
        Debug.Log($"[Tab] SelectTab: {index}");
        currentTab = index;
        for (int i = 0; i < tabs.Length; i++)
        {
            bool isActive = i == index;
            tabs[i].tabImage.sprite = isActive ? tabs[i].activeSprite : tabs[i].inactiveSprite;
            tabs[i].panel.SetActive(isActive);
        }
    }
}