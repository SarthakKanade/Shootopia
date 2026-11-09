using UnityEngine;

namespace Shootopia
{
    /// <summary>
    /// Tag component attached to pooled GameObjects to identify their originating prefab.
    /// </summary>
    public class PooledInstance : MonoBehaviour
    {
        public int PrefabInstanceId { get; set; }
        public int SpawnVersion { get; set; }
        public bool IsInPool { get; set; }
    }
}
