using UnityEngine;
using System.Collections;
using Shootopia;
using Unity.Cinemachine;
using Unity.Netcode;

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
        private ulong _ownerID;
        private bool _hasOwner = false;

        public void Initialize(WeaponSO data, ulong ownerId)
        {
            _data = data;
            _ownerID = ownerId;
            _hasOwner = true;
            _isInitialized = true;
            StartCoroutine(VoidRoutine());
        }

        private void Awake()
        {
            _impulseSource = GetComponent<CinemachineImpulseSource>();
        }

        private IEnumerator VoidRoutine()
        {
            // Wait for initialization with safety timeout so host & clients never hang
            float timeout = 0f;
            while (!_isInitialized && timeout < 0.5f)
            {
                timeout += Time.deltaTime;
                yield return null;
            }

            float elapsed = 0f;
            float radius = (_data != null && _data.pullRadius > 0f) ? _data.pullRadius : 6f;
            float duration = (_data != null && _data.voidDuration > 0f) ? _data.voidDuration : 4f;

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
                    float pullMag = (_data != null ? _data.pullForce : 12f) * Time.fixedDeltaTime;
                    
                    // Check if target has active forcefield shield (immune to void black hole pull!)
                    var playerShield = c.GetComponentInParent<PlayerShield>();
                    if (playerShield == null) playerShield = c.GetComponent<PlayerShield>();
                    if (playerShield != null && playerShield.IsShieldActive)
                    {
                        continue; // Full immunity to Void pull & continuous void damage
                    }

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
                        rb.AddForce(dir * (_data != null ? _data.pullForce : 12f), ForceMode.Acceleration);
                    }

                    // Continuous damage
                    if (c.TryGetComponent<IDamageable>(out var damageable))
                    {
                        if (_hasOwner && Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.LocalClientId == _ownerID)
                        {
                            var targetHealth = c.GetComponentInParent<PlayerHealth>();
                            if (targetHealth == null) targetHealth = c.GetComponent<PlayerHealth>();
                            if (targetHealth != null)
                            {
                                targetHealth.LastAttackerClientId = _ownerID;
                            }

                            // Linear falloff from centre to edge
                            float t = Mathf.Clamp01(dist / radius);
                            float dmgCentre = _data != null ? _data.damageAtCentre : 25f;
                            float dmgEdge = _data != null ? _data.damageAtEdge : 5f;
                            float dmg = Mathf.Lerp(dmgCentre, dmgEdge, t) * Time.fixedDeltaTime;
                            damageable.TakeDamage(dmg, DamageType.Environment, _data);
                        }
                    }
                }

                yield return new WaitForFixedUpdate();
            }

            // Spawn disappear VFX (LaserExplosionBlue) at the end of the black hole on host & clients
            if (disappearVFX == null)
            {
#if UNITY_EDITOR
                disappearVFX = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ImportedProjectiles/3rdParty/Polygon Arsenal/Prefabs/Combat/Explosions/Sci-Fi/Laser/LaserExplosionBlue.prefab");
#endif
            }

            if (disappearVFX != null)
            {
                GameObject spawnedDisappear = Instantiate(disappearVFX, transform.position, Quaternion.LookRotation(Vector3.up));
                float scale = (_data != null && _data.name.Contains("VoidGun")) ? 3.5f : 2.5f;
                spawnedDisappear.transform.localScale = Vector3.one * scale;
            }

            Destroy(gameObject);
        }
    }
}
