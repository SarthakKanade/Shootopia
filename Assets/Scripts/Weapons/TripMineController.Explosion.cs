using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Shootopia
{
    public partial class TripMineController
    {
private void CheckTrigger(Collider col)
        {
            if (!_armed || _isTriggered || _exploded || col == null) return;

            var targetHealth = col.GetComponentInParent<PlayerHealth>() ?? col.GetComponent<PlayerHealth>();
            if (targetHealth != null)
            {
                if (IsFriendlyTarget(targetHealth)) return;

                bool isOwner = (targetHealth.OwnerClientId == (ulong)_ownerID);
                bool allowSelfTrigger = (_data != null && _data.selfTrigger);
                if (!isOwner || allowSelfTrigger)
                {
                    StartTriggerSequence();
                }
            }
            else if (col.GetComponent<IDamageable>() != null || col.GetComponentInParent<IDamageable>() != null)
            {
                StartTriggerSequence();
            }
        }

        private void StartTriggerSequence()
        {
            if (_isTriggered || _exploded) return;
            _isTriggered = true;

            if (_proximitySensor != null) _proximitySensor.enabled = false;
            if (_pulseRoutine != null) StopCoroutine(_pulseRoutine);

            StartCoroutine(TriggerBufferRoutine());
        }

        private IEnumerator TriggerBufferRoutine()
        {
            // Very small buffer after trigger and before blast with rapid warning blink
            float bufferDuration = 0.35f;
            float blinkInterval = 0.07f;
            float elapsed = 0f;
            bool blinkOn = true;

            if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
            Color warningColor = new Color(1.0f, 0.1f, 0.1f);

            while (elapsed < bufferDuration)
            {
                blinkOn = !blinkOn;
                if (_armRing != null)
                {
                    _propBlock.SetColor(HashEmissionColor, blinkOn ? warningColor * 4.5f : Color.black);
                    _armRing.SetPropertyBlock(_propBlock);
                }
                if (_trapLight != null)
                {
                    _trapLight.enabled = blinkOn;
                    _trapLight.color = warningColor;
                    _trapLight.intensity = blinkOn ? 3.5f : 0f;
                }

                yield return new WaitForSeconds(blinkInterval);
                elapsed += blinkInterval;
            }

            Explode();
        }

        private bool IsFriendlyTarget(PlayerHealth targetHealth)
        {
            if (targetHealth == null) return false;
            if (targetHealth.OwnerClientId == (ulong)_ownerID)
            {
                return !(_data != null && _data.selfTrigger);
            }

            if (GameModeBase.Instance is TDMMode tdm)
            {
                int targetTeam = tdm.GetPlayerTeam(targetHealth.OwnerClientId);
                int ownerTeam = tdm.GetPlayerTeam((ulong)_ownerID);
                if (targetTeam != -1 && ownerTeam != -1 && targetTeam == ownerTeam)
                {
                    return true;
                }
            }
            return false;
        }

        public void Explode()
        {
            if (_exploded) return;
            _exploded = true;

            float radius = _data != null && _data.blastRadius > 0.1f ? _data.blastRadius : 4.5f;
            float maxDmg = _data != null ? _data.Damage : 100f;
            float minDmg = _data != null && _data.splashDamage > 0f ? _data.splashDamage : 40f;
            float triggerRadius = _data != null && _data.triggerRadius > 0.1f ? _data.triggerRadius : 2.2f;
            Vector3 poc = transform.position;

            Collider[] hits = Physics.OverlapSphere(poc, radius);
            var damagedTargets = new System.Collections.Generic.HashSet<IDamageable>();
            var damagedPlayers = new System.Collections.Generic.HashSet<PlayerHealth>();

            foreach (Collider c in hits)
            {
                if (c == null) continue;
                var targetHealth = c.GetComponentInParent<PlayerHealth>() ?? c.GetComponent<PlayerHealth>();
                var targetDamageable = c.GetComponentInParent<IDamageable>() ?? c.GetComponent<IDamageable>();
                if (targetDamageable == null) continue;

                if (targetHealth != null)
                {
                    if (!damagedPlayers.Add(targetHealth)) continue;
                }
                else
                {
                    if (!damagedTargets.Add(targetDamageable)) continue;
                }

                bool isServerOrOffline = Unity.Netcode.NetworkManager.Singleton == null || Unity.Netcode.NetworkManager.Singleton.IsServer;
                if (isServerOrOffline)
                {
                    if (targetHealth != null)
                    {
                        targetHealth.LastAttackerClientId = (ulong)_ownerID;
                    }

                    Vector3 closestPt;
                    try { closestPt = c.ClosestPoint(poc); }
                    catch { closestPt = c.bounds.ClosestPoint(poc); }

                    float dist = Vector3.Distance(poc, closestPt);

                    // If target tripped the mine (within trigger radius), guarantee max 100 damage kill; smooth falloff to minDmg towards blast radius
                    float calculatedDamage;
                    if (dist <= triggerRadius)
                    {
                        calculatedDamage = maxDmg;
                    }
                    else
                    {
                        float normDist = Mathf.Clamp01((dist - triggerRadius) / Mathf.Max(0.1f, radius - triggerRadius));
                        calculatedDamage = Mathf.Lerp(maxDmg, minDmg, normDist);
                    }
                    calculatedDamage = Mathf.Clamp(calculatedDamage, minDmg, maxDmg);

                    if (targetHealth != null)
                    {
                        targetHealth.TakeDamage(calculatedDamage, DamageType.Explosion, _data, (ulong)_ownerID);
                    }
                    else
                    {
                        targetDamageable.TakeDamage(calculatedDamage, DamageType.Explosion, _data);
                    }
                }
            }

            var vfxToSpawn = explosionVFX != null ? explosionVFX : (_data != null ? _data.HitVFXPrefab : null);
            if (vfxToSpawn != null)
            {
                Vector3 blastNormal = transform.up;
                if (Physics.Raycast(poc + Vector3.up * 0.2f, Vector3.down, out RaycastHit gHit, 1.5f))
                {
                    blastNormal = gHit.normal;
                }
                else if (blastNormal == Vector3.zero)
                {
                    blastNormal = Vector3.up;
                }

                Quaternion rot = Mathf.Abs(blastNormal.y) > 0.95f
                    ? Quaternion.LookRotation(blastNormal, Vector3.forward)
                    : Quaternion.LookRotation(blastNormal, Vector3.up);
                GameObject vfx = Instantiate(vfxToSpawn, poc, rot);
                vfx.transform.localScale = Vector3.one * 3.5f;

                foreach (var ps in vfx.GetComponentsInChildren<ParticleSystem>(true))
                {
                    var main = ps.main;
                    main.simulationSpeed *= 0.4f;
                }
            }

            CameraShakeManager.Instance?.ShakeExplosion(poc, radius, 1.3f);
            MobileHapticsManager.TriggerExplosion();

            if (TripMineManager.Instance != null)
                TripMineManager.Instance.Unregister(this);

            Destroy(gameObject);
        }

        public void ForceExplode() => Explode();
        
        private void OnDestroy()
        {
            if (TripMineManager.Instance != null)
                TripMineManager.Instance.Unregister(this);
        }
    }
}
