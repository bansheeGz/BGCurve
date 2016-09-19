using UnityEngine;
using BansheeGz.BGSpline.Components;
using BansheeGz.BGSpline.Curve;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
    [CustomEditor(typeof (BGCcCursorObjectRotate))]
    public class BGCcCursorObjectRotateEditor : BGCcCursorObjectEditor
    {
        private BGCcCursorObjectRotate ObjectRotate
        {
            get { return (BGCcCursorObjectRotate) cc; }
        }

        protected override void ShowHandlesSettings()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("handlesScale"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("handlesColor"));
        }

        protected override void InternalOnInspectorGUI()
        {
            base.InternalOnInspectorGUI();

            BGEditorUtility.VerticalBox(() =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("rotationInterpolation"));

                switch (ObjectRotate.RotationInterpolation)
                {
                    case BGCcCursorObjectRotate.RotationInterpolationEnum.Lerp:
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("lerpSpeed"));
                        break;
                    case BGCcCursorObjectRotate.RotationInterpolationEnum.Slerp:
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("slerpSpeed"));
                        break;
                }
            });

            BGEditorUtility.VerticalBox(() =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("upMode"));
                switch (ObjectRotate.UpMode)
                {
                    case BGCcCursorObjectRotate.RotationUpEnum.WorldCustom:
                    case BGCcCursorObjectRotate.RotationUpEnum.LocalCustom:
                    case BGCcCursorObjectRotate.RotationUpEnum.TargetParentUpCustom:
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("upCustom"));
                        break;
                }
            });
        }

        protected override void InternalOnSceneGUI()
        {
            var objectRotate = (BGCcCursorObjectRotate) cc;

            var cursor = objectRotate.Cursor;

            if (cursor == null) return;

            var math = cursor.Math;

            if (math == null || !math.IsCalculated(BGCurveBaseMath.Field.Tangent)) return;

            var position = cursor.CalculatePosition();
            var tangent = cursor.CalculateTangent();

            if (Vector3.SqrMagnitude(tangent) > 0.0001)
            {
                var handleSize = BGEditorUtility.GetHandleSize(position, BGPrivateField.Get<float>(ObjectRotate, "handlesScale"));
                BGEditorUtility.SwapHandlesColor(BGPrivateField.Get<Color>(ObjectRotate, "handlesColor"), () => { Handles.ArrowCap(0, position, Quaternion.LookRotation(tangent), handleSize); });
            }
        }
    }
}