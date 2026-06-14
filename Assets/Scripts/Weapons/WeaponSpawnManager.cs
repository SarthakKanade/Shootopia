using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VelocityWars.Weapons
{
    using VelocityWars;
    public class WeaponSpawnManager : MonoBehaviour
{
        [Header("References")]
        [SerializeField] private WeaponSpawnNode[] _allNodes;
        [SerializeField] private WeaponCratePool _cratePool;
        [SerializeField] private ActiveWeapon _playerActiveWeapon;

        [Header("Settings")]
        [SerializeField] private int _activeNodeCount = 6;
        [SerializeField] private float _midRoundActivationInterval = 30f;

        public WeaponSpawnNode[] GetAllNodes() => _allNodes;

        private void Start()
{
            if (_allNodes == null || _allNodes.Length == 0)
            {
                Debug.LogWarning("[WeaponSpawnManager] No nodes assigned!");
                return;
            }

            ActivateRandomSubset(_activeNodeCount);
            StartCoroutine(MidRoundActivationRoutine());
        }

        private void ActivateRandomSubset(int count)
        {
            var nodes = _allNodes.ToList();
            Shuffle(nodes);

            int activated = 0;
            for (int i = 0; i < nodes.Count && activated < count; i++)
            {
                if (nodes[i].State == NodeState.Inactive)
                {
                    nodes[i].Activate(_cratePool.GetRandom(), _playerActiveWeapon);
                    activated++;
                }
            }
        }

        private IEnumerator MidRoundActivationRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(_midRoundActivationInterval);

                var inactiveNodes = _allNodes.Where(n => n.State == NodeState.Inactive).ToList();
                if (inactiveNodes.Count > 0)
                {
                    int randomIndex = Random.Range(0, inactiveNodes.Count);
                    inactiveNodes[randomIndex].Activate(_cratePool.GetRandom(), _playerActiveWeapon);
                }
            }
        }

        private void Shuffle<T>(IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = Random.Range(0, n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        /*
        Scene setup instructions:
        1. Create empty GO "WeaponSpawnManager" in scene
        2. Add WeaponSpawnManager component
        3. Drag all 12 SpawnNode GameObjects into _allNodes array
        4. Assign DefaultCratePool.asset to _cratePool
        5. Assign Player's ActiveWeapon component to _playerActiveWeapon
        */
    }
}
