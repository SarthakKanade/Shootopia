using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Shootopia
{
    /// <summary>
    /// Displays prominent UGUI notification toasts for network errors, session failures,
    /// and host disconnection messages across all scenes.
    /// </summary>
    public class NetworkNotificationUI : MonoBehaviour
    {
        public static NetworkNotificationUI Instance { get; private set; }

        [Header("Toast UI References")]
        [SerializeField] private GameObject toastRoot;
        [SerializeField] private CanvasGroup toastCanvasGroup;
        [SerializeField] private Image toastBackground;
        [SerializeField] private TextMeshProUGUI toastText;
        [SerializeField] private Image toastIcon;

        private Coroutine _toastRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (toastRoot != null) toastRoot.SetActive(false);
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
            if (Instance != null)
            {
                Instance.DisplayToast(message, duration, isError);
            }
            else
            {
                Debug.Log($"[NetworkNotificationUI] Toast (no instance): '{message}'");
            }
        }

        public void DisplayToast(string message, float duration = 4f, bool isError = true)
        {
            if (toastRoot == null) return;

            if (_toastRoutine != null) StopCoroutine(_toastRoutine);
            _toastRoutine = StartCoroutine(ToastRoutine(message, duration, isError));
        }

        private IEnumerator ToastRoutine(string message, float duration, bool isError)
        {
            if (toastText != null) toastText.text = message;

            if (toastBackground != null)
            {
                toastBackground.color = isError ? new Color(0.85f, 0.2f, 0.2f, 0.95f) : new Color(0.2f, 0.5f, 0.9f, 0.95f);
            }

            toastRoot.SetActive(true);
            UIJuice.PopIn(toastRoot.GetComponent<RectTransform>(), toastCanvasGroup, 0.2f);

            yield return new WaitForSecondsRealtime(duration);

            UIJuice.PopOut(toastRoot.GetComponent<RectTransform>(), toastCanvasGroup, 0.2f, () =>
            {
                toastRoot.SetActive(false);
            });

            _toastRoutine = null;
        }
    }
}
