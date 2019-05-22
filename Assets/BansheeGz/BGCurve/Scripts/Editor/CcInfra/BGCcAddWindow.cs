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
        private const int HeaderHeight = 12;

        private static readonly Vector2 WindowSize = new Vector2(700, 400);


        private static BGCcTreeView tree;

        private static GUIStyle NameStyle
        {
            get
            {
                return new GUIStyle("Label")
                {
                    alignment = TextAnchor.MiddleCenter,
                    clipping = TextClipping.Clip,
                    wordWrap = true,
                    fontStyle = FontStyle.Bold,
                    normal =
                    {
                        textColor = Color.black,
                        background = BGBinaryResources.BGBoxWithBorder123
                    }
                };
            }
        }

        private static GUIStyle DisabledStyle
        {
            get
            {
                return new GUIStyle("Label")
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal =
                    {
                        textColor = Color.red,
                        background = BGBinaryResources.BGBoxWithBorder123
                    }
                };
            }
        }

        private static GUIStyle FilterStyle
        {
            get
            {
                return new GUIStyle("Label")
                {
                    fontStyle = FontStyle.Bold,
                    normal =
                    {
                        textColor = Color.red
                    }
                };
            }
        }

        private static BGCcAddWindow instance;

        private static int tab;
        private Vector2 scrollPos;

        internal static void Open(BGCurve curve, Action<Type> action, Type dependsOnType = null, bool ignoreExcludeFromMenuAttribute = false)
        {
            tree = new BGCcTreeView(curve, dependsOnType, ignoreExcludeFromMenuAttribute, Message, type =>
            {
                action(type);
                instance.Close();
            });

            instance = BGEditorUtility.ShowPopupWindow<BGCcAddWindow>(WindowSize);
        }

        private void OnGUI()
        {
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
            GUI.DrawTexture(GUILayoutUtility.GetLastRect(), BGBinaryResources.BGBoxWithBorder123);

            var headerImage = BGBinaryResources.BGCurveComponents123;
            var rect = new Rect(new Vector2(40, 10), new Vector2(headerImage.Texture.width * HeaderHeight / (float) headerImage.Texture.height, HeaderHeight));
            GUI.DrawTexture(rect, headerImage);

            if (tree.DependsOnType != null)
                GUI.Label(new Rect(rect) {x = rect.xMax + 10, height = 16, width = 400, y = rect.y - 2}, "Filter: Dependent on [" + tree.DependsOnType.Name + "]", FilterStyle);
        }
    
    }
}