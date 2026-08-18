using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Shootopia
{
    /// <summary>
    /// Persistent singleton handling smooth animated loading screen transitions between scenes.
    /// Supports asynchronous progress tracking, rotating tips, and fade-in/out.
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

        [SerializeField] private UIDocument uiDocument;

        private VisualElement _loadingRoot;
        private Label _statusText;
        private VisualElement _progressFill;
        private Label _tipText;

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
                Destroy(this);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }

            if (uiDocument != null && uiDocument.rootVisualElement != null)
            {
                BindElements();
            }
        }

        private void BindElements()
        {
            var root = uiDocument.rootVisualElement;
            _loadingRoot = root.Q<VisualElement>("LoadingRoot");
            _statusText = root.Q<Label>("LoadingStatusText");
            _progressFill = root.Q<VisualElement>("ProgressBarFill");
            _tipText = root.Q<Label>("LoadingTipText");
        }

        /// <summary>
        /// Initiates an asynchronous scene load with a branded loading screen overlay.
        /// </summary>
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
            if (_loadingRoot == null) BindElements();
            if (_loadingRoot == null) return;

            _loadingRoot.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            _loadingRoot.pickingMode = visible ? PickingMode.Position : PickingMode.Ignore;

            if (visible)
            {
                _loadingRoot.AddToClassList("modal-backdrop--visible");
                if (!string.IsNullOrEmpty(status) && _statusText != null)
                {
                    _statusText.text = status;
                }
                if (_tipText != null)
                {
                    _tipText.text = _tips[Random.Range(0, _tips.Length)];
                }
                if (_progressFill != null)
                {
                    _progressFill.style.width = new Length(0, LengthUnit.Percent);
                }
            }
            else
            {
                _loadingRoot.RemoveFromClassList("modal-backdrop--visible");
            }
        }

        private IEnumerator LoadSceneRoutine(string sceneName, string status)
        {
            SetVisible(true, status);

            yield return new WaitForSeconds(0.2f);

            var asyncOp = SceneManager.LoadSceneAsync(sceneName);
            asyncOp.allowSceneActivation = false;

            while (asyncOp.progress < 0.9f)
            {
                float pct = Mathf.Clamp01(asyncOp.progress / 0.9f) * 100f;
                if (_progressFill != null)
                {
                    _progressFill.style.width = new Length(pct, LengthUnit.Percent);
                }
                yield return null;
            }

            if (_progressFill != null)
            {
                _progressFill.style.width = new Length(100, LengthUnit.Percent);
            }

            yield return new WaitForSeconds(0.3f);

            asyncOp.allowSceneActivation = true;

            while (!asyncOp.isDone)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.2f);
            SetVisible(false);
        }
    }
}
