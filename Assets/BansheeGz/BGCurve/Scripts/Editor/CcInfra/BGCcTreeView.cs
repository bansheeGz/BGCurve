using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BansheeGz.BGSpline.Curve;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
    public class BGCcTreeView : BGTreeView<BGCcTreeView.CcNode>
    {
        private const int IconSize = 48;

        private readonly Dictionary<Type, CcNode> type2Node = new Dictionary<Type, CcNode>();

        public BGCurve Curve { get; private set; }

        public Type DependsOnType { get; private set; }

        private static Texture2D noImage;

        private readonly Action<string> messageAction;

        public BGCcTreeView(BGCurve curve, Type dependsOnType, bool ignoreExcludeFromMenuAttribute, Action<string> messageAction, Action<Type> typeWasChosenAction)
            : base(new Config(0, (int) (IconSize*.9f), (int) (IconSize*.5f), 2, 16))
        {
            Curve = curve;
            DependsOnType = dependsOnType;
            this.messageAction = messageAction;

            noImage = BGEditorUtility.LoadTexture2D(BGEditorUtility.Image.BGCcNoImage123);


            //-------------  load all BGCc subclasses
            var typesList = GetAllSubTypes(typeof(BGCc), ignoreExcludeFromMenuAttribute ? null : typeof(BGCc.CcExcludeFromMenu));
            foreach (var node in from type in typesList
                let descriptor = BGCc.GetDescriptor(type)
                let single = BGCc.IsSingle(type)
                select new CcNode(this, descriptor != null ? new CcData(type, single, descriptor) : new CcData(type, single, type.Name), typeWasChosenAction))
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
                if (node == null) messageAction("Error: Can not find component " + DependsOnType);
                else if (!node.HasChildren) messageAction("Component [" + node.CcData.Name + "] does not have any children");
                else node.OnInspectorGui();
            }
        }


        //================================================= Static
        //thanks to Bunny83 
        public static Type[] GetAllSubTypes(Type targetType, Type excludeAttributeType)
        {
            var result = new List<Type>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
                result.AddRange(from type in assembly.GetTypes()
                    where type.IsClass
                    where !type.IsAbstract
                    where type.IsSubclassOf(targetType)
                    where excludeAttributeType == null || BGReflectionAdapter.GetCustomAttributes(type, excludeAttributeType, true).Length == 0
                    select type);

            return result.ToArray();
        }

        //================================================= Tree node
        public sealed class CcNode : BGTreeNode<CcNode>
        {
            private readonly CcData ccData;
            private readonly bool singleAndAdded;
            private Texture2D addIcon;

            private readonly Action<Type> typeWasChosenAction;
            private GUIStyle nameStyle;
            private GUIStyle descriptionStyle;
            private GUIStyle addedStyle;


            public CcData CcData
            {
                get { return ccData; }
            }

            public bool Processed { get; private set; }

            private BGCcTreeView MyTree
            {
                get { return (BGCcTreeView) Tree; }
            }

            public CcNode(BGCcTreeView tree, CcData ccData, Action<Type> typeWasChosenAction)
                : base(tree)
            {
                this.ccData = ccData;
                this.typeWasChosenAction = typeWasChosenAction;
                singleAndAdded = ccData.Single && tree.Curve.GetComponent(ccData.Type) != null;
            }

            public override void OnInspectorGuiInternal(int level)
            {
                const int offset = 2;

                if (singleAndAdded) GUI.enabled = false;
                if (GUILayout.Button("", GUILayout.Height(IconSize + offset*2))) typeWasChosenAction(ccData.Type);

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
                    BGEditorUtility.SwapGuiMatrix(GUI.matrix, () =>
                    {
                        BGEditorUtility.Assign(ref addedStyle, () => new GUIStyle("Box")
                        {
                            fontSize = 14,
                            fontStyle = FontStyle.Bold,
                            normal = { textColor = Color.red, background = BGEditorUtility.Texture1X1(new Color(1, 0, 0, .2f)) }
                        });
                        var content = new GUIContent("added");
                        var labelSize = addedStyle.CalcSize(content);
                        var pivotPoint = new Vector2(iconRect.xMax, iconRect.center.y) + new Vector2(labelSize.y * .5f, 0);
                        GUIUtility.RotateAroundPivot(-90, pivotPoint);
                        GUI.Label(new Rect(pivotPoint - labelSize * .5f, labelSize), content, addedStyle);
                    });
                }


                //name
                var nameStartX = iconRect.xMax + offset*12;
                var nameRect = new Rect(nameStartX, iconRect.y, buttonRect.width - nameStartX, IconSize/3f);
                EditorGUI.LabelField(nameRect, (string) ccData.Name, BGEditorUtility.Assign(ref nameStyle, () => new GUIStyle("Label") {fontStyle = FontStyle.Bold}));

                //description
                var descriptionRect = new Rect(nameStartX, nameRect.yMax + offset, nameRect.width, IconSize*2/3f);
                EditorGUI.LabelField(descriptionRect, (string) ccData.Description, BGEditorUtility.Assign(ref descriptionStyle, () => new GUIStyle("Label") {wordWrap = true}));
            }


            public override void ProcessStructure()
            {
                if (ccData.ParentType == null) MyTree.Roots.Add(this);
                else Parent = MyTree[CcData.ParentType];
                Processed = true;
            }
        }

        //================================================= CC data

        public sealed class CcData
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
                if (!String.IsNullOrEmpty(ccDescriptor.Image)) Icon = BGEditorUtility.LoadCcTexture2D(ccDescriptor.Image) ?? noImage;
            }

            public CcData(Type type, bool single, string name)
            {
                Type = type;

                Name = name;
                Icon = noImage;
                Single = single;

                if (String.IsNullOrEmpty(Name)) Name = Type.Name;
            }

            public override string ToString()
            {
                return Type.Name + ": " + Name;
            }
        }
    }
}