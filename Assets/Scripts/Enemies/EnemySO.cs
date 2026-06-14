using UnityEngine;

namespace VelocityWars.Enemies
{
    [CreateAssetMenu(fileName = "NewEnemyConfig", menuName = "VelocityWars/Enemies/Enemy Config")]
    public class EnemySO : ScriptableObject
    {
        [Header("Stats")]
        public float maxHealth = 100f;
        public float moveSpeed = 3.5f;
        public float detectionRange = 20f;

        [Header("Combat")]
        public float damage = 20f;
        public float attackCooldown = 1f;

        [Header("Velocity Wars — Rewards")]
        public float chargeGrantedOnKill = 0.34f;
        public int scoreValue = 100;

        [Header("VFX")]
        public GameObject deathVFXPrefab;
        public GameObject hitVFXPrefab;
    }
}
