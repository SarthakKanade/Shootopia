using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

namespace Shootopia
{
    public partial class WaitingRoomController
    {
private void PopulateGameOverRoster(bool isTeamMode, PlayerScoreEntry[] scores, GameModeBase activeMode)
        {
            if (scores == null) scores = Array.Empty<PlayerScoreEntry>();

            if (panelFFA != null) panelFFA.SetActive(!isTeamMode);
            if (panelTeams != null) panelTeams.SetActive(isTeamMode);

            ulong localClientId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : ulong.MaxValue;

            if (!isTeamMode)
            {
                if (contentFFA != null)
                {
                    foreach (Transform child in contentFFA) Destroy(child.gameObject);

                    var sorted = new List<PlayerScoreEntry>(scores);
                    sorted.Sort((a, b) =>
                    {
                        int cmp = b.Kills.CompareTo(a.Kills);
                        if (cmp != 0) return cmp;
                        return a.Deaths.CompareTo(b.Deaths);
                    });

                    for (int i = 0; i < sorted.Count; i++)
                    {
                        var entry = sorted[i];
                        bool isWinner = (i == 0);
                        bool isLocal = (entry.ClientId == localClientId);
                        CreateGameOverPlayerCard(contentFFA, i + 1, entry, isWinner, isLocal, isWinner ? ColorGold : ColorSapphire);
                    }
                }
            }
            else
            {
                if (btnSelectAlpha != null) btnSelectAlpha.interactable = false;
                if (btnSelectOmega != null) btnSelectOmega.interactable = false;

                if (contentAlpha != null) foreach (Transform child in contentAlpha) Destroy(child.gameObject);
                if (contentOmega != null) foreach (Transform child in contentOmega) Destroy(child.gameObject);

                var tdm = activeMode as TDMMode ?? FindAnyObjectByType<TDMMode>();

                var alphaList = new List<PlayerScoreEntry>();
                var omegaList = new List<PlayerScoreEntry>();

                foreach (var entry in scores)
                {
                    int team = -1;
                    if (tdm != null)
                    {
                        team = tdm.GetPlayerTeam(entry.ClientId);
                    }
                    if (team == -1 && SessionManager.Instance != null)
                    {
                        string pName = PlayerNameRegistry.GetName(entry.ClientId);
                        var states = SessionManager.Instance.GetPlayerReadyStates();
                        if (states != null)
                        {
                            foreach (var st in states)
                            {
                                if (st.displayName == pName)
                                {
                                    team = st.teamIndex;
                                    break;
                                }
                            }
                        }
                    }
                    if (team == -1) team = 0;

                    if (team == 0) alphaList.Add(entry);
                    else omegaList.Add(entry);
                }

                alphaList.Sort((a, b) =>
                {
                    int cmp = b.Kills.CompareTo(a.Kills);
                    if (cmp != 0) return cmp;
                    return a.Deaths.CompareTo(b.Deaths);
                });

                omegaList.Sort((a, b) =>
                {
                    int cmp = b.Kills.CompareTo(a.Kills);
                    if (cmp != 0) return cmp;
                    return a.Deaths.CompareTo(b.Deaths);
                });

                int alphaKills = tdm != null ? tdm.ScoreBlue : 0;
                int omegaKills = tdm != null ? tdm.ScoreRed : 0;
                if (alphaKills == 0 && omegaKills == 0)
                {
                    foreach (var a in alphaList) alphaKills += a.Kills;
                    foreach (var o in omegaList) omegaKills += o.Kills;
                }

                if (labelAlphaCount != null) labelAlphaCount.text = $"{alphaKills} KILLS";
                if (labelOmegaCount != null) labelOmegaCount.text = $"{omegaKills} KILLS";

                bool alphaWon = alphaKills > omegaKills;
                bool omegaWon = omegaKills > alphaKills;

                for (int i = 0; i < alphaList.Count; i++)
                {
                    var entry = alphaList[i];
                    bool isLocal = (entry.ClientId == localClientId);
                    CreateGameOverPlayerCard(contentAlpha, i + 1, entry, alphaWon && i == 0, isLocal, ColorSapphire);
                }

                for (int i = 0; i < omegaList.Count; i++)
                {
                    var entry = omegaList[i];
                    bool isLocal = (entry.ClientId == localClientId);
                    CreateGameOverPlayerCard(contentOmega, i + 1, entry, omegaWon && i == 0, isLocal, ColorRuby);
                }
            }
        }

        private void CreateGameOverPlayerCard(Transform parent, int rank, PlayerScoreEntry entry, bool isTop, bool isLocal, Color themeColor)
        {
            if (playerCardPrefab == null || parent == null) return;

            var card = Instantiate(playerCardPrefab, parent);
            var rect = card.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(parent.GetComponent<RectTransform>() != null ? parent.GetComponent<RectTransform>().rect.width - 16 : 380, 56);
            }

            var texts = card.GetComponentsInChildren<TextMeshProUGUI>();
            var images = card.GetComponentsInChildren<Image>();

            string displayName = PlayerNameRegistry.GetLastKnownName(entry.ClientId);
            if (string.IsNullOrEmpty(displayName) || displayName == $"Player {entry.ClientId}")
            {
                displayName = isLocal && PlayerProfileManager.Instance != null ? PlayerProfileManager.Instance.DisplayName : "Player";
            }
            if (isLocal) displayName += " <color=#FFD555>(YOU)</color>";

            foreach (var t in texts)
            {
                if (t.gameObject.name == "PlayerName" || t.gameObject.name == "Name")
                {
                    t.text = $"#{rank}  {displayName}";
                    t.color = isLocal ? ColorGold : themeColor;
                }
                else if (t.gameObject.name == "StatusText" || t.gameObject.name == "RoleBadge" || t.gameObject.name == "ReadyStatus")
                {
                    // State kills clearly, no score PTS
                    t.text = isTop
                        ? $"<color=#FFD555><b>{entry.Kills} KILLS</b></color> <color=#A0B0C0><size=11>({entry.Deaths} D)</size></color>"
                        : $"<b>{entry.Kills} KILLS</b> <color=#A0B0C0><size=11>({entry.Deaths} D)</size></color>";
                    t.color = isTop ? ColorGold : ColorGreen;
                }
            }

            foreach (var img in images)
            {
                if (img.gameObject.name.Contains("Crown") || img.gameObject.name.Contains("HostIcon"))
                {
                    img.gameObject.SetActive(isTop);
                    if (isTop) img.color = ColorGold;
                }
                if (img.gameObject.name.Contains("ReadyBadge") || img.gameObject.name.Contains("StatusIndicator"))
                {
                    img.color = isTop ? ColorGold : themeColor;
                }
                if (img.gameObject.name.Contains("CardBorder") || img.gameObject.name.Contains("BorderGlow"))
                {
                    img.color = isTop ? ColorGold : themeColor;
                }
            }
        }
    }
}
