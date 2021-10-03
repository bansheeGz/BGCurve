using System;
using BansheeGz.BGSpline.Components;
using BansheeGz.BGSpline.Curve;
using UnityEditor;
using UnityEngine;

namespace BansheeGz.BGSpline.Editor
{
    [CustomEditor(typeof(BGCcTrs))]
    public class BGCcTrsEditor : BGCcCursorEditor
    {
        private BGCcTrs Trs
        {
            get { return (BGCcTrs) cc; }
        }

        protected override void InternalOnInspectorGUI()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("objectToManipulate"));

            EditorGUILayout.LabelField("Cursor", EditorStyles.boldLabel);
            base.InternalOnInspectorGUI();

            EditorGUILayout.LabelField("Change Cursor", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useFixedUpdate"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("overflowControl"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("cursorChangeMode"));
            switch (Trs.CursorChangeMode)
            {
                case BGCcTrs.CursorChangeModeEnum.Constant:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("speed"));
                    break;
                case BGCcTrs.CursorChangeModeEnum.LinearField:
                case BGCcTrs.CursorChangeModeEnum.LinearFieldInterpolate:
                    BGEditorUtility.CustomField(new GUIContent("Speed field", "Float field to get speed value from"), cc.Curve, Trs.SpeedField, BGCurvePointField.TypeEnum.Float,
                        field => Trs.SpeedField = field);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            EditorGUILayout.LabelField("TRS", EditorStyles.boldLabel);
            BGEditorUtility.VerticalBox(() =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("moveObject"));
            });

            BGEditorUtility.VerticalBox(() =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("rotateObject"));
                if (Trs.RotateObject)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("offsetAngle"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("rotationInterpolation"));
                    switch (Trs.RotationInterpolation)
                    {
                        case BGCcTrs.RotationInterpolationEnum.Lerp:
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("lerpSpeed"));
                            break;
                        case BGCcTrs.RotationInterpolationEnum.Slerp:
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("slerpSpeed"));
                            break;
                    }
                    
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("upVector"));

                    BGEditorUtility.CustomField(new GUIContent("Rotation field", "Quaternion field to get rotation value from"), cc.Curve, Trs.RotationField, BGCurvePointField.TypeEnum.Quaternion,
                        field => Trs.RotationField = field);
                }
            });

            BGEditorUtility.VerticalBox(() =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("scaleObject"));
                if (Trs.ScaleObject)
                {
                    BGEditorUtility.CustomField(new GUIContent("Scale field", "Vector3 field to get scale value from"), cc.Curve, Trs.ScaleField, BGCurvePointField.TypeEnum.Vector3,
                        field => Trs.ScaleField = field);
                }
            });
        }

        protected override void ChangedParams(object sender, EventArgs e)
        {
            base.ChangedParams(sender, e);
            if (Application.isPlaying) return;
            Trs.Trs();
        }
    }
}