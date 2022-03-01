using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

public class FixSingleKeyframeAnimations : MonoBehaviour
{
    [MenuItem("Tools/Fix single keyframe animations")]
    static void FixKeyframesClick()
    {
        Undo.RecordObject(null, "Fix single keyframe animations");
        var undoID = Undo.GetCurrentGroup();

        var animationAssets = AssetDatabase.FindAssets("t:AnimationClip", null);

        foreach (var assetGuid in animationAssets)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);

            // don't touch sdk files
            if (assetPath.StartsWith("Assets/VRCSDK/"))
            {
                continue;
            }

            var animationClip = (AnimationClip)AssetDatabase.LoadAssetAtPath(assetPath, typeof(AnimationClip));

            foreach (var curveBinding in AnimationUtility.GetCurveBindings(animationClip))
            {
                var curve = AnimationUtility.GetEditorCurve(animationClip, curveBinding);
                if (curve.keys.Length == 1)
                {
                    Debug.Log("Duplicating keyframe in " + assetPath);
                    curve.AddKey(1.0f / 60.0f, curve.keys[0].value);

                    Undo.RecordObject(animationClip, "Fix single keyframe animations");

                    AnimationUtility.SetEditorCurve(animationClip, curveBinding, curve);
                }
            }
        }

        AssetDatabase.SaveAssets();

        Undo.CollapseUndoOperations(undoID);
    }
}
