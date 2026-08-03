using UnityEngine;

namespace Shootopia
{
    /// <summary>
    /// ScriptableObject configuration holding audio clips for player actions:
    /// damage taken, hit marker confirmation, death, footsteps, and jump.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerAudioSO", menuName = "Scriptable Objects/PlayerAudioSO")]
    public class PlayerAudioSO : ScriptableObject
    {
        [Header("Combat Audio Clips")]
        public AudioClip hitTakenClip;
        public AudioClip hitMarkerClip;
        public AudioClip deathClip;

        [Header("Movement Audio Clips")]
        public AudioClip footstepClip;
        public AudioClip jumpClip;
    }
}
