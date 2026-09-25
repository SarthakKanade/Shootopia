using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using Shootopia;

namespace Shootopia.Editor
{
    public static class ShootopiaUIBuilder
    {
        private const string FONT_TITLE_PATH = "Assets/3rdParty/Space_Exploration_GUI_Kit/Fonts/Righteous-Regular SDF.asset";
        private const string FONT_BODY_PATH = "Assets/3rdParty/Space_Exploration_GUI_Kit/Fonts/Asap-Bold SDF.asset";

        private const string SPRITE_SETTING_CONTAINER = "Assets/3rdParty/Space_Exploration_GUI_Kit/Containers/Large/setting-container-large.png";
        private const string SPRITE_PAUSE_CONTAINER = "Assets/3rdParty/Space_Exploration_GUI_Kit/Containers/Large/pause-container-large.png";
        private const string SPRITE_VICTORY_CONTAINER = "Assets/3rdParty/Space_Exploration_GUI_Kit/Containers/Large/victory-defeat-container-large.png";
        private const string SPRITE_ITEM_CONTAINER = "Assets/3rdParty/Space_Exploration_GUI_Kit/Containers/Large/item-info-container-large.png";
        private const string SPRITE_CARD_CONTAINER = "Assets/3rdParty/Space_Exploration_GUI_Kit/Containers/Large/shop-item-container-large.png";

        private const string SPRITE_BTN_YELLOW_SPARKLE = "Assets/3rdParty/Space_Exploration_GUI_Kit/Button_Images/Source_Image_Sprites/large/large-yellow-sparkle-large.png";
        private const string SPRITE_BTN_BLUE_SPARKLE = "Assets/3rdParty/Space_Exploration_GUI_Kit/Button_Images/Source_Image_Sprites/large/large-blue-sparkle-large.png";
        private const string SPRITE_BTN_PURPLE_MED = "Assets/3rdParty/Space_Exploration_GUI_Kit/Button_Images/Source_Image_Sprites/large/medium-purple-large.png";
        private const string SPRITE_BTN_BLUE_MED = "Assets/3rdParty/Space_Exploration_GUI_Kit/Button_Images/Source_Image_Sprites/large/medium-blue-large.png";
        private const string SPRITE_BTN_PINK_SHORT = "Assets/3rdParty/Space_Exploration_GUI_Kit/Button_Images/Source_Image_Sprites/large/short-pink-large.png";
        private const string SPRITE_BTN_YELLOW_SHORT = "Assets/3rdParty/Space_Exploration_GUI_Kit/Button_Images/Source_Image_Sprites/large/short-yellow-large.png";
        private const string SPRITE_BTN_BLUE_SHORT = "Assets/3rdParty/Space_Exploration_GUI_Kit/Button_Images/Source_Image_Sprites/large/short-blue-large.png";
        private const string SPRITE_BTN_SQUARE_PURPLE = "Assets/3rdParty/Space_Exploration_GUI_Kit/Button_Images/Source_Image_Sprites/large/square-purple-large.png";
        private const string SPRITE_BTN_SQUARE_BLUE = "Assets/3rdParty/Space_Exploration_GUI_Kit/Button_Images/Source_Image_Sprites/large/square-blue-large.png";

        private const string SPRITE_BAR_BG = "Assets/3rdParty/Space_Exploration_GUI_Kit/Settings_&_Menu_Components/Large/sound-bar-container-large.png";
        private const string SPRITE_BAR_FILL = "Assets/3rdParty/Space_Exploration_GUI_Kit/Settings_&_Menu_Components/Large/level-bar-full-large.png";
        private const string SPRITE_INPUT_BG = "Assets/3rdParty/Space_Exploration_GUI_Kit/Settings_&_Menu_Components/Large/username-password-container-selected-large.png";
        private const string SPRITE_TOGGLE_ON = "Assets/3rdParty/Space_Exploration_GUI_Kit/Settings_&_Menu_Components/Large/on-selected-large.png";
        private const string SPRITE_TOGGLE_OFF = "Assets/3rdParty/Space_Exploration_GUI_Kit/Settings_&_Menu_Components/Large/off-deselected-large.png";

        private const string ICON_ASTRONAUT = "Assets/3rdParty/Space_Exploration_GUI_Kit/Icons/astronaut-128.png";
        private const string ICON_ALIEN = "Assets/3rdParty/Space_Exploration_GUI_Kit/Icons/alien-128.png";
        private const string ICON_CROWN = "Assets/3rdParty/Space_Exploration_GUI_Kit/Icons/crown-gold-128.png";
        private const string ICON_HEART = "Assets/3rdParty/Space_Exploration_GUI_Kit/Icons/heart-128.png";
        private const string ICON_GUN = "Assets/3rdParty/Space_Exploration_GUI_Kit/Icons/gun-128.png";
        private const string ICON_RADAR = "Assets/3rdParty/Space_Exploration_GUI_Kit/Icons/radar-128.png";
        private const string ICON_SOUND = "Assets/3rdParty/Space_Exploration_GUI_Kit/Icons/tv-128.png";
        private const string ICON_STAR = "Assets/3rdParty/Space_Exploration_GUI_Kit/Icons/star-128.png";
        private const string BG_HOME = "Assets/3rdParty/Space_Exploration_GUI_Kit/Background_Images/large/home-background-large.png";

        private static TMP_FontAsset TitleFont => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_TITLE_PATH);
        private static TMP_FontAsset BodyFont => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_BODY_PATH);

        private static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        [MenuItem("Shootopia/Build All UI")]
        public static void BuildAllUI()
        {
            EnsurePrefabsFolder();
            var roomCardPrefab = BuildRoomCardPrefab();
            var playerCardPrefab = BuildPlayerCardPrefab();
            var scoreRowPrefab = BuildScoreRowPrefab();

            BuildMainMenuScene(roomCardPrefab);
            BuildWaitingRoomScene(playerCardPrefab);
            BuildMap1Scene(scoreRowPrefab);
            BuildBootstrapScene();

            Debug.Log("<color=green>[ShootopiaUIBuilder] Successfully built and migrated ALL UI across all scenes to UGUI!</color>");
        }

        private static void EnsurePrefabsFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI"))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
            }
        }

        // ──────────────────────── PREFAB BUILDERS ────────────────────────

        public static GameObject BuildRoomCardPrefab()
        {
            string path = "Assets/Prefabs/UI/RoomCard.prefab";
            var root = new GameObject("RoomCard", typeof(RectTransform), typeof(Image), typeof(UIJuice));
            var rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(580, 70);

            var bg = root.GetComponent<Image>();
            bg.sprite = LoadSprite(SPRITE_CARD_CONTAINER);
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.9f, 0.9f, 1f, 0.95f);

            // Layout
            var hlg = root.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(20, 20, 10, 10);
            hlg.spacing = 15;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            // Name + Info Column
            var col = CreateEmpty("InfoCol", root.transform);
            var colRt = col.GetComponent<RectTransform>();
            colRt.sizeDelta = new Vector2(360, 50);
            var vlg = col.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.spacing = 2;

            var nameTxt = CreateTMPText("RoomName", col.transform, "Sector Alpha", 20, Color.white, TitleFont, TextAlignmentOptions.MidlineLeft);
            var infoTxt = CreateTMPText("RoomInfo", col.transform, "FFA • 3/8 PLAYERS", 14, new Color(0.7f, 0.85f, 1f), BodyFont, TextAlignmentOptions.MidlineLeft);

            // Join Button
            var joinBtn = CreateButton("BtnJoin", root.transform, SPRITE_BTN_YELLOW_SHORT, "JOIN", 18, new Vector2(120, 46));

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            GameObject.DestroyImmediate(root);
            return prefab;
        }

        public static GameObject BuildPlayerCardPrefab()
        {
            string path = "Assets/Prefabs/UI/PlayerLobbyCard.prefab";
            var root = new GameObject("PlayerLobbyCard", typeof(RectTransform), typeof(Image), typeof(UIJuice));
            var rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(280, 80);

            var bg = root.GetComponent<Image>();
            bg.sprite = LoadSprite(SPRITE_CARD_CONTAINER);
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.85f, 0.88f, 0.98f, 0.95f);

            var hlg = root.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(15, 15, 10, 10);
            hlg.spacing = 12;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            // Avatar Icon
            var avatar = CreateImage("AvatarIcon", root.transform, ICON_ASTRONAUT, new Vector2(50, 50));

            // Info Column
            var col = CreateEmpty("InfoCol", root.transform);
            var colRt = col.GetComponent<RectTransform>();
            colRt.sizeDelta = new Vector2(130, 50);
            var vlg = col.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.spacing = 2;

            var nameTxt = CreateTMPText("PlayerName", col.transform, "Agent Alpha", 18, Color.white, TitleFont, TextAlignmentOptions.MidlineLeft);
            var statusTxt = CreateTMPText("StatusText", col.transform, "READY", 14, new Color(0.3f, 0.95f, 0.5f), BodyFont, TextAlignmentOptions.MidlineLeft);

            // Crown / Ready Badges
            var crown = CreateImage("CrownIcon", root.transform, ICON_CROWN, new Vector2(32, 32));
            var readyBadge = CreateImage("ReadyBadge", root.transform, ICON_STAR, new Vector2(28, 28));

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            GameObject.DestroyImmediate(root);
            return prefab;
        }

        public static GameObject BuildScoreRowPrefab()
        {
            string path = "Assets/Prefabs/UI/ScoreboardRow.prefab";
            var root = new GameObject("ScoreboardRow", typeof(RectTransform), typeof(Image));
            var rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(700, 44);

            var bg = root.GetComponent<Image>();
            bg.sprite = LoadSprite(SPRITE_BAR_BG);
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.15f, 0.18f, 0.25f, 0.85f);

            var hlg = root.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(20, 20, 5, 5);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;

            var rankTxt = CreateTMPText("Rank", root.transform, "#1", 18, new Color(1f, 0.85f, 0.3f), TitleFont, TextAlignmentOptions.MidlineLeft);
            rankTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(60, 34);

            var nameTxt = CreateTMPText("Name", root.transform, "Player Name", 18, Color.white, BodyFont, TextAlignmentOptions.MidlineLeft);
            nameTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(280, 34);

            var kdTxt = CreateTMPText("KD", root.transform, "12 K / 3 D", 16, new Color(0.8f, 0.85f, 0.95f), BodyFont, TextAlignmentOptions.Center);
            kdTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(180, 34);

            var scoreTxt = CreateTMPText("Score", root.transform, "1200 PTS", 18, new Color(0.3f, 0.95f, 0.6f), TitleFont, TextAlignmentOptions.MidlineRight);
            scoreTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(140, 34);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            GameObject.DestroyImmediate(root);
            return prefab;
        }

        // ──────────────────────── SCENE BUILDERS ────────────────────────

        public static void BuildMainMenuScene(GameObject roomCardPrefab)
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);

            // 1. Remove old UIDocument UI objects
            var oldUI = GameObject.Find("MainMenuUI");
            if (oldUI != null) GameObject.DestroyImmediate(oldUI);

            var oldLoading = GameObject.Find("LoadingScreenOverlay");
            if (oldLoading != null) GameObject.DestroyImmediate(oldLoading);

            // 2. Setup 3D Character Stage & Main Camera
            var stageObj = GameObject.Find("AvatarStage");
            if (stageObj == null)
            {
                stageObj = new GameObject("AvatarStage");
            }
            stageObj.transform.position = Vector3.zero;
            stageObj.transform.rotation = Quaternion.identity;

            var stageController = stageObj.GetComponent<AvatarStageController>();
            if (stageController == null) stageController = stageObj.AddComponent<AvatarStageController>();

            // Setup Camera
            var mainCamObj = GameObject.FindWithTag("MainCamera");
            if (mainCamObj == null) mainCamObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            mainCamObj.transform.position = new Vector3(0f, 0.85f, -3.4f);
            mainCamObj.transform.eulerAngles = new Vector3(2.5f, 0f, 0f);
            var cam = mainCamObj.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.05f, 0.09f, 1f);
            cam.fieldOfView = 38f;

            // Remove any old 3D backdrop
            var oldBackdrop = GameObject.Find("SpaceBackdrop3D");
            if (oldBackdrop != null) GameObject.DestroyImmediate(oldBackdrop);

            // Avatar Holder & Pedestal
            var holder = stageObj.transform.Find("AvatarHolder");
            if (holder == null)
            {
                var h = new GameObject("AvatarHolder");
                h.transform.SetParent(stageObj.transform, false);
                holder = h.transform;
            }
            holder.localPosition = Vector3.zero;
            holder.localEulerAngles = new Vector3(0f, 180f, 0f); // Standing straight, facing camera

            var pedestal = stageObj.transform.Find("StagePedestal");
            if (pedestal == null)
            {
                var p = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                p.name = "StagePedestal";
                p.transform.SetParent(stageObj.transform, false);
                pedestal = p.transform;
            }
            pedestal.localPosition = new Vector3(0f, -0.02f, 0f);
            pedestal.localScale = new Vector3(1.6f, 0.04f, 1.6f);

            var lightObj = stageObj.transform.Find("StageLight");
            if (lightObj == null)
            {
                var l = new GameObject("StageLight", typeof(Light));
                l.transform.SetParent(stageObj.transform, false);
                lightObj = l.transform;
            }
            lightObj.localPosition = new Vector3(0.8f, 3.2f, -2.2f);
            lightObj.localEulerAngles = new Vector3(45f, -20f, 0f);
            var stageLight = lightObj.GetComponent<Light>();
            stageLight.type = LightType.Directional;
            stageLight.intensity = 2.2f;

            // Wire AvatarStageController fields via SerializedObject
            var stageSo = new SerializedObject(stageController);
            stageSo.FindProperty("stageCamera").objectReferenceValue = cam;
            stageSo.FindProperty("avatarHolder").objectReferenceValue = holder;
            stageSo.FindProperty("stagePedestal").objectReferenceValue = pedestal;
            stageSo.FindProperty("stageLight").objectReferenceValue = stageLight;
            stageSo.FindProperty("defaultAvatarPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PlayerModel/Player.prefab");
            stageSo.FindProperty("robotAvatarPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Robot.prefab");
            stageSo.FindProperty("menuIdleAnimator").objectReferenceValue = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animators/MenuAvatar_Idle.controller");
            stageSo.ApplyModifiedProperties();

            // 3. Build UGUI MainMenuCanvas
            var canvasObj = new GameObject("MainMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(MainMenuController));
            var canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var menuCtrl = canvasObj.GetComponent<MainMenuController>();
            var menuSo = new SerializedObject(menuCtrl);

            // Top Header Bar
            var topBar = CreateEmpty("TopHeaderBar", canvasObj.transform);
            SetStretch(topBar.GetComponent<RectTransform>(), 0, 1, 1, 1, new Vector2(0, -90), new Vector2(0, 0));

            // Profile Pill (Left)
            var profilePill = CreateImage("ProfilePill", topBar.transform, SPRITE_CARD_CONTAINER, new Vector2(300, 60));
            profilePill.GetComponent<RectTransform>().anchoredPosition = new Vector2(180, -45);
            var profileAvatar = CreateImage("AvatarIcon", profilePill.transform, ICON_ASTRONAUT, new Vector2(46, 46));
            profileAvatar.GetComponent<RectTransform>().anchoredPosition = new Vector2(-115, 0);
            var profileName = CreateTMPText("PlayerName", profilePill.transform, "Player", 20, Color.white, TitleFont, TextAlignmentOptions.MidlineLeft);
            profileName.GetComponent<RectTransform>().anchoredPosition = new Vector2(10, 0);
            profileName.GetComponent<RectTransform>().sizeDelta = new Vector2(170, 40);
            var btnEditProf = CreateButton("BtnEdit", profilePill.transform, SPRITE_BTN_SQUARE_BLUE, "✎", 16, new Vector2(36, 36));
            btnEditProf.GetComponent<RectTransform>().anchoredPosition = new Vector2(120, 0);

            // Right Tools (Sound + Settings)
            var btnSound = CreateButton("BtnToggleSound", topBar.transform, SPRITE_BTN_SQUARE_PURPLE, "♪", 20, new Vector2(50, 50));
            btnSound.GetComponent<RectTransform>().anchoredPosition = new Vector2(-130, -45);
            var btnTopSet = CreateButton("BtnTopSettings", topBar.transform, SPRITE_BTN_SQUARE_BLUE, "⚙", 20, new Vector2(50, 50));
            btnTopSet.GetComponent<RectTransform>().anchoredPosition = new Vector2(-65, -45);

            // Main Action Menu (Right-Side Hub)
            var actionHub = CreateEmpty("ActionHub", canvasObj.transform);
            var hubRt = actionHub.GetComponent<RectTransform>();
            hubRt.anchorMin = new Vector2(1, 0.5f);
            hubRt.anchorMax = new Vector2(1, 0.5f);
            hubRt.pivot = new Vector2(1, 0.5f);
            hubRt.anchoredPosition = new Vector2(-60, -30);
            hubRt.sizeDelta = new Vector2(340, 460);

            var vlgHub = actionHub.AddComponent<VerticalLayoutGroup>();
            vlgHub.spacing = 16;
            vlgHub.childAlignment = TextAnchor.MiddleCenter;
            vlgHub.childControlWidth = true;
            vlgHub.childControlHeight = false;

            var btnPlay = CreateButton("BtnPlayMatch", actionHub.transform, SPRITE_BTN_YELLOW_SPARKLE, "PLAY MATCH", 28, new Vector2(320, 95));
            var btnRooms = CreateButton("BtnCustomRooms", actionHub.transform, SPRITE_BTN_BLUE_SPARKLE, "CUSTOM ROOMS", 22, new Vector2(320, 80));
            var btnCust = CreateButton("BtnCustomize", actionHub.transform, SPRITE_BTN_PURPLE_MED, "CUSTOMIZE", 20, new Vector2(320, 68));
            var btnSettings = CreateButton("BtnSettings", actionHub.transform, SPRITE_BTN_BLUE_MED, "SETTINGS", 20, new Vector2(320, 68));
            var btnQuit = CreateButton("BtnQuit", actionHub.transform, SPRITE_BTN_PINK_SHORT, "QUIT", 18, new Vector2(320, 58));

            // Modals:
            // 1. Quick Play Filter Modal
            var qfModal = CreateModal("Modal_QuickFilter", canvasObj.transform, SPRITE_PAUSE_CONTAINER, new Vector2(520, 400), "SELECT GAME MODE");
            var qfContent = qfModal.transform.Find("Content");
            var qfChipAll = CreateButton("ChipAll", qfContent, SPRITE_BTN_BLUE_SHORT, "ALL MODES", 18, new Vector2(380, 55));
            var qfChipFFA = CreateButton("ChipFFA", qfContent, SPRITE_BTN_YELLOW_SHORT, "FREE FOR ALL", 18, new Vector2(380, 55));
            var qfChipTDM = CreateButton("ChipTDM", qfContent, SPRITE_BTN_PURPLE_MED, "TEAM DEATHMATCH", 18, new Vector2(380, 55));
            var qfConfirm = CreateButton("BtnConfirm", qfModal.transform, SPRITE_BTN_YELLOW_SPARKLE, "FIND MATCH", 22, new Vector2(260, 68));
            qfConfirm.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -145);
            var qfClose = CreateButton("BtnClose", qfModal.transform, SPRITE_BTN_SQUARE_PURPLE, "✕", 20, new Vector2(44, 44));
            qfClose.GetComponent<RectTransform>().anchoredPosition = new Vector2(230, 170);

            // 2. Create Room Modal
            var crModal = CreateModal("Modal_CreateRoom", canvasObj.transform, SPRITE_SETTING_CONTAINER, new Vector2(600, 540), "CREATE CUSTOM ROOM");
            var crContent = crModal.transform.Find("Content");
            var crInput = CreateInputField("InputRoomName", crContent, "Enter room name...", new Vector2(440, 55));
            var crChipFFA = CreateButton("ChipFFA", crContent, SPRITE_BTN_YELLOW_SHORT, "FREE FOR ALL", 18, new Vector2(210, 50));
            var crChipTDM = CreateButton("ChipTDM", crContent, SPRITE_BTN_PURPLE_MED, "TEAM DEATHMATCH", 18, new Vector2(210, 50));
            var crSlider = CreateSlider("SliderMaxPlayers", crContent, 2, 12, 8, new Vector2(440, 40));
            var crCountLabel = CreateTMPText("LabelMaxPlayers", crContent, "8 PLAYERS", 18, Color.white, BodyFont, TextAlignmentOptions.Center);
            var crToggle = CreateToggle("TogglePublic", crContent, "PUBLIC ROOM", new Vector2(300, 40));
            var crConfirm = CreateButton("BtnConfirmCreate", crModal.transform, SPRITE_BTN_YELLOW_SPARKLE, "CREATE ROOM", 22, new Vector2(280, 68));
            crConfirm.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -215);
            var crClose = CreateButton("BtnClose", crModal.transform, SPRITE_BTN_SQUARE_PURPLE, "✕", 20, new Vector2(44, 44));
            crClose.GetComponent<RectTransform>().anchoredPosition = new Vector2(270, 240);

            // 3. Join / Browser Modal
            var jrModal = CreateModal("Modal_JoinRoom", canvasObj.transform, SPRITE_SETTING_CONTAINER, new Vector2(760, 620), "CUSTOM ROOMS");
            var jrClose = CreateButton("BtnClose", jrModal.transform, SPRITE_BTN_SQUARE_PURPLE, "✕", 20, new Vector2(44, 44));
            jrClose.GetComponent<RectTransform>().anchoredPosition = new Vector2(350, 280);

            // Tabs
            var tabBrowse = CreateButton("TabBrowse", jrModal.transform, SPRITE_BTN_BLUE_SHORT, "BROWSE ROOMS", 18, new Vector2(220, 48));
            tabBrowse.GetComponent<RectTransform>().anchoredPosition = new Vector2(-120, 230);
            var tabCode = CreateButton("TabCode", jrModal.transform, SPRITE_BTN_PURPLE_MED, "JOIN VIA CODE", 18, new Vector2(220, 48));
            tabCode.GetComponent<RectTransform>().anchoredPosition = new Vector2(120, 230);

            var browseContent = CreateEmpty("TabContent_Browse", jrModal.transform);
            SetStretch(browseContent.GetComponent<RectTransform>(), 0.05f, 0.08f, 0.95f, 0.8f, Vector2.zero, Vector2.zero);

            var scroll = CreateScrollView("RoomScrollView", browseContent.transform, new Vector2(680, 360));
            var roomListCont = scroll.transform.Find("Viewport/Content");
            var vlgRoom = roomListCont.gameObject.AddComponent<VerticalLayoutGroup>();
            vlgRoom.spacing = 10;
            vlgRoom.childControlWidth = true;
            vlgRoom.childControlHeight = false;

            var emptyState = CreateEmpty("EmptyState", browseContent.transform);
            CreateTMPText("EmptyText", emptyState.transform, "NO ACTIVE ROOMS FOUND", 20, Color.gray, TitleFont, TextAlignmentOptions.Center);
            var btnEmptyCr = CreateButton("BtnEmptyCreate", emptyState.transform, SPRITE_BTN_YELLOW_SHORT, "CREATE A ROOM", 18, new Vector2(220, 50));
            btnEmptyCr.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -50);

            var btnRefresh = CreateButton("BtnRefresh", browseContent.transform, SPRITE_BTN_SQUARE_BLUE, "↻", 22, new Vector2(46, 46));
            btnRefresh.GetComponent<RectTransform>().anchoredPosition = new Vector2(300, 160);

            // Code Content
            var codeContent = CreateEmpty("TabContent_Code", jrModal.transform);
            SetStretch(codeContent.GetComponent<RectTransform>(), 0.1f, 0.1f, 0.9f, 0.8f, Vector2.zero, Vector2.zero);
            codeContent.SetActive(false);
            var codePrompt = CreateTMPText("CodePrompt", codeContent.transform, "ENTER 6-CHARACTER ROOM CODE", 20, Color.white, TitleFont, TextAlignmentOptions.Center);
            codePrompt.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 50);
            var inputCode = CreateInputField("InputJoinCode", codeContent.transform, "ABCXYZ", new Vector2(340, 60));
            inputCode.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -10);
            var btnSubmitCode = CreateButton("BtnSubmitCode", codeContent.transform, SPRITE_BTN_YELLOW_SPARKLE, "ENTER ARENA", 20, new Vector2(260, 64));
            btnSubmitCode.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -90);

            // 4. Customize Modal
            var custModal = CreateModal("Modal_Customize", canvasObj.transform, SPRITE_ITEM_CONTAINER, new Vector2(580, 440), "CHARACTER SKINS");
            var custContent = custModal.transform.Find("Content");
            var btnSkinAgent = CreateButton("BtnSkinAgent", custContent, SPRITE_BTN_BLUE_MED, "AGENT ALPHA", 20, new Vector2(360, 65));
            var btnSkinRobot = CreateButton("BtnSkinRobot", custContent, SPRITE_BTN_PURPLE_MED, "COMBAT DROID", 20, new Vector2(360, 65));
            var custClose = CreateButton("BtnClose", custModal.transform, SPRITE_BTN_SQUARE_PURPLE, "✕", 20, new Vector2(44, 44));
            custClose.GetComponent<RectTransform>().anchoredPosition = new Vector2(260, 190);

            // 5. Settings Modal
            var setModal = CreateModal("Modal_Settings", canvasObj.transform, SPRITE_SETTING_CONTAINER, new Vector2(620, 600), "SETTINGS");
            var setContent = setModal.transform.Find("Content");
            var sMaster = CreateSliderWithLabel("SliderMaster", setContent, "MASTER VOLUME", out var lblMaster);
            var sMusic = CreateSliderWithLabel("SliderMusic", setContent, "MUSIC VOLUME", out var lblMusic);
            var sSFX = CreateSliderWithLabel("SliderSFX", setContent, "SFX VOLUME", out var lblSFX);
            var sSens = CreateSliderWithLabel("SliderSens", setContent, "MOUSE SENSITIVITY", out var lblSens, 0.2f, 3.0f, 1.0f);
            var tInvert = CreateToggle("ToggleInvertY", setContent, "INVERT Y-AXIS", new Vector2(300, 36));
            var tFull = CreateToggle("ToggleFullscreen", setContent, "FULLSCREEN", new Vector2(300, 36));
            var setClose = CreateButton("BtnClose", setModal.transform, SPRITE_BTN_SQUARE_PURPLE, "✕", 20, new Vector2(44, 44));
            setClose.GetComponent<RectTransform>().anchoredPosition = new Vector2(280, 270);

            // 6. Edit Profile Modal
            var profModal = CreateModal("Modal_EditProfile", canvasObj.transform, SPRITE_PAUSE_CONTAINER, new Vector2(500, 340), "EDIT PROFILE");
            var profContent = profModal.transform.Find("Content");
            var profInput = CreateInputField("InputDisplayName", profContent, "Enter your callsign...", new Vector2(380, 55));
            var profSave = CreateButton("BtnSaveProfile", profModal.transform, SPRITE_BTN_YELLOW_SPARKLE, "SAVE CALLSIGN", 20, new Vector2(240, 60));
            profSave.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -110);
            var profClose = CreateButton("BtnClose", profModal.transform, SPRITE_BTN_SQUARE_PURPLE, "✕", 20, new Vector2(44, 44));
            profClose.GetComponent<RectTransform>().anchoredPosition = new Vector2(220, 140);

            // 7. Busy Overlay
            var busy = CreateEmpty("BusyOverlay", canvasObj.transform);
            SetStretch(busy.GetComponent<RectTransform>(), 0, 0, 1, 1, Vector2.zero, Vector2.zero);
            var busyBg = busy.AddComponent<Image>();
            busyBg.color = new Color(0.02f, 0.02f, 0.05f, 0.85f);
            var busySpinner = CreateImage("Spinner", busy.transform, ICON_RADAR, new Vector2(80, 80));
            busySpinner.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 40);
            var busyTxt = CreateTMPText("BusyLabel", busy.transform, "WARPING TO ARENA...", 22, Color.white, TitleFont, TextAlignmentOptions.Center);
            busyTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -30);
            var btnCancelBusy = CreateButton("BtnCancel", busy.transform, SPRITE_BTN_PINK_SHORT, "CANCEL", 18, new Vector2(180, 48));
            btnCancelBusy.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -100);
            busy.SetActive(false);

            // 8. Error Modal
            var errModal = CreateModal("Modal_Error", canvasObj.transform, SPRITE_PAUSE_CONTAINER, new Vector2(500, 300), "SYSTEM NOTICE");
            var errTxt = CreateTMPText("ErrorText", errModal.transform, "Connection Error", 18, new Color(1f, 0.4f, 0.4f), BodyFont, TextAlignmentOptions.Center);
            errTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 15);
            errTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(420, 80);
            var errDismiss = CreateButton("BtnDismiss", errModal.transform, SPRITE_BTN_BLUE_SHORT, "OK", 18, new Vector2(160, 48));
            errDismiss.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -90);
            errModal.SetActive(false);

            // Wire all MainMenuController serialized properties
            menuSo.FindProperty("playerNameLabel").objectReferenceValue = profileName;
            menuSo.FindProperty("btnEditProfile").objectReferenceValue = btnEditProf.GetComponent<Button>();
            menuSo.FindProperty("btnToggleSound").objectReferenceValue = btnSound.GetComponent<Button>();
            menuSo.FindProperty("btnTopSettings").objectReferenceValue = btnTopSet.GetComponent<Button>();

            menuSo.FindProperty("btnPlayMatch").objectReferenceValue = btnPlay.GetComponent<Button>();
            menuSo.FindProperty("btnCustomRooms").objectReferenceValue = btnRooms.GetComponent<Button>();
            menuSo.FindProperty("btnCustomize").objectReferenceValue = btnCust.GetComponent<Button>();
            menuSo.FindProperty("btnSettings").objectReferenceValue = btnSettings.GetComponent<Button>();
            menuSo.FindProperty("btnQuit").objectReferenceValue = btnQuit.GetComponent<Button>();

            // Quick filter
            menuSo.FindProperty("modalQuickFilter").objectReferenceValue = qfModal;
            menuSo.FindProperty("quickFilterGroup").objectReferenceValue = qfModal.GetComponent<CanvasGroup>();
            menuSo.FindProperty("btnCloseQuickFilter").objectReferenceValue = qfClose.GetComponent<Button>();
            menuSo.FindProperty("chipQuickAll").objectReferenceValue = qfChipAll.GetComponent<Button>();
            menuSo.FindProperty("chipQuickFFA").objectReferenceValue = qfChipFFA.GetComponent<Button>();
            menuSo.FindProperty("chipQuickTDM").objectReferenceValue = qfChipTDM.GetComponent<Button>();
            menuSo.FindProperty("btnConfirmQuickPlay").objectReferenceValue = qfConfirm.GetComponent<Button>();

            // Create room
            menuSo.FindProperty("modalCreateRoom").objectReferenceValue = crModal;
            menuSo.FindProperty("createRoomGroup").objectReferenceValue = crModal.GetComponent<CanvasGroup>();
            menuSo.FindProperty("btnCloseCreateRoom").objectReferenceValue = crClose.GetComponent<Button>();
            menuSo.FindProperty("inputCreateRoomName").objectReferenceValue = crInput.GetComponent<TMP_InputField>();
            menuSo.FindProperty("chipModeFFA").objectReferenceValue = crChipFFA.GetComponent<Button>();
            menuSo.FindProperty("chipModeTDM").objectReferenceValue = crChipTDM.GetComponent<Button>();
            menuSo.FindProperty("sliderMaxPlayers").objectReferenceValue = crSlider.GetComponent<Slider>();
            menuSo.FindProperty("labelMaxPlayers").objectReferenceValue = crCountLabel;
            menuSo.FindProperty("togglePublicRoom").objectReferenceValue = crToggle.GetComponent<Toggle>();
            menuSo.FindProperty("btnConfirmCreate").objectReferenceValue = crConfirm.GetComponent<Button>();

            // Join room
            menuSo.FindProperty("modalJoinRoom").objectReferenceValue = jrModal;
            menuSo.FindProperty("joinRoomGroup").objectReferenceValue = jrModal.GetComponent<CanvasGroup>();
            menuSo.FindProperty("btnCloseJoinRoom").objectReferenceValue = jrClose.GetComponent<Button>();
            menuSo.FindProperty("tabBtnBrowse").objectReferenceValue = tabBrowse.GetComponent<Button>();
            menuSo.FindProperty("tabBtnCode").objectReferenceValue = tabCode.GetComponent<Button>();
            menuSo.FindProperty("tabContentBrowse").objectReferenceValue = browseContent;
            menuSo.FindProperty("tabContentCode").objectReferenceValue = codeContent;
            menuSo.FindProperty("btnRefreshRooms").objectReferenceValue = btnRefresh.GetComponent<Button>();
            menuSo.FindProperty("roomListContent").objectReferenceValue = roomListCont;
            menuSo.FindProperty("roomCardPrefab").objectReferenceValue = roomCardPrefab;
            menuSo.FindProperty("browseEmptyState").objectReferenceValue = emptyState;
            menuSo.FindProperty("btnEmptyCreate").objectReferenceValue = btnEmptyCr.GetComponent<Button>();
            menuSo.FindProperty("inputJoinCode").objectReferenceValue = inputCode.GetComponent<TMP_InputField>();
            menuSo.FindProperty("btnSubmitJoinCode").objectReferenceValue = btnSubmitCode.GetComponent<Button>();

            // Customize
            menuSo.FindProperty("modalCustomize").objectReferenceValue = custModal;
            menuSo.FindProperty("customizeGroup").objectReferenceValue = custModal.GetComponent<CanvasGroup>();
            menuSo.FindProperty("btnCloseCustomize").objectReferenceValue = custClose.GetComponent<Button>();
            menuSo.FindProperty("btnSkinAgent").objectReferenceValue = btnSkinAgent.GetComponent<Button>();
            menuSo.FindProperty("btnSkinRobot").objectReferenceValue = btnSkinRobot.GetComponent<Button>();

            // Settings
            menuSo.FindProperty("modalSettings").objectReferenceValue = setModal;
            menuSo.FindProperty("settingsGroup").objectReferenceValue = setModal.GetComponent<CanvasGroup>();
            menuSo.FindProperty("btnCloseSettings").objectReferenceValue = setClose.GetComponent<Button>();
            menuSo.FindProperty("sliderMasterVol").objectReferenceValue = sMaster.GetComponent<Slider>();
            menuSo.FindProperty("sliderMusicVol").objectReferenceValue = sMusic.GetComponent<Slider>();
            menuSo.FindProperty("sliderSFXVol").objectReferenceValue = sSFX.GetComponent<Slider>();
            menuSo.FindProperty("sliderMouseSens").objectReferenceValue = sSens.GetComponent<Slider>();
            menuSo.FindProperty("labelMasterVol").objectReferenceValue = lblMaster;
            menuSo.FindProperty("labelMusicVol").objectReferenceValue = lblMusic;
            menuSo.FindProperty("labelSFXVol").objectReferenceValue = lblSFX;
            menuSo.FindProperty("labelMouseSens").objectReferenceValue = lblSens;
            menuSo.FindProperty("toggleInvertY").objectReferenceValue = tInvert.GetComponent<Toggle>();
            menuSo.FindProperty("toggleFullscreen").objectReferenceValue = tFull.GetComponent<Toggle>();

            // Edit Profile
            menuSo.FindProperty("modalEditProfile").objectReferenceValue = profModal;
            menuSo.FindProperty("editProfileGroup").objectReferenceValue = profModal.GetComponent<CanvasGroup>();
            menuSo.FindProperty("btnCloseProfile").objectReferenceValue = profClose.GetComponent<Button>();
            menuSo.FindProperty("inputDisplayName").objectReferenceValue = profInput.GetComponent<TMP_InputField>();
            menuSo.FindProperty("btnSaveProfile").objectReferenceValue = profSave.GetComponent<Button>();

            // Busy & Error
            menuSo.FindProperty("busyOverlay").objectReferenceValue = busy;
            menuSo.FindProperty("busyGroup").objectReferenceValue = busy.GetComponent<CanvasGroup>() ?? busy.AddComponent<CanvasGroup>();
            menuSo.FindProperty("busyLabel").objectReferenceValue = busyTxt;
            menuSo.FindProperty("btnCancelBusy").objectReferenceValue = btnCancelBusy.GetComponent<Button>();
            menuSo.FindProperty("spinnerIcon").objectReferenceValue = busySpinner.transform;
            menuSo.FindProperty("errorModal").objectReferenceValue = errModal;
            menuSo.FindProperty("errorGroup").objectReferenceValue = errModal.GetComponent<CanvasGroup>();
            menuSo.FindProperty("errorLabel").objectReferenceValue = errTxt;
            menuSo.FindProperty("btnDismissError").objectReferenceValue = errDismiss.GetComponent<Button>();

            menuSo.ApplyModifiedProperties();

            // Disable UI canvas to keep only the character model on screen
            canvasObj.SetActive(false);

            // 4. Build Loading Screen Overlay (DontDestroyOnLoad)
            BuildLoadingScreenPrefabInScene();

            // Save Scene
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ShootopiaUIBuilder] MainMenu.unity UGUI canvas successfully built and saved!");
        }

        public static void BuildWaitingRoomScene(GameObject playerCardPrefab)
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/WaitingRoom.unity", OpenSceneMode.Single);

            var oldUI = GameObject.Find("WaitingRoomUI");
            if (oldUI != null) GameObject.DestroyImmediate(oldUI);

            var canvasObj = new GameObject("WaitingRoomCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(WaitingRoomController));
            var canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var waitCtrl = canvasObj.GetComponent<WaitingRoomController>();
            var waitSo = new SerializedObject(waitCtrl);

            // Modal Card in Center
            var card = CreateModal("LobbyCard", canvasObj.transform, SPRITE_SETTING_CONTAINER, new Vector2(900, 680), "LOBBY");
            var cardRt = card.GetComponent<RectTransform>();

            var roleBadge = CreateTMPText("RoleBadge", card.transform, "HOST", 16, new Color(1f, 0.85f, 0.2f), TitleFont, TextAlignmentOptions.MidlineLeft);
            roleBadge.GetComponent<RectTransform>().anchoredPosition = new Vector2(-360, 280);

            var btnLeaveTop = CreateButton("BtnLeaveTop", card.transform, SPRITE_BTN_SQUARE_PURPLE, "✕", 18, new Vector2(44, 44));
            btnLeaveTop.GetComponent<RectTransform>().anchoredPosition = new Vector2(410, 300);

            var roomName = CreateTMPText("RoomName", card.transform, "Sector Alpha Arena", 26, Color.white, TitleFont, TextAlignmentOptions.Center);
            roomName.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 240);

            var roomModeMap = CreateTMPText("RoomModeMap", card.transform, "MODE: FREE FOR ALL  |  MAP: SECTOR ALPHA", 16, new Color(0.7f, 0.85f, 1f), BodyFont, TextAlignmentOptions.Center);
            roomModeMap.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 205);

            // Room Code Pill
            var codePill = CreateImage("CodePill", card.transform, SPRITE_CARD_CONTAINER, new Vector2(300, 50));
            codePill.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 155);
            var codeTxt = CreateTMPText("RoomCode", codePill.transform, "ABCXYZ", 22, new Color(1f, 0.9f, 0.3f), TitleFont, TextAlignmentOptions.Center);
            codeTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(-40, 0);
            var btnCopy = CreateButton("BtnCopyCode", codePill.transform, SPRITE_BTN_BLUE_SHORT, "COPY CODE", 14, new Vector2(110, 36));
            btnCopy.GetComponent<RectTransform>().anchoredPosition = new Vector2(85, 0);
            var btnCopyLbl = btnCopy.GetComponentInChildren<TextMeshProUGUI>();

            // Player Roster Header & Scroll
            var pCount = CreateTMPText("PlayerCount", card.transform, "3/8 PLAYERS", 16, Color.white, BodyFont, TextAlignmentOptions.MidlineLeft);
            pCount.GetComponent<RectTransform>().anchoredPosition = new Vector2(-280, 105);
            var rosterSummary = CreateTMPText("RosterSummary", card.transform, "3/3 READY TO DEPLOY", 16, new Color(0.3f, 0.95f, 0.5f), BodyFont, TextAlignmentOptions.MidlineRight);
            rosterSummary.GetComponent<RectTransform>().anchoredPosition = new Vector2(280, 105);

            var scroll = CreateScrollView("RosterScrollView", card.transform, new Vector2(820, 250));
            scroll.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -35);
            var rosterContent = scroll.transform.Find("Viewport/Content");
            var glg = rosterContent.gameObject.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(260, 75);
            glg.spacing = new Vector2(10, 10);
            glg.childAlignment = TextAnchor.UpperLeft;

            // Host Controls
            var hostGroup = CreateEmpty("HostControls", card.transform);
            hostGroup.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -190);
            var toggleVis = CreateToggle("TogglePublic", hostGroup.transform, "PUBLIC ROOM", new Vector2(260, 36));

            // Bottom Buttons
            var btnStart = CreateButton("BtnStartMatch", card.transform, SPRITE_BTN_YELLOW_SPARKLE, "LAUNCH MATCH", 22, new Vector2(280, 68));
            btnStart.GetComponent<RectTransform>().anchoredPosition = new Vector2(180, -260);

            var btnReady = CreateButton("BtnReady", card.transform, SPRITE_BTN_YELLOW_SPARKLE, "READY", 22, new Vector2(280, 68));
            btnReady.GetComponent<RectTransform>().anchoredPosition = new Vector2(180, -260);
            var btnReadyLbl = btnReady.GetComponentInChildren<TextMeshProUGUI>();

            var btnLeave = CreateButton("BtnLeaveLobby", card.transform, SPRITE_BTN_PINK_SHORT, "LEAVE LOBBY", 18, new Vector2(200, 56));
            btnLeave.GetComponent<RectTransform>().anchoredPosition = new Vector2(-220, -260);

            // Wire WaitingRoomController fields
            waitSo.FindProperty("roomNameLabel").objectReferenceValue = roomName;
            waitSo.FindProperty("lobbyRoleBadge").objectReferenceValue = roleBadge;
            waitSo.FindProperty("roomCodeDisplay").objectReferenceValue = codeTxt;
            waitSo.FindProperty("btnCopyCode").objectReferenceValue = btnCopy.GetComponent<Button>();
            waitSo.FindProperty("btnCopyCodeLabel").objectReferenceValue = btnCopyLbl;
            waitSo.FindProperty("roomModeMapLabel").objectReferenceValue = roomModeMap;
            waitSo.FindProperty("roomPlayerCountLabel").objectReferenceValue = pCount;
            waitSo.FindProperty("rosterStatusSummary").objectReferenceValue = rosterSummary;
            waitSo.FindProperty("playerListContent").objectReferenceValue = rosterContent;
            waitSo.FindProperty("playerCardPrefab").objectReferenceValue = playerCardPrefab;
            waitSo.FindProperty("hostSettingsGroup").objectReferenceValue = hostGroup;
            waitSo.FindProperty("visibilityToggle").objectReferenceValue = toggleVis.GetComponent<Toggle>();
            waitSo.FindProperty("btnReady").objectReferenceValue = btnReady.GetComponent<Button>();
            waitSo.FindProperty("btnReadyLabel").objectReferenceValue = btnReadyLbl;
            waitSo.FindProperty("btnStartMatch").objectReferenceValue = btnStart.GetComponent<Button>();
            waitSo.FindProperty("btnLeaveLobby").objectReferenceValue = btnLeave.GetComponent<Button>();
            waitSo.FindProperty("btnLeaveLobbyTop").objectReferenceValue = btnLeaveTop.GetComponent<Button>();

            waitSo.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ShootopiaUIBuilder] WaitingRoom.unity UGUI canvas successfully built and saved!");
        }

        public static void BuildMap1Scene(GameObject scoreRowPrefab)
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Map1.unity", OpenSceneMode.Single);

            var oldUI = GameObject.Find("GameplayHUD");
            if (oldUI != null) GameObject.DestroyImmediate(oldUI);

            // Ensure EventSystem exists in Map1
            if (GameObject.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            }

            var canvasObj = new GameObject("GameplayHUDCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(HUDController), typeof(PauseMenuController), typeof(ScoreboardController), typeof(ResultsScreenController));
            var canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var hudCtrl = canvasObj.GetComponent<HUDController>();
            var hudSo = new SerializedObject(hudCtrl);

            var pauseCtrl = canvasObj.GetComponent<PauseMenuController>();
            var pauseSo = new SerializedObject(pauseCtrl);

            var scoreCtrl = canvasObj.GetComponent<ScoreboardController>();
            var scoreSo = new SerializedObject(scoreCtrl);

            var resultsCtrl = canvasObj.GetComponent<ResultsScreenController>();
            var resultsSo = new SerializedObject(resultsCtrl);

            // 1. Top Center Timer Banner
            var timerBanner = CreateImage("TimerBanner", canvasObj.transform, SPRITE_BAR_BG, new Vector2(240, 50));
            timerBanner.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 1f);
            timerBanner.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 1f);
            timerBanner.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);
            timerBanner.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -20);
            var timerTxt = CreateTMPText("TimerText", timerBanner.transform, "05:00", 28, Color.white, TitleFont, TextAlignmentOptions.Center);

            // 2. Bottom Left Health & Velocity Frame
            var healthCard = CreateImage("HealthFrame", canvasObj.transform, SPRITE_ITEM_CONTAINER, new Vector2(340, 100));
            var hpRt = healthCard.GetComponent<RectTransform>();
            hpRt.anchorMin = new Vector2(0f, 0f);
            hpRt.anchorMax = new Vector2(0f, 0f);
            hpRt.pivot = new Vector2(0f, 0f);
            hpRt.anchoredPosition = new Vector2(30, 30);

            var hpBarBg = CreateImage("HpBarBg", healthCard.transform, SPRITE_BAR_BG, new Vector2(240, 24));
            hpBarBg.GetComponent<RectTransform>().anchoredPosition = new Vector2(25, 15);
            var hpFill = CreateImage("HpBarFill", hpBarBg.transform, SPRITE_BAR_FILL, new Vector2(236, 20));
            hpFill.type = Image.Type.Filled;
            hpFill.fillMethod = Image.FillMethod.Horizontal;
            hpFill.color = new Color(0.2f, 0.9f, 0.4f, 1f);

            var hpIcon = CreateImage("HpIcon", healthCard.transform, ICON_HEART, new Vector2(36, 36));
            hpIcon.GetComponent<RectTransform>().anchoredPosition = new Vector2(-125, 15);
            var hpTxt = CreateTMPText("HpText", healthCard.transform, "100 / 100", 18, Color.white, TitleFont, TextAlignmentOptions.MidlineLeft);
            hpTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(25, -18);

            // 3. Bottom Right Weapon Frame
            var weaponCard = CreateImage("WeaponFrame", canvasObj.transform, SPRITE_ITEM_CONTAINER, new Vector2(340, 100));
            var wpnRt = weaponCard.GetComponent<RectTransform>();
            wpnRt.anchorMin = new Vector2(1f, 0f);
            wpnRt.anchorMax = new Vector2(1f, 0f);
            wpnRt.pivot = new Vector2(1f, 0f);
            wpnRt.anchoredPosition = new Vector2(-30, 30);

            var wpnCat = CreateTMPText("WeaponCategory", weaponCard.transform, "ASSAULT RIFLE", 14, new Color(0.7f, 0.85f, 1f), BodyFont, TextAlignmentOptions.MidlineRight);
            wpnCat.GetComponent<RectTransform>().anchoredPosition = new Vector2(-20, 22);
            var wpnName = CreateTMPText("WeaponName", weaponCard.transform, "AR-15 AUTOMATIC", 20, Color.white, TitleFont, TextAlignmentOptions.MidlineRight);
            wpnName.GetComponent<RectTransform>().anchoredPosition = new Vector2(-20, -2);
            var ammoTxt = CreateTMPText("AmmoText", weaponCard.transform, "30 / ∞", 22, new Color(1f, 0.85f, 0.2f), TitleFont, TextAlignmentOptions.MidlineRight);
            ammoTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(-20, -26);

            var cdBar = CreateImage("CooldownBar", weaponCard.transform, SPRITE_BAR_BG, new Vector2(180, 8));
            cdBar.GetComponent<RectTransform>().anchoredPosition = new Vector2(-20, -38);
            var cdFill = CreateImage("CooldownFill", cdBar.transform, SPRITE_BAR_FILL, new Vector2(176, 6));
            cdFill.type = Image.Type.Filled;
            cdFill.fillMethod = Image.FillMethod.Horizontal;
            cdFill.color = new Color(1f, 0.5f, 0.2f, 1f);
            cdBar.gameObject.SetActive(false);

            // 4. Top Right Kill Feed
            var killFeed = CreateEmpty("KillFeedContainer", canvasObj.transform);
            var kfRt = killFeed.GetComponent<RectTransform>();
            kfRt.anchorMin = new Vector2(1f, 1f);
            kfRt.anchorMax = new Vector2(1f, 1f);
            kfRt.pivot = new Vector2(1f, 1f);
            kfRt.anchoredPosition = new Vector2(-30, -30);
            kfRt.sizeDelta = new Vector2(360, 300);
            var vlgKf = killFeed.AddComponent<VerticalLayoutGroup>();
            vlgKf.spacing = 6;
            vlgKf.childAlignment = TextAnchor.UpperRight;
            vlgKf.childControlWidth = true;
            vlgKf.childControlHeight = false;

            // 5. Combat Feel: Damage Flash Vignette & Hit Markers
            var dmgFlash = CreateEmpty("DamageFlash", canvasObj.transform);
            SetStretch(dmgFlash.GetComponent<RectTransform>(), 0, 0, 1, 1, Vector2.zero, Vector2.zero);
            var dmgImg = dmgFlash.AddComponent<Image>();
            dmgImg.color = new Color(0.85f, 0.1f, 0.1f, 0.45f);
            var dmgCg = dmgFlash.AddComponent<CanvasGroup>();
            dmgCg.alpha = 0f;
            dmgCg.blocksRaycasts = false;

            var hitMarker = CreateEmpty("HitMarker", canvasObj.transform);
            hitMarker.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            var hmT = CreateImage("HitT", hitMarker.transform, SPRITE_BAR_FILL, new Vector2(4, 16));
            hmT.GetComponent<RectTransform>().anchoredPosition = new Vector2(12, 12);
            hmT.GetComponent<RectTransform>().localEulerAngles = new Vector3(0, 0, -45);
            var hmB = CreateImage("HitB", hitMarker.transform, SPRITE_BAR_FILL, new Vector2(4, 16));
            hmB.GetComponent<RectTransform>().anchoredPosition = new Vector2(-12, -12);
            hmB.GetComponent<RectTransform>().localEulerAngles = new Vector3(0, 0, -45);
            var hmL = CreateImage("HitL", hitMarker.transform, SPRITE_BAR_FILL, new Vector2(4, 16));
            hmL.GetComponent<RectTransform>().anchoredPosition = new Vector2(-12, 12);
            hmL.GetComponent<RectTransform>().localEulerAngles = new Vector3(0, 0, 45);
            var hmR = CreateImage("HitR", hitMarker.transform, SPRITE_BAR_FILL, new Vector2(4, 16));
            hmR.GetComponent<RectTransform>().anchoredPosition = new Vector2(12, -12);
            hmR.GetComponent<RectTransform>().localEulerAngles = new Vector3(0, 0, 45);
            hitMarker.SetActive(false);

            // Elimination Banner
            var elimBanner = CreateImage("KillConfirmation", canvasObj.transform, SPRITE_CARD_CONTAINER, new Vector2(400, 60));
            elimBanner.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 180);
            var elimTxt = CreateTMPText("KillTxt", elimBanner.transform, "+1 ELIMINATED", 22, new Color(1f, 0.9f, 0.3f), TitleFont, TextAlignmentOptions.Center);
            elimBanner.gameObject.SetActive(false);

            // 6. Tactical Pause Menu (ESC)
            var pauseModal = CreateModal("PauseMenuRoot", canvasObj.transform, SPRITE_PAUSE_CONTAINER, new Vector2(500, 420), "PAUSED");
            var pauseContent = pauseModal.transform.Find("Content");
            var btnResume = CreateButton("BtnResume", pauseContent, SPRITE_BTN_YELLOW_SPARKLE, "RESUME MATCH", 22, new Vector2(340, 68));
            var btnPauseSet = CreateButton("BtnSettings", pauseContent, SPRITE_BTN_BLUE_MED, "SETTINGS", 20, new Vector2(340, 60));
            var btnLeaveMatch = CreateButton("BtnLeave", pauseContent, SPRITE_BTN_PINK_SHORT, "LEAVE MATCH", 20, new Vector2(340, 60));
            pauseModal.SetActive(false);

            // 7. Live Tab Scoreboard
            var scoreModal = CreateModal("ScoreboardRoot", canvasObj.transform, SPRITE_SETTING_CONTAINER, new Vector2(800, 620), "MATCH SCOREBOARD");
            var scoreTitle = scoreModal.transform.Find("Title").GetComponent<TextMeshProUGUI>();
            var scoreSub = CreateTMPText("SubTitle", scoreModal.transform, "FREE FOR ALL", 16, new Color(0.7f, 0.85f, 1f), BodyFont, TextAlignmentOptions.Center);
            scoreSub.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 230);
            var scrollScore = CreateScrollView("ScoreScrollView", scoreModal.transform, new Vector2(740, 440));
            scrollScore.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -30);
            var scoreRowsCont = scrollScore.transform.Find("Viewport/Content");
            var vlgScore = scoreRowsCont.gameObject.AddComponent<VerticalLayoutGroup>();
            vlgScore.spacing = 8;
            vlgScore.childControlWidth = true;
            vlgScore.childControlHeight = false;
            scoreModal.SetActive(false);

            // 8. Results Screen (Victory / Defeat)
            var resultsModal = CreateModal("ResultsRoot", canvasObj.transform, SPRITE_SETTING_CONTAINER, new Vector2(1000, 680), "MATCH FINISHED");
            var outcomeTxt = resultsModal.transform.Find("Title").GetComponent<TextMeshProUGUI>();
            outcomeTxt.fontSize = 38;
            outcomeTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 265);

            var winnerTxt = CreateTMPText("WinnerInfo", resultsModal.transform, "WINNER: AGENT ALPHA", 20, new Color(1f, 0.85f, 0.2f), TitleFont, TextAlignmentOptions.Center);
            winnerTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 220);

            // Column Header Row
            var headerObj = new GameObject("ScoreTableHeader", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            headerObj.transform.SetParent(resultsModal.transform, false);
            var hRt = headerObj.GetComponent<RectTransform>();
            hRt.anchoredPosition = new Vector2(0, 175);
            hRt.sizeDelta = new Vector2(880, 36);
            headerObj.GetComponent<Image>().color = new Color(0.06f, 0.08f, 0.16f, 0.96f);
            var hHlg = headerObj.GetComponent<HorizontalLayoutGroup>();
            hHlg.padding = new RectOffset(24, 24, 4, 4);
            hHlg.childAlignment = TextAnchor.MiddleLeft;
            hHlg.childControlWidth = false;
            hHlg.childControlHeight = true;
            hHlg.spacing = 10;

            var rh = CreateTMPText("RankH", headerObj.transform, "RANK", 14, new Color(0.85f, 0.95f, 1f), TitleFont, TextAlignmentOptions.MidlineLeft);
            rh.GetComponent<RectTransform>().sizeDelta = new Vector2(70, 28);
            var ph = CreateTMPText("PlayerH", headerObj.transform, "PLAYER", 14, new Color(0.85f, 0.95f, 1f), TitleFont, TextAlignmentOptions.MidlineLeft);
            ph.GetComponent<RectTransform>().sizeDelta = new Vector2(360, 28);
            var kdh = CreateTMPText("KDH", headerObj.transform, "KILLS / DEATHS", 14, new Color(0.85f, 0.95f, 1f), TitleFont, TextAlignmentOptions.Center);
            kdh.GetComponent<RectTransform>().sizeDelta = new Vector2(220, 28);
            var sh = CreateTMPText("ScoreH", headerObj.transform, "SCORE", 14, new Color(0.85f, 0.95f, 1f), TitleFont, TextAlignmentOptions.MidlineRight);
            sh.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 28);

            var scrollResults = CreateScrollView("ResultsScrollView", resultsModal.transform, new Vector2(880, 340));
            scrollResults.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -20);
            var resTableCont = scrollResults.transform.Find("Viewport/Content");
            var vlgRes = resTableCont.gameObject.AddComponent<VerticalLayoutGroup>();
            vlgRes.spacing = 8;
            vlgRes.childControlWidth = true;
            vlgRes.childControlHeight = false;

            var btnRetLobby = CreateButton("BtnReturnLobby", resultsModal.transform, SPRITE_BTN_YELLOW_SPARKLE, "RETURN TO LOBBY", 18, new Vector2(260, 60));
            btnRetLobby.GetComponent<RectTransform>().anchoredPosition = new Vector2(-155, -250);
            var retTxt = btnRetLobby.transform.Find("Text").GetComponent<TextMeshProUGUI>();
            retTxt.color = new Color(0.12f, 0.06f, 0f, 1f);

            var btnMainMenu = CreateButton("BtnMainMenu", resultsModal.transform, SPRITE_BTN_BLUE_MED, "MAIN MENU", 18, new Vector2(240, 60));
            btnMainMenu.GetComponent<RectTransform>().anchoredPosition = new Vector2(165, -250);
            resultsModal.SetActive(false);

            // Wire HUDController
            hudSo.FindProperty("timerText").objectReferenceValue = timerTxt;
            hudSo.FindProperty("healthFill").objectReferenceValue = hpFill;
            hudSo.FindProperty("healthText").objectReferenceValue = hpTxt;
            hudSo.FindProperty("weaponCategoryText").objectReferenceValue = wpnCat;
            hudSo.FindProperty("weaponNameText").objectReferenceValue = wpnName;
            hudSo.FindProperty("ammoText").objectReferenceValue = ammoTxt;
            hudSo.FindProperty("cooldownBarRoot").objectReferenceValue = cdBar.gameObject;
            hudSo.FindProperty("cooldownFill").objectReferenceValue = cdFill;
            hudSo.FindProperty("killFeedContainer").objectReferenceValue = killFeed.transform;
            hudSo.FindProperty("damageFlashGroup").objectReferenceValue = dmgCg;
            hudSo.FindProperty("hitMarkerRoot").objectReferenceValue = hitMarker;
            var ticksProp = hudSo.FindProperty("hitMarkerTicks");
            ticksProp.arraySize = 4;
            ticksProp.GetArrayElementAtIndex(0).objectReferenceValue = hmT;
            ticksProp.GetArrayElementAtIndex(1).objectReferenceValue = hmB;
            ticksProp.GetArrayElementAtIndex(2).objectReferenceValue = hmL;
            ticksProp.GetArrayElementAtIndex(3).objectReferenceValue = hmR;
            hudSo.FindProperty("killConfirmationRoot").objectReferenceValue = elimBanner.gameObject;
            hudSo.FindProperty("killConfirmationText").objectReferenceValue = elimTxt;
            hudSo.ApplyModifiedProperties();

            // Wire PauseMenuController
            pauseSo.FindProperty("pauseMenuRoot").objectReferenceValue = pauseModal;
            pauseSo.FindProperty("pauseCanvasGroup").objectReferenceValue = pauseModal.GetComponent<CanvasGroup>();
            pauseSo.FindProperty("btnResume").objectReferenceValue = btnResume.GetComponent<Button>();
            pauseSo.FindProperty("btnSettings").objectReferenceValue = btnPauseSet.GetComponent<Button>();
            pauseSo.FindProperty("btnLeave").objectReferenceValue = btnLeaveMatch.GetComponent<Button>();
            pauseSo.ApplyModifiedProperties();

            // Wire ScoreboardController
            scoreSo.FindProperty("scoreboardRoot").objectReferenceValue = scoreModal;
            scoreSo.FindProperty("scoreboardCanvasGroup").objectReferenceValue = scoreModal.GetComponent<CanvasGroup>();
            scoreSo.FindProperty("modeTitleText").objectReferenceValue = scoreTitle;
            scoreSo.FindProperty("subTitleText").objectReferenceValue = scoreSub;
            scoreSo.FindProperty("rowsContainer").objectReferenceValue = scoreRowsCont;
            scoreSo.FindProperty("rowPrefab").objectReferenceValue = scoreRowPrefab;
            scoreSo.ApplyModifiedProperties();

            // Wire ResultsScreenController
            resultsSo.FindProperty("resultsRoot").objectReferenceValue = resultsModal;
            resultsSo.FindProperty("resultsCanvasGroup").objectReferenceValue = resultsModal.GetComponent<CanvasGroup>();
            resultsSo.FindProperty("outcomeBannerText").objectReferenceValue = outcomeTxt;
            resultsSo.FindProperty("winnerInfoText").objectReferenceValue = winnerTxt;
            resultsSo.FindProperty("scoreTableContent").objectReferenceValue = resTableCont;
            resultsSo.FindProperty("scoreRowPrefab").objectReferenceValue = scoreRowPrefab;
            resultsSo.FindProperty("btnReturnToLobby").objectReferenceValue = btnRetLobby.GetComponent<Button>();
            resultsSo.FindProperty("btnMainMenu").objectReferenceValue = btnMainMenu.GetComponent<Button>();
            resultsSo.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ShootopiaUIBuilder] Map1.unity UGUI HUD & overlays successfully built and saved!");
        }

        public static void BuildBootstrapScene()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Bootstrap.unity", OpenSceneMode.Single);

            var oldLoading = GameObject.Find("LoadingScreenOverlay");
            if (oldLoading != null) GameObject.DestroyImmediate(oldLoading);

            var oldJoin = GameObject.Find("JoinByCodeUI");
            if (oldJoin != null) GameObject.DestroyImmediate(oldJoin);

            BuildLoadingScreenPrefabInScene();

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ShootopiaUIBuilder] Bootstrap.unity UGUI transition overlay successfully built and saved!");
        }

        public static void BuildLoadingScreenPrefabInScene()
        {
            var existing = GameObject.Find("LoadingScreenManager");
            if (existing != null) GameObject.DestroyImmediate(existing);

            var loadObj = new GameObject("LoadingScreenManager", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(LoadingScreenManager));
            var canvas = loadObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            var scaler = loadObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var root = CreateEmpty("LoadingRoot", loadObj.transform);
            SetStretch(root.GetComponent<RectTransform>(), 0, 0, 1, 1, Vector2.zero, Vector2.zero);
            var bg = root.AddComponent<Image>();
            bg.sprite = LoadSprite(BG_HOME);
            bg.color = new Color(0.1f, 0.12f, 0.18f, 1f);

            var icon = CreateImage("Spinner", root.transform, ICON_RADAR, new Vector2(90, 90));
            icon.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 60);

            var status = CreateTMPText("StatusText", root.transform, "WARPING TO ARENA...", 26, Color.white, TitleFont, TextAlignmentOptions.Center);
            status.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -10);

            var barBg = CreateImage("BarBg", root.transform, SPRITE_BAR_BG, new Vector2(500, 24));
            barBg.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -60);
            var barFill = CreateImage("BarFill", barBg.transform, SPRITE_BAR_FILL, new Vector2(494, 20));
            barFill.type = Image.Type.Filled;
            barFill.fillMethod = Image.FillMethod.Horizontal;
            barFill.color = new Color(0.3f, 0.85f, 1f, 1f);

            var tip = CreateTMPText("TipText", root.transform, "Tip: Maintain killstreaks to unlock powerful weapon abilities!", 18, new Color(0.8f, 0.9f, 1f), BodyFont, TextAlignmentOptions.Center);
            tip.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -120);
            tip.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 60);

            var loadMgr = loadObj.GetComponent<LoadingScreenManager>();
            var loadSo = new SerializedObject(loadMgr);
            loadSo.FindProperty("loadingRoot").objectReferenceValue = root;
            loadSo.FindProperty("statusText").objectReferenceValue = status;
            loadSo.FindProperty("progressFillImage").objectReferenceValue = barFill;
            loadSo.FindProperty("tipText").objectReferenceValue = tip;
            loadSo.FindProperty("spinnerIcon").objectReferenceValue = icon.transform;
            loadSo.ApplyModifiedProperties();

            root.SetActive(false);
        }

        // ──────────────────────── UI ELEMENT FACTORY HELPERS ────────────────────────

        private static GameObject CreateEmpty(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static Image CreateImage(string name, Transform parent, string spritePath, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            if (!string.IsNullOrEmpty(spritePath))
            {
                img.sprite = LoadSprite(spritePath);
            }
            return img;
        }

        private static TextMeshProUGUI CreateTMPText(string name, Transform parent, string text, float fontSize, Color color, TMP_FontAsset font, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            if (font != null) tmp.font = font;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static GameObject CreateButton(string name, Transform parent, string spritePath, string label, float fontSize, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(UIJuice));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;

            var img = go.GetComponent<Image>();
            img.sprite = LoadSprite(spritePath);
            img.type = Image.Type.Sliced;

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;

            var txt = CreateTMPText("Label", go.transform, label, fontSize, Color.white, TitleFont, TextAlignmentOptions.Center);
            SetStretch(txt.GetComponent<RectTransform>(), 0, 0, 1, 1, Vector2.zero, Vector2.zero);

            return go;
        }

        private static GameObject CreateModal(string name, Transform parent, string containerSpritePath, Vector2 size, string title)
        {
            var modal = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            modal.transform.SetParent(parent, false);
            var rt = modal.GetComponent<RectTransform>();
            rt.sizeDelta = size;

            var img = modal.GetComponent<Image>();
            img.sprite = LoadSprite(containerSpritePath);
            img.type = Image.Type.Sliced;
            img.color = new Color(0.95f, 0.95f, 1f, 0.98f);

            var titleTxt = CreateTMPText("Title", modal.transform, title, 26, Color.white, TitleFont, TextAlignmentOptions.Center);
            titleTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, (size.y / 2f) - 40);

            var content = CreateEmpty("Content", modal.transform);
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 14;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;
            content.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -20);
            content.GetComponent<RectTransform>().sizeDelta = new Vector2(size.x - 60, size.y - 120);

            modal.SetActive(false);
            return modal;
        }

        private static GameObject CreateInputField(string name, Transform parent, string placeholderText, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;

            var img = go.GetComponent<Image>();
            img.sprite = LoadSprite(SPRITE_INPUT_BG);
            img.type = Image.Type.Sliced;

            var input = go.GetComponent<TMP_InputField>();

            var textObj = CreateTMPText("Text", go.transform, "", 20, Color.white, BodyFont, TextAlignmentOptions.MidlineLeft);
            SetStretch(textObj.GetComponent<RectTransform>(), 0.05f, 0.1f, 0.95f, 0.9f, Vector2.zero, Vector2.zero);

            var placeObj = CreateTMPText("Placeholder", go.transform, placeholderText, 18, new Color(0.6f, 0.7f, 0.85f, 0.6f), BodyFont, TextAlignmentOptions.MidlineLeft);
            SetStretch(placeObj.GetComponent<RectTransform>(), 0.05f, 0.1f, 0.95f, 0.9f, Vector2.zero, Vector2.zero);

            input.textComponent = textObj;
            input.placeholder = placeObj;

            return go;
        }

        private static GameObject CreateSlider(string name, Transform parent, float min, float max, float current, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;

            var bg = CreateImage("Background", go.transform, SPRITE_BAR_BG, size);
            SetStretch(bg.GetComponent<RectTransform>(), 0, 0, 1, 1, Vector2.zero, Vector2.zero);

            var fillArea = CreateEmpty("FillArea", go.transform);
            SetStretch(fillArea.GetComponent<RectTransform>(), 0.02f, 0.1f, 0.98f, 0.9f, Vector2.zero, Vector2.zero);
            var fill = CreateImage("Fill", fillArea.transform, SPRITE_BAR_FILL, Vector2.zero);
            SetStretch(fill.GetComponent<RectTransform>(), 0, 0, 1, 1, Vector2.zero, Vector2.zero);
            fill.type = Image.Type.Sliced;
            fill.color = new Color(0.2f, 0.8f, 1f, 1f);

            var slider = go.GetComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = current;
            slider.fillRect = fill.GetComponent<RectTransform>();

            return go;
        }

        private static GameObject CreateSliderWithLabel(string name, Transform parent, string label, out TextMeshProUGUI valLabel, float min = 0f, float max = 1f, float def = 1f)
        {
            var row = CreateEmpty(name + "_Row", parent);
            row.GetComponent<RectTransform>().sizeDelta = new Vector2(480, 50);

            var title = CreateTMPText("Label", row.transform, label, 16, Color.white, BodyFont, TextAlignmentOptions.MidlineLeft);
            title.GetComponent<RectTransform>().anchoredPosition = new Vector2(-120, 0);
            title.GetComponent<RectTransform>().sizeDelta = new Vector2(220, 40);

            var sliderObj = CreateSlider(name, row.transform, min, max, def, new Vector2(180, 24));
            sliderObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(90, 0);

            valLabel = CreateTMPText("ValLabel", row.transform, "100%", 16, new Color(0.3f, 0.9f, 1f), BodyFont, TextAlignmentOptions.MidlineRight);
            valLabel.GetComponent<RectTransform>().anchoredPosition = new Vector2(210, 0);
            valLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(60, 40);

            return sliderObj;
        }

        private static GameObject CreateToggle(string name, Transform parent, string label, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Toggle));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;

            var bg = CreateImage("Background", go.transform, SPRITE_TOGGLE_OFF, new Vector2(70, 34));
            bg.GetComponent<RectTransform>().anchoredPosition = new Vector2(-80, 0);

            var check = CreateImage("Checkmark", bg.transform, SPRITE_TOGGLE_ON, new Vector2(70, 34));
            check.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            var txt = CreateTMPText("Label", go.transform, label, 16, Color.white, BodyFont, TextAlignmentOptions.MidlineLeft);
            txt.GetComponent<RectTransform>().anchoredPosition = new Vector2(30, 0);
            txt.GetComponent<RectTransform>().sizeDelta = new Vector2(180, 34);

            var toggle = go.GetComponent<Toggle>();
            toggle.graphic = check;
            toggle.isOn = true;

            return go;
        }

        private static GameObject CreateScrollView(string name, Transform parent, Vector2 size)
        {
            var scroll = new GameObject(name, typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scroll.transform.SetParent(parent, false);
            var rt = scroll.GetComponent<RectTransform>();
            rt.sizeDelta = size;

            var bg = scroll.GetComponent<Image>();
            bg.color = new Color(0.05f, 0.06f, 0.1f, 0.5f);

            var viewport = CreateEmpty("Viewport", scroll.transform);
            SetStretch(viewport.GetComponent<RectTransform>(), 0, 0, 1, 1, Vector2.zero, Vector2.zero);
            var mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            var vImg = viewport.AddComponent<Image>();

            var content = CreateEmpty("Content", viewport.transform);
            var cRt = content.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0, 1);
            cRt.anchorMax = new Vector2(1, 1);
            cRt.pivot = new Vector2(0.5f, 1);
            cRt.sizeDelta = new Vector2(0, 0);

            var sr = scroll.GetComponent<ScrollRect>();
            sr.viewport = viewport.GetComponent<RectTransform>();
            sr.content = cRt;
            sr.horizontal = false;
            sr.vertical = true;
            sr.scrollSensitivity = 25f;

            return scroll;
        }

        private static void SetStretch(RectTransform rt, float minX, float minY, float maxX, float maxY, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = new Vector2(minX, minY);
            rt.anchorMax = new Vector2(maxX, maxY);
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }
    }
}
