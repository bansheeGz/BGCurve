using System;
using System.Collections.Generic;
using BansheeGz.BGSpline.Curve;
using UnityEngine;

namespace BansheeGz.BGSpline.Components
{
    /// <summary>Spline to polyline splitter </summary>
    public class BGPolylineSplitter
    {
        //different providers for different modes
        private PositionsProvider positionsProvider;
        //reusable array, storing if section is straight. 
        private bool[] straightBits;

        //to remove GC we should keep all providers
        private PositionsProviderMath providerMath;
        private PositionsProviderPartsPerSection providerPartsPerSection;
        private PositionsProviderTotalParts providerTotalParts;

        public void Bind(List<Vector3> positions, BGCcMath math, Config config)
        {
            positions.Clear();

            var baseMath = math.Math;
            var sectionsCount = baseMath.SectionsCount;

            //count number of straight lines
            var straightLinesCount = 0;
            if (!config.DoNotOptimizeStraightLines)
            {
                //resize only if length < sectionsCount to reduce GC
                if (straightBits == null || straightBits.Length < sectionsCount) Array.Resize(ref straightBits, sectionsCount);
                straightLinesCount = CountStraightLines(baseMath, straightBits);
            }


            //recalculate points
            InitProvider(ref positionsProvider, math, config).Build(positions, straightLinesCount, straightBits);

            if (!config.UseLocal) return;

            //slow convertion (world->local)
            var matrix = config.Transform.worldToLocalMatrix;
            var count = positions.Count;
            for (var i = 0; i < count; i++) positions[i] = matrix.MultiplyPoint(positions[i]);
        }

        //count the number of straight lines
        public static int CountStraightLines(BGCurveBaseMath math, bool[] straight)
        {
            var curve = math.Curve;
            var points = curve.Points;
            if (points.Length == 0) return 0;

            var sections = math.SectionInfos;
            var sectionsCount = sections.Count;
            var fillArray = straight != null;

            var straightLinesCount = 0;
            var previousControlAbsent = points[0].ControlType == BGCurvePoint.ControlTypeEnum.Absent;
            for (var i = 0; i < sectionsCount; i++)
            {
                var nextPoint = curve.Closed && i == sectionsCount - 1 ? points[0] : points[i + 1];
                var nextControlAbsent = nextPoint.ControlType == BGCurvePoint.ControlTypeEnum.Absent;

                if (previousControlAbsent && nextControlAbsent)
                {
                    if (fillArray) straight[i] = true;
                    straightLinesCount++;
                }
                else if (fillArray) straight[i] = false;

                previousControlAbsent = nextControlAbsent;
            }
            return straightLinesCount;
        }

        //===============================================================================================
        //                                                    private methods
        //===============================================================================================
        //init required provider. Each mode has it's own provider
        private PositionsProvider InitProvider(ref PositionsProvider positionsProvider, BGCcMath math, Config config)
        {
            //assign positions provider if needed
            var mode = config.SplitMode;
            var providerObsolete = positionsProvider == null || !positionsProvider.Comply(mode);
            switch (mode)
            {
                case BGCcSplitterPolyline.SplitModeEnum.PartsTotal:
                    if (providerObsolete)
                    {
                        if (providerTotalParts == null) providerTotalParts = new PositionsProviderTotalParts();
                        positionsProvider = providerTotalParts;
                    }
                    providerTotalParts.Init(math, config.PartsTotal);
                    break;
                case BGCcSplitterPolyline.SplitModeEnum.PartsPerSection:
                    if (providerObsolete)
                    {
                        if (providerPartsPerSection == null) providerPartsPerSection = new PositionsProviderPartsPerSection();
                        positionsProvider = providerPartsPerSection;
                    }
                    providerPartsPerSection.Init(math, config.PartsPerSection);
                    break;
                default:
                    //                    case SplitModeEnum.UseMathData:
                    if (providerObsolete)
                    {
                        if (providerMath == null) providerMath = new PositionsProviderMath();
                        positionsProvider = providerMath;
                    }
                    providerMath.Init(math);
                    break;
            }

            if (config.DistanceMin >= 0 || config.DistanceMax >= 0)
            {
                //currently DistanceMin and DistanceMax supported by SplitModeEnum.UseMathData only
                if (mode != BGCcSplitterPolyline.SplitModeEnum.UseMathData) throw new Exception("DistanceMin and DistanceMax supported by SplitModeEnum.UseMathData mode only");

                positionsProvider.DistanceMin = config.DistanceMin;
                positionsProvider.DistanceMax = config.DistanceMax;
            }
            return positionsProvider;
        }

        //===============================================================================================
        //                                                    Helper classes
        //===============================================================================================
        /// <summary>all possible splitter parameters. limits field (DistanceMin & DistanceMax) currently supported by useMathData mode only!</summary>
        public class Config
        {
            public bool DoNotOptimizeStraightLines;
            public BGCcSplitterPolyline.SplitModeEnum SplitMode;
            public int PartsTotal;
            public int PartsPerSection;

            //local
            public bool UseLocal;
            public Transform Transform;

            //limits (currently supported by useMathData mode only!)
            public float DistanceMin;
            public float DistanceMax;
        }

        //============================================= Abstract provider
        /// <summary>Abstract provider</summary>
        public abstract class PositionsProvider
        {
            //used math
            protected BGCcMath Math;

            // currently supported by UseMathData mode only
            private float distanceMin = -1;
            private float distanceMax = -1;
            protected bool LastPointAdded;

            protected bool DistanceMinConstrained;
            protected bool DistanceMaxConstrained;

            public float DistanceMin
            {
                get { return distanceMin; }
                set
                {
                    distanceMin = value;
                    DistanceMinConstrained = value > 0;
                }
            }

            public float DistanceMax
            {
                get { return distanceMax; }
                set
                {
                    distanceMax = value;
                    DistanceMaxConstrained = value > 0;
                }
            }


            public virtual void Init(BGCcMath math)
            {
                Math = math;
                LastPointAdded = false;
            }

            /// <summary>if this provider can build points for the given mode </summary>
            public abstract bool Comply(BGCcSplitterPolyline.SplitModeEnum splitMode);

            /// <summary>calculate points fro polyline</summary>
            public virtual void Build(List<Vector3> positions, int straightLinesCount, bool[] straightBits)
            {
                var math = Math.Math;

                var sections = math.SectionInfos;
                var sectionsCount = sections.Count;

                //first point always present
                if (!DistanceMinConstrained) positions.Add(math[0][0].Position);

                //fill in points
                for (var i = 0; i < sectionsCount; i++)
                {
                    var section = sections[i];
                    if (DistanceMinConstrained && section.DistanceFromEndToOrigin < DistanceMin) continue;
                    if (DistanceMaxConstrained && section.DistanceFromStartToOrigin > DistanceMax) continue;

                    if (straightLinesCount != 0 && straightBits[i])
                    {
                        // straight line
                        var lastPoint = section[section.PointsCount - 1];
                        var prevPoint = section[section.PointsCount - 2];

                        if (DistanceMinConstrained && positions.Count == 0) AddFirstPointIfNeeded(positions, section, lastPoint, prevPoint);
                        if (DistanceMaxConstrained && !LastPointAdded && lastPoint.DistanceToSectionStart + section.DistanceFromStartToOrigin > DistanceMax
                            && AddLastPointIfNeeded(positions, section, lastPoint, prevPoint)) break;

                        positions.Add(lastPoint.Position);
                    }
                    // curved line
                    else FillInSplitSection(section, positions);
                }
            }

            //if distanceMin is used, we need to add additional point
            protected void AddFirstPointIfNeeded(List<Vector3> positions, BGCurveBaseMath.SectionInfo section,
                BGCurveBaseMath.SectionPointInfo firstPointInRange, BGCurveBaseMath.SectionPointInfo previousPoint)
            {
                var firstPointDistance = firstPointInRange.DistanceToSectionStart + section.DistanceFromStartToOrigin;
                var distanceToAdd = firstPointDistance - DistanceMin;
                if (!(distanceToAdd > BGCurve.Epsilon)) return;

                var ratio = 1 - distanceToAdd/(firstPointInRange.DistanceToSectionStart - previousPoint.DistanceToSectionStart);
                positions.Add(Vector3.Lerp(previousPoint.Position, firstPointInRange.Position, ratio));
            }

            //if distanceMin is used, we need to add additional point
            protected bool AddLastPointIfNeeded(List<Vector3> positions, BGCurveBaseMath.SectionInfo section,
                BGCurveBaseMath.SectionPointInfo currentPoint, BGCurveBaseMath.SectionPointInfo previousPoint)
            {
                var currentPointDistance = currentPoint.DistanceToSectionStart + section.DistanceFromStartToOrigin;
                if (!(currentPointDistance > DistanceMax)) return false;

                //there are 2 options: 1) prev point is in the list 2) prevpoint=previousPoint
                //we can determine it by sqrMagnitude
                var lastPointInTheListMagnitude = Vector3.SqrMagnitude(positions[positions.Count - 1] - currentPoint.Position);
                var prevPointMagnitude = Vector3.SqrMagnitude(previousPoint.Position - currentPoint.Position);
                if (lastPointInTheListMagnitude > prevPointMagnitude)
                {
                    var distanceToAdd = currentPointDistance - DistanceMax;
                    LastPointAdded = true;
                    var ratio = distanceToAdd / (currentPoint.DistanceToSectionStart - previousPoint.DistanceToSectionStart);
                    positions.Add(Vector3.Lerp(previousPoint.Position, currentPoint.Position, ratio));
                }
                else
                {
                    var distanceToLastPoint = Mathf.Sqrt(lastPointInTheListMagnitude);
                    var distanceToMaxPoint = currentPointDistance - DistanceMax;
                    var ratio = 1 - distanceToMaxPoint / distanceToLastPoint;
                    positions.Add(Vector3.Lerp(positions[positions.Count - 1], currentPoint.Position, ratio));
                }
                return true;
            }

            // add points for a section by a given number of points
            protected static void FillIn(BGCurveBaseMath.SectionInfo section, List<Vector3> result, int parts)
            {
                var onePartDistance = section.Distance/parts;
                for (var j = 1; j <= parts; j++)
                {
                    Vector3 tangent;
                    Vector3 pos;
                    section.CalcByDistance(onePartDistance*j, out pos, out tangent, true, false);
                    result.Add(pos);
                }
            }

            // add points for a split section 
            protected abstract void FillInSplitSection(BGCurveBaseMath.SectionInfo section, List<Vector3> result);
        }

        //============================================= Provider for total parts mode
        /// <summary>Provider for total parts mode</summary>
        public sealed class PositionsProviderTotalParts : PositionsProvider
        {
            private int parts;
            private int reminderForCurved;
            private int partsPerSectionFloor;


            public void Init(BGCcMath math, int parts)
            {
                base.Init(math);

                this.parts = parts;
            }

            public override bool Comply(BGCcSplitterPolyline.SplitModeEnum splitMode)
            {
                return splitMode == BGCcSplitterPolyline.SplitModeEnum.PartsTotal;
            }

            public override void Build(List<Vector3> positions, int straightLinesCount, bool[] straightBits)
            {
                var curve = Math.Curve;
                var sections = Math.Math.SectionInfos;
                var sectionsCount = sections.Count;

                //at least one section is curved (maxParts>=sectionsCount, so floatParts >=1)
                var floatParts = (parts - straightLinesCount)/(float) (sectionsCount - straightLinesCount);

                reminderForCurved = (int) ((parts - straightLinesCount)%(float) (sectionsCount - straightLinesCount));
                partsPerSectionFloor = Mathf.FloorToInt(floatParts);


                if (parts < sectionsCount)
                {
                    if (parts == 1)
                    {
                        //only one part per whole curve (one->last)
                        positions.Add(sections[0][0].Position);
                        positions.Add(curve.Closed ? Math.CalcByDistanceRatio(BGCurveBaseMath.Field.Position, .5f) : sections[sectionsCount - 1][sections[sectionsCount - 1].PointsCount - 1].Position);
                    }
                    else if (parts == 2 && curve.Closed)
                    {
                        positions.Add(sections[0][0].Position);
                        positions.Add(Math.CalcByDistanceRatio(BGCurveBaseMath.Field.Position, .3333f));
                        positions.Add(Math.CalcByDistanceRatio(BGCurveBaseMath.Field.Position, .6667f));
                    }
                    else for (var i = 0; i <= parts; i++) positions.Add(Math.CalcByDistanceRatio(BGCurveBaseMath.Field.Position, i/(float) parts));
                }
                else base.Build(positions, straightLinesCount, straightBits);
            }

            // add points for a split section 
            protected override void FillInSplitSection(BGCurveBaseMath.SectionInfo section, List<Vector3> result)
            {
                //curved
                var partsForSection = partsPerSectionFloor;
                if (reminderForCurved > 0)
                {
                    partsForSection++;
                    reminderForCurved--;
                }

                FillIn(section, result, partsForSection);
            }
        }

        //============================================= Provider for parts per section mode

        /// <summary>Provider for parts per section mode</summary>
        public sealed class PositionsProviderPartsPerSection : PositionsProvider
        {
            private int parts;

            public void Init(BGCcMath math, int partsPerSection)
            {
                base.Init(math);
                parts = partsPerSection;
            }

            public override bool Comply(BGCcSplitterPolyline.SplitModeEnum splitMode)
            {
                return splitMode == BGCcSplitterPolyline.SplitModeEnum.PartsPerSection;
            }

            // add points for a split section 
            protected override void FillInSplitSection(BGCurveBaseMath.SectionInfo section, List<Vector3> result)
            {
                FillIn(section, result, parts);
            }
        }

        //============================================= Provider for useMath mode

        /// <summary>Provider for useMath mode</summary>
        public sealed class PositionsProviderMath : PositionsProvider
        {
            public override bool Comply(BGCcSplitterPolyline.SplitModeEnum splitMode)
            {
                return splitMode == BGCcSplitterPolyline.SplitModeEnum.UseMathData;
            }

            // add points for a split section 
            protected override void FillInSplitSection(BGCurveBaseMath.SectionInfo section, List<Vector3> result)
            {
                if (LastPointAdded) return;

                var sectionPoints = section.Points;
                var count = sectionPoints.Count;

                for (var j = 1; j < count; j++)
                {
                    var point = sectionPoints[j];

                    if (DistanceMinConstrained && result.Count == 0) AddFirstPointIfNeeded(result, section, point, sectionPoints[j - 1]);

                    if (DistanceMaxConstrained && AddLastPointIfNeeded(result, section, point, sectionPoints[j - 1])) break;

                    result.Add(point.Position);
                }
            }
        }
    }
}