using UnityEngine;
using VelocityWars;

namespace Shootopia
{
    /// <summary>
    /// TPS attachment data for a weapon. Keeps <see cref="WeaponSO"/> (gameplay
    /// stats) clean by storing only how the weapon mounts to the hand and where
    /// the IK grips / muzzle live. Character-agnostic: grips are named children
    /// on the visual prefab, resolved at runtime so any Humanoid mesh works.
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponAttach", menuName = "Shootopia/Weapon Attach")]
    public class WeaponAttachSO : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("FALLBACK ONLY. Stance is now derived from gameplayData.handedness (OneHanded -> HandGun, TwoHanded -> AR). This value is used only when gameplayData is null. 0 = AR, 1 = HandGun.")]
        public int stanceId = 0;

        [Tooltip("Gameplay data (damage, fire rate, durability, projectile). Reused from the existing weapon system.")]
        public WeaponSO gameplayData;

        [Header("Visual")]
        [Tooltip("Mesh prefab instantiated under WeaponSocket_R. Pack meshes (AssaultRifle/Pistol) align with the grip clips.")]
        public GameObject visualPrefab;

        [Tooltip("Local position offset applied to the visual under the hand socket.")]
        public Vector3 equipLocalPos = Vector3.zero;

        [Tooltip("Local euler rotation offset applied to the visual under the hand socket.")]
        public Vector3 equipLocalEuler = Vector3.zero;

        [Header("Grip / Muzzle child names (on visualPrefab)")]
        [Tooltip("Child transform marking the barrel tip (forward = +Z). Projectiles spawn here.")]
        public string muzzleChildName = "Muzzle";

        [Tooltip("Child transform for the primary (right hand) grip.")]
        public string gripRChildName = "GripR";

        [Tooltip("Child transform for the support (left hand) foregrip.")]
        public string gripLChildName = "GripL";

        [Header("Grip placement")]
        [Tooltip("If true, on equip the gun is slid so GripR coincides with the hand socket. " +
                 "The right hand then holds the gun by construction (no right-hand IK / no feedback drift).")]
        public bool snapRightGripToHand = false;

        [Header("IK Tuning (corrective — clips already grip)")]
        [Range(0f, 1f)] public float rightHandWeight = 1f;
        [Range(0f, 1f)] public float leftHandWeight = 1f;

        [Tooltip("World-ish hint offset (relative to the visual) to keep the left elbow from twisting.")]
        public Vector3 leftElbowHint = new Vector3(-0.3f, -0.2f, 0.2f);

        [Tooltip("World-ish hint offset (relative to the visual) to keep the right elbow from twisting.")]
        public Vector3 rightElbowHint = new Vector3(0.3f, -0.2f, -0.1f);
    }
}
