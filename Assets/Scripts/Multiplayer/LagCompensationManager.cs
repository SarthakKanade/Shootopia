using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Shootopia
{
    /// <summary>
    /// Snapshot of a single player's position and orientation at a given time.
    /// </summary>
    public struct ColliderSnapshot
    {
        public Vector3 Position;
        public Quaternion Rotation;
    }

    /// <summary>
    /// Global snapshot of all active players at a specific network timestamp.
    /// </summary>
    public class WorldLagSnapshot
    {
        public float Timestamp;
        public Dictionary<ulong, ColliderSnapshot> PlayerTransforms = new Dictionary<ulong, ColliderSnapshot>();
    }

    /// <summary>
    /// Server-side lag compensation (hitbox history buffer + server rewind).
    /// Runs on the Host (IsServer). Stores 1000ms of historical player positions.
    /// When a client fires hitscan, rewinds colliders to the exact moment the client aimed,
    /// performs Physics.Raycast, and restores colliders to the present.
    /// </summary>
    public class LagCompensationManager : MonoBehaviour
    {
        private static LagCompensationManager _instance;
        public static LagCompensationManager Instance => _instance;

        private const float HistoryDurationSeconds = 1.0f;
        private readonly List<WorldLagSnapshot> _history = new List<WorldLagSnapshot>(70);

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }
            _instance = this;
        }

        private void FixedUpdate()
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            {
                return;
            }

            RecordSnapshot();
        }

        /// <summary>
        /// Records the current positions and rotations of all active players in the match.
        /// </summary>
        private void RecordSnapshot()
        {
            float now = Time.time;
            var snapshot = new WorldLagSnapshot { Timestamp = now };

            if (PlayerHealth.ActivePlayers != null)
            {
                foreach (var player in PlayerHealth.ActivePlayers)
                {
                    if (player == null || player.NetworkObject == null || !player.NetworkObject.IsSpawned) continue;
                    snapshot.PlayerTransforms[player.NetworkObjectId] = new ColliderSnapshot
                    {
                        Position = player.transform.position,
                        Rotation = player.transform.rotation
                    };
                }
            }

            _history.Add(snapshot);

            // Prune snapshots older than HistoryDurationSeconds
            float expireThreshold = now - HistoryDurationSeconds;
            while (_history.Count > 0 && _history[0].Timestamp < expireThreshold)
            {
                _history.RemoveAt(0);
            }
        }

        /// <summary>
        /// Executes a lag-compensated raycast by rewinding all target colliders to targetTimestamp.
        /// Rewinds, raycasts, and safely restores colliders to present in a try-finally block.
        /// </summary>
        public bool RaycastLagCompensated(
            float targetTimestamp,
            Vector3 origin,
            Vector3 direction,
            float maxRange,
            LayerMask mask,
            ulong shooterNetworkObjectId,
            out RaycastHit hit)
        {
            hit = default;

            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer || _history.Count == 0)
            {
                // Fallback to standard present-time raycast
                return Physics.Raycast(origin, direction, out hit, maxRange, mask);
            }

            // Find the closest historical snapshot
            WorldLagSnapshot targetSnapshot = FindClosestSnapshot(targetTimestamp);
            if (targetSnapshot == null)
            {
                return Physics.Raycast(origin, direction, out hit, maxRange, mask);
            }

            // Save present transforms
            var presentTransforms = new Dictionary<Transform, ColliderSnapshot>();
            var rewoundTransforms = new List<Transform>();

            try
            {
                if (PlayerHealth.ActivePlayers != null)
                {
                    foreach (var player in PlayerHealth.ActivePlayers)
                    {
                        if (player == null || player.NetworkObject == null || !player.NetworkObject.IsSpawned) continue;
                        if (player.NetworkObjectId == shooterNetworkObjectId) continue; // Don't rewind shooter

                        if (targetSnapshot.PlayerTransforms.TryGetValue(player.NetworkObjectId, out var histData))
                        {
                            var tr = player.transform;
                            presentTransforms[tr] = new ColliderSnapshot { Position = tr.position, Rotation = tr.rotation };

                            // Temporarily move to historical position
                            tr.position = histData.Position;
                            tr.rotation = histData.Rotation;
                            rewoundTransforms.Add(tr);
                        }
                    }
                }

                if (rewoundTransforms.Count > 0)
                {
                    Physics.SyncTransforms();
                }

                // Perform the raycast in the historical state
                return Physics.Raycast(origin, direction, out hit, maxRange, mask);
            }
            finally
            {
                // Guarantee full restoration to present state
                if (rewoundTransforms.Count > 0)
                {
                    foreach (var tr in rewoundTransforms)
                    {
                        if (tr != null && presentTransforms.TryGetValue(tr, out var cur))
                        {
                            tr.position = cur.Position;
                            tr.rotation = cur.Rotation;
                        }
                    }
                    Physics.SyncTransforms();
                }
            }
        }

        private WorldLagSnapshot FindClosestSnapshot(float timestamp)
        {
            if (_history.Count == 0) return null;

            WorldLagSnapshot best = _history[0];
            float bestDiff = Mathf.Abs(best.Timestamp - timestamp);

            for (int i = 1; i < _history.Count; i++)
            {
                float diff = Mathf.Abs(_history[i].Timestamp - timestamp);
                if (diff < bestDiff)
                {
                    bestDiff = diff;
                    best = _history[i];
                }
            }

            return best;
        }
    }
}

