using BansheeGz.BGSpline.Curve;
using BansheeGz.BGSpline.EditorHelpers;
using UnityEditor;
using UnityEngine;

namespace BansheeGz.BGSpline.Editor
{
    public class BGCurvePainterHandles : BGCurvePainterGizmo
    {
        public BGCurvePainterHandles(BGCurve curve) : base(curve)
        {
        }

        public BGCurvePainterHandles(BGCurve curve, BGCurveBaseMath math) : base(curve, math)
        {
        }

        public override void DrawCurve()
        {
            var color = Handles.color;
            Handles.color = settings.LineColor;
            base.DrawCurve();
            Handles.color = color;
        }

        public override void DrawSphere(Vector3 pos, float sphereRadius)
        {
            var color = Handles.color;
            Handles.color = settings.SphereColor;
            Handles.SphereCap(0, pos, Quaternion.identity, sphereRadius*2);
            Handles.color = color;
        }

        public override void DrawLine(Vector3 from, Vector3 to)
        {
            Handles.DrawLine(from, to);
        }
    }
}