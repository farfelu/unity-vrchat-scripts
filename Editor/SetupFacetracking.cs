using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.Animations;
using System.Linq;
using VRC.SDK3.Avatars.ScriptableObjects;

public class SetupFacetracking : EditorWindow
{
    // preset for the Rexouium
    private static readonly TrackingSetup[] LayerSetup = new TrackingSetup[] {
        new TrackingSetup("JawOpen") {
            Single = "MouthOpen"
        },
        new TrackingSetup("JawX") {
            Left = "JawLeft",
            Right = "JawRight"
        },
        new TrackingSetup("TongueX", "TongueY") {
            Top = "TongueUp",
            TopRight = "TongueUpRight",
            Right = "TongueRight",
            BottomRight = "TongueDownRight",
            Bottom = "TongueDown",
            BottomLeft = "TongueDownLeft",
            Left = "TongueLeft",
            TopLeft = "TongueUpLeft"
        },
        new TrackingSetup("TongueLongStep1") {
            Single = "TongueExtend"
        },
        new TrackingSetup("MouthUpperUpLeftUpperInside") {
            Left = "UpperLipIn",
            Right = "LipCornerTopUp_L"
        },
        new TrackingSetup("MouthUpperUpRightUpperInside") {
            Left = "UpperLipIn",
            Right = "UpperLipRight"
        },
        new TrackingSetup("MouthUpper") {
            Left = "UpperLipLeft",
            Right = "UpperLipRight"
        },
        new TrackingSetup("MouthLower") {
            Left = "LowerLipLeft",
            Right = "LowerLipRight"
        },
        new TrackingSetup("MouthPout") {
            Single = "Pout_Pucker"
        },
        new TrackingSetup("SmileSadRight") {
            Left = "Grin_R",
            Right = "Frown_R"
        },
        new TrackingSetup("SmileSadLeft") {
            Left = "Grin_L",
            Right = "Frown_L"
        },
        new TrackingSetup("PuffSuckRight") {
            Left = "CheeksIn_R",
            Right = "CheekPuff_R"
        },
        new TrackingSetup("PuffSuckLeft") {
            Left = "CheeksIn_L",
            Right = "CheekPuff_L"
        }
    };

    [MenuItem("Tools/Set up facetracking")]
    static void Init()
    {
        var window = (SetupFacetracking)GetWindow(typeof(SetupFacetracking));
        window.titleContent = new GUIContent("Set up face tracking");
        window.Show();
    }

    private GameObject Avatar { get; set; }
    private SkinnedMeshRenderer Body { get; set; }
    private AnimatorController Controller { get; set; }
    private VRCExpressionParameters Parameters { get; set; }

    private void OnGUI()
    {
        var newAvatar = (GameObject)EditorGUILayout.ObjectField("Avatar", Avatar, typeof(GameObject), true);

        // just reset the body mesh and controller if the selection changes
        if (Avatar != newAvatar)
        {
            Body = null;
            Controller = null;
        }

        Avatar = newAvatar;

        if (Avatar != null)
        {
            if (Body == null)
            {
                var bodyTransform = Avatar.transform.Find("Body");
                Body = bodyTransform?.GetComponent<SkinnedMeshRenderer>();
            }

            if (Controller == null)
            {
                var vrcDescriptor = Avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
                Parameters = vrcDescriptor?.expressionParameters;
                var fxLayer = vrcDescriptor?.baseAnimationLayers.SingleOrDefault(x => x.type == VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.AnimLayerType.FX);
                Controller = fxLayer?.animatorController as AnimatorController;
            }
        }

        GUI.enabled = false;
        Body = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("Body mesh", Body, typeof(SkinnedMeshRenderer), true);
        Parameters = (VRCExpressionParameters)EditorGUILayout.ObjectField("Parameters", Parameters, typeof(VRCExpressionParameters), false);
        Controller = (AnimatorController)EditorGUILayout.ObjectField("FX controller", Controller, typeof(AnimatorController), false);

        GUI.enabled = Avatar != null
            && Body != null
            && Controller != null
            && Parameters != null
            && Body.GetComponent<SkinnedMeshRenderer>() != null;

        if (GUILayout.Button("Set up facetracking"))
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Set up facetracking");
            var undoID = Undo.GetCurrentGroup();

            Setup();

            Undo.CollapseUndoOperations(undoID);
        }
    }

    private struct TrackingSetup
    {
        public string[] Parameters { get; set; }

        public string Single { get; set; }

        public string Top { get; set; }
        public string TopRight { get; set; }
        public string Right { get; set; }
        public string BottomRight { get; set; }
        public string Bottom { get; set; }
        public string BottomLeft { get; set; }
        public string Left { get; set; }
        public string TopLeft { get; set; }

        // probably better with attributes, but I don't know Unity
        public Vector2 Vectorize(string property)
        {
            switch (property)
            {
                case "Top":
                    return new Vector2(0.0f, 1.0f);
                case "TopRight":
                    return new Vector2(1.0f, 1.0f);
                case "Right":
                    return new Vector2(1.0f, 0.0f);
                case "BottomRight":
                    return new Vector2(1.0f, -1.0f);
                case "Bottom":
                    return new Vector2(0.0f, -1.0f);
                case "BottomLeft":
                    return new Vector2(-1.0f, -1.0f);
                case "Left":
                    return new Vector2(-1.0f, 0.0f);
                case "TopLeft":
                    return new Vector2(-1.0f, 1.0f);
                default:
                    return Vector2.zero;
            }
        }

        public TrackingSetup(params string[] parameters) : this()
        {
            Parameters = parameters;
        }

        // just to make it easier later
        // has no fail checks
        public string this[string property]
        {
            get { return (string)GetType().GetProperty(property).GetValue(this); }
        }
    }

    private struct ClipSettings
    {
        public string Blendshape { get; set; }
        public float Value { get; set; }

        public ClipSettings(string blendshape, float value)
        {
            Blendshape = blendshape;
            Value = value;
        }
    }

    private void Setup()
    {
        SetupParameters();
        SetupController();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private void SetupParameters()
    {
        // get all the parameters we need to add
        var parameters = LayerSetup.SelectMany(x => x.Parameters);

        // easier to just convert to a generic than handling an array
        var list = Parameters.parameters.ToList();

        // add parameters at the top for higher priority
        var idx = 0;
        foreach (var parameter in parameters)
        {
            // remove if exists
            list.RemoveAll(x => x.name == parameter);

            list.Insert(idx++, new VRCExpressionParameters.Parameter()
            {
                name = parameter,
                saved = false,
                defaultValue = 0.0f,
                valueType = VRCExpressionParameters.ValueType.Float
            });
        }

        Parameters.parameters = list.ToArray();

        EditorUtility.SetDirty(Parameters);
    }

    private void SetupController()
    {
        foreach (var setup in LayerSetup)
        {
            // add layer Parameters
            foreach (var param in setup.Parameters)
            {
                if (!Controller.parameters.Any(x => x.name == param))
                {
                    Controller.AddParameter(param, AnimatorControllerParameterType.Float);
                }
            }

            // used for the animation filename
            var parameterNames = string.Join("-", setup.Parameters);
            var layerName = "FT_" + parameterNames;

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
                        var bt = state.state.motion as BlendTree;
                        while (bt != null && bt.children.Length > 0)
                        {
                            bt.RemoveChild(0);
                        }
                        AssetDatabase.RemoveObjectFromAsset(bt);
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

            // add a new blendtree as default state
            var defaultState = newLayer.stateMachine.AddState(parameterNames + " Blendtree", new Vector3(280.0f, 120.0f));
            defaultState.hideFlags = HideFlags.HideInHierarchy;
            defaultState.writeDefaultValues = true;
            var blendTree = CreateBlendTree(setup);
            if (blendTree == null)
            {
                return;
            }
            defaultState.motion = blendTree;

            EditorUtility.SetDirty(blendTree);
            EditorUtility.SetDirty(newLayer.stateMachine);
            EditorUtility.SetDirty(defaultState);

            // make unity actually save it
            AssetDatabase.AddObjectToAsset(blendTree, Controller);
            AssetDatabase.AddObjectToAsset(defaultState, Controller);
            AssetDatabase.AddObjectToAsset(newLayer.stateMachine, Controller);
        }

        EditorUtility.SetDirty(Controller);
    }

    private BlendTree CreateBlendTree(TrackingSetup setup)
    {
        var blendTree = new BlendTree()
        {
            name = string.Join("-", setup.Parameters) + " Blendtree",
            useAutomaticThresholds = false,
            blendParameter = setup.Parameters[0],
            blendType = BlendTreeType.Simple1D,
            hideFlags = HideFlags.HideInHierarchy
        };

        // more than 1 parameter means 2D blend tree
        if (setup.Parameters.Length > 1)
        {
            blendTree.blendParameterY = setup.Parameters[1];
            blendTree.blendType = BlendTreeType.SimpleDirectional2D;
        }

        // if single is set, then we animate from 0 to 1
        if (setup.Single != null)
        {
            var clip0 = CreateClip("0", new ClipSettings(setup.Single, 0.0f));
            var clip1 = CreateClip("1", new ClipSettings(setup.Single, 1.0f));

            blendTree.AddChild(clip0, 0.0f);
            blendTree.AddChild(clip1, 1.0f);
        }
        else // more than one we place them wherever they need to be
        {
            // to simplify, just loop over the properties
            // there's probably a nicer way to do this
            var properties = new string[] { "Top", "TopRight", "Right", "BottomRight", "Bottom", "BottomLeft", "Left", "TopLeft" };

            // create a neutral animation clip zeroing all blendshapes

            var neutralBlendshapes = properties
                .Select(x => setup[x])
                .Where(x => x != null)
                .Select(x => new ClipSettings(x, 0.0f))
                .ToArray();

            var neutralClip = CreateClip("neutral", neutralBlendshapes);

            // for a 1D blendtree take Left and Right properties to animate from -1 to 1
            if (blendTree.blendType == BlendTreeType.Simple1D)
            {
                if (setup.Left != null)
                {
                    var leftClip = CreateClip("Left", new ClipSettings(setup.Left, 1.0f));
                    blendTree.AddChild(leftClip, -1.0f);
                }

                blendTree.AddChild(neutralClip, 0.0f);

                if (setup.Right != null)
                {
                    var rightClip = CreateClip("Right", new ClipSettings(setup.Right, 1.0f));
                    blendTree.AddChild(rightClip, 1.0f);
                }
            }
            else
            {
                blendTree.AddChild(neutralClip, Vector2.zero);

                // create 2D blendtree
                foreach (var prop in properties)
                {
                    if (setup[prop] != null)
                    {
                        var clip = CreateClip(prop, new ClipSettings(setup[prop], 1.0f));
                        blendTree.AddChild(clip, setup.Vectorize(prop));
                    }
                }
            }
        }

        return blendTree;
    }

    private AnimationClip CreateClip(string position, params ClipSettings[] blendshapes)
    {
        var path = "Assets/FaceTracking/Animations/";
        System.IO.Directory.CreateDirectory(path);

        var name = "FT_" + string.Join("-", blendshapes.Select(x => x.Blendshape));
        var filePath = path + name + "_" + position + ".anim";

        // try mapping given blendshapes to actual blendshapes on the body
        var blendShapes = new ClipSettings[blendshapes.Length];

        for (var i = 0; i < blendshapes.Length; i++)
        {
            var blendShape = SearchBlendshape(Body, blendshapes[i].Blendshape);

            if (blendShape == null)
            {
                EditorUtility.DisplayDialog("Error setting up face tracking", "Could not find Blendshape \"" + blendshapes[i].Blendshape + "\" on \"" + Body.name + "\"", "Ok");
                return null;
            }

            blendShapes[i] = new ClipSettings(blendShape, blendshapes[i].Value);
        }

        // if the file already exists, load it so we don't recreate the UID if it is already used somewhere
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(filePath) ?? new AnimationClip();
        clip.ClearCurves();

        foreach (var blendShape in blendShapes)
        {
            var curveBinding = new EditorCurveBinding() { path = Body.name, type = typeof(SkinnedMeshRenderer), propertyName = "blendShape." + blendShape.Blendshape };
            var curve = AnimationCurve.Linear(0.0f, blendShape.Value, 1.0f / 60, blendShape.Value);

            clip.SetCurve(curveBinding.path, curveBinding.type, curveBinding.propertyName, curve);
            AnimationUtility.SetEditorCurve(clip, curveBinding, curve);
        }

        if (!System.IO.File.Exists(filePath))
        {
            AssetDatabase.CreateAsset(clip, filePath);
        }

        return clip;
    }

    private string SearchBlendshape(SkinnedMeshRenderer meshRenderer, string name)
    {
        var blendshapeCount = meshRenderer.sharedMesh.blendShapeCount;

        for (var i = 0; i < blendshapeCount; i++)
        {
            var blendShape = meshRenderer.sharedMesh.GetBlendShapeName(i);

            if (blendShape.ToLower().EndsWith(name.ToLower()))
            {
                return blendShape;
            }
        }

        return null;
    }
}
