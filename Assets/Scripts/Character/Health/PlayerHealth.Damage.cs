using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace Shootopia
{
    /// <summary>
    /// Partial class managing damage handling, RPCs, and healing for PlayerHealth.
    /// </summary>
    public partial class PlayerHealth
    {
        public void TakeDamage(float amount, DamageType type = DamageType.Projectile, WeaponSO weaponUsed = null)
        {
            TakeDamage(amount, type, weaponUsed, ulong.MaxValue);
        }

        public void TakeDamage(float amount, DamageType type, WeaponSO weaponUsed, ulong attackerClientId)
        {
            bool isLethalAbyss = (type == DamageType.Environment && amount >= 9000f);
            if (_isDead || (IsInvincible && !isLethalAbyss)) return;

            string weaponName = weaponUsed != null ? (!string.IsNullOrEmpty(weaponUsed.weaponName) ? weaponUsed.weaponName : weaponUsed.name)
                              : (type == DamageType.Melee ? "Melee" : (type == DamageType.Environment ? "Lava" : "Unknown"));

            ulong resolvedAttacker = (attackerClientId != ulong.MaxValue) 
                ? attackerClientId 
                : (LastAttackerClientId != ulong.MaxValue && LastAttackerClientId != OwnerClientId && NetworkManager.Singleton != null && NetworkManager.Singleton.ConnectedClients.ContainsKey(LastAttackerClientId) ? LastAttackerClientId : OwnerClientId);

            if (IsSpawned)
            {
                TakeDamageServerRpc(amount, type, weaponName, resolvedAttacker);
            }
            else
            {
                float damageToApply = amount;
                var shield = GetComponent<PlayerShield>();
                if (shield != null && shield.IsShieldActive)
                {
                    damageToApply = shield.AbsorbDamage(damageToApply);
                    if (damageToApply <= 0f) return;
                }

                float previousHp = _currentHp.Value;
                _currentHp.Value = Mathf.Max(_currentHp.Value - damageToApply, 0f);
                float actual = previousHp - _currentHp.Value;
                Vector3 fallbackPos = transform.position - transform.forward * 10f;
                OnLocalPlayerDamagedWithPosition?.Invoke(fallbackPos, actual);
                HUDController.Instance?.TriggerDamageFlash(actual);
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void TakeDamageServerRpc(float amount, DamageType type, string weaponName, ulong attackerClientId)
        {
            bool isLethalAbyss = (type == DamageType.Environment && amount >= 9000f);
            if (!IsServer || _isDead || (_isInvincibleNet.Value && !isLethalAbyss) || _currentHp.Value <= 0f) return;

            if (SceneManager.GetActiveScene().name == "WaitingRoom") return;
            
            var activeMode = NetworkedGameManager.Instance != null && NetworkedGameManager.Instance.ActiveGameMode != null 
                ? NetworkedGameManager.Instance.ActiveGameMode 
                : GameModeBase.Instance;

            bool isSelfOrEnvironment = (type == DamageType.Environment || attackerClientId == OwnerClientId);
            bool isMatchActive = (activeMode != null && activeMode.MatchActive.Value) || 
                                 (NetworkedGameManager.Instance != null && NetworkedGameManager.Instance.MatchStarted.Value);
            if (!isSelfOrEnvironment && !isMatchActive) return;

            if (activeMode is TDMMode tdm)
            {
                int victimTeam = tdm.GetPlayerTeam(OwnerClientId);
                int attackerTeam = tdm.GetPlayerTeam(attackerClientId);
                if (attackerClientId != OwnerClientId && victimTeam != -1 && attackerTeam != -1 && victimTeam == attackerTeam)
                {
                    return;
                }
            }

            float damageToApply = amount;
            var shield = GetComponent<PlayerShield>();
            if (shield != null && shield.IsShieldActive)
            {
                damageToApply = shield.AbsorbDamage(damageToApply);
                if (damageToApply <= 0f) return;
            }

            float previousHp = _currentHp.Value;
            _currentHp.Value = Mathf.Max(_currentHp.Value - damageToApply, 0f);
            float actualDamage = previousHp - _currentHp.Value;

            if (actualDamage > 0f)
            {
                bool isKill = _currentHp.Value <= 0f;

                if (attackerClientId != OwnerClientId && NetworkManager.Singleton.ConnectedClients.ContainsKey(attackerClientId))
                {
                    var clientParams = new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams { TargetClientIds = new[] { attackerClientId } }
                    };
                    NotifyHitConfirmedClientRpc(isKill, clientParams);
                }

                Vector3 attackerPos = transform.position + transform.forward * 8f;
                if (NetworkManager.Singleton != null && 
                    NetworkManager.Singleton.ConnectedClients.TryGetValue(attackerClientId, out var client) && 
                    client.PlayerObject != null)
                {
                    attackerPos = client.PlayerObject.transform.position;
                }

                var victimParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } }
                };
                NotifyDamageTakenClientRpc(attackerPos, actualDamage, victimParams);

                if (isKill)
                {
                    _isDead = true;
                    HandleDeath(attackerClientId, type, weaponName);
                }
            }
        }

        private void HandleDeath(ulong killerClientId, DamageType type, string weaponName)
        {
            if (!IsServer) return;

            _isDead = true;
            _deathCount.Value++;
            _livesRemaining.Value = Mathf.Max(_livesRemaining.Value - 1, 0);

            ulong resolvedKiller = killerClientId;
            if (resolvedKiller == ulong.MaxValue || resolvedKiller == OwnerClientId)
            {
                if (LastAttackerClientId != ulong.MaxValue && LastAttackerClientId != OwnerClientId &&
                    NetworkManager.Singleton != null && NetworkManager.Singleton.ConnectedClients.ContainsKey(LastAttackerClientId))
                {
                    resolvedKiller = LastAttackerClientId;
                }
                else
                {
                    resolvedKiller = OwnerClientId;
                }
            }

            ulong reportedKiller = resolvedKiller;
            string reportedWeapon = (type == DamageType.Environment && (string.IsNullOrEmpty(weaponName) || weaponName == "Unknown")) ? "Lava" : weaponName;
            NetworkedGameManager.ReportKill(reportedKiller, OwnerClientId, reportedWeapon);

            Vector3 spawnPos = new Vector3(120f, 16f, 2.5f);
            if (SpawnPointManager.Instance != null)
            {
                Transform sp = SpawnPointManager.Instance.GetSpawnPoint(GetSpawnTypeForPlayer());
                if (sp != null) spawnPos = sp.position;
            }

            DeathClientRpc(type, spawnPos);
            StartCoroutine(RespawnSequence(spawnPos));
        }

        [ClientRpc]
        private void NotifyHitConfirmedClientRpc(bool isKill, ClientRpcParams clientRpcParams = default)
        {
            OnLocalPlayerHitConfirmed?.Invoke(isKill);

            if (AudioManager.Instance != null && playerAudioConfig != null && playerAudioConfig.hitMarkerClip != null)
            {
                AudioManager.Instance.PlaySFX2D(playerAudioConfig.hitMarkerClip);
            }
        }

        [ClientRpc]
        private void NotifyDamageTakenClientRpc(Vector3 attackerWorldPos, float damageAmount, ClientRpcParams clientRpcParams = default)
        {
            if (!IsOwner) return;

            OnLocalPlayerDamagedWithPosition?.Invoke(attackerWorldPos, damageAmount);
            HUDController.Instance?.TriggerDamageFlash(damageAmount);
            CameraShakeManager.Instance?.ShakeDamage(damageAmount);
            MobileHapticsManager.TriggerDamageTaken();
        }

        [ClientRpc]
        private void DeathClientRpc(DamageType type, Vector3 spawnPos)
        {
            _isDead = true;
            _localInvincible = false;
            ShowInvincibilityAura(false);
            onPlayerDied?.Raise();

            var nameplate = GetComponentInChildren<PlayerNameplate>(true);
            if (nameplate != null) nameplate.SetDeathHidden(true);

            if (IsOwner)
            {
                OnLocalPlayerDied?.Invoke(3.0f);
                if (AudioManager.Instance != null)
                {
                    AudioClip deathClip = (playerAudioConfig != null && playerAudioConfig.deathClip != null) ? playerAudioConfig.deathClip : deathSound;
                    if (deathClip != null) AudioManager.Instance.PlaySFX2D(deathClip, 1f, isCombatPriority: true);
                }
            }

            var anim = GetComponentInChildren<Animator>(true);
            if (anim != null)
            {
                if (anim.layerCount > 1) anim.SetLayerWeight(1, 0f);
                anim.SetTrigger("Die");
            }

            SetPlayerControlsEnabled(false);
            SetCollidersEnabled(false);

            var weaponCtrl = GetComponent<WeaponController>();
            if (weaponCtrl != null) weaponCtrl.OnPlayerDied();

            if (_hideModelRoutine != null) StopCoroutine(_hideModelRoutine);
            _hideModelRoutine = StartCoroutine(HideModelAndTeleportRoutine(spawnPos));
        }

        public void Heal(float amount) => HealServerRpc(amount);

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void HealServerRpc(float amount)
        {
            if (!IsServer || _currentHp.Value <= 0f) return;
            _currentHp.Value = Mathf.Min(_currentHp.Value + amount, maxHp);
        }

        public float GetCurrentHp() => _currentHp.Value;
        public float GetMaxHp() => maxHp;
    }
}
