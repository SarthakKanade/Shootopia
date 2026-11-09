using System.Collections;
using UnityEngine;
using Unity.Netcode;

namespace Shootopia
{
    public partial class PlayerHealth
    {
private void OnInvincibleNetChanged(bool previousValue, bool newValue)
        {
            ShowInvincibilityAura(newValue);
        }

        public void StartInvincibilityServer(float duration = 5.0f)
        {
            if (!IsServer) return;
            if (_serverInvincibilityRoutine != null) StopCoroutine(_serverInvincibilityRoutine);
            _serverInvincibilityRoutine = StartCoroutine(ServerInvincibilityRoutine(duration));
        }

        private IEnumerator ServerInvincibilityRoutine(float duration)
        {
            _isInvincibleNet.Value = true;
            yield return new WaitForSeconds(duration);
            _isInvincibleNet.Value = false;
            _serverInvincibilityRoutine = null;
        }

        public void StartInvincibilityLocal(float duration)
        {
            _localInvincible = true;
            if (_localInvincibilityRoutine != null) StopCoroutine(_localInvincibilityRoutine);
            _localInvincibilityRoutine = StartCoroutine(LocalInvincibilityRoutine(duration));
        }

        private IEnumerator LocalInvincibilityRoutine(float duration)
        {
            _localInvincible = true;
            ShowInvincibilityAura(true);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (_isDead)
                {
                    ShowInvincibilityAura(false);
                    _localInvincible = false;
                    _localInvincibilityRoutine = null;
                    yield break;
                }

                float remaining = duration - elapsed;
                if (remaining < 1.2f && _invincibilityAuraInstance != null)
                {
                    bool blinkVisible = (Mathf.FloorToInt(remaining * 8f) % 2) == 0;
                    _invincibilityAuraInstance.SetActive(blinkVisible);
                }
                else if (_invincibilitySkinMaterial != null)
                {
                    float pulse = 0.70f + Mathf.Sin(elapsed * 5.0f) * 0.20f;
                    _invincibilitySkinMaterial.SetColor("_EmissionColor", new Color(1.0f, 0.72f, 0.20f) * pulse);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            ShowInvincibilityAura(false);
            _localInvincible = false;
            _localInvincibilityRoutine = null;
        }

        private Material GetOrCreateInvincibilityMaterial()
        {
            if (_invincibilitySkinMaterial == null)
            {
                Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Unlit");
                _invincibilitySkinMaterial = new Material(s);
                _invincibilitySkinMaterial.name = "InvincibilitySkinMat_Runtime";
                _invincibilitySkinMaterial.SetFloat("_Surface", 1.0f); // Transparent
                _invincibilitySkinMaterial.SetFloat("_Blend", 0.0f);   // 0 = Alpha blend for translucent see-through
                _invincibilitySkinMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                _invincibilitySkinMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                _invincibilitySkinMaterial.SetInt("_ZWrite", 0);
                _invincibilitySkinMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                _invincibilitySkinMaterial.EnableKeyword("_EMISSION");
                _invincibilitySkinMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 100;
                // Soft translucent amber-gold sheen (highly see-through, letting character clothes/textures show clearly)
                _invincibilitySkinMaterial.SetColor("_BaseColor", new Color(1.0f, 0.82f, 0.25f, 0.20f));
                _invincibilitySkinMaterial.SetColor("_EmissionColor", new Color(1.0f, 0.72f, 0.20f) * 0.75f);
            }
            return _invincibilitySkinMaterial;
        }

        private void ShowInvincibilityAura(bool show)
        {
            if (show)
            {
                if (_invincibilityAuraInstance == null || _invincibilityAuraInstance.transform.childCount == 0)
                {
                    if (_invincibilityAuraInstance != null)
                    {
                        Destroy(_invincibilityAuraInstance);
                    }

                    // Body-hugging translucent skin overlay across the player's active model itself (COD / PUBG spawn protection style)
                    _invincibilityAuraInstance = new GameObject("SpawnInvincibilitySkinLayer");
                    _invincibilityAuraInstance.transform.SetParent(transform, false);
                    _invincibilityAuraInstance.transform.localPosition = Vector3.zero;
                    _invincibilityAuraInstance.transform.localRotation = Quaternion.identity;
                    _invincibilityAuraInstance.transform.localScale = Vector3.one;

                    var mat = GetOrCreateInvincibilityMaterial();

                    // 1. SkinnedMeshRenderers (body, head, limbs) - strictly matching only the currently ACTIVE model (Vanguard or Valkyrie)
                    var smrs = GetComponentsInChildren<SkinnedMeshRenderer>(true);
                    foreach (var smr in smrs)
                    {
                        if (smr == null || smr.transform.IsChildOf(_invincibilityAuraInstance.transform)) continue;
                        if (!smr.gameObject.activeInHierarchy || !smr.enabled) continue; // Skip inactive character variants

                        var overlayObj = new GameObject($"{smr.gameObject.name}_SkinOverlay");
                        overlayObj.transform.SetParent(_invincibilityAuraInstance.transform, false);

                        var overlaySmr = overlayObj.AddComponent<SkinnedMeshRenderer>();
                        overlaySmr.sharedMesh = smr.sharedMesh;
                        overlaySmr.bones = smr.bones;
                        overlaySmr.rootBone = smr.rootBone;
                        overlaySmr.material = mat;
                        overlaySmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        overlaySmr.receiveShadows = false;
                    }

                    // 2. MeshRenderers (helmets, attachments, props if any)
                    var mrs = GetComponentsInChildren<MeshRenderer>(true);
                    foreach (var mr in mrs)
                    {
                        if (mr == null || mr.transform.IsChildOf(_invincibilityAuraInstance.transform)) continue;
                        if (!mr.gameObject.activeInHierarchy || !mr.enabled) continue;
                        var mf = mr.GetComponent<MeshFilter>();
                        if (mf == null || mf.sharedMesh == null) continue;

                        var overlayObj = new GameObject($"{mr.gameObject.name}_SkinOverlay");
                        overlayObj.transform.SetParent(mr.transform, false);
                        overlayObj.transform.localPosition = Vector3.zero;
                        overlayObj.transform.localRotation = Quaternion.identity;
                        overlayObj.transform.localScale = Vector3.one;

                        var overlayFilter = overlayObj.AddComponent<MeshFilter>();
                        overlayFilter.sharedMesh = mf.sharedMesh;

                        var overlayRend = overlayObj.AddComponent<MeshRenderer>();
                        overlayRend.material = mat;
                        overlayRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        overlayRend.receiveShadows = false;
                    }
                }

                _invincibilityAuraInstance.SetActive(true);
            }
            else
            {
                if (_invincibilityAuraInstance != null)
                {
                    _invincibilityAuraInstance.SetActive(false);
                }
            }
        }

private IEnumerator InvincibilityFlashRoutine()
        {
            float duration = 1.5f;
            float interval = 0.1f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (_isDead) yield break;
                bool visible = (Mathf.FloorToInt(elapsed / interval) % 2) == 0;
                ToggleRenderers(visible);
                yield return new WaitForSeconds(interval);
                elapsed += interval;
            }

            if (!_isDead) ToggleRenderers(true);
            _invincibilityFlashRoutine = null;
        }

    }
}
