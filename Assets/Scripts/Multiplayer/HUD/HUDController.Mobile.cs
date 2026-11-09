using UnityEngine;

namespace Shootopia
{
    public partial class HUDController
    {
private void EnsureMobileControls()
        {
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            }

            if (_mobileControlsInstance != null || MobileControlsCanvas.Instance != null) return;

            GameObject prefabToSpawn = mobileControlsPrefab;
            if (prefabToSpawn == null)
            {
                prefabToSpawn = Resources.Load<GameObject>("UI/MobileControlsCanvas");
            }

            if (prefabToSpawn != null)
            {
                _mobileControlsInstance = Instantiate(prefabToSpawn);
                _mobileControlsInstance.name = "MobileControlsCanvas";
            }
        }

        private void HandleControlModeChanged(InputControlMode mode)
        {
            ApplyControlModeLayout(mode);
        }

        private void ApplyControlModeLayout(InputControlMode mode)
        {
            bool isMobile = (mode == InputControlMode.MobileTouch);
            if (playerStatusGroup != null)
            {
                playerStatusGroup.anchoredPosition = isMobile
                    ? _defaultPlayerStatusPos + mobilePlayerStatusOffset
                    : _defaultPlayerStatusPos;
            }
            if (weaponStatusGroup != null)
            {
                weaponStatusGroup.anchoredPosition = isMobile
                    ? _defaultWeaponStatusPos + mobileWeaponStatusOffset
                    : _defaultWeaponStatusPos;
            }
        }
    }
}
