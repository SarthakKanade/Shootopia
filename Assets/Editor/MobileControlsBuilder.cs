using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace Shootopia.EditorTools
{
    public static class MobileControlsBuilder
    {
        [MenuItem("Shootopia/Build Mobile Controls Prefab")]
        public static void BuildPrefab()
        {
            // Ensure directory exists
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI"))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
            }

            // Load Sprites
            var bevelBaseSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/3rdParty/StarterAssets/Mobile/UI/UI_Circle_Bevel_Base.png");
            var circleFadedSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/3rdParty/StarterAssets/Mobile/UI/UI_Circle_Faded.png");
            var moveIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/3rdParty/Space_Exploration_GUI_Kit/Picto_Icons/White/expand-128.png");
            var jumpIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/3rdParty/Space_Exploration_GUI_Kit/Picto_Icons/White/up-arrow-128.png");
            var sprintIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/3rdParty/Space_Exploration_GUI_Kit/Picto_Icons/White/fast-forward-128.png");
            var lookIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/3rdParty/Space_Exploration_GUI_Kit/Picto_Icons/White/zoom-in-128.png");
            var gunIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/3rdParty/Space_Exploration_GUI_Kit/Picto_Icons/White/gun-128.png");
            var pauseIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/3rdParty/Space_Exploration_GUI_Kit/Picto_Icons/White/pause-128.png");
            var trophyIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/3rdParty/Space_Exploration_GUI_Kit/Picto_Icons/White/trophy-128.png");

            // Root Canvas
            var rootObj = new GameObject("MobileControlsCanvas");
            var canvas = rootObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            var scaler = rootObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            rootObj.AddComponent<GraphicRaycaster>();
            var canvasGroup = rootObj.AddComponent<CanvasGroup>();
            var masterCtrl = rootObj.AddComponent<MobileControlsCanvas>();

            // SafeArea Container
            var safeAreaObj = new GameObject("SafeAreaContainer");
            safeAreaObj.transform.SetParent(rootObj.transform, false);
            var safeRect = safeAreaObj.AddComponent<RectTransform>();
            safeRect.anchorMin = Vector2.zero;
            safeRect.anchorMax = Vector2.one;
            safeRect.offsetMin = Vector2.zero;
            safeRect.offsetMax = Vector2.zero;
            safeAreaObj.AddComponent<SafeAreaFitter>();

            // 1. Touch Look Zone (Right 65% of screen)
            var lookObj = new GameObject("TouchLookZone");
            lookObj.transform.SetParent(safeAreaObj.transform, false);
            var lookRect = lookObj.AddComponent<RectTransform>();
            lookRect.anchorMin = new Vector2(0.35f, 0f);
            lookRect.anchorMax = Vector2.one;
            lookRect.offsetMin = Vector2.zero;
            lookRect.offsetMax = Vector2.zero;
            var lookImg = lookObj.AddComponent<Image>();
            lookImg.color = new Color(0, 0, 0, 0);
            lookImg.raycastTarget = true;
            var lookZone = lookObj.AddComponent<TouchLookZone>();

            // 2. Movement Joystick Zone (Reduced size: Left 35% width, Bottom 52% height)
            var moveZoneObj = new GameObject("MovementZone");
            moveZoneObj.transform.SetParent(safeAreaObj.transform, false);
            var moveZoneRect = moveZoneObj.AddComponent<RectTransform>();
            moveZoneRect.anchorMin = Vector2.zero;
            moveZoneRect.anchorMax = new Vector2(0.35f, 0.52f);
            moveZoneRect.offsetMin = Vector2.zero;
            moveZoneRect.offsetMax = Vector2.zero;
            var moveZoneImg = moveZoneObj.AddComponent<Image>();
            moveZoneImg.color = new Color(0, 0, 0, 0);
            moveZoneImg.raycastTarget = true;
            var joystick = moveZoneObj.AddComponent<TouchVirtualJoystick>();

            // Joystick Base
            var joyBaseObj = new GameObject("JoystickBase");
            joyBaseObj.transform.SetParent(moveZoneObj.transform, false);
            var joyBaseRect = joyBaseObj.AddComponent<RectTransform>();
            joyBaseRect.anchorMin = new Vector2(0f, 0f);
            joyBaseRect.anchorMax = new Vector2(0f, 0f);
            joyBaseRect.anchoredPosition = new Vector2(175f, 175f);
            joyBaseRect.sizeDelta = new Vector2(210f, 210f);
            var joyBaseImg = joyBaseObj.AddComponent<Image>();
            joyBaseImg.sprite = bevelBaseSprite;
            joyBaseImg.color = new Color(0.2f, 0.3f, 0.45f, 0.4f);
            joyBaseImg.raycastTarget = false;
            var joyCg = joyBaseObj.AddComponent<CanvasGroup>();

            // Joystick Handle
            var joyHandleObj = new GameObject("JoystickHandle");
            joyHandleObj.transform.SetParent(joyBaseObj.transform, false);
            var joyHandleRect = joyHandleObj.AddComponent<RectTransform>();
            joyHandleRect.anchorMin = new Vector2(0.5f, 0.5f);
            joyHandleRect.anchorMax = new Vector2(0.5f, 0.5f);
            joyHandleRect.anchoredPosition = Vector2.zero;
            joyHandleRect.sizeDelta = new Vector2(90f, 90f);
            var joyHandleImg = joyHandleObj.AddComponent<Image>();
            joyHandleImg.sprite = circleFadedSprite;
            joyHandleImg.color = new Color(0.3f, 0.8f, 1f, 0.85f);
            joyHandleImg.raycastTarget = false;

            // Handle Icon
            var joyIconObj = new GameObject("Icon");
            joyIconObj.transform.SetParent(joyHandleObj.transform, false);
            var joyIconRect = joyIconObj.AddComponent<RectTransform>();
            joyIconRect.anchorMin = new Vector2(0.5f, 0.5f);
            joyIconRect.anchorMax = new Vector2(0.5f, 0.5f);
            joyIconRect.sizeDelta = new Vector2(45f, 45f);
            var joyIconImg = joyIconObj.AddComponent<Image>();
            joyIconImg.sprite = moveIcon;
            joyIconImg.color = Color.white;
            joyIconImg.raycastTarget = false;

            // Wire Joystick fields via SerializedObject
            var soJoy = new SerializedObject(joystick);
            soJoy.FindProperty("containerRect").objectReferenceValue = moveZoneRect;
            soJoy.FindProperty("baseRect").objectReferenceValue = joyBaseRect;
            soJoy.FindProperty("handleRect").objectReferenceValue = joyHandleRect;
            soJoy.FindProperty("canvasGroup").objectReferenceValue = joyCg;
            soJoy.ApplyModifiedPropertiesWithoutUndo();

            // 3. Action Cluster (Bottom Right)
            var actionCluster = new GameObject("CombatActionCluster");
            actionCluster.transform.SetParent(safeAreaObj.transform, false);
            var actionClusterRect = actionCluster.AddComponent<RectTransform>();
            actionClusterRect.anchorMin = new Vector2(1f, 0f);
            actionClusterRect.anchorMax = new Vector2(1f, 0f);
            actionClusterRect.pivot = new Vector2(1f, 0f);
            actionClusterRect.anchoredPosition = Vector2.zero;
            actionClusterRect.sizeDelta = new Vector2(600f, 600f);

            TouchActionButton CreateActionButton(string name, Vector2 pos, Vector2 size, Sprite icon, Color baseColor, Color pressedColor, float iconSize)
            {
                var btnObj = new GameObject(name);
                btnObj.transform.SetParent(actionCluster.transform, false);
                var rt = btnObj.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(1f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = pos;
                rt.sizeDelta = size;

                var baseImg = btnObj.AddComponent<Image>();
                baseImg.sprite = bevelBaseSprite;
                baseImg.color = baseColor;
                baseImg.raycastTarget = true;

                // Inner ring
                var innerObj = new GameObject("InnerGlow");
                innerObj.transform.SetParent(btnObj.transform, false);
                var innerRt = innerObj.AddComponent<RectTransform>();
                innerRt.anchorMin = Vector2.zero;
                innerRt.anchorMax = Vector2.one;
                innerRt.offsetMin = new Vector2(6f, 6f);
                innerRt.offsetMax = new Vector2(-6f, -6f);
                var innerImg = innerObj.AddComponent<Image>();
                innerImg.sprite = circleFadedSprite;
                innerImg.color = new Color(1f, 1f, 1f, 0.15f);
                innerImg.raycastTarget = false;

                // Icon
                var icObj = new GameObject("Icon");
                icObj.transform.SetParent(btnObj.transform, false);
                var icRt = icObj.AddComponent<RectTransform>();
                icRt.anchorMin = new Vector2(0.5f, 0.5f);
                icRt.anchorMax = new Vector2(0.5f, 0.5f);
                icRt.sizeDelta = new Vector2(iconSize, iconSize);
                var icImg = icObj.AddComponent<Image>();
                icImg.sprite = icon;
                icImg.color = Color.white;
                icImg.raycastTarget = false;

                // Cooldown overlay
                var cdObj = new GameObject("CooldownOverlay");
                cdObj.transform.SetParent(btnObj.transform, false);
                var cdRt = cdObj.AddComponent<RectTransform>();
                cdRt.anchorMin = Vector2.zero;
                cdRt.anchorMax = Vector2.one;
                cdRt.offsetMin = Vector2.zero;
                cdRt.offsetMax = Vector2.zero;
                var cdImg = cdObj.AddComponent<Image>();
                cdImg.sprite = bevelBaseSprite;
                cdImg.type = Image.Type.Filled;
                cdImg.fillMethod = Image.FillMethod.Radial360;
                cdImg.fillOrigin = (int)Image.Origin360.Top;
                cdImg.fillClockwise = false;
                cdImg.color = new Color(0f, 0f, 0f, 0.65f);
                cdImg.raycastTarget = false;
                cdObj.SetActive(false);

                var actionBtn = btnObj.AddComponent<TouchActionButton>();
                var soBtn = new SerializedObject(actionBtn);
                soBtn.FindProperty("targetGraphic").objectReferenceValue = baseImg;
                soBtn.FindProperty("normalColor").colorValue = baseColor;
                soBtn.FindProperty("pressedColor").colorValue = pressedColor;
                soBtn.FindProperty("cooldownOverlay").objectReferenceValue = cdImg;
                soBtn.ApplyModifiedPropertiesWithoutUndo();

                return actionBtn;
            }

            // 2-Part CODM Fire Button: Outer Border Housing + Deflecting Inner Button
            TouchActionButton CreateCODM2PartFireButton(Vector2 pos, Vector2 outerSize, Vector2 innerSize, Sprite icon, Color baseColor, Color pressedColor, float iconSize)
            {
                // 1. Outer Border Housing
                var outerObj = new GameObject("FireButton");
                outerObj.transform.SetParent(actionCluster.transform, false);
                var outerRt = outerObj.AddComponent<RectTransform>();
                outerRt.anchorMin = new Vector2(1f, 0f);
                outerRt.anchorMax = new Vector2(1f, 0f);
                outerRt.pivot = new Vector2(0.5f, 0.5f);
                outerRt.anchoredPosition = pos;
                outerRt.sizeDelta = outerSize;

                var outerBorderImg = outerObj.AddComponent<Image>();
                outerBorderImg.sprite = bevelBaseSprite;
                outerBorderImg.color = new Color(0.12f, 0.16f, 0.26f, 0.70f);
                outerBorderImg.raycastTarget = true;

                // Outer accent ring
                var ringGlowObj = new GameObject("OuterRingBorder");
                ringGlowObj.transform.SetParent(outerObj.transform, false);
                var ringGlowRt = ringGlowObj.AddComponent<RectTransform>();
                ringGlowRt.anchorMin = Vector2.zero;
                ringGlowRt.anchorMax = Vector2.one;
                ringGlowRt.offsetMin = new Vector2(2f, 2f);
                ringGlowRt.offsetMax = new Vector2(-2f, -2f);
                var ringGlowImg = ringGlowObj.AddComponent<Image>();
                ringGlowImg.sprite = circleFadedSprite;
                ringGlowImg.color = new Color(1f, 0.35f, 0.35f, 0.30f);
                ringGlowImg.raycastTarget = false;

                // 2. Inner Movable Disc
                var innerObj = new GameObject("InnerButton");
                innerObj.transform.SetParent(outerObj.transform, false);
                var innerRt = innerObj.AddComponent<RectTransform>();
                innerRt.anchorMin = new Vector2(0.5f, 0.5f);
                innerRt.anchorMax = new Vector2(0.5f, 0.5f);
                innerRt.pivot = new Vector2(0.5f, 0.5f);
                innerRt.anchoredPosition = Vector2.zero;
                innerRt.sizeDelta = innerSize;

                var innerImg = innerObj.AddComponent<Image>();
                innerImg.sprite = bevelBaseSprite;
                innerImg.color = baseColor;
                innerImg.raycastTarget = false;

                // Inner Glow
                var innerGlowObj = new GameObject("InnerGlow");
                innerGlowObj.transform.SetParent(innerObj.transform, false);
                var innerGlowRt = innerGlowObj.AddComponent<RectTransform>();
                innerGlowRt.anchorMin = Vector2.zero;
                innerGlowRt.anchorMax = Vector2.one;
                innerGlowRt.offsetMin = new Vector2(4f, 4f);
                innerGlowRt.offsetMax = new Vector2(-4f, -4f);
                var innerGlowImg = innerGlowObj.AddComponent<Image>();
                innerGlowImg.sprite = circleFadedSprite;
                innerGlowImg.color = new Color(1f, 1f, 1f, 0.20f);
                innerGlowImg.raycastTarget = false;

                // Icon inside inner button
                var icObj = new GameObject("Icon");
                icObj.transform.SetParent(innerObj.transform, false);
                var icRt = icObj.AddComponent<RectTransform>();
                icRt.anchorMin = new Vector2(0.5f, 0.5f);
                icRt.anchorMax = new Vector2(0.5f, 0.5f);
                icRt.sizeDelta = new Vector2(iconSize, iconSize);
                var icImg = icObj.AddComponent<Image>();
                icImg.sprite = icon;
                icImg.color = Color.white;
                icImg.raycastTarget = false;

                // Cooldown overlay inside inner button
                var cdObj = new GameObject("CooldownOverlay");
                cdObj.transform.SetParent(innerObj.transform, false);
                var cdRt = cdObj.AddComponent<RectTransform>();
                cdRt.anchorMin = Vector2.zero;
                cdRt.anchorMax = Vector2.one;
                cdRt.offsetMin = Vector2.zero;
                cdRt.offsetMax = Vector2.zero;
                var cdImg = cdObj.AddComponent<Image>();
                cdImg.sprite = bevelBaseSprite;
                cdImg.type = Image.Type.Filled;
                cdImg.fillMethod = Image.FillMethod.Radial360;
                cdImg.fillOrigin = (int)Image.Origin360.Top;
                cdImg.fillClockwise = false;
                cdImg.color = new Color(0f, 0f, 0f, 0.65f);
                cdImg.raycastTarget = false;
                cdObj.SetActive(false);

                // TouchActionButton on outer housing
                var actionBtn = outerObj.AddComponent<TouchActionButton>();
                var soBtn = new SerializedObject(actionBtn);
                soBtn.FindProperty("targetGraphic").objectReferenceValue = innerImg;
                soBtn.FindProperty("normalColor").colorValue = baseColor;
                soBtn.FindProperty("pressedColor").colorValue = pressedColor;
                soBtn.FindProperty("cooldownOverlay").objectReferenceValue = cdImg;
                soBtn.FindProperty("allowDragToAim").boolValue = true;
                soBtn.FindProperty("innerButtonRect").objectReferenceValue = innerRt;
                soBtn.FindProperty("maxDeflectionDistance").floatValue = 35f;
                soBtn.FindProperty("innerPressedScale").floatValue = 0.88f;
                soBtn.FindProperty("springReturnSpeed").floatValue = 22f;
                soBtn.ApplyModifiedPropertiesWithoutUndo();

                return actionBtn;
            }

            // Colors
            Color colFire = new Color(0.92f, 0.25f, 0.25f, 0.90f);
            Color colFirePress = new Color(1.0f, 0.55f, 0.55f, 1f);

            Color colStandard = new Color(0.12f, 0.16f, 0.26f, 0.82f);
            Color colStandardPress = new Color(0.35f, 0.75f, 1.0f, 1f);

            Color colAbility = new Color(0.18f, 0.35f, 0.55f, 0.85f);
            Color colAbilityPress = new Color(0.45f, 0.90f, 1.0f, 1f);

            // Action Buttons with clean ergonomic layout matching desktop/laptop gameplay actions
            var fireBtn = CreateCODM2PartFireButton(new Vector2(-125f, 125f), new Vector2(155f, 155f), new Vector2(118f, 118f), gunIcon, colFire, colFirePress, 65f);
            var jumpBtn = CreateActionButton("JumpButton", new Vector2(-115f, 290f), new Vector2(110f, 110f), jumpIcon, colStandard, colStandardPress, 60f);
            var dashBtn = CreateActionButton("DashButton", new Vector2(-250f, 220f), new Vector2(100f, 100f), sprintIcon, colAbility, colAbilityPress, 55f);
            var adsBtn = CreateActionButton("AltFireButton", new Vector2(-245f, 350f), new Vector2(100f, 100f), lookIcon, colStandard, colStandardPress, 55f);

            // 4. Top Navigation Bar
            var topBar = new GameObject("TopBarCluster");
            topBar.transform.SetParent(safeAreaObj.transform, false);
            var topBarRect = topBar.AddComponent<RectTransform>();
            topBarRect.anchorMin = new Vector2(0f, 1f);
            topBarRect.anchorMax = new Vector2(1f, 1f);
            topBarRect.offsetMin = Vector2.zero;
            topBarRect.offsetMax = Vector2.zero;

            Button CreateNavButton(string name, Vector2 anchor, Vector2 pos, Sprite icon)
            {
                var btnObj = new GameObject(name);
                btnObj.transform.SetParent(topBar.transform, false);
                var rt = btnObj.AddComponent<RectTransform>();
                rt.anchorMin = anchor;
                rt.anchorMax = anchor;
                rt.anchoredPosition = pos;
                rt.sizeDelta = new Vector2(65f, 65f);

                var img = btnObj.AddComponent<Image>();
                img.sprite = bevelBaseSprite;
                img.color = new Color(0.12f, 0.16f, 0.26f, 0.85f);

                var icObj = new GameObject("Icon");
                icObj.transform.SetParent(btnObj.transform, false);
                var icRt = icObj.AddComponent<RectTransform>();
                icRt.anchorMin = new Vector2(0.5f, 0.5f);
                icRt.anchorMax = new Vector2(0.5f, 0.5f);
                icRt.sizeDelta = new Vector2(38f, 38f);
                var icImg = icObj.AddComponent<Image>();
                icImg.sprite = icon;
                icImg.color = Color.white;
                icImg.raycastTarget = false;

                var btn = btnObj.AddComponent<Button>();
                var colors = btn.colors;
                colors.highlightedColor = new Color(0.3f, 0.7f, 1f, 1f);
                colors.pressedColor = new Color(0.5f, 0.9f, 1f, 1f);
                btn.colors = colors;

                return btn;
            }

            var pauseBtn = CreateNavButton("PauseButton", new Vector2(0f, 1f), new Vector2(50f, -50f), pauseIcon);
            var scoreBtn = CreateNavButton("ScoreboardButton", new Vector2(0.5f, 1f), new Vector2(0f, -50f), trophyIcon);

            // Wire master controller
            var soMaster = new SerializedObject(masterCtrl);
            soMaster.FindProperty("movementJoystick").objectReferenceValue = joystick;
            soMaster.FindProperty("lookZone").objectReferenceValue = lookZone;
            soMaster.FindProperty("fireButton").objectReferenceValue = fireBtn;
            soMaster.FindProperty("altFireButton").objectReferenceValue = adsBtn;
            soMaster.FindProperty("jumpButton").objectReferenceValue = jumpBtn;
            soMaster.FindProperty("dashButton").objectReferenceValue = dashBtn;
            soMaster.FindProperty("pauseButton").objectReferenceValue = pauseBtn;
            soMaster.FindProperty("scoreboardButton").objectReferenceValue = scoreBtn;
            soMaster.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
            soMaster.ApplyModifiedPropertiesWithoutUndo();

            // Save as Prefab to both Prefabs and Resources
            string prefabPath = "Assets/Prefabs/UI/MobileControlsCanvas.prefab";
            PrefabUtility.SaveAsPrefabAsset(rootObj, prefabPath);

            string resourcesPath = "Assets/Resources/UI/MobileControlsCanvas.prefab";
            PrefabUtility.SaveAsPrefabAsset(rootObj, resourcesPath);

            Object.DestroyImmediate(rootObj);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[MobileControlsBuilder] Successfully built and saved {prefabPath} and {resourcesPath}");
        }
    }
}
