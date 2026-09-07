using System.Collections.Generic;
using UnityEngine;

namespace Shootopia
{
    /// <summary>
    /// Manages high-performance 360-degree off-screen threat direction indicator arcs.
    /// Listens to damage events from PlayerHealth, calculates azimuthal angle relative
    /// to the player camera, and dynamically tracks enemy position as the camera rotates.
    /// </summary>
    public class DamageIndicatorManager : MonoBehaviour
    {
        public static DamageIndicatorManager Instance { get; private set; }

        [Header("Pool & Timing")]
        [SerializeField] private int poolSize = 8;
        [SerializeField] private float displayDuration = 1.8f;
        [SerializeField] private float arcRadius = 145f;
        [SerializeField] private float arcThickness = 16f;

        private class IndicatorSlot
        {
            public GameObject root;
            public RectTransform rectTransform;
            public CanvasGroup canvasGroup;
            public DamageIndicatorArcGraphic graphic;
            public Vector3 attackerWorldPos;
            public float remainingTime;
            public bool isActive;
        }

        private readonly List<IndicatorSlot> _slots = new List<IndicatorSlot>();
        private Camera _cachedMainCamera;

        private void Awake()
        {
            Instance = this;
            InitializePool();
        }

        private void OnEnable()
        {
            PlayerHealth.OnLocalPlayerDamagedWithPosition += HandleDamageTaken;
        }

        private void OnDisable()
        {
            PlayerHealth.OnLocalPlayerDamagedWithPosition -= HandleDamageTaken;
        }

        private void InitializePool()
        {
            _slots.Clear();

            for (int i = 0; i < poolSize; i++)
            {
                var itemGo = new GameObject($"DamageArc_{i}", typeof(RectTransform), typeof(CanvasGroup), typeof(DamageIndicatorArcGraphic));
                itemGo.transform.SetParent(transform, false);

                var rt = itemGo.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(arcRadius * 2.5f, arcRadius * 2.5f);

                var cg = itemGo.GetComponent<CanvasGroup>();
                cg.alpha = 0f;
                cg.interactable = false;
                cg.blocksRaycasts = false;

                var graphic = itemGo.GetComponent<DamageIndicatorArcGraphic>();
                graphic.Radius = arcRadius;
                graphic.Thickness = arcThickness;
                graphic.raycastTarget = false;

                itemGo.SetActive(false);

                _slots.Add(new IndicatorSlot
                {
                    root = itemGo,
                    rectTransform = rt,
                    canvasGroup = cg,
                    graphic = graphic,
                    remainingTime = 0f,
                    isActive = false
                });
            }
        }

        /// <summary>
        /// Displays or refreshes a damage indicator pointing toward attackerWorldPos.
        /// </summary>
        public void ShowIndicator(Vector3 attackerWorldPos, float damageAmount)
        {
            if (_slots.Count == 0) return;

            Camera cam = GetMainCamera();
            if (cam == null) return;

            // Calculate angle to see if an active indicator is already pointing roughly in this direction
            float newAngle = CalculateAngle(cam, attackerWorldPos);

            IndicatorSlot targetSlot = null;

            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (!slot.isActive) continue;

                float existingAngle = -slot.rectTransform.localEulerAngles.z;
                if (Mathf.Abs(Mathf.DeltaAngle(existingAngle, newAngle)) < 18f)
                {
                    targetSlot = slot;
                    break;
                }
            }

            // If no nearby active slot found, pick first available inactive slot (or oldest)
            if (targetSlot == null)
            {
                for (int i = 0; i < _slots.Count; i++)
                {
                    if (!_slots[i].isActive)
                    {
                        targetSlot = _slots[i];
                        break;
                    }
                }

                // If all slots are active, take the one with least remaining time
                if (targetSlot == null)
                {
                    float minTime = float.MaxValue;
                    for (int i = 0; i < _slots.Count; i++)
                    {
                        if (_slots[i].remainingTime < minTime)
                        {
                            minTime = _slots[i].remainingTime;
                            targetSlot = _slots[i];
                        }
                    }
                }
            }

            if (targetSlot == null) return;

            targetSlot.attackerWorldPos = attackerWorldPos;
            targetSlot.remainingTime = displayDuration;
            targetSlot.isActive = true;
            targetSlot.root.SetActive(true);
            targetSlot.canvasGroup.alpha = 1f;

            UpdateSlotRotation(targetSlot, cam);
        }

        private void HandleDamageTaken(Vector3 attackerPos, float damageAmount)
        {
            ShowIndicator(attackerPos, damageAmount);
        }

        private void Update()
        {
            Camera cam = GetMainCamera();
            if (cam == null) return;

            float dt = Time.deltaTime;

            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (!slot.isActive) continue;

                slot.remainingTime -= dt;

                if (slot.remainingTime <= 0f)
                {
                    slot.isActive = false;
                    slot.canvasGroup.alpha = 0f;
                    slot.root.SetActive(false);
                    continue;
                }

                // Smooth non-linear fade
                float progress = slot.remainingTime / displayDuration;
                slot.canvasGroup.alpha = Mathf.Pow(Mathf.Clamp01(progress), 1.25f);

                UpdateSlotRotation(slot, cam);
            }
        }

        private void UpdateSlotRotation(IndicatorSlot slot, Camera cam)
        {
            float angle = CalculateAngle(cam, slot.attackerWorldPos);
            slot.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -angle);
        }

        private float CalculateAngle(Camera cam, Vector3 targetWorldPos)
        {
            Vector3 toTarget = targetWorldPos - cam.transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude < 0.001f) return 0f;

            Vector3 localDir = cam.transform.InverseTransformDirection(toTarget);
            return Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
        }

        private Camera GetMainCamera()
        {
            if (_cachedMainCamera == null || !_cachedMainCamera.gameObject.activeInHierarchy)
            {
                _cachedMainCamera = Camera.main;
            }
            return _cachedMainCamera;
        }
    }
}
