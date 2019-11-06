using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using BansheeGz.BGSpline.Curve;
using UnityEditor;
using Object = UnityEngine.Object;

namespace BansheeGz.BGSpline.Editor
{
    public class BGCurveEditorComponents : BGCurveEditorTab
    {
        private const int ConnectorLineAlpha = 100;
        private const int HeaderAlpha = 25;
        private const int HeaderFoldedAlpha = 75;

        private static bool customEditorsOn = true;

        private Texture2D whiteTexture;

        private readonly Tree tree;
        private BGCc[] components;

        public override Texture2D Header2D
        {
            get { return BGBinaryResources.BGComponents123; }
        }

        public BGCurveEditorComponents(BGCurveEditor editor, SerializedObject curveObject) : base(editor, curveObject)
        {
            tree = new Tree(Curve);
        }


        private bool HasError
        {
            get { return AnyComponentHasError(Curve); }
        }

        private bool HasWarning
        {
            get { return AnyComponentHasWarning(Curve); }
        }

        // ================================================================================ Inspector
        public override void OnInspectorGui()
        {
            BGEditorUtility.Assign(ref whiteTexture, () => BGEditorUtility.Texture1X1(Color.white));

            components = Curve.GetComponents<BGCc>();
            var length = components.Length;

            tree.Refresh(components);

            if (tree.InitException != null)
            {
                EditorGUILayout.HelpBox("There was an error initializing editors for component's Tree View: " + tree.InitException.Message +
                                        "\r\n\r\nYou still can use default Unity's editors for components below.", MessageType.Error);
                return;
            }


            var hasError = HasError;
            var hasWarning = HasWarning;

            BGEditorUtility.HorizontalBox(() =>
            {
                EditorGUILayout.LabelField("Components: " + length + " (" + (hasError ? "Error" : "Ok") + ")");

                GUILayout.FlexibleSpace();

                // turn on/off handles
                var handlesOff = BGCurveSettingsForEditor.CcInspectorHandlesOff;
                if (BGEditorUtility.ButtonWithIcon(
                    handlesOff
                        ? BGBinaryResources.BGHandlesOff123
                        : BGBinaryResources.BGHandlesOn123,
                    "Turn on/off handles settings in Inspector"))
                {
                    BGCurveSettingsForEditor.CcInspectorHandlesOff = !BGCurveSettingsForEditor.CcInspectorHandlesOff;
                }
                EditorGUILayout.Separator();

                // turn on/off colored tree
                if (BGEditorUtility.ButtonWithIcon(customEditorsOn ? BGBinaryResources.BGOn123: BGBinaryResources.BGOff123, "Use custom UI for components (colored tree) and hide standard unity editors for components"))
                {
                    customEditorsOn = !customEditorsOn;
                    tree.Refresh(null, true);
                    SceneView.RepaintAll();
                    GUIUtility.ExitGUI();
                }
                EditorGUILayout.Separator();

                if (length > 0)
                {
                    // collapse/expand
                    if (BGEditorUtility.ButtonWithIcon(BGBinaryResources.BGCollapseAll123, "Collapse all components")) tree.ExpandCollapseAll(true);
                    EditorGUILayout.Separator();
                    if (BGEditorUtility.ButtonWithIcon(BGBinaryResources.BGExpandAll123, "Expand all components")) tree.ExpandCollapseAll(false);
                    EditorGUILayout.Separator();


                    // delete all Ccs
                    if (BGEditorUtility.ButtonWithIcon(BGBinaryResources.BGDelete123, "Delete all components")
                        && BGEditorUtility.Confirm("Delete", "Are you sure you want to delete " + length + " component(s)?", "Delete")) tree.Delete();
                    EditorGUILayout.Separator();
                }

                //add new Cc
                if (BGEditorUtility.ButtonWithIcon(BGBinaryResources.BGAdd123, "Add new component")) BGCcAddWindow.Open(Curve, type => AddComponent(Curve, type));
            });


            if (length > 0)
            {
                // warnings/errors
                if (hasWarning || hasError)
                {
                    for (var i = 0; i < components.Length; i++)
                    {
                        var component = components[i];

                        var name = (component.Descriptor != null ? component.Descriptor.Name + " " : "") + component.CcName;

                        var error = component.Error;
                        if (!string.IsNullOrEmpty(error)) BGEditorUtility.HelpBox("Component error [" + name + "]: " + error, MessageType.Error);

                        var warning = component.Warning;
                        if (!string.IsNullOrEmpty(warning)) BGEditorUtility.HelpBox("Component warning [" + name + "]: " + warning, MessageType.Warning);
                    }
                }
                else BGEditorUtility.HelpBox("No warnings or errors", MessageType.Info);

                // tree GUI
                tree.OnInspectorGui();
            }
            else
                EditorGUILayout.HelpBox(
                    "Hit the Plus icon to add a component"
                    + "\r\n"
                    + "\r\n"
                    + "Components allows to add functionality without any scripting."
                    , MessageType.Info);


            if (hasError ^ HasError || hasWarning ^ HasWarning) EditorApplication.RepaintHierarchyWindow();
        }

        public static BGCc AddComponent(BGCurve curve, Type type)
        {
            var newCc = Undo.AddComponent(curve.gameObject, type);
            if (newCc == null) return null;

            var bgCc = ((BGCc) newCc);
            UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(bgCc, true);
            bgCc.AddedInEditor();
            EditorUtility.SetDirty(curve.gameObject);
            return bgCc;
        }

        public override void OnEnable()
        {
            tree.Refresh();
        }

        public override void OnDisable()
        {
            tree.OnDestroy();
        }

        public override void OnSceneGui(Plane[] frustum)
        {
            if (!customEditorsOn) return;

            tree.OnSceneGui();
        }

        public override string GetStickerMessage(ref MessageType type)
        {
            var length = Curve.GetComponents<BGCc>().Length;
            if (length != 0)
            {
                bool hasError = false, hasWarning = false;
                ComponentsStatus(Curve, ref hasError, ref hasWarning);
                type = hasError ? MessageType.Error : hasWarning ? MessageType.Warning : MessageType.None;
            }

            return type != MessageType.None ? "!!" : "" + length;
        }


        public static bool AnyComponentHasError(BGCurve curve)
        {
            var components = curve.GetComponents<BGCc>();
            if (components == null || components.Length == 0) return false;
            return components.Any(t => t.HasError());
        }

        public static bool AnyComponentHasWarning(BGCurve curve)
        {
            var components = curve.GetComponents<BGCc>();
            if (components == null || components.Length == 0) return false;
            return components.Any(t => t.HasWarning());
        }

        public static void ComponentsStatus(BGCurve curve, ref bool hasError, ref bool hasWarning)
        {
            var components = curve.GetComponents<BGCc>();
            if (components == null || components.Length == 0) return;

            foreach (var component in components)
            {
                if (component.HasError()) hasError = true;
                if (component.HasWarning()) hasWarning = true;
            }
        }


        // ================================================================================ Tree
        //tree structure of Cc's
        internal sealed class Tree : BGTreeView<Tree.CcNode>
        {
            private Dictionary<int, Texture2D> level2LinkTexture = GetLevel2LinkTexture();

            private static Dictionary<int, Texture2D> GetLevel2LinkTexture()
            {
                return new Dictionary<int, Texture2D>
                {
                    {0, BGEditorUtility.Texture1X1(new Color32(255, 0, 0, ConnectorLineAlpha))},
                    {1, BGEditorUtility.Texture1X1(new Color32(0, 255, 0, ConnectorLineAlpha))},
                    {2, BGEditorUtility.Texture1X1(new Color32(0, 0, 255, ConnectorLineAlpha))},
                    {3, BGEditorUtility.Texture1X1(new Color32(255, 255, 0, ConnectorLineAlpha))},
                };
            }

            private readonly Dictionary<int, Color> level2Color = new Dictionary<int, Color>
            {
                {0, new Color32(255, 0, 0, HeaderAlpha)},
                {1, new Color32(0, 255, 0, HeaderAlpha)},
                {2, new Color32(0, 0, 255, HeaderAlpha)},
                {3, new Color32(255, 255, 0, HeaderAlpha)},
            };

            private readonly Dictionary<int, Color> level2FoldedColor = new Dictionary<int, Color>
            {
                {0, new Color32(255, 0, 0, HeaderFoldedAlpha)},
                {1, new Color32(0, 255, 0, HeaderFoldedAlpha)},
                {2, new Color32(0, 0, 255, HeaderFoldedAlpha)},
                {3, new Color32(255, 255, 0, HeaderFoldedAlpha)},
            };

            private readonly Texture2D whiteTexture;


            private readonly Dictionary<Type, List<CcNode>> type2NodeList = new Dictionary<Type, List<CcNode>>();

            public BGCurve Curve;
            public BGCc.CcException InitException;

            private int count;

            public Tree(BGCurve curve) : base(new Config(2, 8, 0, 2, 0))
            {
                Curve = curve;

                whiteTexture = BGEditorUtility.Texture1X1(Color.white);
            }

            public void Refresh(BGCc[] components = null, bool force = false)
            {
                if (components == null) components = Curve.GetComponents<BGCc>();

                //it should be enough
                if (count == components.Length && !force) return;

                SetHideFlag(components, customEditorsOn ? HideFlags.HideInInspector : HideFlags.None);

                //Recalc
                var instanceId2Collapsed = new Dictionary<int, bool>();
                //try to preserve expanded/collapsed state
                if (Roots.Count > 0) foreach (var root in Roots) root.FillState(instanceId2Collapsed);

                OnDestroy();
                Roots.Clear();
                type2NodeList.Clear();


                if (!customEditorsOn) return;


                try
                {
                    InitException = null;

                    //try to init custom tree view for components
                    count = components.Length;
                    foreach (var cc in components)
                    {
                        if (BGReflectionAdapter.GetCustomAttributes(cc.GetType(), typeof(BGCc.CcExcludeFromMenu), true).Length > 0) continue;

                        var node = new CcNode(this, cc);

                        if (instanceId2Collapsed.ContainsKey(cc.GetInstanceID())) node.Collapsed = true;

                        var type = cc.GetType();

                        if (!type2NodeList.ContainsKey(type)) type2NodeList[type] = new List<CcNode>();

                        type2NodeList[type].Add(node);
                    }

                    foreach (var list in type2NodeList.Values) foreach (var node in list) if (!node.Processed) node.ProcessStructure();
                }
                catch (BGCc.CcException e)
                {
                    InitException = e;
                    //fallback (show default stuff)
                    SetHideFlag(components, HideFlags.None);
                }
            }

            private static void SetHideFlag(BGCc[] components, HideFlags hideFlags)
            {
                foreach (var component in components) component.hideFlags = hideFlags;
            }


            public CcNode Get(BGCc cc)
            {
                var nodes = type2NodeList[cc.GetType()];

                return nodes.FirstOrDefault(node => node.Cc == cc);
            }

            public void Delete()
            {
                foreach (var root in Roots) root.Delete();

                GUIUtility.ExitGUI();
            }

            public void OnDestroy()
            {
                foreach (var root in Roots) root.OnDestroy();
            }

            public override Texture2D GetLinkTexture(int level)
            {
                if (level2LinkTexture[0] == null) level2LinkTexture = GetLevel2LinkTexture();
                return level2LinkTexture.Count > level ? level2LinkTexture[level] : whiteTexture;
            }

            private Color GetColor(int level, bool collapsed, Func<Color> defaultColor)
            {
                return level2Color.Count > level ? (collapsed ? level2FoldedColor[level] : level2Color[level]) : defaultColor();
            }

            public void OnSceneGui()
            {
                foreach (var root in Roots) root.OnSceneGui();
            }

            public override void OnInspectorGui()
            {
                if (!customEditorsOn)
                {
                    EditorGUILayout.HelpBox("You disabled tree view. Use standard Unity editors below to change parameters.", MessageType.Warning);
                    return;
                }


                try
                {
                    for (var i = 0; i < Roots.Count; i++)
                    {
                        var root = Roots[i];
                        if (i != 0) EditorGUILayout.Separator();
                        root.OnInspectorGui();
                    }
                }
                catch (BGEditorUtility.ExitException)
                {
                    Refresh();
                    GUIUtility.ExitGUI();
                }
            }

            // ================================================================================ Tree Node
            // one cc
            internal sealed class CcNode : BGTreeNode<CcNode>
            {
                public bool Processed;
                //Cc= Curve's component
                public readonly BGCc Cc;
                private readonly BGCc.CcDescriptor descriptor;

                private GUIStyle headerFoldoutStyle;
                private GUIStyle headerFoldoutStyleDisabled;
                private GUIStyle okStyle;
                private GUIStyle errorStyle;


                private readonly BGCcEditor ccEditor;
                private readonly MethodInfo onSceneGuiMethod;
                private readonly Type parentType;
                private GUIStyle headerBoxStyle;


                public override bool Collapsed
                {
                    get { return !UnityEditorInternal.InternalEditorUtility.GetIsInspectorExpanded(Cc); }
                    set { UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(Cc, !value); }
                }

                internal CcNode(Tree tree, BGCc cc) : base(tree)
                {
                    Cc = cc;
                    descriptor = cc.Descriptor;

                    var editor = UnityEditor.Editor.CreateEditor(cc);

                    if (!(editor is BGCcEditor)) throw new BGCc.CcException("Unable to init an Editor for " + cc.GetType() + ": editor does not extend from BGCcEditor.");

                    ccEditor = (BGCcEditor) editor;
                    ccEditor.ChangedParent += ChangedParent;
//                    UnityEditor.Editor.CreateCachedEditor(cc, null, ref ccEditor);

                    onSceneGuiMethod = ccEditor.GetType().GetMethod("OnSceneGUI", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

                    parentType = cc.GetParentClass();
                }

                private Tree MyTree
                {
                    get { return (Tree) Tree; }
                }

                private void ChangedParent(object sender, EventArgs e)
                {
                    MyTree.Refresh(null, true);
                }

                public override void ProcessStructure()
                {
                    if (parentType == null)
                    {
                        MyTree.Roots.Add(this);
                    }
                    else
                    {
                        Parent = MyTree.Get(Cc.GetParent(parentType));
                    }
                    Processed = true;
                }

                //true if exit gui pass
                public override void OnInspectorGuiInternal(int level)
                {
                    BGEditorUtility.Assign(ref okStyle, () => new GUIStyle("Label") {normal = {textColor = new Color32(66, 166, 33, 255)}, fontStyle = FontStyle.Bold});
                    BGEditorUtility.Assign(ref errorStyle, () => new GUIStyle("Label") {normal = {textColor = new Color32(166, 66, 33, 255)}, fontStyle = FontStyle.Bold});
                    BGEditorUtility.Assign(ref headerBoxStyle, () => new GUIStyle {padding = new RectOffset(4, 4, 4, 4)});
                    BGEditorUtility.Assign(ref headerFoldoutStyle, () => new GUIStyle(EditorStyles.foldout) {fontStyle = FontStyle.Bold, clipping = TextClipping.Clip});
                    BGEditorUtility.Assign(ref headerFoldoutStyleDisabled, () => new GUIStyle(headerFoldoutStyle) {normal = {textColor = Color.gray}});

                    var color = MyTree.GetColor(level, Collapsed, () => Color.white);
                    color.a = ConnectorLineAlpha;

                    //colored box
                    BGEditorUtility.SwapGuiColor(color, () => EditorGUILayout.BeginVertical(new GUIStyle("Box")
                    {
                        padding = new RectOffset(),
                        margin = new RectOffset(),
                        border = new RectOffset(4, 4, 4, 4),
                        normal =
                        {
                            background = BGBinaryResources.BGBoxWhite123
                        }
                    }));

                    //header
                    HeaderUi(level, !String.IsNullOrEmpty(Cc.Error));


                    if (!Collapsed && !Cc.Hidden)
                    {
                        BGEditorUtility.VerticalBox(() =>
                        {
                            //show inspector
                            ccEditor.OnInspectorGUI();
                        });
                    }

                    //do not remove it (EditorGUILayout.BeginVertical is a little higher- colored box)
                    EditorGUILayout.EndVertical();
                }

                private void HeaderUi(int level, bool hasError)
                {
                    var color = MyTree.GetColor(level, Collapsed, () => new Color(0, 0, 0, 0));
                    BGEditorUtility.SwapGuiBackgroundColor(color, () =>
                    {
                        BGEditorUtility.Horizontal(headerBoxStyle, () =>
                        {
                            BGEditorUtility.Indent(1, () =>
                            {
                                var content = new GUIContent(descriptor == null ? Cc.GetType().Name : descriptor.Name + " (" + BGEditorUtility.Trim(Cc.CcName, 10) + ")",
                                    descriptor == null ? null : descriptor.Description);
                                var width = headerFoldoutStyle.CalcSize(content).x + 16;

                                BGEditorUtility.SwapLabelWidth((int) width, () =>
                                {
                                    //foldout (we dont use layout version cause it does not support clickin on labels)
                                    Collapsed = EditorGUI.Foldout(
                                        GUILayoutUtility.GetRect(width, 16f),
                                        Collapsed,
                                        content,
                                        true,
                                        Cc.enabled ? headerFoldoutStyle : headerFoldoutStyleDisabled);
                                });
                            });

                            GUILayout.FlexibleSpace();

                            // status(error or Ok)
                            EditorGUI.LabelField(GUILayoutUtility.GetRect(70, 16, EditorStyles.label), hasError ? "Error" : "Ok.", hasError ? errorStyle : okStyle);


                            //help url
                            if (!String.IsNullOrEmpty(Cc.HelpURL))
                            {
                                if (BGEditorUtility.ButtonWithIcon(BGBinaryResources.BGHelp123, "Open help in the browser")) Application.OpenURL(Cc.HelpURL);
                                EditorGUILayout.Separator();
                            }

                            //change visibility
                            if (BGEditorUtility.ButtonWithIcon(Cc.Hidden ? BGBinaryResources.BGHiddenOn123 : BGBinaryResources.BGHiddenOff123, "Hide/Show properties")) Cc.Hidden = !Cc.Hidden;
                            EditorGUILayout.Separator();

                            //change name
                            if (BGEditorUtility.ButtonWithIcon(BGBinaryResources.BGCcEditName123, "Change the name")) BGCcChangeNameWindow.Open(Cc);
                            EditorGUILayout.Separator();

                            //add a child
                            if (BGEditorUtility.ButtonWithIcon(BGBinaryResources.BGAdd123, "Add a component, which is dependant on this component"))
                                BGCcAddWindow.Open(MyTree.Curve, type =>
                                {
                                    //cache some data
                                    var gameObject = Cc.Curve.gameObject;
                                    var oldComponents = gameObject.GetComponents<BGCc>();
                                    var currentCcType = Cc.GetType();

                                    //add
                                    var addedCc = AddComponent(MyTree.Curve, type);
                                    if (addedCc == null) return;

                                    //we need to process all the way up to the Cc and link Ccs to right (newly created) parents
                                    var parentClass = addedCc.GetParentClass();
                                    var recursionLimit = 16;
                                    var cc = addedCc;
                                    while (parentClass != null && recursionLimit-- > 0)
                                    {
                                        if (currentCcType == parentClass)
                                        {
                                            //we reached the current Cc
                                            cc.SetParent(Cc);
                                            break;
                                        }

                                        //going up
                                        var possibleParents = gameObject.GetComponents(parentClass);
                                        var parent = possibleParents.Where(possibleParent => !oldComponents.Contains(possibleParent)).Cast<BGCc>().FirstOrDefault();

                                        if (parent == null) break;

                                        cc.SetParent(parent);
                                        cc = parent;
                                        parentClass = cc.GetParentClass();
                                    }
                                }, Cc.GetType());
                            EditorGUILayout.Separator();


                            //enable/disable
                            if (BGEditorUtility.ButtonWithIcon(Cc.enabled ? BGBinaryResources.BGTickYes123 : BGBinaryResources.BGTickNo123, "Enable/disable a component")) Enable(!Cc.enabled);
                            EditorGUILayout.Separator();

                            //delete
                            if (!BGEditorUtility.ButtonWithIcon(BGBinaryResources.BGDelete123, "Remove this component")) return;


                            //remove
                            Delete();

                            EditorUtility.SetDirty(MyTree.Curve.gameObject);

                            //not sure how to make proper exit
                            throw new BGEditorUtility.ExitException();
                        });
                    });
                }

                private void Enable(bool enabled)
                {
                    Cc.enabled = enabled;
                    IterateChildren(node => node.Enable(enabled));
                }

                public void Delete()
                {
                    IterateChildren(node => node.Delete());

                    if (ccEditor != null)
                    {
                        ccEditor.OnDelete();
                        ccEditor.ChangedParent -= ChangedParent;
                        Object.DestroyImmediate(ccEditor);
                    }

                    Undo.DestroyObjectImmediate(Cc);
                }

                public void OnDestroy()
                {
                    IterateChildren(node => node.OnDestroy());

                    if (ccEditor == null) return;

                    ccEditor.ChangedParent -= ChangedParent;
                    Object.DestroyImmediate(ccEditor);
                }

                public void OnSceneGui()
                {
                    IterateChildren(node => node.OnSceneGui());

                    if (onSceneGuiMethod != null) onSceneGuiMethod.Invoke(ccEditor, null);
                }

                public void FillState(Dictionary<int, bool> instanceId2Collapsed)
                {
                    if (Collapsed) instanceId2Collapsed[Cc.GetInstanceID()] = true;

                    IterateChildren(node => node.FillState(instanceId2Collapsed));
                }
            }
        }
    }
}