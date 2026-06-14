using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using System.Linq;

namespace Shootopia.EditorScripts {
    public static class AddJumpState {
        [MenuItem("Tools/Add Jump State To Animator")]
        public static void Run() {
            string controllerPath = "Assets/_Shootopia/Animators/Shootopia_Locomotion.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null) { Debug.LogError("Controller not found"); return; }
            
            var baseLayer = controller.layers[0];
            var stateMachine = baseLayer.stateMachine;
            
            if (stateMachine.states.Any(s => s.state.name == "Jump")) {
                Debug.Log("Jump state already exists.");
                return;
            }

            string clipPath = "Assets/BattleRoyaleDuoPAPBR/Animation/AssaultRifleStance/Jump_AR_Anim.fbx";
            var objects = AssetDatabase.LoadAllAssetsAtPath(clipPath);
            AnimationClip jumpClip = null;
            foreach(var obj in objects) {
                if (obj is AnimationClip clip && !clip.name.StartsWith("__preview")) {
                    jumpClip = clip;
                    break;
                }
            }

            if (jumpClip == null) { Debug.LogError("Clip not found"); return; }

            var jumpState = stateMachine.AddState("Jump");
            jumpState.motion = jumpClip;
            
            var anyTrans = stateMachine.AddAnyStateTransition(jumpState);
            anyTrans.AddCondition(AnimatorConditionMode.If, 0, "Jump");
            anyTrans.hasFixedDuration = true;
            anyTrans.duration = 0.1f;
            
            var exitTrans = jumpState.AddTransition(stateMachine.defaultState);
            exitTrans.hasExitTime = true;
            exitTrans.exitTime = 0.85f;
            exitTrans.hasFixedDuration = true;
            exitTrans.duration = 0.15f;
            
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("Jump state added successfully.");
        }
    }
}
