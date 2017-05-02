using BansheeGz.BGSpline.Curve;
using UnityEditor;
using UnityEngine;

namespace BansheeGz.BGSpline.Editor
{
    public class BGCurvePainterHandles : BGCurvePainterGizmo
    {
        private Color tempColorBeforeSpheresDraw;

        public BGCurvePainterHandles(BGCurveBaseMath math)
            : base(math)
        {
        }

        public override void DrawCurve()
        {
            BGEditorUtility.SwapHandlesColor(BGPrivateField.GetSettings(Math.Curve).LineColor, () => base.DrawCurve());
        }

        protected override void BeforeDrawingSpheres(BGCurveSettings settings)
        {
            tempColorBeforeSpheresDraw = Handles.color;
            Handles.color = settings.SphereColor;
        }

        protected override void AfterDrawingSpheres()
        {
            Handles.color = tempColorBeforeSpheresDraw;
        }

        public override void DrawSphere(BGCurveSettings settings, Vector3 pos, float sphereRadius)
        {
#if UNITY_5_6_OR_NEWER
			Handles.SphereHandleCap(0, pos, Quaternion.identity, sphereRadius * 2, EventType.Repaint);
#else
			Handles.SphereCap(0, pos, Quaternion.identity, sphereRadius*2);
#endif
        }

        protected override void DrawLine(Vector3 @from, Vector3 to)
        {
            Handles.DrawLine(from, to);
        }
    }
}