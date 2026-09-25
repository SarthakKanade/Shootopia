using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

namespace Shootopia
{
    /// <summary>
    /// Master UGUI Controller for the Match Results (Victory / Defeat) screen.
    /// Displays match outcome, winner announcement, final player leaderboard,
    /// and Return to Lobby navigation styled with WaitingRoom sci-fi aesthetic.
    /// </summary>
    public class ResultsScreenController : MonoBehaviour
    {
        [Header("Results Screen UI")]
        [SerializeField] private GameObject resultsRoot;
        [SerializeField] private CanvasGroup resultsCanvasGroup;
        [SerializeField] private TextMeshProUGUI outcomeBannerText;
        [SerializeField] private TextMeshProUGUI winnerInfoText;
        [SerializeField] private Transform scoreTableContent;
        [SerializeField] private GameObject scoreRowPrefab;
        [SerializeField] private Button btnReturnToLobby;
        [SerializeField] private Button btnMainMenu;

        [Header("Audio Feedback")]
        [SerializeField] private AudioClip victoryClip;
        [SerializeField] private AudioClip defeatClip;

        // Visual Styling Constants matching WaitingRoomModal
        private static readonly Color ColorDarkBackdrop = new Color(0.04f, 0.07f, 0.14f, 0.96f);
        private static readonly Color ColorFrameCyan = new Color(0.85f, 0.92f, 1f, 1f);
        private static readonly Color ColorHeaderBg = new Color(0.06f, 0.08f, 0.16f, 0.96f);
        private static readonly Color ColorHeaderText = new Color(0.85f, 0.95f, 1f, 1f);
        private static readonly Color ColorGold = new Color(1f, 0.85f, 0.2f, 1f);
        private static readonly Color ColorSilver = new Color(0.82f, 0.86f, 0.92f, 1f);
        private static readonly Color ColorBronze = new Color(0.85f, 0.58f, 0.35f, 1f);
        private static readonly Color ColorWhite = Color.white;
        private static readonly Color ColorLocalHighlight = new Color(0.18f, 0.72f, 1.0f, 0.35f);

        private const string SPRITE_CYBER_FRAME = "Assets/3rdParty/Space_Exploration_GUI_Kit/Containers/Large/setting-container-large.png";
        private const string SPRITE_GOLD_BTN = "Assets/3rdParty/Space_Exploration_GUI_Kit/Button_Images/Source_Image_Sprites/large/large-yellow-sparkle-large.png";
        private const string SPRITE_BLUE_BTN = "Assets/3rdParty/Space_Exploration_GUI_Kit/Button_Images/Source_Image_Sprites/large/medium-blue-large.png";

        private bool _isActionInProgress = false;
        private bool _isStyled = false;

        private void Awake()
        {
            ApplyWaitingRoomStyling();
        }

        private void OnEnable()
        {
            GameModeBase.OnMatchEndedWithScoreboard += ShowResults;

            if (btnReturnToLobby != null)
            {
                btnReturnToLobby.onClick.RemoveAllListeners();
                btnReturnToLobby.onClick.AddListener(OnReturnToLobbyClicked);
            }
            if (btnMainMenu != null)
            {
                btnMainMenu.onClick.RemoveAllListeners();
                btnMainMenu.onClick.AddListener(OnMainMenuClicked);
            }

            if (resultsRoot != null) resultsRoot.SetActive(false);
            _isActionInProgress = false;
        }

        private void OnDisable()
        {
            GameModeBase.OnMatchEndedWithScoreboard -= ShowResults;

            if (btnReturnToLobby != null) btnReturnToLobby.onClick.RemoveAllListeners();
            if (btnMainMenu != null) btnMainMenu.onClick.RemoveAllListeners();
        }

        private void ApplyWaitingRoomStyling()
        {
            if (_isStyled || resultsRoot == null) return;
            _isStyled = true;

            var rootRt = resultsRoot.GetComponent<RectTransform>();
            if (rootRt != null)
            {
                rootRt.sizeDelta = new Vector2(1000f, 680f);
            }

            // Outer border image
            var rootImg = resultsRoot.GetComponent<Image>();
            if (rootImg != null)
            {
                rootImg.color = ColorFrameCyan;
                rootImg.type = Image.Type.Sliced;
#if UNITY_EDITOR
                var frameSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_CYBER_FRAME);
                if (frameSprite != null) rootImg.sprite = frameSprite;
#endif
            }

            // Dark inner backdrop
            Transform backdropTr = resultsRoot.transform.Find("ModalBackdrop");
            if (backdropTr == null)
            {
                var backdropObj = new GameObject("ModalBackdrop", typeof(RectTransform), typeof(Image));
                backdropObj.transform.SetParent(resultsRoot.transform, false);
                backdropObj.transform.SetSiblingIndex(0); // keep at back
                backdropTr = backdropObj.transform;

                var bRt = backdropObj.GetComponent<RectTransform>();
                bRt.anchorMin = Vector2.zero;
                bRt.anchorMax = Vector2.one;
                bRt.sizeDelta = new Vector2(-16f, -16f);
                bRt.anchoredPosition = Vector2.zero;

                var bImg = backdropObj.GetComponent<Image>();
                bImg.color = ColorDarkBackdrop;
                bImg.raycastTarget = false;
            }

            // Header titles positioning
            if (outcomeBannerText != null)
            {
                var bannerRt = outcomeBannerText.GetComponent<RectTransform>();
                bannerRt.anchoredPosition = new Vector2(0f, 265f);
                outcomeBannerText.fontSize = 38;
            }

            if (winnerInfoText != null)
            {
                var winnerRt = winnerInfoText.GetComponent<RectTransform>();
                winnerRt.anchoredPosition = new Vector2(0f, 220f);
                winnerInfoText.fontSize = 20;
            }

            // Setup Column Header Row above scroll area
            Transform headerBarTr = resultsRoot.transform.Find("ScoreTableHeader");
            if (headerBarTr == null)
            {
                var headerObj = new GameObject("ScoreTableHeader", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
                headerObj.transform.SetParent(resultsRoot.transform, false);
                headerBarTr = headerObj.transform;

                var hRt = headerObj.GetComponent<RectTransform>();
                hRt.anchorMin = new Vector2(0.5f, 0.5f);
                hRt.anchorMax = new Vector2(0.5f, 0.5f);
                hRt.sizeDelta = new Vector2(880f, 36f);
                hRt.anchoredPosition = new Vector2(0f, 175f);

                var hImg = headerObj.GetComponent<Image>();
                hImg.color = ColorHeaderBg;

                var hlg = headerObj.GetComponent<HorizontalLayoutGroup>();
                hlg.padding = new RectOffset(24, 24, 4, 4);
                hlg.childAlignment = TextAnchor.MiddleLeft;
                hlg.childControlWidth = false;
                hlg.childControlHeight = true;
                hlg.spacing = 10;

                CreateHeaderCol("RankHeader", headerBarTr, "RANK", 70f, TextAlignmentOptions.MidlineLeft);
                CreateHeaderCol("PlayerHeader", headerBarTr, "PLAYER", 360f, TextAlignmentOptions.MidlineLeft);
                CreateHeaderCol("KDHeader", headerBarTr, "KILLS / DEATHS", 220f, TextAlignmentOptions.Center);
                CreateHeaderCol("ScoreHeader", headerBarTr, "SCORE", 160f, TextAlignmentOptions.MidlineRight);
            }

            // Adjust Scroll Area position and size
            Transform scrollTr = resultsRoot.transform.Find("ResultsScrollView");
            if (scrollTr != null)
            {
                var sRt = scrollTr.GetComponent<RectTransform>();
                sRt.sizeDelta = new Vector2(880f, 340f);
                sRt.anchoredPosition = new Vector2(0f, -20f);
            }

            // Style Buttons with WaitingRoom appearance and Juice
            StyleActionButton(btnReturnToLobby, SPRITE_GOLD_BTN, new Vector2(260f, 60f), new Vector2(-155f, -250f), "RETURN TO LOBBY", new Color(0.12f, 0.06f, 0f, 1f));
            StyleActionButton(btnMainMenu, SPRITE_BLUE_BTN, new Vector2(240f, 60f), new Vector2(165f, -250f), "MAIN MENU", Color.white);
        }

        private void CreateHeaderCol(string name, Transform parent, string text, float width, TextAlignmentOptions align)
        {
            var colObj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            colObj.transform.SetParent(parent, false);

            var rt = colObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, 28f);

            var tmp = colObj.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 14;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = ColorHeaderText;
            tmp.alignment = align;
            if (outcomeBannerText != null) tmp.font = outcomeBannerText.font;
        }

        private void StyleActionButton(Button btn, string spritePath, Vector2 size, Vector2 pos, string labelText, Color textColor)
        {
            if (btn == null) return;

            var rt = btn.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = size;
                rt.anchoredPosition = pos;
            }

            var img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.type = Image.Type.Sliced;
#if UNITY_EDITOR
                var spr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (spr != null) img.sprite = spr;
#endif
            }

            var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                txt.text = labelText;
                txt.fontSize = 18;
                txt.color = textColor;
                txt.fontStyle = FontStyles.Bold;
                txt.alignment = TextAlignmentOptions.Center;
            }

            // Ensure UIJuice spring component is attached for snappy tactile feedback
            if (btn.GetComponent<UIJuice>() == null)
            {
                btn.gameObject.AddComponent<UIJuice>();
            }
        }

        private void ShowResults(string winnerInfo, PlayerScoreEntry[] scores)
        {
            CursorManager.PushUnlock("ResultsScreen");

            if (resultsRoot != null)
            {
                ApplyWaitingRoomStyling();
                resultsRoot.SetActive(true);
                UIJuice.PopIn(resultsRoot.GetComponent<RectTransform>(), resultsCanvasGroup);
            }

            string localName = PlayerProfileManager.Instance != null ? PlayerProfileManager.Instance.DisplayName : "";
            bool isWinner = !string.IsNullOrEmpty(localName) && winnerInfo.IndexOf(localName, System.StringComparison.OrdinalIgnoreCase) >= 0;

            if (outcomeBannerText != null)
            {
                outcomeBannerText.text = isWinner ? "VICTORY!" : "MATCH FINISHED";
                outcomeBannerText.color = isWinner ? ColorGold : new Color(0.95f, 0.35f, 0.35f, 1f);
            }

            if (winnerInfoText != null)
            {
                winnerInfoText.text = winnerInfo.ToUpper();
                winnerInfoText.color = ColorGold;
            }

            if (victoryClip != null && isWinner && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX2D(victoryClip, 1.0f);
            }
            else if (defeatClip != null && !isWinner && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX2D(defeatClip, 1.0f);
            }

            PopulateFinalScoreboard(scores);
        }

        private void PopulateFinalScoreboard(PlayerScoreEntry[] scores)
        {
            if (scoreTableContent == null) return;

            foreach (Transform child in scoreTableContent)
            {
                Destroy(child.gameObject);
            }

            if (scores == null || scores.Length == 0) return;

            // Sort scores descending by kills, then ascending by deaths
            var list = new List<PlayerScoreEntry>(scores);
            list.Sort((a, b) => b.Kills != a.Kills ? b.Kills.CompareTo(a.Kills) : a.Deaths.CompareTo(b.Deaths));

            ulong localClientId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : ulong.MaxValue;

            for (int i = 0; i < list.Count; i++)
            {
                var entry = list[i];
                var rowObj = scoreRowPrefab != null ? Instantiate(scoreRowPrefab, scoreTableContent) : new GameObject("Row");
                var rowRt = rowObj.GetComponent<RectTransform>();
                if (rowRt != null)
                {
                    rowRt.sizeDelta = new Vector2(860f, 46f);
                }

                bool isLocalPlayer = (entry.ClientId == localClientId);

                // Row background styling
                var rowImg = rowObj.GetComponent<Image>();
                if (rowImg != null)
                {
                    if (isLocalPlayer)
                    {
                        rowImg.color = ColorLocalHighlight;
                    }
                    else
                    {
                        rowImg.color = new Color(0.07f, 0.10f, 0.20f, 0.85f);
                    }
                }

                // Rank color distinction
                Color rankColor;
                switch (i)
                {
                    case 0: rankColor = ColorGold; break;
                    case 1: rankColor = ColorSilver; break;
                    case 2: rankColor = ColorBronze; break;
                    default: rankColor = ColorWhite; break;
                }

                var texts = rowObj.GetComponentsInChildren<TextMeshProUGUI>();
                string pName = PlayerNameRegistry.GetName(entry.ClientId);
                if (string.IsNullOrEmpty(pName)) pName = $"Agent {entry.ClientId}";
                if (isLocalPlayer) pName += " <color=#FFD555>(YOU)</color>";

                int score = entry.Kills * 100 - entry.Deaths * 25;
                if (score < 0) score = 0;

                if (texts.Length >= 4)
                {
                    texts[0].text = $"#{i + 1}";
                    texts[0].color = rankColor;
                    texts[0].fontStyle = FontStyles.Bold;

                    texts[1].text = pName;
                    texts[1].color = isLocalPlayer ? ColorGold : ColorWhite;

                    texts[2].text = $"{entry.Kills} KILLS / {entry.Deaths} DEATHS";
                    texts[2].color = new Color(0.80f, 0.88f, 0.98f);

                    texts[3].text = $"{score} PTS";
                    texts[3].color = new Color(0.35f, 0.95f, 0.65f);
                    texts[3].fontStyle = FontStyles.Bold;
                }
            }
        }

        private void OnReturnToLobbyClicked()
        {
            if (_isActionInProgress) return;
            _isActionInProgress = true;

            CursorManager.PopUnlock("ResultsScreen");
            if (btnReturnToLobby != null) btnReturnToLobby.interactable = false;
            if (btnMainMenu != null) btnMainMenu.interactable = false;

            LoadingScreenManager.ShowOverlay("RETURNING TO WAITING ROOM...");

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.SceneManager.LoadScene("WaitingRoom", LoadSceneMode.Single);
            }
            else
            {
                LoadingScreenManager.LoadScene("WaitingRoom", "RETURNING TO WAITING ROOM...");
            }
        }

        private async void OnMainMenuClicked()
        {
            if (_isActionInProgress) return;
            _isActionInProgress = true;

            CursorManager.PopUnlock("ResultsScreen");
            if (btnReturnToLobby != null) btnReturnToLobby.interactable = false;
            if (btnMainMenu != null) btnMainMenu.interactable = false;

            LoadingScreenManager.ShowOverlay("RETURNING TO MAIN MENU...");

            try
            {
                if (SessionManager.Instance != null)
                {
                    // LeaveSession handles session cleanup, NGO shutdown, and loading MainMenu via LoadingScreenManager
                    await SessionManager.Instance.LeaveSession();
                }
                else
                {
                    if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                    {
                        NetworkManager.Singleton.Shutdown();
                        while (NetworkManager.Singleton != null && NetworkManager.Singleton.ShutdownInProgress)
                        {
                            await Task.Yield();
                        }
                    }

                    LoadingScreenManager.LoadScene("MainMenu", "RETURNING TO MAIN MENU...");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ResultsScreenController] MainMenu return note: {ex.Message}");
                LoadingScreenManager.LoadScene("MainMenu", "RETURNING TO MAIN MENU...");
            }
        }
    }
}
