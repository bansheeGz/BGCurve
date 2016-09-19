using System;
using UnityEngine;
using BansheeGz.BGSpline.Curve;

namespace BansheeGz.BGSpline.Components
{
    /// <summary> 
    /// Visualize curve in Play mode with standard LineRenderer Component. 
    /// This component updates LineRenderer vertex count & positions only
    /// </summary>
    [HelpURL("http://www.bansheegz.com/BGCurve/Cc/BGCcVisualizationLineRenderer")]
    [RequireComponent(typeof (LineRenderer))]
    [DisallowMultipleComponent]
    [
        CcDescriptor(
            Description = "Visualize curve with standard LineRenderer Unity component.",
            Name = "Visualization Line Renderer",
            Image = "Assets/BansheeGz/BGCurve/Icons/Components/BGCcVisualizationLineRenderer123.png")
    ]
    [AddComponentMenu("BansheeGz/BGCurve/Components/BGCcLineRenderer", 2)]
    public class BGCcVisualizationLineRenderer : BGCcWithMath
    {
        public enum SplitModeEnum
        {
            UseMathData = 0,
            PartsTotal = 1,
            PartsPerSection = 2
        }

        /// <summary>ui updated</summary>
        public event EventHandler ChangedVisualization;

        [SerializeField] [Tooltip("How to split the curve. " +
                                  "TotalSections -total sections for whole curve;\r\n " +
                                  "PartSections - each part (between 2 points) will use the same amount of splits;\r\n" +
                                  "UseMathData -use data, precalculated by Math component. Note, you can tweak some params at Math as well.")] private SplitModeEnum splitMode =
                                      SplitModeEnum.UseMathData;


        [SerializeField] [Range(1, 1000)] [Tooltip("Total number of parts to split a curve to. The actual number of parts can be less than partsTotal due to optimization, but never more.")] private
            int partsTotal = 30;


        [SerializeField] [Range(1, 150)] [Tooltip("Every section of the curve will be split on even parts. The actual number of parts can be less than partsPerSection due to optimization, but never more.")] private int
            partsPerSection = 30;


        [SerializeField] [Tooltip("Split straight lines. Straight lines are optimized by default and are not split.")] private bool doNotOptimizeStraightLines;



        public SplitModeEnum SplitMode
        {
            get { return splitMode; }
            set { ParamChanged(ref splitMode, value); }
        }

        public int PartsTotal
        {
            get { return Mathf.Clamp(partsTotal, 1, 1000); }
            //idea.. we may skip updateui if splitmode is not PartsTotal
            set { ParamChanged(ref partsTotal, Mathf.Clamp(value, 1, 1000)); }
        }

        public int PartsPerSection
        {
            get { return Mathf.Clamp(partsPerSection, 1, 150); }
            //idea.. we may skip updateui if splitmode is not PartsPerSection
            set { ParamChanged(ref partsPerSection, Mathf.Clamp(value, 1, 150)); }
        }


        public bool DoNotOptimizeStraightLines
        {
            get { return doNotOptimizeStraightLines; }
            set { if (ParamChanged(ref doNotOptimizeStraightLines, value)) UpdateUI(); }
        }


        public override string Error
        {
            get
            {
                if (LineRenderer == null) return "LineRenderer is null";
                if (Math == null) return "Math is null";
                return null;
            }
        }

        public override string Warning
        {
            get
            {
                var lineRenderer = LineRenderer;
                var math = Math;

                if (lineRenderer == null || math == null) return null;

                //check math precision
                var warning = "";
                switch (SplitMode)
                {
                    case SplitModeEnum.PartsTotal:

                        var partsForCurved = partsTotal/(math.Math.SectionsCount - (doNotOptimizeStraightLines ? 0 : PositionsProvider.CountStraightLines(Math.Math, straightBits)));

                        if (partsForCurved > math.SectionParts)
                            warning = "Math use less parts per section (" + math.SectionParts + "). You now use " + partsForCurved +
                                      " parts for curved section. You need to increase Math's 'SectionParts' field accordingly to increase LineRenderer precision.";
                        break;
                    case SplitModeEnum.PartsPerSection:

                        if (partsPerSection > math.SectionParts)
                            warning = "Math use less parts per section (" + math.SectionParts + "). You need to increase Math's 'SectionParts' field accordingly to increase LineRenderer precision.";

                        break;
                }


                if (!lineRenderer.useWorldSpace)
                    warning += "\r\nLineRenderer uses local space (LineRenderer.useWorldSpace=false)! " +
                               "This is not optimal, especially if you plan to update a curve at runtime. Try to set LineRenderer.useWorldSpace to true";

                return warning.Length == 0 ? null : warning;
            }
        }

        public override string Info
        {
            get { return lineRenderer != null ? "LineRenderer uses " + pointsCount + " points" : "LineRenderer is null"; }
        }

        //linerenderer  cached
        private LineRenderer lineRenderer;

        //for status
        private int pointsCount;

        //different providers for different modes
        private PositionsProvider positionsProvider;

        private bool[] straightBits;

        private Vector3[] positions;

        public LineRenderer LineRenderer
        {
            get
            {
                //do not replace with ??
                if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
                return lineRenderer;
            }
        }

        // Use this for initialization
        public override void Start()
        {
            UpdateUI();

            AddListeners();
        }


        public void AddListeners()
        {
            //monitor underlying math changes 
            Math.ChangedMath -= UpdateUi;
            Math.ChangedMath += UpdateUi;

            //monitor own params
            ChangedParams += UpdateUi;
        }

        public override void OnDestroy()
        {
            RemoveListeners();
        }

        public void RemoveListeners()
        {
            Math.ChangedMath -= UpdateUi;
            ChangedParams -= UpdateUi;
        }

        private void UpdateUi(object sender, EventArgs e)
        {
            UpdateUI();
        }

        public override void AddedInEditor()
        {
            UpdateUI();
        }

        private void SetStraightBits(bool[] straightBits)
        {
            this.straightBits = straightBits;
        }

        //copy paste is used for performance reason
        public void UpdateUI()
        {

            try
            {
                //have no idea how to cope with UndoRedo
                if (Math == null) return;
            }
            catch (MissingReferenceException e)
            {
                e.HelpLink = e.HelpLink;
                return;
            }
            var math = Math.Math;

            if (math == null) return;

            LineRenderer lineRenderer;
            try
            {
                //have no idea how to cope with UndoRedo
                lineRenderer = LineRenderer;
            }
            catch (MissingReferenceException e)
            {
                e.HelpLink = e.HelpLink;
                return;
            }

            if (lineRenderer == null) return;

            var curve = Curve;

            if (curve == null) return;

            //not enough points
            if (math.SectionsCount == 0)
            {
                var changed = pointsCount != 0;
                pointsCount = 0;
                lineRenderer.SetVertexCount(0);
                if (changed && ChangedVisualization != null) ChangedVisualization(this, null);
                return;
            }

            //==============  ok

            //get points
            InitProvider(ref positionsProvider, this).Build(ref positions);

            //if spaces are different -> convert
            if (!lineRenderer.useWorldSpace)
            {
                //slow convertion
                var matrix = transform.worldToLocalMatrix;
                for (var i = 0; i < positions.Length; i++) positions[i] = matrix.MultiplyPoint(positions[i]);
            }

            //update LineRenderer
            lineRenderer.SetVertexCount(positions.Length);
            lineRenderer.SetPositions(positions);

            pointsCount = positions.Length;

            if (ChangedVisualization != null) ChangedVisualization(this, null);
        }

        private static PositionsProvider InitProvider(ref PositionsProvider positionsProvider, BGCcVisualizationLineRenderer cc)
        {
            //assign positions provider if needed
            var mode = cc.splitMode;
            if (positionsProvider == null || !positionsProvider.Comply(mode))
            {
                switch (mode)
                {
                    case SplitModeEnum.PartsTotal:
                        positionsProvider = new PositionsProviderTotalParts();
                        break;
                    case SplitModeEnum.PartsPerSection:
                        positionsProvider = new PositionsProviderPartsPerSection();
                        break;
                    default:
//                    case SplitModeEnum.UseMathData:
                        positionsProvider = new PositionsProviderMath();
                        break;
                }
            }

            //init provider
            positionsProvider.Init(cc);
            return positionsProvider;
        }


        //==========================================  Abstract provider
        private abstract class PositionsProvider
        {
            protected BGCurveBaseMath Math;

            private BGCcVisualizationLineRenderer cc;
            private bool doNotOptimizeStraightLines;
            private bool[] straightBits;

            public virtual void Init(BGCcVisualizationLineRenderer cc)
            {
                this.cc = cc;
                doNotOptimizeStraightLines = cc.doNotOptimizeStraightLines;
                Math = cc.Math.Math;
                straightBits = cc.straightBits;
            }

            public abstract bool Comply(SplitModeEnum splitMode);

            public virtual void Build(ref Vector3[] positions)
            {
                var sections = Math.SectionInfos;
                var sectionsCount = sections.Length;

                //count number of straight lines
                var straightLinesCount = 0;
                if (!doNotOptimizeStraightLines)
                {
                    if (straightBits == null || straightBits.Length != sectionsCount)
                    {
                        Array.Resize(ref straightBits, sectionsCount);
                        cc.SetStraightBits(straightBits);
                    }
                    straightLinesCount = CountStraightLines(Math, straightBits);
                }
                else
                {
                    straightBits = null;
                    cc.SetStraightBits(straightBits);
                }

                var totalPoints = 1 + straightLinesCount + TotalNumberOfPointsForCurvedLines(straightLinesCount, sectionsCount - straightLinesCount);


                Array.Resize(ref positions, totalPoints);
                var cursor = 0;

                //first point always present
                positions[cursor++] = Math[0][0].Position;

                //fill in points
                if (straightBits == null)
                {
                    for (var i = 0; i < sectionsCount; i++) FillInSplitSection(sections[i], positions, ref cursor);
                }
                else
                {
                    for (var i = 0; i < sectionsCount; i++)
                    {
                        var section = sections[i];

                        if (straightBits[i]) positions[cursor++] = section[section.PointsCount - 1].Position;
                        else FillInSplitSection(section, positions, ref cursor);
                    }
                }
            }

            public static int CountStraightLines(BGCurveBaseMath math, bool[] straight)
            {
                var curve = math.Curve;
                var points = curve.Points;
                var sections = math.SectionInfos;
                var sectionsCount = sections.Length;


                var straightLinesCount = 0;
                var previousControlAbsent = points[0].ControlType == BGCurvePoint.ControlTypeEnum.Absent;
                for (var i = 0; i < sectionsCount; i++)
                {
                    var nextPoint = curve.Closed && i == sectionsCount - 1 ? points[0] : points[i + 1];
                    var nextControlAbsent = nextPoint.ControlType == BGCurvePoint.ControlTypeEnum.Absent;

                    if (previousControlAbsent && nextControlAbsent)
                    {
                        straight[i] = true;
                        straightLinesCount++;
                    }

                    previousControlAbsent = nextControlAbsent;
                }
                return straightLinesCount;
            }

            protected static void FillIn(BGCurveBaseMath.SectionInfo section, Vector3[] result, ref int cursor, int parts)
            {
                var onePartDistance = section.Distance/parts;
                for (var j = 1; j <= parts; j++)
                {
                    Vector3 tangent;
                    section.CalcByDistance(onePartDistance*j, out result[cursor++], out tangent, true, false);
                }
            }


            protected abstract void FillInSplitSection(BGCurveBaseMath.SectionInfo section, Vector3[] result, ref int cursor);

            protected abstract int TotalNumberOfPointsForCurvedLines(int straightLinesCount, int curvedLinesCount);
        }

        //==========================================  Total parts
        private sealed class PositionsProviderTotalParts : PositionsProvider
        {
            private int parts;
            private int reminderForCurved;
            private int partsPerSectionFloor;

            public override void Init(BGCcVisualizationLineRenderer cc)
            {
                base.Init(cc);
                parts = cc.PartsTotal;
            }

            public override bool Comply(SplitModeEnum splitMode)
            {
                return splitMode == SplitModeEnum.PartsTotal;
            }

            protected override int TotalNumberOfPointsForCurvedLines(int straightLinesCount, int curvedLinesCount)
            {
                var sectionsCount = Math.SectionsCount;

                //at least one section is curved (maxParts>=sectionsCount, so floatParts >=1)
                var floatParts = (parts - straightLinesCount)/(float) (sectionsCount - straightLinesCount);

                reminderForCurved = (int) ((parts - straightLinesCount)%(float) (sectionsCount - straightLinesCount));
                partsPerSectionFloor = Mathf.FloorToInt(floatParts);

                return parts - straightLinesCount;
            }

            protected override void FillInSplitSection(BGCurveBaseMath.SectionInfo section, Vector3[] result, ref int cursor)
            {
                //curved
                var partsForSection = partsPerSectionFloor;
                if (reminderForCurved > 0)
                {
                    partsForSection++;
                    reminderForCurved--;
                }

                FillIn(section, result, ref cursor, partsForSection);
            }

            public override void Build(ref Vector3[] positions)
            {
                var curve = Math.Curve;
                var sections = Math.SectionInfos;
                var sectionsCount = sections.Length;

                if (parts < sectionsCount)
                {
                    if (parts == 1)
                    {
                        //only one part per whole curve (one->last)
                        Array.Resize(ref positions, 2);
                        positions[0] = sections[0][0].Position;
                        if (curve.Closed)
                        {
                            positions[1] = Math.CalcByDistanceRatio(BGCurveBaseMath.Field.Position, .5f);
                        }
                        else
                        {
                            positions[1] = sections[sections.Length - 1][sections[sections.Length - 1].PointsCount - 1].Position;
                        }
                    }
                    else if (parts == 2 && curve.Closed)
                    {
                        Array.Resize(ref positions, 3);
                        positions[0] = sections[0][0].Position;
                        positions[1] = Math.CalcByDistanceRatio(BGCurveBaseMath.Field.Position, .33f);
                        positions[2] = Math.CalcByDistanceRatio(BGCurveBaseMath.Field.Position, .66f);
                    }
                    else
                    {
                        Array.Resize(ref positions, parts + 1);
                        for (var i = 0; i <= parts; i++) positions[i] = Math.CalcByDistanceRatio(BGCurveBaseMath.Field.Position, i/(float) parts);
                    }
                }
                else
                {
                    base.Build(ref positions);
                }
            }
        }

        //==========================================  Parts per section
        private sealed class PositionsProviderPartsPerSection : PositionsProvider
        {
            private int parts;

            public override void Init(BGCcVisualizationLineRenderer cc)
            {
                base.Init(cc);
                parts = cc.PartsPerSection;
            }

            public override bool Comply(SplitModeEnum splitMode)
            {
                return splitMode == SplitModeEnum.PartsPerSection;
            }

            protected override int TotalNumberOfPointsForCurvedLines(int straightLinesCount, int curvedLinesCount)
            {
                return parts*curvedLinesCount;
            }

            protected override void FillInSplitSection(BGCurveBaseMath.SectionInfo section, Vector3[] result, ref int cursor)
            {
                FillIn(section, result, ref cursor, parts);
            }
        }

        //==========================================  Use Math
        private sealed class PositionsProviderMath : PositionsProvider
        {
            public override bool Comply(SplitModeEnum splitMode)
            {
                return splitMode == SplitModeEnum.UseMathData;
            }

            protected override int TotalNumberOfPointsForCurvedLines(int straightLinesCount, int curvedLinesCount)
            {
                return Math.Configuration.Parts*curvedLinesCount;
            }

            protected override void FillInSplitSection(BGCurveBaseMath.SectionInfo section, Vector3[] result, ref int cursor)
            {
                var sectionPoints = section.Points;
                var count = sectionPoints.Length;

                for (var j = 1; j < count; j++) result[cursor++] = sectionPoints[j].Position;
            }
        }
    }
}