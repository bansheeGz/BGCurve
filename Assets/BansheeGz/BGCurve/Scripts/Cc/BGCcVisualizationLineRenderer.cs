using System;
using UnityEngine;

namespace BansheeGz.BGSpline.Components
{
    /// <summary> 
    /// Visualize curve in Play mode with standard LineRenderer Component. 
    /// This component updates LineRenderer vertex count & positions only
    /// </summary>
    [HelpURL("http://www.bansheegz.com/BGCurve/Cc/BGCcVisualizationLineRenderer")]
    [RequireComponent(typeof(LineRenderer))]
    [DisallowMultipleComponent]
    [
        CcDescriptor(
            Description = "Visualize curve with standard LineRenderer Unity component.",
            Name = "Cc Line Renderer",
            Image = "Assets/BansheeGz/BGCurve/Icons/Components/BGCcVisualizationLineRenderer123.png")
    ]
    [AddComponentMenu("BansheeGz/BGCurve/Components/BGCcLineRenderer")]
    public class BGCcVisualizationLineRenderer : BGCcSplitterPolyline
    {
        //===============================================================================================
        //                                                    Events
        //===============================================================================================
        /// <summary>ui updated</summary>
        public event EventHandler ChangedVisualization;

        //===============================================================================================
        //                                                    Fields (Persistent)
        //===============================================================================================
        [SerializeField] [Tooltip("Update LineRenderer at Start method.")] private bool updateAtStart;

        /// <summary>If LineRenderer should be updated then Start is called </summary>
        public bool UpdateAtStart
        {
            get { return updateAtStart; }
            set { updateAtStart = value; }
        }

        //===============================================================================================
        //                                                    Editor stuff
        //===============================================================================================
        public override string Error
        {
            get { return ChoseMessage(base.Error, () => (LineRenderer == null) ? "LineRenderer is null" : null); }
        }

        public override string Warning
        {
            get
            {
                var warning = base.Warning;

                var lineRenderer = LineRenderer;
                if (lineRenderer == null) return warning;

                if (!lineRenderer.useWorldSpace)
                    warning += "\r\nLineRenderer uses local space (LineRenderer.useWorldSpace=false)! " +
                               "This is not optimal, especially if you plan to update a curve at runtime. Try to set LineRenderer.useWorldSpace to true";

                return warning.Length == 0 ? null : warning;
            }
        }

        public override string Info
        {
            get { return lineRenderer != null ? "LineRenderer uses " + PointsCount + " points" : "LineRenderer is null"; }
        }

        public override bool SupportHandles
        {
            get { return false; }
        }


        //===============================================================================================
        //                                                    Fields (Not persistent)
        //===============================================================================================
        //linerenderer  cached
        private LineRenderer lineRenderer;

        /// <summary>Get Unity's LineRenderer component </summary>
        public LineRenderer LineRenderer
        {
            get
            {
                //do not replace with ??
                if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
                return lineRenderer;
            }
        }

        //===============================================================================================
        //                                                    Unity Callbacks
        //===============================================================================================
        // Use this for initialization
        public override void Start()
        {
            base.Start();

            if (updateAtStart) UpdateUI();
            else Math.EnsureMathIsCreated();
        }

        //===============================================================================================
        //                                                    Public functions
        //===============================================================================================
        //see parent for comments
        public override void AddedInEditor()
        {
            UpdateUI();
        }

        /// <summary>Update underlying Unity's LineRenderer component </summary>
        public void UpdateUI()
        {
            try
            {
                //have no idea how to cope with UndoRedo
                if (Math == null) return;
            }
            catch (MissingReferenceException)
            {
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
            catch (MissingReferenceException)
            {
                return;
            }

            if (lineRenderer == null) return;

            var curve = Curve;

            if (curve == null) return;


            if (math.SectionsCount == 0)
            {
                //not enough points
#if UNITY_5_5 
                lineRenderer.numPositions = 0;
#elif UNITY_5_6_OR_NEWER
                lineRenderer.positionCount = 0;
#else
                lineRenderer.SetVertexCount(0);
#endif

                if (positions != null && positions.Count > 0 && ChangedVisualization != null) ChangedVisualization(this, null);
                positions.Clear();
            }
            else
            {
                //==============  ok
                useLocal = !lineRenderer.useWorldSpace;
                var newPositions = Positions;

                //update LineRenderer
                var count = newPositions.Count;
                if (count > 0)
                {
#if UNITY_5_5
                    lineRenderer.numPositions = count;
#elif UNITY_5_6_OR_NEWER
                    lineRenderer.positionCount = count;
#else
                    lineRenderer.SetVertexCount(count);
#endif
                    //the only way to get rid of GC is to use slow one-by-one setter unfortunately
                    for (var i = 0; i < count; i++) lineRenderer.SetPosition(i, newPositions[i]);
                }
                else
                {
#if UNITY_5_5
                    lineRenderer.numPositions = 0;
#elif UNITY_5_6_OR_NEWER
                    lineRenderer.positionCount = 0;
#else
                    lineRenderer.SetVertexCount(0);
#endif
                }
                if (ChangedVisualization != null) ChangedVisualization(this, null);
            }
        }

        //===============================================================================================
        //                                                    Private functions
        //===============================================================================================
        //math/curve is changed
        protected override void UpdateRequested(object sender, EventArgs e)
        {
            base.UpdateRequested(sender, e);
            UpdateUI();
        }
    }
}