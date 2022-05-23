using UnityEngine;
using UnityEditor;
using UnityEngine.Animations;
using System.Linq;

public class AddParameter : EditorWindow
{
    [MenuItem("Tools/Apply Clothes")]
    static void Init()
    {
        var window = (AddParameter)GetWindow(typeof(AddParameter));
        window.titleContent = new GUIContent("Apply Clothes");
        window.Show();
    }

    GameObject armature = null;
    GameObject clothes = null;
    bool allowHierarchyMismatch = false;
    bool useParentConstraints = true;
    string reparentPrefix = "";

    private void OnGUI()
    {
        armature = (GameObject)EditorGUILayout.ObjectField("Avatar Armature", armature, typeof(GameObject), true);
        clothes = (GameObject)EditorGUILayout.ObjectField("Clothes Armature", clothes, typeof(GameObject), true);
        useParentConstraints = (bool)EditorGUILayout.Toggle("Use parent constraints instead of reparenting", useParentConstraints);
        if (!useParentConstraints)
        {
            if (clothes != null && string.IsNullOrEmpty(reparentPrefix))
            {
                reparentPrefix = clothes.transform.parent.name;
            }
            reparentPrefix = EditorGUILayout.TextField("Optional Bone prefix", reparentPrefix);
        }

        allowHierarchyMismatch = (bool)EditorGUILayout.Toggle("Allow hierarchy mismatch", allowHierarchyMismatch);

        GUI.enabled = armature != null && clothes != null;

        if (GUILayout.Button("Apply Clothes"))
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Apply Clothes");
            var undoID = Undo.GetCurrentGroup();

            foreach (Transform clothBones in clothes.transform.Cast<Transform>().ToArray())
            {
                ApplyBones(clothBones, armature.transform);
            }

            Undo.CollapseUndoOperations(undoID);
        }
    }

    private void ApplyBones(Transform clothes, Transform armature)
    {
        var boneName = clothes.name;

        var matched = false;
        foreach (Transform armatureChild in armature)
        {
            if (boneName.Contains(armatureChild.name))
            {
                matched = true;
                foreach (Transform clothesChild in clothes.transform.Cast<Transform>().ToArray())
                {
                    ApplyBones(clothesChild, armatureChild);
                }

                if (useParentConstraints)
                {
                    CreateParentConstraint(clothes, armatureChild);
                }
                else
                {
                    Reparent(clothes, armatureChild);
                }
                break;
            }
        }

        // if none matched, try looping through all children instead if enabled
        if (allowHierarchyMismatch && !matched)
        {
            foreach (Transform armatureChild in armature)
            {
                ApplyBones(clothes, armatureChild);
            }
        }
    }

    private void CreateParentConstraint(Transform obj, Transform target)
    {
        var parentConstraint = obj.gameObject.GetComponent<ParentConstraint>();
        if (parentConstraint != null)
        {
            Undo.DestroyObjectImmediate(parentConstraint);
        }

        parentConstraint = Undo.AddComponent<ParentConstraint>(obj.gameObject);
        parentConstraint.AddSource(new ConstraintSource()
        {
            sourceTransform = target,
            weight = 1
        });

        parentConstraint.constraintActive = true;
    }

    private void Reparent(Transform obj, Transform target)
    {
        Debug.Log("Reparenting " + obj.name + " -> " + target.name);
        Undo.SetTransformParent(obj, target, "");

        if (!string.IsNullOrEmpty(reparentPrefix))
        {
            Undo.RegisterCompleteObjectUndo(obj, "");
            obj.name = reparentPrefix + "_" + obj.name;
        }
    }
}