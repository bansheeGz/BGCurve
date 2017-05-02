using BansheeGz.BGSpline.Components;
using UnityEditor;
using UnityEngine;

namespace BansheeGz.BGSpline.Editor
{
    [CustomEditor(typeof(BGCcSplitterPolyline))]
    public class BGCcSplitterPolylineEditor : BGCcEditor
    {
        protected bool paramsChanged;
        private bool listenersAdded;

        private BGCcSplitterPolyline Splitter
        {
            get { return (BGCcSplitterPolyline) cc; }
        }

        protected override void InternalOnEnable()
        {
            if (!Splitter.enabled) return;

            if (Application.isPlaying) return;
            Splitter.AddListeners();
            listenersAdded = true;
            Splitter.InvalidateData();
        }

        protected override void InternalOnDestroy()
        {
            if (Application.isPlaying) return;

            if (Splitter != null) Splitter.RemoveListeners();
        }

        protected override void InternalOnInspectorGUI()
        {
            if (!listenersAdded) InternalOnEnable();

            paramsChanged = false;
            BGEditorUtility.ChangeCheck(() =>
            {
                BGEditorUtility.VerticalBox(() =>
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("splitMode"));

                    switch (Splitter.SplitMode)
                    {
                        case BGCcSplitterPolyline.SplitModeEnum.PartsTotal:
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("partsTotal"));
                            break;
                        case BGCcSplitterPolyline.SplitModeEnum.PartsPerSection:
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("partsPerSection"));
                            break;
                    }
                });

                EditorGUILayout.PropertyField(serializedObject.FindProperty("doNotOptimizeStraightLines"));

                AdditionalParams();
            }, () => paramsChanged = true);
        }

        protected virtual void AdditionalParams()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useLocal"));
        }

        protected override void ShowHandlesSettings()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("spheresScale"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("spheresColor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("spheresCount"));
        }

        protected override void InternalOnSceneGUI()
        {
            var splitter = Splitter;
            if (splitter == null) return;

            var positions = splitter.Positions;

            if (positions == null || positions.Count == 0) return;

            var sphereScale = BGPrivateField.Get<float>(splitter, "spheresScale");

            BGEditorUtility.SwapHandlesColor(BGPrivateField.Get<Color>(splitter, "spheresColor"), () =>
            {
                var count = Mathf.Min(positions.Count, BGPrivateField.Get<int>(splitter, "spheresCount"));

                var localToWorldMatrix = splitter.transform.localToWorldMatrix;
                for (var i = 0; i < count; i++)
                {
                    var position = positions[i];
                    if (splitter.UseLocal) position = localToWorldMatrix.MultiplyPoint(position);

#if UNITY_5_6_OR_NEWER
				Handles.SphereHandleCap(0, position, Quaternion.identity, sphereScale * BGEditorUtility.GetHandleSize(position, .07f), EventType.Repaint);
#else
				Handles.SphereCap(0, position, Quaternion.identity, sphereScale*BGEditorUtility.GetHandleSize(position, .07f));
#endif
                }
            });
        }
    }
}