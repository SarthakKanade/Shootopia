using UnityEngine;

namespace Shootopia
{
    public class SpawnPoint : MonoBehaviour
    {
        [SerializeField] private SpawnPointType _type = SpawnPointType.FFA;
        public SpawnPointType Type => _type;

        private void OnDrawGizmos()
        {
            Gizmos.color = _type == SpawnPointType.FFA ? Color.green :
                           _type == SpawnPointType.TeamBlue ? Color.blue : Color.red;
            
            // Draw visual representation in the editor
            Gizmos.DrawSphere(transform.position, 0.5f);
            Gizmos.DrawRay(transform.position, transform.forward * 1.0f);
        }
    }
}
