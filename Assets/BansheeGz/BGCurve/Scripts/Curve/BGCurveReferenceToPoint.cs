using UnityEngine;

namespace BansheeGz.BGSpline.Curve
{
    /// <summary>This is an indicator class, showing that GameObject is used as transform for Curve's point. It is used by Editor only </summary>
    public class BGCurveReferenceToPoint : MonoBehaviour
    {
        [SerializeField] private BGCurvePointComponent pointComponent;
        [SerializeField] private BGCurvePointGO pointGo;

        /// <summary>referenced point </summary>
        public BGCurvePointI Point
        {
            get { return pointGo != null ? (BGCurvePointI) pointGo : pointComponent; }
            set
            {
                if (value == null)
                {
                    pointGo = null;
                    pointComponent = null;
                }
                else
                {
                    if (value is BGCurvePointGO)
                    {
                        pointGo = (BGCurvePointGO) value;
                        pointComponent = null;
                    }
                    else if (value is BGCurvePointComponent)
                    {
                        pointComponent = (BGCurvePointComponent) value;
                        pointGo = null;
                    }
                    else
                    {
                        pointGo = null;
                        pointComponent = null;
                    }
                }
            }
        }

        /// <summary>find referenced point, attached to target gameobject </summary>
        public static BGCurveReferenceToPoint GetReferenceToPoint(BGCurvePointI point)
        {
            if (point.PointTransform == null) return null;
            var referencesToPoints = point.PointTransform.GetComponents<BGCurveReferenceToPoint>();
            if (referencesToPoints.Length == 0) return null;


            var length = referencesToPoints.Length;
            for (var i = 0; i < length; i++)
            {
                var referencesToPoint = referencesToPoints[i];
                if (referencesToPoint.Point == point) return referencesToPoint;
            }
            return null;
        }
    }
}