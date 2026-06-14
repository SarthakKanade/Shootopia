using System.Collections;
using UnityEngine;
using VelocityWars.Core;
using VelocityWars;

namespace VelocityWars.Weapons
{
    public enum NodeState { Inactive, Spawning, Active, Cooldown }

    public class WeaponSpawnNode : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _cooldownDuration = 20f;
        [SerializeField] private GameObject _cratePrefab;
        [SerializeField] private GameEvent _onWeaponPickedUp;

        [Header("State")]
        [SerializeField, ReadOnly] private NodeState _state = NodeState.Inactive;
        [SerializeField, ReadOnly] private WeaponSO _assignedWeapon;

        private GameObject _spawnedCrate;
        private ActiveWeapon _playerActiveWeapon;

        public NodeState State => _state;
        public GameObject SpawnedCrate => _spawnedCrate;

        public void Activate(WeaponSO weapon, ActiveWeapon playerWeapon)
{
            if (_state != NodeState.Inactive) return;

            _assignedWeapon = weapon;
            _playerActiveWeapon = playerWeapon;
            _state = NodeState.Spawning;
            
            StartCoroutine(SpawnRoutine());
            Debug.Log($"[WeaponSpawnNode] {name} activated with {weapon.name}");
        }

        public void Deactivate()
        {
            if (_spawnedCrate != null)
            {
                Destroy(_spawnedCrate);
            }
            _state = NodeState.Inactive;
            _assignedWeapon = null;
        }

        private IEnumerator SpawnRoutine()
        {
            Vector3 targetPos = transform.position;
            Vector3 startPos = targetPos + Vector3.up * 4f;

            if (_cratePrefab != null)
            {
                _spawnedCrate = Instantiate(_cratePrefab, startPos, Quaternion.identity);
                
                var trigger = _spawnedCrate.GetComponent<WeaponCrateTrigger>();
                if (trigger != null)
                {
                    trigger.Init(_assignedWeapon, _playerActiveWeapon, this);
                }

                // Drop animation
                float duration = 0.6f;
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    if (_spawnedCrate == null) yield break;
                    
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    // Ease in for a "heavy" drop feel
                    float easedT = t * t; 
                    _spawnedCrate.transform.position = Vector3.Lerp(startPos, targetPos, easedT);
                    yield return null;
                }

                if (_spawnedCrate != null)
                    _spawnedCrate.transform.position = targetPos;
            }

            _state = NodeState.Active;
        }

        public void OnCratePickedUp()
        {
            if (_state != NodeState.Active) return;

            if (_onWeaponPickedUp != null)
            {
                _onWeaponPickedUp.Raise();
            }

            _spawnedCrate = null; // Crate destroys itself or is destroyed by trigger
            _state = NodeState.Cooldown;
            StartCoroutine(CooldownRoutine());
        }

        public void OnCrateDelivered()
        {
            if (_spawnedCrate != null)
            {
                Destroy(_spawnedCrate);
                _spawnedCrate = null;
            }
            _state = NodeState.Cooldown;
            StartCoroutine(CooldownRoutine());
        }

        private IEnumerator CooldownRoutine()
        {
            yield return new WaitForSeconds(_cooldownDuration);
            _state = NodeState.Inactive;
        }

        private void OnDrawGizmos()
        {
            switch (_state)
            {
                case NodeState.Inactive: Gizmos.color = Color.yellow; break;
                case NodeState.Spawning: Gizmos.color = Color.blue; break;
                case NodeState.Active: Gizmos.color = Color.green; break;
                case NodeState.Cooldown: Gizmos.color = Color.red; break;
            }

            Gizmos.DrawWireSphere(transform.position, 0.8f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 2f);
        }
    }

    // Simple attribute to show read-only fields in inspector
    public class ReadOnlyAttribute : PropertyAttribute { }
}
