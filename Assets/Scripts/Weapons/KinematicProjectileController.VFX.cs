using UnityEngine;

namespace Shootopia
{
    public partial class KinematicProjectileController
    {
        private void SpawnExplosionVFX(RaycastHit hit)
        {
            Quaternion hitRotation = hit.normal != Vector3.zero
                ? (Mathf.Abs(hit.normal.y) > 0.95f ? Quaternion.LookRotation(hit.normal, Vector3.forward) : Quaternion.LookRotation(hit.normal, Vector3.up))
                : Quaternion.LookRotation(Vector3.up, Vector3.forward);

            bool isPlayerHit = hit.collider != null && (
                hit.collider.GetComponentInParent<PlayerHealth>() != null ||
                hit.collider.GetComponent<PlayerHealth>() != null ||
                hit.collider.CompareTag("Player"));

            GameObject vfxToSpawn = null;
            if (isPlayerHit)
            {
                if (playerHitVFXPrefab != null) vfxToSpawn = playerHitVFXPrefab;
                else if (_weaponData != null && _weaponData.playerHitVFXPrefab != null) vfxToSpawn = _weaponData.playerHitVFXPrefab;
                else if (_weaponData != null && _weaponData.HitVFXPrefab != null) vfxToSpawn = _weaponData.HitVFXPrefab;
            }
            else
            {
                if (environmentHitVFXPrefab != null) vfxToSpawn = environmentHitVFXPrefab;
                else if (_weaponData != null && _weaponData.HitVFXPrefab != null) vfxToSpawn = _weaponData.HitVFXPrefab;
            }

            if (vfxToSpawn != null)
            {
                var spawned = Instantiate(vfxToSpawn, hit.point, hitRotation);
                ApplyExplosionModifiers(spawned);
            }

            if (extraExplosionPrefabs != null)
            {
                foreach (var extra in extraExplosionPrefabs)
                {
                    if (extra != null)
                    {
                        var spawnedExtra = Instantiate(extra, hit.point, hitRotation);
                        ApplyExplosionModifiers(spawnedExtra);
                    }
                }
            }
        }

        private void ApplyExplosionModifiers(GameObject vfx)
        {
            if (vfx == null) return;
            vfx.transform.localScale = Vector3.one * explosionScale;

            foreach (var ps in vfx.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main;
                main.simulationSpeed *= explosionSpeed;
            }

            foreach (var audio in vfx.GetComponentsInChildren<AudioSource>(true))
            {
                audio.volume = Mathf.Clamp01(audio.volume * 1.5f);
                audio.pitch *= 0.7f;
            }
        }
    }
}
