using BansheeGz.BGSpline.Components;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
    [CustomEditor(typeof (BGCcVisualizationLineRenderer))]
    public class BGCcVisualizationLineRendererEditor : BGCcSplitterPolylineEditor
    {

        private BGCcVisualizationLineRenderer LineRenderer
        {
            get { return (BGCcVisualizationLineRenderer) cc; }
        }

        protected override void InternalOnInspectorGUI()
        {
            base.InternalOnInspectorGUI();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("updateAtStart"));
            
        }

        protected override void AdditionalParams()
        {
            //we no need useLocal param, cause it depends on LineRenderer itself
        }

        protected override void InternalOnInspectorGUIPost()
        {
            if (paramsChanged) LineRenderer.UpdateUI();
        }
    }
}