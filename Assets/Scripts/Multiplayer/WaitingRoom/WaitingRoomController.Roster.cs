using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using Unity.Services.Multiplayer;

namespace Shootopia
{
    public partial class WaitingRoomController
    {
private void PopulateRoster(ISession session, bool isHost, bool isTeamMode)
        {
            // Clear existing cards
            if (contentFFA != null) foreach (Transform child in contentFFA) Destroy(child.gameObject);
            if (contentAlpha != null) foreach (Transform child in contentAlpha) Destroy(child.gameObject);
            if (contentOmega != null) foreach (Transform child in contentOmega) Destroy(child.gameObject);

            var readyStates = SessionManager.Instance != null ? SessionManager.Instance.GetPlayerReadyStates() : null;
            if (readyStates == null || readyStates.Count == 0) return;

            string localPlayerId = (session != null && session.CurrentPlayer != null) ? session.CurrentPlayer.Id : (NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId.ToString() : "0");
            string hostId = (session != null && !string.IsNullOrEmpty(session.Host)) ? session.Host : (NetworkManager.Singleton != null ? NetworkManager.ServerClientId.ToString() : "0");
            int readyCount = 0;
            int alphaCount = 0;
            int omegaCount = 0;

            foreach (var p in readyStates)
            {
                bool isPlayerHost = (p.playerId == hostId);
                bool isLocal = (p.playerId == localPlayerId);
                bool isReady = (p.isReady || isPlayerHost);
                if (isReady) readyCount++;

                if (isLocal)
                {
                    _localTeamIndex = p.teamIndex;
                }

                Transform targetParent = contentFFA;
                Color themeColor = ColorSapphire;

                if (isTeamMode)
                {
                    if (p.teamIndex == 0)
                    {
                        targetParent = contentAlpha;
                        themeColor = ColorSapphire;
                        alphaCount++;
                    }
                    else
                    {
                        targetParent = contentOmega;
                        themeColor = ColorRuby;
                        omegaCount++;
                    }
                }

                if (targetParent != null && playerCardPrefab != null)
                {
                    CreatePlayerCard(targetParent, p, isPlayerHost, isLocal, isReady, themeColor);
                }
            }

            // Update Counts
            if (labelAlphaCount != null) labelAlphaCount.text = $"({alphaCount})";
            if (labelOmegaCount != null) labelOmegaCount.text = $"({omegaCount})";

            bool allReady = (readyCount >= readyStates.Count);
            if (btnStartMatch != null && isHost)
            {
                btnStartMatch.interactable = allReady;
            }

            if (rosterStatusSummary != null)
            {
                if (isHost)
                {
                    rosterStatusSummary.text = allReady
                        ? "<color=#45F595>ALL PLAYERS READY — LAUNCH ARENA</color>"
                        : $"<color=#FFD555>WAITING FOR PLAYERS ({readyCount}/{readyStates.Count} READY)</color>";
                }
                else
                {
                    rosterStatusSummary.text = _isLocalReady
                        ? "<color=#45F595>YOU ARE READY! WAITING FOR HOST...</color>"
                        : "<color=#FFD555>CLICK READY TO CONFIRM</color>";
                }
            }
        }

        private void CreatePlayerCard(Transform parent, PlayerReadyState player, bool isHost, bool isLocal, bool isReady, Color nameColor)
        {
            var card = Instantiate(playerCardPrefab, parent);
            var rect = card.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(parent.GetComponent<RectTransform>() != null ? parent.GetComponent<RectTransform>().rect.width - 16 : 380, 56);
            }

            var texts = card.GetComponentsInChildren<TextMeshProUGUI>();
            var images = card.GetComponentsInChildren<Image>();

            string namePrefix = "";
            if (isHost)
            {
                namePrefix = "<color=#FFD555>[HOST] </color>";
            }
            else if (isReady)
            {
                namePrefix = "<color=#45F595>[READY] </color>";
            }

            string displayName = namePrefix + player.displayName;
            if (isLocal) displayName += " <color=#FFD555>(YOU)</color>";

            foreach (var t in texts)
            {
                if (t.gameObject.name == "PlayerName" || t.gameObject.name == "Name")
                {
                    t.text = displayName;
                    t.color = nameColor;
                }
                else if (t.gameObject.name == "StatusText" || t.gameObject.name == "RoleBadge" || t.gameObject.name == "ReadyStatus")
                {
                    // No text for host/client role; ready state is shown via green tick in front of name and icons
                    t.text = "";
                    t.gameObject.SetActive(false);
                }
            }

            foreach (var img in images)
            {
                if (img.gameObject.name.Contains("Crown") || img.gameObject.name.Contains("HostIcon"))
                {
                    img.gameObject.SetActive(isHost);
                    if (isHost) img.color = ColorGold;
                }
                if (img.gameObject.name.Contains("ReadyBadge") || img.gameObject.name.Contains("StatusIndicator"))
                {
                    img.gameObject.SetActive(!isHost && isReady);
                    img.color = ColorGreen;
                }
                if (img.gameObject.name == "CardBorder" || img.gameObject.name == "BorderGlow")
                {
                    img.color = nameColor;
                }
            }
        }

        public async void SwitchTeam(int newTeamIndex)
        {
            if (_isLocalReady) return; // Locked when ready
            _localTeamIndex = newTeamIndex;

            if (SessionManager.Instance != null)
            {
                await SessionManager.Instance.SetPlayerTeam(newTeamIndex);
            }

            RefreshLobbyUI();
        }

        private async void ToggleReady()
        {
            _isLocalReady = !_isLocalReady;
            if (SessionManager.Instance != null)
            {
                await SessionManager.Instance.SetReadyStatus(_isLocalReady);
            }
            RefreshLobbyUI();
        }
    }
}
