using System;
using BansheeGz.BGSpline.Components;
using BansheeGz.BGSpline.Curve;
using UnityEditor;
using UnityEngine;

namespace BansheeGz.BGSpline.Editor
{
    [CustomEditor(typeof(BGCcMath))]
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
            BGEditorUtility.Horizontal(() =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("fields"));
//                if (GUILayout.Button("Update")) Math.Recalculate();
            });


            BGEditorUtility.VerticalBox(() =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("mathType"));

                switch (Math.MathType)
                {
                    case BGCcMath.MathTypeEnum.Base:
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("sectionParts"));
                        break;
                    case BGCcMath.MathTypeEnum.Adaptive:
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("tolerance"));
                        break;
                }
            });

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

            try
            {
                //by some reason NullReferenceException exceptions are fired at certain GUI passes
                EditorGUILayout.PropertyField(serializedObject.FindProperty("mathChangedEvent"));
            }
            catch (NullReferenceException)
            {
            }
        }

        protected override void ShowHandlesSettings()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("spheresScale"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("spheresColor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("spheresCount"));
        }

        protected override void InternalOnUndoRedo()
        {
            if (Math != null) Math.Recalculate();
        }


        protected override void InternalOnSceneGUI()
        {
            var mathCc = Math;

            if (mathCc == null || mathCc.Math == null || mathCc.Math.SectionsCount == 0) return;

            if (mathCc.Curve.ForceChangedEventMode != BGCurve.ForceChangedEventModeEnum.Off) mathCc.Recalculate();

            var math = mathCc.Math;

            var sphereScale = BGPrivateField.Get<float>(mathCc, "spheresScale");

            BGEditorUtility.SwapHandlesColor(BGPrivateField.Get<Color>(mathCc, "spheresColor"), () =>
            {
                var count = BGPrivateField.Get<int>(mathCc, "spheresCount");

                for (var i = 0; i < math.SectionsCount; i++)
                {
                    var section = math[i];
                    var points = section.Points;
                    for (var j = 0; j < points.Count; j++)
                    {
                        var pos = points[j].Position;
#if UNITY_5_6_OR_NEWER
				Handles.SphereHandleCap(0, pos, Quaternion.identity, sphereScale * BGEditorUtility.GetHandleSize(pos, .07f), EventType.Repaint);
#else
				Handles.SphereCap(0, pos, Quaternion.identity, sphereScale*BGEditorUtility.GetHandleSize(pos, .07f));
#endif
				if (count-- <= 0) return;
                    }
                }
            });
        }
    }
}