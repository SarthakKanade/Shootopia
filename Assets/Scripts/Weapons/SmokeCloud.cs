using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Shootopia
{
    public class SmokeCloud : MonoBehaviour
    {
        private WeaponSO _data;
        private int _ownerID;
        private float _radius = 5.5f;
        private float _duration = 8f;
        private float _dps = 24f;
        private const float TICK_INTERVAL = 0.5f;
        private Coroutine _cloudRoutine;

        public void Initialize(WeaponSO data, int ownerID)
        {
            _data = data;
            _ownerID = ownerID;
            _radius = _data != null && _data.cloudRadius > 0.1f ? _data.cloudRadius : 5.5f;
            _duration = _data != null && _data.cloudDuration > 0.1f ? _data.cloudDuration : 8f;
            _dps = _data != null && _data.damagePerSecond > 0f ? _data.damagePerSecond : (_data != null && _data.Damage > 0 ? _data.Damage : 24f);

            // Configure trigger collider to match radius
            var sc = GetComponent<SphereCollider>();
            if (sc == null) sc = gameObject.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = _radius;

            // Align ParticleSystem shape radius so visual cloud bounds match the damage radius 1:1
            var ps = GetComponentInChildren<ParticleSystem>();
            if (ps != null)
            {
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = _radius * 0.7f; // Particles expand outward from emission core to exactly fill _radius
                if (!ps.isPlaying) ps.Play();
            }

            if (_cloudRoutine != null) StopCoroutine(_cloudRoutine);
            _cloudRoutine = StartCoroutine(CloudRoutine());
        }

        private IEnumerator CloudRoutine()
        {
            float elapsed = 0f;
            float tickTimer = 0f;
            float damagePerTick = _dps * TICK_INTERVAL;
            int mask = ~LayerMask.GetMask("Ignore Raycast", "UI");

            while (elapsed < _duration)
            {
                elapsed += Time.fixedDeltaTime;
                tickTimer += Time.fixedDeltaTime;

                if (tickTimer >= TICK_INTERVAL)
                {
                    tickTimer = 0f;

                    bool isServerOrOffline = NetworkManager.Singleton == null || NetworkManager.Singleton.IsServer;
                    if (isServerOrOffline)
                    {
                        // Skip damage during warmup if match is not active
                        if (GameModeBase.Instance != null && !GameModeBase.Instance.MatchActive.Value)
                        {
                            yield return new WaitForFixedUpdate();
                            continue;
                        }

                        Collider[] hits = Physics.OverlapSphere(transform.position, _radius, mask, QueryTriggerInteraction.Ignore);
                        var damagedEntities = new HashSet<IDamageable>();
                        var damagedPlayers = new HashSet<PlayerHealth>();

                        for (int i = 0; i < hits.Length; i++)
                        {
                            var hit = hits[i];
                            if (hit == null) continue;

                            var ph = hit.GetComponentInParent<PlayerHealth>() ?? hit.GetComponent<PlayerHealth>();
                            IDamageable damageable = null;
                            if (ph != null)
                            {
                                if (!damagedPlayers.Add(ph)) continue;
                                ph.LastAttackerClientId = (ulong)_ownerID;
                                damageable = ph;
                            }
                            else
                            {
                                damageable = hit.GetComponentInParent<IDamageable>() ?? hit.GetComponent<IDamageable>();
                                if (damageable == null || !damagedEntities.Add(damageable)) continue;
                            }

                            if (ph != null)
                            {
                                ph.TakeDamage(damagePerTick, DamageType.Projectile, _data, (ulong)_ownerID);
                            }
                            else
                            {
                                damageable.TakeDamage(damagePerTick, DamageType.Projectile, _data);
                            }
                        }
                    }
                }

                yield return new WaitForFixedUpdate();
            }

            // Dissipate smoke
            var psEnd = GetComponentInChildren<ParticleSystem>();
            if (psEnd != null) psEnd.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            Destroy(gameObject, 2.5f);
        }
    }
}
