using UnityEngine;
using BansheeGz.BGSpline.Curve;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
    [InitializeOnLoad]
    public static class BGStartUp
    {
        private const int IconSize = 20;

        private static readonly GUIStyle curveStyle = new GUIStyle {fontStyle = FontStyle.Bold, normal = {textColor = new Color32(0, 144, 182, 255)}};
        private static readonly GUIStyle curveWarningStyle = new GUIStyle {fontStyle = FontStyle.Bold, normal = {textColor = Color.yellow}};
        private static readonly GUIStyle curveErrorStyle = new GUIStyle {fontStyle = FontStyle.Bold, normal = {textColor = Color.red}};

        private static readonly GUIStyle selectedCurveStyle = new GUIStyle {fontStyle = FontStyle.Bold, normal = {textColor = new Color(1, 1, 1, 1)}};


        static BGStartUp()
        {
            EditorApplication.hierarchyWindowItemOnGUI += ShowIcon;
        }

        //thanks to laurentlavigne
        private static void ShowIcon(int instanceId, Rect selectionRect)
        {
            var go = EditorUtility.InstanceIDToObject(instanceId) as GameObject;

            if (go == null) return;
            var curve = go.GetComponent<BGCurve>();
            if (curve == null) return;

            bool hasError = false, hasWarning = false;
            BGCurveEditorComponents.ComponentsStatus(curve, ref hasError, ref hasWarning);

            string text;
            GUIStyle style;
            if (hasError)
            {
                text = "C!";
                style = curveErrorStyle;
            }
            else if (hasWarning)
            {
                text = "C!";
                style = curveWarningStyle;
            }
            else
            {
                text = "C";
                var selected = Selection.Contains(instanceId);
                style = selected ? selectedCurveStyle : curveStyle;
            }

            GUI.Label(new Rect(selectionRect) {x = selectionRect.xMax - IconSize, width = IconSize}, text, style);
        }
    }
}