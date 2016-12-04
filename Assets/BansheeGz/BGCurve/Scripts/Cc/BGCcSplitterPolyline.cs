using System;
using System.Collections.Generic;
using UnityEngine;
using BansheeGz.BGSpline.Curve;

namespace BansheeGz.BGSpline.Components
{
    /// <summary>
    /// Splits the curve to single line. This class does not change anything except providing point's positions.
    /// Subclasses, like BGCcVisualizationLineRenderer or BGCcTriangulate2D uses this data for their own purposes.
    /// </summary>
    [HelpURL("http://www.bansheegz.com/BGCurve/Cc/BGCcSplitterPolyline")]
    [
        CcDescriptor(
            Description = "Calculates points positions for polyline along the curve. It does not change or modify anything. Use Positions field to access points.",
            Name = "Splitter Polyline",
            Image = "Assets/BansheeGz/BGCurve/Icons/Components/BGCcSplitterPolyline123.png")
    ]
    [AddComponentMenu("BansheeGz/BGCurve/Components/BGCcSplitterPolyline")]
    public class BGCcSplitterPolyline : BGCcWithMath
    {
        //===============================================================================================
        //                                                    Events
        //===============================================================================================
        /// <summary>ui updated</summary>
        public event EventHandler ChangedPositions;


        //===============================================================================================
        //                                                    Enums
        //===============================================================================================
        /// <summary>Split mode </summary>
        public enum SplitModeEnum
        {
            /// <summary>Math precalculated data is reused</summary>
            UseMathData = 0,

            /// <summary>Use specified number of parts for whole spline</summary>
            PartsTotal = 1,

            /// <summary>Use specified number of parts for each spline's section</summary>
            PartsPerSection = 2
        }

        //===============================================================================================
        //                                                    Fields (Persistent)
        //===============================================================================================
        [SerializeField] [Tooltip("How to split the curve. " +
                                  "TotalSections -total sections for whole curve;\r\n " +
                                  "PartSections - each part (between 2 points) will use the same amount of splits;\r\n" +
                                  "UseMathData -use data, precalculated by Math component. " +
                                  "Note, you can tweak some params at Math as well.")] private SplitModeEnum splitMode = SplitModeEnum.UseMathData;

        [SerializeField] [Range(1, 1000)] [Tooltip("Total number of parts to split a curve to. " +
                                                   "The actual number of parts can be less than partsTotal due to optimization, but never more.")] private int partsTotal = 30;


        [SerializeField] [Range(1, 150)] [Tooltip("Every section of the curve will be split on even parts. " +
                                                  "The actual number of parts can be less than partsPerSection due to optimization, but never more.")] private int partsPerSection = 30;

        [SerializeField] [Tooltip("Split straight lines. Straight lines are optimized by default and are not split.")] private bool doNotOptimizeStraightLines;

        [SerializeField] [Tooltip("By default positions in world coordinates. Set this parameter to true to use local coordinates. " +
                                  "Local coordinates are calculated slower.")] protected bool useLocal;


        /// <summary>Split mode </summary>
        public SplitModeEnum SplitMode
        {
            get { return splitMode; }
            set { ParamChanged(ref splitMode, value); }
        }

        /// <summary>Number of parts for PartsTotal splitMode</summary>
        public int PartsTotal
        {
            get { return Mathf.Clamp(partsTotal, 1, 1000); }
            //idea.. we may skip updateui if splitmode is not PartsTotal
            set { ParamChanged(ref partsTotal, Mathf.Clamp(value, 1, 1000)); }
        }

        /// <summary>Number of parts for PartsPerSection splitMode</summary>
        public int PartsPerSection
        {
            get { return Mathf.Clamp(partsPerSection, 1, 150); }
            //idea.. we may skip updateui if splitmode is not PartsPerSection
            set { ParamChanged(ref partsPerSection, Mathf.Clamp(value, 1, 150)); }
        }


        /// <summary>skip straight lines optimization. Used by Base Math</summary>
        public bool DoNotOptimizeStraightLines
        {
            get { return doNotOptimizeStraightLines; }
            set { ParamChanged(ref doNotOptimizeStraightLines, value); }
        }

        /// <summary>Use local coordinates instead of world for positions</summary>
        public virtual bool UseLocal
        {
            get { return useLocal; }
            set { ParamChanged(ref useLocal, value); }
        }

        //===============================================================================================
        //                                                    Editor stuff
        //===============================================================================================
        public override string Warning
        {
            get
            {
                var math = Math;
                var warning = "";

                if (math == null) return warning;

                //check math precision
                switch (SplitMode)
                {
                    case SplitModeEnum.PartsTotal:

                        var curvedSectionsCount = math.Math.SectionsCount - (DoNotOptimizeStraightLines ? 0 : CountStraightLines(Math.Math, null));
                        var partsForCurved = curvedSectionsCount == 0 ? 0 : PartsTotal/curvedSectionsCount;

                        if (partsForCurved > math.SectionParts)
                            warning = "Math use less parts per section (" + math.SectionParts + "). You now use " + partsForCurved +
                                      " parts for curved section. You need to increase Math's 'SectionParts' field accordingly to increase polyline precision.";
                        break;
                    case SplitModeEnum.PartsPerSection:

                        if (PartsPerSection > math.SectionParts)
                            warning = "Math use less parts per section (" + math.SectionParts + "). You need to increase Math's 'SectionParts' field accordingly to increase polyline precision.";

                        break;
                }
                return warning;
            }
        }

        public override string Info
        {
            get { return "Polyline has " + PointsCount + " points"; }
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
        [Range(.5f, 1.5f)] [Tooltip("Spheres scale")] [SerializeField] private float spheresScale = 1;
        [SerializeField] [Tooltip("Spheres color")] private Color spheresColor = Color.white;
        [Range(2, 100)] [Tooltip("Maximum number of spheres. This parameter only affects how much points are shown in the Editor")] [SerializeField] private int spheresCount = 100;

        public float SpheresScale
        {
            get { return spheresScale; }
            set { spheresScale = value; }
        }

        public Color SpheresColor
        {
            get { return spheresColor; }
            set { spheresColor = value; }
        }

        public int SpheresCount
        {
            get { return spheresCount; }
            set { spheresCount = value; }
        }
#endif

        //===============================================================================================
        //                                                    Fields (Not persistent)
        //===============================================================================================

        //do not remove readonly. Reusable positions list, storing last calculated positions
        protected readonly List<Vector3> positions = new List<Vector3>();
        //if current data valid?
        private bool dataValid;
        //reusable array, storing if section is straight. 
        private bool[] straightBits;


        //different providers for different modes
        private PositionsProvider positionsProvider;


        /// <summary> latest points count. It's not getting updated until it's queried </summary>
        public int PointsCount
        {
            get
            {
                if (!dataValid) UpdateData();
                return positions == null ? 0 : positions.Count;
            }
        }

        /// <summary> latest points positions used. It's not getting updated until it's queried </summary>
        public List<Vector3> Positions
        {
            get
            {
                if (!dataValid) UpdateData();
                return positions;
            }
        }

        //===============================================================================================
        //                                                    Unity Callbacks
        //===============================================================================================
        public override void Start()
        {
            AddListeners();
        }

        public override void OnDestroy()
        {
            RemoveListeners();
        }

        //===============================================================================================
        //                                                    Public methods
        //===============================================================================================
        /// <summary>init all listeners for math and it's own params </summary>
        public void AddListeners()
        {
            //monitor underlying math changes 
            Math.ChangedMath -= UpdateRequested;
            Math.ChangedMath += UpdateRequested;

            //monitor own params
            ChangedParams -= UpdateRequested;
            ChangedParams += UpdateRequested;
        }

        /// <summary>mark data as invalid</summary>
        public void InvalidateData()
        {
            dataValid = false;
            if (ChangedPositions != null) ChangedPositions(this, null);
        }

        /// <summary>remove attached listeners</summary>
        public void RemoveListeners()
        {
            try
            {
            Math.ChangedMath -= UpdateRequested;
            ChangedParams -= UpdateRequested;
        }
            catch (MissingReferenceException)
            {
            }
        }


        //===============================================================================================
        //                                                    Private methods
        //===============================================================================================
        //refresh data
        private void UpdateData()
        {
            dataValid = true;


            Transform myTransform;
            try
            {
                myTransform = transform;
            }
            catch (MissingReferenceException)
            {
                RemoveListeners();
                return;
            }


            var noData = true;

            //have no idea how to cope with UndoRedo
            try
            {
                noData = Math == null || Math.Math == null || Math.Math.SectionsCount == 0;
            }
            catch (MissingReferenceException)
            {
            }

            positions.Clear();
            if (noData) return;

            var baseMath = Math.Math;
            var sectionsCount = baseMath.SectionsCount;

            //count number of straight lines
            var straightLinesCount = 0;
            if (!doNotOptimizeStraightLines)
            {
                //resize only if length < sectionsCount to reduce GC
                if (straightBits == null || straightBits.Length < sectionsCount) Array.Resize(ref straightBits, sectionsCount);
                straightLinesCount = CountStraightLines(baseMath, straightBits);
            }

            //recalculate points
            InitProvider(ref positionsProvider, this).Build(positions, straightLinesCount, straightBits);


            if (!UseLocal) return;

            //slow convertion (world->local)
            var matrix = myTransform.worldToLocalMatrix;
            var count = positions.Count;
            for (var i = 0; i < count; i++) positions[i] = matrix.MultiplyPoint(positions[i]);
        }


        //curve was updated
        protected virtual void UpdateRequested(object sender, EventArgs e)
        {
            InvalidateData();
        }

        //init required provider. Each mode has it's own provider
        private static PositionsProvider InitProvider(ref PositionsProvider positionsProvider, BGCcSplitterPolyline cc)
        {
            //assign positions provider if needed
            var mode = cc.splitMode;
            var providerObsolete = positionsProvider == null || !positionsProvider.Comply(mode);
            switch (mode)
            {
                case SplitModeEnum.PartsTotal:
                    if (providerObsolete) positionsProvider = new PositionsProviderTotalParts();
                    ((PositionsProviderTotalParts) positionsProvider).Init(cc.Math, cc.PartsTotal);
                    break;
                case SplitModeEnum.PartsPerSection:
                    if (providerObsolete) positionsProvider = new PositionsProviderPartsPerSection();
                    ((PositionsProviderPartsPerSection) positionsProvider).Init(cc.Math, cc.PartsPerSection);
                    break;
                default:
                    //                    case SplitModeEnum.UseMathData:
                    if (providerObsolete) positionsProvider = new PositionsProviderMath();
                    ((PositionsProviderMath) positionsProvider).Init(cc.Math);
                    break;
            }
            return positionsProvider;
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
        //                                                    Helper classes
        //===============================================================================================

        //============================================= Abstract provider
        /// <summary>Abstract provider</summary>
        public abstract class PositionsProvider
        {
            //used math
            protected BGCcMath Math;

            protected virtual void InitInner(BGCcMath math)
            {
                Math = math;
            }

            /// <summary>if this provider can build points for the given mode </summary>
            public abstract bool Comply(SplitModeEnum splitMode);

            /// <summary>calculate points fro polyline</summary>
            public virtual void Build(List<Vector3> positions, int straightLinesCount, bool[] straightBits)
            {
                var math = Math.Math;

                var sections = math.SectionInfos;
                var sectionsCount = sections.Count;


                //first point always present
                positions.Add(math[0][0].Position);

                //fill in points
                if (straightLinesCount == 0) for (var i = 0; i < sectionsCount; i++) FillInSplitSection(sections[i], positions);
                else
                {
                    for (var i = 0; i < sectionsCount; i++)
                    {
                        var section = sections[i];

                        if (straightBits[i]) positions.Add(section[section.PointsCount - 1].Position);
                        else FillInSplitSection(section, positions);
                    }
                }
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
                InitInner(math);

                this.parts = parts;
            }

            public override bool Comply(SplitModeEnum splitMode)
            {
                return splitMode == SplitModeEnum.PartsTotal;
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
                InitInner(math);
                parts = partsPerSection;
            }

            public override bool Comply(SplitModeEnum splitMode)
            {
                return splitMode == SplitModeEnum.PartsPerSection;
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
            public void Init(BGCcMath ccMath)
            {
                InitInner(ccMath);
            }

            public override bool Comply(SplitModeEnum splitMode)
            {
                return splitMode == SplitModeEnum.UseMathData;
            }

            // add points for a split section 
            protected override void FillInSplitSection(BGCurveBaseMath.SectionInfo section, List<Vector3> result)
            {
                var sectionPoints = section.Points;
                var count = sectionPoints.Count;

                for (var j = 1; j < count; j++) result.Add(sectionPoints[j].Position);
            }
        }
    }
}