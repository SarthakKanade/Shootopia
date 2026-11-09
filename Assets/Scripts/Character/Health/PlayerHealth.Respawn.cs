using System.Collections;
using UnityEngine;
using Unity.Netcode;

namespace Shootopia
{
    public partial class PlayerHealth
    {
private IEnumerator RespawnSequence(Vector3 spawnPos)
        {
            // Wait 1.0 second while death animation plays
            yield return new WaitForSeconds(1.0f);

            // Move server representation to spawn point while invisible
            SetCharacterPosition(spawnPos);
            Physics.SyncTransforms();

            // Wait remaining 2.0 seconds (total 3.0s delay) while client transform & buffers settle at spawnPos
            yield return new WaitForSeconds(2.0f);

            StartInvincibilityServer(spawnInvincibilityDuration);

            SetCharacterPosition(spawnPos);
            Physics.SyncTransforms();
            var netTransform = GetComponent<Unity.Netcode.Components.NetworkTransform>();
            if (netTransform != null && netTransform.CanCommitToTransform)
            {
                try { netTransform.Teleport(spawnPos, transform.rotation, transform.localScale); } catch { }
            }
            ResetMovementState();

            _currentHp.Value = maxHp;
            _isDead = false;

            // Ensure player respawns unarmed
            var weaponCtrl = GetComponent<WeaponController>();
            if (weaponCtrl != null) weaponCtrl.UnequipAll();

            RemoveBurnEffect();
            RespawnClientRpc(spawnPos);
        }

private IEnumerator HideModelAndTeleportRoutine(Vector3 spawnPos)
        {
            yield return new WaitForSeconds(1.0f);
            SetPlayerVisibility(false);

            // Teleport to spawn position while invisible so network transform & interpolation buffers settle ahead of time
            Vector3 delta = spawnPos - transform.position;
            SetCharacterPosition(spawnPos);
            Physics.SyncTransforms();

            var netTransform = GetComponent<Unity.Netcode.Components.NetworkTransform>();
            if (netTransform != null && netTransform.CanCommitToTransform)
            {
                try { netTransform.Teleport(spawnPos, transform.rotation, transform.localScale); } catch { }
            }

            if (IsOwner)
            {
                var cinemachineCam = FindAnyObjectByType<Unity.Cinemachine.CinemachineCamera>();
                if (cinemachineCam != null)
                {
                    cinemachineCam.OnTargetObjectWarped(transform, delta);
                }
            }

            _hideModelRoutine = null;
        }

        [ClientRpc]
        private void RespawnClientRpc(Vector3 spawnPosition)
        {
            if (_hideModelRoutine != null)
            {
                StopCoroutine(_hideModelRoutine);
                _hideModelRoutine = null;
            }

            _isDead = false;

            StartInvincibilityLocal(spawnInvincibilityDuration);

            SetCharacterPosition(spawnPosition);
            Physics.SyncTransforms();

            var netTransform = GetComponent<Unity.Netcode.Components.NetworkTransform>();
            if (netTransform != null && netTransform.CanCommitToTransform)
            {
                try { netTransform.Teleport(spawnPosition, transform.rotation, transform.localScale); } catch { }
            }

            ResetLocomotionAnimator();
            ResetMovementState();

            SetCollidersEnabled(true);
            SetPlayerVisibility(true);
            RemoveBurnEffect();

            var weaponCtrl = GetComponent<WeaponController>();
            if (weaponCtrl != null) weaponCtrl.UnequipAllLocal();

            if (spawnVFXPrefab != null) Instantiate(spawnVFXPrefab, spawnPosition, Quaternion.identity);
            if (spawnSound != null) AudioSource.PlayClipAtPoint(spawnSound, spawnPosition, 1f);

            if (IsOwner)
            {
                SetPlayerControlsEnabled(true);
                OnLocalPlayerRespawned?.Invoke();
            }
        }

public void Teleport(Vector3 position)
        {
            if (!IsServer) return;
            SetCharacterPosition(position);
            Physics.SyncTransforms();
            var netTransform = GetComponent<Unity.Netcode.Components.NetworkTransform>();
            if (netTransform != null && netTransform.CanCommitToTransform)
            {
                try { netTransform.Teleport(position, transform.rotation, transform.localScale); } catch { }
            }
            TeleportClientRpc(position);
        }

        [ClientRpc]
        private void TeleportClientRpc(Vector3 position)
        {
            SetCharacterPosition(position);
            Physics.SyncTransforms();
            var netTransform = GetComponent<Unity.Netcode.Components.NetworkTransform>();
            if (netTransform != null && netTransform.CanCommitToTransform)
            {
                try { netTransform.Teleport(position, transform.rotation, transform.localScale); } catch { }
            }
            ResetMovementState();
            RemoveBurnEffect();
        }

        public void ResetStateForMatch(Vector3 position, Quaternion? rotation = null)
        {
            if (!IsServer) return;
            _isDead = false;
            _deathCount.Value = 0;

            SetCharacterPosition(position);
            if (rotation.HasValue) transform.rotation = rotation.Value;
            Physics.SyncTransforms();
            var netTransform = GetComponent<Unity.Netcode.Components.NetworkTransform>();
            if (netTransform != null && netTransform.CanCommitToTransform)
            {
                try { netTransform.Teleport(position, transform.rotation, transform.localScale); } catch { }
            }
            ResetMovementState();
            RemoveBurnEffect();

            _currentHp.Value = maxHp;

            // Ensure player starts match unarmed
            var weaponCtrl = GetComponent<WeaponController>();
            if (weaponCtrl != null) weaponCtrl.UnequipAll();

            // Clear shields and invisibility carried from warmup
            var shield = GetComponent<PlayerShield>();
            if (shield != null) shield.ResetShield();

            var invis = GetComponent<PlayerInvisibility>();
            if (invis != null) invis.ResetInvisibility();

            var gm = GameModeBase.Instance ?? NetworkedGameManager.Instance?.ActiveGameMode;
            bool isMatchActive = gm == null || gm.MatchActive.Value || gm.IsInProgress || (NetworkedGameManager.Instance != null && NetworkedGameManager.Instance.MatchStarted.Value);

            if (isMatchActive)
            {
                StartInvincibilityServer(spawnInvincibilityDuration);
            }
            else
            {
                if (_serverInvincibilityRoutine != null)
                {
                    StopCoroutine(_serverInvincibilityRoutine);
                    _serverInvincibilityRoutine = null;
                }
                _isInvincibleNet.Value = false;
                ShowInvincibilityAura(false);
            }

            ResetPlayerClientRpc(position, rotation.HasValue ? rotation.Value : transform.rotation, isMatchActive);
        }

        [ClientRpc]
        private void ResetPlayerClientRpc(Vector3 position, Quaternion rotation, bool grantInvincibility = false)
        {
            _isDead = false;
            SetCharacterPosition(position);
            transform.rotation = rotation;
            Physics.SyncTransforms();
            var netTransform = GetComponent<Unity.Netcode.Components.NetworkTransform>();
            if (netTransform != null && netTransform.CanCommitToTransform)
            {
                try { netTransform.Teleport(position, rotation, transform.localScale); } catch { }
            }

            SetPlayerControlsEnabled(true);
            SetPlayerVisibility(true);
            ResetMovementState();
            ResetLocomotionAnimator();
            RemoveBurnEffect();

            // Ensure player starts match unarmed on client
            var weaponCtrl = GetComponent<WeaponController>();
            if (weaponCtrl != null) weaponCtrl.UnequipAllLocal();

            var shield = GetComponent<PlayerShield>();
            if (shield != null && !IsServer) shield.ResetShield();

            var invis = GetComponent<PlayerInvisibility>();
            if (invis != null && !IsServer) invis.ResetInvisibility();

            if (grantInvincibility && !IsServer)
            {
                StartInvincibilityLocal(spawnInvincibilityDuration);
            }
            else if (!grantInvincibility)
            {
                if (_localInvincibilityRoutine != null)
                {
                    StopCoroutine(_localInvincibilityRoutine);
                    _localInvincibilityRoutine = null;
                }
                _localInvincible = false;
                ShowInvincibilityAura(false);
            }
        }


        private SpawnPointType GetSpawnTypeForPlayer()
        {
            var activeMode = NetworkedGameManager.Instance?.ActiveGameMode ?? GameModeBase.Instance;

            if (activeMode is TDMMode tdm)
            {
                int team = tdm.GetPlayerTeam(OwnerClientId);
                if (team == 0) return SpawnPointType.TeamBlue;
                if (team == 1) return SpawnPointType.TeamRed;
            }
            return SpawnPointType.FFA;
        }
    }
}
