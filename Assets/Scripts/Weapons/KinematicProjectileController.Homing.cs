using UnityEngine;

namespace Shootopia
{
    public partial class KinematicProjectileController
    {
private void FindHomingTarget()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, homingRange, _homingHitBuffer);
            float minAngle = 360f;
            Transform bestTarget = null;
            int enemyLayer = LayerMask.NameToLayer("Enemy");

            for (int i = 0; i < count; i++)
            {
                var col = _homingHitBuffer[i];
                if (col == null || col.isTrigger) continue;
                if (_owner != null && col.transform.root == _owner.transform.root) continue;

                if (col.GetComponent<IDamageable>() != null || (enemyLayer >= 0 && col.gameObject.layer == enemyLayer))
                {
                    Vector3 toTarget = (col.transform.position - transform.position).normalized;
                    float angle = Vector3.Angle(transform.forward, toTarget);
                    if (angle < minAngle)
                    {
                        minAngle = angle;
                        bestTarget = col.transform;
                    }
                }
            }

            _homingTarget = bestTarget;
        }
    }
}
