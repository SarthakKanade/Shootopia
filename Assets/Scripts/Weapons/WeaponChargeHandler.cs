using UnityEngine;

namespace Shootopia
{
    /// <summary>
    /// Manages weapon charging logic, charge accumulation timer, audio pitch ramping,
    /// and dynamic charging visual effects (Luminous Core, Halo, Sparks, and Implosion).
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

            // Play single charge SFX (one-shot play, no loop or trailing sound)
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
            if (data.maxBounces > 0 || (data.weaponName != null && data.weaponName.Contains("Bounce"))) return;

            if (_activeChargeVFX != null)
            {
                Object.Destroy(_activeChargeVFX);
                _activeChargeVFX = null;
            }

            Color chargeColor = GetWeaponThemeColor(data);

            _activeChargeVFX = new GameObject("ChargeVFXContainer");
            _activeChargeVFX.transform.SetParent(muzzle, false);
            _activeChargeVFX.transform.localPosition = Vector3.zero;
            _activeChargeVFX.transform.localRotation = Quaternion.identity;

            Shader urpShader = Shader.Find("Universal Render Pipeline/Unlit") 
                            ?? Shader.Find("Universal Render Pipeline/Particles/Unlit") 
                            ?? Shader.Find("Standard");

            // 1. Inner White-Hot Luminous Core
            GameObject coreObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            coreObj.name = "ChargeCore";
            coreObj.transform.SetParent(_activeChargeVFX.transform, false);
            coreObj.transform.localScale = Vector3.one * 0.015f;
            var coreCol = coreObj.GetComponent<Collider>();
            if (coreCol != null) Object.DestroyImmediate(coreCol);

            var coreMr = coreObj.GetComponent<MeshRenderer>();
            if (coreMr != null && urpShader != null)
            {
                Material coreMat = new Material(urpShader) { name = "ChargeCore_Mat" };
                Color hotWhite = Color.Lerp(chargeColor, Color.white, 0.75f);
                if (coreMat.HasProperty("_BaseColor")) coreMat.SetColor("_BaseColor", hotWhite);
                if (coreMat.HasProperty("_Color")) coreMat.SetColor("_Color", hotWhite);
                coreMat.EnableKeyword("_EMISSION");
                if (coreMat.HasProperty("_EmissionColor")) coreMat.SetColor("_EmissionColor", hotWhite * 4.5f);
                coreMr.material = coreMat;
                coreMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            // 2. Outer Soft Alpha Corona
            GameObject haloObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            haloObj.name = "ChargeHalo";
            haloObj.transform.SetParent(_activeChargeVFX.transform, false);
            haloObj.transform.localScale = Vector3.one * 0.03f;
            var haloCol = haloObj.GetComponent<Collider>();
            if (haloCol != null) Object.DestroyImmediate(haloCol);

            var haloMr = haloObj.GetComponent<MeshRenderer>();
            if (haloMr != null && urpShader != null)
            {
                Material haloMat = new Material(urpShader) { name = "ChargeHalo_AlphaMat", renderQueue = 3050 };
                Color coronaCol = new Color(chargeColor.r, chargeColor.g, chargeColor.b, 0.22f);
                if (haloMat.HasProperty("_BaseColor")) haloMat.SetColor("_BaseColor", coronaCol);
                if (haloMat.HasProperty("_Color")) haloMat.SetColor("_Color", coronaCol);
                haloMat.EnableKeyword("_EMISSION");
                haloMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                haloMat.EnableKeyword("_ALPHABLEND_ON");
                if (haloMat.HasProperty("_EmissionColor")) haloMat.SetColor("_EmissionColor", chargeColor * 2.5f);
                if (haloMat.HasProperty("_Surface")) haloMat.SetFloat("_Surface", 1);
                if (haloMat.HasProperty("_Blend")) haloMat.SetFloat("_Blend", 0);
                if (haloMat.HasProperty("_ZWrite")) haloMat.SetFloat("_ZWrite", 0);
                haloMr.material = haloMat;
                haloMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            // 3. Ready Particles (Crackling Energy Sparks on Full Charge)
            GameObject readyObj = new GameObject("ReadyParticles");
            readyObj.transform.SetParent(_activeChargeVFX.transform, false);
            var readyPs = readyObj.AddComponent<ParticleSystem>();
            readyPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = readyPs.main;
            main.loop = true;
            main.playOnAwake = false;
            main.duration = 0.5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.06f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.startColor = Color.Lerp(chargeColor, Color.white, 0.35f);

            var emission = readyPs.emission;
            emission.rateOverTime = 0f;

            var shape = readyPs.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.12f;

            var psRenderer = readyObj.GetComponent<ParticleSystemRenderer>();
            if (psRenderer != null)
            {
                Shader partShader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");
                if (partShader != null)
                {
                    Material partMat = new Material(partShader) { name = "ReadySparks_Mat", renderQueue = 3100 };
                    if (partMat.HasProperty("_BaseColor")) partMat.SetColor("_BaseColor", chargeColor);
                    if (partMat.HasProperty("_Color")) partMat.SetColor("_Color", chargeColor);
                    partMat.EnableKeyword("_EMISSION");
                    if (partMat.HasProperty("_EmissionColor")) partMat.SetColor("_EmissionColor", chargeColor * 4f);
                    psRenderer.material = partMat;
                }
            }

            // 4. Implosion Particle VFX
            GameObject implosionPrefab = _vfxImplosionPrefab;
            if (implosionPrefab == null)
            {
#if UNITY_EDITOR
                implosionPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/3rdParty/GabrielAguiarProductions/FreeQuickEffectsVol1/Prefabs/vfx_Implosion_01.prefab");
#endif
            }

            if (implosionPrefab != null)
            {
                GameObject implosionInst = Object.Instantiate(implosionPrefab, _activeChargeVFX.transform, false);
                implosionInst.name = "ImplosionVFX";
                implosionInst.transform.localScale = Vector3.one * 0.175f;

                // Dynamically sync implosion particle simulation speed with data.chargeTime
                float defaultDuration = 0.85f;
                float simSpeed = defaultDuration / Mathf.Max(data.chargeTime, 0.1f);

                foreach (var ps in implosionInst.GetComponentsInChildren<ParticleSystem>(true))
                {
                    var pMain = ps.main;
                    pMain.simulationSpeed = simSpeed;
                    Color pCol = chargeColor;
                    pCol.a = pMain.startColor.color.a > 0.01f ? pMain.startColor.color.a : 0.8f;
                    pMain.startColor = pCol;
                }
            }
        }

        private void UpdateChargeVisual(WeaponSO data, float t)
        {
            if (_activeChargeVFX == null || data == null) return;

            Color chargeColor = GetWeaponThemeColor(data);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            // Core scaling
            Transform coreTr = _activeChargeVFX.transform.Find("ChargeCore");
            if (coreTr != null)
            {
                float maxCoreScale = 0.14f;
                float currentScale = Mathf.Lerp(0.015f, maxCoreScale, smoothT);
                if (t >= 1.0f)
                {
                    float pulse = Mathf.Sin(Time.time * 24f) * 0.012f;
                    coreTr.localScale = Vector3.one * (maxCoreScale + pulse);
                }
                else
                {
                    coreTr.localScale = Vector3.one * currentScale;
                }
            }

            // Corona scaling
            Transform haloTr = _activeChargeVFX.transform.Find("ChargeHalo");
            if (haloTr != null)
            {
                float maxHaloScale = 0.26f;
                float currentScale = Mathf.Lerp(0.03f, maxHaloScale, smoothT);
                if (t >= 1.0f)
                {
                    float pulse = Mathf.Sin(Time.time * 20f + 1.2f) * 0.02f;
                    haloTr.localScale = Vector3.one * (maxHaloScale + pulse);
                }
                else
                {
                    haloTr.localScale = Vector3.one * currentScale;
                }
            }

            // Sparks particle trigger on 100% charge
            Transform readyTr = _activeChargeVFX.transform.Find("ReadyParticles");
            if (readyTr != null)
            {
                var readyPs = readyTr.GetComponent<ParticleSystem>();
                if (readyPs != null)
                {
                    var em = readyPs.emission;
                    if (t >= 1.0f)
                    {
                        if (!readyPs.isPlaying)
                        {
                            readyPs.Play();
                            readyPs.Emit(18);
                        }
                        em.rateOverTime = 40f;
                    }
                    else
                    {
                        em.rateOverTime = 0f;
                    }
                }
            }

            // Implosion scale
            Transform implosionTr = _activeChargeVFX.transform.Find("ImplosionVFX");
            if (implosionTr != null)
            {
                implosionTr.localScale = Vector3.one * Mathf.Lerp(0.08f, 0.22f, smoothT);
            }
        }

        public static Color GetWeaponThemeColor(WeaponSO data)
        {
            if (data == null) return new Color(0.3f, 0.7f, 1.0f, 0.9f);

            string name = (data.weaponName + " " + (data.name ?? "")).ToLowerInvariant();

            if (name.Contains("void") || name.Contains("gravity"))
                return new Color(0.55f, 0.08f, 0.95f, 0.7f); // Deep Cosmic Purple
            if (name.Contains("area") || name.Contains("blaster") || name.Contains("explosive"))
                return new Color(1.0f, 0.25f, 0.02f, 0.7f); // Fiery Red-Orange
            if (name.Contains("freeze") || name.Contains("cryo") || name.Contains("ice"))
                return new Color(0.15f, 0.85f, 1.0f, 0.65f); // Cyan Frost
            if (name.Contains("trick") || name.Contains("ricochet") || name.Contains("bounce"))
                return new Color(0.15f, 1.0f, 0.35f, 0.65f); // Neon Green
            if (name.Contains("sniper"))
                return new Color(1.0f, 0.85f, 0.15f, 0.7f); // Amber / Yellow

            return new Color(0.3f, 0.7f, 1.0f, 0.8f); // Default electric blue
        }
    }
}
