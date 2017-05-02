using UnityEngine;
using BansheeGz.BGSpline.Components;
using UnityEditor;

//add points filter + pager for Editor

namespace BansheeGz.BGSpline.Editor
{
    [CustomEditor(typeof (BGCcCursor))]
    public class BGCcCursorEditor : BGCcEditor
    {
        private BGCcCursor Cursor
        {
            get { return (BGCcCursor) cc; }
        }

        protected override void ShowHandlesSettings()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("handlesScale"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("handlesColor"));
        }

        protected override void InternalOnInspectorGUI()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("distance"));

            var distanceRatio = Cursor.DistanceRatio;
            var newValue = EditorGUILayout.Slider("Distance Ratio", distanceRatio, 0, 1);
            if (BGEditorUtility.AnyChange(distanceRatio, newValue)) Cursor.DistanceRatio = newValue;
        }

        protected override void InternalOnSceneGUI()
        {
            var cursor = Cursor;

            if (cursor == null) return;

            var position = cursor.CalculatePosition();

            var handleSize = BGEditorUtility.GetHandleSize(position, BGPrivateField.Get<float>(cursor, "handlesScale"));
            BGEditorUtility.SwapHandlesColor(BGPrivateField.Get<Color>(cursor, "handlesColor"), () =>
            {
#if UNITY_5_6_OR_NEWER
                  Handles.ArrowHandleCap(0, position + Vector3.up * handleSize * 1.2f, Quaternion.LookRotation(Vector3.down), handleSize, EventType.Repaint);
                  Handles.SphereHandleCap(0, position, Quaternion.LookRotation(Vector3.down), handleSize * .15f, EventType.Repaint);
#else
                  Handles.ArrowCap(0, position + Vector3.up*handleSize*1.2f, Quaternion.LookRotation(Vector3.down), handleSize);
                  Handles.SphereCap(0, position, Quaternion.LookRotation(Vector3.down), handleSize*.15f);
#endif


		});
        }
    }
}