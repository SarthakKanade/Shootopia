using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Linq;

public class AnalyzeAnimator : EditorWindow
{
    [MenuItem("Tools/Analyze Animator")]
    public static void Analyze()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/_Shootopia/Animators/Shootopia_Locomotion.controller");
        if (controller == null)
        {
            Debug.LogError("Controller not found at Assets/_Shootopia/Animators/Shootopia_Locomotion.controller");
            return;
        }

        string info = "Parameters:\n";
        foreach (var p in controller.parameters)
        {
            info += $"- {p.name} ({p.type})\n";
        }
        
        info += "\nLayers:\n";
        foreach (var layer in controller.layers)
        {
            info += $"Layer: {layer.name} (Weight: {layer.defaultWeight})\n";
            info += "States:\n";
            foreach (var state in layer.stateMachine.states)
            {
                info += $"  - {state.state.name}\n";
                if (state.state.motion is BlendTree bt)
                {
                    info += $"    BlendTree: {bt.name} (BlendType: {bt.blendType}, Parameter: {bt.blendParameter})\n";
                    foreach (var child in bt.children)
                    {
                        info += $"      Child: {(child.motion ? child.motion.name : "None")} (Threshold: {child.threshold})\n";
                    }
                }
            }
        }

        File.WriteAllText("AnimatorInfo.txt", info);
        Debug.Log("Animator info written to AnimatorInfo.txt");
    }
}
