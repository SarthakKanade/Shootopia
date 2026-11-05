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
    /// Pre-allocated and reused to achieve 0-allocation server tick tracking.
    /// </summary>
    public class WorldLagSnapshot
    {
        public float Timestamp;
        public readonly Dictionary<ulong, ColliderSnapshot> PlayerTransforms = new Dictionary<ulong, ColliderSnapshot>(16);
    }

    /// <summary>
    /// Server-side lag compensation (hitbox history buffer + server rewind).
    /// Runs on the Host (IsServer). Stores 1000ms of historical player positions using a zero-allocation ring buffer.
    /// When a client fires hitscan, rewinds colliders to the exact moment the client aimed,
    /// performs Physics.Raycast, and restores colliders to the present.
    /// </summary>
    public class LagCompensationManager : MonoBehaviour
    {
        private static LagCompensationManager _instance;
        public static LagCompensationManager Instance => _instance;

        private const int BufferCapacity = 64; // At 30-50 ticks/sec, 64 entries holds > 1200ms
        private readonly WorldLagSnapshot[] _ringBuffer = new WorldLagSnapshot[BufferCapacity];
        private int _headIndex = 0;
        private int _count = 0;

        // Reusable collections for rewinding to guarantee 0 GC allocations during raycasts
        private readonly Dictionary<Transform, ColliderSnapshot> _presentTransforms = new Dictionary<Transform, ColliderSnapshot>(16);
        private readonly List<Transform> _rewoundTransforms = new List<Transform>(16);

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }
            _instance = this;

            for (int i = 0; i < BufferCapacity; i++)
            {
                _ringBuffer[i] = new WorldLagSnapshot();
            }
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
        /// Records the current positions and rotations of all active players into the preallocated ring buffer.
        /// Zero heap allocation per tick.
        /// </summary>
        private void RecordSnapshot()
        {
            float now = Time.time;
            var snapshot = _ringBuffer[_headIndex];
            snapshot.Timestamp = now;
            snapshot.PlayerTransforms.Clear();

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

            _headIndex = (_headIndex + 1) % BufferCapacity;
            if (_count < BufferCapacity)
            {
                _count++;
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

            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer || _count == 0)
            {
                return Physics.Raycast(origin, direction, out hit, maxRange, mask);
            }

            WorldLagSnapshot targetSnapshot = FindClosestSnapshot(targetTimestamp);
            if (targetSnapshot == null)
            {
                return Physics.Raycast(origin, direction, out hit, maxRange, mask);
            }

            _presentTransforms.Clear();
            _rewoundTransforms.Clear();

            try
            {
                if (PlayerHealth.ActivePlayers != null)
                {
                    foreach (var player in PlayerHealth.ActivePlayers)
                    {
                        if (player == null || player.NetworkObject == null || !player.NetworkObject.IsSpawned) continue;
                        if (player.NetworkObjectId == shooterNetworkObjectId) continue;

                        if (targetSnapshot.PlayerTransforms.TryGetValue(player.NetworkObjectId, out var histData))
                        {
                            var tr = player.transform;
                            _presentTransforms[tr] = new ColliderSnapshot { Position = tr.position, Rotation = tr.rotation };

                            tr.position = histData.Position;
                            tr.rotation = histData.Rotation;
                            _rewoundTransforms.Add(tr);
                        }
                    }
                }

                if (_rewoundTransforms.Count > 0)
                {
                    Physics.SyncTransforms();
                }

                return Physics.Raycast(origin, direction, out hit, maxRange, mask);
            }
            finally
            {
                if (_rewoundTransforms.Count > 0)
                {
                    foreach (var tr in _rewoundTransforms)
                    {
                        if (tr != null && _presentTransforms.TryGetValue(tr, out var cur))
                        {
                            tr.position = cur.Position;
                            tr.rotation = cur.Rotation;
                        }
                    }
                    Physics.SyncTransforms();
                }
                _presentTransforms.Clear();
                _rewoundTransforms.Clear();
            }
        }

        private WorldLagSnapshot FindClosestSnapshot(float timestamp)
        {
            if (_count == 0) return null;

            WorldLagSnapshot best = null;
            float bestDiff = float.MaxValue;

            for (int i = 0; i < _count; i++)
            {
                var snap = _ringBuffer[i];
                float diff = Mathf.Abs(snap.Timestamp - timestamp);
                if (diff < bestDiff)
                {
                    bestDiff = diff;
                    best = snap;
                }
            }

            return best;
        }
    }
}
