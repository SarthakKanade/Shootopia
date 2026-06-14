using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;
using StarterAssets;
using VelocityWars.Core;

namespace VelocityWars
{
    public class SmokeCloud : MonoBehaviour
    {
        private List<IDamageable> _inCloud = new List<IDamageable>();
        private Dictionary<NavMeshAgent, float> _origSpeeds = new Dictionary<NavMeshAgent, float>();
        private Dictionary<FirstPersonController, (float move, float sprint)> _playerOrigSpeeds = new Dictionary<FirstPersonController, (float, float)>();
        
        private WeaponSO _data;
        private int _ownerID;
        private bool _dissipating = false;

        public void Initialize(WeaponSO data, int ownerID)
        {
            _data = data;
            _ownerID = ownerID;

            float radius = _data != null ? _data.cloudRadius : 5f;
            GetComponent<SphereCollider>().radius = radius;
            
            // Adjust particle system radius if needed
            var ps = GetComponentInChildren<ParticleSystem>();
            if (ps != null)
            {
                var shape = ps.shape;
                shape.radius = radius;
            }

            StartCoroutine(ExpandRoutine());
            
            float tick = _data != null ? _data.tickRate : 0.5f;
            InvokeRepeating(nameof(TickDamage), tick, tick);
            
            float duration = _data != null ? _data.cloudDuration : 5f;
            Invoke(nameof(Dissipate), duration);
        }

        private IEnumerator ExpandRoutine()
        {
            float t = 0f;
            float duration = 0.3f;
            Vector3 targetScale = Vector3.one;
            transform.localScale = Vector3.one * 0.01f;

            while (t < duration)
            {
                t += Time.deltaTime;
                transform.localScale = Vector3.Lerp(Vector3.one * 0.01f, targetScale, t / duration);
                yield return null;
            }
            transform.localScale = targetScale;
        }

        private void OnTriggerEnter(Collider col)
        {
            if (_dissipating) return;

            if (col.TryGetComponent(out IDamageable t))
            {
                _inCloud.Add(t);
            }

            float slow = _data != null ? _data.slowPercent : 0.35f;

            // Handle Robots
            NavMeshAgent agent = col.GetComponent<NavMeshAgent>();
            if (agent != null && !_origSpeeds.ContainsKey(agent))
            {
                _origSpeeds[agent] = agent.speed;
                agent.speed *= (1f - slow);
            }

            // Handle Player
            FirstPersonController fpc = col.GetComponent<FirstPersonController>();
            if (fpc != null && !_playerOrigSpeeds.ContainsKey(fpc))
            {
                _playerOrigSpeeds[fpc] = (fpc.MoveSpeed, fpc.SprintSpeed);
                fpc.MoveSpeed *= (1f - slow);
                fpc.SprintSpeed *= (1f - slow);
                
                // Fullscreen tint could be handled here or by a separate singleton
                ScreenTintManager.Instance?.SetTintActive(true);
            }
        }

        private void OnTriggerExit(Collider col)
        {
            if (col.TryGetComponent(out IDamageable t))
            {
                StartCoroutine(RemoveDelay(t));
            }

            // Restore Robot Speed
            NavMeshAgent agent = col.GetComponent<NavMeshAgent>();
            if (agent != null && _origSpeeds.ContainsKey(agent))
            {
                agent.speed = _origSpeeds[agent];
                _origSpeeds.Remove(agent);
            }

            // Restore Player Speed
            FirstPersonController fpc = col.GetComponent<FirstPersonController>();
            if (fpc != null && _playerOrigSpeeds.ContainsKey(fpc))
            {
                fpc.MoveSpeed = _playerOrigSpeeds[fpc].move;
                fpc.SprintSpeed = _playerOrigSpeeds[fpc].sprint;
                _playerOrigSpeeds.Remove(fpc);
                
                ScreenTintManager.Instance?.SetTintActive(false);
            }
        }

        private IEnumerator RemoveDelay(IDamageable t)
        {
            yield return new WaitForSeconds(0.3f);
            if (_inCloud.Contains(t)) _inCloud.Remove(t);
        }

        private void TickDamage()
        {
            float dmg = _data != null ? (_data.damagePerSecond * (_data.tickRate > 0 ? _data.tickRate : 0.5f)) : 5f;
            
            for (int i = _inCloud.Count - 1; i >= 0; i--)
            {
                var target = _inCloud[i];
                if (target != null && (Object)target != null)
                {
                    target.TakeDamage(dmg, DamageType.Environment);
                }
                else
                {
                    _inCloud.RemoveAt(i);
                }
            }
        }

        private void Dissipate()
        {
            _dissipating = true;
            CancelInvoke();
            
            GetComponent<Collider>().enabled = false;
            
            var ps = GetComponentInChildren<ParticleSystem>();
            if (ps != null) ps.Stop();

            // Restore all speeds immediately for anyone left inside
            foreach (var kvp in _origSpeeds)
            {
                if (kvp.Key != null) kvp.Key.speed = kvp.Value;
            }
            foreach (var kvp in _playerOrigSpeeds)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.MoveSpeed = kvp.Value.move;
                    kvp.Key.SprintSpeed = kvp.Value.sprint;
                    ScreenTintManager.Instance?.SetTintActive(false);
                }
            }

            Destroy(gameObject, 3f);
        }
    }
}
