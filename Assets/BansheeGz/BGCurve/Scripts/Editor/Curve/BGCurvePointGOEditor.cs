using BansheeGz.BGSpline.Curve;
using UnityEditor;
using UnityEngine;
using UnityEngineInternal;

namespace BansheeGz.BGSpline.Editor
{
    //bug in editor while curve is rotated in 2d mode (position overflow to super big values while being dragged in SceneView)
    [CustomEditor(typeof(BGCurvePointGO))]
    public class BGCurvePointGOEditor : UnityEditor.Editor
    {
        public static bool PointSelected;

        private BGCurvePointI point;
        private BGCurveEditorPoint pointEditor;

        private BGCurveEditor curveEditor;
        private BGTransition.SwayTransition pointIndicatorTransition;

        protected virtual BGCurvePointI Point
        {
            get { return (BGCurvePointI) target; }
        }

        protected virtual void OnEnable()
        {
            point = Point;

            pointEditor = new BGCurveEditorPoint(() => null, null);

            if (curveEditor != null) curveEditor.OnDestroy();

            curveEditor = (BGCurveEditor) CreateEditor(point.Curve);

            PointSelected = true;
        }


        void OnDisable()
        {
            PointSelected = false;
            if (curveEditor != null) curveEditor.OnDisable();
        }

        void OnDestroy()
        {
            PointSelected = false;
            if (curveEditor != null) curveEditor.OnDestroy();
        }

        public override void OnInspectorGUI()
        {
            var curve = point.Curve;

            BGCurveEditorPoints.SwapVector2Labels(curve.Mode2D, () => pointEditor.OnInspectorGui(point, curve.IndexOf(point), BGPrivateField.GetSettings(curve)));
        }

        public virtual void OnSceneGUI()
        {
            BGEditorUtility.Assign(ref pointIndicatorTransition, () => new BGTransition.SwayTransition(30, 30, 1));

            BGSceneViewOverlay.DrawHandlesGuiTexture(BGEditorUtility.GetSceneViewPosition(point.PositionWorld), pointIndicatorTransition, BGBinaryResources.BGPointSelected123);


            curveEditor.OnSceneGUI();

            // animation is off for now
//            SceneView.RepaintAll();
        }


        [DrawGizmo(GizmoType.Selected)]
        public static void DrawGizmos(BGCurvePointGO point, GizmoType gizmoType)
        {
            BGCurveEditor.DrawGizmos(point.Curve, gizmoType);
        }
    }
}