using UnityEngine;

namespace Shootopia
{
    public partial class WeaponController
    {
        private void CacheAnimParameters()
        {
            _validAnimParams.Clear();
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                foreach (var p in animator.parameters) _validAnimParams.Add(p.nameHash);
            }
        }

        private void SafeSetBool(int hash, bool val) { if (animator != null && _validAnimParams.Contains(hash)) animator.SetBool(hash, val); }
        private void SafeSetFloat(int hash, float val) { if (animator != null && _validAnimParams.Contains(hash)) animator.SetFloat(hash, val); }
        private void SafeSetTrigger(int hash) { if (animator != null && _validAnimParams.Contains(hash)) animator.SetTrigger(hash); }
        private int GetUpperBodyLayer() => (animator != null && animator.layerCount > 1) ? 1 : 0;

        private void SafeCrossFade(string stateName, float transitionDuration, int layer, float normalizedTime = 0f)
        {
            if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null || animator.layerCount <= 0) return;
            int validLayer = Mathf.Clamp(layer, 0, animator.layerCount - 1);
            if (animator.HasState(validLayer, Animator.StringToHash(stateName)))
                animator.CrossFade(stateName, transitionDuration, validLayer, normalizedTime);
        }

        private static Transform FindChild(Transform root, string childName)
        {
            if (string.IsNullOrEmpty(childName)) return null;
            if (root.name.Equals(childName, System.StringComparison.OrdinalIgnoreCase)) return root;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name.Equals(childName, System.StringComparison.OrdinalIgnoreCase)) return t;
            return null;
        }
    }
}

