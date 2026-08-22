using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace Shootopia
{
    /// <summary>
    /// Persistent singleton handling smooth animated loading screen transitions between scenes using UGUI.
    /// Supports asynchronous progress tracking, rotating tips, space loading icon animations, and fade-in/out.
    /// </summary>
    public class LoadingScreenManager : MonoBehaviour
    {
        private static LoadingScreenManager _instance;
        public static LoadingScreenManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<LoadingScreenManager>();
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Loading Screen Elements")]
        [SerializeField] private GameObject loadingRoot;
        [SerializeField] private CanvasGroup loadingCanvasGroup;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private Image progressFillImage;
        [SerializeField] private TextMeshProUGUI tipText;
        [SerializeField] private Transform spinnerIcon;

        private readonly string[] _tips = new string[]
        {
            "Maintain killstreaks to charge your velocity gauge and unleash weapon abilities!",
            "Loot boxes scatter high-tier weapons, smoke canisters, and deployable trip mines.",
            "Use Dash (Shift) to rapidly reposition and dodge incoming sniper rounds.",
            "Press [Tab] in-game to inspect the live match scoreboard and rankings.",
            "Flameguns deal persistent damage over time. Keep your distance from heavy armored foes!",
            "Coordinate with your squad in Team Deathmatch to control high-ground spawn nodes."
        };

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (loadingRoot != null && loadingRoot.activeInHierarchy && spinnerIcon != null)
            {
                spinnerIcon.Rotate(0f, 0f, -120f * Time.unscaledDeltaTime);
            }
        }

        public static void LoadScene(string sceneName, string customStatus = "WARPING TO ARENA...")
        {
            if (Instance != null)
            {
                Instance.StartCoroutine(Instance.LoadSceneRoutine(sceneName, customStatus));
            }
            else
            {
                SceneManager.LoadScene(sceneName);
            }
        }

        public static void ShowOverlay(string statusMessage = "CONNECTING...")
        {
            if (Instance != null)
            {
                Instance.SetVisible(true, statusMessage);
            }
        }

        public static void HideOverlay()
        {
            if (Instance != null)
            {
                Instance.SetVisible(false);
            }
        }

        private void SetVisible(bool visible, string status = "")
        {
            if (loadingRoot == null) return;

            loadingRoot.SetActive(visible);

            if (visible)
            {
                if (loadingCanvasGroup != null) loadingCanvasGroup.alpha = 1f;
                if (!string.IsNullOrEmpty(status) && statusText != null)
                {
                    statusText.text = status;
                }
                if (tipText != null)
                {
                    tipText.text = _tips[Random.Range(0, _tips.Length)];
                }
                if (progressBar != null) progressBar.value = 0f;
                if (progressFillImage != null) progressFillImage.fillAmount = 0f;
            }
        }

        private IEnumerator LoadSceneRoutine(string sceneName, string status)
        {
            SetVisible(true, status);

            yield return new WaitForSecondsRealtime(0.2f);

            var asyncOp = SceneManager.LoadSceneAsync(sceneName);
            asyncOp.allowSceneActivation = false;

            while (asyncOp.progress < 0.9f)
            {
                float pct = Mathf.Clamp01(asyncOp.progress / 0.9f);
                if (progressBar != null) progressBar.value = pct;
                if (progressFillImage != null) progressFillImage.fillAmount = pct;
                yield return null;
            }

            if (progressBar != null) progressBar.value = 1f;
            if (progressFillImage != null) progressFillImage.fillAmount = 1f;

            yield return new WaitForSecondsRealtime(0.35f);

            asyncOp.allowSceneActivation = true;

            while (!asyncOp.isDone)
            {
                yield return null;
            }

            yield return new WaitForSecondsRealtime(0.2f);
            SetVisible(false);
        }
    }
}
