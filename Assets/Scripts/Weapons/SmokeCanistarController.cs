using UnityEngine;
using System.Collections;

namespace Shootopia
{
    public class SmokeCanistarController : TrapBase
    {
        [SerializeField] private GameObject smokeCloudPrefab;
        [SerializeField] private GameObject blastVFXPrefab;
        private Rigidbody rb;
        private bool _activated = false;
        private WeaponSO _data;
        private Light _blinkLight;

        private void Awake()
        {
            _blinkLight = GetComponentInChildren<Light>();
        }

        protected override void OnInitialised(WeaponSO data, Vector3 launchVelocity)
        {
            _data = data;
            rb = GetComponent<Rigidbody>();
            
            if (rb != null)
            {
                if (launchVelocity.sqrMagnitude > 0.01f)
                {
                    rb.linearVelocity = launchVelocity;
                }
                else if (_cam != null)
                {
                    float angle = data != null ? data.throwAngle : 20f;
                    float force = data != null ? data.throwForce : 14f;
                    Vector3 throwDir = Quaternion.AngleAxis(-angle, _cam.transform.right) * _cam.transform.forward;
                    rb.linearVelocity = throwDir * force;
                }
            }

            StartCoroutine(FuseRoutine());
            StartCoroutine(BlinkRoutine());
        }

        private IEnumerator FuseRoutine()
        {
            float fuse = _data != null ? _data.fuseTime : 2.5f;
            yield return new WaitForSeconds(fuse);
            ActivateCloud();
        }

        private IEnumerator BlinkRoutine()
        {
            float start = Time.time;
            float fuseTime = _data != null ? _data.fuseTime : 2.5f;
            while (!_activated)
            {
                float elapsed = Time.time - start;
                float interval = Mathf.Lerp(0.5f, 0.1f, elapsed / fuseTime);
                if (_blinkLight != null)
                {
                    _blinkLight.enabled = !_blinkLight.enabled;
                }
                yield return new WaitForSeconds(interval);
            }
        }

        private void ActivateCloud()
        {
            if (_activated) return;
            _activated = true;

            var blastVFX = blastVFXPrefab != null ? blastVFXPrefab : (_data != null ? _data.HitVFXPrefab : null);
            if (blastVFX != null)
            {
                GameObject blast = Instantiate(blastVFX, transform.position, Quaternion.identity);
                Destroy(blast, 3f);
            }
            
            if (smokeCloudPrefab != null)
            {
                GameObject cloud = Instantiate(smokeCloudPrefab, transform.position, Quaternion.identity);
                cloud.GetComponent<SmokeCloud>()?.Initialize(_data, _ownerID);
            }
            
            Destroy(gameObject);
        }
    }
}
