using UnityEngine;

namespace BansheeGz.BGSpline.Curve
{
    /// <summary> Spline related formulas.</summary>
    public static class BGCurveFormulas
    {
        /// <summary> Cubic Bezier curve interpolation. <see href="https://en.wikipedia.org/wiki/Bézier_curve">More info</see></summary>
        // P0=from, P1=fromControl, P2=toControl, P3=to
        public static Vector3 BezierCubic(float t, Vector3 from, Vector3 fromControl, Vector3 toControl, Vector3 to)
        {
            // tr= t reverse
            var tr = 1 - t;
            var tr2 = tr*tr;
            var t2 = t*t;

            return tr*tr2*from + 3*tr2*t*fromControl + 3*tr*t2*toControl + t*t2*to;
        }

        /// <summary> Quadratic Bezier curve interpolation. <see href="https://en.wikipedia.org/wiki/Bézier_curve">More info</see></summary>
        // P0=from, P1=control, P2 = to
        public static Vector3 BezierQuadratic(float t, Vector3 from, Vector3 control, Vector3 to)
        {
            // tr= t reverse
            var tr = 1 - t;
            var tr2 = tr*tr;
            var t2 = t*t;

            return tr2*from + 2*tr*t*control + t2*to;
        }

        /// <summary> Cubic Bezier curve derivative. <see href="https://en.wikipedia.org/wiki/Bézier_curve">More info</see> </summary>
        // P0=from, P1=fromControl, P2=toControl, P3=to
        public static Vector3 BezierCubicDerivative(float t, Vector3 from, Vector3 fromControl, Vector3 toControl, Vector3 to)
        {
            // tr= t reverse
            var tr = 1 - t;

            return 3*(tr*tr)*(fromControl - from) + 6*tr*t*(toControl - fromControl) + 3*(t*t)*(to - toControl);
        }

        /// <summary> Quadratic Bezier curve derivative. <see href="https://en.wikipedia.org/wiki/Bézier_curve">More info</see></summary>
        // P0=from, P1=control, P2 = to
        public static Vector3 BezierQuadraticDerivative(float t, Vector3 from, Vector3 control, Vector3 to)
        {
            // tr= t reverse
            var tr = 1 - t;

            return 2*tr*(control - from) + 2*t*(to - control);
        }
    }
}