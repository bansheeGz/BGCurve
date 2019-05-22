using UnityEngine;

namespace BansheeGz.BGSpline.Curve
{
    /// <summary> This is an interface for curve's Math solver</summary>
    public interface BGCurveMathI
    {
        //===============================================================================================
        //                                                    Generic (position or tangent)
        //===============================================================================================

        /// <summary> Calculate spline's field value by distance ratio.  </summary>
        /// <param name="field">field to retrieve (like position or tangent etc.)</param>
        /// <param name="distanceRatio">Ratio between (0,1)</param>
        /// <param name="useLocal">Use local coordinates instead of world</param>
        /// <returns>result field's value</returns>
        Vector3 CalcByDistanceRatio(BGCurveBaseMath.Field field, float distanceRatio, bool useLocal = false);

        /// <summary> Calculate spline's field value by distance.</summary>
        /// <param name="field">field to retrieve</param>
        /// <param name="distance">distance from the curve's start between (0, GetDistance())</param>
        /// <param name="useLocal">Use local coordinates instead of world</param>
        /// <returns>result field's value</returns>
        Vector3 CalcByDistance(BGCurveBaseMath.Field field, float distance, bool useLocal = false);

        //===============================================================================================
        //                                                    Position and tangent
        //===============================================================================================
        /// <summary> Calculate both spline's fields (position and tangent) by distance ratio. </summary>
        /// <param name="distanceRatio">Ratio between (0,1)</param>
        /// <param name="tangent">result tangent</param>
        /// <param name="useLocal">Use local coordinates instead of world</param>
        /// <returns>result position</returns>
        Vector3 CalcByDistanceRatio(float distanceRatio, out Vector3 tangent, bool useLocal = false);

        /// <summary> Calculate both curve's fields (position and tangent) by distance. </summary>
        /// <param name="distance">distance from the curve's start between (0, GetDistance())</param>
        /// <param name="tangent">result tangent</param>
        /// <param name="useLocal">Use local coordinates instead of world</param>
        /// <returns>result position</returns>
        Vector3 CalcByDistance(float distance, out Vector3 tangent, bool useLocal = false);

        /// <summary> Calculate approximate spline's point position using distance ratio. </summary>
        /// <param name="distanceRatio">Ratio between (0,1)</param>
        /// <param name="tangent">result tangent</param>
        /// <param name="useLocal">Use local coordinates instead of world</param>
        /// <returns>result position</returns>
        Vector3 CalcPositionAndTangentByDistanceRatio(float distanceRatio, out Vector3 tangent, bool useLocal = false);

        /// <summary> Calculate approximate spline's point position using distance. </summary>
        /// <param name="distance">distance from curve's start between (0, GetDistance())</param>
        /// <param name="tangent">result tangent</param>
        /// <param name="useLocal">Use local coordinates instead of world </param>
        /// <returns>result position</returns>
        Vector3 CalcPositionAndTangentByDistance(float distance, out Vector3 tangent, bool useLocal = false);

        //===============================================================================================
        //                                                    Position
        //===============================================================================================
        /// <summary> Calculate approximate spline's point position using distance ratio.</summary>
        /// <param name="distanceRatio">Ratio between (0,1)</param>
        /// <param name="useLocal">Use local coordinates instead of world</param>
        Vector3 CalcPositionByDistanceRatio(float distanceRatio, bool useLocal = false);

        /// <summary> Calculate approximate spline's point position using distance.  </summary>
        /// <param name="distance">distance from curve's start between (0, GetDistance())</param>
        /// <param name="useLocal">Use local coordinates instead of world </param>
        Vector3 CalcPositionByDistance(float distance, bool useLocal = false);

        //===============================================================================================
        //                                                    Tangent
        //===============================================================================================
        /// <summary> Calculate approximate spline's tangent using distance ratio. </summary>
        /// <param name="distanceRatio">Ratio between (0,1)</param>
        /// <param name="useLocal">Use local coordinates instead of world </param>
        Vector3 CalcTangentByDistanceRatio(float distanceRatio, bool useLocal = false);

        /// <summary> Calculate approximate spline's tangent using distance. </summary>
        /// <param name="distance">distance from curve's start between (0, GetDistance())</param>
        /// <param name="useLocal">Use local coordinates instead of world</param>
        Vector3 CalcTangentByDistance(float distance, bool useLocal = false);

        //===============================================================================================
        //                                                    Section index
        //===============================================================================================
        /// <summary> Calculate spline's section using distance. </summary>
        /// <param name="distance">distance from curve's start between (0, GetDistance())</param>
        int CalcSectionIndexByDistance(float distance);

        /// <summary> Calculate spline's section using distance ratio. </summary>
        /// <param name="distanceRatio">Ratio between (0,1)</param>
        int CalcSectionIndexByDistanceRatio(float ratio);

        //===============================================================================================
        //                                                    By closest point
        //===============================================================================================
        /// <summary> Calculate spline's world point position by a point, which is closest to a given point.</summary>
        /// <param name="point">Point's position</param>
        /// <param name="skipSectionsOptimization">Skip any optimization at section level, if any</param>
        /// <param name="skipPointsOptimization">Skip any optimization at approximation points level, if any</param>
        /// <returns>Position on the spline, which is closest to a given point</returns>
        Vector3 CalcPositionByClosestPoint(Vector3 point, bool skipSectionsOptimization = false, bool skipPointsOptimization = false);

        /// <summary> Calculate spline's world point position and distance by a point, which is closest to a given point.</summary>
        /// <param name="point">Point's position</param>
        /// <param name="distance">Result distance from the start of the spline</param>
        /// <param name="skipSectionsOptimization">Skip any optimization at section level, if any</param>
        /// <param name="skipPointsOptimization">Skip any optimization at approximation points level, if any</param>
        /// <returns>Position on the spline, which is closest to a given point</returns>
        Vector3 CalcPositionByClosestPoint(Vector3 point, out float distance, bool skipSectionsOptimization = false, bool skipPointsOptimization = false);

        /// <summary> Calculate spline's world point position, distance and tangent by a point, which is closest to a given point.</summary>
        /// <param name="point">Point's position</param>
        /// <param name="distance">Result distance from the start of the spline</param>
        /// <param name="tangent">Result tangent</param>
        /// <param name="skipSectionsOptimization">Skip any optimization at section level, if any</param>
        /// <param name="skipPointsOptimization">Skip any optimization at approximation points level, if any</param>
        /// <returns>Position on the spline, which is closest to a given point</returns>
        Vector3 CalcPositionByClosestPoint(Vector3 point, out float distance, out Vector3 tangent, bool skipSectionsOptimization = false, bool skipPointsOptimization = false);

        //===============================================================================================
        //                                                  Total spline's length or distance to a point
        //===============================================================================================
        /// <summary>Get spline's approximate total distance or distance from start to point</summary>
        /// <param name="pointIndex">Point's index</param>
        /// <returns>Spline's total distance</returns>
        float GetDistance(int pointIndex = -1);
        
        //===============================================================================================
        //                                                  Misc
        //===============================================================================================
        /// <summary>Recalculate internal caches. this is a costly operation </summary>
        void Recalculate(bool force = false);
    }
}