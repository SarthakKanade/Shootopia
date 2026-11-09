using UnityEngine;

namespace Shootopia
{
    public partial class AudioManager
    {
private void Update()
        {
            UpdateDynamicAudioDucking(Time.unscaledDeltaTime);
        }

        private void UpdateDynamicAudioDucking(float dt)
        {
            if (dt <= 0f) return;

            bool duckActive = _isSustainedDucking || _combatDuckTimer > 0f;
            if (_combatDuckTimer > 0f)
            {
                _combatDuckTimer -= dt;
                if (_combatDuckTimer <= 0f)
                {
                    _targetDuckLevel = 1f;
                    _targetLowPassCutoff = 22000f;
                }
            }

            float targetDuck = 1f;
            float targetCutoff = 22000f;

            if (duckActive)
            {
                targetDuck = _isSustainedDucking ? _sustainedDuckLevel : _targetDuckLevel;
                targetCutoff = duckLowPassCutoff;
            }

            if (_isLowHealthMuffled)
            {
                targetCutoff = Mathf.Min(targetCutoff, _lowHealthCutoff);
                targetDuck = Mathf.Min(targetDuck, 0.45f);
            }

            // Smooth envelope interpolation: fast attack, smooth musical release
            float duckSpeed = (targetDuck < _currentDuckMultiplier)
                ? (1f / Mathf.Max(duckAttackTime, 0.01f))
                : (1f / Mathf.Max(duckReleaseTime, 0.05f));
            _currentDuckMultiplier = Mathf.MoveTowards(_currentDuckMultiplier, targetDuck, duckSpeed * dt);

            float filterSpeed = (targetCutoff < _currentLowPassCutoff)
                ? (22000f / Mathf.Max(duckAttackTime, 0.01f))
                : (22000f / Mathf.Max(duckReleaseTime, 0.05f));
            _currentLowPassCutoff = Mathf.MoveTowards(_currentLowPassCutoff, targetCutoff, filterSpeed * dt);

            // Apply low-pass filter
            if (_musicLowPassFilter != null)
            {
                _musicLowPassFilter.cutoffFrequency = _currentLowPassCutoff;
                _musicLowPassFilter.enabled = (_currentLowPassCutoff < 21000f);
            }

            // Apply ducked music volume
            if (_musicSource != null && _musicSource.isPlaying && _musicFadeCoroutine == null)
            {
                float nominalMusic = _masterVolume * (_musicVolume * musicHeadroomScale);
                _musicSource.volume = nominalMusic * _currentDuckMultiplier;
            }
        }

        /// <summary>
        /// Applies low-pass muffling to background audio during critical low-health states (<30% HP).
        /// </summary>
        public void SetLowHealthMuffled(bool active, float cutoffFrequency = 1000f)
        {
            _isLowHealthMuffled = active;
            _lowHealthCutoff = Mathf.Clamp(cutoffFrequency, 400f, 22000f);
        }

        /// <summary>
        /// Triggers dynamic sidechain combat ducking on the background music.
        /// Temporarily attenuates music and applies frequency carving (low-pass filter)
        /// so projectiles, weapon fire, charge blasts, and explosions punch through cleanly.
        /// </summary>
        public void TriggerCombatDucking(float duckAmount = 0.55f, float holdDuration = 0.8f, bool applyLowPass = false)
        {
            // Duck background music to ~50-60% of actual during other sounds
            float clampedDuck = Mathf.Clamp(duckAmount, 0.50f, 0.60f);
            _targetDuckLevel = Mathf.Min(_targetDuckLevel, clampedDuck);
            _combatDuckTimer = Mathf.Max(_combatDuckTimer, holdDuration);
            _targetLowPassCutoff = applyLowPass ? 17000f : 22000f;
        }

        /// <summary>
        /// Enables or disables sustained ducking for continuous combat actions (e.g. weapon charging).
        /// </summary>
        public void SetSustainedDucking(bool active, float duckAmount = 0.55f)
        {
            _isSustainedDucking = active;
            _sustainedDuckLevel = Mathf.Clamp(duckAmount, 0.50f, 0.60f);
            if (!active)
            {
                // Smooth hold buffer after releasing charge
                _combatDuckTimer = Mathf.Max(_combatDuckTimer, 0.2f);
            }
        }
    }
}
