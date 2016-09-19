using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using BansheeGz.BGSpline.Curve;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
    public class BGCcAddWindow : EditorWindow
    {
        private const int IconSize = 48;
        private const int HeaderHeight = 12;

        private static readonly Vector2 WindowSize = new Vector2(700, 400);


        private static Tree tree;

        private static Texture2D headerImage;
        private static Texture2D noImage;
        private static Texture2D boxWithBorderImage;
        private static GUIStyle nameStyle;
        private static GUIStyle disabledStyle;
        private static GUIStyle filterStyle;

        private static Action<Type> action;
        private static BGCcAddWindow instance;

        private static int tab;
        private Vector2 scrollPos;

        internal static void Open(BGCurve curve, Action<Type> action, Type dependsOnType = null)
        {
            BGCcAddWindow.action = action;

            noImage = BGEditorUtility.LoadTexture2D(BGEditorUtility.Image.BGCcNoImage123);

            tree = new Tree(curve, dependsOnType);

            instance = BGEditorUtility.ShowPopupWindow<BGCcAddWindow>(WindowSize);
        }

        private void OnGUI()
        {
            //styles
            AssighSyles();


            //draw header
            DrawHeader();


            if (tree.Roots.Count == 0)
            {
                Message("Did not find any component");
            }
            else
            {
                BGEditorUtility.VerticalBox(() =>
                {
                    scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
                    tree.OnInspectorGui();
                    EditorGUILayout.EndScrollView();
                    
                });
            }
        }

        private static void Message(string message)
        {
            EditorGUILayout.LabelField(message, new GUIStyle("Label")
            {
                fontSize = 22,
                wordWrap = true
            }, GUILayout.Height(200));
        }

        private static void DrawHeader()
        {
            BGEditorUtility.HorizontalBox(() => { GUILayout.Label("   "); });
            GUI.DrawTexture(GUILayoutUtility.GetLastRect(), boxWithBorderImage);

            var rect = new Rect(new Vector2(40, 10), new Vector2(headerImage.width*HeaderHeight/(float) headerImage.height, HeaderHeight));
            GUI.DrawTexture(rect, headerImage);

            if (tree.DependsOnType != null)
                GUI.Label(new Rect(rect) {x = rect.xMax + 10, height = 16, width = 400, y = rect.y - 2}, "Filter: Dependent on [" + tree.DependsOnType.Name + "]", filterStyle);
        }

        private static void AssighSyles()
        {
            BGEditorUtility.Assign(ref nameStyle, () => new GUIStyle("Label")
            {
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Clip,
                wordWrap = true,
                fontStyle = FontStyle.Bold,
                normal =
                {
                    textColor = Color.black,
                    background = BGEditorUtility.LoadTexture2D(BGEditorUtility.Image.BGBoxWithBorder123)
                }
            });
            BGEditorUtility.Assign(ref disabledStyle, () => new GUIStyle("Label")
            {
                alignment = TextAnchor.MiddleCenter,
                normal =
                {
                    textColor = Color.red,
                    background = BGEditorUtility.LoadTexture2D(BGEditorUtility.Image.BGBoxWithBorder123)
                }
            });
            BGEditorUtility.Assign(ref filterStyle, () => new GUIStyle("Label")
            {
                fontStyle = FontStyle.Bold,
                normal =
                {
                    textColor = Color.red
                }
            });
            BGEditorUtility.Assign(ref headerImage, () => BGEditorUtility.LoadTexture2D(BGEditorUtility.Image.BGCurveComponents123));
            BGEditorUtility.Assign(ref boxWithBorderImage, () => BGEditorUtility.LoadTexture2D(BGEditorUtility.Image.BGBoxWithBorder123));
        }


        //thanks to Bunny83 
        public static Type[] GetAllSubTypes(Type targetType)
        {
            var result = new List<Type>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies) result.AddRange(from type in assembly.GetTypes() where type.IsClass where !type.IsAbstract where type.IsSubclassOf(targetType) select type);
            return result.ToArray();
        }

        //===================================================================   Inner classes

        //================================================= Tree 
        private sealed class Tree : BGTreeView<CcNode>
        {
            private readonly Dictionary<Type, CcNode> type2Node = new Dictionary<Type, CcNode>();

            public BGCurve Curve { get; private set; }

            public Type DependsOnType { get; private set; }

            public Tree(BGCurve curve, Type dependsOnType)
                : base(new Config(0, (int) (IconSize*.9f), (int) (IconSize*.5f), 2, 16))
            {
                Curve = curve;
                DependsOnType = dependsOnType;

                //-------------  load all BGCc subclasses
                var typesList = GetAllSubTypes(typeof (BGCc));
                foreach (var node in from type in typesList
                    let descriptor = BGCc.GetDescriptor(type)
                    let single = BGCc.IsSingle(type)
                    select new CcNode(this, descriptor != null ? new CcData(type, single, descriptor) : new CcData(type, single, type.Name)))
                {
                    try
                    {
                        node.CcData.ParentType = BGCc.GetParentClass(node.CcData.Type);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        continue;
                    }
                    type2Node[node.CcData.Type] = node;
                }

                foreach (var node in type2Node.Values) if (!node.Processed) node.ProcessStructure();
            }

            public CcNode this[Type type]
            {
                get { return type2Node[type]; }
            }

            public override void OnInspectorGui()
            {
                if (DependsOnType == null) base.OnInspectorGui();
                else
                {
                    var node = type2Node[DependsOnType];
                    if (node == null)
                    {
                        Message("Error: Can not find component " + DependsOnType);
                    }
                    else if (!node.HasChildren)
                    {
                        Message("Component [" + node.CcData.Name + "] does not have any children");
                    }
                    else
                    {
                        node.OnInspectorGui();
                    }
                }
            }
        }

        //================================================= Tree node
        private sealed class CcNode : BGTreeNode<CcNode>
        {
            private readonly CcData ccData;
            private readonly bool singleAndAdded;
            private Texture2D addIcon;


            private GUIStyle nameStyle;
            private GUIStyle descriptionStyle;
            private GUIStyle addedStyle;


            public CcData CcData
            {
                get { return ccData; }
            }

            public bool Processed { get; private set; }

            private Tree MyTree
            {
                get { return (Tree) Tree; }
            }

            public CcNode(Tree tree, CcData ccData)
                : base(tree)
            {
                this.ccData = ccData;
                singleAndAdded = ccData.Single && tree.Curve.GetComponent(ccData.Type) != null;
            }

            public override void OnInspectorGuiInternal(int level)
            {
                const int offset = 2;

                if (singleAndAdded) GUI.enabled = false;
                if (GUILayout.Button("", GUILayout.Height(IconSize + offset*2)))
                {
                    action(ccData.Type);
                    instance.Close();
                }
                if (singleAndAdded) GUI.enabled = true;

                // Draw on top of the button (no more Layout stuff)
                var buttonRect = GUILayoutUtility.GetLastRect();


                var iconRect = new Rect(buttonRect.x + offset, buttonRect.y + offset, IconSize, IconSize);
                GUI.DrawTexture(iconRect, ccData.Icon);

/*
                //already added
                if (singleAndAdded) GUI.DrawTexture(new Rect(iconRect) {x = iconRect.xMax + offset, width = offset*8}, 
                        BGEUtil.Assign(ref addIcon, () => BGEUtil.LoadTexture2D(BGEUtil.Image.BGCcAdded123)));
*/
                if (singleAndAdded)
                {
                    var oldMatrix = GUI.matrix;
                    BGEditorUtility.Assign(ref addedStyle, () => new GUIStyle("Box")
                    {
                        fontSize = 14,
                        fontStyle = FontStyle.Bold,
                        normal = {textColor = Color.red, background = BGEditorUtility.Texture1X1(new Color(1,0,0,.2f))}
                    });
                    var content = new GUIContent("added");
                    var labelSize = addedStyle.CalcSize(content);
                    var pivotPoint = new Vector2(iconRect.xMax, iconRect.center.y) + new Vector2(labelSize.y*.5f, 0);
                    GUIUtility.RotateAroundPivot(-90, pivotPoint);
                    GUI.Label(new Rect(pivotPoint - labelSize*.5f, labelSize), content, addedStyle);
                    GUI.matrix = oldMatrix;
                }


                //name
                var nameStartX = iconRect.xMax + offset*12;
                var nameRect = new Rect(nameStartX, iconRect.y, buttonRect.width - nameStartX, IconSize/3f);
                EditorGUI.LabelField(nameRect, ccData.Name, BGEditorUtility.Assign(ref nameStyle, () => new GUIStyle("Label") {fontStyle = FontStyle.Bold}));

                //description
                var descriptionRect = new Rect(nameStartX, nameRect.yMax + offset, nameRect.width, IconSize*2/3f);
                EditorGUI.LabelField(descriptionRect, ccData.Description, BGEditorUtility.Assign(ref descriptionStyle, () => new GUIStyle("Label") {wordWrap = true}));
            }


            public override void ProcessStructure()
            {
                if (ccData.ParentType == null)
                {
                    MyTree.Roots.Add(this);
                }
                else
                {
                    Parent = MyTree[CcData.ParentType];
                }
                Processed = true;
            }
        }

        //================================================= CC data
        private sealed class CcData
        {
            public Type Type { get; private set; }

            public Type ParentType { get; set; }

            public string Description { get; private set; }

            public string Name { get; private set; }

            public Texture2D Icon { get; private set; }

            public bool Single { get; private set; }

            public CcData(Type type, bool single, BGCc.CcDescriptor ccDescriptor)
                : this(type, single, ccDescriptor.Name)
            {
                Description = ccDescriptor.Description;
                if (!string.IsNullOrEmpty(ccDescriptor.Image)) Icon = BGEditorUtility.LoadCcTexture2D(ccDescriptor.Image) ?? noImage;
            }

            public CcData(Type type, bool single, string name)
            {
                Type = type;

                Name = name;
                Icon = noImage;
                Single = single;

                if (string.IsNullOrEmpty(Name)) Name = Type.Name;
            }

            public override string ToString()
            {
                return Type.Name + ": " + Name;
            }
        }
    }
}