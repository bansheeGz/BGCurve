using UnityEngine;
using BansheeGz.BGSpline.Curve;

namespace BansheeGz.BGSpline.Components
{
    /// <summary>Identify the position on the curve by the distance from the start. </summary>
    [HelpURL("http://www.bansheegz.com/BGCurve/Cc/BGCcCursor")]
    [
        CcDescriptor(
            Description = "Identify location on the curve by distance.",
            Name = "Cursor",
            Image = "Assets/BansheeGz/BGCurve/Icons/Components/BGCcCursor123.png")
    ]
    [AddComponentMenu("BansheeGz/BGCurve/Components/BGCcCursor")]
    public class BGCcCursor : BGCcWithMath
    {
        //===============================================================================================
        //                                                    Fields
        //===============================================================================================

        [SerializeField] [Tooltip("Distance from start of the curve.")] private float distance;

        /// <summary>Distance from the start </summary>
        public float Distance
        {
            get { return distance; }
            set
            {
                distance = Math.ClampDistance(value);
                FireChangedParams();
            }
        }

        /// <summary>Normalized distance from the start [Range(0,1)]</summary>
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

        //===============================================================================================
        //                                                    Public functions
        //===============================================================================================
        /// <summary>Calculates tangent by current distance </summary>
        public Vector3 CalculateTangent()
        {
            return Math.CalcByDistance(BGCurveBaseMath.Field.Tangent, distance);
        }

        /// <summary>Calculates world position by current distance </summary>
        public Vector3 CalculatePosition()
        {
            return Math.CalcByDistance(BGCurveBaseMath.Field.Position, distance);
        }

        /// <summary>Calculates section's index by current distance </summary>
        public int CalculateSectionIndex()
        {
            return Math.CalcSectionIndexByDistance(distance);
        }

        //===============================================================================================
        //                                                    Unity Callbacks
        //===============================================================================================
        public override void Start()
        {
            //clamp
            Distance = distance;
        }
    }
}