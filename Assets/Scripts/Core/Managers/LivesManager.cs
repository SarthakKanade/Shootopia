using System.Collections;
using UnityEngine;
using VelocityWars.Core;

namespace VelocityWars.Managers
{
    public class LivesManager : MonoBehaviour
    {
        [SerializeField] private int _totalLives = 3;
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private bool _isMultiplayer = false;
        [SerializeField] private FloatGameEvent _onLifeLost;
        [SerializeField] private GameEvent _onPlayerDied;

        private int _currentLives;
        private GameObject _player;

        void Start()
        {
            _currentLives = _totalLives;
            _player = GameObject.FindGameObjectWithTag("Player");

            if (_onPlayerDied != null)
                _onPlayerDied.RegisterListener(HandlePlayerDeath);
            
            // Initial UI update
            _onLifeLost?.Raise(_currentLives);
        }

        private void HandlePlayerDeath()
        {
            if (_isMultiplayer)
            {
                StartCoroutine(RespawnPlayer());
                return;
            }

            _currentLives--;
            _onLifeLost?.Raise(_currentLives);

            if (_currentLives <= 0)
            {
                if (GameManager.Instance != null)
                    GameManager.Instance.SetState(GameState.GameOver);
            }
            else
            {
                StartCoroutine(RespawnPlayer());
            }
        }

        private IEnumerator RespawnPlayer()
        {
            yield return new WaitForSeconds(2f);

            if (_player != null && _spawnPoint != null)
            {
                _player.transform.position = _spawnPoint.position;
                _player.SetActive(true);
                
                PlayerHealth health = _player.GetComponent<PlayerHealth>();
                if (health != null)
                    health.Heal(100f);
            }
        }

        public void ResetLives()
        {
            _currentLives = _totalLives;
            _onLifeLost?.Raise(_currentLives);
        }

        void OnDestroy()
        {
            if (_onPlayerDied != null)
                _onPlayerDied.UnregisterListener(HandlePlayerDeath);
        }
    }
}