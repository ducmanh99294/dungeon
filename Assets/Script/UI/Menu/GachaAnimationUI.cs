// GachaAnimationUI.cs — gắn vào GachaPanel
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

public class GachaAnimationUI : MonoBehaviour
{
    public static GachaAnimationUI Instance;

    [Header("UI References")]
    public GameObject gachaPanel;
    public RawImage videoDisplay;
    public GameObject skillPopup;
    public Button btnContinue;

    [Header("Skill Reveal")]
    public Image skillIcon;
    public TMP_Text skillName;
    public TMP_Text skillDescription;
    public TMP_Text skillType;
    public TMP_Text manaCost;
    public TMP_Text cooldown;

    [Header("Video")]
    public VideoPlayer videoPlayer;
    public VideoClip gachaClip;

    [Header("Rarity Colors")]
    public Color commonColor = Color.white;
    public Color uncommonColor = new Color(0.3f, 1f, 0.3f);
    public Color rareColor = new Color(0.3f, 0.5f, 1f);
    public Color epicColor = new Color(0.8f, 0.3f, 1f);
    public Color legendaryColor = new Color(1f, 0.6f, 0.1f);

    private SkillData pendingSkill;
    private int pendingLevel;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        gachaPanel.SetActive(false);
        skillPopup.SetActive(false);
        btnContinue.gameObject.SetActive(false);
        btnContinue.onClick.AddListener(OnContinue);

        // Lắng nghe khi video kết thúc
        videoPlayer.loopPointReached += OnVideoEnd;
    }

    // Gọi từ DroppedItem khi nhặt Skill Book
    public void PlayGacha(SkillData skill, int level)
    {
        pendingSkill = skill;
        pendingLevel = level;
        gachaPanel.SetActive(true); // bật trước khi StartCoroutine
        StartCoroutine(GachaRoutine());
    }

    IEnumerator GachaRoutine()
    {
        skillPopup.SetActive(false);
        btnContinue.gameObject.SetActive(false);

        // Pause game
        Time.timeScale = 1f;

        // Play video
        videoPlayer.clip = gachaClip;
        videoPlayer.Play();

        // Đợi video chạy xong (dùng unscaled time vì timeScale = 0)
        while (videoPlayer.isPlaying)
            yield return null;
    }

    void OnVideoEnd(VideoPlayer vp)
    {
        // Hiện skill reveal
        skillPopup.SetActive(true);
        btnContinue.gameObject.SetActive(true);

        if (pendingSkill == null) return;

        skillIcon.sprite = pendingSkill.icon;
        skillName.text = pendingSkill.skillName;
        if (skillDescription != null) skillDescription.text = pendingSkill.description;
        if (skillType != null) skillType.text = $"Type: {pendingSkill.skillType}";
        if (manaCost != null) manaCost.text = $"Mana Cost: {pendingSkill.manaCost}";
        if (cooldown != null) cooldown.text = $"Cooldown: {pendingSkill.cooldown} sec";
    }

    Color GetRarityColor(SkillData skill)
    {
        // Dựa theo maxLevel làm tiêu chí rarity
        return skill.maxLevel switch
        {
            1 => commonColor,
            2 => uncommonColor,
            3 => rareColor,
            4 => epicColor,
            _ => legendaryColor
        };
    }

    void OnContinue()
    {
        // Học skill
        if (pendingSkill != null)
            SkillManager.Instance?.LearnSkill(pendingSkill);

        // Đóng panel
        gachaPanel.SetActive(false);
        Time.timeScale = 1f;
        pendingSkill = null;
    }
}