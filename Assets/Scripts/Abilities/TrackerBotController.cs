using UnityEngine;
using UnityEngine.AI;
using VelocityWars.Core;
using VelocityWars.Weapons;

namespace VelocityWars.Abilities
{
    using VelocityWars;
    [RequireComponent(typeof(NavMeshAgent))]
public class TrackerBotController : MonoBehaviour, IDamageable, IKillable
    {
        [Header("Settings")]
        [SerializeField] private float _maxHP = 50f;
        [SerializeField] private float _lifespan = 15f;
        [SerializeField] private float _arrivalThreshold = 1.2f;
        [SerializeField] private float _crateDeliverDuration = 2f;
        
        [Header("Visuals & UI")]
        [SerializeField] private Animator _animator;
        [SerializeField] private GameObject _hpBarContainer;
        [SerializeField] private UnityEngine.UI.Image _hpBarFill;
        [SerializeField] private MeshRenderer _visorRenderer;
        [SerializeField] private Light _visorLight;
        [SerializeField] private GameObject _deathVFXPrefab;

        private float _currentHP;
        private Transform _playerTransform;
        private WeaponSpawnManager _spawnManager;
        private NavMeshAgent _agent;
        private bool _isDead;
        private Material _visorMaterial;
        private Color _normalVisorColor;
        private float _normalLightIntensity;

        public void Initialise(Transform playerTransform, WeaponSpawnManager spawnManager)
        {
            _playerTransform = playerTransform;
            _spawnManager = spawnManager;
            _currentHP = _maxHP;
            _agent = GetComponent<NavMeshAgent>();

            if (_visorRenderer != null)
            {
                _visorMaterial = _visorRenderer.material;
                // Use "emissiveFactor" for glTF shaders or fallback to "_EmissionColor"
                string propName = _visorMaterial.HasProperty("emissiveFactor") ? "emissiveFactor" : "_EmissionColor";
                _normalVisorColor = _visorMaterial.GetColor(propName);
            }

            if (_visorLight != null)
            {
                _normalLightIntensity = _visorLight.intensity;
            }

            if (_hpBarContainer != null) _hpBarContainer.SetActive(false);

            StartCoroutine(LifespanRoutine());
            StartCoroutine(DeliverLoop());
        }

        private void Update()
        {
            if (_isDead || _agent == null) return;
            
            float speed = _agent.velocity.magnitude;
            
            if (_animator != null && _animator.runtimeAnimatorController != null)
            {
                _animator.SetFloat("Speed", speed);
            }
            else
            {
                // Procedural Animation Fallback
                ProceduralAnimate(speed);
            }
        }

        private void ProceduralAnimate(float speed)
        {
            // Simple bobbing
            float bobSpeed = speed > 0.1f ? 10f : 2f;
            float bobAmount = speed > 0.1f ? 0.05f : 0.02f;
            float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmount;
            
            // Apply bob to the visual child to keep the root on the NavMesh
            if (transform.childCount > 0)
            {
                Transform visuals = transform.Find("Visuals");
                if (visuals != null)
                {
                    visuals.localPosition = new Vector3(0, bob, 0);
                    
                    // Tilt forward when moving
                    if (speed > 0.1f)
                    {
                        visuals.localRotation = Quaternion.Euler(5f, 0, Mathf.Sin(Time.time * 8f) * 2f);
                    }
                    else
                    {
                        visuals.localRotation = Quaternion.identity;
                    }
                }
            }
        }

        private System.Collections.IEnumerator LifespanRoutine()
        {
            yield return new WaitForSeconds(_lifespan);
            if (!_isDead) OnDeath();
        }

        private System.Collections.IEnumerator DeliverLoop()
        {
            while (!_isDead)
            {
                WeaponSpawnNode target = FindNearestActiveNode();
                if (target == null)
                {
                    yield return new WaitForSeconds(1f);
                    continue;
                }

                _agent.SetDestination(target.transform.position);

                yield return new WaitUntil(() =>
                    !_agent.pathPending &&
                    _agent.remainingDistance <= _arrivalThreshold);

                if (_isDead) yield break;

                // Re-check: another system may have claimed the crate
                if (target.SpawnedCrate == null) continue;

                yield return StartCoroutine(DeliverCrate(target));
            }
        }

        private System.Collections.IEnumerator DeliverCrate(WeaponSpawnNode node)
        {
            GameObject crate = node.SpawnedCrate;
            if (crate == null) yield break;

            node.OnCrateDelivered(); // node enters cooldown, its internal ref cleared

            float elapsed = 0f;
            Vector3 startPos = crate.transform.position;

            while (elapsed < _crateDeliverDuration && crate != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _crateDeliverDuration;
                // Chase current player position each frame — intentional moving target
                crate.transform.position = Vector3.Lerp(startPos, _playerTransform.position, t);
                yield return null;
            }
            // Crate is now at player feet. Existing trigger handles pickup.
        }

        private WeaponSpawnNode FindNearestActiveNode()
        {
            WeaponSpawnNode[] allNodes = _spawnManager.GetAllNodes();
            WeaponSpawnNode nearest = null;
            float minDist = float.MaxValue;

            foreach (var node in allNodes)
            {
                if (node.State != NodeState.Active) continue;
                float dist = Vector3.Distance(transform.position, node.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = node;
                }
            }
            return nearest;
        }

        public void TakeDamage(float amount, DamageType type = DamageType.Projectile, WeaponSO weaponUsed = null)
        {
            if (_isDead) return;
            _currentHP -= amount;

            if (_hpBarContainer != null && !_hpBarContainer.activeSelf)
                _hpBarContainer.SetActive(true);

            UpdateHPBar();
            StartCoroutine(FlickerVisor());

            if (_currentHP <= 0f) OnDeath();
        }

        private void UpdateHPBar()
        {
            if (_hpBarFill == null) return;

            float fill = _currentHP / _maxHP;
            _hpBarFill.fillAmount = fill;

            Color hpColor;
            if (fill < 0.3f)
            {
                ColorUtility.TryParseHtmlString("#FF2200", out hpColor);
            }
            else
            {
                ColorUtility.TryParseHtmlString("#FF6B00", out hpColor);
            }
            _hpBarFill.color = hpColor;
        }

        private System.Collections.IEnumerator FlickerVisor()
        {
            if (_visorMaterial == null && _visorLight == null) yield break;

            string propName = _visorMaterial != null && _visorMaterial.HasProperty("emissiveFactor") ? "emissiveFactor" : "_EmissionColor";
            float flickerDuration = 0.1f;
            if (_visorMaterial != null) _visorMaterial.SetColor(propName, Color.black);
            if (_visorLight != null) _visorLight.intensity = 0f;

            yield return new WaitForSeconds(flickerDuration);

            if (_visorMaterial != null) _visorMaterial.SetColor(propName, _normalVisorColor);
            if (_visorLight != null) _visorLight.intensity = _normalLightIntensity;
        }

        public void OnDeath()
        {
            if (_isDead) return;
            _isDead = true;

            if (_agent != null) _agent.enabled = false;
            StopAllCoroutines();

            if (_animator != null)
            {
                _animator.SetTrigger("Death");
                StartCoroutine(DelayedDestroy(0.8f));
            }
            else
            {
                ExecuteDeath();
            }
        }

        private System.Collections.IEnumerator DelayedDestroy(float delay)
        {
            yield return new WaitForSeconds(delay);
            ExecuteDeath();
        }

        private void ExecuteDeath()
        {
            if (_deathVFXPrefab != null)
                Instantiate(_deathVFXPrefab, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
}
}
