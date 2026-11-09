using UnityEngine;
using Unity.Cinemachine;
using Unity.Netcode;

namespace Shootopia
{
    public partial class WeaponController
    {
        private readonly NetworkVariable<Unity.Collections.FixedString64Bytes> _equippedAttachName = 
            new NetworkVariable<Unity.Collections.FixedString64Bytes>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _equippedAttachName.OnValueChanged += OnEquippedAttachNameChanged;

            if (IsOwner)
            {
                if (playerFollowCamera == null) playerFollowCamera = FindAnyObjectByType<CinemachineCamera>();
                _adsHandler.SetCamera(playerFollowCamera);
            }

            string currentNetName = _equippedAttachName.Value.ToString();
            if (!string.IsNullOrEmpty(currentNetName)) EquipAttachByNameLocal(currentNetName, forceFresh: false);
        }

        public override void OnNetworkDespawn()
        {
            _adsHandler.ExitADS();
            _chargeHandler.CancelCharge();
            _aimHandler.DisableBouncePreview();
            base.OnNetworkDespawn();
            _equippedAttachName.OnValueChanged -= OnEquippedAttachNameChanged;
        }

        private void OnDisable()
        {
            _adsHandler?.ExitADS();
            _chargeHandler?.CancelCharge();
            _aimHandler?.DisableBouncePreview();
        }

        private void OnEquippedAttachNameChanged(Unity.Collections.FixedString64Bytes previousValue, Unity.Collections.FixedString64Bytes newValue)
        {
            string newName = newValue.ToString();
            if (!string.IsNullOrEmpty(newName)) EquipAttachByNameLocal(newName, forceFresh: false);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void FireProjectileRpc(int weaponIndex, Vector3 origin, Quaternion rotation, Vector3 velocity)
        {
            if (IsOwner) return;

            WeaponAttachSO attach = (loadout != null && weaponIndex >= 0 && weaponIndex < loadout.Length) ? loadout[weaponIndex] : CurrentAttach;
            if (attach?.gameplayData == null) return;
            var data = attach.gameplayData;

            if (data.audio?.fireClip != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFXAtPosition(data.audio.fireClip, origin);

            if (data.projectilePrefab != null)
            {
                var proj = GlobalPoolManager.Instance != null ? GlobalPoolManager.Instance.Spawn(data.projectilePrefab, origin, rotation) : Instantiate(data.projectilePrefab, origin, rotation);
                proj.GetComponent<BaseProjectile>()?.Init(data, gameObject);
                IgnoreOwnerCollisions(proj);
                proj.GetComponent<TrapBase>()?.Initialise((int)OwnerClientId, null, data, velocity);
            }
            else if (Physics.Raycast(origin, rotation * Vector3.forward, out RaycastHit hit, hitRange, hitMask, QueryTriggerInteraction.Ignore))
            {
                GameObject hitVFX = data.HitVFXPrefab ?? fallbackHitscanImpactVFX;
                if (hitVFX != null)
                {
                    var vfx = GlobalPoolManager.Instance != null ? GlobalPoolManager.Instance.Spawn(hitVFX, hit.point, Quaternion.LookRotation(hit.normal)) : Instantiate(hitVFX, hit.point, Quaternion.LookRotation(hit.normal));
                    if (vfx != null && vfx.GetComponent<PooledParticleEffect>() == null) vfx.AddComponent<PooledParticleEffect>();
                }
                if (hitscanImpactSound != null) AudioSource.PlayClipAtPoint(hitscanImpactSound, hit.point, 0.6f);
            }

            if (_muzzleFlash != null) _muzzleFlash.Play();
        }

        private void IgnoreOwnerCollisions(GameObject projectileObj)
        {
            if (projectileObj == null) return;
            var projCols = projectileObj.GetComponentsInChildren<Collider>(true);
            if (_ownerColliders == null || _ownerColliders.Length == 0) _ownerColliders = GetComponentsInChildren<Collider>(true);

            for (int i = 0; i < projCols.Length; i++)
            {
                var pCol = projCols[i];
                if (pCol == null || pCol is CharacterController) continue;
                for (int j = 0; j < _ownerColliders.Length; j++)
                {
                    var oCol = _ownerColliders[j];
                    if (oCol == null || oCol is CharacterController || pCol == oCol) continue;
                    Physics.IgnoreCollision(pCol, oCol, true);
                }
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void FireWeaponServerRpc(Vector3 firePoint, Vector3 aimDir, string weaponName, float clientTimestamp = 0f)
        {
            if (!IsServer) return;

            var attach = CurrentAttach ?? GetAttachByName(weaponName);
            if (attach?.gameplayData == null || attach.gameplayData.projectilePrefab != null) return;
            var data = attach.gameplayData;

            bool hasHit = (LagCompensationManager.Instance != null && clientTimestamp > 0.001f)
                ? LagCompensationManager.Instance.RaycastLagCompensated(clientTimestamp, firePoint, aimDir, hitRange, hitMask, NetworkObjectId, out RaycastHit hit)
                : Physics.Raycast(firePoint, aimDir, out hit, hitRange, hitMask, QueryTriggerInteraction.Ignore);

            if (hasHit)
            {
                var targetHealth = hit.collider.GetComponentInParent<PlayerHealth>() ?? hit.collider.GetComponent<PlayerHealth>();
                var damageable = hit.collider.GetComponentInParent<IDamageable>() ?? hit.collider.GetComponent<IDamageable>();

                if (targetHealth != null || damageable != null)
                {
                    bool wasAlive = targetHealth != null && targetHealth.GetCurrentHp() > 0f;
                    if (targetHealth != null)
                    {
                        targetHealth.LastAttackerClientId = OwnerClientId;
                        targetHealth.TakeDamage(data.Damage, DamageType.Bullet, data, OwnerClientId);
                    }
                    else damageable.TakeDamage(data.Damage, DamageType.Bullet, data);

                    if (wasAlive && targetHealth != null && targetHealth.GetCurrentHp() <= 0f)
                    {
                        NetworkedGameManager.ReportKill(OwnerClientId, targetHealth.OwnerClientId, weaponName);
                    }
                }
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestEquipServerRpc(string attachName)
        {
            if (!IsServer) return;
            _equippedAttachName.Value = attachName;
            ForceEquipWeaponRpc(attachName);
        }

        [Rpc(SendTo.Everyone)]
        private void ForceEquipWeaponRpc(string attachName) => EquipAttachByNameLocal(attachName, forceFresh: true);

        [Rpc(SendTo.NotMe)]
        private void StartChargeRpc() => StartChargeLocal();

        [Rpc(SendTo.NotMe)]
        private void CancelChargeRpc(bool exitCombatStance = true) => CancelChargeLocal(exitCombatStance);

        public void EquipNetworkedWeapon(string attachName)
        {
            if (string.IsNullOrEmpty(attachName)) return;
            if (IsServer)
            {
                _equippedAttachName.Value = attachName;
                ForceEquipWeaponRpc(attachName);
            }
            else RequestEquipServerRpc(attachName);
        }

        public void UnequipAll()
        {
            if (IsServer)
            {
                _equippedAttachName.Value = "";
                ForceEquipWeaponRpc("");
            }
        }

        public void UnequipAllLocal() => ResetWeaponState();
    }
}

