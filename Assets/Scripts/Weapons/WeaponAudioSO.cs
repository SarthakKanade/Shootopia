using UnityEngine;

namespace Shootopia
{
    /// <summary>
    /// ScriptableObject configuration holding audio clips for a weapon type.
    /// Referenced by WeaponSO for modular, asset-based weapon audio.
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponAudioSO", menuName = "Scriptable Objects/WeaponAudioSO")]
    public class WeaponAudioSO : ScriptableObject
    {
        public AudioClip fireClip;
        public AudioClip reloadClip;
        public AudioClip emptyClickClip;
        public AudioClip drawClip;
    }
}
