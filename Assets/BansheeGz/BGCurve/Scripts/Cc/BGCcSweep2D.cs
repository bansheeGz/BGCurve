using System;
using UnityEngine;
using System.Collections.Generic;
using BansheeGz.BGSpline.Curve;

namespace BansheeGz.BGSpline.Components
{
    /// <summary> Sweep a line or 2d spline along another 2d spline.</summary>
    [HelpURL("http://www.bansheegz.com/BGCurve/Cc/BGCcSweep2D")]
    [DisallowMultipleComponent]
    [
        CcDescriptor(
            Description = "Sweep a line or 2d spline along another 2d spline.",
            Name = "Sweep 2D",
            Icon = "BGCcSweep2d123")
    ]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [AddComponentMenu("BansheeGz/BGCurve/Components/BGCcSweep2D")]
    [ExecuteInEditMode]
    public class BGCcSweep2D : BGCcSplitterPolyline
    {
        //===============================================================================================
        //                                                    Enums
        //===============================================================================================
        /// <summary>Profile mode </summary>
        public enum ProfileModeEnum
        {
            /// <summary>Sweep straight line</summary>
            Line = 0,

            /// <summary>Sweep 2d spline</summary>
            Spline = 1,
        }

        //===============================================================================================
        //                                                    Fields (Persistent)
        //===============================================================================================
        [SerializeField] [Tooltip("Profile mode.\r\n " +
                                  "StraightLine -use straight line as cross section;\r\n " +
                                  "Spline - use 2d spline as cross section;")] private ProfileModeEnum profileMode = ProfileModeEnum.Line;

        [SerializeField] [Tooltip("Line width for StraightLine profile mode")] private float lineWidth = 1;

        [SerializeField] [Tooltip("U coordinate for line start")] private float uCoordinateStart;

        [SerializeField] [Tooltip("U coordinate for line end")] private float uCoordinateEnd = 1;

        [SerializeField] [Tooltip("Profile spline for Spline profile mode")] private BGCcSplitterPolyline profileSpline;

//        [SerializeField] [Tooltip("Custom field, providing U coordinate")] private BGCurvePointField uCoordinateField;

        [SerializeField] [Tooltip("V coordinate multiplier")] private float vCoordinateScale = 1;

        [SerializeField] [Tooltip("Swap U with V coordinate")] private bool swapUV;

        [SerializeField] [Tooltip("Swap mesh normals direction")] private bool swapNormals;

        /// <summary>Mode for profile (cross section) </summary>
        public ProfileModeEnum ProfileMode
        {
            get { return profileMode; }
            set { ParamChanged(ref profileMode, value); }
        }

        /// <summary>Line width for StraightLine profile mode</summary>
        public float LineWidth
        {
            get { return lineWidth; }
            set { ParamChanged(ref lineWidth, value); }
        }

        /// <summary>U coordinate for line start</summary>
        public float UCoordinateStart
        {
            get { return uCoordinateStart; }
            set { ParamChanged(ref uCoordinateStart, value); }
        }

        /// <summary>U coordinate for line end</summary>
        public float UCoordinateEnd
        {
            get { return uCoordinateEnd; }
            set { ParamChanged(ref uCoordinateEnd, value); }
        }

        /// <summary>Profile spline for Spline profile mode</summary>
        public BGCcSplitterPolyline ProfileSpline
        {
            get { return profileSpline; }
            set
            {
                ParamChanged(ref profileSpline, value);
//                if () uCoordinateField = null;
            }
        }

        /// <summary>Swap U and V coordinates</summary>
        public bool SwapUv
        {
            get { return swapUV; }
            set { ParamChanged(ref swapUV, value); }
        }

        /// <summary>Swap normal direction 180 degrees</summary>
        public bool SwapNormals
        {
            get { return swapNormals; }
            set { ParamChanged(ref swapNormals, value); }
        }

/*
        /// <summary>Custom field, providing U coordinate</summary>
        public BGCurvePointField UCoordinateField
        {
            get { return uCoordinateField; }
            set { ParamChanged(ref uCoordinateField, value); }
        }
*/

        /// <summary>V coordinate scale</summary>
        public float VCoordinateScale
        {
            get { return vCoordinateScale; }
            set { ParamChanged(ref vCoordinateScale, value); }
        }

        //===============================================================================================
        //                                                    Editor stuff
        //===============================================================================================
        public override string Error
        {
            get
            {
                return ChoseMessage(base.Error, () =>
                {
                    if (!Curve.Mode2DOn) return "Curve should be in 2D mode";

                    if (profileMode == ProfileModeEnum.Spline)
                    {
                        if (profileSpline == null) return "Profile spline is not set.";
                        if (profileSpline.Curve.Mode2D != BGCurve.Mode2DEnum.XY) return "Profile spline should be in XY 2D mode.";
                        profileSpline.InvalidateData();
                        if (profileSpline.PointsCount < 2) return "Profile spline should have at least 2 points.";
                    }

                    var profilePointsCount = profileMode == ProfileModeEnum.Line ? 2 : profileSpline.PointsCount;
                    if (PointsCount*profilePointsCount > 65534) return "Vertex count per mesh limit is exceeded ( > 65534)";
                    return null;
                });
            }
        }

        //===============================================================================================
        //                                                    Fields (Not persistent)
        //===============================================================================================
        private static readonly List<PositionWithU> crossSectionList = new List<PositionWithU>();

        [NonSerialized] private MeshFilter meshFilter;
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Vector2> uvs = new List<Vector2>();
        private readonly List<int> triangles = new List<int>();

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
            useLocal = true;
            base.Start();
            if (MeshFilter.sharedMesh == null) UpdateUI();
        }

        //===============================================================================================
        //                                                    Public Functions
        //===============================================================================================
        public void UpdateUI()
        {
            if (Error != null) return;

            if (!UseLocal)
            {
                useLocal = true;
                dataValid = false;
            }
            var positions = Positions;
            if (positions.Count < 2) return;

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

            //prepare
            crossSectionList.Clear();
            triangles.Clear();
            uvs.Clear();
            vertices.Clear();

            //------------- cross section points
            if (profileMode == ProfileModeEnum.Line)
            {
                crossSectionList.Add(new PositionWithU {Position = Vector3.left*lineWidth*.5f, U = uCoordinateStart});
                crossSectionList.Add(new PositionWithU {Position = Vector3.right*lineWidth*.5f, U = uCoordinateEnd});
            }
            else
            {
                var points = profileSpline.Positions;
                for (var i = 0; i < points.Count; i++) crossSectionList.Add(new PositionWithU {Position = points[i]});
            }
            var crossSectionCount = crossSectionList.Count;
            var crossSectionDistance = .0f;
            for (var i = 0; i < crossSectionCount - 1; i++) crossSectionDistance += Vector3.Distance(crossSectionList[i].Position, crossSectionList[i + 1].Position);

            //------------- calculate U coord for profile spline
            if (profileMode == ProfileModeEnum.Spline)
            {
                var distance = 0f;
                for (var i = 0; i < crossSectionCount - 1; i++)
                {
                    crossSectionList[i] = new PositionWithU {Position = crossSectionList[i].Position, U = uCoordinateStart + (distance/crossSectionDistance)*(uCoordinateEnd - uCoordinateStart)};
                    distance += Vector3.Distance(crossSectionList[i].Position, crossSectionList[i + 1].Position);
                }
                crossSectionList[crossSectionList.Count - 1] = new PositionWithU {Position = crossSectionList[crossSectionList.Count - 1].Position, U = uCoordinateEnd};
            }

            //------------- normal
            Vector3 normal;
            switch (Curve.Mode2D)
            {
                case BGCurve.Mode2DEnum.XY:
                    normal = swapNormals ? Vector3.back : Vector3.forward;
                    break;
                case BGCurve.Mode2DEnum.XZ:
                    normal = swapNormals ? Vector3.down : Vector3.up;
                    break;
                case BGCurve.Mode2DEnum.YZ:
                    normal = swapNormals ? Vector3.left : Vector3.right;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Curve.Mode2D");
            }

            //------------- build mesh
            //first row
            var closed = Curve.Closed;
            Vector3 firstTangent;
            if (closed)
            {
                var first = positions[1] - positions[0];
                var firstDistance = first.magnitude;
                var last = positions[positions.Count - 1] - positions[positions.Count - 2];
                var lastDistance = last.magnitude;

                var distanceRatio = firstDistance/lastDistance;
                firstTangent = first.normalized + last.normalized*distanceRatio;
            }
            else firstTangent = positions[1] - positions[0];

            var previousForward = firstTangent;
            var previousForwardNormalized = previousForward.normalized;
            var previousForwardDistance = (positions[1] - positions[0]).magnitude;
            var matrix = Matrix4x4.TRS(positions[0], Quaternion.LookRotation(previousForward, normal), Vector3.one);
            for (var i = 0; i < crossSectionCount; i++)
            {
                var positionWithU = crossSectionList[i];
                vertices.Add(matrix.MultiplyPoint(positionWithU.Position));
                uvs.Add(swapUV ? new Vector2(0, positionWithU.U) : new Vector2(positionWithU.U, 0));
            }

            //iterate points
            var currentDistance = previousForwardDistance;
            var positionsCount = positions.Count;
            for (var i = 1; i < positionsCount; i++)
            {
                var pos = positions[i];
                var lastPoint = i == positionsCount - 1;

                var forward = lastPoint ? previousForward : positions[i + 1] - pos;
                var forwardNormalized = forward.normalized;
                var forwardDistance = forward.magnitude;

                var distanceRatio = forwardDistance/previousForwardDistance;
                var tangent = forwardNormalized + previousForwardNormalized*distanceRatio;

                //hack for closed curve
                if (lastPoint && closed) tangent = firstTangent;

                matrix = Matrix4x4.TRS(pos, Quaternion.LookRotation(tangent, normal), Vector3.one);
                var v = currentDistance/crossSectionDistance*vCoordinateScale;

                //vertices + uvs
                for (var j = 0; j < crossSectionCount; j++)
                {
                    var positionWithU = crossSectionList[j];
                    vertices.Add(matrix.MultiplyPoint(positionWithU.Position));
                    uvs.Add(swapUV ? new Vector2(v, positionWithU.U) : new Vector2(positionWithU.U, v));
                }

                //tris
                var firstRowStart = vertices.Count - crossSectionCount*2;
                var secondRowStart = vertices.Count - crossSectionCount;
                for (var j = 0; j < crossSectionCount - 1; j++)
                {
                    triangles.Add(firstRowStart + j);
                    triangles.Add(secondRowStart + j);
                    triangles.Add(firstRowStart + j + 1);

                    triangles.Add(firstRowStart + j + 1);
                    triangles.Add(secondRowStart + j);
                    triangles.Add(secondRowStart + j + 1);
                }

                //update vars
                currentDistance += forwardDistance;
                previousForward = forward;
                previousForwardNormalized = forwardNormalized;
                previousForwardDistance = forwardDistance;
            }


            //set values
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
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


        //===============================================================================================
        //                                                    Private classes
        //===============================================================================================
        private struct PositionWithU
        {
            public Vector3 Position;
            public float U;
        }
    }
}