using UnityEngine;
using System.Collections;
using VelocityWars.Core;
using Unity.Cinemachine;

namespace VelocityWars
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

        protected override void OnInitialised(WeaponSO data)
        {
            _data = data;
            rb = GetComponent<Rigidbody>();
            
            float angle = data != null ? data.throwAngle : 30f;
            float force = data != null ? data.throwForce : 12f;

            Vector3 throwDir = Quaternion.AngleAxis(-angle, _cam.transform.right) * _cam.transform.forward;
            rb.AddForce(throwDir * force, ForceMode.VelocityChange);

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
                if (c.TryGetComponent(out IDamageable t))
                {
                    float dist = Vector3.Distance(transform.position, c.transform.position);
                    float dmg = Mathf.Lerp(maxDamage, minDamage, dist / blastRadius);
                    t.TakeDamage(Mathf.Clamp(dmg, minDamage, maxDamage), DamageType.Explosion);
                }
            }

            if (explosionVFX != null)
            {
                Instantiate(explosionVFX, transform.position, Quaternion.identity);
            }

            if (impulseSource != null)
            {
                impulseSource.GenerateImpulse(0.25f);
            }

            Destroy(gameObject);
        }
    }
}
