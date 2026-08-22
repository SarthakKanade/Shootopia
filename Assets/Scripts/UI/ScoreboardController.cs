using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

namespace Shootopia
{
    /// <summary>
    /// Live UGUI scoreboard overlay active during gameplay when the Tab key is held.
    /// Lists all connected players, ranks by score in FFA, or splits into teams in TDM.
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

        private bool _isShowing = false;

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
            else
            {
                tabHeld = Input.GetKey(KeyCode.Tab);
            }

            if (tabHeld != _isShowing)
            {
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
                scoreboardRoot.SetActive(true);
                if (scoreboardCanvasGroup != null) scoreboardCanvasGroup.alpha = 1f;
                RefreshScoreboardData();
            }
            else
            {
                scoreboardRoot.SetActive(false);
                if (scoreboardCanvasGroup != null) scoreboardCanvasGroup.alpha = 0f;
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
            if (activeMode == null) return;

            string modeStr = (activeMode is TDMMode) ? "TEAM DEATHMATCH" : "FREE FOR ALL";
            if (modeTitleText != null) modeTitleText.text = $"MATCH SCOREBOARD • {modeStr}";

            var playerEntries = new List<ScoreRowData>();
            ulong localClientId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0;

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.ConnectedClientsList != null)
            {
                foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    ulong id = client.ClientId;
                    string pName = PlayerNameRegistry.GetName(id);
                    int kills = activeMode.GetPlayerKills(id);
                    int deaths = activeMode.GetPlayerDeaths(id);
                    int score = kills * 100 - deaths * 25;
                    if (score < 0) score = 0;

                    playerEntries.Add(new ScoreRowData
                    {
                        ClientId = id,
                        PlayerName = pName,
                        Kills = kills,
                        Deaths = deaths,
                        Score = score,
                        IsLocal = (id == localClientId)
                    });
                }
            }

            // Sort descending by kills/score
            playerEntries.Sort((a, b) => b.Kills != a.Kills ? b.Kills.CompareTo(a.Kills) : b.Score.CompareTo(a.Score));

            for (int i = 0; i < playerEntries.Count; i++)
            {
                var entry = playerEntries[i];
                CreateScoreboardRow(i + 1, entry);
            }
        }

        private void CreateScoreboardRow(int rank, ScoreRowData entry)
        {
            if (rowPrefab == null || rowsContainer == null) return;

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

            if (entry.IsLocal && bgImg != null)
            {
                bgImg.color = new Color(0.2f, 0.6f, 1f, 0.4f);
            }
        }

        private struct ScoreRowData
        {
            public ulong ClientId;
            public string PlayerName;
            public int Kills;
            public int Deaths;
            public int Score;
            public bool IsLocal;
        }
    }
}
