using UnityEngine;
using UnityEditor;
using UnityEngine.Animations;

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

    private void OnGUI()
    {
        armature = (GameObject)EditorGUILayout.ObjectField("Avatar Armature", armature, typeof(GameObject), true);
        clothes = (GameObject)EditorGUILayout.ObjectField("Clothes Armature", clothes, typeof(GameObject), true);
        allowHierarchyMismatch = (bool)EditorGUILayout.Toggle("Allow hierarchy mismatch", allowHierarchyMismatch);

        GUI.enabled = armature != null && clothes != null;

        if (GUILayout.Button("Apply Clothes"))
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Apply Clothes");
            var undoID = Undo.GetCurrentGroup();

            foreach (Transform clothBones in clothes.transform)
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

                CreateParentConstraint(clothes, armatureChild);
                foreach (Transform clothesChild in clothes)
                {
                    ApplyBones(clothesChild, armatureChild);
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
}