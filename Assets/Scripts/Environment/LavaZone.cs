using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Shootopia
{
    /// <summary>
    /// Handles lava zone logic:
    /// - Disables player movement & action controls when inside lava (Y <= 0.5f).
    /// - Applies the flamethrower burn effect (fiery visual glow + passive DoT) ONLY when in lava.
    /// - Immediately removes burn effect and restores controls when player leaves lava or respawns on land.
    /// - Slowly sinks the player downwards into the lava.
    /// </summary>
    public class LavaZone : MonoBehaviour
    {
        [Header("Lava Damage & Sinking Parameters")]
        [SerializeField] private float burnDPS = 30.0f;          // Continuous Damage Per Second while in lava
        [SerializeField] private float sinkSpeed = 1.2f;         // Downward sinking velocity (m/s)
        [SerializeField] private float lavaSurfaceY = 0.5f;       // Surface elevation threshold for lava (Y <= 0.5f)

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
            // Process sinking for all players currently in lava
            List<GameObject> toRemove = null;
            foreach (var player in _playersInLava)
            {
                if (player == null)
                {
                    if (toRemove == null) toRemove = new List<GameObject>();
                    toRemove.Add(player);
                    continue;
                }

                var health = player.GetComponent<PlayerHealth>();
                if (health != null && health.IsDead)
                {
                    if (toRemove == null) toRemove = new List<GameObject>();
                    toRemove.Add(player);
                    continue;
                }

                ProcessPlayerInLava(player);
            }

            if (toRemove != null)
            {
                foreach (var p in toRemove)
                {
                    HandlePlayerExit(p);
                }
            }
        }

        private void HandlePlayerEnter(GameObject playerObj)
        {
            var motor = playerObj.GetComponentInParent<CharacterMotor>();
            if (motor == null) return;

            var health = motor.GetComponent<PlayerHealth>();
            if (health != null && health.IsDead) return;

            if (_playersInLava.Add(motor.gameObject))
            {
                // Apply flamethrower burn effect (fiery tint + passive DoT)
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
            if (motor != null)
            {
                if (_playersInLava.Remove(motor.gameObject))
                {
                    var input = motor.GetComponent<PlayerInputRouter>();
                    if (input != null) input.DisableControls = false;
                    motor.IsInLava = false;

                    var health = motor.GetComponent<PlayerHealth>();
                    if (health != null)
                    {
                        health.RemoveBurnEffect();
                    }
                }
            }
        }

        private void ProcessPlayerInLava(GameObject playerObj)
        {
            var motor = playerObj.GetComponent<CharacterMotor>();
            if (motor == null) return;

            // 1. Disable controls
            var input = motor.GetComponent<PlayerInputRouter>();
            if (input != null)
            {
                input.DisableControls = true;
            }

            // 2. Slow sinking into lava
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
        }
    }
}
