using UnityEngine;
using System.Collections;
using Shootopia;
using Unity.Cinemachine;
using Unity.Netcode;

namespace Shootopia
{
    public class GrenadeController : TrapBase
    {
        [SerializeField] private GameObject explosionVFX;
        [SerializeField] private Light _blinkLight;
        [SerializeField] private CinemachineImpulseSource impulseSource;

        private Rigidbody rb;
        private bool _exploded = false;

        void Awake()
        {
            if (_blinkLight == null) _blinkLight = GetComponentInChildren<Light>();
            if (_blinkLight != null) _blinkLight.enabled = false;
            if (impulseSource == null) impulseSource = GetComponent<CinemachineImpulseSource>();
            if (impulseSource == null) impulseSource = gameObject.AddComponent<CinemachineImpulseSource>();
        }

        private WeaponSO _data;

        protected override void OnInitialised(WeaponSO data, Vector3 launchVelocity)
        {
            _data = data;
            rb = GetComponent<Rigidbody>();
            
            if (rb != null)
            {
                if (launchVelocity.sqrMagnitude > 0.01f)
                {
                    rb.linearVelocity = launchVelocity;
                }
                else if (_cam != null)
                {
                    float angle = data != null ? data.throwAngle : 20f;
                    float force = data != null ? data.throwForce : 14f;
                    Vector3 throwDir = Quaternion.AngleAxis(-angle, _cam.transform.right) * _cam.transform.forward;
                    rb.linearVelocity = throwDir * force;
                }
            }

            StartCoroutine(FuseRoutine());
            StartCoroutine(BlinkRoutine());
        }

        private IEnumerator FuseRoutine()
        {
            float fuse = _data != null ? _data.fuseTime : 2.5f;
            yield return new WaitForSeconds(fuse);
            Explode();
        }

        private IEnumerator BlinkRoutine()
        {
            float start = Time.time;
            float fuseTime = _data != null ? _data.fuseTime : 2.5f;
            while (!_exploded)
            {
                float elapsed = Time.time - start;
                float interval = Mathf.Lerp(0.5f, 0.1f, elapsed / fuseTime);
                if (_blinkLight != null)
                {
                    _blinkLight.enabled = !_blinkLight.enabled;
                }
                yield return new WaitForSeconds(interval);
            }
        }

        private void Explode()
        {
            if (_exploded) return;
            _exploded = true;
            
            float blastRadius = _data != null && _data.blastRadius > 0.1f ? _data.blastRadius : 4.5f;
            float maxDamage = _data != null ? _data.Damage : 100f;
            float minDamage = _data != null && _data.splashDamage > 0f ? _data.splashDamage : 25f;
            Vector3 poc = transform.position;

            Collider[] hits = Physics.OverlapSphere(poc, blastRadius);
            var damagedTargets = new System.Collections.Generic.HashSet<IDamageable>();

            foreach (Collider c in hits)
            {
                var t = c.GetComponentInParent<IDamageable>() ?? c.GetComponent<IDamageable>();
                if (t != null)
                {
                    if (!damagedTargets.Add(t)) continue;

                    bool isServerOrOffline = Unity.Netcode.NetworkManager.Singleton == null || Unity.Netcode.NetworkManager.Singleton.IsServer;
                    if (isServerOrOffline)
                    {
                        var targetHealth = c.GetComponentInParent<PlayerHealth>() ?? c.GetComponent<PlayerHealth>();
                        if (targetHealth != null)
                        {
                            targetHealth.LastAttackerClientId = (ulong)_ownerID;
                        }

                        // Distance from grenade POC to closest point on target collider (matching Area Blaster damage model)
                        Vector3 closestPt = c.ClosestPoint(poc);
                        float dist = Vector3.Distance(poc, closestPt);

                        bool isDirect = dist <= 0.05f;
                        float calculatedDamage;
                        if (isDirect)
                        {
                            calculatedDamage = maxDamage;
                        }
                        else
                        {
                            float normalizedDist = Mathf.Clamp01(dist / blastRadius);
                            calculatedDamage = Mathf.Lerp(maxDamage, minDamage, normalizedDist);
                            calculatedDamage = Mathf.Clamp(calculatedDamage, minDamage, maxDamage);
                        }

                        if (targetHealth != null)
                        {
                            targetHealth.TakeDamage(calculatedDamage, DamageType.Explosion, _data, (ulong)_ownerID);
                        }
                        else
                        {
                            t.TakeDamage(calculatedDamage, DamageType.Explosion, _data);
                        }
                    }
                }
            }

            var vfxToSpawn = explosionVFX != null ? explosionVFX : (_data != null ? _data.HitVFXPrefab : null);
            if (vfxToSpawn != null)
            {
                Vector3 blastNormal = Vector3.up;
                if (Physics.Raycast(poc + Vector3.up * 0.2f, Vector3.down, out RaycastHit gHit, 1.5f))
                {
                    blastNormal = gHit.normal;
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

            if (impulseSource != null)
            {
                impulseSource.GenerateImpulse(0.35f);
            }
            else
            {
                CameraShakeManager.Instance?.ShakeExplosion(poc, blastRadius, 1.3f);
            }

            MobileHapticsManager.TriggerExplosion();
            Destroy(gameObject);
        }
    }
}
