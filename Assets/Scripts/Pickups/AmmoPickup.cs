using UnityEngine;
using VelocityWars;

public class AmmoPickup : Pickup
{
    [SerializeField] int ammoAmount = 100;

    protected override void OnPickup(ActiveWeapon activeWeapon)
    {
        activeWeapon.AdjustAmmo(ammoAmount);
    }
}
