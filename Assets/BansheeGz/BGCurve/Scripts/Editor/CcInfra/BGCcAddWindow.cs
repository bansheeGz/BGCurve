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

        private static Texture2D headerImage;
        private static Texture2D boxWithBorderImage;
        private static GUIStyle nameStyle;
        private static GUIStyle disabledStyle;
        private static GUIStyle filterStyle;

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
            //styles
            AssighStyles();


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

        private static void AssighStyles()
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
    }
}