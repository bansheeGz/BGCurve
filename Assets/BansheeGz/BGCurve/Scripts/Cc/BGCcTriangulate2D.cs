using System;
using System.Collections;
using System.Collections.Generic;
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
            Image = "Assets/BansheeGz/BGCurve/Icons/Components/BGCcTriangulate2D123.png")
    ]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [AddComponentMenu("BansheeGz/BGCurve/Components/BGCcTriangulate2D")]
    [ExecuteInEditMode]
    public class BGCcTriangulate2D : BGCcSplitterPolyline
    {
        //===============================================================================================
        //                                                    Static
        //===============================================================================================
        //min scale for UV
        private const float MinUvScale = 0.000001f;
        //max scale for UV
        private const float MaxUvScale = 1000000;

        //temp List to avoid GC (it's used by traingulation algorithm)
        private static readonly List<int> V = new List<int>();

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
            set { }
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
            get { return "Mesh uses " + vertices.Count + " vertices and " + (triangles.Count / 3) + " triangles."; }
        }

        //===============================================================================================
        //                                                    Fields (Not persistent)
        //===============================================================================================

        [NonSerialized] private MeshFilter meshFilter;
        [NonSerialized] private readonly List<Vector3> vertices = new List<Vector3>();
        [NonSerialized] private readonly List<Vector2> points = new List<Vector2>();
        [NonSerialized] private readonly List<Vector2> uvs = new List<Vector2>();
        [NonSerialized] private readonly List<int> triangles = new List<int>();



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

            var mode2D = Curve.Mode2D;

            var positions = Positions;

            var positionsCount = positions.Count;
            //I dont know why, but triangulator works wrong in case first pos= last pos
            if (Curve.Closed) positionsCount--;

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
            var mesh = meshFilter.sharedMesh;
            if (mesh == null)
            {
                mesh = new Mesh();
                meshFilter.mesh = mesh;
            }

            vertices.Clear();
            triangles.Clear();
            uvs.Clear();
            if (positionsCount > 2)
            {
                //we need to triangulate
                points.Clear();
                var minMax = new Vector4(float.MaxValue, float.MaxValue, float.MinValue, float.MinValue);
                for (var i = 0; i < positionsCount; i++)
                {
                    var pos = positions[i];

                    Vector3 vert;
                    Vector2 point;
                    switch (mode2D)
                    {
                        case BGCurve.Mode2DEnum.XY:
                            vert = new Vector3(pos.x, pos.y);
                            point = new Vector2(pos.x, pos.y);
                            break;
                        case BGCurve.Mode2DEnum.XZ:
                            vert = new Vector3(pos.x, 0, pos.z);
                            point = new Vector2(pos.x, pos.z);
                            break;
                        default:
                            // case BGCurve.Mode2DEnum.YZ:
                            vert = new Vector3(0, pos.y, pos.z);
                            point = new Vector2(pos.y, pos.z);
                            break;
                    }
                    vertices.Add(vert);
                    points.Add(point);

                    //min
                    if (point.x < minMax.x) minMax.x = point.x;
                    if (point.y < minMax.y) minMax.y = point.y;
                    //max
                    if (point.x > minMax.z) minMax.z = point.x;
                    if (point.y > minMax.w) minMax.w = point.y;
                }

                //tris
                Triangulate(points, triangles);
                if (!flip) triangles.Reverse();

                //uvs
                var width = minMax.z - minMax.x;
                var height = minMax.w - minMax.y;
                var pointsCount = points.Count;
                var scaleX = Mathf.Clamp(scaleUV.x, MinUvScale, MaxUvScale);
                var scaleY = Mathf.Clamp(scaleUV.y, MinUvScale, MaxUvScale);
                for (var i = 0; i < pointsCount; i++)
                {
                    var point = points[i];
                    uvs.Add(new Vector2(offsetUV.x + ((point.x - minMax.x) / width) * scaleX, offsetUV.y + ((point.y - minMax.y) / height) * scaleY));
                }

                // doubleSided
                if (doubleSided)
                {
                    //add points, we can not reuse the same points, cause we need unique UVs
                    var verticesCount = vertices.Count;
                    for (var i = 0; i < verticesCount; i++) vertices.Add(vertices[i]);

                    //add triangles
                    var trianglesCount = triangles.Count;
                    for (var i = trianglesCount - 1; i >= 0; i--) triangles.Add(triangles[i] + verticesCount);

                    //uvs
                    scaleX = Mathf.Clamp(scaleBackUV.x, MinUvScale, MaxUvScale);
                    scaleY = Mathf.Clamp(scaleBackUV.y, MinUvScale, MaxUvScale);
                    for (var i = 0; i < pointsCount; i++)
                    {
                        var point = points[i];
                        uvs.Add(new Vector2(offsetBackUV.x + ((point.x - minMax.x) / width) * scaleX, offsetBackUV.y + ((point.y - minMax.y) / height) * scaleY));
                    }
                }
            }

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
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


        //==================================== This code from http://wiki.unity3d.com/index.php?title=Triangulator
        // Triangulate simple polygon. I have no idea how good this algorithm is and what it's actually doing
        private static void Triangulate(List<Vector2> points, List<int> tris)
        {
            tris.Clear();

            int n = points.Count;
            if (n < 3) return;

            V.Clear();

            if (Area(points) > 0) for (int v = 0; v < n; v++) V.Add(v);
            else for (int v = 0; v < n; v++) V.Add((n - 1) - v);

            int nv = n;
            int count = 2*nv;
            for (int m = 0, v = nv - 1; nv > 2;)
            {
                if ((count--) <= 0) return;

                int u = v;
                if (nv <= u) u = 0;

                v = u + 1;

                if (nv <= v) v = 0;
                int w = v + 1;
                if (nv <= w) w = 0;

                if (Snip(points, u, v, w, nv, V))
                {
                    int a, b, c, s, t;
                    a = V[u];
                    b = V[v];
                    c = V[w];
                    tris.Add(a);
                    tris.Add(b);
                    tris.Add(c);
                    m++;
                    for (s = v, t = v + 1; t < nv; s++, t++) V[s] = V[t];
                    nv--;
                    count = 2*nv;
                }
            }
        }

        private static float Area(List<Vector2> points)
        {
            int n = points.Count;
            float A = 0.0f;
            for (int p = n - 1, q = 0; q < n; p = q++)
            {
                Vector2 pval = points[p];
                Vector2 qval = points[q];
                A += pval.x*qval.y - qval.x*pval.y;
            }
            return (A*0.5f);
        }

        private static bool Snip(List<Vector2> points, int u, int v, int w, int n, List<int> V)
        {
            int p;
            Vector2 A = points[V[u]];
            Vector2 B = points[V[v]];
            Vector2 C = points[V[w]];
            if (Mathf.Epsilon > (((B.x - A.x)*(C.y - A.y)) - ((B.y - A.y)*(C.x - A.x)))) return false;
            for (p = 0; p < n; p++)
            {
                if ((p == u) || (p == v) || (p == w)) continue;
                Vector2 P = points[V[p]];
                if (InsideTriangle(A, B, C, P)) return false;
            }
            return true;
        }

        private static bool InsideTriangle(Vector2 A, Vector2 B, Vector2 C, Vector2 P)
        {
            float ax, ay, bx, by, cx, cy, apx, apy, bpx, bpy, cpx, cpy;
            float cCROSSap, bCROSScp, aCROSSbp;

            ax = C.x - B.x;
            ay = C.y - B.y;
            bx = A.x - C.x;
            by = A.y - C.y;
            cx = B.x - A.x;
            cy = B.y - A.y;
            apx = P.x - A.x;
            apy = P.y - A.y;
            bpx = P.x - B.x;
            bpy = P.y - B.y;
            cpx = P.x - C.x;
            cpy = P.y - C.y;

            aCROSSbp = ax*bpy - ay*bpx;
            cCROSSap = cx*apy - cy*apx;
            bCROSScp = bx*cpy - by*cpx;

            return ((aCROSSbp >= 0.0f) && (bCROSScp >= 0.0f) && (cCROSSap >= 0.0f));
        }
    }
}