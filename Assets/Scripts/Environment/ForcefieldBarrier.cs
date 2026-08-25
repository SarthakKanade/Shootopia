using UnityEngine;

namespace Shootopia
{
    /// <summary>
    /// Manages the visual energy barrier and physical containment colliders bounding the gameplay arena.
    /// Prevents players, projectiles, and vehicles from moving out of the play area.
    /// </summary>
    [ExecuteAlways]
    public class ForcefieldBarrier : MonoBehaviour
    {
        [Header("Boundary Dimensions")]
        [SerializeField] private Vector3 center = new Vector3(-5f, 10f, 2.5f);
        [SerializeField] private Vector2 arenaSize = new Vector2(130f, 115f); // Width (X), Depth (Z)
        [SerializeField] private float wallHeight = 20f;

        [Header("Audio / Feedback")]
        [SerializeField] private AudioClip boundaryHitSound;
        [SerializeField] [Range(0f, 1f)] private float hitVolume = 0.6f;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1.0f, 0.35f);
            Gizmos.DrawWireCube(new Vector3(center.x, center.y, center.z), new Vector3(arenaSize.x, wallHeight, arenaSize.y));
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player") || collision.gameObject.name.Contains("Player"))
            {
                if (boundaryHitSound != null && AudioManager.Instance != null && collision.contactCount > 0)
                {
                    AudioManager.Instance.PlaySFXAtPosition(boundaryHitSound, collision.contacts[0].point, hitVolume);
                }
            }
        }
    }
}
