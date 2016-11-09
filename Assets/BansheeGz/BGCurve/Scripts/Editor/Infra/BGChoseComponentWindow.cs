using System;
using UnityEngine;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
    // idea.. partially copy/pasted from BGCcChoseWindow
    public class BGChoseComponentWindow : EditorWindow
    {
        private static readonly Vector2 WindowSize = new Vector2(400, 400);

        private static Action<Component> action;
        private static BGChoseComponentWindow instance;
        private static Component current;
        private static GUIStyle boxStyle;


        private Vector2 scrollPos;


        internal static void Open(Component current, Action<Component> action)
        {
            BGChoseComponentWindow.action = action;
            BGChoseComponentWindow.current = current;

            instance = BGEditorUtility.ShowPopupWindow<BGChoseComponentWindow>(WindowSize);
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            BGEditorUtility.VerticalBox(() =>
            {
                var allComponents = current.GetComponents(typeof (Component));
                foreach (var component in allComponents)
                {
                    var comp = component;
                    BGEditorUtility.DisableGui(() =>
                    {
                        if (GUILayout.Button(comp.ToString(), GUILayout.Width(380)))
                        {
                            action(comp);
                            instance.Close();
                        }
                    }, current == component);
                }
            });

            EditorGUILayout.EndScrollView();

        }
    }
}