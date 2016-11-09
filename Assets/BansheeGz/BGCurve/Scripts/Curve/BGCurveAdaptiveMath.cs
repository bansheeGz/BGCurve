using System;
using UnityEngine;

namespace BansheeGz.BGSpline.Curve
{
    /// <summary> 
    /// Adaptive curve subdivision math. It split the spline into uneven parts, based on the curvature.
    /// Strictly speaking, it still uses uniform splitting, but it can omit and add points when needed, resulting in better approximation and less points.
    /// </summary>
    public class BGCurveAdaptiveMath : BGCurveBaseMath
    {
        //------------------------------------------------------------------------
        //min tolerance value
        public const float MinTolerance = 0.1f;
        //max tolerance value
        public const float MaxTolerance = 0.9999750f;
        //distance tolerance (to avoid spamming points at one single location)
        public const float DistanceTolerance = 0.01f;
        //max recursion level
        private const int RecursionLimit = 24;

        //force section to recalculate
        private bool ignoreSectionChangedCheckOverride;

        //normalized tolerance
        private float toleranceRatio;
        //normalized tolerance squared
        private float toleranceRatioSquared;

        //tolerance
        private float tolerance;

        /// <summary>Create new math with config options </summary>
        public BGCurveAdaptiveMath(BGCurve curve, ConfigAdaptive config)
            : base(curve, config)
        {
        }

        /// <summary>Init math with new config and recalculate data </summary>
        public override void Init(Config config)
        {
            var configAdaptive = (ConfigAdaptive) config;

            tolerance = Mathf.Clamp(configAdaptive.Tolerance, MinTolerance, MaxTolerance);

            //pow 4
            tolerance *= tolerance;
            tolerance *= tolerance;

            //normalized 
            toleranceRatio = 1/(1 - tolerance);
            toleranceRatioSquared = toleranceRatio*toleranceRatio;

            //if old tolerance is different- we force section recalculation
            ignoreSectionChangedCheckOverride = this.config == null || Math.Abs(((ConfigAdaptive) this.config).Tolerance - tolerance) > BGCurve.Epsilon;

            //recalculate
            base.Init(config);
        }

        //Reset pooled section with new data and recalculates if needed
        protected override bool Reset(SectionInfo section, BGCurvePointI @from, BGCurvePointI to, int pointsCount)
        {
            return section.Reset(@from, to, section.PointsCount, ignoreSectionChangedCheck || ignoreSectionChangedCheckOverride);
        }

        protected override bool IsUseDistanceToAdjustTangents(SectionInfo section, SectionInfo prevSection)
        {
            return true;
        }

        //approximate split section
        //this method contains some intentional copy/paste
        protected override void CalculateSplitSection(SectionInfo section, BGCurvePointI @from, BGCurvePointI to)
        {
            //if set to true, tangent formula will be used to calc tangents
            var calcTangents = cacheTangent && !config.UsePointPositionsToCalcTangents;


            var points = section.points;
            var count = points.Count;
            //move all existing points to the pool (the number of points depends on the curvature)
            for (var i = 0; i < count; i++) poolPointInfos.Add(points[i]);
            points.Clear();

            //original points and control types
            var p0 = section.OriginalFrom;
            var p1 = section.OriginalFromControl;
            var p2 = section.OriginalToControl;
            var p3 = section.OriginalTo;

            //type of the curve (0-absent,3-both Bezier, 1 or 2 - one of the points has Bezier controls)
            var type = (section.OriginalFromControlType != BGCurvePoint.ControlTypeEnum.Absent ? 2 : 0) + (section.OriginalToControlType != BGCurvePoint.ControlTypeEnum.Absent ? 1 : 0);

            //=====================================   first point
            SectionPointInfo firstPoint;
            var poolCursor = poolPointInfos.Count - 1;
            if (poolCursor >= 0)
            {
                firstPoint = poolPointInfos[poolCursor];
                poolPointInfos.RemoveAt(poolCursor);
            }
            else firstPoint = new SectionPointInfo();
            firstPoint.Position = p0;
            firstPoint.DistanceToSectionStart = 0;
            points.Add(firstPoint);

            //====================================   split recursively
            switch (type)
            {
                case 3:
                    RecursiveCubicSplit(section, p0.x, p0.y, p0.z, p1.x, p1.y, p1.z, p2.x, p2.y, p2.z, p3.x, p3.y, p3.z, 0, calcTangents, 0, 1);
                    break;
                case 2:
                    RecursiveQuadraticSplit(section, p0.x, p0.y, p0.z, p1.x, p1.y, p1.z, p3.x, p3.y, p3.z, 0, false, calcTangents, 0, 1);
                    break;
                case 1:
                    RecursiveQuadraticSplit(section, p0.x, p0.y, p0.z, p2.x, p2.y, p2.z, p3.x, p3.y, p3.z, 0, true, calcTangents, 0, 1);
                    break;
            }

            //=====================================   last point
            SectionPointInfo lastPoint;
            poolCursor = poolPointInfos.Count - 1;
            if (poolCursor >= 0)
            {
                lastPoint = poolPointInfos[poolCursor];
                poolPointInfos.RemoveAt(poolCursor);
            }
            else lastPoint = new SectionPointInfo();
            lastPoint.Position = p3;
            points.Add(lastPoint);


            //calculate distances (and optionally tangents)
            //if set to true, points positions will be used to calculate tangents
            calcTangents = cacheTangent && config.UsePointPositionsToCalcTangents;
            var prevPoint = points[0];
            for (var i = 1; i < points.Count; i++)
            {
                var point = points[i];

                var pos = point.Position;
                var prevPos = prevPoint.Position;

                var dX = (double) pos.x - prevPos.x;
                var dY = (double) pos.y - prevPos.y;
                var dZ = (double) pos.z - prevPos.z;

                //distance
                point.DistanceToSectionStart = prevPoint.DistanceToSectionStart + ((float) Math.Sqrt(dX*dX + dY*dY + dZ*dZ));

                //tangents
                if (calcTangents) point.Tangent = Vector3.Normalize(pos - prevPos);

                prevPoint = point;
            }

            //set first and last points tangents (cause they was omitted during recursive split)
            if (cacheTangent)
            {
                if (config.UsePointPositionsToCalcTangents)
                {
                    firstPoint.Tangent = (points[1].Position - firstPoint.Position).normalized;
                    lastPoint.Tangent = points[points.Count - 2].Tangent;
                }
                else
                {
                    switch (type)
                    {
                        case 0:
                            firstPoint.Tangent = lastPoint.Tangent = (lastPoint.Position - firstPoint.Position).normalized;
                            break;
                        case 1:
                            firstPoint.Tangent = Vector3.Normalize(section.OriginalToControl - section.OriginalFrom);
                            lastPoint.Tangent = Vector3.Normalize(section.OriginalTo - section.OriginalToControl);
                            break;
                        case 2:
                            firstPoint.Tangent = Vector3.Normalize(section.OriginalFromControl - section.OriginalFrom);
                            lastPoint.Tangent = Vector3.Normalize(section.OriginalTo - section.OriginalFromControl);
                            break;
                        case 3:
                            firstPoint.Tangent = Vector3.Normalize(section.OriginalFromControl - section.OriginalFrom);
                            lastPoint.Tangent = Vector3.Normalize(section.OriginalTo - section.OriginalToControl);
                            break;
                    }
                }
            }
        }


        //------------------------------------------------------------------------ Quadratic
        //this method contains some intentional copy/paste
        private void RecursiveQuadraticSplit(SectionInfo section, double x0, double y0, double z0, double x1, double y1, double z1, double x2, double y2, double z2, int level, bool useSecond,
            bool calcTangents, double fromT, double toT)
        {
            if (level > RecursionLimit) return;

            // is curve flat
            // http://www.malinc.se/m/DeCasteljauAndBezier.php
            // sqr(b) + sqr(c) < sqr(a) * tolerance
            var dx01 = x0 - x1;
            var dy01 = y0 - y1;
            var dz01 = z0 - z1;

            var dx12 = x1 - x2;
            var dy12 = y1 - y2;
            var dz12 = z1 - z2;

            var dx02 = x0 - x2;
            var dy02 = y0 - y2;
            var dz02 = z0 - z2;

            var a = dx02*dx02 + dy02*dy02 + dz02*dz02;
            var b = dx01*dx01 + dy01*dy01 + dz01*dz01;
            var c = dx12*dx12 + dy12*dy12 + dz12*dz12;

            var temp = a*toleranceRatioSquared - b - c;

            if (4*b*c < temp*temp || a + b + c < DistanceTolerance) return;

            //at this point we know, the curve is not flat
            // split (Casteljau algorithm)
            var x01 = (x0 + x1)*.5;
            var y01 = (y0 + y1)*.5;
            var z01 = (z0 + z1)*.5;

            var x12 = (x1 + x2)*.5;
            var y12 = (y1 + y2)*.5;
            var z12 = (z1 + z2)*.5;

            var x012 = (x01 + x12)*.5;
            var y012 = (y01 + y12)*.5;
            var z012 = (z01 + z12)*.5;


            var t = calcTangents ? (fromT + toT)*.5 : .0;

            var pos = new Vector3((float) x012, (float) y012, (float) z012);

            //apply snapping 
            if (curve.SnapType == BGCurve.SnapTypeEnum.Curve) curve.ApplySnapping(ref pos);

            //split first section
            RecursiveQuadraticSplit(section, x0, y0, z0, x01, y01, z01, x012, y012, z012, level + 1, useSecond, calcTangents, fromT, t);

            //add point
            SectionPointInfo pointInfo;
            var poolCursor = poolPointInfos.Count - 1;
            if (poolCursor >= 0)
            {
                pointInfo = poolPointInfos[poolCursor];
                poolPointInfos.RemoveAt(poolCursor);
            }
            else pointInfo = new SectionPointInfo();
            pointInfo.Position = pos;
            section.points.Add(pointInfo);

            //tangents
            if (calcTangents)
            {
                var control = useSecond ? section.OriginalToControl : section.OriginalFromControl;
                //idea.. optimize it
                pointInfo.Tangent = Vector3.Normalize(2*(1 - (float) t)*(control - section.OriginalFrom) + 2*(float) t*(section.OriginalTo - control));
            }

            //split second section
            RecursiveQuadraticSplit(section, x012, y012, z012, x12, y12, z12, x2, y2, z2, level + 1, useSecond, calcTangents, t, toT);
        }


        //------------------------------------------------------------------------ Cubic
        //this method contains some intentional copy/paste
        private void RecursiveCubicSplit(SectionInfo section, double x0, double y0, double z0, double x1, double y1, double z1, double x2, double y2, double z2, double x3, double y3, double z3,
            int level, bool calcTangents, double fromT, double toT)
        {
            if (level > RecursionLimit) return;

            // is curve flat
            // http://www.malinc.se/m/DeCasteljauAndBezier.php
            // sqr(b) + sqr(c) + sqr(d) < sqr(a) * tolerance

            var dx01 = x0 - x1;
            var dy01 = y0 - y1;
            var dz01 = z0 - z1;

            var dx12 = x1 - x2;
            var dy12 = y1 - y2;
            var dz12 = z1 - z2;

            var dx23 = x2 - x3;
            var dy23 = y2 - y3;
            var dz23 = z2 - z3;

            var dx03 = x0 - x3;
            var dy03 = y0 - y3;
            var dz03 = z0 - z3;

            var a = dx03*dx03 + dy03*dy03 + dz03*dz03;
            var b = dx01*dx01 + dy01*dy01 + dz01*dz01;
            var c = dx12*dx12 + dy12*dy12 + dz12*dz12;
            var d = dx23*dx23 + dy23*dy23 + dz23*dz23;

            //idea.. optimize sqrt. Sqrt is expensive call, and 3 Sqrt calls is super expensive to say the least
            if (Math.Sqrt(b*c) + Math.Sqrt(c*d) + Math.Sqrt(b*d) < (a*toleranceRatioSquared - b - c - d)*.5 || a + b + c + d < DistanceTolerance) return;

            //at this point we know, the curve is not flat
            //split (Casteljau algorithm)
            var x01 = (x0 + x1)*.5;
            var y01 = (y0 + y1)*.5;
            var z01 = (z0 + z1)*.5;
            var x12 = (x1 + x2)*.5;
            var y12 = (y1 + y2)*.5;
            var z12 = (z1 + z2)*.5;
            var x23 = (x2 + x3)*.5;
            var y23 = (y2 + y3)*.5;
            var z23 = (z2 + z3)*.5;

            var x012 = (x01 + x12)*.5;
            var y012 = (y01 + y12)*.5;
            var z012 = (z01 + z12)*.5;
            var x123 = (x12 + x23)*.5;
            var y123 = (y12 + y23)*.5;
            var z123 = (z12 + z23)*.5;

            var x0123 = (x012 + x123)*.5;
            var y0123 = (y012 + y123)*.5;
            var z0123 = (z012 + z123)*.5;

            var t = calcTangents ? (fromT + toT)*.5 : .0;

            var pos = new Vector3((float) x0123, (float) y0123, (float) z0123);

            if (curve.SnapType == BGCurve.SnapTypeEnum.Curve) curve.ApplySnapping(ref pos);

            //split first section
            RecursiveCubicSplit(section, x0, y0, z0, x01, y01, z01, x012, y012, z012, x0123, y0123, z0123, level + 1, calcTangents, fromT, t);

            //add point
            SectionPointInfo pointInfo;
            var poolCursor = poolPointInfos.Count - 1;
            if (poolCursor >= 0)
            {
                pointInfo = poolPointInfos[poolCursor];
                poolPointInfos.RemoveAt(poolCursor);
            }
            else pointInfo = new SectionPointInfo();
            pointInfo.Position = pos;
            section.points.Add(pointInfo);

            //tangent
            if (calcTangents)
            {
                var tr = 1 - t;
                //idea.. optimize it
                pointInfo.Tangent = Vector3.Normalize(3*((float) (tr*tr))*(section.OriginalFromControl - section.OriginalFrom) +
                                                      6*(float) (tr*t)*(section.OriginalToControl - section.OriginalFromControl) +
                                                      3*(float) (t*t)*(section.OriginalTo - section.OriginalToControl));
            }


            //split second section
            RecursiveCubicSplit(section, x0123, y0123, z0123, x123, y123, z123, x23, y23, z23, x3, y3, z3, level + 1, calcTangents, t, toT);
        }

        /// <summary>Configuration options for Adaptive Math </summary>
        public class ConfigAdaptive : Config
        {
            /// <summary>
            /// This parameter is used to calculate if curve's flat and no more splitting is required
            /// Note: The final tolerance parameter, used by Math, is based on this value, but differs
            /// </summary>
            public float Tolerance = .2f;

            /// <summary>Construct new config</summary>
            public ConfigAdaptive(Fields fields) : base(fields)
            {
            }
        }

        public override string ToString()
        {
            return "Adaptive Math for curve (" + Curve + "), sections=" + SectionsCount;
        }
    }

    /*
            Roger Willcocks flat criteria for quadratic (something wrong here)
            var ax = 2*x1 - x2 - x0;
            var ay = 2*y1 - y2 - y0;
            var az = 2*z1 - z2 - z0;
            ax *= ax;
            ay *= ay;
            az *= az;

            if (ax + ay + az < tolerance) return;
*/

    /*
            Roger Willcocks flat criteria for cubic (does not work well at all)- maybe a bbug or something- have no idea
            var ax = 3*x1 - 2*x0 - x3;
            var ay = 3*y1 - 2*y0 - y3;
            var az = 3*z1 - 2*z0 - z3;
            ax *= ax;
            ay *= ay;
            az *= az;

            var bx = 3*x2 - x0 - 2*x3;
            var by = 3*y2 - y0 - 2*y3;
            var bz = 3*z2 - z0 - 2*z3;
            bx *= bx;
            by *= by;
            bz *= bz;

            if (Math.Max(ax, bx) + Math.Max(ay, by) + Math.Max(az, bz) < tolerance) return;
*/
/*
                    // For Willckock's criteria: 16 and exponenta originate from the flatness calculation
                    tolerance *= tolerance*16;
        */
}