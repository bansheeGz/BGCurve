using UnityEngine;
using BansheeGz.BGSpline.Components;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
    [CustomEditor(typeof (BGCcVisualizationLineRenderer))]
    public class BGCcVisualizationLineRendererEditor : BGCcEditor
    {
        private bool updateUi;
        private bool listenersAdded;

        private BGCcVisualizationLineRenderer LineRenderer
        {
            get { return (BGCcVisualizationLineRenderer) cc; }
        }

        protected override void InternalOnEnable()
        {
            if (!LineRenderer.enabled) return;

            if (Application.isPlaying) return;

            LineRenderer.UpdateUI();
            LineRenderer.AddListeners();
            listenersAdded = true;
        }

        protected override void InternalOnDestroy()
        {
            if (Application.isPlaying) return;

            if (LineRenderer != null) LineRenderer.RemoveListeners();
        }

        protected override void InternalOnInspectorGUI()
        {
            if (!listenersAdded) InternalOnEnable();

            EditorGUI.BeginChangeCheck();


            BGEditorUtility.VerticalBox(() =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("splitMode"));

                switch (LineRenderer.SplitMode)
                {
                    case BGCcVisualizationLineRenderer.SplitModeEnum.PartsTotal:
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("partsTotal"));
                        break;
                    case BGCcVisualizationLineRenderer.SplitModeEnum.PartsPerSection:
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("partsPerSection"));
                        break;
                }
            });


            EditorGUILayout.PropertyField(serializedObject.FindProperty("doNotOptimizeStraightLines"));

            updateUi = EditorGUI.EndChangeCheck();
        }

        protected override void InternalOnInspectorGUIPost()
        {
            if (updateUi) LineRenderer.UpdateUI();
        }
    }
}