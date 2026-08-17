using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Unity.Netcode;

namespace Shootopia
{
    /// <summary>
    /// Controls the in-match Tactical Pause Menu (ESC overlay).
    /// Manages cursor locking, settings access, and graceful match departure.
    /// </summary>
    public class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;

        private VisualElement _root;
        private VisualElement _pauseMenuRoot;
        private Button _btnResume;
        private Button _btnSettings;
        private Button _btnLeave;

        private bool _isPaused = false;
        public bool IsPaused => _isPaused;

        private void OnEnable()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }

            if (uiDocument == null) return;

            _root = uiDocument.rootVisualElement;
            if (_root == null) return;

            _pauseMenuRoot = _root.Q<VisualElement>("PauseMenuRoot");
            _btnResume = _root.Q<Button>("BtnResumeMatch");
            _btnSettings = _root.Q<Button>("BtnPauseSettings");
            _btnLeave = _root.Q<Button>("BtnLeaveMatch");

            if (_btnResume != null) _btnResume.clicked += () => TogglePause(false);
            if (_btnLeave != null) _btnLeave.clicked += OnLeaveMatchClicked;

            TogglePause(false);
        }

        private void OnDisable()
        {
            if (_isPaused)
            {
                CursorManager.PopUnlock("PauseMenu");
                _isPaused = false;
            }
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                // Don't toggle pause if waiting room menu is handling it
                if (WaitingRoomController.IsLobbyMenuOpen) return;

                TogglePause(!_isPaused);
            }
        }

        public void TogglePause(bool pause)
        {
            _isPaused = pause;

            if (_pauseMenuRoot != null)
            {
                _pauseMenuRoot.style.display = pause ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (pause)
            {
                CursorManager.PushUnlock("PauseMenu");
            }
            else
            {
                CursorManager.PopUnlock("PauseMenu");
            }
        }

        private async void OnLeaveMatchClicked()
        {
            TogglePause(false);
            LoadingScreenManager.ShowOverlay("RETURNING TO MAIN MENU...");

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                try
                {
                    NetworkManager.Singleton.Shutdown();
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[PauseMenuController] Shutdown note: {ex.Message}");
                }
            }

            if (SessionManager.Instance != null)
            {
                try
                {
                    await SessionManager.Instance.LeaveSession();
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[PauseMenuController] Leave session note: {ex.Message}");
                }
            }

            SceneManager.LoadScene("Main");
        }
    }
}
