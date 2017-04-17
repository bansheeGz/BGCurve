using UnityEngine;
using System.Collections;
using BansheeGz.BGSpline.Components;
using BansheeGz.BGSpline.Curve;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
    [CustomEditor(typeof(BGCcSweep2D))]
    public class BGCcSweep2DEditor : BGCcSplitterPolylineEditor
    {
        private BGCcSweep2D Sweep2D
        {
            get { return (BGCcSweep2D) cc; }
        }

        protected override void AdditionalParams()
        {
            BGEditorUtility.VerticalBox(() =>
            {
                BGEditorUtility.Horizontal(() =>
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("profileMode"));
                    if (!GUILayout.Button("Rebuild")) return;

                    Sweep2D.UpdateUI();
                });

                if (Sweep2D.ProfileMode == BGCcSweep2D.ProfileModeEnum.Line)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("lineWidth"));
                }
                else
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("profileSpline"));
                    if (Sweep2D.ProfileSpline != null)
                    {
//                        BGEditorUtility.CustomField(new GUIContent("U Coord Field"), Sweep2D.ProfileSpline.Curve, Sweep2D.UCoordinateField, BGCurvePointField.TypeEnum.Float, field => Sweep2D.UCoordinateField = field);
                    }
                }

            });

            EditorGUILayout.PropertyField(serializedObject.FindProperty("uCoordinateStart"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("uCoordinateEnd"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("swapUV"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("swapNormals"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("vCoordinateScale"));
        }

        protected override void InternalOnInspectorGUIPost()
        {
            if (paramsChanged) Sweep2D.UpdateUI();
        }

        protected override void InternalOnUndoRedo()
        {
            Sweep2D.UpdateUI();
        }
    }
}