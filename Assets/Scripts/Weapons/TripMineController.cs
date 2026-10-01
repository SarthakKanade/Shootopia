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
            
            // Don't stick to player or any protection shield
            if (col.gameObject.CompareTag("Player") || 
                col.gameObject.layer == LayerMask.NameToLayer("Player") ||
                col.gameObject.GetComponentInParent<PlayerShield>() != null ||
                col.gameObject.GetComponent<ShieldColliderReceiver>() != null ||
                col.gameObject.name.Contains("Shield"))
            {
                return;
            }

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
            if (_proximitySensor != null)
            {
                if (_data != null && _data.triggerRadius > 0f)
                {
                    _proximitySensor.radius = _data.triggerRadius;
                }
                _proximitySensor.enabled = true;
            }
            
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

            float radius = _data != null ? _data.blastRadius : 5.5f;
            float maxDmg = _data != null ? _data.Damage : 95f;
            float minDmg = _data != null ? _data.splashDamage : 35f;
            Vector3 poc = transform.position;

            Collider[] hits = Physics.OverlapSphere(poc, radius);
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

                        Vector3 closestPt = c.ClosestPoint(poc);
                        float dist = Vector3.Distance(poc, closestPt);
                        float tDist = Mathf.Clamp01(dist / radius);
                        float dmg = Mathf.Lerp(maxDmg, minDmg, tDist);
                        t.TakeDamage(Mathf.Clamp(dmg, minDmg, maxDmg), DamageType.Explosion, _data);
                    }
                }
            }

            var vfxToSpawn = explosionVFX != null ? explosionVFX : (_data != null ? _data.HitVFXPrefab : null);
            if (vfxToSpawn != null)
            {
                Vector3 blastNormal = transform.up;
                if (blastNormal == Vector3.zero) blastNormal = Vector3.up;
                Quaternion rot = Mathf.Abs(blastNormal.y) > 0.95f
                    ? Quaternion.LookRotation(blastNormal, Vector3.forward)
                    : Quaternion.LookRotation(blastNormal, Vector3.up);
                GameObject vfx = Instantiate(vfxToSpawn, poc, rot);
                vfx.transform.localScale = Vector3.one * 1.35f;
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
