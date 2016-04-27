using BansheeGz.BGSpline.Curve;
using UnityEngine;

namespace BansheeGz.BGSpline.Editor
{

    //draws a curve in editor
    public class BGCurvePainterGizmo
    {
        protected BGCurve curve;
        protected BGCurveBaseMath curveBaseMath;

        public BGCurvePainterGizmo(BGCurve curve):this(curve,new BGCurveBaseMath(curve, false))
        {
        }

        public BGCurvePainterGizmo(BGCurve curve, BGCurveBaseMath math)
        {
            this.curve = curve;
            curveBaseMath = math;
        }


        public virtual void DrawCurve()
        {
            var settings = BGPrivateField.GetSettings(curve);
            var color = Gizmos.color;
            Gizmos.color = settings.LineColor;

            //========================================  Draw line
            for (var i = 0; i < curve.PointsCount - 1; i++) DrawSection(curve[i], curve[i + 1], settings.Sections);

            if (curve.Closed) DrawSection(curve[curve.PointsCount - 1], curve[0], settings.Sections);

            //========================================  Draw spheres
            if (settings.ShowSpheres)
            {
                Gizmos.color = settings.SphereColor;

                foreach (var point in curve.Points) DrawSphere(point.PositionWorld, settings.SphereRadius);
            }

            Gizmos.color = color;
        }

        public virtual void DrawSphere(Vector3 pos, float sphereRadius)
        {
            Gizmos.DrawSphere(pos, sphereRadius);
        }

        public void DrawSection(BGCurvePoint from, BGCurvePoint to, int sections)
        {
            var lastPosition = from.PositionWorld;
            for (var i = 1; i < sections + 1; i++)
            {
                var currentPosition = curveBaseMath.CalcPositionByT(@from, to, i / (float)sections);
                DrawLine(lastPosition, currentPosition);
                lastPosition = currentPosition;
            }
        }

        public virtual void DrawLine(Vector3 from, Vector3 to)
        {
            Gizmos.DrawLine(from, to);
        }
    }
}