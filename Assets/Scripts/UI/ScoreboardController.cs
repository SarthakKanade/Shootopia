using UnityEngine;

namespace Shootopia
{
    /// <summary>
    /// Legacy scoreboard controller. The separate scoreboard UI has been removed.
    /// The unified Tactical Pause Menu (PauseMenuController) now serves both Tab and Esc keys.
    /// Any calls to ToggleScoreboard() are routed to PauseMenuController.
    /// </summary>
    public class ScoreboardController : MonoBehaviour
    {
        public static ScoreboardController Instance { get; private set; }

        public bool IsScoreboardVisible => PauseMenuController.IsMenuPaused;

        private void Awake()
        {
            Instance = this;

            // Ensure any existing ScoreboardRoot UI is completely deactivated and hidden
            var found = transform.Find("ScoreboardRoot") ?? (transform.parent != null ? transform.parent.Find("ScoreboardRoot") : null);
            if (found == null)
            {
                var go = GameObject.Find("ScoreboardRoot");
                if (go != null) found = go.transform;
            }
            if (found != null)
            {
                found.gameObject.SetActive(false);
            }
        }

        public void ToggleScoreboard()
        {
            if (PauseMenuController.Instance != null)
            {
                PauseMenuController.Instance.TogglePause(!PauseMenuController.IsMenuPaused);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
