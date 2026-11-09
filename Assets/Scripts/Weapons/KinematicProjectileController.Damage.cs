using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Shootopia
{
    public partial class KinematicProjectileController
    {
private void HandleImpact(RaycastHit hit)
        {
            Vector3 poc = hit.point;
            bool isSplashWeapon = _weaponData != null && (_weaponData.isExplosive || _weaponData.blastRadius > 0.1f);

            if (isSplashWeapon)
            {
                ApplySplashDamage(poc, hit);
            }
            else
            {
                ApplyDirectDamage(hit);
            }

            if (AudioManager.Instance != null)
            {
                float duckAmount = isSplashWeapon ? 0.35f : 0.42f;
                float holdDuration = isSplashWeapon ? 1.2f : 0.8f;
                AudioManager.Instance.TriggerCombatDucking(duckAmount, holdDuration, true);
            }

            SpawnVoidZone(hit);
            SpawnExplosionVFX(hit);

            if (GlobalPoolManager.Instance != null) GlobalPoolManager.Instance.Despawn(gameObject);
            else Destroy(gameObject);
        }

        private void ApplySplashDamage(Vector3 poc, RaycastHit directHit)
        {
            float blastRadius = (_weaponData != null && _weaponData.blastRadius > 0.1f) ? _weaponData.blastRadius : 3.5f;
            float maxDamage = _weaponData != null ? _weaponData.Damage : 25f;
            float minDamage = (_weaponData != null && _weaponData.splashDamage > 0f) ? _weaponData.splashDamage : (maxDamage * 0.25f);

            Collider[] hitColliders = Physics.OverlapSphere(poc, blastRadius);
            var processedTargets = new HashSet<IDamageable>();
            var processedPlayers = new HashSet<PlayerHealth>();
            ulong attackerId = GetAttackerClientId();

            foreach (var col in hitColliders)
            {
                if (col == null) continue;
                bool isOwner = (_owner != null && col.transform.root == _owner.transform.root);
                bool allowSelfDamage = (_weaponData != null && (_weaponData.selfDamage || _weaponData.isExplosive));
                if (isOwner && !allowSelfDamage)
                    continue;

                IDamageable targetDamageable = null;
                var targetHealth = col.GetComponentInParent<PlayerHealth>() ?? col.GetComponent<PlayerHealth>();
                if (targetHealth != null)
                {
                    if (!processedPlayers.Add(targetHealth)) continue;
                }
                else
                {
                    targetDamageable = col.GetComponentInParent<IDamageable>() ?? col.GetComponent<IDamageable>();
                    if (targetDamageable == null || !processedTargets.Add(targetDamageable)) continue;
                }

                if (IsOwnerProjectile())
                {
                    if (targetHealth != null) targetHealth.LastAttackerClientId = attackerId;

                    // Check if this target was directly struck by the projectile (head-on direct hit)
                    bool isDirect = (directHit.collider != null && (col == directHit.collider || col.transform.root == directHit.collider.transform.root))
                                    || (col != null && Vector3.Distance(poc, col.ClosestPoint(poc)) <= 0.05f);

                    float calculatedDamage;
                    float normalizedDist = 0f;

                    if (isDirect)
                    {
                        // Direct contact hit guarantees full center damage (100 = 1-hit KO for Area Blaster, 60 for Freeze Gun)
                        calculatedDamage = maxDamage;
                    }
                    else
                    {
                        Vector3 closestPoint = col.ClosestPoint(poc);
                        float distToPOC = Vector3.Distance(poc, closestPoint);
                        normalizedDist = Mathf.Clamp01(distToPOC / blastRadius);
                        calculatedDamage = Mathf.Lerp(maxDamage, minDamage, normalizedDist);
                        calculatedDamage = Mathf.Clamp(calculatedDamage, minDamage, maxDamage);
                    }

                    if (targetHealth != null)
                    {
                        targetHealth.TakeDamage(calculatedDamage, DamageType.Explosion, _weaponData, attackerId);
                    }
                    else
                    {
                        targetDamageable.TakeDamage(calculatedDamage, DamageType.Explosion, _weaponData);
                    }

                    // Freeze status effect (blocked if target has an active or recently-absorbed shield or is invincible)
                    if (IsFreezeWeapon() && targetHealth != null)
                    {
                        var targetShield = targetHealth.GetComponent<PlayerShield>();
                        bool shieldBlocked = targetShield != null && (targetShield.IsShieldActive || targetShield.WasActiveOrAbsorbedRecently);
                        if (!shieldBlocked && !targetHealth.IsInvincible)
                        {
                            float baseDuration = (_weaponData != null && _weaponData.freezeDuration > 0f) ? _weaponData.freezeDuration : 2.5f;
                            if (isDirect)
                            {
                                targetHealth.ApplyFreezeEffect(baseDuration, 1.0f, true);
                            }
                            else
                            {
                                float maxSlow = (_weaponData != null && _weaponData.maxSlowPercent > 0f) ? _weaponData.maxSlowPercent : 0.80f;
                                float minSlow = (_weaponData != null && _weaponData.minSlowPercent > 0f) ? _weaponData.minSlowPercent : 0.20f;
                                float slowPercent = Mathf.Lerp(maxSlow, minSlow, normalizedDist);
                                float slowDuration = Mathf.Lerp(baseDuration, 1.0f, normalizedDist);
                                targetHealth.ApplyFreezeEffect(slowDuration, slowPercent, false);
                            }
                        }
                    }
                }
            }
        }

        private void ApplyDirectDamage(RaycastHit hit)
        {
            if (!IsOwnerProjectile()) return;

            var targetHealth = hit.collider.GetComponentInParent<PlayerHealth>() ?? hit.collider.GetComponent<PlayerHealth>();
            var damageable = hit.collider.GetComponentInParent<IDamageable>() ?? hit.collider.GetComponent<IDamageable>();

            if (targetHealth != null || damageable != null)
            {
                ulong attackerId = GetAttackerClientId();

                if (targetHealth != null)
                {
                    targetHealth.LastAttackerClientId = attackerId;

                    var targetShield = targetHealth.GetComponent<PlayerShield>();
                    bool shieldBlocked = targetShield != null && (targetShield.IsShieldActive || targetShield.WasActiveOrAbsorbedRecently);

                    if (IsFreezeWeapon() && !shieldBlocked && !targetHealth.IsInvincible)
                    {
                        float baseDuration = (_weaponData != null && _weaponData.freezeDuration > 0f) ? _weaponData.freezeDuration : 2.5f;
                        targetHealth.ApplyFreezeEffect(baseDuration, 1.0f, true);
                    }

                    if (IsFireWeapon() && !shieldBlocked && !targetHealth.IsInvincible)
                    {
                        float duration = _weaponData.afterburnDuration > 0f ? _weaponData.afterburnDuration : 5.0f;
                        float dps = _weaponData.afterburnDPS > 0f ? _weaponData.afterburnDPS : 5.0f;
                        targetHealth.ApplyBurnEffect(duration, dps, _weaponData);
                    }
                }

                float damage = _weaponData != null ? _weaponData.Damage : 10f;
                if (targetHealth != null)
                {
                    targetHealth.TakeDamage(damage, DamageType.Projectile, _weaponData, attackerId);
                }
                else
                {
                    damageable.TakeDamage(damage, DamageType.Projectile, _weaponData);
                }
            }
        }

        private void SpawnVoidZone(RaycastHit hit)
        {
            if (voidZonePrefab == null) return;

            Vector3 spawnPoint = hit.point;
            if (hit.normal.y > 0.7f) spawnPoint += Vector3.up * 0.5f;

            var zone = Instantiate(voidZonePrefab, spawnPoint, Quaternion.identity);
            var zoneScript = zone.GetComponent<VoidZone>();
            if (zoneScript != null && _weaponData != null)
            {
                zoneScript.Initialize(_weaponData, GetAttackerClientId());
            }
        }

        private ulong GetAttackerClientId()
        {
            if (_owner != null)
            {
                var netObj = _owner.GetComponent<NetworkObject>();
                if (netObj != null) return netObj.OwnerClientId;
            }
            return NetworkManager.ServerClientId;
        }

        private bool IsFreezeWeapon()
        {
            if (_weaponData == null) return false;
            return _weaponData.freezeDuration > 0f || _weaponData.maxSlowPercent > 0f ||
                   (!string.IsNullOrEmpty(_weaponData.weaponName) && _weaponData.weaponName.ToLower().Contains("freeze"));
        }

        private bool IsFireWeapon()
        {
            if (_weaponData == null) return false;
            return _weaponData.afterburnDuration > 0f || _weaponData.afterburnDPS > 0f ||
                   (!string.IsNullOrEmpty(_weaponData.weaponName) && (_weaponData.weaponName.ToLower().Contains("flame") || _weaponData.weaponName.ToLower().Contains("fire")));
        }
    }
}
