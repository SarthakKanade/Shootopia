using UnityEngine;
using VelocityWars.Core;

namespace VelocityWars.Abilities
{
    [CreateAssetMenu(menuName = "VelocityWars/Abilities/TrackerBot")]
    public class TrackerBotAbilitySO : AbilitySO
    {
        public GameObject trackerBotPrefab;
        public float crateLockDuration = 6f;
        public int botHP = 50;

        public override void Activate(GameObject player, GameContext context)
        {
            var spawnManager = Object.FindFirstObjectByType<VelocityWars.Weapons.WeaponSpawnManager>();
            if (spawnManager == null)
            {
                Debug.LogWarning("TrackerBot: No WeaponSpawnManager in scene.");
                return;
            }

            Vector3 spawnPos = player.transform.position + player.transform.forward * 2f;
            UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out UnityEngine.AI.NavMeshHit hit, 3f, UnityEngine.AI.NavMesh.AllAreas);
            Vector3 finalPos = hit.hit ? hit.position : player.transform.position;

            GameObject bot = Instantiate(trackerBotPrefab, finalPos, Quaternion.identity);
            var controller = bot.GetComponent<TrackerBotController>();
            if (controller == null)
            {
                Debug.LogError("TrackerBot prefab is missing TrackerBotController.");
                return;
            }
            controller.Initialise(player.transform, spawnManager);
        }

        public override void Deactivate(GameObject player) { }
}
}