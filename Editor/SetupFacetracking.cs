using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.Animations;
using System.Linq;
using VRC.SDK3.Avatars.ScriptableObjects;

public class SetupFacetracking : EditorWindow
{
    // preset for the Rexouium
    private static readonly TrackingSetup[] Setups = new TrackingSetup[] {
        new TrackingSetup("Rexouium") {
            Options = new TrackingOption[] {
                new TrackingOption("JawOpen") {
                    Single = new BW("MouthOpen")
                },
                new TrackingOption("JawX") {
                    Left = new BW("JawLeft"),
                    Right = new BW("JawRight")
                },
                new TrackingOption("TongueX", "TongueY") {
                    Top = new BW(1.25f, "TongueUp"),
                    TopRight = new BW(1.25f, "TongueUpRight"),
                    Right = new BW(1.25f, "TongueRight"),
                    BottomRight = new BW(1.25f, "TongueDownRight"),
                    Bottom = new BW(1.25f, "TongueDown"),
                    BottomLeft = new BW(1.25f, "TongueDownLeft"),
                    Left = new BW(1.25f, "TongueLeft"),
                    TopLeft = new BW(1.25f, "TongueUpLeft")
                },
                new TrackingOption("TongueLongStep1") {
                    Single = new BW(2.0f, "TongueExtend")
                },
                new TrackingOption("MouthUpperUpLeftUpperInside") {
                    Left = new BW("UpperLipIn"),
                    Right = new BW(0.5f, "LipCornerTopUp_L")
                },
                new TrackingOption("MouthUpperUpRightUpperInside") {
                    Left = new BW("UpperLipIn"),
                    Right = new BW(0.5f, "LipCornerTopUp_R")
                },
                new TrackingOption("MouthUpper") {
                    Left = new BW("UpperLipLeft"),
                    Right = new BW("UpperLipRight")
                },
                new TrackingOption("MouthLower") {
                    Left = new BW("LowerLipLeft"),
                    Right = new BW("LowerLipRight")
                },
                new TrackingOption("MouthPout") {
                    Single = new BW(1.25f, "Pout_Pucker")
                },
                new TrackingOption("SmileSadRight") {
                    Left = new BW("Frown_R"),
                    Right = new BW(0.5f, "Grin_R")
                },
                new TrackingOption("SmileSadLeft") {
                    Left = new BW("Frown_L"),
                    Right = new BW(0.5f, "Grin_L")
                },
                new TrackingOption("PuffSuckRight") {
                    Left = new BW("CheeksIn_R"),
                    Right = new BW("CheekPuff_R")
                },
                new TrackingOption("PuffSuckLeft") {
                    Left = new BW("CheeksIn_L"),
                    Right = new BW("CheekPuff_L")
                }
            }
        },
        new TrackingSetup("Nardoragon") {
            Options = new TrackingOption[] {
                new TrackingOption("JawX") {
                    Left = new BW("Jaw_Left"),
                    Right = new BW("Jaw_Right")
                },
                new TrackingOption("MouthUpper") {
                    Left = new BW("Mouth_Upper_Left"),
                    Right = new BW("Mouth_Upper_Right")
                },
                new TrackingOption("MouthLower") {
                    Left = new BW("Mouth_Lower_Left"),
                    Right = new BW("Mouth_Lower_Right")
                },
                new TrackingOption("SmileSadRight") {
                    Left = new BW("Mouth_Sad_Right"),
                    Right = new BW("Mouth_Smile_Right")
                },
                new TrackingOption("SmileSadLeft") {
                    Left = new BW("Mouth_Sad_Left"),
                    Right = new BW("Mouth_Smile_Left")
                },
                new TrackingOption("TongueY") {
                    Left = new BW("Tongue_Down"),
                    Right = new BW("Tongue_Up")
                },
                new TrackingOption("TongueX") {
                    Left = new BW("Tongue_Left"),
                    Right = new BW("Tongue_Right")
                },
                new TrackingOption("TongueRoll") {
                    Single = new BW("Tongue_Roll")
                },
                new TrackingOption("TongueSteps") {
                    Left = new BW(1.0f),
                    Center = new BW("Tongue_LongStep1"),
                    Right = new BW("Tongue_LongStep2")
                },
                new TrackingOption("PuffSuckRight") {
                    Left = new BW("Cheek_Suck"),
                    Right = new BW("Cheek_Puff_Right")
                },
                new TrackingOption("PuffSuckLeft") {
                    Left = new BW("Cheek_Suck"),
                    Right = new BW("Cheek_Puff_Left")
                },
                new TrackingOption("JawOpenApe") {
                    Left = new BW("Mouth_Ape_Shape"),
                    Right = new BW("Jaw_Open")
                },
                new TrackingOption("MouthUpperUpRightUpperInside") {
                    Left = new BW("Mouth_Upper_Inside"),
                    Right = new BW("Mouth_Upper_UpRight")
                },
                new TrackingOption("MouthUpperUpLeftUpperInside") {
                    Left = new BW("Mouth_Upper_Inside"),
                    Right = new BW("Mouth_Upper_UpLeft")
                },
                new TrackingOption("MouthLowerDownRightLowerInside") {
                    Left = new BW("Mouth_Lower_Inside"),
                    Right = new BW("Mouth_Lower_DownRight")
                },
                new TrackingOption("MouthLowerDownLeftLowerInside") {
                    Left = new BW("Mouth_Lower_Inside"),
                    Right = new BW("Mouth_Lower_DownLeft")
                },
                new TrackingOption("MouthPout") {
                    Single = new BW("Mouth_Pout")
                },
                new TrackingOption("MouthUpperOverturn") {
                    Single = new BW("Mouth_Upper_Overturn")
                },
                new TrackingOption("MouthLowerOverturn") {
                    Single = new BW("Mouth_Lower_Overturn")
                }
            }
        }
    };

    [MenuItem("Tools/Set up facetracking")]
    static void Init()
    {
        var window = (SetupFacetracking)GetWindow(typeof(SetupFacetracking));
        window.titleContent = new GUIContent("Set up face tracking");
        window.Show();
    }

    private static readonly string[] SetupNames = Setups.Select(x => x.Name + " (" + x.Options.Length * 8 + " bits)").ToArray();
    private int SetupIndex { get; set; } = 0;
    private TrackingOption[] SelectedOptions => Setups[SetupIndex].Options;

    private GameObject Avatar { get; set; }
    private SkinnedMeshRenderer Body { get; set; }
    private AnimatorController Controller { get; set; }
    private VRCExpressionParameters Parameters { get; set; }
    private bool AddToggle { get; set; }

    private const string ToggleName = "FT_Enabled";

    private void OnGUI()
    {
        SetupIndex = EditorGUILayout.Popup("Setup template", SetupIndex, SetupNames);

        EditorGUILayout.Space();

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

        GUI.enabled = true;


        EditorGUILayout.HelpBox("The toggle \"" + ToggleName + "\" will always be added to the controller and can be optionally added to the menu to toggle it ingame\nTo disable face tracking set the \"" + ToggleName + "\" parameter to false in the controller", MessageType.None);

        EditorGUILayout.Space();

        AddToggle = (bool)EditorGUILayout.ToggleLeft("Add toggle to menu parameters", AddToggle);

        if (AddToggle)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Toggle has to be added to the menu manually.\nAdd a toggle for the \"" + ToggleName + "\" parameter to the menu", MessageType.Warning);
        }

        GUI.enabled = Avatar != null
            && Body != null
            && Controller != null
            && Parameters != null
            && Body.GetComponent<SkinnedMeshRenderer>() != null;

        EditorGUILayout.Space();

        if (GUILayout.Button("Set up facetracking"))
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Set up facetracking");
            var undoID = Undo.GetCurrentGroup();

            Setup();

            Undo.CollapseUndoOperations(undoID);
        }
    }

    public struct BW // blendshape weight
    {
        public string[] Blendshapes { get; set; }
        public float Weight { get; set; }

        public BW(params string[] blendshapes)
        {
            Blendshapes = blendshapes;
            Weight = 1.0f;
        }

        public BW(float weight, params string[] blendshapes)
        {
            Weight = weight;
            Blendshapes = blendshapes;
        }
    }

    private class TrackingSetup
    {
        public string Name { get; set; }
        public TrackingOption[] Options { get; set; }

        public TrackingSetup(string name)
        {
            Name = name;
            Options = null;
        }
    }

    private struct TrackingOption
    {
        public string[] Parameters { get; set; }

        public BW? Single { get; set; }

        public BW? Top { get; set; }
        public BW? TopRight { get; set; }
        public BW? Right { get; set; }
        public BW? BottomRight { get; set; }
        public BW? Bottom { get; set; }
        public BW? BottomLeft { get; set; }
        public BW? Left { get; set; }
        public BW? TopLeft { get; set; }
        public BW? Center { get; set; }

        // probably better with attributes, but I don't know Unity
        public Vector2 Vectorize(string property, float weight)
        {
            switch (property)
            {
                case "Top":
                    return new Vector2(0.0f, 1.0f / weight);
                case "TopRight":
                    return new Vector2(1.0f / weight, 1.0f / weight);
                case "Right":
                    return new Vector2(1.0f / weight, 0.0f);
                case "BottomRight":
                    return new Vector2(1.0f / weight, -1.0f / weight);
                case "Bottom":
                    return new Vector2(0.0f, -1.0f / weight);
                case "BottomLeft":
                    return new Vector2(-1.0f / weight, -1.0f / weight);
                case "Left":
                    return new Vector2(-1.0f / weight, 0.0f);
                case "TopLeft":
                    return new Vector2(-1.0f / weight, 1.0f / weight);
                default:
                    return Vector2.zero;
            }
        }

        public TrackingOption(params string[] parameters) : this()
        {
            Parameters = parameters;
        }

        // just to make it easier later
        // has no fail checks
        public BW? this[string property]
        {
            get { return (BW?)GetType().GetProperty(property).GetValue(this); }
        }
    }

    private struct ClipSettings
    {
        public enum BlendshapeState
        {
            Off,
            On
        }

        public string[] Blendshapes { get; set; }
        public BlendshapeState State { get; set; }

        public float Value => State == BlendshapeState.On ? 100.0f : 0.0f;

        public ClipSettings(string blendshape, BlendshapeState state) : this(new[] { blendshape }, state)
        {

        }

        public ClipSettings(string[] blendshapes, BlendshapeState state)
        {
            Blendshapes = blendshapes;
            State = state;
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
        var parameters = SelectedOptions.SelectMany(x => x.Parameters);

        // easier to just convert to a generic than handling an array
        var list = Parameters.parameters.ToList();

        // add parameters
        foreach (var parameter in parameters)
        {
            // remove if exists
            list.RemoveAll(x => x.name == parameter);

            list.Add(new VRCExpressionParameters.Parameter()
            {
                name = parameter,
                saved = false,
                defaultValue = 0.0f,
                valueType = VRCExpressionParameters.ValueType.Float
            });
        }

        if (AddToggle && !list.Any(x => x.name == ToggleName))
        {
            list.Add(new VRCExpressionParameters.Parameter()
            {
                name = ToggleName,
                saved = true,
                defaultValue = 1.0f,
                valueType = VRCExpressionParameters.ValueType.Bool
            });
        }

        Parameters.parameters = list.ToArray();

        EditorUtility.SetDirty(Parameters);
    }

    private void SetupController()
    {
        foreach (var option in SelectedOptions)
        {
            // add layer Parameters
            foreach (var param in option.Parameters)
            {
                if (!Controller.parameters.Any(x => x.name == param))
                {
                    Controller.AddParameter(param, AnimatorControllerParameterType.Float);
                }
            }

            // add toggle parameter, default to true
            if (!Controller.parameters.Any(x => x.name == ToggleName))
            {
                Controller.AddParameter(new AnimatorControllerParameter()
                {
                    name = ToggleName,
                    type = AnimatorControllerParameterType.Bool,
                    defaultBool = true
                });
            }

            // used for the animation filename
            var parameterNames = string.Join("-", option.Parameters);
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
                        // remove animationsclips
                        var motion = state.state.motion;
                        if (motion is BlendTree)
                        {
                            var bt = motion as BlendTree;
                            while (bt.children.Length > 0)
                            {
                                bt.RemoveChild(0);
                            }
                        }

                        // remove transitions
                        foreach (var transition in state.state.transitions)
                        {
                            AssetDatabase.RemoveObjectFromAsset(transition);
                            state.state.RemoveTransition(transition);
                        }

                        if (motion != null)
                        {
                            AssetDatabase.RemoveObjectFromAsset(motion);
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

            // add toggle state to check if face tracking is on or off
            var toggleState = newLayer.stateMachine.AddState("disabled", new Vector3(280.0f, 120.0f));
            toggleState.hideFlags = HideFlags.HideInHierarchy;
            toggleState.writeDefaultValues = true;

            // add a new blendtree as default state
            var blendTree = CreateBlendTree(option);
            if (blendTree == null)
            {
                return;
            }

            var blendtreeState = newLayer.stateMachine.AddState(blendTree.name, new Vector3(280.0f, 250.0f));
            blendtreeState.hideFlags = HideFlags.HideInHierarchy;
            blendtreeState.writeDefaultValues = true;
            blendtreeState.motion = blendTree;

            // add transition with toggle condition between toggle state and blendtree state
            var onTransition = toggleState.AddTransition(blendtreeState, false);
            onTransition.hideFlags = HideFlags.HideInHierarchy;
            onTransition.hasExitTime = false;
            onTransition.duration = 0.0f;
            onTransition.AddCondition(AnimatorConditionMode.If, 1.0f, ToggleName);

            var offTransition = blendtreeState.AddTransition(toggleState, false);
            offTransition.hideFlags = HideFlags.HideInHierarchy;
            offTransition.hasExitTime = false;
            offTransition.duration = 0.0f;
            offTransition.AddCondition(AnimatorConditionMode.IfNot, 1.0f, ToggleName);

            EditorUtility.SetDirty(toggleState);
            EditorUtility.SetDirty(blendTree);
            EditorUtility.SetDirty(newLayer.stateMachine);
            EditorUtility.SetDirty(blendtreeState);

            // make unity actually save it
            AssetDatabase.AddObjectToAsset(blendTree, Controller);
            AssetDatabase.AddObjectToAsset(toggleState, Controller);
            AssetDatabase.AddObjectToAsset(blendtreeState, Controller);
            AssetDatabase.AddObjectToAsset(onTransition, Controller);
            AssetDatabase.AddObjectToAsset(offTransition, Controller);
            AssetDatabase.AddObjectToAsset(newLayer.stateMachine, Controller);
        }

        EditorUtility.SetDirty(Controller);
    }

    private BlendTree CreateBlendTree(TrackingOption setup)
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
            var clip0 = CreateClip("0", new ClipSettings(setup.Single.Value.Blendshapes, ClipSettings.BlendshapeState.Off));
            var clip1 = CreateClip("1", new ClipSettings(setup.Single.Value.Blendshapes, ClipSettings.BlendshapeState.On));

            blendTree.AddChild(clip0, 0.0f);
            blendTree.AddChild(clip1, 1.0f / setup.Single.Value.Weight);
        }
        else // more than one we place them wherever they need to be
        {
            // to simplify, just loop over the properties
            // there's probably a nicer way to do this
            var properties = new string[] { "Top", "TopRight", "Right", "BottomRight", "Bottom", "BottomLeft", "Left", "TopLeft", "Center" };

            // create a neutral animation clip zeroing all blendshapes

            var positions = properties
                .Select(x => new { Position = x, Setup = setup[x] })
                .Where(x => x.Setup.HasValue)
                .ToArray();

            var neutralBlendshapes = positions
                .Where(x => x.Setup.Value.Blendshapes != null)
                .Select(x => new ClipSettings(x.Setup.Value.Blendshapes, ClipSettings.BlendshapeState.Off))
                .ToArray();

            var neutralClip = CreateClip("neutral", neutralBlendshapes);

            // for a 1D blendtree take Left and Right properties to animate from -1 to 1
            if (blendTree.blendType == BlendTreeType.Simple1D)
            {
                var left = positions.Where(x => x.Position == "Left").SingleOrDefault();
                var center = positions.Where(x => x.Position == "Center").SingleOrDefault();
                var right = positions.Where(x => x.Position == "Right").SingleOrDefault();

                var hasNeutral = positions.Any(x => x.Setup.Value.Blendshapes.Length == 0);

                if (left != null)
                {
                    var clip = left.Setup.Value.Blendshapes.Length == 0 ? neutralClip : CreateClip("Left", new ClipSettings(left.Setup.Value.Blendshapes, ClipSettings.BlendshapeState.On));
                    blendTree.AddChild(clip, -1.0f / left.Setup.Value.Weight);
                }

                if (center != null)
                {
                    var clip = center.Setup.Value.Blendshapes.Length == 0 ? neutralClip : CreateClip("Center", new ClipSettings(center.Setup.Value.Blendshapes, ClipSettings.BlendshapeState.On));
                    blendTree.AddChild(clip, 0.0f);
                }
                else if(!hasNeutral)
                { 
                    blendTree.AddChild(neutralClip, 0.0f);
                }

                if (right != null)
                {
                    var clip = right.Setup.Value.Blendshapes.Length == 0 ? neutralClip : CreateClip("Right", new ClipSettings(right.Setup.Value.Blendshapes, ClipSettings.BlendshapeState.On));
                    blendTree.AddChild(clip, 1.0f / right.Setup.Value.Weight);
                }
            }
            else
            {
                blendTree.AddChild(neutralClip, Vector2.zero);

                // create 2D blendtree
                foreach (var position in positions.Where(x => x.Setup.Value.Blendshapes != null))
                {
                    var clip = CreateClip(position.Position, new ClipSettings(position.Setup.Value.Blendshapes, ClipSettings.BlendshapeState.On));
                    blendTree.AddChild(clip, setup.Vectorize(position.Position, position.Setup.Value.Weight));
                }
            }
        }

        return blendTree;
    }

    private AnimationClip CreateClip(string position, params ClipSettings[] blendshapes)
    {
        var path = "Assets/FaceTracking/Animations/";
        System.IO.Directory.CreateDirectory(path);

        var name = "FT_" + string.Join("-", blendshapes.SelectMany(x => x.Blendshapes));
        var filePath = path + name + "_" + position + ".anim";

        // try mapping given blendshapes to actual blendshapes on the body
        var meshBlendshapes = new ClipSettings[blendshapes.Length];

        for (var i = 0; i < blendshapes.Length; i++)
        {
            var blendshape = blendshapes[i];
            var foundBlendshapes = new string[blendshape.Blendshapes.Length];
            for (var j = 0; j < blendshapes[i].Blendshapes.Length; j++)
            {
                var targetBlendshape = blendshape.Blendshapes[j];
                var foundBlendshape = SearchBlendshape(Body, targetBlendshape);

                if (foundBlendshape == null)
                {
                    EditorUtility.DisplayDialog("Error setting up face tracking", "Could not find Blendshape \"" + targetBlendshape + "\" on \"" + Body.name + "\"", "Ok");
                    return null;
                }

                foundBlendshapes[j] = foundBlendshape;
            }

            meshBlendshapes[i] = new ClipSettings(foundBlendshapes, blendshape.State);
        }

        // if the file already exists, load it so we don't recreate the UID if it is already used somewhere
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(filePath) ?? new AnimationClip();
        clip.ClearCurves();

        foreach (var blendShape in meshBlendshapes)
        {
            foreach (var blendShapeKey in blendShape.Blendshapes)
            {
                var curveBinding = new EditorCurveBinding() { path = Body.name, type = typeof(SkinnedMeshRenderer), propertyName = "blendShape." + blendShapeKey };
                var curve = AnimationCurve.Linear(0.0f, blendShape.Value, 1.0f / 60, blendShape.Value);

                clip.SetCurve(curveBinding.path, curveBinding.type, curveBinding.propertyName, curve);
                AnimationUtility.SetEditorCurve(clip, curveBinding, curve);
            }
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
