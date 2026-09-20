using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace Shootopia
{
    /// <summary>
    /// Editor utility to set up the CrosshairUI on the gameplay HUD canvas.
    /// Run from the menu: Shootopia > Setup Crosshair UI
    /// </summary>
    public static class CrosshairSetup
    {
        [MenuItem("Shootopia/Setup Crosshair UI")]
        public static void SetupCrosshair()
        {
            // Find the HUD canvas in the scene (the one with HUDController)
            var hudController = Object.FindAnyObjectByType<HUDController>();
            if (hudController == null)
            {
                Debug.LogError("[CrosshairSetup] Could not find HUDController in the scene. Make sure a gameplay scene is loaded.");
                return;
            }

            Canvas hudCanvas = hudController.GetComponentInParent<Canvas>();
            if (hudCanvas == null)
            {
                hudCanvas = hudController.GetComponent<Canvas>();
            }
            if (hudCanvas == null)
            {
                Debug.LogError("[CrosshairSetup] Could not find Canvas on or above HUDController.");
                return;
            }

            // Check if CrosshairUI already exists
            var existingCrosshair = hudCanvas.GetComponentInChildren<CrosshairUI>(true);
            if (existingCrosshair != null)
            {
                Debug.LogWarning("[CrosshairSetup] CrosshairUI already exists on this canvas. Skipping.");
                Selection.activeGameObject = existingCrosshair.gameObject;
                return;
            }

            // Create CrosshairRoot GameObject
            var crosshairRoot = new GameObject("CrosshairUI");
            crosshairRoot.transform.SetParent(hudCanvas.transform, false);

            // RectTransform: centered, anchored to center
            var rootRect = crosshairRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.sizeDelta = new Vector2(24f, 24f);

            // Image component
            var img = crosshairRoot.AddComponent<Image>();
            img.raycastTarget = false;
            img.color = new Color(1f, 1f, 1f, 0.85f);

            // Load the crosshair sprite
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/Crosshair.png");
            if (sprite != null)
            {
                img.sprite = sprite;
            }
            else
            {
                Debug.LogWarning("[CrosshairSetup] Could not load Crosshair.png sprite. Assign it manually.");
            }

            // Add CrosshairUI component
            var crosshairUI = crosshairRoot.AddComponent<CrosshairUI>();

            // Use SerializedObject to set the private serialized field
            var so = new SerializedObject(crosshairUI);
            var crosshairImageProp = so.FindProperty("crosshairImage");
            if (crosshairImageProp != null)
            {
                crosshairImageProp.objectReferenceValue = img;
                so.ApplyModifiedProperties();
            }

            // Ensure the crosshair is at the top of the sibling order so it renders on top
            crosshairRoot.transform.SetAsLastSibling();

            // Mark the scene dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );

            Selection.activeGameObject = crosshairRoot;
            Debug.Log("[CrosshairSetup] CrosshairUI successfully created on the HUD canvas. Don't forget to save the scene!");
        }
    }
}
