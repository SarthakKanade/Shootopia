using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VelocityWars.Core
{
    public class UIManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text enemiesLeftText;
        [SerializeField] private GameObject youWinText;
        [SerializeField] private GameObject gameOverContainer;
        [SerializeField] private Image[] shieldBars;

        [Header("Durability HUD")]
        [SerializeField] private TMP_Text _durabilityText;
        [SerializeField] private Image _durabilityBar;

        [Header("Velocity Gauge")]
        [SerializeField] private Slider _gaugeSlider;
        [SerializeField] private Animator _gaugeAnimator;

        [Header("Lives")]
        [SerializeField] private Image[] _lifeIcons;

        private const string ENEMIES_LEFT_STRING = "Enemies Left: ";
        private float _maxDurability = 1f;

        public void ShowGameOver()
        {
            if (gameOverContainer != null)
                gameOverContainer.SetActive(true);
        }

        public void ShowVictory()
        {
            if (youWinText != null)
                youWinText.SetActive(true);
        }

        public void OnEnemyCountChanged(float count)
        {
            if (enemiesLeftText != null)
                enemiesLeftText.text = ENEMIES_LEFT_STRING + count.ToString();
            
            Debug.Log($"[UIManager] Enemy count updated: {count}");
        }

        public void OnPlayerHealthChanged(float health)
        {
            Debug.Log($"[UIManager] HP Update Received: {health}");
            if (shieldBars == null || shieldBars.Length == 0) return;

            int barCount = shieldBars.Length - 1;
            float segmentValue = 100f / barCount;
            
            Debug.Log($"[UIManager] HP: {health}, Bars: {barCount}, Segment: {segmentValue}");

            for (int i = 0; i < barCount; i++)
            {
                if (shieldBars[i] == null) continue;
                bool active = health > i * segmentValue;
                shieldBars[i].gameObject.SetActive(active);
                // Debug.Log($"  Bar {i} ({shieldBars[i].name}): Threshold {i * segmentValue}, Active {active}");
            }

            if (shieldBars[barCount] != null)
            {
                shieldBars[barCount].gameObject.SetActive(health > 0);
            }
        }

        public void SetMaxDurability(float max)
        {
            _maxDurability = max;
            if (_maxDurability <= 0) _maxDurability = 1f;
        }

        public void OnDurabilityChanged(float value)
        {
            if (_durabilityText != null)
            {
                _durabilityText.text = value > 0 ? value.ToString("F1") : "—";
            }

            if (_durabilityBar != null)
            {
                _durabilityBar.fillAmount = value / _maxDurability;
            }
        }

        public void OnWeaponExpired()
        {
            if (_durabilityText != null)
                _durabilityText.text = "—";
            
            if (_durabilityBar != null)
                _durabilityBar.fillAmount = 0;
        }

        public void OnGaugeChanged(float value)
        {
            if (_gaugeSlider != null)
                _gaugeSlider.value = value;

            if (_gaugeAnimator != null)
                _gaugeAnimator.SetBool("IsReady", value >= 100f);
        }

        public void OnLifeLost(float remaining)
        {
            Debug.Log($"[UIManager] OnLifeLost Received: {remaining}");
            int r = Mathf.RoundToInt(remaining);
            if (_lifeIcons == null) return;

            for (int i = 0; i < _lifeIcons.Length; i++)
            {
                if (_lifeIcons[i] != null)
                    _lifeIcons[i].enabled = i < r;
            }
        }
    }
}
