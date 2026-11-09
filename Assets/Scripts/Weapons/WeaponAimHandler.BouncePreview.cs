using UnityEngine;
using System.Collections.Generic;

namespace Shootopia
{
    public partial class WeaponAimHandler
    {
        private static readonly RaycastHit[] s_HitsBuffer = new RaycastHit[16];

        public void UpdateBouncePreview(bool isOwner, WeaponAttachSO currentAttach, Transform muzzle, IAimProvider aim, bool isAiming = true)
        {
            if (!isOwner || !isAiming || currentAttach == null || currentAttach.gameplayData == null)
            {
                DisableBouncePreview();
                return;
            }

            bool isBounceGun = currentAttach.gameplayData.maxBounces > 0 || currentAttach.name.Contains("TrickShot");
            if (!isBounceGun)
            {
                DisableBouncePreview();
                return;
            }

            EnsureBounceLineRenderer();
            if (_bounceLineInstance == null) return;

            _bounceLineInstance.enabled = true;
            Transform muzzleTr = muzzle != null ? muzzle : _ownerTransform;
            Vector3 startPos = muzzleTr.position;

            Camera mainCam = Camera.main;
            Vector3 camForward = mainCam != null ? mainCam.transform.forward : (aim != null ? aim.CameraPlanarForward : _ownerTransform.forward);
            Vector3 aimPoint = (aim != null && aim.AimPoint != Vector3.zero) ? aim.AimPoint : (startPos + camForward * 100f);
            Vector3 aimDir = (aimPoint - startPos).normalized;
            if (aimDir.sqrMagnitude < 0.001f) aimDir = camForward;

            // Retain Layer 0 (Default) so all level geometry/colliders are hit; only ignore UI and raycast-ignore layers
            int layerMask = ~LayerMask.GetMask("Ignore Raycast", "UI");

            float previewDist = currentAttach.gameplayData.previewDistance > 1f ? currentAttach.gameplayData.previewDistance : 12f;
            int maxBounces = currentAttach.gameplayData.maxBounces > 0 ? currentAttach.gameplayData.maxBounces : 3;

            var points = new List<Vector3> { startPos };

            Vector3 curOrigin = startPos;
            Vector3 curDir = aimDir;
            float remainingDist = previewDist;

            for (int b = 0; b <= maxBounces; b++)
            {
                Ray ray = new Ray(curOrigin, curDir);
                int hitCount = Physics.RaycastNonAlloc(ray, s_HitsBuffer, remainingDist, layerMask, QueryTriggerInteraction.Ignore);

                bool hitFound = false;
                int bestIdx = -1;
                float bestDist = float.MaxValue;

                for (int i = 0; i < hitCount; i++)
                {
                    var h = s_HitsBuffer[i];
                    if (_ownerTransform != null && (h.collider.transform == _ownerTransform || h.collider.transform.IsChildOf(_ownerTransform)))
                    {
                        continue; // Do not hit own body
                    }

                    if (h.distance < bestDist)
                    {
                        bestDist = h.distance;
                        bestIdx = i;
                    }
                }

                if (bestIdx >= 0)
                {
                    var bestHit = s_HitsBuffer[bestIdx];
                    points.Add(bestHit.point);
                    remainingDist -= bestHit.distance;
                    curDir = Vector3.Reflect(curDir, bestHit.normal).normalized;
                    curOrigin = bestHit.point + bestHit.normal * 0.02f;
                    hitFound = true;
                }

                if (!hitFound || remainingDist <= 0.5f)
                {
                    if (!hitFound)
                    {
                        points.Add(curOrigin + curDir * remainingDist);
                    }
                    break;
                }
            }

            _bounceLineInstance.positionCount = points.Count;
            _bounceLineInstance.SetPositions(points.ToArray());

            // Animate scrolling dashes along the trajectory line
            _bounceLineOffset -= Time.deltaTime * 3.5f;
            if (_bounceLineInstance.material != null)
            {
                var offset = new Vector2(_bounceLineOffset, 0f);
                if (_bounceLineInstance.material.HasProperty("_BaseMap"))
                    _bounceLineInstance.material.SetTextureOffset("_BaseMap", offset);
                if (_bounceLineInstance.material.HasProperty("_MainTex"))
                    _bounceLineInstance.material.SetTextureOffset("_MainTex", offset);
            }
        }

        public void DisableBouncePreview()
        {
            if (_bounceLineInstance != null && _bounceLineInstance.enabled)
            {
                _bounceLineInstance.enabled = false;
            }
        }

        private void EnsureBounceLineRenderer()
        {
            if (_bounceLineInstance != null || _ownerTransform == null) return;

            GameObject lineObj = new GameObject("BouncePreviewLine");
            lineObj.transform.SetParent(_ownerTransform, false);
            _bounceLineInstance = lineObj.AddComponent<LineRenderer>();
            _bounceLineInstance.useWorldSpace = true;
            _bounceLineInstance.startWidth = 0.05f;
            _bounceLineInstance.endWidth = 0.05f;
            _bounceLineInstance.textureMode = LineTextureMode.Tile;

            Material dashedMat = Resources.Load<Material>("DashedLineMat");
            if (dashedMat != null)
            {
                _bounceLineInstance.material = Object.Instantiate(dashedMat);
            }
            else
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit") 
                             ?? Shader.Find("Sprites/Default");

                if (shader != null)
                {
                    Material mat = new Material(shader) { name = "BounceLaser_Mat" };
                    Texture2D dashTex = new Texture2D(32, 2, TextureFormat.RGBA32, false);
                    dashTex.wrapMode = TextureWrapMode.Repeat;
                    dashTex.filterMode = FilterMode.Point;
                    Color[] pixels = new Color[32 * 2];
                    for (int y = 0; y < 2; y++)
                    {
                        for (int x = 0; x < 32; x++)
                        {
                            pixels[y * 32 + x] = (x < 18) ? Color.white : Color.clear;
                        }
                    }
                    dashTex.SetPixels(pixels);
                    dashTex.Apply();

                    if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", dashTex);
                    if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", dashTex);
                    Color laserColor = new Color(0.15f, 1.0f, 0.45f, 1.0f);
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", laserColor);
                    if (mat.HasProperty("_Color")) mat.SetColor("_Color", laserColor);
                    mat.renderQueue = 3000;
                    _bounceLineInstance.material = mat;
                }
            }
        }

        public void Cleanup()
        {
            DisableBouncePreview();
            if (_bounceLineInstance != null)
            {
                Object.Destroy(_bounceLineInstance.gameObject);
                _bounceLineInstance = null;
            }
        }
    }
}

