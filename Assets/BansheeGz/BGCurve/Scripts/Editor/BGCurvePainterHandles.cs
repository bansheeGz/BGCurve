using BansheeGz.BGSpline.Curve;
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
            var settings = BGPrivateField.GetSettings(curve);

            BGEditorUtility.SwapHandlesColor(settings.LineColor, () => base.DrawCurve());
        }

        public override void DrawSphere(Vector3 pos, float sphereRadius)
        {
            var settings = BGPrivateField.GetSettings(curve);

            BGEditorUtility.SwapHandlesColor(settings.SphereColor, () => Handles.SphereCap(0, pos, Quaternion.identity, sphereRadius*2));
        }

        public override void DrawLine(Vector3 from, Vector3 to)
        {
            Handles.DrawLine(from, to);
        }
    }
}