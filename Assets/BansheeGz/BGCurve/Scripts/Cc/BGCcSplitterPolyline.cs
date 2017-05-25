using System;
using System.Collections.Generic;
using UnityEngine;

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

                        var curvedSectionsCount = math.Math.SectionsCount - (DoNotOptimizeStraightLines ? 0 : BGPolylineSplitter.CountStraightLines(Math.Math, null));
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
        protected bool dataValid;

        private BGPolylineSplitter splitter;
        private BGPolylineSplitter.Config config;


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

            if (splitter == null) splitter = new BGPolylineSplitter();
            if (config == null) config = new BGPolylineSplitter.Config();

            config.DoNotOptimizeStraightLines = doNotOptimizeStraightLines;
            config.SplitMode = splitMode;
            config.PartsTotal = partsTotal;
            config.PartsPerSection = partsPerSection;
            config.UseLocal = UseLocal;
            config.Transform = myTransform;

            splitter.Bind(positions, Math, config);
        }


        //curve was updated
        protected virtual void UpdateRequested(object sender, EventArgs e)
        {
            InvalidateData();
        }


    }
}