using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

namespace Shootopia
{
    public partial class HUDController
    {
        // ──────────────────────── KILL FEED ────────────────────────

        public Sprite GetWeaponSprite(string weaponName)
        {
            if (string.IsNullOrEmpty(weaponName)) return defaultWeaponSprite != null ? defaultWeaponSprite : pistolSprite;
            string lower = weaponName.ToLower();
            if (lower.Contains("flame") || lower.Contains("fire")) return flameSprite != null ? flameSprite : defaultWeaponSprite;
            if (lower.Contains("freeze") || lower.Contains("cryo") || lower.Contains("ice")) return cryoSprite != null ? cryoSprite : defaultWeaponSprite;
            if (lower.Contains("void") || lower.Contains("gravity") || lower.Contains("blackhole")) return voidSprite != null ? voidSprite : defaultWeaponSprite;
            if (lower.Contains("bounce") || lower.Contains("trick") || lower.Contains("laser")) return bounceSprite != null ? bounceSprite : defaultWeaponSprite;
            if (lower.Contains("sniper")) return sniperSprite != null ? sniperSprite : defaultWeaponSprite;
            if (lower.Contains("knife") || lower.Contains("sword") || lower.Contains("melee") || lower.Contains("axe")) return meleeSprite != null ? meleeSprite : defaultWeaponSprite;
            if (lower.Contains("blaster") || lower.Contains("area") || lower.Contains("rocket") || lower.Contains("blast")) return blasterSprite != null ? blasterSprite : (rifleSprite != null ? rifleSprite : defaultWeaponSprite);
            if (lower.Contains("grenade") || lower.Contains("explosive") || lower.Contains("bomb")) return grenadeSprite != null ? grenadeSprite : defaultWeaponSprite;
            if (lower.Contains("mine") || lower.Contains("trip") || lower.Contains("trap")) return mineSprite != null ? mineSprite : defaultWeaponSprite;
            if (lower.Contains("smoke") || lower.Contains("canister") || lower.Contains("gas")) return smokeSprite != null ? smokeSprite : defaultWeaponSprite;
            if (lower.Contains("pistol") || lower.Contains("sidearm")) return pistolSprite != null ? pistolSprite : defaultWeaponSprite;
            if (lower.Contains("machine") || lower.Contains("rifle") || lower.Contains("gun") || lower.Contains("shooter") || lower.Contains("ar-15")) return rifleSprite != null ? rifleSprite : defaultWeaponSprite;
            return defaultWeaponSprite != null ? defaultWeaponSprite : pistolSprite;
        }

        private void OnKillFeedEvent(string killerName, string victimName, string weaponName, ulong killerId, ulong victimId)
        {
            ulong localClientId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 9999;
            bool isLocalKiller = (killerId == localClientId);
            bool isLocalVictim = (victimId == localClientId);

            var activeMode = NetworkedGameManager.Instance != null ? NetworkedGameManager.Instance.ActiveGameMode : GameModeBase.Instance;
            if (activeMode == null)
            {
                var ffaFallback = FindAnyObjectByType<FFAMode>();
                var tdmFallback = FindAnyObjectByType<TDMMode>();
                if (ffaFallback != null && ffaFallback.enabled) activeMode = ffaFallback;
                else if (tdmFallback != null && tdmFallback.enabled) activeMode = tdmFallback;
                else activeMode = (GameModeBase)ffaFallback ?? (GameModeBase)tdmFallback;
            }
            bool isKillerFriendly = false;
            bool isVictimFriendly = false;

            if (activeMode is TDMMode tdm)
            {
                int localTeam = tdm.GetPlayerTeam(localClientId);
                int killerTeam = tdm.GetPlayerTeam(killerId);
                int victimTeam = tdm.GetPlayerTeam(victimId);
                if (localTeam >= 0)
                {
                    isKillerFriendly = (killerTeam == localTeam);
                    isVictimFriendly = (victimTeam == localTeam);
                }
            }

            // Trigger local player banners
            if (isLocalKiller && !isLocalVictim)
            {
                ShowEliminationBanner(victimName, weaponName);
            }
            else if (isLocalVictim)
            {
                string displayKiller = (killerId == victimId) ? (weaponName == "Lava" ? "Lava" : "Self") : killerName;
                ShowDeathBanner(displayKiller, weaponName);
            }

            // Spawn Kill Feed Item with icon & team identification
            Sprite icon = GetWeaponSprite(weaponName);
            AddKillFeedEntry(killerName, victimName, weaponName, icon, isLocalKiller, isLocalVictim, isKillerFriendly, isVictimFriendly);
        }

        public void AddKillFeedEntry(string killer, string victim, string weapon, bool isLocalKiller, bool isLocalVictim)
        {
            Sprite icon = GetWeaponSprite(weapon);
            AddKillFeedEntry(killer, victim, weapon, icon, isLocalKiller, isLocalVictim, false, false);
        }

        public void AddKillFeedEntry(string killer, string victim, string weapon, Sprite weaponIcon, bool isLocalKiller, bool isLocalVictim, bool isKillerFriendly = false, bool isVictimFriendly = false)
        {
            if (killFeedContainer == null) return;

            // Enforce max active entries
            if (killFeedContainer.childCount >= 6)
            {
                var oldest = killFeedContainer.GetChild(0);
                if (oldest != null) Destroy(oldest.gameObject);
            }

            GameObject entryObj = null;
            if (killFeedEntryPrefab != null)
            {
                entryObj = Instantiate(killFeedEntryPrefab, killFeedContainer);
            }
            else
            {
                entryObj = new GameObject("KillEntry");
                entryObj.transform.SetParent(killFeedContainer, false);
                var txt = entryObj.AddComponent<TextMeshProUGUI>();
                txt.fontSize = 15;
                txt.color = Color.white;
            }

            var kfItem = entryObj.GetComponent<KillFeedItem>();
            if (kfItem != null)
            {
                kfItem.Initialize(killer, victim, weapon, weaponIcon, isLocalKiller, isLocalVictim, isKillerFriendly, isVictimFriendly);
            }
            else
            {
                var label = entryObj.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                {
                    string wpnStr = !string.IsNullOrEmpty(weapon) ? $" [{weapon}] " : " ➔ ";
                    label.text = $"<color=#00F0FF>{killer}</color>{wpnStr}<color=#FF4D4D>{victim}</color>";
                }
                StartCoroutine(FadeAndDestroyKillEntry(entryObj, 4.0f));
            }
        }

        public void ClearKillFeed()
        {
            if (killFeedContainer == null) return;
            for (int i = killFeedContainer.childCount - 1; i >= 0; i--)
            {
                var child = killFeedContainer.GetChild(i);
                if (child != null) Destroy(child.gameObject);
            }
        }

        public void AddSystemFeedEntry(string message)
        {
            if (killFeedContainer == null) return;

            var entryObj = new GameObject("SystemEntry");
            entryObj.transform.SetParent(killFeedContainer, false);
            var txt = entryObj.AddComponent<TextMeshProUGUI>();
            txt.fontSize = 14;
            txt.color = new Color(0.6f, 0.9f, 1f, 0.9f);
            txt.text = $"• {message}";

            StartCoroutine(FadeAndDestroyKillEntry(entryObj, 4.0f));
        }

        private IEnumerator FadeAndDestroyKillEntry(GameObject entry, float lifetime)
        {
            yield return new WaitForSeconds(lifetime);
            if (entry == null) yield break;
            var cg = entry.GetComponent<CanvasGroup>();
            if (cg == null) cg = entry.AddComponent<CanvasGroup>();

            float fadeDuration = 0.5f;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                if (entry == null) yield break;
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                yield return null;
            }

            if (entry != null) Destroy(entry);
        }
    }
}
