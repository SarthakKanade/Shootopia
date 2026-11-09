using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Shootopia
{
    /// <summary>
    /// Handles lava zone logic:
    /// - Continuously detects players inside or below the lava surface (Y <= 0.5f).
    /// - Disables player movement & action controls when inside lava.
    /// - Applies continuous server-authoritative heat/burn damage every frame without stopping.
    /// - Applies the fiery visual burn effect while inside lava.
    /// - Sinks the player downwards into the lava at sinkSpeed.
    /// - Instantly applies lethal damage if player falls below the kill floor (Y <= -10f).
    /// - Restores controls and removes burn effect when player respawns on land.
    /// </summary>
    public class LavaZone : MonoBehaviour
    {
        [Header("Lava Damage & Sinking Parameters")]
        [SerializeField] private float sinkSpeed = 1.2f;         // Downward sinking velocity (m/s)
        [SerializeField] private float lavaSurfaceY = 0.5f;       // Surface elevation threshold for lava (Y <= 0.5f)
        [SerializeField] private float killAbyssY = -10.0f;       // Abyss depth threshold for instant death
        [SerializeField] private float deathDelay = 2.0f;         // Keep full health for 2 seconds before instant kill

        private readonly HashSet<GameObject> _playersInLava = new HashSet<GameObject>();
        private readonly Dictionary<GameObject, float> _lavaImmersionTimers = new Dictionary<GameObject, float>();

        private void OnTriggerEnter(Collider other)
        {
            HandlePlayerEnter(other.gameObject);
        }

        private void OnTriggerExit(Collider other)
        {
            HandlePlayerExit(other.gameObject);
        }

        private float _elevationCheckTimer = 0f;

        private bool IsMatchActive()
        {
            return (GameModeBase.Instance != null && (GameModeBase.Instance.MatchActive.Value || GameModeBase.Instance.IsInProgress))
                || (NetworkedGameManager.Instance != null && 
                    (NetworkedGameManager.Instance.MatchStarted.Value || 
                     (NetworkedGameManager.Instance.ActiveGameMode != null && (NetworkedGameManager.Instance.ActiveGameMode.MatchActive.Value || NetworkedGameManager.Instance.ActiveGameMode.IsInProgress))));
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            bool isMatchActive = IsMatchActive();

            // Process players currently sinking in lava
            if (_playersInLava.Count > 0)
            {
                var activeList = PlayerHealth.ActivePlayers;
                for (int i = 0; i < activeList.Count; i++)
                {
                    var health = activeList[i];
                    if (health != null && _playersInLava.Contains(health.gameObject))
                    {
                        ProcessPlayerInLava(health.gameObject, health, dt);
                    }
                }
            }

            // Throttled 10 Hz check for elevation-based lava entry/exit
            _elevationCheckTimer += dt;
            if (_elevationCheckTimer < 0.1f) return;
            _elevationCheckTimer = 0f;

            var activePlayers = PlayerHealth.ActivePlayers;
            for (int i = 0; i < activePlayers.Count; i++)
            {
                var health = activePlayers[i];
                if (health == null || health.gameObject == null) continue;

                var playerObj = health.gameObject;
                bool isInLavaElevation = playerObj.transform.position.y <= lavaSurfaceY;
                bool isDeepAbyss = playerObj.transform.position.y <= killAbyssY;

                // During warmup lobby: safe teleport rescue back to land instead of lethal death
                if (!isMatchActive)
                {
                    if (isInLavaElevation || isDeepAbyss)
                    {
                        if (health.IsOwner || NetworkManager.Singleton == null || NetworkManager.Singleton.IsServer)
                        {
                            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                            Vector3 safePos = MapRegistry.GetDefaultSafeSpawnPosition(currentScene);

                            if (SpawnPointManager.Instance != null)
                            {
                                Transform sp = SpawnPointManager.Instance.GetSpawnPoint(SpawnPointType.FFA);
                                if (sp != null && !MapRegistry.IsPositionInHazardOrVoid(currentScene, sp.position)) safePos = sp.position;
                            }
                            health.Teleport(safePos);
                        }
                        HandlePlayerExit(playerObj);
                    }
                    else if (_playersInLava.Contains(playerObj))
                    {
                        HandlePlayerExit(playerObj);
                    }
                    continue;
                }

                if (isInLavaElevation && !health.IsDead && (!health.IsInvincible || isDeepAbyss))
                {
                    if (_playersInLava.Add(playerObj))
                    {
                        _lavaImmersionTimers[playerObj] = 0f;
                        health.ApplyBurnEffect(999f, 0f);
                    }
                }
                else if (_playersInLava.Contains(playerObj))
                {
                    HandlePlayerExit(playerObj);
                }
            }
        }

        private void HandlePlayerEnter(GameObject playerObj)
        {
            if (!IsMatchActive()) return;

            var motor = playerObj.GetComponentInParent<CharacterMotor>();
            if (motor == null) motor = playerObj.GetComponent<CharacterMotor>();
            if (motor == null) return;

            var health = motor.GetComponent<PlayerHealth>();
            if (health != null && (health.IsDead || health.IsInvincible)) return;

            if (_playersInLava.Add(motor.gameObject))
            {
                _lavaImmersionTimers[motor.gameObject] = 0f;
                if (health != null)
                {
                    health.ApplyBurnEffect(999f, 0f); // Visual fiery tint only, zero tick damage
                }
            }
        }

        private void HandlePlayerExit(GameObject playerObj)
        {
            if (playerObj == null) return;
            _lavaImmersionTimers.Remove(playerObj);

            var motor = playerObj.GetComponentInParent<CharacterMotor>();
            if (motor == null) motor = playerObj.GetComponent<CharacterMotor>();

            if (motor != null)
            {
                _lavaImmersionTimers.Remove(motor.gameObject);
                _playersInLava.Remove(motor.gameObject);

                var input = motor.GetComponent<PlayerInputRouter>();
                if (input != null) input.DisableControls = false;
                motor.IsInLava = false;

                var health = motor.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    health.RemoveBurnEffect();
                }
            }
            else
            {
                _playersInLava.Remove(playerObj);
            }
        }

        private void ProcessPlayerInLava(GameObject playerObj, PlayerHealth health, float dt)
        {
            if (!IsMatchActive())
            {
                HandlePlayerExit(playerObj);
                return;
            }

            var motor = playerObj.GetComponent<CharacterMotor>();
            if (motor == null) return;

            // 1. Disable controls
            var input = motor.GetComponent<PlayerInputRouter>();
            if (input != null)
            {
                input.DisableControls = true;
            }

            // 2. Sinking into lava
            motor.IsInLava = true;
            var cc = motor.GetComponent<CharacterController>();
            if (cc != null && cc.enabled)
            {
                cc.Move(Vector3.down * sinkSpeed * dt);
            }
            else
            {
                motor.transform.position += Vector3.down * sinkSpeed * dt;
            }

            // 3. Keep health full for the first 2 seconds, then instant kill (no repetitive damage tick SFX)
            if (!_lavaImmersionTimers.ContainsKey(playerObj))
            {
                _lavaImmersionTimers[playerObj] = 0f;
            }
            _lavaImmersionTimers[playerObj] += dt;

            bool canApplyDamage = (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || NetworkManager.Singleton.IsServer);
            if (canApplyDamage && health != null && !health.IsDead)
            {
                bool reachedAbyss = playerObj.transform.position.y <= killAbyssY;
                bool reachedDelay = _lavaImmersionTimers[playerObj] >= deathDelay;

                if (reachedAbyss || reachedDelay)
                {
                    health.TakeDamage(9999f, DamageType.Environment);
                    _lavaImmersionTimers[playerObj] = 0f;
                }
            }
        }
    }
}
