using UnityEngine;
using System.Collections;
using Shootopia;
using Unity.Cinemachine;

namespace Shootopia
{
    public class VoidZone : MonoBehaviour
    {
        [Header("VFX")]
        [SerializeField] private GameObject implosionVFX;
        [SerializeField] private GameObject disappearVFX;
        
        private WeaponSO _data;
        private CinemachineImpulseSource _impulseSource;
        private bool _isInitialized = false;

        public void Initialize(WeaponSO data)
        {
            _data = data;
            if (_data != null)
            {
                implosionVFX = _data.HitVFXPrefab;
            }
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

            // Spawn blast before the black hole
            if (implosionVFX != null)
            {
                GameObject spawnedImplosion = Instantiate(implosionVFX, transform.position, Quaternion.LookRotation(Vector3.up));
                if (_data != null && _data.name.Contains("VoidGun"))
                {
                    spawnedImplosion.transform.localScale = Vector3.one * 1.8f;
                }
            }

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
                        damageable.TakeDamage(dmg, DamageType.Environment, _data);
                    }
                }

                yield return new WaitForFixedUpdate();
            }

            // Spawn disappear VFX at the end of the black hole
            if (disappearVFX != null)
            {
                GameObject spawnedDisappear = Instantiate(disappearVFX, transform.position, Quaternion.LookRotation(Vector3.up));
                if (_data != null && _data.name.Contains("VoidGun"))
                {
                    spawnedDisappear.transform.localScale = Vector3.one * 3.5f; // Increased from 1.8f to make the final blast larger
                }
            }

            Destroy(gameObject);
        }
    }
}
