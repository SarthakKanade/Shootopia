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
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Registers a request to unlock the cursor (e.g., from MainMenu, PauseMenu, Scoreboard, ResultsScreen).
        /// </summary>
        public static void PushUnlock(string requesterId)
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

        public static bool IsCursorUnlocked => Instance != null && Instance._unlockRequesters.Count > 0;

        private void ApplyCursorState()
        {
            if (_unlockRequesters.Count > 0)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
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
