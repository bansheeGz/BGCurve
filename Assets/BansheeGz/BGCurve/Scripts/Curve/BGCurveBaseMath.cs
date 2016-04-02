using System;
using UnityEngine;

namespace BansheeGz.BGSpline.Curve
{
    /// <summary>
    ///  VERY basic math operations for curves.
    ///  It's not optimized by any means and serves as an example only.
    ///  You can easily extend this class or create your own from scratch for more better optimized functions using this class as an example. 
    /// </summary>
    public class BGCurveBaseMath
    {
        private BGCurve curve;

        //number of sections for each curve's part
        private int sections;

        //cached data for length calculations
        private SectionInfo[] cachedSectionInfos;
        //curve's length
        private float cachedLength;

        //if set to true, tangent will be calculated by using point's positions (instead of derivative function)
        public bool UsePointPositionToCalcTangent { get; set; }

        public BGCurve Curve
        {
            get { return curve; }
        }

        public SectionInfo[] SectionInfos
        {
            get { return cachedSectionInfos; }
        }

        /// <param name="traceChanges">use traceChanges if Curve will be changing (pos rot scale &amp; any change to  points) .Keep in mind traceChanges set to true will be resetting Transform.hasChanged field</param>
        /// <param name="sections">how much sections will be used to split  a segment between 2 points (More is better approximation &amp; less performance). Range(1,1000)</param>
        /// <param name="usePointPositionsToCalcTangents">Use point's positions to calculate tangents</param>
        public BGCurveBaseMath(BGCurve curve, bool traceChanges, int sections = 30, bool usePointPositionsToCalcTangents = false)
        {
            this.curve = curve;
            this.sections = Mathf.Clamp(sections, 1, 1000);
            UsePointPositionToCalcTangent = usePointPositionsToCalcTangents;
            if (traceChanges)
            {
                curve.TraceChanges = true;
                curve.Changed += (sender, args) => Recalculate();
            }
            Recalculate();
        }

        #region Public methods

        /// <summary>
        /// Get point world position between 2 points using linear interpolation. 
        /// It does not guarantee the result segments will be the same length by using even segments for t
        /// </summary>
        /// <param name="t">Ratio between (0,1)</param>
        public virtual Vector3 CalcPositionByT(BGCurvePoint @from, BGCurvePoint to, float t)
        {
            t = Mathf.Clamp01(t);

            var fromPos = from.PositionWorld;
            var toPos = to.PositionWorld;

            Vector3 result;
            if (from.ControlType == BGCurvePoint.ControlTypeEnum.Absent && to.ControlType == BGCurvePoint.ControlTypeEnum.Absent)
            {
                //lerp
                result = fromPos + ((toPos - fromPos)*t);
            }
            else
            {
                var fromPosHandle = from.ControlSecondWorld;
                var toPosHandle = to.ControlFirstWorld;

                // tr= t reverse
                var tr = 1 - t;
                var tr2 = tr*tr;
                var t2 = t*t;

                if (from.ControlType != BGCurvePoint.ControlTypeEnum.Absent && to.ControlType != BGCurvePoint.ControlTypeEnum.Absent)
                {
                    //cubic
                    result = tr*tr2*fromPos + 3*tr2*t*fromPosHandle + 3*tr*t2*toPosHandle + t*t2*toPos;
                }
                else if (from.ControlType == BGCurvePoint.ControlTypeEnum.Absent)
                {
                    //quadrant
                    result = tr2*fromPos + 2*tr*t*toPosHandle + t2*toPos;
                }
                else
                {
                    //quadrant
                    result = tr2*fromPos + 2*tr*t*fromPosHandle + t2*toPos;
                }
            }
            return result;
        }

        /// <summary>
        /// calculates a tangent between 2 points using linear interpolation. 
        /// </summary>
        /// <param name="t">Ratio between (0,1)</param>
        public virtual Vector3 CalcTangentByT(BGCurvePoint @from, BGCurvePoint to, float t)
        {
            if (Curve.PointsCount < 2) return Vector3.zero;

            t = Mathf.Clamp01(t);

            var fromPos = from.PositionWorld;
            var toPos = to.PositionWorld;

            Vector3 result;
            if (from.ControlType == BGCurvePoint.ControlTypeEnum.Absent && to.ControlType == BGCurvePoint.ControlTypeEnum.Absent)
            {
                //lerp
                result = (toPos - fromPos).normalized;
            }
            else
            {
                var fromPosHandle = from.ControlSecondWorld;
                var toPosHandle = to.ControlFirstWorld;

                // tr= t reverse
                var tr = 1 - t;

                if (from.ControlType != BGCurvePoint.ControlTypeEnum.Absent && to.ControlType != BGCurvePoint.ControlTypeEnum.Absent)
                {
                    //cubic derivative
                    result = 3*(tr*tr)*(fromPosHandle - fromPos) + 6*tr*t*(toPosHandle - fromPosHandle) + 3*(t*t)*(toPos - toPosHandle);
                }
                else if (from.ControlType == BGCurvePoint.ControlTypeEnum.Absent)
                {
                    //quadrant derivative
                    result = 2*tr*(toPosHandle - fromPos) + 2*t*(toPos - toPosHandle);
                }
                else
                {
                    //quadrant derivative
                    result = 2*tr*(fromPosHandle - fromPos) + 2*t*(toPos - fromPosHandle);
                }
                result = result.normalized;
            }

            return result;
        }

        /// <summary>
        /// Get approximate curve's point world position using distance ratio. 
        /// </summary>
        /// <param name="distanceRatio">Ratio between (0,1)</param>
        public virtual Vector3 CalcPositionByDistanceRatio(float distanceRatio)
        {
            return CalcPositionByDistance(GetDistance()*Mathf.Clamp01(distanceRatio));
        }

        /// <summary>
        /// Get approximate curve's point world position using distance. 
        /// </summary>
        /// <param name="distance">distance from curve's start between (0, GetDistance())</param>
        public virtual Vector3 CalcPositionByDistance(float distance)
        {
            return BinarySearchByDistance(Mathf.Clamp(distance, 0, GetDistance()));
        }

        /// <summary>
        /// Get approximate curve's tangent using distance ratio. 
        /// </summary>
        /// <param name="distanceRatio">Ratio between (0,1)</param>
        public virtual Vector3 CalcTangentByDistanceRatio(float distanceRatio)
        {
            return CalcTangentByDistance(GetDistance()*Mathf.Clamp01(distanceRatio));
        }

        /// <summary>
        /// Get approximate curve's tangent using distance. 
        /// </summary>
        /// <param name="distance">distance from curve's start between (0, GetDistance())</param>
        public virtual Vector3 CalcTangentByDistance(float distance)
        {
            return BinarySearchByDistance(Mathf.Clamp(distance, 0, GetDistance()), true);
        }

        /// <summary>Get curve's approximate distance</summary>
        public virtual float GetDistance()
        {
            return cachedLength;
        }

        #endregion

        #region Private methods

        //calculates and cache all required data for length calculations for performance reason
        private void Recalculate()
        {
            if (curve.PointsCount < 2)
            {
                cachedLength = 0;
                cachedSectionInfos = new SectionInfo[0];
                return;
            }

            var pointsCount = curve.PointsCount;

            cachedSectionInfos = new SectionInfo[curve.Closed ? pointsCount : pointsCount - 1];
            for (var i = 0; i < pointsCount - 1; i++)
            {
                cachedSectionInfos[i] = CalculateSectionInfo(i == 0 ? null : cachedSectionInfos[i - 1], curve.Points[i], curve.Points[i + 1]);
            }

            if (curve.Closed)
            {
                cachedSectionInfos[cachedSectionInfos.Length - 1] = CalculateSectionInfo(cachedSectionInfos[cachedSectionInfos.Length - 2], curve.Points[pointsCount - 1], curve.Points[0]);
            }

            cachedLength = cachedSectionInfos[cachedSectionInfos.Length - 1].DistanceFromEndToOrigin;
        }

        //calculates one single section data
        private SectionInfo CalculateSectionInfo(SectionInfo prevSection, BGCurvePoint from, BGCurvePoint to)
        {
            var sectionInfo = new SectionInfo
            {
                DistanceFromStartToOrigin = prevSection == null ? 0 : prevSection.DistanceFromEndToOrigin,
                Points = new SectionPointInfo[sections + 1]
            };


            for (var i = 0; i <= sections; i++)
            {
                var t = i/(float) sections;
                var pointInfo = new SectionPointInfo {Position = CalcPositionByT(@from, to, t)};

                if (UsePointPositionToCalcTangent)
                {
                    if (i > 0)
                    {
                        var tangent = (pointInfo.Position - sectionInfo.Points[i - 1].Position).normalized;

                        sectionInfo.Points[i - 1].Tangent = tangent;

                        if (i == 1)
                        {
                            if (prevSection != null)
                            {
                                prevSection.Points[prevSection.Points.Length - 1].Tangent = tangent;
                            }
                        }
                        else if (i == sections)
                        {
                            //we will adjust it later (if there is another section after this one , otherwise- no more data for more precise calculation)
                            pointInfo.Tangent = tangent;
                        }
                    }
                }
                else
                {
                    pointInfo.Tangent = CalcTangentByT(from, to, t);
                }
                if (i != 0)
                {
                    pointInfo.DistanceToSectionStart = sectionInfo.Points[i - 1].DistanceToSectionStart + Vector3.Distance(pointInfo.Position, sectionInfo.Points[i - 1].Position);
                }
                sectionInfo.Points[i] = pointInfo;
            }
            sectionInfo.DistanceFromEndToOrigin = sectionInfo.DistanceFromStartToOrigin + sectionInfo.Points[sections].DistanceToSectionStart;

            return sectionInfo;
        }


        //search cached data and returns point's position at distance from curve's start
        private Vector3 BinarySearchByDistance(float distance, bool searchForTangent = false)
        {
            if (Curve.PointsCount < 2) return Vector3.zero;

            // find a section first
            SectionInfo targetSection = null;
            int low = 0, high = cachedSectionInfos.Length;
            while (low < high)
            {
                var mid = (low + high) >> 1;
                var sectionInfo = cachedSectionInfos[mid];

                if (distance >= sectionInfo.DistanceFromStartToOrigin && distance <= sectionInfo.DistanceFromEndToOrigin)
                {
                    targetSection = sectionInfo;
                    break;
                }

                if (distance < sectionInfo.DistanceFromStartToOrigin)
                {
                    high = mid;
                }
                else
                {
                    low = mid + 1;
                }
            }

            if (targetSection == null)
            {
                //this should never happens
                throw new Exception("Something wrong: can not find a target section");
            }

            //after we found a section, let's search within this section
            var sectionDistance = distance - targetSection.DistanceFromStartToOrigin;
            if (targetSection.Points.Length == 2)
            {
                //linear
                return searchForTangent
                    ? Vector3.Lerp(targetSection.Points[0].Tangent, targetSection.Points[1].Tangent, sectionDistance/targetSection.Distance)
                    : Vector3.Lerp(targetSection.Points[0].Position, targetSection.Points[1].Position, sectionDistance/targetSection.Distance);
            }

            // non linear- let's do another binary search within section
            low = 0;
            high = targetSection.Points.Length;
            var targetIndex = 0;
            while (low < high)
            {
                var mid = (low + high) >> 1;
                var pointInfo = targetSection.Points[mid];

                var less = sectionDistance < pointInfo.DistanceToSectionStart;

                if (!less && (mid == targetSection.Points.Length - 1 || targetSection.Points[mid + 1].DistanceToSectionStart >= sectionDistance))
                {
                    targetIndex = mid;
                    break;
                }

                if (less)
                {
                    high = mid;
                }
                else
                {
                    low = mid + 1;
                }
            }

            var targetPoint = targetSection.Points[targetIndex];

            if (targetIndex == targetSection.Points.Length - 1)
            {
                return searchForTangent ? targetPoint.Tangent : targetPoint.Position;
            }


            var ratio = (sectionDistance - targetPoint.DistanceToSectionStart)/Vector3.Distance(targetPoint.Position, targetSection.Points[targetIndex + 1].Position);
            return searchForTangent
                ? Vector3.Lerp(targetPoint.Tangent, targetSection.Points[targetIndex + 1].Tangent, ratio)
                : Vector3.Lerp(targetPoint.Position, targetSection.Points[targetIndex + 1].Position, ratio);
        }

        #endregion

        #region Private classes

        public class SectionInfo
        {
            //distance from section start to curve start
            public float DistanceFromStartToOrigin;

            //distance from section end to curve start
            public float DistanceFromEndToOrigin;

            //all the points in this section
            private SectionPointInfo[] points = new SectionPointInfo[0];

            public SectionPointInfo[] Points
            {
                get { return points; }
                internal set { points = value; }
            }

            public float Distance
            {
                get { return DistanceFromEndToOrigin - DistanceFromStartToOrigin; }
            }

            public override string ToString()
            {
                return "Section distance=(" + Distance + ")";
            }
        }

        public class SectionPointInfo
        {
            //point's position
            public Vector3 Position;

            //distance from the start of the section
            public float DistanceToSectionStart;

            //point's tangent
            public Vector3 Tangent;

            public override string ToString()
            {
                return "Point at (" + Position + ")";
            }
        }

        #endregion
    }
}