using UnityEngine;
using System.Collections;
using Shootopia;
using Unity.Netcode;

namespace Shootopia
{
    public class TripMineController : TrapBase
    {
        [SerializeField] private SphereCollider _proximitySensor;
        [SerializeField] private Renderer _armRing;
        [SerializeField] private GameObject explosionVFX;

        private Rigidbody rb;
        private WeaponSO _data;
        private bool _armed = false;
        private bool _stuck = false;
        private bool _exploded = false;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        protected override void OnInitialised(WeaponSO data, Vector3 launchVelocity)
        {
            _data = data;
            
            if (rb != null)
            {
                if (launchVelocity.sqrMagnitude > 0.01f)
                {
                    rb.linearVelocity = launchVelocity;
                }
                else if (Camera.main != null)
                {
                    float angle = data != null ? data.throwAngle : 20f;
                    float force = data != null ? data.throwForce : 14f;
                    Vector3 throwDir = Quaternion.AngleAxis(-angle, Camera.main.transform.right) * Camera.main.transform.forward;
                    rb.linearVelocity = throwDir * force;
                }
            }

            if (TripMineManager.Instance != null)
                TripMineManager.Instance.Register(this);
        }

        private void OnCollisionEnter(Collision col)
        {
            if (_stuck) return;
            
            // Don't stick to player
            if (col.gameObject.CompareTag("Player") || col.gameObject.layer == LayerMask.NameToLayer("Player")) return;

            _stuck = true;
            rb.isKinematic = true;

            // Align to surface normal
            Vector3 n = col.contacts[0].normal;
            transform.rotation = Quaternion.FromToRotation(Vector3.up, n);
            transform.position = col.contacts[0].point + n * 0.015f;

            StartCoroutine(ArmRoutine());
        }

        private IEnumerator ArmRoutine()
        {
            float delay = _data != null ? _data.armDelay : 0.3f;
            yield return new WaitForSeconds(delay);
            
            _armed = true;
            if (_proximitySensor != null) _proximitySensor.enabled = true;
            
            StartCoroutine(PulseRoutine());
        }

        private IEnumerator PulseRoutine()
        {
            if (_armRing == null) yield break;

            Material mat = _armRing.material;
            mat.EnableKeyword("_EMISSION");

            while (_armed && !_exploded)
            {
                float t = (Mathf.Sin(Time.time * Mathf.PI * 2f) + 1f) / 2f;
                Color pulseColor = Color.red * Mathf.Lerp(0.5f, 4.0f, t);
                mat.SetColor("_EmissionColor", pulseColor);
                yield return null;
            }
        }

        private void OnTriggerEnter(Collider col)
        {
            if (!_armed || _exploded) return;

            var targetHealth = col.GetComponentInParent<PlayerHealth>();
            if (targetHealth == null) targetHealth = col.GetComponent<PlayerHealth>();

            if (targetHealth != null)
            {
                bool isOwner = (targetHealth.OwnerClientId == (ulong)_ownerID);
                bool allowSelfTrigger = (_data != null && _data.selfTrigger);
                if (!isOwner || allowSelfTrigger)
                {
                    Explode();
                }
            }
            else if (col.GetComponent<IDamageable>() != null || col.GetComponentInParent<IDamageable>() != null)
            {
                Explode();
            }
        }

        public void Explode()
        {
            if (_exploded) return;
            _exploded = true;

            float radius = _data != null ? _data.blastRadius : 3.5f;
            float maxDmg = _data != null ? _data.Damage : 75f;
            float minDmg = _data != null ? _data.splashDamage : 25f;

            Collider[] hits = Physics.OverlapSphere(transform.position, radius);
            foreach (Collider c in hits)
            {
                if (c.TryGetComponent(out IDamageable t))
                {
                    if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.LocalClientId == (ulong)_ownerID)
                    {
                        var targetHealth = c.GetComponentInParent<PlayerHealth>();
                        if (targetHealth == null) targetHealth = c.GetComponent<PlayerHealth>();
                        if (targetHealth != null)
                        {
                            targetHealth.LastAttackerClientId = (ulong)_ownerID;
                        }

                        float dist = Vector3.Distance(transform.position, c.transform.position);
                        float dmg = Mathf.Lerp(maxDmg, minDmg, dist / radius);
                        t.TakeDamage(Mathf.Clamp(dmg, minDmg, maxDmg), DamageType.Explosion, _data);
                    }
                }
            }

            var vfxToSpawn = explosionVFX != null ? explosionVFX : (_data != null ? _data.HitVFXPrefab : null);
            if (vfxToSpawn != null)
                Instantiate(vfxToSpawn, transform.position, Quaternion.identity);

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
