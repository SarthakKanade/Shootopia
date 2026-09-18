using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Shootopia
{
    /// <summary>
    /// Component on each Kill Feed entry UI item.
    /// Animates into view, displays killer and victim names with weapon icon,
    /// highlights entries involving the local player, and smoothly fades out over time.
    /// </summary>
    public class KillFeedItem : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TextMeshProUGUI killerText;
        [SerializeField] private Image weaponIconImage;
        [SerializeField] private TextMeshProUGUI weaponFallbackText;
        [SerializeField] private TextMeshProUGUI victimText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Timing")]
        [SerializeField] private float displayDuration = 4.0f;
        [SerializeField] private float fadeOutDuration = 0.6f;

        [Header("Colors")]
        [SerializeField] private Color localPlayerColor = new Color(0.0f, 0.94f, 1.0f, 1.0f); // Bright Cyan #00F0FF
        [SerializeField] private Color friendlyPlayerColor = new Color(0.25f, 0.65f, 1.0f, 1.0f); // Allied Blue #40A6FF
        [SerializeField] private Color enemyPlayerColor = new Color(0.98f, 0.28f, 0.28f, 1.0f); // Light Crimson #FA4747
        [SerializeField] private Color defaultTextColor = new Color(0.90f, 0.93f, 0.98f, 1.0f); // Soft White
        [SerializeField] private Color localKillBgColor = new Color(0.04f, 0.22f, 0.30f, 0.88f);
        [SerializeField] private Color localDeathBgColor = new Color(0.32f, 0.08f, 0.08f, 0.88f);
        [SerializeField] private Color neutralBgColor = new Color(0.06f, 0.08f, 0.12f, 0.82f);

        private Coroutine _fadeRoutine;

        public void Initialize(string killerName, string victimName, string weaponName, Sprite weaponIcon, bool isLocalKiller, bool isLocalVictim, bool isKillerFriendly = false, bool isVictimFriendly = false)
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;

            // Set killer text
            if (killerText != null)
            {
                string kName = string.IsNullOrEmpty(killerName) ? "Unknown" : killerName;
                killerText.text = isLocalKiller ? $"<b>{kName}</b>" : kName;
                if (isLocalKiller)
                    killerText.color = localPlayerColor;
                else if (isKillerFriendly)
                    killerText.color = friendlyPlayerColor;
                else if (isVictimFriendly || isLocalVictim)
                    killerText.color = enemyPlayerColor;
                else
                    killerText.color = defaultTextColor;
            }

            // Set victim text
            if (victimText != null)
            {
                string vName = string.IsNullOrEmpty(victimName) ? "Unknown" : victimName;
                victimText.text = isLocalVictim ? $"<b>{vName}</b>" : vName;
                if (isLocalVictim)
                    victimText.color = enemyPlayerColor;
                else if (isVictimFriendly)
                    victimText.color = friendlyPlayerColor;
                else if (isLocalKiller || isKillerFriendly)
                    victimText.color = enemyPlayerColor;
                else
                    victimText.color = defaultTextColor;
            }

            // Set weapon icon or fallback text
            if (weaponIconImage != null)
            {
                if (weaponIcon != null)
                {
                    weaponIconImage.sprite = weaponIcon;
                    weaponIconImage.color = isLocalKiller ? new Color(1f, 0.9f, 0.35f, 1f) : Color.white;
                    weaponIconImage.gameObject.SetActive(true);
                    if (weaponFallbackText != null) weaponFallbackText.gameObject.SetActive(false);
                }
                else
                {
                    weaponIconImage.gameObject.SetActive(false);
                    if (weaponFallbackText != null)
                    {
                        weaponFallbackText.gameObject.SetActive(true);
                        weaponFallbackText.text = FormatWeaponName(weaponName);
                    }
                }
            }

            // Set background styling
            if (backgroundImage != null)
            {
                if (isLocalKiller)
                    backgroundImage.color = localKillBgColor;
                else if (isLocalVictim)
                    backgroundImage.color = localDeathBgColor;
                else
                    backgroundImage.color = neutralBgColor;
            }

            // Start life cycle
            if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
            _fadeRoutine = StartCoroutine(AnimateLifeCycle());
        }

        private string FormatWeaponName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "☠";
            if (raw.Contains("Knife") || raw.Contains("Sword")) return "🗡";
            if (raw.Contains("Punch")) return "👊";
            if (raw.Contains("Sniper")) return "🎯";
            if (raw.Contains("Flame")) return "🔥";
            if (raw.Contains("Freeze") || raw.Contains("Cryo")) return "❄";
            if (raw.Contains("Void")) return "🌌";
            if (raw.Contains("Grenade") || raw.Contains("Mine")) return "💣";
            if (raw.Contains("Bounce") || raw.Contains("Trick")) return "⚡";
            return $"[{raw.ToUpper()}]";
        }

        private IEnumerator AnimateLifeCycle()
        {
            // Pop in quickly with punch scale
            float elapsed = 0f;
            float popDuration = 0.15f;
            Vector3 startScale = new Vector3(0.85f, 0.85f, 1f);
            Vector3 endScale = Vector3.one;

            transform.localScale = startScale;

            while (elapsed < popDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / popDuration);
                canvasGroup.alpha = t;
                transform.localScale = Vector3.Lerp(startScale, endScale, t);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            transform.localScale = endScale;

            // Wait on screen
            yield return new WaitForSeconds(displayDuration);

            // Fade out
            elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOutDuration);
                canvasGroup.alpha = 1f - t;
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
