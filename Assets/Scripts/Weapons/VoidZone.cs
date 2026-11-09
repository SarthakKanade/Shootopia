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
        [SerializeField] private float explosionScale = 3.5f;
        
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
            float duration = (_data != null && _data.voidDuration > 0f) ? _data.voidDuration : 5f;
            float damageTickInterval = 0.25f; // 4 ticks/sec for responsive void vortex shredding
            float damageTickTimer = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.fixedDeltaTime;
                damageTickTimer += Time.fixedDeltaTime;

                bool shouldTickDamage = false;
                if (damageTickTimer >= damageTickInterval)
                {
                    shouldTickDamage = true;
                    damageTickTimer -= damageTickInterval;
                }

                // Gentle impulse every second
                if (Mathf.FloorToInt(elapsed) > Mathf.FloorToInt(elapsed - Time.fixedDeltaTime))
                {
                    if (_impulseSource != null) _impulseSource.GenerateImpulse(0.1f);
                }

                Collider[] hits = Physics.OverlapSphere(transform.position, radius);
                var processedEntities = new System.Collections.Generic.HashSet<GameObject>();

                foreach (Collider c in hits)
                {
                    // Ignore triggers (except maybe specific ones, but generally pull colliders)
                    if (c.isTrigger) continue;

                    var rootObj = c.transform.root.gameObject;
                    if (!processedEntities.Add(rootObj)) continue;

                    var motor = c.GetComponentInParent<CharacterMotor>();
                    Vector3 targetPos = motor != null ? motor.transform.position : c.bounds.center;
                    float dist = Vector3.Distance(transform.position, targetPos);
                    Vector3 dir = (transform.position - targetPos);
                    dir.y = 0f; // Keep pull horizontal so player run speed directly counters the pull
                    if (dir.sqrMagnitude > 0.0001f) dir.Normalize();

                    float distNormalized = Mathf.Clamp01(dist / radius);
                    float coreProximity = 1f - distNormalized;

                    // Pull force magnitude: scaled by distance
                    // Outer edge allows player (5.0 m/s run speed) to break away if running opposite
                    // Mid-range matches run speed (stuck in place); core overpowers and locks in place
                    float basePull = (_data != null && _data.pullForce > 0f) ? _data.pullForce : 5.0f;
                    float pullForceVal = Mathf.Lerp(basePull * 0.92f, basePull * 1.35f, Mathf.Pow(coreProximity, 1.5f));
                    float pullMag = pullForceVal * Time.fixedDeltaTime;
                    
                    // Check if target has active forcefield shield (immune to void black hole pull!)
                    var playerShield = c.GetComponentInParent<PlayerShield>() ?? c.GetComponent<PlayerShield>();
                    if (playerShield != null && playerShield.IsShieldActive)
                    {
                        continue; // Full immunity to Void pull & continuous void damage
                    }

                    // Apply movement to CharacterController (Player)
                    var cc = motor != null ? motor.GetComponent<CharacterController>() : c.GetComponentInParent<CharacterController>();
                    if (cc != null && cc.enabled)
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
                        rb.AddForce(dir * pullForceVal, ForceMode.Acceleration);
                    }

                    // Proximity-based void damage ticks (quadratic scaling up to sure-kill lethal DPS at core)
                    if (shouldTickDamage)
                    {
                        var damageable = c.GetComponentInParent<IDamageable>() ?? c.GetComponent<IDamageable>();
                        if (damageable != null)
                        {
                            bool isServerOrOffline = Unity.Netcode.NetworkManager.Singleton == null || Unity.Netcode.NetworkManager.Singleton.IsServer;
                            if (isServerOrOffline)
                            {
                                // Skip damage if match is not active (Warmup)
                                if (GameModeBase.Instance != null && !GameModeBase.Instance.MatchActive.Value)
                                {
                                    continue;
                                }

                                var targetHealth = c.GetComponentInParent<PlayerHealth>() ?? c.GetComponent<PlayerHealth>();
                                if (targetHealth != null)
                                {
                                    targetHealth.LastAttackerClientId = _hasOwner ? _ownerID : Unity.Netcode.NetworkManager.ServerClientId;
                                }

                                float centerDPS = (_data != null && _data.damageAtCentre > 0f) ? _data.damageAtCentre : 60f;
                                float edgeDPS = (_data != null && _data.damageAtEdge > 0f) ? _data.damageAtEdge : 3f;
                                float coreDamageFactor = Mathf.Pow(coreProximity, 2f);
                                float dps = Mathf.Lerp(edgeDPS, centerDPS, coreDamageFactor);
                                float tickDamage = dps * damageTickInterval;

                                ulong attackerId = _hasOwner ? _ownerID : (Unity.Netcode.NetworkManager.Singleton != null ? Unity.Netcode.NetworkManager.Singleton.LocalClientId : ulong.MaxValue);
                                if (targetHealth != null)
                                {
                                    targetHealth.TakeDamage(tickDamage, DamageType.Projectile, _data, attackerId);
                                }
                                else
                                {
                                    damageable.TakeDamage(tickDamage, DamageType.Projectile, _data);
                                }
                            }
                        }
                    }
                }

                yield return new WaitForFixedUpdate();
            }

            if (disappearVFX != null)
            {
                try
                {
                    GameObject spawnedDisappear = Instantiate(disappearVFX, transform.position, Quaternion.LookRotation(Vector3.up));
                    if (spawnedDisappear != null)
                    {
                        spawnedDisappear.transform.localScale = Vector3.one * explosionScale;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[VoidZone] Failed to instantiate disappearVFX: {ex.Message}");
                }
            }

            Destroy(gameObject);
        }
    }
}
