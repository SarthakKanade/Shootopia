using UnityEngine;
using VelocityWars.Core;
using VelocityWars.Player;
using VelocityWars;

namespace VelocityWars.Abilities
{
    [CreateAssetMenu(menuName = "VelocityWars/Abilities/Overclock")]
    public class OverclockAbilitySO : AbilitySO
    {
        public float fireRateMultiplier = 1.5f;
        public int tempUses = 999;

        public override void Activate(GameObject player, GameContext context)
        {
            var aw = player.GetComponentInChildren<ActiveWeapon>();
            if (aw != null)
            {
                aw.FireRateMultiplier = fireRateMultiplier;
                aw.IsOverclocked = true;
            }

            var visuals = player.GetComponent<OverclockVisuals>();
            if (visuals != null)
            {
                visuals.Activate(duration);
            }
        }

        public override void Deactivate(GameObject player)
        {
            var aw = player.GetComponentInChildren<ActiveWeapon>();
            if (aw != null)
            {
                aw.FireRateMultiplier = 1.0f;
                aw.IsOverclocked = false;
            }

            var visuals = player.GetComponent<OverclockVisuals>();
            if (visuals != null)
            {
                visuals.Deactivate();
            }
        }

    }
}
