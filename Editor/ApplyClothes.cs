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

    private void OnGUI()
    {
        armature = (GameObject)EditorGUILayout.ObjectField("Avatar Armature", armature, typeof(GameObject), true);
        clothes = (GameObject)EditorGUILayout.ObjectField("Clothes Armature", clothes, typeof(GameObject), true);

        GUI.enabled = armature != null && clothes != null;

        if (GUILayout.Button("Apply Clothes"))
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Apply Clothes");
            var undoID = Undo.GetCurrentGroup();

            ApplyBones(clothes.transform, armature.transform);

            Undo.CollapseUndoOperations(undoID);
        }
    }

    private void ApplyBones(Transform clothes, Transform armature)
    {
        foreach (Transform clothesChild in clothes)
        {
            var boneName = clothesChild.name;

            foreach (Transform armatureChild in armature)
            {
                if (boneName.EndsWith(armatureChild.name))
                {
                    CreateParentConstraint(clothesChild, armatureChild);
                    ApplyBones(clothesChild, armatureChild);
                    break;
                }
            }
        }
    }

    private void CreateParentConstraint(Transform obj, Transform target)
    {
        if (obj.gameObject.GetComponent<ParentConstraint>() != null)
        {
            return;
        }

        var parentConstraint = Undo.AddComponent<ParentConstraint>(obj.gameObject);
        parentConstraint.AddSource(new ConstraintSource()
        {
            sourceTransform = target,
            weight = 1
        });

        parentConstraint.constraintActive = true;
    }
}