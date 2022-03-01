using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.IO;
using System;

public class BlendshapeStuff : MonoBehaviour
{
    [MenuItem("Tools/Blendshapes/Copy weighted blendshapes as JSON")]
    static void CopyWeightedBlendshapesJson()
    {
        CopyBlendshapesJson(true);
    }

    [MenuItem("Tools/Blendshapes/Copy all blendshapes as JSON")]
    static void CopyAllBlendshapesJson()
    {
        CopyBlendshapesJson();
    }

    private static void CopyBlendshapesJson(bool weightedOnly = false)
    {
        if (Selection.gameObjects.Length != 1)
        {
            return;
        }

        var obj = Selection.gameObjects[0];
        if (obj == null)
        {
            return;
        }

        var blendShapes = GetBlendShapes(obj.gameObject);

        if (weightedOnly)
        {
            blendShapes = blendShapes.Where(x => x.Weight > 0.0f);
        }

        GUIUtility.systemCopyBuffer = JsonHelper.ToJson(blendShapes.ToArray(), true);
    }

    [MenuItem("Tools/Blendshapes/Paste JSON blendshapes")]
    static void PasteJsonBlendshapes()
    {
        if (Selection.gameObjects.Length != 1)
        {
            return;
        }

        var obj = Selection.gameObjects[0];
        if (obj == null)
        {
            return;
        }

        Blendshape[] data;
        try
        {
            data = JsonHelper.FromJson<Blendshape>(GUIUtility.systemCopyBuffer);
        }
        catch (Exception)
        {
            EditorUtility.DisplayDialog("Could not paste blendshapes", "Could not paste blendshapes, not a valid JSON object", "Ok");
            return;
        }

        if (data == null || data.Length == 0)
        {
            return;
        }

        Undo.RecordObject(null, "Paste JSON blendshapes");
        var undoID = Undo.GetCurrentGroup();

        SetBlendShapes(obj.gameObject, data);

        Undo.CollapseUndoOperations(undoID);
    }

    static void SetBlendShapes(GameObject obj, IEnumerable<Blendshape> blendshapes)
    {
        var skinnedMesh = obj.GetComponent<SkinnedMeshRenderer>();
        var mesh = skinnedMesh.sharedMesh;

        var existingBlendshapes = GetBlendShapes(obj);

        foreach (var blendshape in existingBlendshapes)
        {
            var newBlendshape = blendshapes.SingleOrDefault(x => x.Name == blendshape.Name);

            var newWeight = newBlendshape?.Weight ?? 0.0f;

            Undo.RecordObject(skinnedMesh, "Update blendshape weight");
            skinnedMesh.SetBlendShapeWeight(blendshape.Index, newWeight);
        }
    }

    static IEnumerable<Blendshape> GetBlendShapes(GameObject obj)
    {
        var skinnedMesh = obj.GetComponent<SkinnedMeshRenderer>();
        var mesh = skinnedMesh.sharedMesh;
        for (int i = 0; i < mesh.blendShapeCount; i++)
        {
            yield return new Blendshape()
            {
                Index = i,
                Name = mesh.GetBlendShapeName(i),
                Weight = skinnedMesh.GetBlendShapeWeight(i)
            };
        }
    }

    [Serializable]
    class Blendshape
    {
        // fields because unitys json serializer can't handle properties
        public int Index;
        public string Name;
        public float Weight;

        public override string ToString()
        {
            return Index + " -> " + Name + " -> " + Weight;
        }
    }

    // because unitys json serializer also can't handle lists
    public static class JsonHelper
    {
        public static T[] FromJson<T>(string json)
        {
            var wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
            return wrapper.Blendshapes;
        }

        public static string ToJson<T>(T[] array)
        {
            var wrapper = new Wrapper<T>();
            wrapper.Blendshapes = array;
            return JsonUtility.ToJson(wrapper);
        }

        public static string ToJson<T>(T[] array, bool prettyPrint)
        {
            var wrapper = new Wrapper<T>();
            wrapper.Blendshapes = array;
            return JsonUtility.ToJson(wrapper, prettyPrint);
        }

        [Serializable]
        private class Wrapper<T>
        {
            public T[] Blendshapes;
        }
    }
}