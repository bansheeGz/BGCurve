using BansheeGz.BGSpline.Components;
using BansheeGz.BGSpline.Curve;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
    [CustomEditor(typeof (BGCcMath))]
    public class BGCcMathEditor : BGCcEditor
    {
        private BGCcMath Math
        {
            get { return ((BGCcMath) cc); }
        }


        protected override void InternalOnEnable()
        {
            //ensure math is created and listeners attached
            var math = Math.Math;
            math.SuppressWarning = math.SuppressWarning;
        }

        protected override void InternalOnInspectorGUI()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fields"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sectionParts"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("optimizeStraightLines"));

            if (Math.Fields == BGCurveBaseMath.Fields.PositionAndTangent)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("usePositionToCalculateTangents"));
            }
            BGEditorUtility.VerticalBox(() =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("updateMode"));
                if (Math.UpdateMode == BGCcMath.UpdateModeEnum.RendererVisible)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("rendererForUpdateCheck"));
                }
            });
        }

        protected override void InternalOnUndoRedo()
        {
            if (Math != null) Math.Recalculate();
        }
    }
}