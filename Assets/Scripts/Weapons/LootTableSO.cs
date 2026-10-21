using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shootopia
{
    public enum LootItemType
    {
        Weapon,
        FullHeal,
        Shield,
        Invisibility
    }

    [Serializable]
    public class LootEntry
    {
        public string displayName;
        public LootItemType itemType = LootItemType.Weapon;
        public WeaponAttachSO weaponAttach;
        public GameObject customPreviewPrefab;
        [Tooltip("Higher weight = higher chance of dropping relative to other items in the table.")]
        public float dropWeight = 10f;
    }

    [CreateAssetMenu(fileName = "LootTable_", menuName = "Shootopia/Loot Table")]
    public class LootTableSO : ScriptableObject
    {
        [Header("Loot Pool")]
        [SerializeField] private List<LootEntry> entries = new List<LootEntry>();

        public List<LootEntry> Entries => entries;

        /// <summary>
        /// Selects a random LootEntry index based on weighted drop chances.
        /// </summary>
        public int GetRandomEntryIndex()
        {
            if (entries == null || entries.Count == 0) return -1;

            /*
            // [ISOLATED / COMMENTED OUT] Weighted drop chances logic:
            float totalWeight = 0f;
            foreach (var entry in entries)
            {
                if (entry != null && entry.dropWeight > 0f)
                {
                    totalWeight += entry.dropWeight;
                }
            }

            if (totalWeight <= 0f) return 0;

            float randomValue = UnityEngine.Random.Range(0f, totalWeight);
            float currentSum = 0f;

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null || entry.dropWeight <= 0f) continue;
                currentSum += entry.dropWeight;
                if (randomValue <= currentSum)
                {
                    return i;
                }
            }

            return entries.Count - 1;
            */

            // Equal chance for all items in that chest
            return UnityEngine.Random.Range(0, entries.Count);
        }

        /// <summary>
        /// Selects a random LootEntry based on weighted drop chances.
        /// </summary>
        public LootEntry GetRandomEntry()
        {
            int index = GetRandomEntryIndex();
            if (index >= 0 && index < entries.Count) return entries[index];
            return null;
        }
    }
}
