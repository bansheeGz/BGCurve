using UnityEngine;
using BansheeGz.BGSpline.Curve;
using UnityEditor;


namespace BansheeGz.BGSpline.Editor
{
    public class BGCcChangeNameWindow : EditorWindow
    {
        private static readonly Vector2 WindowSize = new Vector2(400, 50);

        private static BGCc current;
        private static string ccName;
        private static BGCcChangeNameWindow instance;


        private Vector2 scrollPos;
        private GUIStyle boxStyle;


        internal static void Open(BGCc current)
        {
            BGCcChangeNameWindow.current = current;

            ccName = current.CcName;

            instance = BGEditorUtility.ShowPopupWindow<BGCcChangeNameWindow>(WindowSize);
        }

        private void OnGUI()
        {
            BGEditorUtility.SwapLabelWidth(60, () =>
            {
                BGEditorUtility.Horizontal(BGEditorUtility.Assign(ref boxStyle, () => new GUIStyle("Box") {padding = new RectOffset(8, 8, 8, 8)}), () =>
                {
                    ccName = EditorGUILayout.TextField("Name", ccName);

                    GUILayout.Space(8);

                    if (GUILayout.Button("Save and Close"))
                    {
                        if (!string.Equals(ccName, current.CcName))
                        {
                            Undo.RecordObject(current, "Change name");
                            current.CcName = ccName;
                            EditorUtility.SetDirty(current);
                        }
                        instance.Close();
                    }
                });
            });
        }
    }
}