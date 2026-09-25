using UnityEngine;
using Unity.Cinemachine;

namespace Shootopia
{
    /// <summary>
    /// Centralized 6-DOF procedural camera impulse & recoil manager.
    /// Hooks directly into Cinemachine 3.x impulse listeners to provide
    /// visceral weapon kick, explosion shockwaves, and damage flinches.
    /// </summary>
    public class CameraShakeManager : MonoBehaviour
    {
        public static CameraShakeManager Instance { get; private set; }

        [Header("Global Shake Multiplier")]
        [Range(0f, 2f)]
        [SerializeField] private float shakeMultiplier = 0.35f;

        [Header("Impulse Sources")]
        [SerializeField] private CinemachineImpulseSource recoilSource;
        [SerializeField] private CinemachineImpulseSource explosionSource;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            EnsureImpulseSources();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void EnsureImpulseSources()
        {
            if (recoilSource == null)
            {
                var recoilGo = new GameObject("RecoilImpulseSource");
                recoilGo.transform.SetParent(transform, false);
                recoilSource = recoilGo.AddComponent<CinemachineImpulseSource>();
                recoilSource.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Recoil;
                recoilSource.ImpulseDefinition.ImpulseDuration = 0.16f;
            }

            if (explosionSource == null)
            {
                var explGo = new GameObject("ExplosionImpulseSource");
                explGo.transform.SetParent(transform, false);
                explosionSource = explGo.AddComponent<CinemachineImpulseSource>();
                explosionSource.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Explosion;
                explosionSource.ImpulseDefinition.ImpulseDuration = 0.40f;
            }
        }

        /// <summary>
        /// Generates a caliber-specific camera recoil kick when firing.
        /// </summary>
        public void ShakeWeaponFire(WeaponSO weaponData)
        {
            if (shakeMultiplier <= 0f) return;
            EnsureImpulseSources();

            Vector3 velocity;

            if (weaponData != null)
            {
                switch (weaponData.category)
                {
                    case WeaponCategory.Precision: // Sniper
                        velocity = new Vector3(Random.Range(-0.04f, 0.04f), 0.38f, -0.65f);
                        break;
                    case WeaponCategory.Chaos: // Void Gun / Heavy Launchers
                        velocity = new Vector3(Random.Range(-0.06f, 0.06f), 0.30f, -0.48f);
                        break;
                    case WeaponCategory.Burst: // Machine Gun / Rapid Fire
                        velocity = new Vector3(Random.Range(-0.06f, 0.06f), 0.12f, -0.15f);
                        break;
                    case WeaponCategory.Melee:
                    case WeaponCategory.Trap:
                        velocity = new Vector3(Random.Range(-0.02f, 0.02f), 0.08f, -0.10f);
                        break;
                    default: // Pistol / Default
                        velocity = new Vector3(Random.Range(-0.03f, 0.03f), 0.18f, -0.25f);
                        break;
                }
            }
            else
            {
                velocity = new Vector3(Random.Range(-0.03f, 0.03f), 0.18f, -0.25f);
            }

            recoilSource.GenerateImpulseWithVelocity(velocity * shakeMultiplier);
        }

        /// <summary>
        /// Generates an omnidirectional explosion shockwave with distance falloff.
        /// </summary>
        public void ShakeExplosion(Vector3 explosionWorldPos, float maxRadius, float baseIntensity = 1.0f)
        {
            if (shakeMultiplier <= 0f) return;
            EnsureImpulseSources();

            Camera cam = Camera.main;
            Vector3 camPos = cam != null ? cam.transform.position : transform.position;

            float dist = Vector3.Distance(camPos, explosionWorldPos);
            float radius = Mathf.Max(maxRadius, 5f);
            if (dist > radius) return;

            float normalizedDist = Mathf.Clamp01(dist / radius);
            float falloff = (1.0f - normalizedDist) * (1.0f - normalizedDist); // Quadratic falloff

            Vector3 pushDir = dist > 0.1f ? (camPos - explosionWorldPos).normalized : Vector3.up;
            Vector3 rumble = (pushDir + Random.insideUnitSphere * 0.6f).normalized;

            Vector3 velocity = rumble * (baseIntensity * falloff * 0.95f * shakeMultiplier);
            explosionSource.GenerateImpulseWithVelocity(velocity);
        }

        /// <summary>
        /// Generates a sharp camera flinch when taking damage.
        /// </summary>
        public void ShakeDamage(float damageAmount)
        {
            if (shakeMultiplier <= 0f) return;
            EnsureImpulseSources();

            float intensity = Mathf.Clamp(damageAmount / 40f, 0.25f, 1.2f);
            Vector3 flinch = new Vector3(Random.Range(-0.2f, 0.2f), -0.18f, -0.22f) * (intensity * shakeMultiplier);

            recoilSource.GenerateImpulseWithVelocity(flinch);
        }

        public void SetShakeMultiplier(float mult)
        {
            shakeMultiplier = Mathf.Clamp(mult, 0f, 2f);
        }
    }
}
