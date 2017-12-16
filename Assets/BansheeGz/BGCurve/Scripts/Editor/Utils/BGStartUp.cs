using UnityEngine;
using BansheeGz.BGSpline.Curve;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
    [InitializeOnLoad]
    public static class BGStartUp
    {
        private const int IconSize = 20;

        // Not selected
        private static readonly Texture2D CurveIcon;
        private static readonly Texture2D CurveWarningIcon;
        private static readonly Texture2D CurveErrorIcon;

        // Selected
        private static readonly Texture2D CurveSelectedIcon;
        private static readonly Texture2D CurveWarningSelectedIcon;
        private static readonly Texture2D CurveErrorSelectedIcon;


        static BGStartUp()
        {
            CurveIcon = BGEditorUtility.LoadTexture2D(BGEditorUtility.Image.BGHierarchyIcon123, critical:false);
            if (CurveIcon!=null)
            {
                CurveWarningIcon = BGEditorUtility.LoadTexture2D(BGEditorUtility.Image.BGHierarchyWarningIcon123);
                CurveErrorIcon = BGEditorUtility.LoadTexture2D(BGEditorUtility.Image.BGHierarchyErrorIcon123);

                CurveSelectedIcon = BGEditorUtility.LoadTexture2D(BGEditorUtility.Image.BGHierarchySelectedIcon123);
                CurveWarningSelectedIcon = BGEditorUtility.LoadTexture2D(BGEditorUtility.Image.BGHierarchyWarningSelectedIcon123);
                CurveErrorSelectedIcon = BGEditorUtility.LoadTexture2D(BGEditorUtility.Image.BGHierarchyErrorSelectedIcon123);
                EditorApplication.hierarchyWindowItemOnGUI += ShowIcon;
            }
        }

        //thanks to laurentlavigne
        private static void ShowIcon(int instanceId, Rect selectionRect)
        {
            var go = EditorUtility.InstanceIDToObject(instanceId) as GameObject;

            if (go == null) return;
            var curve = go.GetComponent<BGCurve>();
            if (curve == null) return;

            var selected = Selection.Contains(instanceId);
            bool hasError = false, hasWarning = false;
            BGCurveEditorComponents.ComponentsStatus(curve, ref hasError, ref hasWarning);

            var icon = selected
                //selected
                ? hasError
                    ? CurveErrorSelectedIcon
                    : hasWarning
                        ? CurveWarningSelectedIcon
                        : CurveSelectedIcon

                // Not selected
                : hasError
                    ? CurveErrorIcon
                    : hasWarning
                        ? CurveWarningIcon
                        : CurveIcon;

            GUI.Label(new Rect(selectionRect) {x = selectionRect.xMax - IconSize, width = IconSize}, icon);
        }
    }
}