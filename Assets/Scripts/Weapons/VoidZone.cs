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
            float duration = (_data != null && _data.voidDuration > 0f) ? _data.voidDuration : 7f;
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
                foreach (Collider c in hits)
                {
                    // Ignore triggers (except maybe specific ones, but generally pull colliders)
                    if (c.isTrigger) continue;

                    float dist = Vector3.Distance(transform.position, c.transform.position);
                    Vector3 dir = (transform.position - c.transform.position).normalized;

                    // Pull force magnitude (matched in power to player run speed: 5.0 m/s)
                    float pullForceVal = (_data != null && _data.pullForce > 0f) ? _data.pullForce : 5.0f;
                    var motor = c.GetComponentInParent<CharacterMotor>() ?? c.GetComponent<CharacterMotor>();
                    if (motor != null && motor.moveSpeed > 0f)
                    {
                        pullForceVal = motor.moveSpeed;
                    }
                    float pullMag = pullForceVal * Time.fixedDeltaTime;
                    
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
                        rb.AddForce(dir * pullForceVal, ForceMode.Acceleration);
                    }

                    // Proximity-based void damage ticks (scaling up to 200+ HP if trapped at core)
                    if (shouldTickDamage)
                    {
                        var damageable = c.GetComponentInParent<IDamageable>();
                        if (damageable == null) damageable = c.GetComponent<IDamageable>();
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

                                var targetHealth = c.GetComponentInParent<PlayerHealth>();
                                if (targetHealth == null) targetHealth = c.GetComponent<PlayerHealth>();
                                if (targetHealth != null)
                                {
                                    targetHealth.LastAttackerClientId = _hasOwner ? _ownerID : Unity.Netcode.NetworkManager.ServerClientId;
                                }

                                float t = Mathf.Clamp01(dist / radius);
                                float centerDPS = (_data != null && _data.damageAtCentre > 0f) ? _data.damageAtCentre : 30f;
                                float edgeDPS = (_data != null && _data.damageAtEdge > 0f) ? _data.damageAtEdge : 8f;
                                float dps = Mathf.Lerp(centerDPS, edgeDPS, t);
                                float tickDamage = dps * damageTickInterval;

                                damageable.TakeDamage(tickDamage, DamageType.Environment, _data);
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
