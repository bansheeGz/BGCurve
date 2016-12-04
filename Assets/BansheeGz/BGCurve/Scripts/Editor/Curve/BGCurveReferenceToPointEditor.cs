using System.Linq;
using BansheeGz.BGSpline.Curve;
using UnityEditor;
using UnityEngine;

namespace BansheeGz.BGSpline.Editor
{
    [CustomEditor(typeof(BGCurveReferenceToPoint))]
    public class BGCurveReferenceToPointEditor : BGCurvePointGOEditor
    {
        private BGCurveReferenceToPoint pointReference;

        private BGTransformMonitor transformMonitor;

        protected override BGCurvePointI Point
        {
            get { return pointReference.Point; }
        }

        protected override void OnEnable()
        {
            pointReference = (BGCurveReferenceToPoint) target;

            var point = pointReference.Point;
            if (!IsValid(point))
            {
                //no need for it anymore
                DestroyImmediate(pointReference);
                return;
            }

            var allComponents = pointReference.GetComponents<BGCurveReferenceToPoint>();
            if (allComponents.Any(component => component != pointReference && component.Point == pointReference.Point))
            {
                DestroyImmediate(pointReference);
                return;
            }

            transformMonitor = BGTransformMonitor.GetMonitor(pointReference.transform, transform => point.Curve.FireChange(null));

            base.OnEnable();
        }

        public void OnDestroy()
        {
            if (transformMonitor != null) transformMonitor.Release();
            transformMonitor = null;
            pointReference = null;
        }


        private static bool IsValid(BGCurvePointI point)
        {
            return point != null && point.Curve != null && point.Curve.IndexOf(point) >= 0;
        }

        public override void OnInspectorGUI()
        {
            transformMonitor.CheckForChange();

            var point = pointReference.Point;

            if (!IsValid(point)) return;

            BGEditorUtility.DisableGui(() => EditorGUILayout.TextField("BGCurve", point.Curve.gameObject.name));

            base.OnInspectorGUI();
        }

        public override void OnSceneGUI()
        {
            var point = pointReference.Point;

            if (!IsValid(point)) return;

            transformMonitor.CheckForChange();

            base.OnSceneGUI();
        }

        [DrawGizmo(GizmoType.Selected)]
        public new static void DrawGizmos(BGCurve curve, GizmoType gizmoType)
        {
            BGCurvePointGOEditor.DrawGizmos(curve, gizmoType);
        }
    }
}