using System;
using BansheeGz.BGSpline.Components;
using BansheeGz.BGSpline.Curve;
using UnityEditor;
using UnityEngine;

namespace BansheeGz.BGSpline.Editor
{
    [CustomEditor(typeof (BGCcCursorChangeLinear))]
    public class BGCcCursorChangeLinearEditor : BGCcEditor
    {
        private GUIContent speedFieldContent;
        private GUIContent delayFieldContent;

        private BGCcCursorChangeLinear ChangeLinear
        {
            get { return (BGCcCursorChangeLinear) cc; }
        }

        protected override void InternalOnInspectorGUI()
        {
            BGEditorUtility.Assign(ref speedFieldContent, () => new GUIContent("Speed Field", "Speed field to take a speed from. Each point will have it's own speed. Should be a float field."));
            BGEditorUtility.Assign(ref delayFieldContent, () => new GUIContent("Delay Field", "Delay field to take a delay from. Each point will have it's own delay. Should be a float field."));

            //use fixedupdate
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useFixedUpdate"));
            
            //speed
            BGEditorUtility.VerticalBox(() =>
            {
                BGEditorUtility.CustomField(speedFieldContent, cc.Curve, ChangeLinear.SpeedField, BGCurvePointField.TypeEnum.Float, field => ChangeLinear.SpeedField = field);

                if (ChangeLinear.SpeedField == null) EditorGUILayout.PropertyField(serializedObject.FindProperty("speed"));
            });

            //delay
            BGEditorUtility.VerticalBox(() =>
            {
                BGEditorUtility.CustomField(delayFieldContent, cc.Curve, ChangeLinear.DelayField, BGCurvePointField.TypeEnum.Float, field => ChangeLinear.DelayField = field);

                if (ChangeLinear.DelayField == null) EditorGUILayout.PropertyField(serializedObject.FindProperty("delay"));
            });


            EditorGUILayout.PropertyField(serializedObject.FindProperty("overflowControl"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("adjustByTotalLength"));

            try
            {
                //by some reason NullReferenceException exceptions are fired at certain GUI passes
                EditorGUILayout.PropertyField(serializedObject.FindProperty("pointReachedEvent"));
            }
            catch (NullReferenceException)
            {
            }
        }
    }
}