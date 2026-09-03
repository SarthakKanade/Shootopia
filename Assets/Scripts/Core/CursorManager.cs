using System.Collections.Generic;
using UnityEngine;

namespace Shootopia
{
    /// <summary>
    /// Centralized stack-based cursor state manager.
    /// Prevents conflicting cursor lock/unlock calls across menus, waiting room, HUD, and scoreboard.
    /// If any system requests an unlock, the cursor remains unlocked and visible until all requesters release.
    /// </summary>
    public class CursorManager : MonoBehaviour
    {
        public static CursorManager Instance { get; private set; }

        private readonly HashSet<string> _unlockRequesters = new HashSet<string>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private static void EnsureInstance()
        {
            if (Instance == null)
            {
                Instance = Object.FindFirstObjectByType<CursorManager>();
                if (Instance == null)
                {
                    var go = new GameObject("CursorManager");
                    Instance = go.AddComponent<CursorManager>();
                    DontDestroyOnLoad(go);
                }
            }
        }

        /// <summary>
        /// Registers a request to unlock the cursor (e.g., from MainMenu, WaitingRoom, PauseMenu, Scoreboard, ResultsScreen).
        /// </summary>
        public static void PushUnlock(string requesterId)
        {
            EnsureInstance();
            Instance._unlockRequesters.Add(requesterId);
            Instance.ApplyCursorState();
        }

        /// <summary>
        /// Releases an unlock request when a menu or modal closes.
        /// </summary>
        public static void PopUnlock(string requesterId)
        {
            if (Instance == null) return;

            Instance._unlockRequesters.Remove(requesterId);
            Instance.ApplyCursorState();
        }

        /// <summary>
        /// Clears all unlock requests (e.g., when transitioning cleanly into a match).
        /// </summary>
        public static void ClearAllRequests()
        {
            if (Instance == null) return;
            Instance._unlockRequesters.Clear();
            Instance.ApplyCursorState();
        }

        public static bool IsCursorUnlocked
        {
            get
            {
                if (Instance != null && Instance._unlockRequesters.Count > 0) return true;
                if (WaitingRoomController.IsLobbyMenuOpen) return true;
                if (PauseMenuController.IsMenuPaused) return true;
                return false;
            }
        }

        private void Update()
        {
            // Keep cursor state continuously synchronized with active UI demands
            ApplyCursorState();
        }

        public void ApplyCursorState()
        {
            string activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            bool isMenuScene = (activeScene == "MainMenu" || activeScene == "WaitingRoom");

            if (isMenuScene || _unlockRequesters.Count > 0 || WaitingRoomController.IsLobbyMenuOpen || PauseMenuController.IsMenuPaused)
            {
                if (Cursor.lockState != CursorLockMode.None)
                {
                    Cursor.lockState = CursorLockMode.None;
                }
                if (!Cursor.visible)
                {
                    Cursor.visible = true;
                }
            }
            else
            {
                if (Cursor.lockState != CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                }
                if (Cursor.visible)
                {
                    Cursor.visible = false;
                }
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                ApplyCursorState();
            }
        }
    }
}
