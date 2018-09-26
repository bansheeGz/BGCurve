using System;
using UnityEngine;
using BansheeGz.BGSpline.Components;
using BansheeGz.BGSpline.Curve;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
    [CustomEditor(typeof(BGCcCursorObjectRotate))]
    public class BGCcCursorObjectRotateEditor : BGCcCursorObjectEditor
    {
        private GUIContent rotationFieldContent;
        private GUIContent revolutionsFieldContent;
        private GUIContent clockwiseFieldContent;
//        private GUIContent customUpFieldContent;

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

            // reusable labels
            BGEditorUtility.Assign(ref rotationFieldContent,
                () => new GUIContent("Rotation Field", "Rotation field to take a rotation from. Each point will have it's own rotation. Should be a Quaternion field."));
            BGEditorUtility.Assign(ref revolutionsFieldContent,
                () => new GUIContent("Revolutions Field", "Field to store additional revolutions around tangent. It should be an int field."));
            BGEditorUtility.Assign(ref clockwiseFieldContent,
                () => new GUIContent("Revolutions Clockwise Field", "Field to store if the rotation around tangent should be clockwise. It should be a bool field."));
//            BGEditorUtility.Assign(ref customUpFieldContent,
//                () => new GUIContent("Custom up Vector", "Field to store custom up vector. It should be a Vector3 field."));


            //type of the rotation 1) tangent (without field) 2) by field's values
            BGEditorUtility.VerticalBox(() =>
            {
                BGEditorUtility.CustomField(rotationFieldContent, cc.Curve, ObjectRotate.RotationField, BGCurvePointField.TypeEnum.Quaternion, field => ObjectRotate.RotationField = field);

                if (ObjectRotate.RotationField != null)
                {
                    //============================== field is used

                    BGEditorUtility.VerticalBox(() =>
                    {
                        // additional revolutions
                        BGEditorUtility.CustomField(revolutionsFieldContent, cc.Curve, ObjectRotate.RevolutionsAroundTangentField, BGCurvePointField.TypeEnum.Int,
                            field => ObjectRotate.RevolutionsAroundTangentField = field);
                        if (ObjectRotate.RevolutionsAroundTangentField == null) EditorGUILayout.PropertyField(serializedObject.FindProperty("revolutionsAroundTangent"));

                        // clockwise?
                        BGEditorUtility.CustomField(clockwiseFieldContent, cc.Curve, ObjectRotate.RevolutionsClockwiseField,
                            BGCurvePointField.TypeEnum.Bool, field => ObjectRotate.RevolutionsClockwiseField = field);

                        if (ObjectRotate.RevolutionsClockwiseField == null) EditorGUILayout.PropertyField(serializedObject.FindProperty("revolutionsClockwise"));
                    });
                }
                else
                {
                    //============================== no field- tangent is used
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
//                            case BGCcCursorObjectRotate.RotationUpEnum.CustomField:
//                                EditorGUILayout.PropertyField(serializedObject.FindProperty("upCustomField"));
//                                break;
                        }
                    });
                }
            });


            //interpolation
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

            EditorGUILayout.PropertyField(serializedObject.FindProperty("offsetAngle"));
        }

        protected override void InternalOnSceneGUI()
        {
            var curve = ObjectRotate.Curve;

            if (curve == null || curve.PointsCount == 0) return;

            BGCcCursor cursor;
            try
            {
                cursor = ObjectRotate.Cursor;
            }
            catch (MissingReferenceException)
            {
                return;
            }

            if (cursor == null) return;

            var math = cursor.Math;

            if (math == null || !math.IsCalculated(BGCurveBaseMath.Field.Tangent)) return;

            var position = cursor.CalculatePosition();

            //by field
            var result = Quaternion.identity;
            if (!ObjectRotate.TryToCalculateRotation(ref result)) return;

            var handleSize = BGEditorUtility.GetHandleSize(position, BGPrivateField.Get<float>(ObjectRotate, "handlesScale"));
            BGEditorUtility.SwapHandlesColor(BGPrivateField.Get<Color>(ObjectRotate, "handlesColor"), () =>
            {
#if UNITY_5_6_OR_NEWER
			Handles.ArrowHandleCap(0, position, result, handleSize, EventType.Repaint);
#else
			Handles.ArrowCap(0, position, result, handleSize);
#endif
            });
        }
    }
}