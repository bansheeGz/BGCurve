using BansheeGz.BGSpline.Components;
using BansheeGz.BGSpline.Curve;
using UnityEngine;

namespace BansheeGz.BGSpline.Editor
{
    //draws a curve in editor
    public class BGCurvePainterGizmo
    {
        public BGCurveBaseMath Math;

        private readonly BGTransformMonitor transformMonitor;

        public BGCurvePainterGizmo(BGCurveBaseMath math, bool monitorTransform = false)
        {
            Math = math;
            if (monitorTransform) transformMonitor = new BGTransformMonitor(math.Curve);
        }

        public virtual void DrawCurve()
        {
            if (transformMonitor != null) transformMonitor.Check();

            var settings = BGPrivateField.GetSettings(Math.Curve);

            BGEditorUtility.SwapGizmosColor(settings.LineColor, () =>
            {
                //========================================  Draw section
                for (var i = 0; i < Math.SectionsCount; i++) DrawSection(Math[i]);
            });


            //========================================  Draw spheres
            if (settings.ShowSpheres)
            {
                BGEditorUtility.SwapGizmosColor(settings.SphereColor, () =>
                {
                    BeforeDrawingSpheres(settings);
                    for (var i = 0; i < Math.Curve.PointsCount; i++) DrawSphere(settings, Math.GetPosition(i), settings.SphereRadius);
                    AfterDrawingSpheres();
                });
            }
        }

        protected virtual void BeforeDrawingSpheres(BGCurveSettings settings)
        {
        }

        protected virtual void AfterDrawingSpheres()
        {
        }

        public virtual void DrawSphere(BGCurveSettings settings, Vector3 pos, float sphereRadius)
        {
            Gizmos.DrawSphere(pos, sphereRadius);
        }

        protected void DrawSection(BGCurveBaseMath.SectionInfo section)
        {
            if (section.PointsCount < 2) return;

            var points = section.Points;
            var prevPoint = points[0];
            for (var i = 1; i < points.Length; i++)
            {
                var nexPoint = points[i];
                DrawLine(prevPoint.Position, nexPoint.Position);
                prevPoint = nexPoint;
            }
        }

        protected virtual void DrawLine(Vector3 from, Vector3 to)
        {
            Gizmos.DrawLine(from, to);
        }
    }
}