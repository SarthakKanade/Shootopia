using System.Collections.Generic;
using UnityEngine;

namespace Shootopia
{
    public partial class PlayerInvisibility
    {
private void ApplyInvisibilityVisuals(bool invisible)
        {
            if (_nameplate == null)
            {
                _nameplate = GetComponentInChildren<PlayerNameplate>(true);
            }

            var bodyRenderers = GetBodyRenderers();

            if (invisible)
            {
                if (IsOwner)
                {
                    // Local player visual feedback: keep renderers enabled, swap to translucent stealth material
                    foreach (var r in bodyRenderers)
                    {
                        if (r == null) continue;
                        if (!_originalMaterials.ContainsKey(r))
                        {
                            _originalMaterials[r] = r.sharedMaterials;
                        }

                        if (stealthTranslucentMaterial != null)
                        {
                            int matCount = r.sharedMaterials.Length;
                            Material[] stealthMats = new Material[matCount];
                            for (int i = 0; i < matCount; i++)
                            {
                                stealthMats[i] = stealthTranslucentMaterial;
                            }
                            r.sharedMaterials = stealthMats;
                        }
                        r.enabled = true;
                    }
                }
                else
                {
                    // Remote clients: hide player body renderers completely
                    foreach (var r in bodyRenderers)
                    {
                        if (r != null) r.enabled = false;
                    }

                    // Hide overhead nameplate
                    if (_nameplate != null)
                    {
                        _nameplate.SetInvisibilityHidden(true);
                    }
                }
            }
            else
            {
                RestoreNormalVisuals();
            }
        }

        private void RestoreNormalVisuals()
        {
            var bodyRenderers = GetBodyRenderers();

            if (IsOwner)
            {
                foreach (var r in bodyRenderers)
                {
                    if (r == null) continue;
                    if (_originalMaterials.TryGetValue(r, out var origMats) && origMats != null)
                    {
                        r.sharedMaterials = origMats;
                    }
                    r.enabled = true;
                }
                _originalMaterials.Clear();
            }
            else
            {
                foreach (var r in bodyRenderers)
                {
                    if (r != null) r.enabled = true;
                }

                if (_nameplate != null)
                {
                    _nameplate.SetInvisibilityHidden(false);
                }
            }
        }

        /// <summary>
        /// Collects all character body renderers, strictly excluding:
        /// - Weapon socket children (so held weapons remain visible!)
        /// - UI canvases
        /// - Shield forcefield spheres
        /// </summary>
        private List<Renderer> _cachedBodyRenderers;

        private List<Renderer> GetBodyRenderers()
        {
            if (_cachedBodyRenderers != null && _cachedBodyRenderers.Count > 0)
            {
                return _cachedBodyRenderers;
            }

            _cachedBodyRenderers = new List<Renderer>();

            Transform weaponSocket = _weaponController != null ? _weaponController.WeaponSocket : null;
            if (weaponSocket == null)
            {
                var socketObj = transform.Find("WeaponSocket_R") ?? transform.Find("weaponSocket");
                if (socketObj != null) weaponSocket = socketObj;
            }
            var allRenderers = GetComponentsInChildren<Renderer>(true);

            foreach (var r in allRenderers)
            {
                if (r == null) continue;

                // Keep weapons in hand visible at all times
                if (weaponSocket != null && r.transform.IsChildOf(weaponSocket)) continue;

                // Exclude UI Canvas renderers (e.g. overhead health bars)
                if (r.GetComponentInParent<Canvas>() != null) continue;

                // Exclude Shield sphere renderers
                if (r.name.Contains("Shield") || (r.transform.parent != null && r.transform.parent.name.Contains("Shield"))) continue;

                _cachedBodyRenderers.Add(r);
            }

            return _cachedBodyRenderers;
        }

        private void OnDisable()
        {
            RestoreNormalVisuals();
        }
    }
}
