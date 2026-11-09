using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

namespace Shootopia
{
    public partial class ResultsScreenController
    {
        private void OnReturnToLobbyClicked()
        {
            if (_isActionInProgress) return;
            _isActionInProgress = true;

            CursorManager.PopUnlock("ResultsScreen");
            if (btnReturnToLobby != null) btnReturnToLobby.interactable = false;
            if (btnMainMenu != null) btnMainMenu.interactable = false;

            if (NetworkedGameManager.Instance != null)
            {
                NetworkedGameManager.Instance.RequestRematch();
            }
            else if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                string currentMap = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                NetworkManager.Singleton.SceneManager.LoadScene(currentMap, LoadSceneMode.Single);
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

