using System;
using System.Collections;
using UnityEngine;
using BansheeGz.BGSpline.Curve;

namespace BansheeGz.BGSpline.Components
{
    /// <summary> Triangulate 2D spline. Currently only simple polygons are supported</summary>
    [HelpURL("http://www.bansheegz.com/BGCurve/Cc/BGCcTriangulate2D")]
    [DisallowMultipleComponent]
    [
        CcDescriptor(
            Description = "Triangulate 2D spline. Currently only simple polygons are supported.",
            Name = "Triangulate 2D",
            Icon = "BGCcTriangulate2D123")
    ]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [AddComponentMenu("BansheeGz/BGCurve/Components/BGCcTriangulate2D")]
    [ExecuteInEditMode]
    public class BGCcTriangulate2D : BGCcSplitterPolyline
    {
        //===============================================================================================
        //                                                    Fields (Persistent)
        //===============================================================================================

        [SerializeField] [Tooltip("UV scale")] private Vector2 scaleUV = new Vector2(1, 1);
        [SerializeField] [Tooltip("UV offset")] private Vector2 offsetUV = new Vector2(0, 0);
        [SerializeField] [Tooltip("Flip triangles")] private bool flip;
        [SerializeField] [Tooltip("Double sided")] private bool doubleSided;
        [SerializeField] [Tooltip("UV scale for back side")] private Vector2 scaleBackUV = new Vector2(1, 1);
        [SerializeField] [Tooltip("UV offset for back side")] private Vector2 offsetBackUV = new Vector2(0, 0);
        [SerializeField] [Tooltip("Update mesh every frame, even if curve's not changed. This can be useful, if UVs are animated.")] private bool updateEveryFrame;

        //the number of frame, last triangulation was build
        private int updateAtFrame;

        //keeps track about running Coroutine for every frame update
        private bool everyFrameUpdateIsRunning;

        /// <summary>Scale all UV values</summary>
        public Vector2 ScaleUv
        {
            get { return scaleUV; }
            set
            {
                if (Mathf.Abs(scaleUV.x - value.x) < BGCurve.Epsilon && Mathf.Abs(scaleUV.y - value.y) < BGCurve.Epsilon) return;
                ParamChanged(ref scaleUV, value);
            }
        }

        /// <summary>Should faces be flipped </summary>
        public bool Flip
        {
            get { return flip; }
            set
            {
                if (flip == value) return;
                ParamChanged(ref flip, value);
            }
        }

        /// <summary>Generate triangles for backside</summary>
        public bool DoubleSided
        {
            get { return doubleSided; }
            set { ParamChanged(ref doubleSided, value); }
        }

        /// <summary>Should triangulation occur every frame even if curve is not changed </summary>
        public bool UpdateEveryFrame
        {
            get { return updateEveryFrame; }
            set
            {
                if (updateEveryFrame == value) return;
                updateEveryFrame = value;
                ParamChanged(ref updateEveryFrame, value);

                if (updateEveryFrame && !everyFrameUpdateIsRunning && gameObject.activeSelf && Application.isPlaying) StartCoroutine(UiUpdater());
            }
        }

        // override parent UseLocal to be always true. I doubt, there is any way to use world's coordinates for meshes (like local for LineRenderer)
        public override bool UseLocal
        {
            get { return true; }
        }

        //===============================================================================================
        //                                                    Editor stuff
        //===============================================================================================

        public override string Error
        {
            get { return !Curve.Mode2DOn ? "Curve should be in 2D mode" : null; }
        }

        public override string Info
        {
            get
            {
                var meshFilter = MeshFilter;
                if (meshFilter == null) return "No data.";
                var mesh = meshFilter.sharedMesh;
                if (mesh == null) return "No data.";
                return "Mesh uses " + mesh.vertexCount + " vertices and " + mesh.triangles.Length/3 + " triangles.";
            }
        }

        //===============================================================================================
        //                                                    Fields (Not persistent)
        //===============================================================================================

        [NonSerialized] private MeshFilter meshFilter;
        [NonSerialized] private BGTriangulator2D triangulator;


        public MeshFilter MeshFilter
        {
            get
            {
                //do not replace with ??
                if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
                return meshFilter;
            }
        }

        //===============================================================================================
        //                                                    Unity Callbacks
        //===============================================================================================

        public override void Start()
        {
            base.Start();
            if (MeshFilter.sharedMesh == null) UpdateUI();

            if (updateEveryFrame && gameObject.activeSelf && Application.isPlaying) StartCoroutine(UiUpdater());
        }

        void OnEnable()
        {
            if (updateEveryFrame && !everyFrameUpdateIsRunning && Application.isPlaying) StartCoroutine(UiUpdater());
        }

        void OnDisable()
        {
            if (updateEveryFrame && everyFrameUpdateIsRunning && Application.isPlaying) everyFrameUpdateIsRunning = false;
        }

        //===============================================================================================
        //                                                    Public Functions
        //===============================================================================================
        public void UpdateUI()
        {
            updateAtFrame = Time.frameCount;

            if (!Curve.Mode2DOn) return;

            var positions = Positions;

            MeshFilter meshFilter;
            try
            {
                meshFilter = MeshFilter;
            }
            catch (MissingReferenceException)
            {
                RemoveListeners();
                return;
            }
            var mesh = meshFilter.mesh;
            if (mesh == null)
            {
                mesh = new Mesh();
                meshFilter.mesh = mesh;
            }

            if (triangulator == null) triangulator = new BGTriangulator2D();

            triangulator.Bind(mesh, positions, new BGTriangulator2D.Config
            {
                Closed = Curve.Closed,
                Mode2D = Curve.Mode2D,
                Flip = flip,
                ScaleUV = scaleUV,
                OffsetUV = offsetUV,
                DoubleSided = doubleSided,
                ScaleBackUV = scaleBackUV,
                OffsetBackUV = offsetBackUV,
            });
        }

        //===============================================================================================
        //                                                    Private Functions
        //===============================================================================================
        // curve's changed
        protected override void UpdateRequested(object sender, EventArgs e)
        {
            base.UpdateRequested(sender, e);
            UpdateUI();
        }

        private IEnumerator UiUpdater()
        {
            everyFrameUpdateIsRunning = true;
            while (updateEveryFrame)
            {
                if (updateAtFrame != Time.frameCount) UpdateUI();
                yield return null;
            }

            everyFrameUpdateIsRunning = false;
        }
    }
}