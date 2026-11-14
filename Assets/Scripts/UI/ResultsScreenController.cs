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
    public partial class ResultsScreenController : MonoBehaviour
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

        [Header("Styling Sprites")]
        [SerializeField] private Sprite cyberFrameSprite;
        [SerializeField] private Sprite goldButtonSprite;
        [SerializeField] private Sprite blueButtonSprite;

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

        private void ShowResults(string winnerInfo, PlayerScoreEntry[] scores)
        {
            if (resultsRoot != null)
            {
                resultsRoot.SetActive(false);
            }

            string localName = PlayerProfileManager.Instance != null ? PlayerProfileManager.Instance.DisplayName : "";
            bool isWinner = !string.IsNullOrEmpty(localName) && winnerInfo.IndexOf(localName, System.StringComparison.OrdinalIgnoreCase) >= 0;

            AudioClip vClip = victoryClip != null ? victoryClip : (AudioManager.Instance != null ? AudioManager.Instance.VictoryClip : null);
            AudioClip dClip = defeatClip != null ? defeatClip : (AudioManager.Instance != null ? AudioManager.Instance.DefeatClip : null);

            if (vClip != null && isWinner && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX2D(vClip, 1.0f);
            }
            else if (dClip != null && !isWinner && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX2D(dClip, 1.0f);
            }

            // Route directly to unified WaitingRoom GameOver Modal
            WaitingRoomController.ShowGameOver(winnerInfo, scores);
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
                string pName = PlayerNameRegistry.GetLastKnownName(entry.ClientId);
                if (string.IsNullOrEmpty(pName) || pName == $"Player {entry.ClientId}" || pName == $"Agent {entry.ClientId}")
                {
                    pName = isLocalPlayer && PlayerProfileManager.Instance != null ? PlayerProfileManager.Instance.DisplayName : "Player";
                }
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
    }
}
