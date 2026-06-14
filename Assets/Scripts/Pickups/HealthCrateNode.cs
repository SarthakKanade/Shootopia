using System.Collections;
using UnityEngine;

namespace VelocityWars.Pickups
{
    public class HealthCrateNode : MonoBehaviour
    {
        [SerializeField] private GameObject _healthCratePrefab;
        [SerializeField] private float _respawnTime = 25f;

        private GameObject _activeCrate;

        void Start()
        {
            SpawnCrate();
        }

        private void SpawnCrate()
        {
            if (_activeCrate != null) return;

            _activeCrate = Instantiate(_healthCratePrefab, transform.position, transform.rotation, transform);
            HealthCrate crate = _activeCrate.GetComponent<HealthCrate>();
            if (crate != null)
                crate.OnPickedUp += OnCratePickedUp;
        }

        private void OnCratePickedUp()
        {
            if (_activeCrate != null)
            {
                HealthCrate crate = _activeCrate.GetComponent<HealthCrate>();
                if (crate != null)
                    crate.OnPickedUp -= OnCratePickedUp;

                Destroy(_activeCrate);
                _activeCrate = null;
            }

            StartCoroutine(RespawnAfterDelay());
        }

        private IEnumerator RespawnAfterDelay()
        {
            yield return new WaitForSeconds(_respawnTime);
            SpawnCrate();
        }

        void OnDestroy()
        {
            if (_activeCrate != null)
            {
                HealthCrate crate = _activeCrate.GetComponent<HealthCrate>();
                if (crate != null)
                    crate.OnPickedUp -= OnCratePickedUp;
            }
        }
    }
}