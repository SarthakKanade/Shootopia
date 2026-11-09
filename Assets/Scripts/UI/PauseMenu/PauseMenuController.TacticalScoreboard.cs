using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

namespace Shootopia
{
    public partial class PauseMenuController
    {
        private bool IsTDMActive()
        {
            var activeMode = NetworkedGameManager.Instance != null ? NetworkedGameManager.Instance.ActiveGameMode : GameModeBase.Instance;
            if (activeMode is TDMMode) return true;
            if (NetworkedGameManager.Instance != null && NetworkedGameManager.Instance.ActiveModeName.Value.ToString().Contains("TDM")) return true;
            if (SessionManager.Instance != null && SessionManager.Instance.GetSessionGameMode().Contains("TDM")) return true;
            return false;
        }

        private void RefreshLiveScoreboardData()
        {
            if (scoreTableContent == null && pauseMenuRoot != null)
            {
                scoreTableContent = TacticalUIBuilder.EnsureScrollRowsContainer(pauseMenuRoot, 350f, -5f);
            }
            if (scoreTableContent == null) return;
            foreach (Transform child in scoreTableContent) Destroy(child.gameObject);

            bool isTDM = IsTDMActive();
            var activeMode = NetworkedGameManager.Instance != null ? NetworkedGameManager.Instance.ActiveGameMode : GameModeBase.Instance;
            var tdmMode = (activeMode as TDMMode) ?? (GameModeBase.Instance as TDMMode);

            ulong localClientId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0;
            int localTeam = (isTDM && tdmMode != null) ? tdmMode.GetPlayerTeam(localClientId) : -1;

            if (pauseSubtitleText != null)
            {
                if (isTDM)
                {
                    int blue = tdmMode != null ? tdmMode.ScoreBlue : 0;
                    int red = tdmMode != null ? tdmMode.ScoreRed : 0;
                    int target = (tdmMode != null && tdmMode.TargetScoreLimit.Value > 0) ? tdmMode.TargetScoreLimit.Value : 15;
                    pauseSubtitleText.text = $"<color=#00F0FF>ALLIES: {blue}</color>  <size=12>— VS —</size>  <color=#FF4444>{red} :AXIS</color>  <size=12><color=#8A99B5>(TARGET: {target} PTS)</color></size>";
                }
                else
                {
                    int target = (activeMode != null && activeMode.TargetScoreLimit.Value > 0) ? activeMode.TargetScoreLimit.Value : 10;
                    pauseSubtitleText.text = $"FREE FOR ALL • LIVE LEADERBOARD (TARGET: {target} KILLS)";
                }
            }

            var allPlayers = new List<ScoreboardRowEntry>();
            var processedIds = new HashSet<ulong>();

            // 1. Server/Host authoritative client list
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    if (processedIds.Add(client.ClientId))
                        allPlayers.Add(BuildEntry(client.ClientId, localClientId, isTDM, tdmMode, activeMode));
                }
            }

            // 2. Query PlayerHealth.ActivePlayers (all active avatars across Host and Clients)
            if (PlayerHealth.ActivePlayers != null)
            {
                for (int i = 0; i < PlayerHealth.ActivePlayers.Count; i++)
                {
                    var ph = PlayerHealth.ActivePlayers[i];
                    if (ph != null && processedIds.Add(ph.OwnerClientId))
                        allPlayers.Add(BuildEntry(ph.OwnerClientId, localClientId, isTDM, tdmMode, activeMode, ph.DeathCount.Value));
                }
            }

            // 3. Query SessionManager players if available
            var session = SessionManager.Instance != null ? SessionManager.Instance.GetCurrentSession() : null;
            if (session != null && session.Players != null)
            {
                foreach (var sp in session.Players)
                {
                    if (ulong.TryParse(sp.Id, out ulong id) && processedIds.Add(id))
                    {
                        string pName = sp.Properties != null && sp.Properties.TryGetValue("DisplayName", out var np) ? np.Value : null;
                        allPlayers.Add(BuildEntry(id, localClientId, isTDM, tdmMode, activeMode, 0, pName));
                    }
                }
            }

            // 4. Guarantee local player presence even if testing solo/offline
            if (!processedIds.Contains(localClientId))
            {
                string localName = PlayerPrefs.GetString("Shootopia_PlayerName", PlayerPrefs.GetString("PlayerName", "Operator"));
                allPlayers.Add(BuildEntry(localClientId, localClientId, isTDM, tdmMode, activeMode, 0, localName));
            }

            if (isTDM)
            {
                var blueSquad = new List<ScoreboardRowEntry>();
                var redSquad = new List<ScoreboardRowEntry>();
                foreach (var p in allPlayers)
                {
                    if (p.Team == 0) blueSquad.Add(p);
                    else if (p.Team == 1) redSquad.Add(p);
                    else if (blueSquad.Count <= redSquad.Count) blueSquad.Add(p);
                    else redSquad.Add(p);
                }

                blueSquad.Sort((a, b) => b.Kills != a.Kills ? b.Kills.CompareTo(a.Kills) : b.Score.CompareTo(a.Score));
                redSquad.Sort((a, b) => b.Kills != a.Kills ? b.Kills.CompareTo(a.Kills) : b.Score.CompareTo(a.Score));

                int bScore = tdmMode != null ? tdmMode.ScoreBlue : 0;
                int rScore = tdmMode != null ? tdmMode.ScoreRed : 0;

                CreateTeamBanner("◆ ALLIES // BLUE SQUAD", new Color(0.04f, 0.22f, 0.42f, 0.90f), bScore, blueSquad.Count);
                for (int i = 0; i < blueSquad.Count; i++) CreateScoreboardRow(i + 1, blueSquad[i], true);

                CreateSectionSpacer(8f);

                CreateTeamBanner("◆ AXIS // RED SQUAD", new Color(0.42f, 0.08f, 0.12f, 0.90f), rScore, redSquad.Count);
                for (int i = 0; i < redSquad.Count; i++) CreateScoreboardRow(i + 1, redSquad[i], true);
            }
            else
            {
                allPlayers.Sort((a, b) => b.Kills != a.Kills ? b.Kills.CompareTo(a.Kills) : b.Score.CompareTo(a.Score));
                for (int i = 0; i < allPlayers.Count; i++) CreateScoreboardRow(i + 1, allPlayers[i], false);
            }
        }

        private void CreateTeamBanner(string title, Color color, int score, int count)
        {
            var banner = new GameObject($"TeamBanner_{title}", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            banner.transform.SetParent(scoreTableContent, false);
            banner.GetComponent<RectTransform>().sizeDelta = new Vector2(860f, 32f);
            banner.GetComponent<Image>().color = color;

            var hlg = banner.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(16, 16, 2, 2);
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.spacing = 12f;

            var font = pauseModeTitleText != null ? pauseModeTitleText.font : null;
            TacticalUIBuilder.CreateHeaderCol("Title", banner.transform, title.ToUpper(), 500f, TextAlignmentOptions.MidlineLeft, font);
            TacticalUIBuilder.CreateHeaderCol("Stats", banner.transform, $"{score} PTS • {count} OPERATIVES", 320f, TextAlignmentOptions.MidlineRight, font);
        }

        private void CreateSectionSpacer(float height)
        {
            var spacer = new GameObject("Spacer", typeof(RectTransform));
            spacer.transform.SetParent(scoreTableContent, false);
            spacer.GetComponent<RectTransform>().sizeDelta = new Vector2(860f, height);
        }

        private void CreateScoreboardRow(int rank, ScoreboardRowEntry entry, bool isTDM)
        {
            bool isHost = entry.IsLocal && NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
            string squadText = isTDM ? (entry.Team == 0 ? "<color=#00F0FF>BLUE</color>" : "<color=#FF4444>RED</color>") : $"#{rank}";
            string nameText = entry.IsLocal ? $"{entry.PlayerName} <size=11><color=#FFD555>[YOU]</color></size>" : entry.PlayerName;

            Color bgColor = isTDM
                ? (entry.Team == 0 ? (entry.IsLocal ? new Color(0.10f, 0.40f, 0.65f, 0.65f) : new Color(0.04f, 0.12f, 0.22f, 0.85f))
                                   : (entry.IsLocal ? new Color(0.65f, 0.15f, 0.18f, 0.65f) : new Color(0.22f, 0.05f, 0.08f, 0.85f)))
                : (entry.IsLocal ? new Color(0.18f, 0.72f, 1.0f, 0.35f) : new Color(0.06f, 0.09f, 0.18f, 0.8f));

            var row = new GameObject($"Row_{rank}", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(scoreTableContent, false);
            row.GetComponent<RectTransform>().sizeDelta = new Vector2(860f, 40f);
            row.GetComponent<Image>().color = bgColor;

            var hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(20, 20, 4, 4);
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.spacing = 10;

            var font = pauseModeTitleText != null ? pauseModeTitleText.font : null;
            TacticalUIBuilder.CreateHeaderCol("Rank", row.transform, squadText, 65f, TextAlignmentOptions.MidlineLeft, font);
            TacticalUIBuilder.CreateHeaderCol("Player", row.transform, nameText, 320f, TextAlignmentOptions.MidlineLeft, font);
            TacticalUIBuilder.CreateHeaderCol("KD", row.transform, $"{entry.Kills} K / {entry.Deaths} D", 195f, TextAlignmentOptions.Center, font);
            TacticalUIBuilder.CreateHeaderCol("Score", row.transform, $"{entry.Score} PTS", 135f, TextAlignmentOptions.MidlineRight, font);
            TacticalUIBuilder.CreatePingWidget(row.transform, entry.Ping, isHost, 95f, font);
        }

        private ScoreboardRowEntry BuildEntry(ulong id, ulong localClientId, bool isTDM, TDMMode tdm, GameModeBase mode, int fallbackDeaths = 0, string customName = null)
        {
            string pName = !string.IsNullOrEmpty(customName) ? customName : PlayerNameRegistry.GetName(id);
            int kills = mode != null ? mode.GetPlayerKills(id) : 0;
            int deaths = mode != null ? mode.GetPlayerDeaths(id) : fallbackDeaths;
            int score = Mathf.Max(0, kills * 100 - deaths * 25);
            int ping = NetworkPingUtility.GetCurrentPing(id);
            int pTeam = (isTDM && tdm != null) ? tdm.GetPlayerTeam(id) : -1;
            return new ScoreboardRowEntry { ClientId = id, PlayerName = pName, Kills = kills, Deaths = deaths, Score = score, Ping = ping, Team = pTeam, IsLocal = (id == localClientId) };
        }

        private struct ScoreboardRowEntry
        {
            public ulong ClientId;
            public string PlayerName;
            public int Kills;
            public int Deaths;
            public int Score;
            public int Ping;
            public int Team;
            public bool IsLocal;
        }
    }
}

