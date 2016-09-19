using BansheeGz.BGSpline.Components;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
    [CustomEditor(typeof (BGCcCursorChangeLinear))]
    public class BGCcCursorChangeLinearEditor : BGCcEditor
    {
        protected override void InternalOnInspectorGUI()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("speed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("overflowControl"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("adjustByTotalLength"));
        }
    }
}