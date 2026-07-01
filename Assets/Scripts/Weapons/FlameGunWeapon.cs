using UnityEngine;
using System.Collections.Generic;
using Shootopia;

namespace Shootopia
{
    public class FlameGunWeapon : Weapon
    {
        private WeaponSO _so;
        private Transform _muzzlePoint;
        private ParticleSystem _flameParticles;
        private Light _flameLight;
        private float _originalLightIntensity = 3f;
        private bool _isFiring;
        private float _tickTimer;
        private HashSet<IDamageable> _currentlyInStream = new HashSet<IDamageable>();
        private LayerMask _targetLayer;

        public void Initialize(WeaponSO so, Transform muzzle, LayerMask targetLayer)
        {
            _so = so;
            _muzzlePoint = muzzle;
            _targetLayer = targetLayer;

            _flameParticles = GetComponentInChildren<ParticleSystem>();
            _flameLight = GetComponentInChildren<Light>();
        }

        public void SetFiring(bool firing)
        {
            if (_isFiring == firing) return;
            _isFiring = firing;

            if (_flameParticles != null)
            {
                var emission = _flameParticles.emission;
                emission.enabled = _isFiring;
            }

            if (_flameLight != null)
            {
                _flameLight.intensity = _isFiring ? _originalLightIntensity : 0f;
            }

            if (!_isFiring)
            {
                ApplyAfterburnToRemaining();
            }
        }

        private void Update()
        {
            if (!_isFiring || _so == null) return;

            // Handle Light Flicker
            if (_flameLight != null)
            {
                float flicker = Mathf.Lerp(2f, 4f, Mathf.PerlinNoise(Time.time * 5f, 0f));
                _flameLight.intensity = flicker;
            }

            _tickTimer += Time.deltaTime;
            if (_tickTimer >= _so.tickRate)
            {
                _tickTimer = 0f;
                ProcessFlameTick();
            }
        }

        private void ProcessFlameTick()
        {
            HashSet<IDamageable> thisFrame = new HashSet<IDamageable>();
            
            // Cone: OverlapCapsule from muzzle to muzzle + forward * range
            Vector3 start = _muzzlePoint.position;
            Vector3 end = start + Camera.main.transform.forward * _so.maxRange;
            float radius = 0.75f; // From requirements

            Collider[] hits = Physics.OverlapCapsule(start, end, radius, _targetLayer);
            
            foreach (Collider c in hits)
            {
                // Use bounds.center for better angle accuracy (avoids floor-pivot issues)
                Vector3 toTgt = (c.bounds.center - start).normalized;
                if (Vector3.Angle(Camera.main.transform.forward, toTgt) > _so.coneAngle) continue;

                // Search parents for IDamageable (handles child colliders)
                IDamageable t = c.GetComponentInParent<IDamageable>();
                if (t != null && t is MonoBehaviour mb && mb != null)
                {
                    // 45/s via 0.1s ticks = 4.5 damage per tick
                    float damagePerTick = (_so.damagePerSecond * _so.tickRate);
                    t.TakeDamage(damagePerTick, DamageType.Environment, _so);
                    thisFrame.Add(t);
                }
            }

            // Targets leaving stream -> apply afterburn
            foreach (IDamageable prev in _currentlyInStream)
            {
                if (!thisFrame.Contains(prev))
                {
                    ApplyAfterburn(prev);
                }
            }

            _currentlyInStream = thisFrame;
        }

        private void ApplyAfterburnToRemaining()
        {
            foreach (var t in _currentlyInStream)
            {
                ApplyAfterburn(t);
            }
            _currentlyInStream.Clear();
        }

        private void ApplyAfterburn(IDamageable target)
        {
        }
    }
}
