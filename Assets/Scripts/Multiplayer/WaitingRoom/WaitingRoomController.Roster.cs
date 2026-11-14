using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using Unity.Collections;
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
                    bool canKick = isHost && !isLocal && !isPlayerHost;
                    CreatePlayerCard(targetParent, p, isPlayerHost, isLocal, isReady, themeColor, canKick);
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

        private void CreatePlayerCard(Transform parent, PlayerReadyState player, bool isPlayerHost, bool isLocal, bool isReady, Color nameColor, bool canKick = false)
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
            if (isPlayerHost)
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
                    img.gameObject.SetActive(isPlayerHost);
                    if (isPlayerHost) img.color = ColorGold;
                }
                if (img.gameObject.name.Contains("ReadyBadge") || img.gameObject.name.Contains("StatusIndicator"))
                {
                    img.gameObject.SetActive(!isPlayerHost && isReady);
                    img.color = ColorGreen;
                    if (canKick)
                    {
                        var badgeRt = img.GetComponent<RectTransform>();
                        if (badgeRt != null)
                        {
                            badgeRt.anchoredPosition = new Vector2(-114f, 0f);
                        }
                    }
                }
                if (img.gameObject.name == "CardBorder" || img.gameObject.name == "BorderGlow")
                {
                    img.color = nameColor;
                }
            }

            if (canKick)
            {
                CreateKickButton(card.transform, player);
            }
        }

        private void CreateKickButton(Transform cardTransform, PlayerReadyState player)
        {
            var kickObj = new GameObject("BtnKick", typeof(RectTransform), typeof(Image), typeof(Button));
            kickObj.transform.SetParent(cardTransform, false);

            var rt = kickObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-48f, 0f);
            rt.sizeDelta = new Vector2(58f, 26f);

            var img = kickObj.GetComponent<Image>();
            img.color = new Color(0.65f, 0.12f, 0.16f, 0.9f);

            var btn = kickObj.GetComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = new Color(0.65f, 0.12f, 0.16f, 0.9f);
            colors.highlightedColor = new Color(0.85f, 0.2f, 0.25f, 1f);
            colors.pressedColor = new Color(0.4f, 0.08f, 0.1f, 1f);
            btn.colors = colors;

            var textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(kickObj.transform, false);
            var textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;

            var tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = "KICK";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 11f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            string targetPlayerId = player.playerId;
            string targetDisplayName = player.displayName;
            btn.onClick.AddListener(() => KickPlayer(targetPlayerId, targetDisplayName));
        }

        public async void KickPlayer(string playerId, string displayName)
        {
            if (SessionManager.Instance == null || !SessionManager.Instance.IsHost) return;

            Debug.Log($"[WaitingRoomController] Host kicking player '{displayName}' (ID: {playerId})...");

            // 1. Resolve NGO clientId
            ulong targetClientId = ulong.MaxValue;
            if (PlayerNameRegistry.TryGetClientIdByPlayerId(playerId, out ulong foundId))
            {
                targetClientId = foundId;
            }
            else if (ulong.TryParse(playerId, out ulong parsedId))
            {
                targetClientId = parsedId;
            }
            else if (NetworkManager.Singleton != null && NetworkManager.Singleton.ConnectedClientsList != null)
            {
                foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    if (client.ClientId != NetworkManager.Singleton.LocalClientId)
                    {
                        string name = PlayerNameRegistry.GetName(client.ClientId);
                        if (name == displayName)
                        {
                            targetClientId = client.ClientId;
                            break;
                        }
                    }
                }
            }

            // 2. Send kick notification direct message to client if connected to NGO
            if (targetClientId != ulong.MaxValue && NetworkManager.Singleton != null && NetworkManager.Singleton.CustomMessagingManager != null)
            {
                try
                {
                    using (var writer = new FastBufferWriter(0, Allocator.Temp))
                    {
                        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("Shootopia_Kicked", targetClientId, writer);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[WaitingRoomController] Note on SendNamedMessage Kick: {ex.Message}");
                }

                // Disconnect from NGO
                try
                {
                    NetworkManager.Singleton.DisconnectClient(targetClientId);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[WaitingRoomController] Note on DisconnectClient: {ex.Message}");
                }
            }

            // 3. Remove from UGS session if online
            if (SessionManager.Instance != null)
            {
                await SessionManager.Instance.KickPlayerAsync(playerId);
            }

            NetworkNotificationUI.ShowToast($"{displayName} was kicked from the room.", 3f, false);
            RefreshLobbyUI();
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
