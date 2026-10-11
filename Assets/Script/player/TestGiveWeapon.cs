// TestGiveWeapon.cs — SCRIPT TEST, gắn vào Player.
// Kéo ItemData vũ khí (sword.asset / arrow.asset) vào ô testWeapon trong Inspector.
// Khi bấm Play, script tự đợi InventoryManager + WeaponController sẵn sàng,
// rồi thêm vũ khí vào túi (và equip nếu bật autoEquip).
// XÓA script này khi không cần test nữa.
using System.Collections;
using UnityEngine;

public class TestGiveWeapon : MonoBehaviour
{
    [Header("Kéo ItemData vũ khí vào đây (vd: sword.asset)")]
    public ItemData testWeapon;

    [Header("Tự equip ngay khi vào game")]
    public bool autoEquip = true;

    [Header("Chờ manager tối đa (giây)")]
    public float waitTimeout = 5f;

    void Start()
    {
        StartCoroutine(WaitAndGive());
    }

    IEnumerator WaitAndGive()
    {
        if (testWeapon == null)
        {
            Debug.LogWarning("[Test] Chưa kéo vũ khí vào ô testWeapon!");
            yield break;
        }

        // Đợi các manager khởi tạo (tránh lỗi thứ tự chạy script)
        float timer = waitTimeout;
        while ((InventoryManager.Instance == null || WeaponController.Instance == null) && timer > 0f)
        {
            timer -= Time.deltaTime;
            yield return null;
        }

        var inv = InventoryManager.Instance;
        var wc = WeaponController.Instance;

        if (inv == null || wc == null)
        {
            string thieu = "";
            if (inv == null) thieu += "InventoryManager ";
            if (wc == null) thieu += "WeaponController ";
            Debug.LogError(
                $"[Test] Thiếu {thieu}trong scene!\n" +
                "Kiểm tra:\n" +
                "1) GameObject chứa script này có đang BẬT không (ô tích trong Hierarchy, kể cả các object cha)?\n" +
                "2) Script có nằm trong scene đang chạy (MainMap) không?");
            yield break;
        }

        yield return null; // đợi 1 frame cho manager init xong

        if (inv.AddItem(testWeapon, 1))
            Debug.Log($"[Test] Đã thêm {testWeapon.itemName} vào túi.");
        else
        {
            Debug.LogWarning("[Test] Thêm vào túi thất bại (túi đầy/quá nặng)!");
            yield break;
        }

        if (autoEquip)
        {
            yield return new WaitUntil(() => wc.CanSwitch);

            if (wc.Equip(testWeapon))
                Debug.Log($"[Test] Đã equip {testWeapon.itemName}.");
            else
                Debug.LogWarning("[Test] Equip thất bại!");
        }
    }
}
