using System.Collections;
using UnityEngine;

public class SpawnGate : MonoBehaviour
{
    [SerializeField] GameObject robotPrefab;
    [SerializeField] float spawnTime = 5f;
    [SerializeField] Transform spawnPoint;

    PlayerHealth player;

    void Start()
    {
        player = FindFirstObjectByType<PlayerHealth>();
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            if (player == null)
            {
                player = FindFirstObjectByType<PlayerHealth>();
            }

            bool canSpawn = player != null && 
                            GameManager.Instance != null && 
                            GameManager.Instance.CurrentState == GameState.Playing;

            if (canSpawn && robotPrefab != null && spawnPoint != null)
            {
                Instantiate(robotPrefab, spawnPoint.position, transform.rotation);
            }
            
            yield return new WaitForSeconds(spawnTime);
        }
    }
}
