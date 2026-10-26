using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

namespace Shootopia
{
    /// <summary>
    /// Master UGUI Tactical Scoreboard overlay active during gameplay when the Tab key is held.
    /// Features high-tech cybernetic styling, match overview header, column labels,
    /// dynamic player ranking with K/D and score metrics, and local player highlighting.
    /// </summary>
    public class ScoreboardController : MonoBehaviour
    {
        [Header("Scoreboard Panel")]
        [SerializeField] private GameObject scoreboardRoot;
        [SerializeField] private CanvasGroup scoreboardCanvasGroup;
        [SerializeField] private TextMeshProUGUI modeTitleText;
        [SerializeField] private TextMeshProUGUI subTitleText;
        [SerializeField] private Transform rowsContainer;
        [SerializeField] private GameObject rowPrefab;

        public static ScoreboardController Instance { get; private set; }

        private bool _isShowing = false;
        private bool _lastKeyboardTab = false;
        private bool _isTacticalLayoutBuilt = false;

        private void Awake()
        {
            Instance = this;
            if (scoreboardRoot == null)
            {
                var found = transform.Find("ScoreboardRoot");
                if (found == null && transform.parent != null)
                {
                    found = transform.parent.Find("ScoreboardRoot");
                }
                if (found == null)
                {
                    var go = GameObject.Find("ScoreboardRoot");
                    if (go != null) found = go.transform;
                }
                if (found != null)
                {
                    scoreboardRoot = found.gameObject;
                }
            }

            if (scoreboardRoot != null && scoreboardCanvasGroup == null)
            {
                scoreboardCanvasGroup = scoreboardRoot.GetComponent<CanvasGroup>();
            }

            ApplyTacticalScoreboardStyling();
            ClampMenuScales();
        }

        public void ToggleScoreboard()
        {
            _isShowing = !_isShowing;
            ToggleScoreboard(_isShowing);
        }

        private void Update()
        {
            // Do not show scoreboard during Warmup lobby
            if (WaitingRoomController.IsLobbyMenuOpen || (NetworkedGameManager.Instance != null && NetworkedGameManager.Instance.ActiveGameMode != null && !NetworkedGameManager.Instance.ActiveGameMode.MatchActive.Value))
            {
                if (_isShowing)
                {
                    _isShowing = false;
                    ToggleScoreboard(false);
                }
                return;
            }

            bool tabHeld = false;
            if (Keyboard.current != null)
            {
                tabHeld = Keyboard.current.tabKey.isPressed;
            }
            if (!tabHeld && Gamepad.current != null)
            {
                tabHeld = Gamepad.current.selectButton.isPressed;
            }

            if (tabHeld != _lastKeyboardTab)
            {
                _lastKeyboardTab = tabHeld;
                _isShowing = tabHeld;
                ToggleScoreboard(_isShowing);
            }

            if (_isShowing)
            {
                RefreshScoreboardData();
            }
        }

        private void ToggleScoreboard(bool show)
        {
            if (scoreboardRoot == null) return;

            if (show)
            {
                ApplyTacticalScoreboardStyling();
                ClampMenuScales();
                scoreboardRoot.SetActive(true);
                if (scoreboardCanvasGroup != null)
                {
                    scoreboardCanvasGroup.alpha = 1f;
                }
                RefreshScoreboardData();
            }
            else
            {
                scoreboardRoot.SetActive(false);
                if (scoreboardCanvasGroup != null)
                {
                    scoreboardCanvasGroup.alpha = 0f;
                }
            }
        }

        public void ClampMenuScales()
        {
            if (scoreboardRoot == null) return;

            var rootRt = scoreboardRoot.GetComponent<RectTransform>();
            if (rootRt == null) return;

            rootRt.anchoredPosition = Vector2.zero;
            var canvas = scoreboardRoot.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                var cRt = canvas.GetComponent<RectTransform>();
                if (cRt != null && cRt.rect.height > 0 && cRt.rect.width > 0)
                {
                    float s = Mathf.Min(1f, (cRt.rect.height * 0.94f) / 640f, (cRt.rect.width * 0.94f) / 1000f);
                    rootRt.localScale = new Vector3(s, s, 1f);
                }
            }
        }

        private void ApplyTacticalScoreboardStyling()
        {
            if (_isTacticalLayoutBuilt || scoreboardRoot == null) return;
            _isTacticalLayoutBuilt = true;

            var rootRt = scoreboardRoot.GetComponent<RectTransform>();
            if (rootRt != null)
            {
                rootRt.sizeDelta = new Vector2(1000f, 620f);
                rootRt.anchoredPosition = Vector2.zero;
            }

            var rootImg = scoreboardRoot.GetComponent<Image>();
            if (rootImg != null)
            {
                rootImg.color = new Color(0.18f, 0.72f, 1.0f, 0.45f);
            }

            // Dark inner backdrop matching WaitingRoom / Results screen
            Transform backdropTr = scoreboardRoot.transform.Find("ModalBackdrop");
            if (backdropTr == null)
            {
                var backdropObj = new GameObject("ModalBackdrop", typeof(RectTransform), typeof(Image));
                backdropObj.transform.SetParent(scoreboardRoot.transform, false);
                backdropObj.transform.SetSiblingIndex(0);

                var bRt = backdropObj.GetComponent<RectTransform>();
                bRt.anchorMin = Vector2.zero;
                bRt.anchorMax = Vector2.one;
                bRt.sizeDelta = new Vector2(-16f, -16f);
                bRt.anchoredPosition = Vector2.zero;

                var bImg = backdropObj.GetComponent<Image>();
                bImg.color = new Color(0.04f, 0.07f, 0.14f, 0.96f);
                bImg.raycastTarget = false;
            }

            // Reposition / Style Title
            Transform titleTr = scoreboardRoot.transform.Find("Title");
            if (titleTr != null)
            {
                var tRt = titleTr.GetComponent<RectTransform>();
                if (tRt != null)
                {
                    tRt.anchorMin = new Vector2(0.5f, 0.5f);
                    tRt.anchorMax = new Vector2(0.5f, 0.5f);
                    tRt.sizeDelta = new Vector2(880f, 44f);
                    tRt.anchoredPosition = new Vector2(0f, 255f);
                }

                var tmp = titleTr.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = "TACTICAL SCOREBOARD";
                    tmp.fontSize = 32;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.color = new Color(0.92f, 0.97f, 1f, 1f);
                    tmp.alignment = TextAlignmentOptions.Center;
                    modeTitleText = tmp;
                }
            }

            // Subtitle
            Transform subTr = scoreboardRoot.transform.Find("ScoreboardSubtitle");
            if (subTr == null)
            {
                var subObj = new GameObject("ScoreboardSubtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
                subObj.transform.SetParent(scoreboardRoot.transform, false);

                var sRt = subObj.GetComponent<RectTransform>();
                sRt.anchorMin = new Vector2(0.5f, 0.5f);
                sRt.anchorMax = new Vector2(0.5f, 0.5f);
                sRt.sizeDelta = new Vector2(880f, 26f);
                sRt.anchoredPosition = new Vector2(0f, 218f);

                var sTmp = subObj.GetComponent<TextMeshProUGUI>();
                sTmp.text = "LIVE MATCH OVERVIEW • PLAYER LEADERBOARD";
                sTmp.fontSize = 15;
                sTmp.fontStyle = FontStyles.Normal;
                sTmp.color = new Color(0.6f, 0.78f, 0.95f, 1f);
                sTmp.alignment = TextAlignmentOptions.Center;
                if (modeTitleText != null) sTmp.font = modeTitleText.font;
                subTitleText = sTmp;
            }

            // Scoreboard Table Column Headers
            Transform headerBarTr = scoreboardRoot.transform.Find("ScoreTableHeader");
            if (headerBarTr == null)
            {
                var headerObj = new GameObject("ScoreTableHeader", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
                headerObj.transform.SetParent(scoreboardRoot.transform, false);

                var hRt = headerObj.GetComponent<RectTransform>();
                hRt.anchorMin = new Vector2(0.5f, 0.5f);
                hRt.anchorMax = new Vector2(0.5f, 0.5f);
                hRt.sizeDelta = new Vector2(880f, 36f);
                hRt.anchoredPosition = new Vector2(0f, 175f);

                var hImg = headerObj.GetComponent<Image>();
                hImg.color = new Color(0.06f, 0.08f, 0.16f, 0.96f);

                var hlg = headerObj.GetComponent<HorizontalLayoutGroup>();
                hlg.padding = new RectOffset(24, 24, 4, 4);
                hlg.childAlignment = TextAnchor.MiddleLeft;
                hlg.childControlWidth = false;
                hlg.childControlHeight = true;
                hlg.spacing = 10;

                CreateHeaderCol("RankHeader", headerObj.transform, "RANK", 65f, TextAlignmentOptions.MidlineLeft);
                CreateHeaderCol("PlayerHeader", headerObj.transform, "PLAYER", 320f, TextAlignmentOptions.MidlineLeft);
                CreateHeaderCol("KDHeader", headerObj.transform, "KILLS / DEATHS", 195f, TextAlignmentOptions.Center);
                CreateHeaderCol("ScoreHeader", headerObj.transform, "SCORE", 135f, TextAlignmentOptions.MidlineRight);
                CreateHeaderCol("PingHeader", headerObj.transform, "PING", 95f, TextAlignmentOptions.MidlineRight);
            }

            // Scroll View for live score rows
            Transform scrollTr = scoreboardRoot.transform.Find("ScoreScrollView");
            if (scrollTr != null)
            {
                var sRt = scrollTr.GetComponent<RectTransform>();
                if (sRt != null)
                {
                    sRt.anchorMin = new Vector2(0.5f, 0.5f);
                    sRt.anchorMax = new Vector2(0.5f, 0.5f);
                    sRt.sizeDelta = new Vector2(880f, 400f);
                    sRt.anchoredPosition = new Vector2(0f, -40f);
                }

                Transform viewport = scrollTr.Find("Viewport");
                if (viewport != null)
                {
                    Transform content = viewport.Find("Content");
                    if (content == null)
                    {
                        var contentObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
                        contentObj.transform.SetParent(viewport, false);
                        content = contentObj.transform;
                    }

                    var cRt = content.GetComponent<RectTransform>();
                    if (cRt != null)
                    {
                        cRt.anchorMin = new Vector2(0f, 1f);
                        cRt.anchorMax = new Vector2(1f, 1f);
                        cRt.pivot = new Vector2(0.5f, 1f);
                        cRt.sizeDelta = new Vector2(0f, 0f);
                    }

                    var vlg = content.GetComponent<VerticalLayoutGroup>();
                    if (vlg == null)
                    {
                        vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
                    }
                    if (vlg != null)
                    {
                        vlg.padding = new RectOffset(8, 8, 8, 8);
                        vlg.spacing = 6f;
                        vlg.childControlWidth = true;
                        vlg.childControlHeight = false;
                        vlg.childForceExpandWidth = true;
                        vlg.childForceExpandHeight = false;
                    }

                    var csf = content.GetComponent<ContentSizeFitter>();
                    if (csf == null)
                    {
                        csf = content.gameObject.AddComponent<ContentSizeFitter>();
                    }
                    if (csf != null)
                    {
                        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                    }

                    var sr = scrollTr.GetComponent<ScrollRect>();
                    if (sr != null)
                    {
                        sr.content = cRt;
                        sr.horizontal = false;
                        sr.vertical = true;
                    }

                    rowsContainer = content;
                }
            }

            // Hide old Content child if it was directly under scoreboardRoot
            Transform oldContent = scoreboardRoot.transform.Find("Content");
            if (oldContent != null && oldContent != rowsContainer)
            {
                oldContent.gameObject.SetActive(false);
            }
        }

        private void CreateHeaderCol(string name, Transform parent, string text, float width, TextAlignmentOptions align)
        {
            if (parent == null) return;
            var colObj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            colObj.transform.SetParent(parent, false);

            var rt = colObj.GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = new Vector2(width, 28f);

            var tmp = colObj.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = text;
                tmp.fontSize = 14;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = new Color(0.85f, 0.95f, 1f, 1f);
                tmp.alignment = align;
                if (modeTitleText != null) tmp.font = modeTitleText.font;
            }
        }

        private void RefreshScoreboardData()
        {
            if (rowsContainer == null) return;

            // Clear old rows
            foreach (Transform child in rowsContainer)
            {
                Destroy(child.gameObject);
            }

            var activeMode = NetworkedGameManager.Instance != null ? NetworkedGameManager.Instance.ActiveGameMode : GameModeBase.Instance;
            if (activeMode != null && subTitleText != null)
            {
                string modeStr = (activeMode is TDMMode) ? "TEAM DEATHMATCH" : "FREE FOR ALL";
                subTitleText.text = $"LIVE MATCH OVERVIEW • {modeStr}";
            }

            var playerEntries = new List<ScoreRowData>();
            ulong localClientId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0;

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.ConnectedClientsList != null)
            {
                foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    ulong id = client.ClientId;
                    string pName = PlayerNameRegistry.GetName(id);
                    int kills = (activeMode != null) ? activeMode.GetPlayerKills(id) : 0;
                    int deaths = (activeMode != null) ? activeMode.GetPlayerDeaths(id) : 0;
                    int score = kills * 100 - deaths * 25;
                    if (score < 0) score = 0;
                    int ping = NetworkPingUtility.GetCurrentPing(id);

                    playerEntries.Add(new ScoreRowData
                    {
                        ClientId = id,
                        PlayerName = pName,
                        Kills = kills,
                        Deaths = deaths,
                        Score = score,
                        Ping = ping,
                        IsLocal = (id == localClientId)
                    });
                }
            }

            // Sort descending by kills then score
            playerEntries.Sort((a, b) => b.Kills != a.Kills ? b.Kills.CompareTo(a.Kills) : b.Score.CompareTo(a.Score));

            if (rowPrefab == null)
            {
                rowPrefab = Resources.Load<GameObject>("ScoreboardRow");
            }

            for (int i = 0; i < playerEntries.Count; i++)
            {
                CreateScoreboardRow(i + 1, playerEntries[i]);
            }
        }

        private void CreateScoreboardRow(int rank, ScoreRowData entry)
        {
            if (rowsContainer == null) return;
            bool isHost = entry.IsLocal && NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;

            if (rowPrefab != null)
            {
                var rowObj = Instantiate(rowPrefab, rowsContainer);
                var texts = rowObj.GetComponentsInChildren<TextMeshProUGUI>();
                var bgImg = rowObj.GetComponent<Image>();

                if (texts.Length >= 4)
                {
                    texts[0].text = $"#{rank}";
                    texts[1].text = entry.PlayerName;
                    texts[2].text = $"{entry.Kills} K / {entry.Deaths} D";
                    texts[3].text = $"{entry.Score} PTS";
                }

                if (texts.Length >= 5)
                {
                    texts[4].text = NetworkPingUtility.FormatPingString(entry.Ping, isHost);
                    texts[4].color = NetworkPingUtility.GetPingColor(entry.Ping);
                }
                else
                {
                    CreatePingCol(rowObj.transform, entry.Ping, isHost, 95f);
                }

                if (entry.IsLocal && bgImg != null)
                {
                    bgImg.color = new Color(0.18f, 0.72f, 1.0f, 0.35f);
                }
            }
            else
            {
                // Resilient fallback row creation
                var rowObj = new GameObject($"Row_{rank}", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
                rowObj.transform.SetParent(rowsContainer, false);

                var rRt = rowObj.GetComponent<RectTransform>();
                rRt.sizeDelta = new Vector2(860f, 40f);

                var bgImg = rowObj.GetComponent<Image>();
                bgImg.color = entry.IsLocal ? new Color(0.18f, 0.72f, 1.0f, 0.35f) : new Color(0.06f, 0.09f, 0.18f, 0.8f);

                var hlg = rowObj.GetComponent<HorizontalLayoutGroup>();
                hlg.padding = new RectOffset(20, 20, 4, 4);
                hlg.childAlignment = TextAnchor.MiddleLeft;
                hlg.childControlWidth = false;
                hlg.childControlHeight = true;
                hlg.spacing = 10;

                CreateHeaderCol("Rank", rowObj.transform, $"#{rank}", 65f, TextAlignmentOptions.MidlineLeft);
                CreateHeaderCol("Player", rowObj.transform, entry.PlayerName, 320f, TextAlignmentOptions.MidlineLeft);
                CreateHeaderCol("KD", rowObj.transform, $"{entry.Kills} K / {entry.Deaths} D", 195f, TextAlignmentOptions.Center);
                CreateHeaderCol("Score", rowObj.transform, $"{entry.Score} PTS", 135f, TextAlignmentOptions.MidlineRight);
                CreatePingCol(rowObj.transform, entry.Ping, isHost, 95f);
            }
        }

        private void CreatePingCol(Transform parent, int ping, bool isHost, float width)
        {
            var colObj = new GameObject("Ping", typeof(RectTransform), typeof(TextMeshProUGUI));
            colObj.transform.SetParent(parent, false);

            var rt = colObj.GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = new Vector2(width, 28f);

            var tmp = colObj.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = NetworkPingUtility.FormatPingString(ping, isHost);
                tmp.fontSize = 14;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = NetworkPingUtility.GetPingColor(ping);
                tmp.alignment = TextAlignmentOptions.MidlineRight;
                if (modeTitleText != null) tmp.font = modeTitleText.font;
            }
        }

        private struct ScoreRowData
        {
            public ulong ClientId;
            public string PlayerName;
            public int Kills;
            public int Deaths;
            public int Score;
            public int Ping;
            public bool IsLocal;
        }
    }
}
