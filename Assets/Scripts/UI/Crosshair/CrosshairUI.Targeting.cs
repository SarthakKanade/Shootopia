using UnityEngine;

namespace Shootopia
{
    public partial class CrosshairUI
    {
#region Hit Marker

        private void UpdateHitMarker()
        {
            if (_hitFlashTimer > 0f)
            {
                _hitFlashTimer -= Time.deltaTime;
                float alpha = Mathf.Clamp01(_hitFlashTimer / 0.15f);
                if (_hitMarkerGroup != null) _hitMarkerGroup.alpha = alpha;

                // Color the hit ticks
                Color tickColor = _hitIsKill ? killColor : hitColor;
                tickColor.a = alpha;
                SetHitTickColor(tickColor);
            }
            else if (_hitMarkerGroup != null && _hitMarkerGroup.alpha > 0f)
            {
                _hitMarkerGroup.alpha = 0f;
            }
        }

        private void OnHitConfirmed(bool isKill)
        {
            _hitIsKill = isKill;
            _hitFlashTimer = isKill ? 0.30f : 0.18f;
            if (_hitMarkerGroup != null) _hitMarkerGroup.alpha = 1f;
            SetHitTickColor(isKill ? killColor : hitColor);
        }

        private void SetHitTickColor(Color c)
        {
            if (_hitTL != null) _hitTL.color = c;
            if (_hitTR != null) _hitTR.color = c;
            if (_hitBL != null) _hitBL.color = c;
            if (_hitBR != null) _hitBR.color = c;
        }

        #endregion

        #region Enemy Aim Detection

        private void UpdateTargetDetection()
        {
            _targetCheckTimer -= Time.deltaTime;
            if (_targetCheckTimer <= 0f)
            {
                _targetCheckTimer = 0.033f; // ~30Hz raycast check
                _isTargetingEnemy = CheckAimAtEnemy();
            }

            Color targetColor = _isTargetingEnemy ? enemyTargetColor : normalColor;
            SetCrosshairColor(targetColor);
        }

        private bool CheckAimAtEnemy()
        {
            Camera cam = Camera.main;
            if (cam == null) return false;

            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (!Physics.Raycast(ray, out RaycastHit hit, 200f, ~0, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            if (hit.collider == null) return false;

            // Check if hit object belongs to a Player
            var health = hit.collider.GetComponentInParent<PlayerHealth>() ?? hit.collider.GetComponent<PlayerHealth>();
            if (health == null)
            {
                var receiver = hit.collider.GetComponentInParent<ShieldColliderReceiver>() ?? hit.collider.GetComponent<ShieldColliderReceiver>();
                if (receiver != null) health = receiver.GetComponentInParent<PlayerHealth>();
            }

            if (health != null)
            {
                if (health.IsDead) return false;

                // Check if target is locally owned player
                bool isLocal = false;
                if (Unity.Netcode.NetworkManager.Singleton != null && 
                    Unity.Netcode.NetworkManager.Singleton.LocalClient != null &&
                    Unity.Netcode.NetworkManager.Singleton.LocalClient.PlayerObject != null)
                {
                    isLocal = (health.gameObject == Unity.Netcode.NetworkManager.Singleton.LocalClient.PlayerObject.gameObject);
                }
                else if (_cachedWC != null)
                {
                    isLocal = (health.gameObject == _cachedWC.gameObject);
                }
                if (isLocal) return false;

                // Check invisibility (don't reveal invisible enemies)
                var invis = health.GetComponent<PlayerInvisibility>() ?? health.GetComponentInParent<PlayerInvisibility>();
                if (invis != null && invis.IsInvisible) return false;

                return true;
            }

            // Fallback: Check general IDamageable (e.g. dummies or destructible targets)
            var damageable = hit.collider.GetComponentInParent<IDamageable>() ?? hit.collider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                if (_cachedWC != null && hit.collider.transform.IsChildOf(_cachedWC.transform)) return false;
                return true;
            }

            return false;
        }

        private void SetCrosshairColor(Color c)
        {
            if (_dot != null) _dot.color = c;
            if (_lineTop != null) _lineTop.color = c;
            if (_lineBottom != null) _lineBottom.color = c;
            if (_lineLeft != null) _lineLeft.color = c;
            if (_lineRight != null) _lineRight.color = c;
        }

        #endregion
    }
}
