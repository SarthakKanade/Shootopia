using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Shootopia
{
    public partial class LootBoxController
    {
        private List<Transform> GetLidBones()
        {
            var bones = new List<Transform>();
            if (chestMeshObject == null) return bones;

            foreach (var t in chestMeshObject.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "UP" || t.name == "Left" || t.name == "Right" || t.name == "Rotation" || t.name == "Down")
                {
                    bones.Add(t);
                }
            }
            return bones;
        }

        private void ResetLidBones()
        {
            if (chestMeshObject != null)
            {
                var anim = chestMeshObject.GetComponentInChildren<Animator>(true);
                if (anim != null)
                {
                    anim.speed = 0f;
                    anim.Play(0, 0, 0f);
                    anim.Update(0f);
                    return;
                }
            }

            var lidBones = GetLidBones();
            foreach (var b in lidBones)
            {
                if (b.name == "Down") b.localRotation = Quaternion.Euler(89.98f, 0f, 0f);
                else if (b.name == "Rotation") b.localRotation = Quaternion.Euler(90f, 270f, 0f);
                else if (b.name == "UP" && chestType == LootChestType.Chest3_AdvancedWeapons) b.localRotation = Quaternion.Euler(89.98f, 0f, 0f);
                else if (b.name == "UP" && chestType == LootChestType.Chest2_TrapsAndStarters) b.localRotation = Quaternion.Euler(359.12f, 180f, 180f);
                else if (b.name == "UP" || b.name == "Left" || b.name == "Right") b.localRotation = Quaternion.Euler(0.62f, 180f, 180f);
            }
        }

        public void TriggerDropAnimation(Vector3 targetGroundPos)
        {
            if (!gameObject.activeInHierarchy) return;

            if (targetGroundPos != Vector3.zero) _groundPosition = targetGroundPos;
            else if (_groundPosition == Vector3.zero) _groundPosition = transform.position;

            if (_dropRoutine != null) StopCoroutine(_dropRoutine);
            _dropRoutine = StartCoroutine(DropAnimationRoutine(_groundPosition));
        }

        private IEnumerator DropAnimationRoutine(Vector3 targetGroundPos)
        {
            _isDropping = true;
            Vector3 baseScale = new Vector3(0.75f, 0.75f, 0.75f);
            _groundPosition = targetGroundPos;
            Vector3 startPos = targetGroundPos + Vector3.up * dropHeight;
            transform.position = startPos;
            transform.localScale = baseScale;

            float elapsed = 0f;
            float duration = Mathf.Max(0.08f, dropDuration);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.position = Vector3.Lerp(startPos, targetGroundPos, t * t);
                yield return null;
            }

            transform.position = targetGroundPos;
            if (landingSound != null) AudioSource.PlayClipAtPoint(landingSound, targetGroundPos, 0.7f);

            // Squash & settle
            Vector3 squashScale = new Vector3(baseScale.x * 1.10f, baseScale.y * 0.75f, baseScale.z * 1.10f);
            Vector3 reboundScale = new Vector3(baseScale.x * 0.94f, baseScale.y * 1.08f, baseScale.z * 0.94f);

            yield return LerpScale(baseScale, squashScale, 0.05f);
            yield return LerpScale(squashScale, reboundScale, 0.07f);
            yield return LerpScale(reboundScale, baseScale, 0.05f);

            transform.position = targetGroundPos;
            transform.localScale = baseScale;
            _isDropping = false;
            _dropRoutine = null;
        }

        private IEnumerator LerpScale(Vector3 from, Vector3 to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
        }
    }
}

