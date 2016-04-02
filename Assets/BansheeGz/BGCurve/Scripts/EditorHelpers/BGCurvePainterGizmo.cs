using BansheeGz.BGSpline.Curve;
using UnityEngine;

namespace BansheeGz.BGSpline.EditorHelpers
{

#if UNITY_EDITOR
    // ========================== This class is supposed to work in Editor ONLY

    //draws a curve in editor
    public class BGCurvePainterGizmo
    {
        private BGCurve curve;
        private BGCurveBaseMath curveBaseMath;
        protected BGCurveSettings settings;

        public BGCurvePainterGizmo(BGCurve curve):this(curve,new BGCurveBaseMath(curve, false))
        {
        }

        public BGCurvePainterGizmo(BGCurve curve, BGCurveBaseMath math)
        {
            this.curve = curve;
            curveBaseMath = math;
            settings = BGPrivateField.GetSettings(curve);
        }


        public virtual void DrawCurve()
        {
            var color = Gizmos.color;
            Gizmos.color = settings.LineColor;

            //========================================  Draw line
            for (var i = 0; i < curve.Points.Length - 1; i++)
            {
                DrawSection(curve.Points[i], curve.Points[i + 1], settings.Sections);
            }

            if (curve.Closed)
            {
                DrawSection(curve.Points[curve.Points.Length - 1], curve.Points[0], settings.Sections);
            }

            //========================================  Draw spheres
            if (settings.ShowSpheres)
            {
                Gizmos.color = settings.SphereColor;
                foreach (var point in curve.Points)
                {
                    DrawSphere(point.PositionWorld, settings.SphereRadius);
                }
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
#endif
}