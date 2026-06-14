using UnityEngine;
using System.Collections;
using VelocityWars.Core;
using Unity.Cinemachine;

namespace VelocityWars
{
    public class VoidZone : MonoBehaviour
    {
        [Header("VFX")]
        [SerializeField] private GameObject implosionVFX;
        
        private WeaponSO _data;
        private CinemachineImpulseSource _impulseSource;
        private bool _isInitialized = false;

        public void Initialize(WeaponSO data)
        {
            _data = data;
            _isInitialized = true;
            StartCoroutine(VoidRoutine());
        }

        private void Awake()
        {
            _impulseSource = GetComponent<CinemachineImpulseSource>();
        }

        private IEnumerator VoidRoutine()
        {
            // Wait for initialization if needed
            while (!_isInitialized) yield return null;

            float elapsed = 0f;
            float radius = _data.pullRadius;
            float duration = _data.voidDuration;

            while (elapsed < duration)
            {
                elapsed += Time.fixedDeltaTime;

                // Gentle impulse every second
                if (Mathf.FloorToInt(elapsed) > Mathf.FloorToInt(elapsed - Time.fixedDeltaTime))
                {
                    if (_impulseSource != null) _impulseSource.GenerateImpulse(0.1f);
                }

                Collider[] hits = Physics.OverlapSphere(transform.position, radius);
                foreach (Collider c in hits)
                {
                    // Ignore triggers (except maybe specific ones, but generally pull colliders)
                    if (c.isTrigger) continue;

                    float dist = Vector3.Distance(transform.position, c.transform.position);
                    Vector3 dir = (transform.position - c.transform.position).normalized;

                    // Pull force magnitude
                    float pullMag = _data.pullForce * Time.fixedDeltaTime;
                    
                    // Apply movement to CharacterController (Player)
                    if (c.TryGetComponent<CharacterController>(out var cc))
                    {
                        cc.Move(dir * pullMag);
                    }
                    // Apply movement to NavMeshAgent (Robots)
                    else if (c.TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var agent))
                    {
                        if (agent.enabled && agent.isOnNavMesh)
                        {
                            agent.Move(dir * pullMag);
                        }
                    }
                    // Apply movement to Rigidbody
                    else if (c.TryGetComponent<Rigidbody>(out var rb))
                    {
                        rb.AddForce(dir * _data.pullForce, ForceMode.Acceleration);
                    }

                    // Continuous damage
                    if (c.TryGetComponent<IDamageable>(out var damageable))
                    {
                        // Linear falloff from centre to edge
                        float t = Mathf.Clamp01(dist / radius);
                        float dmg = Mathf.Lerp(_data.damageAtCentre, _data.damageAtEdge, t) * Time.fixedDeltaTime;
                        damageable.TakeDamage(dmg, DamageType.Environment);
                    }
                }

                yield return new WaitForFixedUpdate();
            }

            // Final Implosion collapse
            if (implosionVFX != null)
            {
                Instantiate(implosionVFX, transform.position, Quaternion.identity);
            }

            Destroy(gameObject);
        }
    }
}
