using UnityEngine;
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
        return true;
    }

    void Apply()
    {
        bool isBow = Current != null && Current.weaponKind == WeaponKind.Bow;
        sword.enabled = !isBow;       // tắt Update() nên Fire1 không còn kích combo
        bow.enabled = isBow;
        weaponVisual?.SetWeapon(isBow ? null : Current);  // cung tự hiển thị bằng BowAttackController
    }
}