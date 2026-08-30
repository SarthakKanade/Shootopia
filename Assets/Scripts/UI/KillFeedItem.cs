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
        [SerializeField] private Color enemyPlayerColor = new Color(0.95f, 0.35f, 0.35f, 1.0f); // Light Crimson #F25A5A
        [SerializeField] private Color defaultTextColor = new Color(0.90f, 0.93f, 0.98f, 1.0f); // Soft White
        [SerializeField] private Color localKillBgColor = new Color(0.0f, 0.45f, 0.65f, 0.75f);
        [SerializeField] private Color localDeathBgColor = new Color(0.65f, 0.15f, 0.15f, 0.75f);
        [SerializeField] private Color neutralBgColor = new Color(0.05f, 0.08f, 0.14f, 0.65f);

        private Coroutine _fadeRoutine;

        public void Initialize(string killerName, string victimName, string weaponName, Sprite weaponIcon, bool isLocalKiller, bool isLocalVictim)
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;

            // Set killer text
            if (killerText != null)
            {
                killerText.text = string.IsNullOrEmpty(killerName) ? "Unknown" : killerName;
                killerText.color = isLocalKiller ? localPlayerColor : (isLocalVictim ? enemyPlayerColor : defaultTextColor);
                if (isLocalKiller) killerText.fontStyle = FontStyles.Bold;
            }

            // Set victim text
            if (victimText != null)
            {
                victimText.text = string.IsNullOrEmpty(victimName) ? "Unknown" : victimName;
                victimText.color = isLocalVictim ? localPlayerColor : (isLocalKiller ? enemyPlayerColor : defaultTextColor);
                if (isLocalVictim) victimText.fontStyle = FontStyles.Bold;
            }

            // Set weapon icon or fallback text
            if (weaponIconImage != null)
            {
                if (weaponIcon != null)
                {
                    weaponIconImage.sprite = weaponIcon;
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
            // Pop in quickly
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
