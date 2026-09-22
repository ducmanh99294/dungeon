using UnityEngine;

public class WeaponVisual : MonoBehaviour
{
    public Animator weaponAnimator;
    public SpriteRenderer weaponRenderer;

    public void SetWeapon(ItemData weaponData)
    {
        if (weaponData == null || weaponData.weaponAnimatorController == null)
        {
            weaponRenderer.enabled = false;
            return;
        }

        weaponRenderer.enabled = true;
        weaponAnimator.runtimeAnimatorController = weaponData.weaponAnimatorController;
    }
}