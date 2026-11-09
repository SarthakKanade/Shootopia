using UnityEngine;
using UnityEngine.UI;

namespace Shootopia
{
    public partial class HUDController
    {
        public void EnsureLowHealthSensoryUI()
        {
            if (lowHealthVignetteGroup != null && _heartbeatSource != null) return;

            Transform existing = transform.Find("LowHealthVignette");
            if (existing != null)
            {
                lowHealthVignetteGroup = existing.GetComponent<CanvasGroup>();
            }

            if (lowHealthVignetteGroup == null)
            {
                var vigObj = new GameObject("LowHealthVignette", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
                vigObj.transform.SetParent(transform, false);
                vigObj.transform.SetAsFirstSibling(); // Render beneath HUD text and crosshairs

                var rt = vigObj.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = Vector2.zero;

                lowHealthVignetteGroup = vigObj.GetComponent<CanvasGroup>();
                lowHealthVignetteGroup.alpha = 0f;
                lowHealthVignetteGroup.blocksRaycasts = false;
                lowHealthVignetteGroup.interactable = false;

                var img = vigObj.GetComponent<Image>();
                img.raycastTarget = false;

                if (_vignetteSprite == null)
                {
                    int w = 256, h = 256;
                    Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                    tex.wrapMode = TextureWrapMode.Clamp;
                    Vector2 center = new Vector2(w * 0.5f, h * 0.5f);
                    float maxDist = Vector2.Distance(Vector2.zero, center);
                    Color[] cols = new Color[w * h];
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            float d = Vector2.Distance(new Vector2(x, y), center);
                            float norm = Mathf.Clamp01(d / maxDist);
                            float a = Mathf.SmoothStep(0.2f, 1.0f, norm);
                            a = Mathf.Pow(a, 1.8f);
                            cols[y * w + x] = new Color(0.85f, 0.04f, 0.04f, a);
                        }
                    }
                    tex.SetPixels(cols);
                    tex.Apply();
                    _vignetteSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
                }
                img.sprite = _vignetteSprite;
                img.color = Color.white;
            }

            if (_heartbeatSource == null)
            {
                _heartbeatSource = gameObject.AddComponent<AudioSource>();
                _heartbeatSource.spatialBlend = 0f; // 2D Audio
                _heartbeatSource.loop = true;
                _heartbeatSource.playOnAwake = false;
                _heartbeatSource.priority = 64; // High priority combat audio
            }

            if (_heartbeatSource.clip == null && lowHealthHeartbeatClip != null)
            {
                _heartbeatSource.clip = lowHealthHeartbeatClip;
            }
        }

        private void EnsureAudioListenerFilter()
        {
            if (_listenerLowPassFilter != null) return;
            if (_cachedAudioListener == null) _cachedAudioListener = FindAnyObjectByType<AudioListener>();
            if (_cachedAudioListener != null)
            {
                _listenerLowPassFilter = _cachedAudioListener.GetComponent<AudioLowPassFilter>() ?? _cachedAudioListener.gameObject.AddComponent<AudioLowPassFilter>();
                _listenerLowPassFilter.cutoffFrequency = 22000f;
                _listenerLowPassFilter.enabled = false;
            }
        }

        private void UpdateLowHealthSensory()
        {
            bool isAlive = _localHealth != null && !_localHealth.IsDead;
            float maxHp = isAlive ? _localHealth.GetMaxHp() : 100f;
            float curHp = isAlive ? _localHealth.CurrentHp.Value : 100f;
            float hpRatio = isAlive ? Mathf.Clamp01(curHp / Mathf.Max(1f, maxHp)) : 1f;

            bool isLowHealth = isAlive && hpRatio <= 0.30f && curHp > 0f;

            if (isLowHealth)
            {
                float severity = (0.30f - hpRatio) / 0.30f; // 0 at 30% HP, 1 at 0% HP

                // Pulsating red perimeter vignette
                float pulseHz = Mathf.Lerp(1.4f, 3.2f, severity);
                float pulseWave = 0.65f + 0.35f * Mathf.Sin(Time.time * pulseHz * Mathf.PI * 2f);
                float targetAlpha = Mathf.Lerp(0.35f, 0.85f, severity) * pulseWave;

                if (lowHealthVignetteGroup != null)
                {
                    lowHealthVignetteGroup.alpha = Mathf.MoveTowards(lowHealthVignetteGroup.alpha, targetAlpha, Time.deltaTime * 6f);
                }

                // Heartbeat audio loop
                if (_heartbeatSource != null && _heartbeatSource.clip != null)
                {
                    if (!_heartbeatSource.isPlaying)
                    {
                        _heartbeatSource.volume = 0.1f;
                        _heartbeatSource.Play();
                    }
                    float targetVol = Mathf.Lerp(0.25f, 0.90f, severity);
                    _heartbeatSource.volume = Mathf.MoveTowards(_heartbeatSource.volume, targetVol, Time.deltaTime * 2.5f);
                    _heartbeatSource.pitch = Mathf.Lerp(0.95f, 1.25f, severity);
                }

                // Audio muffling via AudioManager
                float targetCutoff = Mathf.Lerp(1600f, 650f, severity);
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.SetLowHealthMuffled(true, targetCutoff);
                }

                // Listener filter on AudioListener
                EnsureAudioListenerFilter();
                if (_listenerLowPassFilter != null)
                {
                    _listenerLowPassFilter.enabled = true;
                    _listenerLowPassFilter.cutoffFrequency = Mathf.MoveTowards(_listenerLowPassFilter.cutoffFrequency, targetCutoff, Time.deltaTime * 4000f);
                }
            }
            else
            {
                // Smooth recovery / fade out
                if (lowHealthVignetteGroup != null && lowHealthVignetteGroup.alpha > 0f)
                {
                    lowHealthVignetteGroup.alpha = Mathf.MoveTowards(lowHealthVignetteGroup.alpha, 0f, Time.deltaTime * 3.5f);
                }

                if (_heartbeatSource != null && _heartbeatSource.isPlaying)
                {
                    _heartbeatSource.volume = Mathf.MoveTowards(_heartbeatSource.volume, 0f, Time.deltaTime * 3.5f);
                    if (_heartbeatSource.volume <= 0.01f)
                    {
                        _heartbeatSource.Stop();
                    }
                }

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.SetLowHealthMuffled(false);
                }

                if (_listenerLowPassFilter != null && _listenerLowPassFilter.enabled)
                {
                    _listenerLowPassFilter.cutoffFrequency = Mathf.MoveTowards(_listenerLowPassFilter.cutoffFrequency, 22000f, Time.deltaTime * 12000f);
                    if (_listenerLowPassFilter.cutoffFrequency >= 21000f)
                    {
                        _listenerLowPassFilter.enabled = false;
                    }
                }
            }
        }
    }
}

