#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Shootopia.Editor
{
    public static class MainMenuBuilder
    {
        [MenuItem("Shootopia/Rebuild Main Menu UI")]
        public static void RebuildMainMenu()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);

            // Load Assets
            var bgMediumTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/3rdParty/Space_Exploration_GUI_Kit/Background_Images/medium/home-background-medium.png");
            var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/3rdParty/Space_Exploration_GUI_Kit/Background_Images/medium/home-background-medium.png");
            if (bgSprite == null)
            {
                var all = AssetDatabase.LoadAllAssetsAtPath("Assets/3rdParty/Space_Exploration_GUI_Kit/Background_Images/medium/home-background-medium.png");
                foreach (var a in all) if (a is Sprite s) { bgSprite = s; break; }
            }

            var planet1Sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/3rdParty/Space_Exploration_GUI_Kit/Icons/exoplanet-1-512.png");
            var planet2Sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/3rdParty/Space_Exploration_GUI_Kit/Icons/exoplanet-4-512.png");
            var starPinkShort = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/3rdParty/Space_Exploration_GUI_Kit/Other_Images/Medium/shooting-star-pink-short-medium.png");
            var starPink = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/3rdParty/Space_Exploration_GUI_Kit/Other_Images/Medium/shooting-star-pink-medium.png");
            var iconRadar = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/3rdParty/Space_Exploration_GUI_Kit/Icons/radar-128.png");

            var btnYellowLarge = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/3rdParty/Space_Exploration_GUI_Kit/Button_Images/Source_Image_Sprites/large/large-yellow-sparkle-large.png");
            var btnBlueLarge = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/3rdParty/Space_Exploration_GUI_Kit/Button_Images/Source_Image_Sprites/large/large-blue-sparkle-large.png");
            var btnBlueMedium = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/3rdParty/Space_Exploration_GUI_Kit/Button_Images/Source_Image_Sprites/medium/medium-blue-medium.png");
            var btnYellowMedium = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/3rdParty/Space_Exploration_GUI_Kit/Button_Images/Source_Image_Sprites/medium/medium-yellow-medium.png");
            var btnSquareBlue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/3rdParty/Space_Exploration_GUI_Kit/Button_Images/Source_Image_Sprites/small/square-blue-small.png");
            var btnSquareYellow = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/3rdParty/Space_Exploration_GUI_Kit/Button_Images/Source_Image_Sprites/small/square-yellow-small.png");

            var containerSetting = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/3rdParty/Space_Exploration_GUI_Kit/Containers/Large/setting-container-large.png");
            var containerPause = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/3rdParty/Space_Exploration_GUI_Kit/Containers/Large/pause-container-large.png");

            var iconAstronaut = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/3rdParty/Space_Exploration_GUI_Kit/Icons/astronaut-128.png");
            var iconAlien = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/3rdParty/Space_Exploration_GUI_Kit/Icons/alien-128.png");
            var iconSettings = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/3rdParty/Space_Exploration_GUI_Kit/Picto_Icons/White/settings-64.png");
            var iconSoundOn = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/3rdParty/Space_Exploration_GUI_Kit/Picto_Icons/White/sound-64.png");
            var iconSoundOff = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/3rdParty/Space_Exploration_GUI_Kit/Picto_Icons/White/sound-off-64.png");
            var iconCross = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/3rdParty/Space_Exploration_GUI_Kit/Picto_Icons/White/cross-64.png");

            var fontTitle = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/3rdParty/Space_Exploration_GUI_Kit/Fonts/Righteous-Regular SDF.asset");
            var fontBody = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/3rdParty/Space_Exploration_GUI_Kit/Fonts/Asap-Bold SDF.asset");

            // Clean existing MainMenuCanvas roots
            foreach (var r in scene.GetRootGameObjects())
            {
                if (r.name == "MainMenuCanvas")
                {
                    Undo.DestroyObjectImmediate(r);
                }
            }

            // Create Master Canvas
            var canvasGO = new GameObject("MainMenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(MainMenuController));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var controller = canvasGO.GetComponent<MainMenuController>();
            var so = new SerializedObject(controller);

            // 1. Background Layer
            var bgRoot = CreateUIObject("BackgroundLayer", canvasGO.transform);
            StretchFull(bgRoot);

            var bgImg = bgRoot.AddComponent<Image>();
            bgImg.sprite = bgSprite;
            bgImg.color = new Color(0.9f, 0.95f, 1f, 1f);
            bgImg.raycastTarget = false;

            // Cosmic FX & Planets
            var planet1GO = CreateUIObject("Planet_UpperLeft", bgRoot.transform);
            var p1Rect = planet1GO.GetComponent<RectTransform>();
            p1Rect.anchorMin = p1Rect.anchorMax = new Vector2(0.12f, 0.82f);
            p1Rect.sizeDelta = new Vector2(180, 180);
            var p1Img = planet1GO.AddComponent<Image>();
            p1Img.sprite = planet1Sprite;
            p1Img.color = new Color(0.85f, 0.9f, 1f, 0.75f);
            p1Img.raycastTarget = false;

            var planet2GO = CreateUIObject("Planet_LowerRight", bgRoot.transform);
            var p2Rect = planet2GO.GetComponent<RectTransform>();
            p2Rect.anchorMin = p2Rect.anchorMax = new Vector2(0.86f, 0.3f);
            p2Rect.sizeDelta = new Vector2(120, 120);
            var p2Img = planet2GO.AddComponent<Image>();
            p2Img.sprite = planet2Sprite;
            p2Img.color = new Color(1f, 0.85f, 0.9f, 0.65f);
            p2Img.raycastTarget = false;

            var starContainer = CreateUIObject("ShootingStarContainer", bgRoot.transform);
            StretchFull(starContainer);
            var cosmicFx = bgRoot.AddComponent<CosmicBackgroundFx>();
            var soFx = new SerializedObject(cosmicFx);
            soFx.FindProperty("planet1").objectReferenceValue = p1Rect;
            soFx.FindProperty("planet2").objectReferenceValue = p2Rect;
            soFx.FindProperty("starContainer").objectReferenceValue = starContainer.GetComponent<RectTransform>();
            var starSpritesProp = soFx.FindProperty("starSprites");
            starSpritesProp.arraySize = 2;
            starSpritesProp.GetArrayElementAtIndex(0).objectReferenceValue = starPinkShort;
            starSpritesProp.GetArrayElementAtIndex(1).objectReferenceValue = starPink;
            soFx.ApplyModifiedProperties();

            // 2. Top Header Bar
            var topHeader = CreateUIObject("TopHeaderBar", canvasGO.transform);
            var thRect = topHeader.GetComponent<RectTransform>();
            thRect.anchorMin = new Vector2(0, 1);
            thRect.anchorMax = new Vector2(1, 1);
            thRect.pivot = new Vector2(0.5f, 1);
            thRect.anchoredPosition = new Vector2(0, -25);
            thRect.sizeDelta = new Vector2(-80, 64);

            // Profile Pill (Left)
            var profilePill = CreateUIObject("ProfilePill", topHeader.transform);
            var pillRect = profilePill.GetComponent<RectTransform>();
            pillRect.anchorMin = pillRect.anchorMax = new Vector2(0f, 0.5f);
            pillRect.pivot = new Vector2(0f, 0.5f);
            pillRect.anchoredPosition = new Vector2(10, 0);
            pillRect.sizeDelta = new Vector2(280, 56);
            var pillImg = profilePill.AddComponent<Image>();
            pillImg.sprite = btnBlueMedium;
            pillImg.type = Image.Type.Sliced;
            pillImg.color = new Color(0.12f, 0.16f, 0.28f, 0.95f);

            var avatarIconGO = CreateUIObject("AvatarIcon", profilePill.transform);
            var aRect = avatarIconGO.GetComponent<RectTransform>();
            aRect.anchorMin = aRect.anchorMax = new Vector2(0, 0.5f);
            aRect.pivot = new Vector2(0, 0.5f);
            aRect.anchoredPosition = new Vector2(8, 0);
            aRect.sizeDelta = new Vector2(40, 40);
            var aImg = avatarIconGO.AddComponent<Image>();
            aImg.sprite = iconAstronaut;

            var nameTextGO = CreateUIObject("PlayerName", profilePill.transform);
            var ntRect = nameTextGO.GetComponent<RectTransform>();
            ntRect.anchorMin = new Vector2(0, 0);
            ntRect.anchorMax = new Vector2(1, 1);
            ntRect.offsetMin = new Vector2(56, 0);
            ntRect.offsetMax = new Vector2(-46, 0);
            var nameTMP = nameTextGO.AddComponent<TextMeshProUGUI>();
            nameTMP.font = fontTitle;
            nameTMP.fontSize = 20;
            nameTMP.color = Color.white;
            nameTMP.alignment = TextAlignmentOptions.Left;
            nameTMP.text = "AGENT_01";

            var btnEditGO = CreateIconButton("BtnEdit", profilePill.transform, iconSettings, new Vector2(36, 36), new Vector2(1, 0.5f), new Vector2(-6, 0), btnSquareBlue);
            so.FindProperty("playerNameLabel").objectReferenceValue = nameTMP;
            so.FindProperty("btnEditProfile").objectReferenceValue = btnEditGO.GetComponent<Button>();

            // Header Right Buttons (Sound toggle, Top Settings)
            var btnSoundGO = CreateIconButton("BtnToggleSound", topHeader.transform, iconSoundOn, new Vector2(50, 50), new Vector2(1, 0.5f), new Vector2(-70, 0), btnSquareBlue);
            var btnTopSettingsGO = CreateIconButton("BtnTopSettings", topHeader.transform, iconSettings, new Vector2(50, 50), new Vector2(1, 0.5f), new Vector2(0, 0), btnSquareBlue);

            so.FindProperty("btnToggleSound").objectReferenceValue = btnSoundGO.GetComponent<Button>();
            so.FindProperty("soundIconImage").objectReferenceValue = btnSoundGO.transform.Find("Icon").GetComponent<Image>();
            so.FindProperty("soundOnSprite").objectReferenceValue = iconSoundOn;
            so.FindProperty("soundOffSprite").objectReferenceValue = iconSoundOff;
            so.FindProperty("btnTopSettings").objectReferenceValue = btnTopSettingsGO.GetComponent<Button>();

            // 3. Main Action Hub (Right side)
            var actionHub = CreateUIObject("ActionHub", canvasGO.transform);
            var ahRect = actionHub.GetComponent<RectTransform>();
            ahRect.anchorMin = ahRect.anchorMax = new Vector2(0.85f, 0.42f);
            ahRect.pivot = new Vector2(0.5f, 0.5f);
            ahRect.sizeDelta = new Vector2(320, 480);
            var ahVlg = actionHub.AddComponent<VerticalLayoutGroup>();
            ahVlg.childAlignment = TextAnchor.MiddleCenter;
            ahVlg.spacing = 18;
            ahVlg.childControlWidth = true;
            ahVlg.childControlHeight = false;

            var btnPlayGO = CreateMenuButton("BtnPlayMatch", actionHub.transform, "QUICK PLAY", btnYellowLarge, fontTitle, 26, 76, new Color(0.1f, 0.05f, 0f, 1f));
            var btnCustomGO = CreateMenuButton("BtnCustomRooms", actionHub.transform, "CUSTOM ROOMS", btnBlueMedium, fontTitle, 21, 62, Color.white);
            var btnCustomizeGO = CreateMenuButton("BtnCustomize", actionHub.transform, "CUSTOMIZE", btnBlueMedium, fontTitle, 21, 62, Color.white);
            var btnSettingsGO = CreateMenuButton("BtnSettings", actionHub.transform, "SETTINGS", btnBlueMedium, fontTitle, 21, 62, Color.white);
            var btnQuitGO = CreateMenuButton("BtnQuit", actionHub.transform, "QUIT", btnBlueMedium, fontTitle, 19, 54, new Color(1f, 0.6f, 0.6f, 1f));

            so.FindProperty("btnPlayMatch").objectReferenceValue = btnPlayGO.GetComponent<Button>();
            so.FindProperty("btnCustomRooms").objectReferenceValue = btnCustomGO.GetComponent<Button>();
            so.FindProperty("btnCustomize").objectReferenceValue = btnCustomizeGO.GetComponent<Button>();
            so.FindProperty("btnSettings").objectReferenceValue = btnSettingsGO.GetComponent<Button>();
            so.FindProperty("btnQuit").objectReferenceValue = btnQuitGO.GetComponent<Button>();

            // 4. Modals
            // Modal 1: Quick Match Filter
            var modalQuick = CreateModalBase("Modal_QuickFilter", canvasGO.transform, "QUICK MATCH", containerSetting, fontTitle, new Vector2(620, 480));
            var quickCg = modalQuick.GetComponent<CanvasGroup>();
            var quickClose = modalQuick.transform.Find("BtnClose").GetComponent<Button>();
            so.FindProperty("modalQuickFilter").objectReferenceValue = modalQuick;
            so.FindProperty("quickFilterGroup").objectReferenceValue = quickCg;
            so.FindProperty("btnCloseQuickFilter").objectReferenceValue = quickClose;

            var qContent = CreateUIObject("Content", modalQuick.transform);
            var qcRect = qContent.GetComponent<RectTransform>();
            qcRect.anchorMin = new Vector2(0.08f, 0.12f);
            qcRect.anchorMax = new Vector2(0.92f, 0.78f);
            qcRect.offsetMin = qcRect.offsetMax = Vector2.zero;
            var qcVlg = qContent.AddComponent<VerticalLayoutGroup>();
            qcVlg.spacing = 16;
            qcVlg.childAlignment = TextAnchor.UpperCenter;
            qcVlg.childControlWidth = true;
            qcVlg.childControlHeight = false;

            var chipRow = CreateUIObject("ChipRow", qContent.transform);
            var crHlg = chipRow.AddComponent<HorizontalLayoutGroup>();
            crHlg.spacing = 12;
            crHlg.childAlignment = TextAnchor.MiddleCenter;
            crHlg.childControlWidth = true;
            crHlg.childControlHeight = false;

            var qChipAll = CreateMenuButton("ChipQuickAll", chipRow.transform, "ALL MODES", btnBlueMedium, fontTitle, 16, 48, Color.white);
            var qChipFFA = CreateMenuButton("ChipQuickFFA", chipRow.transform, "FFA", btnBlueMedium, fontTitle, 16, 48, Color.white);
            var qChipTDM = CreateMenuButton("ChipQuickTDM", chipRow.transform, "TDM", btnBlueMedium, fontTitle, 16, 48, Color.white);

            var descBox = CreateUIObject("DescBox", qContent.transform);
            var descTMP = descBox.AddComponent<TextMeshProUGUI>();
            descTMP.font = fontBody;
            descTMP.fontSize = 17;
            descTMP.color = new Color(0.85f, 0.9f, 1f, 0.9f);
            descTMP.alignment = TextAlignmentOptions.Center;
            descTMP.text = "ANY MODE: Connects instantly to the fastest available public arena.";

            var qConfirmPlay = CreateMenuButton("BtnConfirmQuickPlay", qContent.transform, "PLAY NOW", btnYellowLarge, fontTitle, 24, 70, new Color(0.1f, 0.05f, 0f, 1f));

            so.FindProperty("chipQuickAll").objectReferenceValue = qChipAll.GetComponent<Button>();
            so.FindProperty("chipQuickFFA").objectReferenceValue = qChipFFA.GetComponent<Button>();
            so.FindProperty("chipQuickTDM").objectReferenceValue = qChipTDM.GetComponent<Button>();
            so.FindProperty("quickModeDescText").objectReferenceValue = descTMP;
            so.FindProperty("btnConfirmQuickPlay").objectReferenceValue = qConfirmPlay.GetComponent<Button>();

            // Modal 2: Custom Rooms Hub (Create / Join Tabs)
            var modalCustom = CreateModalBase("Modal_CustomRooms", canvasGO.transform, "CUSTOM ROOMS", containerSetting, fontTitle, new Vector2(740, 580));
            var customCg = modalCustom.GetComponent<CanvasGroup>();
            var customClose = modalCustom.transform.Find("BtnClose").GetComponent<Button>();
            so.FindProperty("modalCustomRooms").objectReferenceValue = modalCustom;
            so.FindProperty("customRoomsGroup").objectReferenceValue = customCg;
            so.FindProperty("btnCloseCustomRooms").objectReferenceValue = customClose;

            // Tab Bar
            var tabRow = CreateUIObject("TabBar", modalCustom.transform);
            var tbRect = tabRow.GetComponent<RectTransform>();
            tbRect.anchorMin = new Vector2(0.12f, 0.8f);
            tbRect.anchorMax = new Vector2(0.88f, 0.89f);
            tbRect.offsetMin = tbRect.offsetMax = Vector2.zero;
            var tbHlg = tabRow.AddComponent<HorizontalLayoutGroup>();
            tbHlg.spacing = 14;
            tbHlg.childAlignment = TextAnchor.MiddleCenter;
            tbHlg.childControlWidth = true;
            tbHlg.childControlHeight = true;

            var tabBtnCreateGO = CreateMenuButton("TabBtnCreate", tabRow.transform, "CREATE ROOM", btnYellowMedium, fontTitle, 18, 48, Color.black);
            var tabBtnJoinGO = CreateMenuButton("TabBtnJoin", tabRow.transform, "JOIN ROOM", btnBlueMedium, fontTitle, 18, 48, Color.white);
            so.FindProperty("tabBtnCreate").objectReferenceValue = tabBtnCreateGO.GetComponent<Button>();
            so.FindProperty("tabBtnJoin").objectReferenceValue = tabBtnJoinGO.GetComponent<Button>();

            // Tab Content 1: Create Room
            var tabCreate = CreateUIObject("TabContent_Create", modalCustom.transform);
            var tcRect = tabCreate.GetComponent<RectTransform>();
            tcRect.anchorMin = new Vector2(0.1f, 0.08f);
            tcRect.anchorMax = new Vector2(0.9f, 0.76f);
            tcRect.offsetMin = tcRect.offsetMax = Vector2.zero;
            var tcVlg = tabCreate.AddComponent<VerticalLayoutGroup>();
            tcVlg.spacing = 14;
            tcVlg.childAlignment = TextAnchor.UpperCenter;
            tcVlg.childControlWidth = true;
            tcVlg.childControlHeight = false;

            var inputCreateName = CreateInputField("InputRoomName", tabCreate.transform, "Arena Name...", fontBody, btnBlueMedium);
            var modeRow = CreateUIObject("ModeRow", tabCreate.transform);
            var mrHlg = modeRow.AddComponent<HorizontalLayoutGroup>();
            mrHlg.spacing = 12;
            mrHlg.childAlignment = TextAnchor.MiddleCenter;
            mrHlg.childControlWidth = true;
            mrHlg.childControlHeight = false;
            var chipFFA = CreateMenuButton("ChipModeFFA", modeRow.transform, "FREE-FOR-ALL", btnBlueMedium, fontTitle, 16, 44, Color.white);
            var chipTDM = CreateMenuButton("ChipModeTDM", modeRow.transform, "TEAM DEATHMATCH", btnBlueMedium, fontTitle, 16, 44, Color.white);

            var sliderPlayers = CreateSliderRow("SliderMaxPlayers", tabCreate.transform, "MAX PLAYERS: ", "8 PLAYERS", 2f, 8f, 8f, true, fontBody);
            var togglePublic = CreateToggleRow("TogglePublic", tabCreate.transform, "PUBLIC ARENA (VISIBLE IN BROWSER)", true, fontBody);
            var btnConfirmCreateGO = CreateMenuButton("BtnConfirmCreate", tabCreate.transform, "CREATE ARENA", btnYellowLarge, fontTitle, 22, 60, Color.black);

            so.FindProperty("tabContentCreate").objectReferenceValue = tabCreate;
            so.FindProperty("inputCreateRoomName").objectReferenceValue = inputCreateName.GetComponent<TMP_InputField>();
            so.FindProperty("chipModeFFA").objectReferenceValue = chipFFA.GetComponent<Button>();
            so.FindProperty("chipModeTDM").objectReferenceValue = chipTDM.GetComponent<Button>();
            so.FindProperty("sliderMaxPlayers").objectReferenceValue = sliderPlayers.GetComponentInChildren<Slider>();
            so.FindProperty("labelMaxPlayers").objectReferenceValue = sliderPlayers.transform.Find("ValueLabel")?.GetComponent<TextMeshProUGUI>();
            var btnMinus = sliderPlayers.transform.Find("BtnMinus")?.GetComponent<Button>();
            var btnPlus = sliderPlayers.transform.Find("BtnPlus")?.GetComponent<Button>();
            if (btnMinus != null) so.FindProperty("btnDecreasePlayers").objectReferenceValue = btnMinus;
            if (btnPlus != null) so.FindProperty("btnIncreasePlayers").objectReferenceValue = btnPlus;
            so.FindProperty("togglePublicRoom").objectReferenceValue = togglePublic.GetComponentInChildren<Toggle>();
            so.FindProperty("btnConfirmCreate").objectReferenceValue = btnConfirmCreateGO.GetComponent<Button>();

            // Tab Content 2: Join Room (Code & Browser)
            var tabJoin = CreateUIObject("TabContent_Join", modalCustom.transform);
            var tjRect = tabJoin.GetComponent<RectTransform>();
            tjRect.anchorMin = new Vector2(0.08f, 0.06f);
            tjRect.anchorMax = new Vector2(0.92f, 0.76f);
            tjRect.offsetMin = tjRect.offsetMax = Vector2.zero;
            var tjVlg = tabJoin.AddComponent<VerticalLayoutGroup>();
            tjVlg.spacing = 10;
            tjVlg.childAlignment = TextAnchor.UpperCenter;
            tjVlg.childControlWidth = true;
            tjVlg.childControlHeight = false;

            // Direct code row
            var codeRow = CreateUIObject("CodeRow", tabJoin.transform);
            var crH = codeRow.AddComponent<HorizontalLayoutGroup>();
            crH.spacing = 10;
            crH.childAlignment = TextAnchor.MiddleCenter;
            crH.childControlWidth = true;
            crH.childControlHeight = false;
            var inputCode = CreateInputField("InputJoinCode", codeRow.transform, "ENTER ROOM CODE", fontTitle, btnBlueMedium);
            var btnSubmitCode = CreateMenuButton("BtnSubmitCode", codeRow.transform, "JOIN", btnYellowMedium, fontTitle, 18, 48, Color.black);
            var le1 = inputCode.AddComponent<LayoutElement>(); le1.flexibleWidth = 2f; le1.preferredHeight = 48;
            var le2 = btnSubmitCode.AddComponent<LayoutElement>(); le2.flexibleWidth = 1f; le2.preferredHeight = 48;

            // Browser filter row
            var browserFilterRow = CreateUIObject("BrowserFilterRow", tabJoin.transform);
            var bfrH = browserFilterRow.AddComponent<HorizontalLayoutGroup>();
            bfrH.spacing = 8;
            bfrH.childAlignment = TextAnchor.MiddleCenter;
            bfrH.childControlWidth = true;
            bfrH.childControlHeight = false;
            var chipBAll = CreateMenuButton("ChipFilterAll", browserFilterRow.transform, "ALL", btnBlueMedium, fontTitle, 14, 38, Color.white);
            var chipBFFA = CreateMenuButton("ChipFilterFFA", browserFilterRow.transform, "FFA", btnBlueMedium, fontTitle, 14, 38, Color.white);
            var chipBTDM = CreateMenuButton("ChipFilterTDM", browserFilterRow.transform, "TDM", btnBlueMedium, fontTitle, 14, 38, Color.white);
            var btnRefresh = CreateMenuButton("BtnRefreshRooms", browserFilterRow.transform, "REFRESH", btnYellowMedium, fontTitle, 14, 38, Color.black);

            // Scroll view for rooms
            var scrollGO = CreateScrollView("RoomScroll", tabJoin.transform, out var roomContent);
            var leScroll = scrollGO.AddComponent<LayoutElement>();
            leScroll.preferredHeight = 180;
            leScroll.flexibleWidth = 1f;

            var emptyStateGO = CreateUIObject("EmptyState", tabJoin.transform);
            var esTMP = emptyStateGO.AddComponent<TextMeshProUGUI>();
            esTMP.font = fontBody;
            esTMP.fontSize = 16;
            esTMP.color = new Color(0.7f, 0.75f, 0.85f, 0.8f);
            esTMP.alignment = TextAlignmentOptions.Center;
            esTMP.text = "NO PUBLIC ROOMS FOUND. ENTER CODE ABOVE OR CREATE ONE!";

            so.FindProperty("tabContentJoin").objectReferenceValue = tabJoin;
            so.FindProperty("inputJoinCode").objectReferenceValue = inputCode.GetComponent<TMP_InputField>();
            so.FindProperty("btnSubmitJoinCode").objectReferenceValue = btnSubmitCode.GetComponent<Button>();
            so.FindProperty("chipFilterAll").objectReferenceValue = chipBAll.GetComponent<Button>();
            so.FindProperty("chipFilterFFA").objectReferenceValue = chipBFFA.GetComponent<Button>();
            so.FindProperty("chipFilterTDM").objectReferenceValue = chipBTDM.GetComponent<Button>();
            so.FindProperty("btnRefreshRooms").objectReferenceValue = btnRefresh.GetComponent<Button>();
            so.FindProperty("roomListContent").objectReferenceValue = roomContent;
            so.FindProperty("browseEmptyState").objectReferenceValue = emptyStateGO;

            // Modal 3: Settings
            var modalSettings = CreateModalBase("Modal_Settings", canvasGO.transform, "SETTINGS", containerSetting, fontTitle, new Vector2(620, 560));
            var settingsCg = modalSettings.GetComponent<CanvasGroup>();
            var settingsClose = modalSettings.transform.Find("BtnClose").GetComponent<Button>();
            so.FindProperty("modalSettings").objectReferenceValue = modalSettings;
            so.FindProperty("settingsGroup").objectReferenceValue = settingsCg;
            so.FindProperty("btnCloseSettings").objectReferenceValue = settingsClose;

            var sContent = CreateUIObject("Content", modalSettings.transform);
            var scRect = sContent.GetComponent<RectTransform>();
            scRect.anchorMin = new Vector2(0.08f, 0.08f);
            scRect.anchorMax = new Vector2(0.92f, 0.82f);
            scRect.offsetMin = scRect.offsetMax = Vector2.zero;
            var scVlg = sContent.AddComponent<VerticalLayoutGroup>();
            scVlg.spacing = 12;
            scVlg.childAlignment = TextAnchor.UpperCenter;
            scVlg.childControlWidth = true;
            scVlg.childControlHeight = false;

            var sMaster = CreateSliderRow("MasterVol", sContent.transform, "MASTER VOLUME", "100%", 0f, 1f, 1f, false, fontBody);
            var sMusic = CreateSliderRow("MusicVol", sContent.transform, "MUSIC VOLUME", "80%", 0f, 1f, 0.8f, false, fontBody);
            var sSFX = CreateSliderRow("SFXVol", sContent.transform, "SFX VOLUME", "100%", 0f, 1f, 1f, false, fontBody);
            var sSens = CreateSliderRow("MouseSens", sContent.transform, "MOUSE SENSITIVITY", "1.0x", 0.2f, 3.0f, 1.0f, false, fontBody);
            var tInvert = CreateToggleRow("ToggleInvertY", sContent.transform, "INVERT LOOK Y-AXIS", false, fontBody);
            var tFull = CreateToggleRow("ToggleFullscreen", sContent.transform, "FULLSCREEN MODE", Screen.fullScreen, fontBody);

            so.FindProperty("sliderMasterVol").objectReferenceValue = sMaster.GetComponentInChildren<Slider>();
            so.FindProperty("labelMasterVol").objectReferenceValue = sMaster.transform.Find("ValueLabel").GetComponent<TextMeshProUGUI>();
            so.FindProperty("sliderMusicVol").objectReferenceValue = sMusic.GetComponentInChildren<Slider>();
            so.FindProperty("labelMusicVol").objectReferenceValue = sMusic.transform.Find("ValueLabel").GetComponent<TextMeshProUGUI>();
            so.FindProperty("sliderSFXVol").objectReferenceValue = sSFX.GetComponentInChildren<Slider>();
            so.FindProperty("labelSFXVol").objectReferenceValue = sSFX.transform.Find("ValueLabel").GetComponent<TextMeshProUGUI>();
            so.FindProperty("sliderMouseSens").objectReferenceValue = sSens.GetComponentInChildren<Slider>();
            so.FindProperty("labelMouseSens").objectReferenceValue = sSens.transform.Find("ValueLabel").GetComponent<TextMeshProUGUI>();
            so.FindProperty("toggleInvertY").objectReferenceValue = tInvert.GetComponentInChildren<Toggle>();
            so.FindProperty("toggleFullscreen").objectReferenceValue = tFull.GetComponentInChildren<Toggle>();

            // Modal 4: Customize
            var modalCust = CreateModalBase("Modal_Customize", canvasGO.transform, "AGENT CUSTOMIZATION", containerSetting, fontTitle, new Vector2(580, 380));
            var custCg = modalCust.GetComponent<CanvasGroup>();
            var custClose = modalCust.transform.Find("BtnClose").GetComponent<Button>();
            so.FindProperty("modalCustomize").objectReferenceValue = modalCust;
            so.FindProperty("customizeGroup").objectReferenceValue = custCg;
            so.FindProperty("btnCloseCustomize").objectReferenceValue = custClose;

            var custContent = CreateUIObject("Content", modalCust.transform);
            var cstRect = custContent.GetComponent<RectTransform>();
            cstRect.anchorMin = new Vector2(0.1f, 0.15f);
            cstRect.anchorMax = new Vector2(0.9f, 0.78f);
            cstRect.offsetMin = cstRect.offsetMax = Vector2.zero;
            var cstHlg = custContent.AddComponent<HorizontalLayoutGroup>();
            cstHlg.spacing = 16;
            cstHlg.childAlignment = TextAnchor.MiddleCenter;
            cstHlg.childControlWidth = true;
            cstHlg.childControlHeight = true;

            var btnAgent = CreateMenuButton("BtnSkinAgent", custContent.transform, "DEFAULT AGENT", btnYellowMedium, fontTitle, 18, 64, Color.black);
            var btnRobot = CreateMenuButton("BtnSkinRobot", custContent.transform, "CYBER ROBOT", btnBlueMedium, fontTitle, 18, 64, Color.white);
            so.FindProperty("btnSkinAgent").objectReferenceValue = btnAgent.GetComponent<Button>();
            so.FindProperty("btnSkinRobot").objectReferenceValue = btnRobot.GetComponent<Button>();

            // Modal 5: Edit Profile
            var modalProf = CreateModalBase("Modal_EditProfile", canvasGO.transform, "CALLSIGN PROFILE", containerSetting, fontTitle, new Vector2(540, 340));
            var profCg = modalProf.GetComponent<CanvasGroup>();
            var profClose = modalProf.transform.Find("BtnClose").GetComponent<Button>();
            so.FindProperty("modalEditProfile").objectReferenceValue = modalProf;
            so.FindProperty("editProfileGroup").objectReferenceValue = profCg;
            so.FindProperty("btnCloseProfile").objectReferenceValue = profClose;

            var profContent = CreateUIObject("Content", modalProf.transform);
            var pfcRect = profContent.GetComponent<RectTransform>();
            pfcRect.anchorMin = new Vector2(0.1f, 0.15f);
            pfcRect.anchorMax = new Vector2(0.9f, 0.78f);
            pfcRect.offsetMin = pfcRect.offsetMax = Vector2.zero;
            var pfcVlg = profContent.AddComponent<VerticalLayoutGroup>();
            pfcVlg.spacing = 16;
            pfcVlg.childAlignment = TextAnchor.MiddleCenter;
            pfcVlg.childControlWidth = true;
            pfcVlg.childControlHeight = false;

            var inputDispName = CreateInputField("InputDisplayName", profContent.transform, "Callsign...", fontTitle, btnBlueMedium);
            var btnSaveProf = CreateMenuButton("BtnSaveProfile", profContent.transform, "SAVE CALLSIGN", btnYellowMedium, fontTitle, 18, 54, Color.black);
            so.FindProperty("inputDisplayName").objectReferenceValue = inputDispName.GetComponent<TMP_InputField>();
            so.FindProperty("btnSaveProfile").objectReferenceValue = btnSaveProf.GetComponent<Button>();

            // Modal 6: Busy Overlay
            var busyGO = CreateUIObject("BusyOverlay", canvasGO.transform);
            StretchFull(busyGO);
            var busyBg = busyGO.AddComponent<Image>();
            busyBg.color = new Color(0.04f, 0.06f, 0.12f, 0.85f);
            var busyCg = busyGO.AddComponent<CanvasGroup>();
            var busyVlg = busyGO.AddComponent<VerticalLayoutGroup>();
            busyVlg.childAlignment = TextAnchor.MiddleCenter;
            busyVlg.spacing = 18;
            busyVlg.childControlWidth = false;
            busyVlg.childControlHeight = false;

            var spinnerGO = CreateUIObject("SpinnerIcon", busyGO.transform);
            spinnerGO.GetComponent<RectTransform>().sizeDelta = new Vector2(72, 72);
            var spImg = spinnerGO.AddComponent<Image>();
            spImg.sprite = iconRadar;

            var busyLabelGO = CreateUIObject("BusyLabel", busyGO.transform);
            var blTMP = busyLabelGO.AddComponent<TextMeshProUGUI>();
            blTMP.font = fontTitle;
            blTMP.fontSize = 24;
            blTMP.color = new Color(1f, 0.9f, 0.4f, 1f);
            blTMP.alignment = TextAlignmentOptions.Center;
            blTMP.text = "WARPING TO ARENA...";

            var btnCancelBusyGO = CreateMenuButton("BtnCancelBusy", busyGO.transform, "CANCEL", btnBlueMedium, fontTitle, 18, 50, Color.white);
            btnCancelBusyGO.GetComponent<RectTransform>().sizeDelta = new Vector2(180, 50);

            so.FindProperty("busyOverlay").objectReferenceValue = busyGO;
            so.FindProperty("busyGroup").objectReferenceValue = busyCg;
            so.FindProperty("busyLabel").objectReferenceValue = blTMP;
            so.FindProperty("btnCancelBusy").objectReferenceValue = btnCancelBusyGO.GetComponent<Button>();
            so.FindProperty("spinnerIcon").objectReferenceValue = spinnerGO.transform;

            // Modal 7: Error Dialog
            var modalErr = CreateModalBase("Modal_Error", canvasGO.transform, "ALERT", containerPause, fontTitle, new Vector2(560, 320));
            var errCg = modalErr.GetComponent<CanvasGroup>();
            var errClose = modalErr.transform.Find("BtnClose").GetComponent<Button>();

            var errLabelGO = CreateUIObject("ErrorLabel", modalErr.transform);
            var elRect = errLabelGO.GetComponent<RectTransform>();
            elRect.anchorMin = new Vector2(0.1f, 0.35f);
            elRect.anchorMax = new Vector2(0.9f, 0.75f);
            elRect.offsetMin = elRect.offsetMax = Vector2.zero;
            var elTMP = errLabelGO.AddComponent<TextMeshProUGUI>();
            elTMP.font = fontBody;
            elTMP.fontSize = 17;
            elTMP.color = new Color(1f, 0.8f, 0.8f, 1f);
            elTMP.alignment = TextAlignmentOptions.Center;
            elTMP.text = "An unexpected error occurred.";

            var btnDismissGO = CreateMenuButton("BtnDismissError", modalErr.transform, "DISMISS", btnYellowMedium, fontTitle, 18, 50, Color.black);
            var dmRect = btnDismissGO.GetComponent<RectTransform>();
            dmRect.anchorMin = dmRect.anchorMax = new Vector2(0.5f, 0.2f);
            dmRect.sizeDelta = new Vector2(180, 50);

            so.FindProperty("errorModal").objectReferenceValue = modalErr;
            so.FindProperty("errorGroup").objectReferenceValue = errCg;
            so.FindProperty("errorLabel").objectReferenceValue = elTMP;
            so.FindProperty("btnDismissError").objectReferenceValue = btnDismissGO.GetComponent<Button>();

            // Create Room Card Prefab for browser
            var cardPrefab = CreateRoomCardPrefab(btnBlueMedium, fontTitle, fontBody);
            so.FindProperty("roomCardPrefab").objectReferenceValue = cardPrefab;

            // Hide modals by default
            modalQuick.SetActive(false);
            modalCustom.SetActive(false);
            modalSettings.SetActive(false);
            modalCust.SetActive(false);
            modalProf.SetActive(false);
            busyGO.SetActive(false);
            modalErr.SetActive(false);

            so.ApplyModifiedProperties();

            // Set up AvatarStage framing
            var stage = Object.FindAnyObjectByType<AvatarStageController>();
            if (stage != null)
            {
                stage.SetupFraming();
            }

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            Debug.Log("[MainMenuBuilder] MainMenu UI rebuilt and wired successfully with Cosmic Space design!");
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void StretchFull(GameObject go)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static GameObject CreateMenuButton(string name, Transform parent, string text, Sprite bg, TMP_FontAsset font, float fontSize, float height, Color textColor)
        {
            var btnGO = CreateUIObject(name, parent);
            var rect = btnGO.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(280, height);

            var img = btnGO.AddComponent<Image>();
            img.sprite = bg;
            img.type = Image.Type.Sliced;
            img.color = Color.white;

            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = img;

            btnGO.AddComponent<UIJuice>();

            var labelGO = CreateUIObject("Label", btnGO.transform);
            StretchFull(labelGO);
            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.fontSize = fontSize;
            tmp.color = textColor;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.text = text;

            return btnGO;
        }

        private static GameObject CreateIconButton(string name, Transform parent, Sprite icon, Vector2 size, Vector2 anchor, Vector2 pos, Sprite bg)
        {
            var btnGO = CreateUIObject(name, parent);
            var rect = btnGO.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            var img = btnGO.AddComponent<Image>();
            img.sprite = bg;
            img.type = Image.Type.Sliced;

            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = img;
            btnGO.AddComponent<UIJuice>();

            var iconGO = CreateUIObject("Icon", btnGO.transform);
            var iRect = iconGO.GetComponent<RectTransform>();
            iRect.anchorMin = iRect.anchorMax = new Vector2(0.5f, 0.5f);
            iRect.sizeDelta = size * 0.55f;
            var iImg = iconGO.AddComponent<Image>();
            iImg.sprite = icon;
            iImg.color = Color.white;
            iImg.raycastTarget = false;

            return btnGO;
        }

        private static GameObject CreateModalBase(string name, Transform parent, string titleText, Sprite bg, TMP_FontAsset font, Vector2 size)
        {
            var modal = CreateUIObject(name, parent);
            var rect = modal.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;

            var img = modal.AddComponent<Image>();
            img.sprite = bg;
            img.type = Image.Type.Sliced;
            img.color = Color.white;

            modal.AddComponent<CanvasGroup>();

            var titleGO = CreateUIObject("Title", modal.transform);
            var tRect = titleGO.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0.1f, 0.88f);
            tRect.anchorMax = new Vector2(0.9f, 0.98f);
            tRect.offsetMin = tRect.offsetMax = Vector2.zero;
            var tmp = titleGO.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.fontSize = 24;
            tmp.color = new Color(1f, 0.9f, 0.4f, 1f);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.text = titleText;

            var closeBtnGO = CreateUIObject("BtnClose", modal.transform);
            var cbRect = closeBtnGO.GetComponent<RectTransform>();
            cbRect.anchorMin = cbRect.anchorMax = new Vector2(0.95f, 0.93f);
            cbRect.sizeDelta = new Vector2(40, 40);
            var cbImg = closeBtnGO.AddComponent<Image>();
            var closeSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/3rdParty/Space_Exploration_GUI_Kit/Picto_Icons/White/cross-64.png");
            cbImg.sprite = closeSprite;
            cbImg.color = new Color(1f, 0.6f, 0.6f, 1f);
            var cbBtn = closeBtnGO.AddComponent<Button>();
            cbBtn.targetGraphic = cbImg;
            closeBtnGO.AddComponent<UIJuice>();

            return modal;
        }

        private static GameObject CreateInputField(string name, Transform parent, string placeholder, TMP_FontAsset font, Sprite bg)
        {
            var ifGO = CreateUIObject(name, parent);
            var rect = ifGO.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(380, 50);

            var img = ifGO.AddComponent<Image>();
            img.sprite = bg;
            img.type = Image.Type.Sliced;
            img.color = new Color(0.15f, 0.2f, 0.35f, 0.95f);

            var inputField = ifGO.AddComponent<TMP_InputField>();

            var textGO = CreateUIObject("Text", ifGO.transform);
            StretchFull(textGO);
            textGO.GetComponent<RectTransform>().offsetMin = new Vector2(16, 4);
            textGO.GetComponent<RectTransform>().offsetMax = new Vector2(-16, -4);
            var textTMP = textGO.AddComponent<TextMeshProUGUI>();
            textTMP.font = font;
            textTMP.fontSize = 18;
            textTMP.color = Color.white;

            var phGO = CreateUIObject("Placeholder", ifGO.transform);
            StretchFull(phGO);
            phGO.GetComponent<RectTransform>().offsetMin = new Vector2(16, 4);
            phGO.GetComponent<RectTransform>().offsetMax = new Vector2(-16, -4);
            var phTMP = phGO.AddComponent<TextMeshProUGUI>();
            phTMP.font = font;
            phTMP.fontSize = 18;
            phTMP.color = new Color(0.6f, 0.65f, 0.75f, 0.7f);
            phTMP.text = placeholder;

            inputField.textComponent = textTMP;
            inputField.placeholder = phTMP;

            return ifGO;
        }

        private static GameObject CreateSliderRow(string name, Transform parent, string label, string defaultVal, float min, float max, float def, bool wholeNumbers, TMP_FontAsset font)
        {
            var rowGO = CreateUIObject(name, parent);
            var rect = rowGO.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(480, 42);
            var hlg = rowGO.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;

            var labelGO = CreateUIObject("Label", rowGO.transform);
            labelGO.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 36);
            var lTMP = labelGO.AddComponent<TextMeshProUGUI>();
            lTMP.font = font;
            lTMP.fontSize = 16;
            lTMP.color = Color.white;
            lTMP.text = label;

            var sliderGO = CreateUIObject("Slider", rowGO.transform);
            var sRect = sliderGO.GetComponent<RectTransform>();
            sRect.sizeDelta = new Vector2(180, 24);

            var sliderBg = sliderGO.AddComponent<Image>();
            sliderBg.color = new Color(0.12f, 0.16f, 0.28f, 0.9f);
            var slider = sliderGO.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = def;
            slider.wholeNumbers = wholeNumbers;

            var fillArea = CreateUIObject("FillArea", sliderGO.transform);
            StretchFull(fillArea);
            var fill = CreateUIObject("Fill", fillArea.transform);
            StretchFull(fill);
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.2f, 0.75f, 1f, 1f);
            slider.fillRect = fill.GetComponent<RectTransform>();

            var valLabelGO = CreateUIObject("ValueLabel", rowGO.transform);
            valLabelGO.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 36);
            var vTMP = valLabelGO.AddComponent<TextMeshProUGUI>();
            vTMP.font = font;
            vTMP.fontSize = 16;
            vTMP.color = new Color(1f, 0.9f, 0.4f, 1f);
            vTMP.alignment = TextAlignmentOptions.Right;
            vTMP.text = defaultVal;

            return rowGO;
        }

        private static GameObject CreateToggleRow(string name, Transform parent, string label, bool def, TMP_FontAsset font)
        {
            var rowGO = CreateUIObject(name, parent);
            var rect = rowGO.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(480, 38);
            var hlg = rowGO.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;

            var toggleGO = CreateUIObject("Toggle", rowGO.transform);
            toggleGO.GetComponent<RectTransform>().sizeDelta = new Vector2(32, 32);
            var tBg = toggleGO.AddComponent<Image>();
            tBg.color = new Color(0.15f, 0.2f, 0.35f, 1f);
            var toggle = toggleGO.AddComponent<Toggle>();

            var checkGO = CreateUIObject("Checkmark", toggleGO.transform);
            StretchFull(checkGO);
            checkGO.GetComponent<RectTransform>().offsetMin = new Vector2(4, 4);
            checkGO.GetComponent<RectTransform>().offsetMax = new Vector2(-4, -4);
            var checkImg = checkGO.AddComponent<Image>();
            checkImg.color = new Color(1f, 0.9f, 0.4f, 1f);
            toggle.graphic = checkImg;
            toggle.isOn = def;

            var labelGO = CreateUIObject("Label", rowGO.transform);
            labelGO.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 36);
            var lTMP = labelGO.AddComponent<TextMeshProUGUI>();
            lTMP.font = font;
            lTMP.fontSize = 16;
            lTMP.color = Color.white;
            lTMP.text = label;

            return rowGO;
        }

        private static GameObject CreateScrollView(string name, Transform parent, out Transform contentTransform)
        {
            var svGO = CreateUIObject(name, parent);
            var rect = svGO.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(580, 180);

            var sv = svGO.AddComponent<ScrollRect>();
            sv.horizontal = false;
            sv.vertical = true;

            var viewport = CreateUIObject("Viewport", svGO.transform);
            StretchFull(viewport);
            var mask = viewport.AddComponent<RectMask2D>();

            var content = CreateUIObject("Content", viewport.transform);
            var cRect = content.GetComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0, 1);
            cRect.anchorMax = new Vector2(1, 1);
            cRect.pivot = new Vector2(0.5f, 1);
            cRect.sizeDelta = new Vector2(0, 0);

            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            var csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sv.viewport = viewport.GetComponent<RectTransform>();
            sv.content = cRect;

            contentTransform = content.transform;
            return svGO;
        }

        private static GameObject CreateRoomCardPrefab(Sprite bg, TMP_FontAsset fontTitle, TMP_FontAsset fontBody)
        {
            string path = "Assets/Prefabs/UI/RoomCard.prefab";
            var go = new GameObject("RoomCard", typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(520, 56);

            var img = go.AddComponent<Image>();
            img.sprite = bg;
            img.type = Image.Type.Sliced;
            img.color = new Color(0.18f, 0.22f, 0.38f, 0.95f);

            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(16, 12, 6, 6);
            hlg.spacing = 10;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;

            var infoCol = CreateUIObject("InfoCol", go.transform);
            var icVlg = infoCol.AddComponent<VerticalLayoutGroup>();
            icVlg.childAlignment = TextAnchor.MiddleLeft;
            icVlg.childControlWidth = true;
            icVlg.childControlHeight = false;

            var titleGO = CreateUIObject("RoomName", infoCol.transform);
            var tTMP = titleGO.AddComponent<TextMeshProUGUI>();
            tTMP.font = fontTitle;
            tTMP.fontSize = 17;
            tTMP.color = Color.white;
            tTMP.text = "Agent's Arena";

            var subGO = CreateUIObject("RoomMeta", infoCol.transform);
            var sTMP = subGO.AddComponent<TextMeshProUGUI>();
            sTMP.font = fontBody;
            sTMP.fontSize = 14;
            sTMP.color = new Color(0.7f, 0.8f, 0.95f, 0.8f);
            sTMP.text = "FFA • MAX 8 PLAYERS";

            var btnJoinGO = CreateMenuButton("BtnJoin", go.transform, "JOIN", bg, fontTitle, 15, 42, Color.white);
            var le = btnJoinGO.AddComponent<LayoutElement>();
            le.preferredWidth = 100;
            le.flexibleWidth = 0;

            if (!System.IO.Directory.Exists("Assets/Prefabs/UI"))
            {
                System.IO.Directory.CreateDirectory("Assets/Prefabs/UI");
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }
    }
}
#endif
