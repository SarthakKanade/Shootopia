using System.Collections.Generic;
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
    /// and Return to Lobby navigation.
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

        private void OnEnable()
        {
            GameModeBase.OnMatchEndedWithScoreboard += ShowResults;

            if (btnReturnToLobby != null) btnReturnToLobby.onClick.AddListener(OnReturnToLobbyClicked);
            if (btnMainMenu != null) btnMainMenu.onClick.AddListener(OnMainMenuClicked);

            if (resultsRoot != null) resultsRoot.SetActive(false);
        }

        private void OnDisable()
        {
            GameModeBase.OnMatchEndedWithScoreboard -= ShowResults;

            if (btnReturnToLobby != null) btnReturnToLobby.onClick.RemoveAllListeners();
            if (btnMainMenu != null) btnMainMenu.onClick.RemoveAllListeners();
        }

        private void ShowResults(string winnerInfo, PlayerScoreEntry[] scores)
        {
            CursorManager.PushUnlock("ResultsScreen");

            if (resultsRoot != null)
            {
                UIJuice.PopIn(resultsRoot.GetComponent<RectTransform>(), resultsCanvasGroup);
            }

            string localName = PlayerProfileManager.Instance != null ? PlayerProfileManager.Instance.DisplayName : "";
            bool isWinner = !string.IsNullOrEmpty(localName) && winnerInfo.IndexOf(localName, System.StringComparison.OrdinalIgnoreCase) >= 0;

            if (outcomeBannerText != null)
            {
                outcomeBannerText.text = isWinner ? "VICTORY!" : "MATCH FINISHED";
                outcomeBannerText.color = isWinner ? new Color(1f, 0.85f, 0.2f) : new Color(0.9f, 0.3f, 0.3f);
            }

            if (winnerInfoText != null)
            {
                winnerInfoText.text = winnerInfo;
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

            // Sort scores descending by kills
            var list = new List<PlayerScoreEntry>(scores);
            list.Sort((a, b) => b.Kills != a.Kills ? b.Kills.CompareTo(a.Kills) : a.Deaths.CompareTo(b.Deaths));

            for (int i = 0; i < list.Count; i++)
            {
                var entry = list[i];
                var rowObj = scoreRowPrefab != null ? Instantiate(scoreRowPrefab, scoreTableContent) : new GameObject("Row");
                var texts = rowObj.GetComponentsInChildren<TextMeshProUGUI>();

                string pName = PlayerNameRegistry.GetName(entry.ClientId);
                int score = entry.Kills * 100 - entry.Deaths * 25;
                if (score < 0) score = 0;

                if (texts.Length >= 4)
                {
                    texts[0].text = $"#{i + 1}";
                    texts[1].text = pName;
                    texts[2].text = $"{entry.Kills} KILLS / {entry.Deaths} DEATHS";
                    texts[3].text = $"{score} PTS";
                }
            }
        }

        private void OnReturnToLobbyClicked()
        {
            LoadingScreenManager.ShowOverlay("RETURNING TO WAITING ROOM...");
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.SceneManager.LoadScene("WaitingRoom", LoadSceneMode.Single);
            }
            else
            {
                SceneManager.LoadScene("WaitingRoom");
            }
        }

        private async void OnMainMenuClicked()
        {
            LoadingScreenManager.ShowOverlay("RETURNING TO MAIN MENU...");

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                try { NetworkManager.Singleton.Shutdown(); } catch { }
            }

            if (SessionManager.Instance != null)
            {
                try { await SessionManager.Instance.LeaveSession(); } catch { }
            }

            SceneManager.LoadScene("MainMenu");
        }
    }
}
