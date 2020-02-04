using System;
using UnityEngine;

namespace BansheeGz.BGSpline.Curve
{
    /// <summary>
    /// Closest point related calculations
    /// </summary>
    //this class contains some intentional copy/paste for the sake of performance
    public class BGCurveCalculatorClosestPoint
    {
        // transitions ("safe" or not)
        private static readonly int[] TransitionsForPartitions;

        // math to use for calculation
        private readonly BGCurveBaseMath math;

        //reusable arrays to reduce GC
        private bool[] excludedSections;
        private float[] minSectionDistances;


        static BGCurveCalculatorClosestPoint()
        {
            // Init transitions data

            //25=inside
            const int nearPlane = (1 << 0) + (1 << 1) + (1 << 2) + (1 << 3) + (1 << 4) + (1 << 5) + (1 << 6) + (1 << 7) + (1 << 24);
            const int farPlane = (1 << 16) + (1 << 17) + (1 << 18) + (1 << 19) + (1 << 20) + (1 << 21) + (1 << 22) + (1 << 23) + (1 << 26);
            const int bottomPlane = (1 << 0) + (1 << 6) + (1 << 7) + (1 << 8) + (1 << 14) + (1 << 15) + (1 << 16) + (1 << 22) + (1 << 23);
            const int topPlane = (1 << 2) + (1 << 3) + (1 << 4) + (1 << 10) + (1 << 11) + (1 << 12) + (1 << 18) + (1 << 19) + (1 << 20);
            const int leftPlane = (1 << 0) + (1 << 1) + (1 << 2) + (1 << 8) + (1 << 9) + (1 << 10) + (1 << 16) + (1 << 17) + (1 << 18);
            const int rightPlane = (1 << 4) + (1 << 5) + (1 << 6) + (1 << 12) + (1 << 13) + (1 << 14) + (1 << 20) + (1 << 21) + (1 << 22);

            //transition from->to which is considered safe in terms of mininum closest point search
            //total 27 partitions
            TransitionsForPartitions = new[]
            {
                //z==0 (near)
                /*0*/ nearPlane | leftPlane | bottomPlane,
                /*1*/ nearPlane | leftPlane,
                /*2*/ nearPlane | leftPlane | topPlane,
                /*3*/ nearPlane | topPlane,
                /*4*/ nearPlane | rightPlane | topPlane,
                /*5*/ nearPlane | rightPlane,
                /*6*/ nearPlane | rightPlane | bottomPlane,
                /*7*/ nearPlane | bottomPlane,

                //z==1 (middle)
                /*8*/ leftPlane | bottomPlane,
                /*9*/ leftPlane,
                /*10*/ leftPlane | topPlane,
                /*11*/ topPlane,
                /*12*/ rightPlane | topPlane,
                /*13*/ rightPlane,
                /*14*/ rightPlane | bottomPlane,
                /*15*/ bottomPlane,

                //z==2 (far)
                /*16*/ farPlane | leftPlane | bottomPlane,
                /*17*/ farPlane | leftPlane,
                /*18*/ farPlane | leftPlane | topPlane,
                /*19*/ farPlane | topPlane,
                /*20*/ farPlane | rightPlane | topPlane,
                /*21*/ farPlane | rightPlane,
                /*22*/ farPlane | rightPlane | bottomPlane,
                /*23*/ farPlane | bottomPlane,

                //3 left partitions in the middle
                /*24*/ nearPlane,
                /*25*/ 0, //inside
                /*26*/ farPlane,
            };
        }

        public BGCurveCalculatorClosestPoint(BGCurveBaseMath math)
        {
            this.math = math;
        }

        //this method contains some intentional copy/paste for the sake of performance
        public Vector3 CalcPositionByClosestPoint(Vector3 targetPoint, out float distance, out Vector3 tangent, bool skipSectionsOptimization = false, bool skipPointsOptimization = false)
        {
/*
            minimalistic thing to do- iterate all lines and use closest point to line formula.

            2 ideas for optimization:
                1) Iterate all sections first, calculate min bounding box and cut off those sections far away from it
                2) Iterate points, and calculate min sphere radius to contain the result point. Then partition the space by the bounding box around this sphere, and cut off
                    all points, that can not be contained in the box or intersect this box with a line;
*/

            var sections = math.SectionInfos;
            var sectionsCount = sections.Count;
            if (sectionsCount == 0)
            {
                distance = 0;
                tangent = Vector3.zero;
                return math.Curve.PointsCount == 1 ? math.Curve[0].PositionWorld : Vector3.zero;
            }


            //==========================================================================================================================
            //                                                                                Section Bbox partitioning (4 passes total)
            //==========================================================================================================================
            var sectionsOptimizationOn = !skipSectionsOptimization && math.Configuration.Parts > 8;
            var minSectionIndex = 0;
            var minSectionMaxDistance = float.MaxValue;
            if (sectionsOptimizationOn)
            {
                //----------------------- Section Bbox Partitioning 1st pass (calculate min AABB (Axis Aligned Bounding Box) by square distance between point and section's AABB)
                Array.Resize(ref excludedSections, sectionsCount);
                Array.Resize(ref minSectionDistances, sectionsCount);

                var minAabbDistance = float.MaxValue;
                for (var i = 0; i < sectionsCount; i++)
                {
                    var sqrDistance = math.GetBoundingBox(i, sections[i]).SqrDistance(targetPoint);
                    excludedSections[i] = false;
                    minSectionDistances[i] = sqrDistance;

                    if (!(minAabbDistance > sqrDistance)) continue;

                    minAabbDistance = sqrDistance;
                    minSectionIndex = i;
                }

                //calc max (between points and their related controls)
                var minSection = sections[minSectionIndex];
                minSectionMaxDistance = MaxDistance(minSection, targetPoint) - BGCurve.Epsilon;

                //----------------------- Section Bbox Partitioning 2nd pass (1) cut off sections, which min distance to point more than max distance to min sections AABB and 2) adjusting min section )
                var sectionsLeft = 0;
                var maxDistanceChanged = false;
                var start = minSectionIndex;
                var end = Mathf.Max(start + 1, sectionsCount - start);
                for (var i = 1; i < end; i++)
                {
                    //the reason why we iterate simulteneously- to decrease chances of MaxDistance expensive calls
                    //to end 
                    var j = start + i;
                    if (j < sectionsCount)
                    {
                        var section = sections[j];

                        if (minSectionDistances[j] > minSectionMaxDistance) excludedSections[j] = true;
                        else
                        {
                            var sectionDistance = MaxDistance(section, targetPoint);
                            if (minSectionMaxDistance > sectionDistance)
                            {
                                minSectionMaxDistance = sectionDistance;
                                maxDistanceChanged = true;
                            }

                            sectionsLeft++;
                        }
                    }

                    //to zero (copy/pasted)
                    j = start - i;
                    if (j >= 0)
                    {
                        var section = sections[j];

                        if (minSectionDistances[j] > minSectionMaxDistance) excludedSections[j] = true;
                        else
                        {
                            var sectionDistance = MaxDistance(section, targetPoint);
                            if (minSectionMaxDistance > sectionDistance)
                            {
                                minSectionMaxDistance = sectionDistance;
                                maxDistanceChanged = true;
                            }

                            sectionsLeft++;
                        }
                    }
                }


                //----------------------- Section Bbox Partitioning 3rd pass (since minSectionMaxDistance may be changed in 2nd pass, we need to iterate sections and filter them again)
                if (maxDistanceChanged && sectionsLeft > 1)
                    for (var i = 0; i < sectionsCount; i++)
                        if (!excludedSections[i] && minSectionDistances[i] > minSectionMaxDistance)
                            excludedSections[i] = true;
            }

            //==========================================================================================================================
            //                                                                                Lines AABB partitioning
            //==========================================================================================================================
            var pointsOptimizationOn = !skipPointsOptimization;
            var closestLineMaxDistance = float.MaxValue;
            var fromDistance = float.MaxValue;
            var fromLess = true;
            var resetFrom = true;
            if (pointsOptimizationOn)
            {
                for (var i = 0; i < sectionsCount; i++)
                {
                    if (sectionsOptimizationOn && excludedSections[i])
                    {
                        resetFrom = true;
                        continue;
                    }

                    var section = sections[i];
                    var points = section.Points;
                    var pointsCount = points.Count;

                    if (resetFrom)
                    {
                        resetFrom = false;
                        fromDistance = Vector3.SqrMagnitude(section[0].Position - targetPoint);
                        fromLess = fromDistance <= closestLineMaxDistance;
                    }

                    for (var j = 1; j < pointsCount; j++)
                    {
                        var to = points[j];

                        var toPos = to.Position;

                        //sqr magnitude inlined
                        var deltaX = toPos.x - targetPoint.x;
                        var deltaY = toPos.y - targetPoint.y;
                        var deltaZ = toPos.z - targetPoint.z;
                        var toDistance = deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ;

                        var toLess = toDistance <= closestLineMaxDistance;

                        if (fromLess && toLess) closestLineMaxDistance = fromDistance > toDistance ? fromDistance : toDistance;

                        fromLess = toLess;
                        fromDistance = toDistance;
                    }
                }

                //======================================================================     Section Bbox partitioning (again)
                //----------------------- Section Bbox Partitioning 4th pass (cause closestLineMaxDistance may be smaller than minSectionMaxDistance we used before)
                if (sectionsOptimizationOn && closestLineMaxDistance < minSectionMaxDistance)
                    for (var i = 0; i < sectionsCount; i++)
                        if (!excludedSections[i] && minSectionDistances[i] > closestLineMaxDistance)
                            excludedSections[i] = true;
            }


            //================================================================================================
            //                                                                                  Final Pass        
            //================================================================================================
            var firstPoint = sections[0][0];
            var fromPoint = firstPoint;
            var result = fromPoint.Position;
            var minDistance = Vector3.SqrMagnitude(targetPoint - fromPoint.Position);
            var sectionIndex = 0;
            var pointIndex = 0;
            var resultLerp = 0f;

            var fromPartition = -1;
            var resetFromPoint = true;


            Vector3 min = Vector3.zero, max = Vector3.zero;
            if (pointsOptimizationOn)
            {
                //bounding Box over sphere (for points optimization)
                var radius = (float) Math.Sqrt(closestLineMaxDistance);
                min = new Vector3(targetPoint.x - radius, targetPoint.y - radius, targetPoint.z - radius);
                max = new Vector3(targetPoint.x + radius, targetPoint.y + radius, targetPoint.z + radius);
            }

            for (var i = 0; i < sectionsCount; i++)
            {
                if (sectionsOptimizationOn && excludedSections[i])
                {
                    resetFromPoint = true;
                    continue;
                }


                var section = sections[i];
                var points = section.Points;

                if (resetFromPoint)
                {
                    resetFromPoint = false;
                    fromPoint = section[0];

                    if (pointsOptimizationOn)
                    {
                        //------------------------------------------ points optimization
                        //xy quadrant clockwise (0-7) 
                        var fromPos = fromPoint.Position;
                        var zSet = false;
                        if (fromPos.x < min.x)
                        {
                            if (fromPos.y < min.y) fromPartition = 0;
                            else if (fromPos.y > max.y) fromPartition = 2;
                            else fromPartition = 1;
                        }
                        else if (fromPos.x > max.x)
                        {
                            if (fromPos.y < min.y) fromPartition = 6;
                            else if (fromPos.y > max.y) fromPartition = 4;
                            else fromPartition = 5;
                        }
                        else
                        {
                            if (fromPos.y < min.y) fromPartition = 7;
                            else if (fromPos.y > max.y) fromPartition = 3;
                            else
                            {
                                if (fromPos.z > max.z) fromPartition = 26;
                                else if (fromPos.z < min.z) fromPartition = 24;
                                else fromPartition = 25;
                                zSet = true;
                            }
                        }

                        //z shift
                        if (!zSet)
                        {
                            if (fromPos.z > max.z) fromPartition = fromPartition | 16;
                            else if (fromPos.z > min.z) fromPartition = fromPartition | 8;
                        }
                    }
                }

                var pointsCount = points.Count;
                for (var j = 1; j < pointsCount; j++)
                {
                    var toPoint = points[j];
                    var toPointPos = toPoint.Position;

                    var excludedByOptimization = false;
                    var toPartition = -1;
                    if (pointsOptimizationOn)
                    {
                        //------------------------------------------ points optimization
                        // copy/pasted
                        var zSet = false;
                        if (toPointPos.x < min.x)
                        {
                            if (toPointPos.y < min.y) toPartition = 0;
                            else if (toPointPos.y > max.y) toPartition = 2;
                            else toPartition = 1;
                        }
                        else if (toPointPos.x > max.x)
                        {
                            if (toPointPos.y < min.y) toPartition = 6;
                            else if (toPointPos.y > max.y) toPartition = 4;
                            else toPartition = 5;
                        }
                        else
                        {
                            if (toPointPos.y < min.y) toPartition = 7;
                            else if (toPointPos.y > max.y) toPartition = 3;
                            else
                            {
                                //center
                                if (toPointPos.z > max.z) toPartition = 26;
                                else if (toPointPos.z < min.z) toPartition = 24;
                                else toPartition = 25; //inside
                                zSet = true;
                            }
                        }

                        //z shift
                        if (!zSet)
                        {
                            if (toPointPos.z > max.z) toPartition = toPartition | 16;
                            else if (toPointPos.z > min.z) toPartition = toPartition | 8;
                        }

                        excludedByOptimization = (TransitionsForPartitions[fromPartition] & (1 << toPartition)) != 0;
                    }


                    if (!excludedByOptimization)
                    {
                        float ratio;
                        var gettingCloser = false;
                        //============================================================================= Get closest point on line formula: Line(from->to), Point(targetPoint)
                        // closest on line
                        double pointX;
                        double pointY;
                        double pointZ;

                        var fromPos = fromPoint.Position;
                        var toPos = toPoint.Position;

                        var fromTargetX = (double) targetPoint.x - (double) fromPos.x;
                        var fromTargetY = (double) targetPoint.y - (double) fromPos.y;
                        var fromTargetZ = (double) targetPoint.z - (double) fromPos.z;

                        var fromToX = (double) toPos.x - (double) fromPos.x;
                        var fromToY = (double) toPos.y - (double) fromPos.y;
                        var fromToZ = (double) toPos.z - (double) fromPos.z;

                        //sqr magnitude inlined
                        var fromToSquaredDistance = (float) (fromToX * fromToX + fromToY * fromToY + fromToZ * fromToZ);


                        if (Math.Abs(fromToSquaredDistance) < BGCurve.Epsilon)
                        {
                            gettingCloser = true;
                            ratio = 1;
                            pointX = toPos.x;
                            pointY = toPos.y;
                            pointZ = toPos.z;
                        }
                        else
                        {
                            // dot inlined
                            var dot = (float) (fromTargetX * fromToX + fromTargetY * fromToY + fromTargetZ * fromToZ);

                            if (dot < 0)
                            {
                                ratio = 0;
                                pointX = fromPos.x;
                                pointY = fromPos.y;
                                pointZ = fromPos.z;
                            }
                            else if (dot > fromToSquaredDistance)
                            {
                                gettingCloser = true;
                                ratio = 1;
                                pointX = toPos.x;
                                pointY = toPos.y;
                                pointZ = toPos.z;
                            }
                            else
                            {
                                var dotRatio = dot / fromToSquaredDistance;
                                ratio = dotRatio;
                                pointX = fromPos.x + fromToX * dotRatio;
                                pointY = fromPos.y + fromToY * dotRatio;
                                pointZ = fromPos.z + fromToZ * dotRatio;
                            }
                        }

                        //------------------------------------------------------ Compare with min distance
                        //sqr magnitude inlined
                        var deltaX = (double) targetPoint.x - (double) pointX;
                        var deltaY = (double) targetPoint.y - (double) pointY;
                        var deltaZ = (double) targetPoint.z - (double) pointZ;
                        var sqrMagnitude = (float) (deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
                        if (sqrMagnitude < minDistance)
                        {
                            minDistance = sqrMagnitude;
                            result.x = (float) pointX;
                            result.y = (float) pointY;
                            result.z = (float) pointZ;
                            sectionIndex = i;

                            if (gettingCloser)
                            {
                                //if we are getting closer-> simply move the pointer to current point
                                if (j == pointsCount - 1 && i < sectionsCount - 1)
                                {
                                    //if it's the last point in the current section-> move pointer to 1st point of the next section (to calc distances correctly)
                                    sectionIndex = i + 1;
                                    pointIndex = 0;
                                }
                                else
                                {
                                    pointIndex = j;
                                }

                                resultLerp = 0;
                            }
                            else
                            {
                                //current closest point somewhere on this line (resultLerp show how far from [j - 1] to [j])
                                pointIndex = j - 1;
                                resultLerp = ratio;
                            }
                        }
                    }

                    fromPoint = toPoint;
                    fromPartition = toPartition;
                }
            }

            //================================================================================================
            //                                                                                  Final result
            //================================================================================================
            var resultSection = sections[sectionIndex];
            if (resultLerp >= 0 && resultLerp <= 1 && pointIndex < resultSection.PointsCount - 1)
            {
                var lerpFrom = resultSection[pointIndex];
                var lerpTo = resultSection[pointIndex + 1];

                tangent = Vector3.Lerp(lerpFrom.Tangent, lerpTo.Tangent, resultLerp);
                distance = sections[sectionIndex].DistanceFromStartToOrigin + Mathf.Lerp(lerpFrom.DistanceToSectionStart, lerpTo.DistanceToSectionStart, resultLerp);
            }
            else
            {
                var resultPoint = sections[sectionIndex][pointIndex];

                tangent = resultPoint.Tangent;
                distance = sections[sectionIndex].DistanceFromStartToOrigin + resultPoint.DistanceToSectionStart;
            }

            return result;
        }

        //max distance between a point and a section
        private static float MaxDistance(BGCurveBaseMath.SectionInfo section, Vector3 position)
        {
//        var fromDistance = Vector3.SqrMagnitude(section.OriginalFrom - position);
            var deltaX = section.OriginalFrom.x - position.x;
            var deltaY = section.OriginalFrom.y - position.y;
            var deltaZ = section.OriginalFrom.z - position.z;
            var fromDistance = deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ;


//        var toDistance = Vector3.SqrMagnitude(section.OriginalTo - position);
            deltaX = section.OriginalTo.x - position.x;
            deltaY = section.OriginalTo.y - position.y;
            deltaZ = section.OriginalTo.z - position.z;
            var toDistance = deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ;


            var fromControlAbsent = section.OriginalFromControlType == BGCurvePoint.ControlTypeEnum.Absent;
            var toControlAbsent = section.OriginalToControlType == BGCurvePoint.ControlTypeEnum.Absent;
            float maxDistance;
            if (fromControlAbsent && toControlAbsent)
            {
                maxDistance = Mathf.Max(fromDistance, toDistance);
            }
            else
            {
                if (fromControlAbsent)
                {
                    //        var toControlDistance = Vector3.SqrMagnitude(section.OriginalToControl - position);
                    deltaX = section.OriginalToControl.x - position.x;
                    deltaY = section.OriginalToControl.y - position.y;
                    deltaZ = section.OriginalToControl.z - position.z;
                    var toControlDistance = deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ;


                    maxDistance = Mathf.Max(Mathf.Max(fromDistance, toDistance), toControlDistance);
                }
                else if (toControlAbsent)
                {
                    //        var fromControlDistance = Vector3.SqrMagnitude(section.OriginalFromControl - position);
                    deltaX = section.OriginalFromControl.x - position.x;
                    deltaY = section.OriginalFromControl.y - position.y;
                    deltaZ = section.OriginalFromControl.z - position.z;
                    var fromControlDistance = deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ;


                    maxDistance = Mathf.Max(Mathf.Max(fromDistance, toDistance), fromControlDistance);
                }
                else
                {
                    //        var fromControlDistance = Vector3.SqrMagnitude(section.OriginalFromControl - position);
                    deltaX = section.OriginalFromControl.x - position.x;
                    deltaY = section.OriginalFromControl.y - position.y;
                    deltaZ = section.OriginalFromControl.z - position.z;
                    var fromControlDistance = deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ;

                    //        var toControlDistance = Vector3.SqrMagnitude(section.OriginalToControl - position);
                    deltaX = section.OriginalToControl.x - position.x;
                    deltaY = section.OriginalToControl.y - position.y;
                    deltaZ = section.OriginalToControl.z - position.z;
                    var toControlDistance = deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ;

                    maxDistance = Mathf.Max(Mathf.Max(Mathf.Max(fromDistance, toDistance), fromControlDistance), toControlDistance);
                }
            }

            return maxDistance;
        }
    }
}