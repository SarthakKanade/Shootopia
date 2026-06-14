using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace VelocityWars.Player
{
    using VelocityWars;
    public class OverclockVisuals : MonoBehaviour
{
        [Header("Weapon Visuals")]
        [SerializeField] private Color _overclockColor = new Color(1f, 179f/255f, 0f); // #FFB300
        [SerializeField] private float _baseIntensity = 1.8f;
        [SerializeField] private float _pulseFrequency = 1f;
        [SerializeField] private float _pulseAmplitude = 0.3f;

        [Header("UI Visuals")]
        [SerializeField] private GameObject _timerPrefab;
        [SerializeField] private Color _urgentColor = new Color(1f, 68f/255f, 0f); // #FF4400

        private ActiveWeapon _activeWeapon;
        private GameObject _timerInstance;
        private Image _timerFill;
        private TextMeshProUGUI _timerText;
        
        private Coroutine _visualsRoutine;
        private Dictionary<Renderer, Material[]> _originalMaterials = new Dictionary<Renderer, Material[]>();
        private List<Material> _tempMaterials = new List<Material>();

        private void Awake()
        {
            _activeWeapon = GetComponentInChildren<ActiveWeapon>();
        }

        public void Activate(float duration)
        {
            if (_visualsRoutine != null)
                StopCoroutine(_visualsRoutine);
            
            _visualsRoutine = StartCoroutine(OverclockRoutine(duration));
        }

        public void Deactivate()
        {
            if (_visualsRoutine != null)
            {
                StopCoroutine(_visualsRoutine);
                _visualsRoutine = StartCoroutine(FadeOutRoutine());
            }
        }

        private Weapon _lastWeapon;

        private IEnumerator OverclockRoutine(float duration)
        {
            SetupUI();
            _lastWeapon = _activeWeapon.CurrentWeapon;
            ApplyWeaponGlow();

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float remaining = duration - elapsed;

                // Handle Weapon Swap
                if (_activeWeapon.CurrentWeapon != _lastWeapon)
                {
                    _lastWeapon = _activeWeapon.CurrentWeapon;
                    ApplyWeaponGlow();
                }

                // Update UI
if (_timerFill != null) _timerFill.fillAmount = remaining / duration;
                if (_timerText != null) _timerText.text = Mathf.CeilToInt(remaining).ToString();

                // Urgent Color Shift
                if (remaining <= 2f)
                {
                    if (_timerFill != null) _timerFill.color = _urgentColor;
                    if (_timerText != null) _timerText.color = _urgentColor;
                }

                // Pulse Emission
                float pulse = Mathf.Sin(Time.time * _pulseFrequency * 2f * Mathf.PI) * _pulseAmplitude;
                float currentIntensity = _baseIntensity + pulse;
                UpdateEmission(currentIntensity);

                yield return null;
            }

            Cleanup();
        }

        private IEnumerator FadeOutRoutine()
        {
            float elapsed = 0f;
            float fadeDuration = 0.3f;
            float startIntensity = _baseIntensity;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                float currentIntensity = Mathf.Lerp(startIntensity, 0f, t);
                UpdateEmission(currentIntensity);
                yield return null;
            }

            Cleanup();
            _visualsRoutine = null;
        }

        private void SetupUI()
        {
            if (_timerInstance == null && _timerPrefab != null)
            {
                // Assuming we find the Canvas named "Overlay Canvas" or similar
                Canvas canvas = GameObject.Find("Overlay Canvas")?.GetComponent<Canvas>();
                if (canvas != null)
                {
                    _timerInstance = Instantiate(_timerPrefab, canvas.transform);
                    // Anchor near ability icon (assuming Velocity Gauge Container is where the ability icon is)
                    Transform gaugeContainer = canvas.transform.Find("Velocity Gauge Container");
                    if (gaugeContainer != null)
                    {
                        _timerInstance.transform.SetParent(gaugeContainer, false);
                        RectTransform rt = _timerInstance.GetComponent<RectTransform>();
                        rt.anchoredPosition = new Vector2(0, 80); // Adjust as needed
                    }
                    
                    _timerFill = _timerInstance.transform.Find("Fill")?.GetComponent<Image>();
                    _timerText = _timerInstance.transform.Find("CountdownText")?.GetComponent<TextMeshProUGUI>();
                }
            }

            if (_timerInstance != null)
            {
                _timerInstance.SetActive(true);
                if (_timerFill != null) _timerFill.color = _overclockColor;
                if (_timerText != null) _timerText.color = Color.white;
            }
        }

        private void ApplyWeaponGlow()
        {
            if (_activeWeapon == null) return;
            
            _originalMaterials.Clear();
            _tempMaterials.Clear();

            Renderer[] renderers = _activeWeapon.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                _originalMaterials[renderer] = renderer.sharedMaterials;
                Material[] newMaterials = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    newMaterials[i] = new Material(renderer.sharedMaterials[i]);
                    newMaterials[i].EnableKeyword("_EMISSION");
                    _tempMaterials.Add(newMaterials[i]);
                }
                renderer.materials = newMaterials;
            }
        }

        private void UpdateEmission(float intensity)
        {
            Color color = _overclockColor * Mathf.Pow(2, intensity);
            foreach (var mat in _tempMaterials)
            {
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", color);
                }
            }
        }

        private void Cleanup()
        {
            if (_timerInstance != null) _timerInstance.SetActive(false);

            foreach (var kvp in _originalMaterials)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.materials = kvp.Value;
                }
            }
            
            foreach (var mat in _tempMaterials)
            {
                Destroy(mat);
            }
            _tempMaterials.Clear();
            _originalMaterials.Clear();
        }
    }
}
