using UnityEngine;

namespace Shootopia
{
    /// <summary>
    /// Lightweight, production-ready weapon charge handler.
    /// Manages charge timer, audio feedback, and muzzle charge visual effects
    /// using pre-configured prefabs and theme colors without procedural runtime allocation.
    /// </summary>
    public class WeaponChargeHandler
    {
        private readonly GameObject _vfxImplosionPrefab;
        private GameObject _activeChargeVFX;
        private AudioSource _activeChargeAudioSource;

        public bool IsCharging { get; private set; }
        public float ChargeTimer { get; private set; }

        public WeaponChargeHandler(GameObject vfxImplosionPrefab)
        {
            _vfxImplosionPrefab = vfxImplosionPrefab;
        }

        public void StartCharge(Transform muzzle, WeaponSO data)
        {
            IsCharging = true;
            ChargeTimer = 0f;

            if (data == null || muzzle == null) return;

            StartChargeVisual(muzzle, data);

            // Play one-shot charge SFX
            if (data.chargeTime > 0f && data.audio != null && AudioManager.Instance != null)
            {
                AudioClip clip = data.audio.chargeClip != null ? data.audio.chargeClip : data.audio.drawClip;
                if (clip != null)
                {
                    _activeChargeAudioSource = AudioManager.Instance.PlayLoopingSFX(clip, muzzle, 0.85f, 1.0f);
                    if (_activeChargeAudioSource != null)
                    {
                        _activeChargeAudioSource.loop = false;
                    }
                }
            }
        }

        public void UpdateCharge(float dt, WeaponSO data)
        {
            if (!IsCharging || data == null) return;

            ChargeTimer += dt;
            float t = Mathf.Clamp01(ChargeTimer / Mathf.Max(data.chargeTime, 0.01f));

            UpdateChargeVisual(data, t);
        }

        public bool IsFullyCharged(WeaponSO data)
        {
            if (data == null || data.chargeTime <= 0f) return true;
            return ChargeTimer >= data.chargeTime;
        }

        public void CancelCharge()
        {
            IsCharging = false;
            ChargeTimer = 0f;

            if (_activeChargeVFX != null)
            {
                Object.Destroy(_activeChargeVFX);
                _activeChargeVFX = null;
            }

            if (_activeChargeAudioSource != null)
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.StopLoopingSFX(_activeChargeAudioSource);
                }
                _activeChargeAudioSource = null;
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetSustainedDucking(false);
            }
        }

        private void StartChargeVisual(Transform muzzle, WeaponSO data)
        {
            if (muzzle == null || data == null || data.chargeTime <= 0f) return;
            if (data.maxBounces > 0) return; // Bounce weapons use laser trajectory line, not muzzle charge flare

            if (_activeChargeVFX != null)
            {
                Object.Destroy(_activeChargeVFX);
                _activeChargeVFX = null;
            }

            GameObject prefabToSpawn = data.chargeVFXPrefab != null ? data.chargeVFXPrefab : _vfxImplosionPrefab;
            if (prefabToSpawn == null) return;

            _activeChargeVFX = Object.Instantiate(prefabToSpawn, muzzle, false);
            _activeChargeVFX.transform.localPosition = Vector3.zero;
            _activeChargeVFX.transform.localRotation = Quaternion.identity;
            _activeChargeVFX.transform.localScale = Vector3.one * 0.08f;

            Color themeColor = GetWeaponThemeColor(data);

            // Dynamically sync particle simulation speed and tint with charge time and weapon theme
            float defaultDuration = 0.85f;
            float simSpeed = defaultDuration / Mathf.Max(data.chargeTime, 0.1f);

            foreach (var ps in _activeChargeVFX.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main;
                main.simulationSpeed = simSpeed;
                main.startColor = themeColor;
                ps.Play();
            }

            foreach (var r in _activeChargeVFX.GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                if (r.material != null)
                {
                    if (r.material.HasProperty("_Color")) r.material.SetColor("_Color", themeColor * 2.5f);
                    if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", themeColor);
                }
            }

            var chargeLight = _activeChargeVFX.GetComponent<Light>();
            if (chargeLight == null) chargeLight = _activeChargeVFX.AddComponent<Light>();
            chargeLight.color = themeColor;
            chargeLight.range = 3.5f;
            chargeLight.intensity = 1.5f;
            chargeLight.shadows = LightShadows.None;
        }

        private void UpdateChargeVisual(WeaponSO data, float t)
        {
            if (_activeChargeVFX == null) return;

            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            float baseScale = Mathf.Lerp(0.08f, 0.22f, smoothT);

            if (t >= 1.0f)
            {
                float pulse = Mathf.Sin(Time.time * 24f) * 0.015f;
                _activeChargeVFX.transform.localScale = Vector3.one * (baseScale + pulse);
            }
            else
            {
                _activeChargeVFX.transform.localScale = Vector3.one * baseScale;
            }

            var chargeLight = _activeChargeVFX.GetComponent<Light>();
            if (chargeLight != null)
            {
                chargeLight.intensity = Mathf.Lerp(1.0f, 3.5f, smoothT);
            }
        }

        public static Color GetWeaponThemeColor(WeaponSO data)
        {
            if (data == null) return new Color(1.0f, 0.35f, 0.03f, 1.0f);

            // 1. Specific elemental signatures take priority
            if (data.voidDuration > 0f || (data.weaponName != null && data.weaponName.IndexOf("Void", System.StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return new Color(0.78f, 0.15f, 0.95f, 1.0f); // Cosmic purple
            }
            if (data.freezeDuration > 0f || (data.weaponName != null && data.weaponName.IndexOf("Freeze", System.StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return new Color(0.2f, 0.85f, 1.0f, 1.0f); // Ice cyan
            }

            // 2. Custom SO theme color if explicitly defined and distinct from default
            if (data.themeColor.a > 0.05f &&
                !(Mathf.Approximately(data.themeColor.r, 0.3f) && 
                  Mathf.Approximately(data.themeColor.g, 0.7f) && 
                  Mathf.Approximately(data.themeColor.b, 1.0f)))
            {
                return data.themeColor;
            }

            // 3. Area / Blaster / Explosive signatures
            if (data.isExplosive || (data.weaponName != null && (data.weaponName.IndexOf("Area", System.StringComparison.OrdinalIgnoreCase) >= 0 || data.weaponName.IndexOf("Blaster", System.StringComparison.OrdinalIgnoreCase) >= 0)))
            {
                return new Color(1.0f, 0.35f, 0.03f, 1.0f); // Fiery blast orange-red for Area Blaster
            }

            return data.themeColor;
        }
    }
}
