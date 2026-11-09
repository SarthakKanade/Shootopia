using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Shootopia
{
    /// <summary>
    /// Animates celestial background elements: periodic shooting stars gliding across the screen,
    /// and subtle floating/breathing motion on cosmic exoplanets.
    /// </summary>
    public class CosmicBackgroundFx : MonoBehaviour
    {
        [Header("Planets")]
        [SerializeField] private RectTransform planet1;
        [SerializeField] private RectTransform planet2;
        [SerializeField] private float planetBobSpeed = 0.5f;
        [SerializeField] private float planetBobAmount = 8f;

        [Header("Shooting Stars")]
        [SerializeField] private RectTransform starContainer;
        [SerializeField] private Sprite[] starSprites;
        [SerializeField] private int maxConcurrentStars = 2;
        [SerializeField] private float minInterval = 2.5f;
        [SerializeField] private float maxInterval = 6.0f;
        [Tooltip("Trajectory angle for falling stars in degrees (default -30 = gliding downward-right)")]
        [SerializeField] private float fallAngle = -30f;
        [Tooltip("Subtle angle variation across spawns for organic celestial motion")]
        [SerializeField] private float fallAngleVariance = 3f;

        private Vector2 _planet1Origin;
        private Vector2 _planet2Origin;
        private readonly List<GameObject> _starPool = new List<GameObject>();

        private void Awake()
        {
            // Isolate background animations into their own sub-canvas to prevent dirtying the main menu canvas
            if (GetComponent<Canvas>() == null) gameObject.AddComponent<Canvas>();
            if (planet1 != null) _planet1Origin = planet1.anchoredPosition;
            if (planet2 != null) _planet2Origin = planet2.anchoredPosition;
        }

        private void Start()
        {
            StartCoroutine(ShootingStarLoop());
        }

        private void Update()
        {
            float t = Time.time * planetBobSpeed;
            if (planet1 != null)
            {
                planet1.anchoredPosition = _planet1Origin + new Vector2(Mathf.Sin(t * 0.7f) * 4f, Mathf.Cos(t) * planetBobAmount);
            }
            if (planet2 != null)
            {
                planet2.anchoredPosition = _planet2Origin + new Vector2(Mathf.Cos(t * 0.6f) * 3f, Mathf.Sin(t * 1.2f) * (planetBobAmount * 0.7f));
            }
        }

        private IEnumerator ShootingStarLoop()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(Random.Range(minInterval, maxInterval));
                if (starContainer != null && starSprites != null && starSprites.Length > 0)
                {
                    StartCoroutine(AnimateSingleShootingStar());
                }
            }
        }

        private IEnumerator AnimateSingleShootingStar()
        {
            var starObj = GetStarInstance();
            if (starObj == null) yield break;

            starObj.SetActive(true);
            var rect = starObj.GetComponent<RectTransform>();
            var img = starObj.GetComponent<Image>();
            var cg = starObj.GetComponent<CanvasGroup>();
            if (cg == null) cg = starObj.AddComponent<CanvasGroup>();

            Sprite chosenSprite = null;
            if (starSprites != null && starSprites.Length > 0 && img != null)
            {
                chosenSprite = starSprites[Random.Range(0, starSprites.Length)];
                img.sprite = chosenSprite;
                if (chosenSprite != null)
                {
                    rect.sizeDelta = chosenSprite.rect.size;
                }
            }

            // Screen bounds setup (Canvas 1920x1080)
            float startX = Random.Range(-700f, 400f);
            float startY = Random.Range(300f, 600f);

            // Compute falling trajectory angle (e.g. ~ -30 deg downward-right)
            float currentFallAngle = fallAngle + Random.Range(-fallAngleVariance * 0.5f, fallAngleVariance * 0.5f);

            // Compensate for the intrinsic angle of the sprite texture so its visual streak
            // aligns precisely with the direction of falling movement
            float intrinsicAngle = GetSpriteIntrinsicAngle(chosenSprite);
            float rotationZ = currentFallAngle - intrinsicAngle;

            rect.anchoredPosition = new Vector2(startX, startY);
            rect.localEulerAngles = new Vector3(0f, 0f, rotationZ);
            float scale = Random.Range(0.6f, 0.95f);
            rect.localScale = new Vector3(scale, scale, 1f);

            float travelDistance = Random.Range(800f, 1200f);
            float duration = Random.Range(0.75f, 1.15f);

            Vector2 dir = new Vector2(Mathf.Cos(currentFallAngle * Mathf.Deg2Rad), Mathf.Sin(currentFallAngle * Mathf.Deg2Rad)).normalized;
            Vector2 endPos = new Vector2(startX, startY) + dir * travelDistance;

            cg.alpha = 0f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);

                // Smooth position
                rect.anchoredPosition = Vector2.Lerp(new Vector2(startX, startY), endPos, progress);

                // Fade in then fade out
                if (progress < 0.25f)
                {
                    cg.alpha = progress / 0.25f;
                }
                else if (progress > 0.6f)
                {
                    cg.alpha = 1f - ((progress - 0.6f) / 0.4f);
                }
                else
                {
                    cg.alpha = 1f;
                }

                yield return null;
            }

            cg.alpha = 0f;
            starObj.SetActive(false);
        }

        /// <summary>
        /// Returns the intrinsic angle of the streak in the unrotated sprite texture (degrees from +X).
        /// Measured via principal axis analysis of the asset pack sprites.
        /// </summary>
        private float GetSpriteIntrinsicAngle(Sprite sprite)
        {
            if (sprite == null) return -25f;
            string sName = sprite.name.ToLower();
            if (sName.Contains("short"))
                return -33.05f;
            if (sName.Contains("blue"))
                return 32.40f;
            return -14.70f;
        }

        private GameObject GetStarInstance()
        {
            int activeCount = 0;
            foreach (var s in _starPool)
            {
                if (s != null && s.activeSelf) activeCount++;
            }
            if (activeCount >= maxConcurrentStars) return null;

            foreach (var s in _starPool)
            {
                if (s != null && !s.activeSelf) return s;
            }

            if (_starPool.Count < maxConcurrentStars * 2 && starContainer != null)
            {
                var newStar = new GameObject("ShootingStar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
                newStar.transform.SetParent(starContainer, false);
                var img = newStar.GetComponent<Image>();
                img.raycastTarget = false;
                img.preserveAspect = true;
                newStar.SetActive(false);
                _starPool.Add(newStar);
                return newStar;
            }

            return null;
        }
    }
}
