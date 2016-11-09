using UnityEngine;
using BansheeGz.BGSpline.Components;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{

    [CustomEditor(typeof(BGCcTriangulate2D))]
    public class BGCcTriangulate2DEditor : BGCcSplitterPolylineEditor
    {

        private BGCcTriangulate2D Triangulate2D
        {
            get { return (BGCcTriangulate2D)cc; }
        }

        protected override void AdditionalParams()
        {
            var updateEveryFrameProperty = serializedObject.FindProperty("updateEveryFrame");

            EditorGUILayout.PropertyField(updateEveryFrameProperty);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("flip"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("scaleUV"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("offsetUV"));
            BGEditorUtility.VerticalBox(() =>
            {
                var doubleSidedProperty = serializedObject.FindProperty("doubleSided");
                EditorGUILayout.PropertyField(doubleSidedProperty);
                if (doubleSidedProperty.boolValue)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("scaleBackUV"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("offsetBackUV"));
                }
            });

            //launch coroutine
            if (updateEveryFrameProperty.boolValue != Triangulate2D.UpdateEveryFrame && Application.isPlaying && updateEveryFrameProperty.boolValue) Triangulate2D.UpdateEveryFrame = true;
        }

        protected override void InternalOnInspectorGUIPost()
        {
            if (paramsChanged) Triangulate2D.UpdateUI();
        }

    }
}