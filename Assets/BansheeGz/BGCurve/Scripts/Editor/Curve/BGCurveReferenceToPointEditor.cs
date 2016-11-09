using BansheeGz.BGSpline.Curve;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
    [CustomEditor(typeof(BGCurveReferenceToPoint))]
    public class BGCurveReferenceToPointEditor : BGCurvePointGOEditor
    {
        private BGCurveReferenceToPoint pointReference;

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
            if (allComponents.Length > 0)
            {
                foreach (var component in allComponents)
                {
                    if (component == pointReference || component.Point!= pointReference.Point) continue;

                    //duplicate
                    DestroyImmediate(pointReference);
                    return;
                }
            }

            base.OnEnable();
        }

        protected override BGCurvePointI GetPoint()
        {
            return pointReference.Point;
        }

        private static bool IsValid(BGCurvePointI point)
        {
            return point != null && point.Curve != null && point.Curve.IndexOf(point) >= 0;
        }

        public override void OnInspectorGUI()
        {
            var point = pointReference.Point;

            if (!IsValid(point)) return;

            BGEditorUtility.DisableGui(() => EditorGUILayout.TextField("BGCurve", point.Curve.gameObject.name));

            base.OnInspectorGUI();
        }

        public override void OnSceneGUI()
        {
            var point = pointReference.Point;

            if (!IsValid(point)) return;

            base.OnSceneGUI();
        }

        [DrawGizmo(GizmoType.Selected)]
        public new static void DrawGizmos(BGCurve curve, GizmoType gizmoType)
        {
            BGCurvePointGOEditor.DrawGizmos(curve, gizmoType);
        }
    }
}