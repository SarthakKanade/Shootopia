using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shootopia
{
    /// <summary>
    /// Handles local scene transitions based on SessionManager connection events.
    /// Redirects players to the Waiting Room scene upon session creation/join,
    /// and back to the Main Menu upon session departure.
    /// </summary>
    public class MultiplayerSceneTransitionHelper : MonoBehaviour
    {
        [Header("Scene Configuration")]
        [SerializeField] private string waitingRoomSceneName = "WaitingRoom";
        [SerializeField] private string mainMenuSceneName = "Main";

        private void Start()
        {
            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.OnSessionCreated.AddListener(LoadWaitingRoom);
                SessionManager.Instance.OnSessionJoined.AddListener(LoadWaitingRoom);
                SessionManager.Instance.OnSessionLeft.AddListener(LoadMainMenu);
            }
            else
            {
                Debug.LogError("[MultiplayerSceneTransitionHelper] SessionManager.Instance is null!");
            }
        }

        private void OnDestroy()
        {
            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.OnSessionCreated.RemoveListener(LoadWaitingRoom);
                SessionManager.Instance.OnSessionJoined.RemoveListener(LoadWaitingRoom);
                SessionManager.Instance.OnSessionLeft.RemoveListener(LoadMainMenu);
            }
        }

        private void LoadWaitingRoom()
        {
            Debug.Log($"[MultiplayerSceneTransitionHelper] Loading waiting room scene: {waitingRoomSceneName}");
            SceneManager.LoadScene(waitingRoomSceneName);
        }

        private void LoadMainMenu()
        {
            Debug.Log($"[MultiplayerSceneTransitionHelper] Loading main menu scene: {mainMenuSceneName}");
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}
