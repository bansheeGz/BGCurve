using System;
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
            Icon = "BGCcCursor123")
    ]
    [AddComponentMenu("BansheeGz/BGCurve/Components/BGCcCursor")]
    public class BGCcCursor : BGCcWithMath
    {
        //===============================================================================================
        //                                                    Fields
        //===============================================================================================

        [SerializeField] [Tooltip("Distance from start of the curve.")]
        protected float distance;

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
            get
            {
                var totalDistance = Math.GetDistance();
                return totalDistance == 0 ? 0 : Mathf.Clamp01(distance / totalDistance);
            }
            set
            {
                distance = Math.GetDistance() * Mathf.Clamp01(value);
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
        //                                                    Lerp values
        //===============================================================================================
        /// <summary> Custom Lerp function</summary>
        public TR Lerp<T, TR>(string fieldName, Func<T, T, float, TR> lerpFunction)
        {
            if (Curve.PointsCount == 0) return lerpFunction(default(T), default(T), 0);

            T from, to;
            var t = GetAdjacentFieldValues(fieldName, out from, out to);
            return lerpFunction(from, to, t);
        }


        /// <summary> Lerp 2 Quaternion field values by current cursor position</summary>
        public Quaternion LerpQuaternion(string fieldName, Func<Quaternion, Quaternion, float, Quaternion> customLerp = null)
        {
            if (Curve.PointsCount == 0) return Quaternion.identity;

            Quaternion from, to;
            var t = GetAdjacentFieldValues(fieldName, out from, out to);

            //not sure how to handle zero cases
            if (from.x == 0 && from.y == 0 && from.z == 0 && from.w == 0) from = Quaternion.identity;
            if (to.x == 0 && to.y == 0 && to.z == 0 && to.w == 0) to = Quaternion.identity;

            //lerp
            var result = customLerp == null ? Quaternion.Lerp(@from, to, t) : customLerp(@from, to, t);
            return float.IsNaN(result.x) || float.IsNaN(result.y) || float.IsNaN(result.z) || float.IsNaN(result.w) ? Quaternion.identity : result;
        }

        /// <summary> Lerp 2 Vector3 field values by current cursor position</summary>
        public Vector3 LerpVector(string fieldName, Func<Vector3, Vector3, float, Vector3> customLerp = null)
        {
            if (Curve.PointsCount == 0) return Vector3.zero;

            Vector3 from, to;
            var t = GetAdjacentFieldValues(fieldName, out from, out to);
            //lerp
            return customLerp == null ? Vector3.Lerp(@from, to, t) : customLerp(@from, to, t);
        }

        /// <summary> Lerp 2 Vector3 field values by current cursor position</summary>
        public float LerpFloat(string fieldName, Func<float, float, float, float> customLerp = null)
        {
            if (Curve.PointsCount == 0) return 0;

            float from, to;
            var t = GetAdjacentFieldValues(fieldName, out from, out to);
            //lerp
            return customLerp == null ? Mathf.Lerp(@from, to, t) : customLerp(@from, to, t);
        }

        /// <summary> Lerp 2 Vector3 field values by current cursor position</summary>
        public Color LerpColor(string fieldName, Func<Color, Color, float, Color> customLerp = null)
        {
            if (Curve.PointsCount == 0) return Color.clear;

            Color from, to;
            var t = GetAdjacentFieldValues(fieldName, out from, out to);
            //lerp
            return customLerp == null ? Color.Lerp(@from, to, t) : customLerp(@from, to, t);
        }

        /// <summary> get surrounding point field values (optionally currentSection is provided to reduce required calculation). return T value for interpolation </summary>
        public float GetAdjacentFieldValues<T>(string fieldName, out T fromValue, out T toValue)
        {
            int indexFrom, indexTo;
            var t = GetTForLerp(out indexFrom, out indexTo);

            fromValue = Curve[indexFrom].GetField<T>(fieldName);
            toValue = Curve[indexTo].GetField<T>(fieldName);

            return t;
        }

        /// <summary> get surrounding point field values (optionally currentSection is provided to reduce required calculation). return T value for interpolation </summary>
        public float GetAdjacentFieldValues(string fieldName, out float fromValue, out float toValue)
        {
            int indexFrom, indexTo;
            var t = GetTForLerp(out indexFrom, out indexTo);

            fromValue = Curve[indexFrom].GetFloat(fieldName);
            toValue = Curve[indexTo].GetFloat(fieldName);

            return t;
        }

        /// <summary> get surrounding point field values (optionally currentSection is provided to reduce required calculation). return T value for interpolation </summary>
        public float GetAdjacentFieldValues(string fieldName, out int fromValue, out int toValue)
        {
            int indexFrom, indexTo;
            var t = GetTForLerp(out indexFrom, out indexTo);

            fromValue = Curve[indexFrom].GetInt(fieldName);
            toValue = Curve[indexTo].GetInt(fieldName);

            return t;
        }

        /// <summary> get surrounding point field values (optionally currentSection is provided to reduce required calculation). return T value for interpolation </summary>
        public float GetAdjacentFieldValues(string fieldName, out bool fromValue, out bool toValue)
        {
            int indexFrom, indexTo;
            var t = GetTForLerp(out indexFrom, out indexTo);

            fromValue = Curve[indexFrom].GetBool(fieldName);
            toValue = Curve[indexTo].GetBool(fieldName);

            return t;
        }

        /// <summary> get surrounding point field values (optionally currentSection is provided to reduce required calculation). return T value for interpolation </summary>
        public float GetAdjacentFieldValues(string fieldName, out Bounds fromValue, out Bounds toValue)
        {
            int indexFrom, indexTo;
            var t = GetTForLerp(out indexFrom, out indexTo);

            fromValue = Curve[indexFrom].GetBounds(fieldName);
            toValue = Curve[indexTo].GetBounds(fieldName);

            return t;
        }

        /// <summary> get surrounding point field values (optionally currentSection is provided to reduce required calculation). return T value for interpolation </summary>
        public float GetAdjacentFieldValues(string fieldName, out Color fromValue, out Color toValue)
        {
            int indexFrom, indexTo;
            var t = GetTForLerp(out indexFrom, out indexTo);

            fromValue = Curve[indexFrom].GetColor(fieldName);
            toValue = Curve[indexTo].GetColor(fieldName);

            return t;
        }

        /// <summary> get surrounding point field values (optionally currentSection is provided to reduce required calculation). return T value for interpolation </summary>
        public float GetAdjacentFieldValues(string fieldName, out Quaternion fromValue, out Quaternion toValue)
        {
            int indexFrom, indexTo;
            var t = GetTForLerp(out indexFrom, out indexTo);

            fromValue = Curve[indexFrom].GetQuaternion(fieldName);
            toValue = Curve[indexTo].GetQuaternion(fieldName);

            return t;
        }

        /// <summary> get surrounding point field values (optionally currentSection is provided to reduce required calculation). return T value for interpolation </summary>
        public float GetAdjacentFieldValues(string fieldName, out Vector3 fromValue, out Vector3 toValue)
        {
            int indexFrom, indexTo;
            var t = GetTForLerp(out indexFrom, out indexTo);

            fromValue = Curve[indexFrom].GetVector3(fieldName);
            toValue = Curve[indexTo].GetVector3(fieldName);

            return t;
        }

        /// <summary> get points indexes by cursor position</summary>
        public float GetTForLerp(out int indexFrom, out int indexTo)
        {
            GetAdjacentPointIndexes(out indexFrom, out indexTo);

            //get t value
            var section = Math[indexFrom];
            var t = (distance - section.DistanceFromStartToOrigin) / section.Distance;

            return t;
        }

        /// <summary> get from point index</summary>
        public void GetAdjacentPointIndexes(out int indexFrom, out int indexTo)
        {
            indexFrom = CalculateSectionIndex();
            indexTo = indexFrom == Curve.PointsCount - 1 ? 0 : indexFrom + 1;
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