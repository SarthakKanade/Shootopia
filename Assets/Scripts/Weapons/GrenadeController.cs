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
            
            float blastRadius = _data != null ? _data.blastRadius : 4f;
            float maxDamage = _data != null ? _data.Damage : 65f;
            float minDamage = _data != null ? _data.splashDamage : 20f;

            Collider[] hits = Physics.OverlapSphere(transform.position, blastRadius);
            foreach (Collider c in hits)
            {
                var t = c.GetComponentInParent<IDamageable>();
                if (t == null) t = c.GetComponent<IDamageable>();
                if (t != null)
                {
                    bool isServerOrOffline = Unity.Netcode.NetworkManager.Singleton == null || Unity.Netcode.NetworkManager.Singleton.IsServer;
                    if (isServerOrOffline)
                    {
                        var targetHealth = c.GetComponentInParent<PlayerHealth>();
                        if (targetHealth == null) targetHealth = c.GetComponent<PlayerHealth>();
                        if (targetHealth != null)
                        {
                            targetHealth.LastAttackerClientId = (ulong)_ownerID;
                        }

                        float dist = Vector3.Distance(transform.position, c.transform.position);
                        float dmg = Mathf.Lerp(maxDamage, minDamage, dist / blastRadius);
                        t.TakeDamage(Mathf.Clamp(dmg, minDamage, maxDamage), DamageType.Explosion, _data);
                    }
                }
            }

            var vfxToSpawn = explosionVFX != null ? explosionVFX : (_data != null ? _data.HitVFXPrefab : null);
            if (vfxToSpawn != null)
            {
                Vector3 blastNormal = Vector3.up;
                if (Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, out RaycastHit gHit, 1.5f))
                {
                    blastNormal = gHit.normal;
                }
                Quaternion rot = Mathf.Abs(blastNormal.y) > 0.95f
                    ? Quaternion.LookRotation(blastNormal, Vector3.forward)
                    : Quaternion.LookRotation(blastNormal, Vector3.up);
                Instantiate(vfxToSpawn, transform.position, rot);
            }

            if (impulseSource != null)
            {
                impulseSource.GenerateImpulse(0.25f);
            }
            else
            {
                CameraShakeManager.Instance?.ShakeExplosion(transform.position, blastRadius, 1.2f);
            }

            MobileHapticsManager.TriggerExplosion();
            Destroy(gameObject);
        }
    }
}
