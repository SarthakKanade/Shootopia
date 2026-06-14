using StarterAssets;
using UnityEngine;
using UnityEngine.AI;
using VelocityWars.Enemies;

public class Robot : MonoBehaviour
{
    [SerializeField] private EnemySO _config;

    private FirstPersonController _player;
    private NavMeshAgent _agent;
    private bool _isFrozen = false;
    public bool IsFrozen => _isFrozen;
    private Renderer[] _renderers;
    private Color[] _originalColors;
    private Coroutine _statusEffectRoutine;
    private Coroutine _burnRoutine;
    [SerializeField] private GameObject _afterburnVFXPrefab;
    private GameObject _activeAfterburnVFX;

    private const string PLAYER_STRING = "Player";

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _renderers = GetComponentsInChildren<Renderer>();
        
        if (_renderers != null && _renderers.Length > 0)
        {
            _originalColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                // Accessing .material instances it, so we use .material.color but save it for restoration
                _originalColors[i] = _renderers[i].material.color;
            }
        }

        if (_config != null && _agent != null)
        {
            _agent.speed = _config.moveSpeed;
            _agent.stoppingDistance = 1.5f; // Keep a small distance for the trigger
        }
    }

    private void Start()
    {
        _player = FindFirstObjectByType<FirstPersonController>();
    }

    private void Update()
    {
        if (_config == null || _isFrozen) return;

        if (_player == null)
        {
            _player = FindFirstObjectByType<FirstPersonController>();
        }

        if (_player == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, _player.transform.position);
        if (distanceToPlayer <= _config.detectionRange)
        {
            if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
            {
                _agent.SetDestination(_player.transform.position);
            }
        }
    }

    public void ApplyFrozen(float dur)
    {
        if (_statusEffectRoutine != null) StopCoroutine(_statusEffectRoutine);
        _statusEffectRoutine = StartCoroutine(FrozenRoutine(dur));
    }

    private System.Collections.IEnumerator FrozenRoutine(float dur)
    {
        _isFrozen = true;
        if (_agent != null) _agent.enabled = false;
        
        Color iceColor = new Color(0.53f, 0.86f, 1f); // #88DDFF
        if (_renderers != null)
        {
            foreach (var r in _renderers) r.material.color = iceColor;
        }
        
        yield return new WaitForSeconds(dur);
        
        RestoreFromStatus();
    }

    public void ApplySlow(float intensity, float dur)
    {
        if (_statusEffectRoutine != null) StopCoroutine(_statusEffectRoutine);
        _statusEffectRoutine = StartCoroutine(SlowRoutine(intensity, dur));
    }

    private System.Collections.IEnumerator SlowRoutine(float intensity, float dur)
    {
        if (_agent == null) yield break;

        float originalSpeed = _config != null ? _config.moveSpeed : _agent.speed;
        _agent.speed = originalSpeed * (1f - intensity);
        
        yield return new WaitForSeconds(dur);
        
        _agent.speed = originalSpeed;
        _statusEffectRoutine = null;
    }

    public void ApplyAfterburn(float dps, float duration)
    {
        if (_burnRoutine != null) StopCoroutine(_burnRoutine);
        _burnRoutine = StartCoroutine(BurnRoutine(dps, duration));
    }

    private System.Collections.IEnumerator BurnRoutine(float dps, float dur)
    {
        if (_activeAfterburnVFX == null && _afterburnVFXPrefab != null)
        {
            _activeAfterburnVFX = Instantiate(_afterburnVFXPrefab, transform);
            _activeAfterburnVFX.transform.localPosition = Vector3.up; // Chest height
        }
        if (_activeAfterburnVFX != null) _activeAfterburnVFX.SetActive(true);

        float elapsed = 0f;
        EnemyHealth health = GetComponent<EnemyHealth>();

        while (elapsed < dur)
        {
            if (health != null)
            {
                health.TakeDamage(dps * 0.1f, VelocityWars.Core.DamageType.Environment);
            }
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }

        if (_activeAfterburnVFX != null) _activeAfterburnVFX.SetActive(false);
        _burnRoutine = null;
    }

    public void BreakStatus()
    {
        if (_statusEffectRoutine != null)
        {
            StopCoroutine(_statusEffectRoutine);
            RestoreFromStatus();
        }
    }

    private void RestoreFromStatus()
    {
        _isFrozen = false;
        if (_agent != null) 
        {
            _agent.enabled = true;
            if (_config != null) _agent.speed = _config.moveSpeed;
        }
        
        if (_renderers != null && _originalColors != null)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                _renderers[i].material.color = _originalColors[i];
            }
        }
        _statusEffectRoutine = null;
    }

    private void OnTriggerEnter(Collider other) 
    {
        if (other.CompareTag(PLAYER_STRING)) 
        {
            if (other.TryGetComponent(out VelocityWars.Core.IDamageable damageable))
            {
                damageable.TakeDamage(_config.damage, VelocityWars.Core.DamageType.Melee);
            }

            EnemyHealth enemyHealth = GetComponent<EnemyHealth>();
            enemyHealth.OnDeath();
        }
    }
}
