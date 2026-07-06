using UnityEngine;
using System.Collections;
using Shootopia;

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

        protected override void OnInitialised(WeaponSO data)
        {
            _data = data;
            
            // Initial throw force
            Vector3 throwDir = Camera.main.transform.forward * 6f + Vector3.up * 2f;
            rb.AddForce(throwDir, ForceMode.VelocityChange);

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

            // Owner check
            int enterID = GetEntityID(col);
            if (enterID != _ownerID)
            {
                // Trigger if it's a damageable target
                if (col.GetComponent<IDamageable>() != null || col.GetComponentInParent<IDamageable>() != null)
                {
                    Explode();
                }
            }
        }

        private int GetEntityID(Collider col)
        {
            // Owner check: for single player, we check if the object is the player
            if (col.CompareTag("Player") || col.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                return _ownerID; // Assume owner is the player
            }
            
            // For enemies/robots, return something else
            return _ownerID + 1;
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
                    float dist = Vector3.Distance(transform.position, c.transform.position);
                    float dmg = Mathf.Lerp(maxDmg, minDmg, dist / radius);
                    t.TakeDamage(Mathf.Clamp(dmg, minDmg, maxDmg), DamageType.Explosion, _data);
                }
            }

            if (explosionVFX != null)
                Instantiate(explosionVFX, transform.position, Quaternion.identity);

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
