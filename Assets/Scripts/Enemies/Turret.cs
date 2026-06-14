using System.Collections;
using UnityEngine;
using VelocityWars.Enemies;

public class Turret : MonoBehaviour
{
    [SerializeField] private EnemySO _config;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform turretHead;
    [SerializeField] private Transform playerTargetPoint;
    [SerializeField] private Transform projectileSpawnPoint;

    private PlayerHealth _player;

    private void Start()
    {
        _player = FindFirstObjectByType<PlayerHealth>();
        if (_config != null)
        {
            StartCoroutine(FireRoutine());
        }
    }

    private void Update()
    {
        if (_player == null)
        {
            _player = FindFirstObjectByType<PlayerHealth>();
            if (_player != null && playerTargetPoint == null)
            {
                // Try to find a good target point like "PlayerCameraRoot" or just the player transform
                Transform root = _player.transform.Find("PlayerCameraRoot");
                playerTargetPoint = root != null ? root : _player.transform;
            }
        }

        if (playerTargetPoint != null)
        {
            turretHead.LookAt(playerTargetPoint);
        }
    }

    private IEnumerator FireRoutine()
    {
        while (true) 
        {
            float cooldown = _config != null ? _config.attackCooldown : 2f;
            yield return new WaitForSeconds(cooldown);

            if (_player == null) continue;
            
            // Re-check detection range if applicable
            float distanceToPlayer = Vector3.Distance(transform.position, _player.transform.position);
            if (_config != null && distanceToPlayer > _config.detectionRange) continue;

            if (projectilePrefab != null && projectileSpawnPoint != null && playerTargetPoint != null)
            {
                Projectile newProjectile = Instantiate(projectilePrefab, projectileSpawnPoint.position, Quaternion.identity).GetComponent<Projectile>();
                newProjectile.transform.LookAt(playerTargetPoint);
                newProjectile.Init(_config != null ? (int)_config.damage : 1);
            }
        }
    }
}
