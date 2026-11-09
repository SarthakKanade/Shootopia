using UnityEngine;
using Unity.Netcode;

namespace Shootopia
{
    public partial class WaitingRoomController
    {
        private void RegisterCustomMessaging()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.CustomMessagingManager != null)
            {
                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("Shootopia_MatchStarted", OnMatchStartedDirectMessage);
            }
        }

        private void UnregisterCustomMessaging()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.CustomMessagingManager != null)
            {
                NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler("Shootopia_MatchStarted");
            }
        }

        private void OnMatchStartedDirectMessage(ulong senderClientId, FastBufferReader reader)
        {
            Debug.Log("[WaitingRoomController] Direct Shootopia_MatchStarted message received. Immediately starting match locally.");
            OnMatchStarted();
        }

        /// <summary>
        /// Called when the match officially starts: closes Waiting Room UI completely.
        /// </summary>
        public void OnMatchStarted()
        {
            _matchStarted = true;
            _isGameOverMode = false;
            IsLobbyMenuOpen = false;
            CursorManager.PopUnlock("WaitingRoom");
            if (dimBackdrop != null) dimBackdrop.SetActive(false);
            if (lobbyModalDiv != null) lobbyModalDiv.SetActive(false);
            if (warmupHudBadge != null) warmupHudBadge.SetActive(false);
            // Keep gameObject active so event listeners, audio, and GameOver triggers remain alive.

            PlayMapBattleMusic();
        }
    }
}
