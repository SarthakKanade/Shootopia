using System.Collections;
using UnityEngine;
using Shootopia;

namespace Shootopia
{
    /// <summary>
    /// Displays prominent in-game notification toasts for network errors, session failures,
    /// and host disconnection messages across all scenes.
    /// </summary>
    public class NetworkNotificationUI : MonoBehaviour
    {
        public static NetworkNotificationUI Instance { get; private set; }

        private string _activeNotification = "";
        private float _hideTime = 0f;
        private bool _isErrorType = true;

        private GUIStyle _toastBgStyle;
        private GUIStyle _toastTextStyle;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.OnSessionError.AddListener(ShowSessionError);
            }
        }

        private void OnDisable()
        {
            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.OnSessionError.RemoveListener(ShowSessionError);
            }
        }

        private void ShowSessionError(string error)
        {
            ShowToast(error, duration: 4.5f, isError: true);
        }

        public static void ShowToast(string message, float duration = 4f, bool isError = true)
        {
            if (Instance == null)
            {
                var go = new GameObject("NetworkNotificationUI");
                Instance = go.AddComponent<NetworkNotificationUI>();
            }

            Instance._activeNotification = message;
            Instance._hideTime = Time.unscaledTime + duration;
            Instance._isErrorType = isError;

            Debug.Log($"[NetworkNotificationUI] Toast: '{message}' (isError={isError})");
        }

        private void InitStyles()
        {
            if (_toastBgStyle != null) return;

            _toastBgStyle = new GUIStyle(GUI.skin.box);
            _toastBgStyle.padding = new RectOffset(20, 20, 10, 10);
            _toastBgStyle.alignment = TextAnchor.MiddleCenter;

            _toastTextStyle = new GUIStyle(GUI.skin.label);
            _toastTextStyle.alignment = TextAnchor.MiddleCenter;
            _toastTextStyle.fontSize = 16;
            _toastTextStyle.fontStyle = FontStyle.Bold;
            _toastTextStyle.wordWrap = true;
        }

        private void OnGUI()
        {
            if (string.IsNullOrEmpty(_activeNotification) || Time.unscaledTime > _hideTime) return;

            InitStyles();

            // Dynamic color background based on error vs info
            Texture2D bgTex = new Texture2D(1, 1);
            if (_isErrorType)
            {
                bgTex.SetPixel(0, 0, new Color(0.8f, 0.2f, 0.2f, 0.95f)); // Deep Red
                _toastTextStyle.normal.textColor = Color.white;
            }
            else
            {
                bgTex.SetPixel(0, 0, new Color(0.2f, 0.6f, 0.9f, 0.95f)); // Cool Blue/Amber info
                _toastTextStyle.normal.textColor = Color.white;
            }
            bgTex.Apply();
            _toastBgStyle.normal.background = bgTex;

            float width = Mathf.Min(600f, Screen.width - 40f);
            float height = 60f;
            float x = (Screen.width - width) / 2f;
            float y = Screen.height - height - 30f;

            GUI.Box(new Rect(x, y, width, height), GUIContent.none, _toastBgStyle);
            GUI.Label(new Rect(x + 10f, y + 5f, width - 20f, height - 10f), _activeNotification, _toastTextStyle);
        }
    }
}
