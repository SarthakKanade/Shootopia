using UnityEngine;
using System.Collections;

namespace Shootopia
{
    public class SmokeCanistarController : TrapBase
    {
        [SerializeField] private GameObject smokeCloudPrefab;
        private Rigidbody rb;
        private bool _activated = false;
        private WeaponSO _data;
        private Light _blinkLight;

        private void Awake()
        {
            _blinkLight = GetComponentInChildren<Light>();
        }

        protected override void OnInitialised(WeaponSO data)
        {
            _data = data;
            rb = GetComponent<Rigidbody>();
            
            float angle = data != null ? data.throwAngle : 30f;
            float force = data != null ? data.throwForce : 12f;

            if (_cam != null)
            {
                Vector3 throwDir = Quaternion.AngleAxis(-angle, _cam.transform.right) * _cam.transform.forward;
                rb.AddForce(throwDir * force, ForceMode.VelocityChange);
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
            
            if (smokeCloudPrefab != null)
            {
                GameObject cloud = Instantiate(smokeCloudPrefab, transform.position, Quaternion.identity);
                cloud.GetComponent<SmokeCloud>()?.Initialize(_data, _ownerID);
            }
            
            Destroy(gameObject);
        }
    }
}
