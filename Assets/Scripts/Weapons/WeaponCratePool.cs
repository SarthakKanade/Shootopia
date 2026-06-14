using System;
using System.Collections.Generic;
using UnityEngine;

namespace VelocityWars.Weapons
{
    using VelocityWars;
    [Serializable]
    public class WeaponSpawnEntry
{
        public WeaponSO weapon;
        public int weight = 10;
    }

    [CreateAssetMenu(fileName = "WeaponCratePool", menuName = "VelocityWars/Weapons/Crate Pool")]
    public class WeaponCratePool : ScriptableObject
    {
        public List<WeaponSpawnEntry> entries = new List<WeaponSpawnEntry>();

        public WeaponSO GetRandom()
        {
            if (entries == null || entries.Count == 0) return null;

            int totalWeight = 0;
            foreach (var entry in entries)
            {
                if (entry.weapon != null)
                    totalWeight += entry.weight;
            }

            if (totalWeight <= 0) return null;

            int randomValue = UnityEngine.Random.Range(0, totalWeight);
            int currentSum = 0;

            foreach (var entry in entries)
            {
                if (entry.weapon == null) continue;

                currentSum += entry.weight;
                if (randomValue < currentSum)
                {
                    return entry.weapon;
                }
            }

            return entries[0].weapon;
        }
    }
}
