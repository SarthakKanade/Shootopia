using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Shootopia.EditorScripts {
    public static class RevampAnimator {
        [MenuItem("Tools/Revamp Animator Controller")]
        public static void Run() {
            string controllerPath = "Assets/_Shootopia/Animators/Shootopia_Locomotion.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null) { Debug.LogError("Controller not found"); return; }
            
            // Clear all layers
            while(controller.layers.Length > 0) {
                controller.RemoveLayer(0);
            }

            // Clear all parameters
            while(controller.parameters.Length > 0) {
                controller.RemoveParameter(0);
            }

            // Add Parameters
            controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
            controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsFiring", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Dash", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Roll", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Reload", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("SwapWeapon", AnimatorControllerParameterType.Trigger);

            // ================= BASE LAYER =================
            var baseLayer = new AnimatorControllerLayer {
                name = "Base Layer",
                defaultWeight = 1f,
                stateMachine = new AnimatorStateMachine()
            };
            baseLayer.stateMachine.name = baseLayer.name;
            baseLayer.stateMachine.hideFlags = HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(baseLayer.stateMachine, controller);
            controller.AddLayer(baseLayer);

            var sm = baseLayer.stateMachine;

            // Load Clips
            string basePath = "Assets/BattleRoyaleDuoPAPBR/Animation/AssaultRifleStance/";
            var idleClip = GetClip(basePath + "IdleNormal01_AR_Anim.fbx");
            var runF = GetClip(basePath + "RunFWD_AR_Anim.fbx");
            var runB = GetClip(basePath + "RunBWD_AR_Anim.fbx");
            var runL = GetClip(basePath + "RunLeft_AR_Anim.fbx");
            var runR = GetClip(basePath + "RunRight_AR_Anim.fbx");

            var dashF = GetClip(basePath + "DashFWD_AR_Anim.fbx");
            var dashB = GetClip(basePath + "DashBWD_AR_Anim.fbx");
            var dashL = GetClip(basePath + "DashLeft_AR_Anim.fbx");
            var dashR = GetClip(basePath + "DashRight_AR_Anim.fbx");

            var rollF = GetClip(basePath + "RollFWD_AR_Anim.fbx");
            var rollB = GetClip(basePath + "RollBWD_AR_Anim.fbx");
            var rollL = GetClip(basePath + "RollLeft_AR_Anim.fbx");
            var rollR = GetClip(basePath + "RollRight_AR_Anim.fbx");

            var jumpStart = GetClip(basePath + "JumpStart_AR_Anim.fbx");
            var jumpAir = GetClip(basePath + "JumpAir_AR_Anim.fbx");
            var jumpEnd = GetClip(basePath + "JumpEnd_AR_Anim.fbx");

            // Locomotion BlendTree
            var locomotionBT = new BlendTree();
            locomotionBT.name = "Locomotion";
            locomotionBT.blendType = BlendTreeType.SimpleDirectional2D;
            locomotionBT.blendParameter = "MoveX";
            locomotionBT.blendParameterY = "MoveY";
            locomotionBT.AddChild(idleClip, new Vector2(0, 0));
            locomotionBT.AddChild(runF, new Vector2(0, 1));
            locomotionBT.AddChild(runB, new Vector2(0, -1));
            locomotionBT.AddChild(runL, new Vector2(-1, 0));
            locomotionBT.AddChild(runR, new Vector2(1, 0));
            AssetDatabase.AddObjectToAsset(locomotionBT, controller);
            
            var locoState = sm.AddState("Locomotion");
            locoState.motion = locomotionBT;
            sm.defaultState = locoState;

            // Dash BlendTree
            var dashBT = new BlendTree();
            dashBT.name = "DashBT";
            dashBT.blendType = BlendTreeType.SimpleDirectional2D;
            dashBT.blendParameter = "MoveX";
            dashBT.blendParameterY = "MoveY";
            dashBT.AddChild(dashF, new Vector2(0, 1));
            dashBT.AddChild(dashB, new Vector2(0, -1));
            dashBT.AddChild(dashL, new Vector2(-1, 0));
            dashBT.AddChild(dashR, new Vector2(1, 0));
            AssetDatabase.AddObjectToAsset(dashBT, controller);

            var dashState = sm.AddState("Dash");
            dashState.motion = dashBT;
            var dashSMB = dashState.AddStateMachineBehaviour<Shootopia.FSM.AnimationExitSMB>();
            dashSMB.stateName = "Dash";

            var dashTrans = sm.AddAnyStateTransition(dashState);
            dashTrans.AddCondition(AnimatorConditionMode.If, 0, "Dash");
            dashTrans.duration = 0.1f;
            var dashExit = dashState.AddTransition(locoState);
            dashExit.hasExitTime = true;
            dashExit.exitTime = 0.8f;
            dashExit.duration = 0.2f;

            // Roll BlendTree
            var rollBT = new BlendTree();
            rollBT.name = "RollBT";
            rollBT.blendType = BlendTreeType.SimpleDirectional2D;
            rollBT.blendParameter = "MoveX";
            rollBT.blendParameterY = "MoveY";
            rollBT.AddChild(rollF, new Vector2(0, 1));
            rollBT.AddChild(rollB, new Vector2(0, -1));
            rollBT.AddChild(rollL, new Vector2(-1, 0));
            rollBT.AddChild(rollR, new Vector2(1, 0));
            AssetDatabase.AddObjectToAsset(rollBT, controller);

            var rollState = sm.AddState("Roll");
            rollState.motion = rollBT;
            var rollSMB = rollState.AddStateMachineBehaviour<Shootopia.FSM.AnimationExitSMB>();
            rollSMB.stateName = "Roll";

            var rollTrans = sm.AddAnyStateTransition(rollState);
            rollTrans.AddCondition(AnimatorConditionMode.If, 0, "Roll");
            rollTrans.duration = 0.1f;
            var rollExit = rollState.AddTransition(locoState);
            rollExit.hasExitTime = true;
            rollExit.exitTime = 0.8f;
            rollExit.duration = 0.2f;

            // Jump Sequence
            var jStart = sm.AddState("JumpStart");
            jStart.motion = jumpStart;
            var jStartSMB = jStart.AddStateMachineBehaviour<Shootopia.FSM.AnimationExitSMB>();
            jStartSMB.stateName = "JumpStart";

            var jAir = sm.AddState("JumpAir");
            jAir.motion = jumpAir;
            
            var jEnd = sm.AddState("JumpEnd");
            jEnd.motion = jumpEnd;
            var jEndSMB = jEnd.AddStateMachineBehaviour<Shootopia.FSM.AnimationExitSMB>();
            jEndSMB.stateName = "JumpEnd";

            var jumpTrans = sm.AddAnyStateTransition(jStart);
            jumpTrans.AddCondition(AnimatorConditionMode.If, 0, "Jump");
            jumpTrans.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded"); // Ensure grounded
            jumpTrans.duration = 0.1f;

            var toAir = jStart.AddTransition(jAir);
            toAir.hasExitTime = true;
            toAir.exitTime = 0.9f;
            toAir.duration = 0.1f;

            var toEnd = jAir.AddTransition(jEnd);
            toEnd.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            toEnd.duration = 0.1f;

            var jumpExit = jEnd.AddTransition(locoState);
            jumpExit.hasExitTime = true;
            jumpExit.exitTime = 0.8f;
            jumpExit.duration = 0.2f;

            // ================= UPPER BODY LAYER =================
            var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>("Assets/_Shootopia/Animators/UpperBody_Mask.mask");
            
            var upperLayer = new AnimatorControllerLayer {
                name = "Upper Body",
                defaultWeight = 1f,
                avatarMask = mask,
                stateMachine = new AnimatorStateMachine()
            };
            upperLayer.stateMachine.name = upperLayer.name;
            upperLayer.stateMachine.hideFlags = HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(upperLayer.stateMachine, controller);
            controller.AddLayer(upperLayer);

            var uSm = upperLayer.stateMachine;

            var shootClip = GetClip(basePath + "ShootAutoshot_AR_Anim.fbx");
            var reloadClip = GetClip(basePath + "Reloading_AR_Anim.fbx");
            var swapClip = GetClip(basePath + "PowerUP_AR_Anim.fbx");

            var emptyState = uSm.AddState("Empty");
            uSm.defaultState = emptyState;

            var shootState = uSm.AddState("Shoot");
            shootState.motion = shootClip;
            var toShoot = emptyState.AddTransition(shootState);
            toShoot.AddCondition(AnimatorConditionMode.If, 0, "IsFiring");
            toShoot.duration = 0.1f;
            var fromShoot = shootState.AddTransition(emptyState);
            fromShoot.AddCondition(AnimatorConditionMode.IfNot, 0, "IsFiring");
            fromShoot.duration = 0.2f;

            var reloadState = uSm.AddState("Reload");
            reloadState.motion = reloadClip;
            var toReload = uSm.AddAnyStateTransition(reloadState);
            toReload.AddCondition(AnimatorConditionMode.If, 0, "Reload");
            toReload.duration = 0.1f;
            var fromReload = reloadState.AddTransition(emptyState);
            fromReload.hasExitTime = true;
            fromReload.exitTime = 0.9f;
            fromReload.duration = 0.1f;

            var swapState = uSm.AddState("SwapWeapon");
            swapState.motion = swapClip;
            var toSwap = uSm.AddAnyStateTransition(swapState);
            toSwap.AddCondition(AnimatorConditionMode.If, 0, "SwapWeapon");
            toSwap.duration = 0.1f;
            var fromSwap = swapState.AddTransition(emptyState);
            fromSwap.hasExitTime = true;
            fromSwap.exitTime = 0.9f;
            fromSwap.duration = 0.1f;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("Animator Revamped Successfully!");
        }

        private static AnimationClip GetClip(string path) {
            var objects = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach(var obj in objects) {
                if (obj is AnimationClip clip && !clip.name.StartsWith("__preview")) {
                    return clip;
                }
            }
            return null;
        }
    }
}
