using UnityEngine;
using BansheeGz.BGSpline.Curve;

namespace BansheeGz.BGSpline.Components
{
    /// <summary>Identify the position on the curve. </summary>
    [HelpURL("http://www.bansheegz.com/BGCurve/Cc/BGCcCursor")]
    [
        CcDescriptor(
            Description = "Identify location on the curve by distance.",
            Name = "Cursor",
            Image = "Assets/BansheeGz/BGCurve/Icons/Components/BGCcCursor123.png")
    ]
    [AddComponentMenu("BansheeGz/BGCurve/Components/BGCcCursor", 1)]
    public class BGCcCursor : BGCcWithMath
    {
        [SerializeField] [Tooltip("Distance from start of the curve.")] private float distance;


        public float Distance
        {
            get { return distance; }
            set
            {
                distance = Math.ClampDistance(value);
                FireChangedParams();
            }
        }

        public float DistanceRatio
        {
            get { return Mathf.Clamp01(distance/Math.GetDistance()); }
            set
            {
                distance = Math.GetDistance()*Mathf.Clamp01(value);
                FireChangedParams();
            }
        }

        public override bool SupportHandles
        {
            get { return true; }
        }

        public override bool SupportHandlesSettings
        {
            get { return true; }
        }

#if UNITY_EDITOR
        [Range(.5f, 1.5f)] [SerializeField] private float handlesScale = 1;
        [SerializeField] private Color handlesColor = Color.white;

        public float HandlesScale
        {
            get { return handlesScale; }
            set { handlesScale = value; }
        }

        public Color HandlesColor
        {
            get { return handlesColor; }
            set { handlesColor = value; }
        }
#endif


        public Vector3 CalculateTangent()
        {
            return Math.CalcByDistance(BGCurveBaseMath.Field.Tangent, distance);
        }

        public Vector3 CalculatePosition()
        {
            return Math.CalcByDistance(BGCurveBaseMath.Field.Position, distance);
        }

        public override void Start()
        {
            //clamp
            Distance = distance;
        }
    }
}