using UnityEngine;
using UnityEngine.UI;

namespace Shootopia
{
    /// <summary>
    /// Procedural UI Graphic generating a smooth, vector-sharp curved indicator arc.
    /// Used by DamageIndicatorManager to visually point in the direction of incoming enemy fire.
    /// Operates with zero texture dependencies and zero texture-filtering blur at any resolution.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class DamageIndicatorArcGraphic : MaskableGraphic
    {
        [Header("Arc Geometry")]
        [Tooltip("Distance in canvas pixels from reticle center to the arc centerline.")]
        [SerializeField] private float radius = 145f;

        [Tooltip("Thickness in canvas pixels of the arc band at its apex.")]
        [SerializeField] private float thickness = 16f;

        [Tooltip("Angular spread of the arc in degrees.")]
        [Range(10f, 90f)]
        [SerializeField] private float arcAngle = 44f;

        [Tooltip("Number of mesh segments defining curve smoothness.")]
        [Range(6, 32)]
        [SerializeField] private int segments = 18;

        [Header("Arc Colors")]
        [SerializeField] private Color outerColor = new Color(1.0f, 0.22f, 0.12f, 0.95f);
        [SerializeField] private Color innerColor = new Color(0.95f, 0.10f, 0.05f, 0.25f);
        [SerializeField] private Color tipColor = new Color(1.0f, 0.22f, 0.12f, 0.0f);

        public float Radius
        {
            get => radius;
            set { radius = value; SetVerticesDirty(); }
        }

        public float Thickness
        {
            get => thickness;
            set { thickness = value; SetVerticesDirty(); }
        }

        public float ArcAngle
        {
            get => arcAngle;
            set { arcAngle = Mathf.Clamp(value, 10f, 90f); SetVerticesDirty(); }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            int segs = Mathf.Max(segments, 4);
            float halfAngle = arcAngle * 0.5f;

            for (int i = 0; i <= segs; i++)
            {
                float t = (float)i / segs; // 0.0 to 1.0
                float angleDeg = Mathf.Lerp(-halfAngle, halfAngle, t);
                float angleRad = angleDeg * Mathf.Deg2Rad;

                // 0 degrees points straight UP (+Y), positive angle moves clockwise (+X)
                Vector2 dir = new Vector2(Mathf.Sin(angleRad), Mathf.Cos(angleRad));

                // Smooth taper: full width in middle, tapers to zero at ends
                float taper = Mathf.Sin(t * Mathf.PI);
                float currentHalfThick = (thickness * 0.5f) * Mathf.Lerp(0.15f, 1.0f, taper);

                Vector2 innerPos = dir * (radius - currentHalfThick);
                Vector2 outerPos = dir * (radius + currentHalfThick);

                // Multiply graphic color (supports CanvasGroup alpha fading)
                Color cInner = Color.Lerp(tipColor, innerColor, taper) * color;
                Color cOuter = Color.Lerp(tipColor, outerColor, taper) * color;

                vh.AddVert(innerPos, cInner, Vector2.zero);
                vh.AddVert(outerPos, cOuter, Vector2.zero);

                if (i > 0)
                {
                    int baseIdx = (i - 1) * 2;
                    // Triangle 1: innerPrev, outerPrev, innerCurr
                    vh.AddTriangle(baseIdx, baseIdx + 1, baseIdx + 2);
                    // Triangle 2: innerCurr, outerPrev, outerCurr
                    vh.AddTriangle(baseIdx + 2, baseIdx + 1, baseIdx + 3);
                }
            }
        }
    }
}
