using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Shootopia
{
    public partial class LootBoxController
    {
        private IEnumerator OpenChestAndGrantLootRoutine(GameObject playerObj)
        {
            _isOpening = true;

            if (IsSpawned) TriggerLootEffectsClientRpc(transform.position);
            else TriggerLocalLootEffects(transform.position);

            if (floatingPreview != null) floatingPreview.ClearPreview();

            yield return new WaitForSeconds(0.6f);
            ExecuteLootForPlayer(playerObj);
            yield return new WaitForSeconds(0.2f);

            _isOpening = false;

            if (IsSpawned && IsServer)
            {
                _isAvailable.Value = false;
                StartCoroutine(CooldownRoutine());
            }
            else if (!IsSpawned)
            {
                StartCoroutine(OfflineCooldownRoutine());
            }
        }

        private void ExecuteLootForPlayer(GameObject playerObj)
        {
            if (playerObj == null) return;

            var health = playerObj.GetComponent<PlayerHealth>();
            var shield = playerObj.GetComponent<PlayerShield>();
            var weaponCtrl = playerObj.GetComponent<WeaponController>();
            if (health == null) return;

            int activeIndex = GetCurrentActiveIndex();
            if (lootTable == null || lootTable.Entries.Count == 0) return;
            if (activeIndex < 0 || activeIndex >= lootTable.Entries.Count) activeIndex = 0;
            var entry = lootTable.Entries[activeIndex];
            if (entry == null) return;

            if (chestType == LootChestType.Chest1_Defense)
            {
                var invis = playerObj.GetComponent<PlayerInvisibility>() ?? playerObj.GetComponentInParent<PlayerInvisibility>();
                LootItemType resolvedType = entry.itemType;
                if (resolvedType == LootItemType.Weapon)
                {
                    string itemName = (entry.displayName ?? entry.weaponAttach?.name ?? "").ToLower();
                    if (itemName.Contains("invis") || itemName.Contains("cloak")) resolvedType = LootItemType.Invisibility;
                    else if (itemName.Contains("shield")) resolvedType = LootItemType.Shield;
                    else resolvedType = LootItemType.FullHeal;
                }

                switch (resolvedType)
                {
                    case LootItemType.Invisibility:
                        if (invis != null) invis.ActivateInvisibility(10f);
                        break;
                    case LootItemType.Shield:
                        if (shield != null) shield.GrantShield(50f);
                        break;
                    default:
                        health.Heal(health.GetMaxHp());
                        break;
                }
            }
            else if (weaponCtrl != null && entry.weaponAttach != null)
            {
                WeaponController.RegisterAttach(entry.weaponAttach);
                weaponCtrl.ReplaceCurrentWeapon(entry.weaponAttach);
            }
        }

        public float GetCurrentCooldown()
        {
            return LootBoxSpawnManager.Instance != null ? LootBoxSpawnManager.Instance.GetChestCooldown() : cooldownSeconds;
        }

        private IEnumerator CooldownRoutine()
        {
            yield return new WaitForSeconds(GetCurrentCooldown());
            if (IsServer)
            {
                RelocateToRandomSpawnPointServer();
                RollNewItemServer();
                _isAvailable.Value = true;
            }
        }

        private IEnumerator OfflineCooldownRoutine()
        {
            yield return new WaitForSeconds(GetCurrentCooldown());
            if (lootTable != null && lootTable.Entries.Count > 0) _localOfflineIndex = lootTable.GetRandomEntryIndex();
            UpdateVisualState();
            TriggerDropAnimation(_groundPosition);
        }

        private void RelocateToRandomSpawnPointServer()
        {
            Vector3 newPos = transform.position;
            if (LootBoxSpawnManager.Instance != null)
            {
                newPos = LootBoxSpawnManager.Instance.GetRandomUnoccupiedSpawnPosition(transform.position, this);
            }
            else
            {
                var spawns = GameObject.FindGameObjectsWithTag("LootSpawnPoint");
                if (spawns != null && spawns.Length > 0) newPos = spawns[Random.Range(0, spawns.Length)].transform.position;
            }

            if (newPos != Vector3.zero)
            {
                _groundPosition = newPos;
                _netPosition.Value = newPos;
                transform.position = newPos;
            }
        }

        private void RollNewItemServer()
        {
            _currentEntryIndex.Value = (lootTable != null && lootTable.Entries.Count > 0) ? Mathf.Max(0, lootTable.GetRandomEntryIndex()) : 0;
        }

        private int GetCurrentActiveIndex()
        {
            return (IsSpawned && _currentEntryIndex.Value >= 0) ? _currentEntryIndex.Value : _localOfflineIndex;
        }

        private void OnAvailabilityChanged(bool previousValue, bool newValue)
        {
            UpdateVisualState();
            if (!previousValue && newValue)
            {
                Vector3 targetPos = (_netPosition != null && _netPosition.Value != Vector3.zero) 
                    ? _netPosition.Value 
                    : (_groundPosition != Vector3.zero ? _groundPosition : transform.position);
                TriggerDropAnimation(targetPos);
            }
        }

        private void OnItemIndexChanged(int previousValue, int newValue) => UpdateVisualState();

        private void UpdateVisualState()
        {
            bool available = IsSpawned ? _isAvailable.Value : true;
            if (chestMeshObject != null && chestMeshObject != gameObject) chestMeshObject.SetActive(available);

            if (!available)
            {
                if (floatingPreview != null) floatingPreview.ClearPreview();
                return;
            }

            ResetLidBones();
            if (floatingPreview == null) return;

            GameObject previewPrefab = null;
            if (lootTable?.Entries != null && lootTable.Entries.Count > 0)
            {
                int idx = Mathf.Clamp(GetCurrentActiveIndex(), 0, lootTable.Entries.Count - 1);
                var entry = lootTable.Entries[idx];
                if (entry != null)
                {
                    previewPrefab = entry.customPreviewPrefab != null ? entry.customPreviewPrefab : (entry.weaponAttach != null ? entry.weaponAttach.visualPrefab : null);
                }
            }
            else if (chestType == LootChestType.Chest1_Defense)
            {
                previewPrefab = healthBoxAttach?.visualPrefab ?? shieldAttach?.visualPrefab;
            }

            if (previewPrefab != null) floatingPreview.SetPreviewVisual(previewPrefab);
            else floatingPreview.ClearPreview();
        }

        [Rpc(SendTo.Everyone)]
        private void TriggerLootEffectsClientRpc(Vector3 lootPosition) => TriggerLocalLootEffects(lootPosition);

        private void TriggerLocalLootEffects(Vector3 lootPosition)
        {
            if (chestMeshObject != null)
            {
                var anim = chestMeshObject.GetComponentInChildren<Animator>(true);
                if (anim != null) { anim.speed = 1.5f; anim.Play(0, 0, 0f); }
            }

            if (floatingPreview != null) floatingPreview.ClearPreview();
            if (lootSound != null) AudioSource.PlayClipAtPoint(lootSound, lootPosition, 1f);
            if (lootVFXPrefab != null)
            {
                var vfx = Instantiate(lootVFXPrefab, transform.position + Vector3.up * 1f, Quaternion.identity);
                Destroy(vfx, 3f);
            }
        }
    }
}

