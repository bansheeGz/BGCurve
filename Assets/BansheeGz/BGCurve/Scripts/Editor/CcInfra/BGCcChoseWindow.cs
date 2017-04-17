using System;
using UnityEngine;
using BansheeGz.BGSpline.Curve;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
    public class BGCcChoseWindow : EditorWindow
    {
        private static readonly Vector2 WindowSize = new Vector2(400, 200);

        private static Action<BGCc> action;
        private static BGCcChoseWindow instance;
        private static BGCc current;
        private static Component[] availableList;
        private static GUIStyle boxStyle;


        private Vector2 scrollPos;


        internal static void Open(BGCc current, Component[] availableList, Action<BGCc> action)
        {
            BGCcChoseWindow.action = action;
            BGCcChoseWindow.current = current;
            BGCcChoseWindow.availableList = availableList;

            instance = BGEditorUtility.ShowPopupWindow<BGCcChoseWindow>(WindowSize);
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            ShowButtons();

            EditorGUILayout.EndScrollView();
        }

        private static void ShowButtons()
        {
            BGEditorUtility.Vertical(BGEditorUtility.Assign(ref boxStyle, () => new GUIStyle("Box") {padding = new RectOffset(8, 8, 8, 8)}), () =>
            {
                for (var i = 0; i < availableList.Length; i++)
                {
                    var cc = (BGCc) availableList[i];

                    BGEditorUtility.DisableGui(() =>
                    {
                        if (!GUILayout.Button(cc.CcName)) return;


                        action(cc);
                        instance.Close();
                    }, cc == current);
                }
            });
        }
    }
}