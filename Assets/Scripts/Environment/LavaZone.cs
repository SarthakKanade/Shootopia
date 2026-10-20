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
        [SerializeField] private float burnDPS = 35.0f;          // Continuous Damage Per Second while in lava
        [SerializeField] private float sinkSpeed = 1.2f;         // Downward sinking velocity (m/s)
        [SerializeField] private float lavaSurfaceY = 0.5f;       // Surface elevation threshold for lava (Y <= 0.5f)
        [SerializeField] private float killAbyssY = -10.0f;       // Abyss depth threshold for instant death

        private readonly HashSet<GameObject> _playersInLava = new HashSet<GameObject>();

        private void OnTriggerEnter(Collider other)
        {
            HandlePlayerEnter(other.gameObject);
        }

        private void OnTriggerStay(Collider other)
        {
            HandlePlayerEnter(other.gameObject);
        }

        private void OnTriggerExit(Collider other)
        {
            HandlePlayerExit(other.gameObject);
        }

        private void Update()
        {
            // Find all player health objects in the scene to guarantee complete coverage
            var allPlayers = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
            foreach (var health in allPlayers)
            {
                if (health == null || health.gameObject == null) continue;

                var playerObj = health.gameObject;
                bool isInLavaElevation = playerObj.transform.position.y <= lavaSurfaceY;

                if (isInLavaElevation && !health.IsDead && !health.IsInvincible)
                {
                    if (_playersInLava.Add(playerObj))
                    {
                        health.ApplyBurnEffect(999f, burnDPS);
                    }
                    ProcessPlayerInLava(playerObj, health);
                }
                else if (_playersInLava.Contains(playerObj))
                {
                    HandlePlayerExit(playerObj);
                }
            }
        }

        private void HandlePlayerEnter(GameObject playerObj)
        {
            var motor = playerObj.GetComponentInParent<CharacterMotor>();
            if (motor == null) motor = playerObj.GetComponent<CharacterMotor>();
            if (motor == null) return;

            var health = motor.GetComponent<PlayerHealth>();
            if (health != null && (health.IsDead || health.IsInvincible)) return;

            if (_playersInLava.Add(motor.gameObject))
            {
                if (health != null)
                {
                    health.ApplyBurnEffect(999f, burnDPS);
                }
            }
        }

        private void HandlePlayerExit(GameObject playerObj)
        {
            if (playerObj == null) return;
            var motor = playerObj.GetComponentInParent<CharacterMotor>();
            if (motor == null) motor = playerObj.GetComponent<CharacterMotor>();

            if (motor != null)
            {
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

        private void ProcessPlayerInLava(GameObject playerObj, PlayerHealth health)
        {
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
                cc.Move(Vector3.down * sinkSpeed * Time.deltaTime);
            }
            else
            {
                motor.transform.position += Vector3.down * sinkSpeed * Time.deltaTime;
            }

            // 3. Apply server-authoritative continuous lava damage
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                if (health != null && !health.IsDead && !health.IsInvincible)
                {
                    if (playerObj.transform.position.y <= killAbyssY)
                    {
                        // Instant death in deep abyss
                        health.TakeDamage(9999f, DamageType.Environment);
                    }
                    else
                    {
                        // Continuous lava heat damage per second
                        health.TakeDamage(burnDPS * Time.deltaTime, DamageType.Environment);
                    }
                }
            }
        }
    }
}
