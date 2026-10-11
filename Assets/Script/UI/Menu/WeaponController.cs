// WeaponController.cs — chuyển đổi vũ khí kiếm/cung.
// Server là nguồn chân lý cho chỉ số: khi equip/unequip, báo server
// (POST /api/player/inventory/equip) để server tính lại damage.
// Yêu cầu: ItemData.serverItemId phải được điền (ObjectId của Item trên MongoDB).
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using static ItemData;

public class WeaponController : MonoBehaviour
{
    public static WeaponController Instance;

    public ComboAttackController sword;
    public BowAttackController bow;
    public WeaponVisual weaponVisual;

    public ItemData Current { get; private set; }   // null = tay không, dùng combo kiếm mặc định

    void Awake() { Instance = this; }
    void Start() { Apply(); }

    public bool CanSwitch => !sword.IsAttacking && !bow.IsDrawing;

    public bool Equip(ItemData item)
    {
        if (!CanSwitch) return false;
        Current = item;
        Apply();
        NotifyServerEquip(item);
        return true;
    }

    void Apply()
    {
        bool isBow = Current != null && Current.weaponKind == WeaponKind.Bow;
        sword.enabled = !isBow;       // tắt Update() nên Fire1 không còn kích combo
        bow.enabled = isBow;
        weaponVisual?.SetWeapon(isBow ? null : Current);  // cung tự hiển thị bằng BowAttackController
    }

    // Báo server để server cộng chỉ số trang bị vào damage.
    // Item chưa có serverItemId (item local) thì báo unequip — damage tính theo base stats.
    void NotifyServerEquip(ItemData item)
    {
        var nm = NetworkManager.Instance;
        if (nm == null || string.IsNullOrEmpty(nm.Token)) return;

        if (item != null && !string.IsNullOrEmpty(item.serverItemId))
            StartCoroutine(PostEquip(item.serverItemId));
        else
            StartCoroutine(PostUnequip());
    }

    IEnumerator PostEquip(string serverItemId)
    {
        var nm = NetworkManager.Instance;
        string url = $"{nm.serverUrl}/api/player/inventory/equip";

        var form = new WWWForm();
        form.AddField("itemId", serverItemId);
        form.AddField("slot", "weapon");

        using var req = UnityWebRequest.Post(url, form);
        req.SetRequestHeader("Authorization", $"Bearer {nm.Token}");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
            Debug.LogWarning($"[Weapon] Server equip failed: {req.error}");
    }

    IEnumerator PostUnequip()
    {
        var nm = NetworkManager.Instance;
        string url = $"{nm.serverUrl}/api/player/inventory/unequip";

        var form = new WWWForm();
        form.AddField("slot", "weapon");

        using var req = UnityWebRequest.Post(url, form);
        req.SetRequestHeader("Authorization", $"Bearer {nm.Token}");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
            Debug.LogWarning($"[Weapon] Server unequip failed: {req.error}");
    }
}
