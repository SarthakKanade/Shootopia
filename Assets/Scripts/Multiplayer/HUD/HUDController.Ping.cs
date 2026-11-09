using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

namespace Shootopia
{
    public partial class HUDController
    {
public void EnsurePingBadgeUI()
        {
            if (pingBadgeRoot != null && pingText != null) return;

            Transform existing = transform.Find("PingBadgeRoot");
            if (existing != null)
            {
                pingBadgeRoot = existing.gameObject;
                pingText = existing.GetComponentInChildren<TextMeshProUGUI>();
                var dotTr = existing.Find("PingDot");
                if (dotTr != null) pingIndicatorDot = dotTr.GetComponent<Image>();
                return;
            }

            var badgeObj = new GameObject("PingBadgeRoot", typeof(RectTransform), typeof(Image));
            badgeObj.transform.SetParent(transform, false);

            var rt = badgeObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(105f, 28f);
            rt.anchoredPosition = new Vector2(-20f, -16f);

            var bgImg = badgeObj.GetComponent<Image>();
            bgImg.color = new Color(0.04f, 0.08f, 0.16f, 0.85f);

            // Subtle neon border frame or indicator
            var dotObj = new GameObject("PingDot", typeof(RectTransform), typeof(Image));
            dotObj.transform.SetParent(badgeObj.transform, false);

            var dRt = dotObj.GetComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0f, 0.5f);
            dRt.anchorMax = new Vector2(0f, 0.5f);
            dRt.pivot = new Vector2(0.5f, 0.5f);
            dRt.sizeDelta = new Vector2(8f, 8f);
            dRt.anchoredPosition = new Vector2(14f, 0f);

            pingIndicatorDot = dotObj.GetComponent<Image>();
            pingIndicatorDot.color = new Color(0.15f, 0.95f, 0.75f, 1f);

            // Text
            var textObj = new GameObject("PingText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(badgeObj.transform, false);

            var tRt = textObj.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = new Vector2(24f, 2f);
            tRt.offsetMax = new Vector2(-6f, -2f);

            pingText = textObj.GetComponent<TextMeshProUGUI>();
            pingText.text = "-- ms";
            pingText.fontSize = 13;
            pingText.fontStyle = FontStyles.Bold;
            pingText.alignment = TextAlignmentOptions.MidlineLeft;
            pingText.color = new Color(0.85f, 0.95f, 1f, 1f);
            if (timerText != null) pingText.font = timerText.font;

            pingBadgeRoot = badgeObj;
        }

        private float _fpsAccumulator = 60f;

        private void UpdatePingBadge()
        {
            float currentFps = 1f / Mathf.Max(0.0001f, Time.unscaledDeltaTime);
            _fpsAccumulator = Mathf.Lerp(_fpsAccumulator, currentFps, 0.08f);

            if (Time.unscaledTime - _lastPingUpdateTime < 0.25f) return;
            _lastPingUpdateTime = Time.unscaledTime;

            if (pingText == null) return;

            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            {
                if (pingBadgeRoot != null && pingBadgeRoot.activeSelf) pingBadgeRoot.SetActive(false);
                return;
            }

            if (pingBadgeRoot != null && !pingBadgeRoot.activeSelf) pingBadgeRoot.SetActive(true);

            int fps = Mathf.Clamp(Mathf.RoundToInt(_fpsAccumulator), 1, 999);
            bool isHost = NetworkManager.Singleton.IsHost;
            int ping = NetworkPingUtility.GetCurrentPing(NetworkManager.Singleton.LocalClientId);
            Color pingColor = NetworkPingUtility.GetPingColor(ping);

            string pingFormatted = NetworkPingUtility.FormatPingString(ping, isHost);
            pingText.text = $"{fps} FPS  •  {pingFormatted}";
            pingText.color = pingColor;

            if (pingIndicatorDot != null)
            {
                pingIndicatorDot.color = pingColor;
            }
        }
    }
}
