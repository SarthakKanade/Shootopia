using UnityEngine;

namespace Shootopia
{
    public enum WeaponCategory { Default, Control, Burst, Precision, Chaos, Trap }
    public enum DurabilityType { Ammo, TimeBased, Uses }
    public enum WeaponHandedness { OneHanded, TwoHanded }

    [CreateAssetMenu(fileName = "WeaponSO", menuName = "Scriptable Objects/WeaponSO")]
    public class WeaponSO : ScriptableObject
    {
        public GameObject weaponPrefab;
        public string weaponName;
        public int Damage = 1;
        public float FireRate = .5f;
        public GameObject HitVFXPrefab;
        public bool isAutomatic = false;
        public bool CanZoom = false;
        public float ZoomAmount = 10f;
        public float ZoomRotationSpeed = .3f;
        public int MagazineSize = 12;

        [Header("Stance / Handling")]
        [Tooltip("OneHanded -> HandGun stance, TwoHanded -> Assault Rifle stance. Drives the Animator 'Stance' param via WeaponController on equip.")]
        public WeaponHandedness handedness = WeaponHandedness.TwoHanded;

        [Header("Velocity Wars — weapon identity")]
        [Tooltip("Used to filter UI categories and spawn rules")]
        public WeaponCategory category = WeaponCategory.Default;

        [Tooltip("How this weapon's uses are tracked")]
        public DurabilityType durabilityType = DurabilityType.Ammo;

        [Tooltip("Max shots before weapon vanishes. Used when durabilityType = Uses")]
        public int maxUses = 5;

        [Tooltip("Seconds before weapon vanishes. Used when durabilityType = TimeBased")]
        public float weaponDuration = 15f;

        [Tooltip("True for crate-only weapons — cannot persist between rounds")]
        public bool isPickupOnly = false;

        [Header("Velocity Gauge")]
        [Tooltip("Percent added to gauge per kill. Set per weapon in Inspector.")]
        public float gaugeKillFill = 20f;

        [Header("Firing Mode")]
        public bool isAutoFire = false;         // hold trigger = continuous fire
        public bool isCharged = false;          // must charge before firing
        public float chargeTime = 0f;           // seconds to full charge
        public bool isContinuous = false;       // flame-style: no projectile, stream damage
        public bool isTrap = false;             // throw-style: instantiate trap prefab not projectile

        [Header("Projectile")]
        public GameObject projectilePrefab;     // prefab to instantiate on fire
        public float projectileSpeed = 20f;
        public bool hasGravity = false;         // true for grenade/smoke arcs
        public float throwAngle = 0f;          // degrees above camera forward (for traps)
        public float throwForce = 12f;         // initial velocity magnitude (for traps)

        [Header("Explosion / AoE")]
        public bool isExplosive = false;
        public float splashDamage = 0f;
        public float blastRadius = 0f;
        public bool selfDamage = false;         // player takes splash from own weapon

        [Header("Status Effects")]
        public float freezeDuration = 0f;
        public float maxSlowPercent = 0f;
        public float minSlowPercent = 0f;
        public float afterburnDPS = 0f;
        public float afterburnDuration = 0f;

        [Header("Void / Gravity")]
        public float pullRadius = 0f;
        public float pullForce = 0f;
        public float voidDuration = 0f;
        public float damageAtCentre = 0f;      // per second
        public float damageAtEdge = 0f;        // per second

        [Header("Bounce (Trick Shot)")]
        public int maxBounces = 0;
        public float projectileLifetime = 0f;
        public float previewDistance = 0f;

        [Header("ADS / Scope")]
        public bool hasADS = false;
        public bool requiresADSToFire = false;
        public float adsFOV = 60f;
        public float adsMoveSpeedMult = 1f;    // 0.6 = 40% penalty

        [Header("Continuous Stream (Flame)")]
        public float damagePerSecond = 0f;
        public float maxRange = 0f;
        public float coneAngle = 0f;
        public float fuelDuration = 0f;
        public float tickRate = 0.1f;

        [Header("Trap Settings")]
        public float fuseTime = 0f;
        public float cloudRadius = 0f;
        public float cloudDuration = 0f;
        public float slowPercent = 0f;
        public float activationDelay = 0f;
        public float triggerRadius = 0f;
        public float armDelay = 0f;
        public bool selfTrigger = false;
        public bool persistOnDeath = false;
        public bool isRare = false;
    }
}
