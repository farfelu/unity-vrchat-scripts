using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.Animations;
using System.Linq;
using VRC.SDK3.Avatars.ScriptableObjects;

public class CreateItemToggle : EditorWindow
{

    [MenuItem("Tools/Create item toggle")]
    static void Init()
    {
        var window = (CreateItemToggle)GetWindow(typeof(CreateItemToggle));
        window.titleContent = new GUIContent("Create item toggle");
        window.Show();
    }

    private GameObject Avatar { get; set; }
    private AnimatorController Controller { get; set; }
    private VRCExpressionParameters Parameters { get; set; }

    private GameObject Item { get; set; }
    private string ItemParameter { get; set; }

    private void OnGUI()
    {
        var newAvatar = (GameObject)EditorGUILayout.ObjectField("Avatar", Avatar, typeof(GameObject), true);

        // just reset the body mesh and controller if the selection changes
        if (Avatar != newAvatar)
        {
            Controller = null;
        }

        Avatar = newAvatar;

        if (Avatar != null)
        {
            if (Controller == null)
            {
                var vrcDescriptor = Avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
                Parameters = vrcDescriptor?.expressionParameters;
                var fxLayer = vrcDescriptor?.baseAnimationLayers.SingleOrDefault(x => x.type == VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.AnimLayerType.FX);
                Controller = fxLayer?.animatorController as AnimatorController;
            }
        }

        GUI.enabled = false;
        Parameters = (VRCExpressionParameters)EditorGUILayout.ObjectField("Parameters", Parameters, typeof(VRCExpressionParameters), false);
        Controller = (AnimatorController)EditorGUILayout.ObjectField("FX controller", Controller, typeof(AnimatorController), false);

        EditorGUILayout.Space();

        GUI.enabled = true;
        Item = (GameObject)EditorGUILayout.ObjectField("Item to toggle", Item, typeof(GameObject), true);
        if (Item != null && (ItemParameter == null || ItemParameter.Length == 0))
        {
            ItemParameter = string.Concat(Item.name.Where(x => char.IsLetterOrDigit(x)));
        }
        ItemParameter = EditorGUILayout.TextField("Parameter name", ItemParameter);

        GUI.enabled = Avatar != null
            && Controller != null
            && Parameters != null
            && Item != null
            && ItemParameter != null
            && ItemParameter.Length > 0;

        if (GUILayout.Button("Create item toggle"))
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Create item toggle");
            var undoID = Undo.GetCurrentGroup();

            CreateToggle();

            Undo.CollapseUndoOperations(undoID);
        }
    }

    private void CreateToggle()
    {
        // if the object is not a child of the avatar, abort
        if (!CheckIfChild())
        {
            EditorUtility.DisplayDialog("Error", "Item \"" + Item.name + "\" must be a child of the Avatar.", "Ok");
            return;
        }

        // if any parameters already exist that aren't bools warn the user
        if (CheckParameters())
        {
            if (!EditorUtility.DisplayDialog("Error setting up parameters", "Parameter \"" + ItemParameter + "\" already exists.\nOverwrite it?", "Overwrite", "Cancel"))
            {
                return;
            }
        }

        AddParameters();
        SetupController();

        Item.gameObject.SetActive(false);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private bool CheckIfChild()
    {
        var parent = Item.transform.parent;

        do
        {
            if (parent.gameObject == Avatar)
            {
                return true;
            }

            parent = parent.parent;

        } while (parent != null);

        return false;
    }

    private bool CheckParameters()
    {
        return Parameters.parameters.Any(x => x.name == ItemParameter && x.valueType != VRCExpressionParameters.ValueType.Bool)
            || Controller.parameters.Any(x => x.name == ItemParameter && x.type != AnimatorControllerParameterType.Bool);
    }

    private void AddParameters()
    {
        // if already exists, overwrite it
        var existingParameter = Parameters.parameters.SingleOrDefault(x => x.name == ItemParameter);
        if (existingParameter != null)
        {
            existingParameter.saved = true;
            existingParameter.defaultValue = 0.0f;
            existingParameter.valueType = VRCExpressionParameters.ValueType.Bool;
        }
        else
        {
            ArrayUtility.Add(ref Parameters.parameters, new VRCExpressionParameters.Parameter()
            {
                name = ItemParameter,
                saved = true,
                defaultValue = 0.0f,
                valueType = VRCExpressionParameters.ValueType.Bool
            });
        }

        EditorUtility.SetDirty(Parameters);
    }

    private void SetupController()
    {
        // if a boolean parameter already exists, keep it
        var existingParameter = Controller.parameters.SingleOrDefault(x => x.name == ItemParameter);
        if (existingParameter != null && existingParameter.type != AnimatorControllerParameterType.Bool)
        {
            Controller.RemoveParameter(existingParameter);
            existingParameter = null;
        }

        if (existingParameter == null)
        {
            Controller.AddParameter(ItemParameter, AnimatorControllerParameterType.Bool);
        }


        var layerName = "toggle" + ItemParameter;

        // remove layer if it already exists
        for (var i = 0; i < Controller.layers.Length; i++)
        {
            var layer = Controller.layers[i];
            if (layer.name == layerName)
            {
                // otherwise it will always keep a reference to the stupid blendtree in the file and never remove it fully
                // removing a layer does NOT clean up the referenced blendtrees/states in it
                foreach (var state in layer.stateMachine.states.ToArray())
                {
                    if (state.state.motion != null)
                    {
                        AssetDatabase.RemoveObjectFromAsset(state.state.motion);
                    }
                    state.state.motion = null;
                    layer.stateMachine.RemoveState(state.state);
                }
                if (layer.stateMachine.defaultState != null)
                {
                    AssetDatabase.RemoveObjectFromAsset(layer.stateMachine.defaultState);
                }
                layer.stateMachine.defaultState = null;
                layer.stateMachine.states = null;
                if (layer.stateMachine != null)
                {
                    AssetDatabase.RemoveObjectFromAsset(layer.stateMachine);
                }

                Controller.RemoveLayer(i);
                break;
            }
        }

        var newLayer = new AnimatorControllerLayer()
        {
            name = layerName,
            stateMachine = new AnimatorStateMachine()
            {
                name = layerName,
                hideFlags = HideFlags.HideInHierarchy
            },
            defaultWeight = 1.0f
        };

        Controller.AddLayer(newLayer);

        var disabledClip = CreateClip(false);
        var enabledClip = CreateClip(true);

        // add item disabled state
        var disabledState = newLayer.stateMachine.AddState(ItemParameter + " disabled", new Vector3(280.0f, 120.0f));
        disabledState.hideFlags = HideFlags.HideInHierarchy;
        disabledState.writeDefaultValues = true;
        disabledState.motion = disabledClip;


        var enabledState = newLayer.stateMachine.AddState(ItemParameter + " enabled", new Vector3(550.0f, 120.0f));
        enabledState.hideFlags = HideFlags.HideInHierarchy;
        enabledState.writeDefaultValues = true;
        enabledState.motion = enabledClip;

        var enableTransition = disabledState.AddTransition(enabledState, false);
        enableTransition.hideFlags = HideFlags.HideInHierarchy;
        enableTransition.hasExitTime = false;
        enableTransition.duration = 0.0f;
        enableTransition.AddCondition(AnimatorConditionMode.If, 1.0f, ItemParameter);

        var disableTransition = enabledState.AddTransition(disabledState, false);
        disableTransition.hideFlags = HideFlags.HideInHierarchy;
        disableTransition.hasExitTime = false;
        disableTransition.duration = 0.0f;
        disableTransition.AddCondition(AnimatorConditionMode.IfNot, 1.0f, ItemParameter);


        EditorUtility.SetDirty(disabledClip);
        EditorUtility.SetDirty(enabledClip);
        EditorUtility.SetDirty(disabledState);
        EditorUtility.SetDirty(enabledState);
        EditorUtility.SetDirty(newLayer.stateMachine);

        // make unity actually save it
        AssetDatabase.AddObjectToAsset(disabledState, Controller);
        AssetDatabase.AddObjectToAsset(enabledState, Controller);
        AssetDatabase.AddObjectToAsset(disableTransition, Controller);
        AssetDatabase.AddObjectToAsset(enableTransition, Controller);
        AssetDatabase.AddObjectToAsset(newLayer.stateMachine, Controller);

        EditorUtility.SetDirty(Controller);
    }

    private AnimationClip CreateClip(bool state)
    {
        var path = "Assets/ItemToggles/" + ItemParameter + "/";

        var itemParent = PrefabUtility.GetCorrespondingObjectFromSource(Item);
        if (itemParent != null)
        {
            path = AssetDatabase.GetAssetPath(itemParent) ?? path;
            path = System.IO.Path.GetDirectoryName(path).Replace("\\", "/") + "/";
        }

        System.IO.Directory.CreateDirectory(path);

        var fileName = ItemParameter + "_" + (state ? "enabled" : "disabled");
        var filePath = path + fileName + ".anim";

        // if the file already exists, load it so we don't recreate the UID if it is already used somewhere
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(filePath) ?? new AnimationClip()
        {
            name = fileName
        };
        clip.ClearCurves();

        var floatState = state ? 1.0f : 0.0f;

        var pathParts = new List<string>();

        var parent = Item.transform.parent;
        if (parent.gameObject != Avatar)
        {
            do
            {
                pathParts.Add(parent.name);
                parent = parent.parent;
            } while (parent.gameObject != Avatar);
        }

        pathParts.Reverse();
        pathParts.Add(Item.name);

        var fullPath = string.Join("/", pathParts);

        var curveBinding = new EditorCurveBinding() { path = fullPath, type = typeof(GameObject), propertyName = "m_IsActive" };
        var curve = AnimationCurve.Linear(0.0f, floatState, 1.0f / 60.0f, floatState);

        clip.SetCurve(curveBinding.path, curveBinding.type, curveBinding.propertyName, curve);
        AnimationUtility.SetEditorCurve(clip, curveBinding, curve);

        if (!System.IO.File.Exists(filePath))
        {
            AssetDatabase.CreateAsset(clip, filePath);
        }

        return clip;
    }
}
