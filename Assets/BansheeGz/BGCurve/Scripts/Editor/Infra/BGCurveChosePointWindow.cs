using System;
using UnityEngine;
using BansheeGz.BGSpline.Curve;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
    // idea.. partially copy/pasted from BGCcChoseWindow
    public class BGCurveChosePointWindow : EditorWindow
    {
        private static readonly Vector2 WindowSize = new Vector2(400, 40);

        private static Action<BGCurvePointComponent> action;
        private static BGCurveChosePointWindow instance;
        private static BGCurve curve;
        private static int current;

        internal static void Open(int current, BGCurve curve, Action<BGCurvePointComponent> action)
        {
            BGCurveChosePointWindow.action = action;
            BGCurveChosePointWindow.current = current;
            BGCurveChosePointWindow.curve = curve;

            instance = BGEditorUtility.ShowPopupWindow<BGCurveChosePointWindow>(WindowSize);
        }

        private void OnGUI()
        {
            BGEditorUtility.HorizontalBox(() =>
            {
                EditorGUILayout.LabelField("Point index", GUILayout.Width(100));
                var newValue = EditorGUILayout.IntSlider(current, 0, curve.PointsCount - 1, GUILayout.Width(200));
                if (current != newValue)
                {
                    current = newValue;
                    action((BGCurvePointComponent) curve[newValue]);
                }

                if (GUILayout.Button("Close", GUILayout.Width(50))) instance.Close();
            });
        }
    }
}