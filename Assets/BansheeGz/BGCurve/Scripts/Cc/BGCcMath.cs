using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using BansheeGz.BGSpline.Curve;

namespace BansheeGz.BGSpline.Components
{
    /// <summary> Math solver for the curve.</summary>
    [HelpURL("http://www.bansheegz.com/BGCurve/Cc/BGCcMath")]
    [DisallowMultipleComponent]
    [
        CcDescriptor(
            Description = "Math solver for the curve (position, tangent, total distance, position by closest point). With this component you can use math functions.",
            Name = "Math",
            Image = "Assets/BansheeGz/BGCurve/Icons/Components/BGCcMath123.png")
    ]
    [AddComponentMenu("BansheeGz/BGCurve/Components/BGCcMath")]
    public class BGCcMath : BGCc, BGCurveMathI
    {
        //===============================================================================================
        //                                                    Static 
        //===============================================================================================
        // maximum parts for Base math sectionParts parameter
        private const int PartsMax = 100;
        //reusable empty vertices array
        private static readonly Vector3[] EmptyVertices = new Vector3[0];

        //===============================================================================================
        //                                                    Enums 
        //===============================================================================================
        /// <summary> Type of approximation</summary>
        public enum MathTypeEnum
        {
            /// <summary>Every spline's section will be split to even parts </summary>
            Base,

            /// <summary>Every spline's section will be split to uneven parts, based on the spline's curvature </summary>
            Adaptive
        }

        /// <summary> How math should be updated</summary>
        public enum UpdateModeEnum
        {
            /// <summary> Always update math then spline is changed</summary>
            Always = 0,

            /// <summary> Update math only if Axis Aligned Bounding Box for the spline is visible</summary>
            AabbVisible = 1,

            /// <summary> Update math only if some renderer is visible</summary>
            RendererVisible = 2,
        }

        //===============================================================================================
        //                                                    Events (Not persistent)
        //===============================================================================================

        /// <summary>if underlying math is recalculated</summary>
        public event EventHandler ChangedMath;


        //===============================================================================================
        //                                                    Fields (Persistent)
        //===============================================================================================

        //=========================================================== Fields to calculate
        [SerializeField] [Tooltip("Which fields you want to use.")] private BGCurveBaseMath.Fields fields = BGCurveBaseMath.Fields.Position;


        //=========================================================== Math type
        [SerializeField] [Tooltip("Math type to use.\r\n" +
                                  "Base - uses uniformely split sections;\r\n " +
                                  "Adaptive - uses non-uniformely split sections, based on the curvature. Expiremental.")] private MathTypeEnum mathType;


        //=========================================================== Base

        [SerializeField] [Tooltip("The number of equal parts for each section, used by Base math.")] [Range(1, PartsMax)] private int sectionParts = 30;

        [SerializeField] [Tooltip("Use only 2 points for straight lines. Tangents may be calculated slightly different. Used by Base math.")] private bool optimizeStraightLines;

        //=========================================================== Adaptive
        [SerializeField] [Tooltip("Tolerance, used by Adaptive Math. The bigger the tolerance- the lesser splits. " +
                                  "Note: The final tolerance used by Math is based on this value but different.")] [Range(BGCurveAdaptiveMath.MinTolerance, BGCurveAdaptiveMath.MaxTolerance)] private
            float tolerance = .2f;

        //=========================================================== Common
        [SerializeField] [Tooltip("Points position will be used for tangent calculation. This can gain some performance")] private bool usePositionToCalculateTangents;


        //=========================================================== Update modes
        [SerializeField] [Tooltip("Updating math takes some resources. You can fine-tune in which cases math is updated."
                                  + "\r\n1) Always- always update"
                                  + "\r\n2) AabbVisible- update only if AABB (Axis Aligned Bounding Box) around points and controls is visible"
                                  + "\r\n3) RendererVisible- update only if some renderer is visible"
                          )] private UpdateModeEnum updateMode;

        [SerializeField] [Tooltip("Renderer to check for updating math. Math will be updated only if renderer is visible")] private Renderer rendererForUpdateCheck;

        //=========================================================== Persistent Event
        [SerializeField] [Tooltip("Event is fired, then math is recalculated")] private MathChangedEvent mathChangedEvent = new MathChangedEvent();


        //---------------------------------------------------------------------------------------------


        /// <summary>Underlying math type</summary>
        public MathTypeEnum MathType
        {
            get { return mathType; }
            set { ParamChanged(ref mathType, value); }
        }

        //=========================================================== Base
        /// <summary>How many parts to use for splitting every spline's section for Base Math</summary>
        public int SectionParts
        {
            get { return Mathf.Clamp(sectionParts, 1, PartsMax); }
            set { ParamChanged(ref sectionParts, Mathf.Clamp(value, 1, PartsMax)); }
        }

        /// <summary>Should we use 2 points for straight lines or should we split it to sectionParts. Used by Base math.</summary>
        public bool OptimizeStraightLines
        {
            get { return optimizeStraightLines; }
            set { ParamChanged(ref optimizeStraightLines, value); }
        }

        //=========================================================== Adaptive
        /// <summary>Tolerance parameter for Adaptive math. Note, the final value math uses is based on it but differs</summary>
        public float Tolerance
        {
            get { return tolerance; }
            set { ParamChanged(ref tolerance, value); }
        }


        //=========================================================== Common
        /// <summary>Which fields to calculate</summary>
        public BGCurveBaseMath.Fields Fields
        {
            get { return fields; }
            set { ParamChanged(ref fields, value); }
        }


        public bool UsePositionToCalculateTangents
        {
            get { return usePositionToCalculateTangents; }
            set { ParamChanged(ref usePositionToCalculateTangents, value); }
        }


        public UpdateModeEnum UpdateMode
        {
            get { return updateMode; }
            set { ParamChanged(ref updateMode, value); }
        }

        public Renderer RendererForUpdateCheck
        {
            get { return rendererForUpdateCheck; }
            set { ParamChanged(ref rendererForUpdateCheck, value); }
        }


        //===============================================================================================
        //                                                    Editor stuff
        //===============================================================================================
        public override string Error
        {
            get
            {
                return updateMode == UpdateModeEnum.RendererVisible && RendererForUpdateCheck == null
                    ? "Update mode is set to " + updateMode + ", however the RendererForUpdateCheck is null"
                    : null;
            }
        }

        public override string Warning
        {
            get
            {
                if (Curve.SnapType == BGCurve.SnapTypeEnum.Curve && Fields == BGCurveBaseMath.Fields.PositionAndTangent && !UsePositionToCalculateTangents)
                    return "Your curve's snap mode is Curve, and you are calculating tangents. However you use formula for tangents, instead of points positions." +
                           "This may result in wrong tangents. Set UsePositionToCalculateTangents to true.";
                return null;
            }
        }

        public override string Info
        {
            get { return Math == null ? null : "Math uses " + Math.PointsCount + " points"; }
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
        // underlying math solver.
        private BGCurveBaseMath math;

        // visibility checking Component. It uses standard Unity callbacks method to trace attached renderer visibility (AabbVisible and RendererVisible modes)
        private VisibilityCheck visibilityCheck;

        // Mesh filter for generated mesh with 2 points for visibility check (AabbVisible mode)
        private MeshFilter meshFilter;
        // Reusable array for 2 points for visibility check (AabbVisible mode)
        private readonly Vector3[] vertices = new Vector3[2];

        /// <summary>Underlying math solver. You should not cache it, unless you are sure, mathType can not be changed at runtime.</summary>
        public BGCurveBaseMath Math
        {
            get
            {
                if (math == null) InitMath(null, null);
                return math;
            }
        }

        //is new math object is required?
        private bool NewMathRequired
        {
            get
            {
                return math == null
                       || (mathType == MathTypeEnum.Base && math.GetType() != typeof(BGCurveBaseMath))
                       || (mathType == MathTypeEnum.Adaptive && math.GetType() != typeof(BGCurveAdaptiveMath));
            }
        }


        //===============================================================================================
        //                                                    Unity Callbacks
        //===============================================================================================
        public override void Start()
        {
            Curve.Changed += SendEventsIfMathIsNotCreated;
        }

        public override void OnDestroy()
        {
            if (math != null)
            {
                math.Changed -= MathWasChanged;
                math.ChangeRequested -= MathOnChangeRequested;
                math.Dispose();
            }
            ChangedParams -= InitMath;
        }

        //===============================================================================================
        //                                                    Public Functions
        //===============================================================================================

        /// <summary>By default math is created only if it's required. Call this method to ensure math is created </summary>
        public void EnsureMathIsCreated()
        {
#pragma warning disable 0168
            //ensure math exists
            var temp = Math;
#pragma warning restore 0168
        }

        /// <summary>Recalculate internal caches. this is a costly operation </summary>
        /// <param name="force">Ignore all checks and force recalculation at any rate</param>
        public void Recalculate(bool force = false)
        {
            Math.Recalculate(force);
        }


        //================================ Misc
        /// <summary>Is given field is calculated?</summary>
        public bool IsCalculated(BGCurveBaseMath.Field field)
        {
            var math = Math;
            return math != null && math.IsCalculated(field);
        }

        /// <summary>Clamp distance to valid range</summary>
        public float ClampDistance(float distance)
        {
            var math = Math;
            return distance < 0 ? 0 : (distance > math.GetDistance() ? math.GetDistance() : distance);
        }


        //================================ Total Distance
        //see interface for comments
        public float GetDistance(int pointIndex = -1)
        {
            return Math.GetDistance(pointIndex);
        }


        //================================ Calc by ratio
        //see interface for comments
        public Vector3 CalcByDistanceRatio(BGCurveBaseMath.Field field, float ratio, bool useLocal = false)
        {
            return Math.CalcByDistanceRatio(field, ratio, useLocal);
        }

        //see interface for comments
        public Vector3 CalcByDistanceRatio(float distanceRatio, out Vector3 tangent, bool useLocal = false)
        {
            return Math.CalcByDistanceRatio(distanceRatio, out tangent, useLocal);
        }

        //see interface for comments
        public Vector3 CalcPositionByDistanceRatio(float ratio, bool useLocal = false)
        {
            return Math.CalcByDistanceRatio(BGCurveBaseMath.Field.Position, ratio, useLocal);
        }

        //see interface for comments
        public Vector3 CalcTangentByDistanceRatio(float ratio, bool useLocal = false)
        {
            return Math.CalcByDistanceRatio(BGCurveBaseMath.Field.Tangent, ratio, useLocal);
        }

        //see interface for comments
        public Vector3 CalcPositionAndTangentByDistanceRatio(float distanceRatio, out Vector3 tangent, bool useLocal = false)
        {
            return Math.CalcPositionAndTangentByDistanceRatio(distanceRatio, out tangent, useLocal);
        }

        //================================ Calc by distance
        //see interface for comments
        public Vector3 CalcByDistance(BGCurveBaseMath.Field field, float distance, bool useLocal = false)
        {
            return Math.CalcByDistance(field, distance, useLocal);
        }

        //see interface for comments
        public Vector3 CalcByDistance(float distance, out Vector3 tangent, bool useLocal = false)
        {
            return Math.CalcByDistance(distance, out tangent, useLocal);
        }

        //see interface for comments
        public Vector3 CalcPositionByDistance(float distance, bool useLocal = false)
        {
            return Math.CalcByDistance(BGCurveBaseMath.Field.Position, distance, useLocal);
        }

        //see interface for comments
        public Vector3 CalcTangentByDistance(float distance, bool useLocal = false)
        {
            return Math.CalcByDistance(BGCurveBaseMath.Field.Tangent, distance, useLocal);
        }

        //see interface for comments
        public Vector3 CalcPositionAndTangentByDistance(float distance, out Vector3 tangent, bool useLocal = false)
        {
            return Math.CalcPositionAndTangentByDistance(distance, out tangent, useLocal);
        }

        /// <summary>Access calculated section's data by index</summary>
        public BGCurveBaseMath.SectionInfo this[int i]
        {
            get { return Math[i]; }
        }


        //================================ Calc by closest point
        //see interface for comments
        public Vector3 CalcPositionByClosestPoint(Vector3 point, out float distance, out Vector3 tangent, bool skipSectionsOptimization = false, bool skipPointsOptimization = false)
        {
            return Math.CalcPositionByClosestPoint(point, out distance, out tangent, skipSectionsOptimization, skipPointsOptimization);
        }

        //see interface for comments
        public Vector3 CalcPositionByClosestPoint(Vector3 point, out float distance, bool skipSectionsOptimization = false, bool skipPointsOptimization = false)
        {
            return Math.CalcPositionByClosestPoint(point, out distance, skipSectionsOptimization, skipPointsOptimization);
        }

        //see interface for comments
        public Vector3 CalcPositionByClosestPoint(Vector3 point, bool skipSectionsOptimization = false, bool skipPointsOptimization = false)
        {
            return Math.CalcPositionByClosestPoint(point, skipSectionsOptimization, skipPointsOptimization);
        }

        //================================ Calc section index
        //see interface for comments
        public int CalcSectionIndexByDistance(float distance)
        {
            return Math.CalcSectionIndexByDistance(distance);
        }

        //see interface for comments
        public int CalcSectionIndexByDistanceRatio(float distanceRatio)
        {
            return Math.CalcSectionIndexByDistanceRatio(distanceRatio);
        }


        //===============================================================================================
        //                                                    Private Functions
        //===============================================================================================
        //we do not have to create a math implementation until it's needed, but we have to trace spline's changes and fire events
        private void SendEventsIfMathIsNotCreated(object sender, BGCurveChangedArgs e)
        {
            //no more need to trace changes, since math is tracing it by itself
            if (math != null) Curve.Changed -= SendEventsIfMathIsNotCreated;
            //implementation is not created yet, but we need to fire events to indicate math 'would be' changed if it is created
            else MathWasChanged(sender, e);
        }

        //init math with current params
        private void InitMath(object sender, EventArgs e)
        {
            //new config
            var config = mathType == MathTypeEnum.Adaptive
                ? new BGCurveAdaptiveMath.ConfigAdaptive(fields) {Tolerance = tolerance}
                : new BGCurveBaseMath.Config(fields) {Parts = sectionParts};

            config.UsePointPositionsToCalcTangents = usePositionToCalculateTangents;
            config.OptimizeStraightLines = optimizeStraightLines;
            config.Fields = fields;

            if (updateMode != UpdateModeEnum.Always && Application.isPlaying)
            {
                switch (updateMode)
                {
                    case UpdateModeEnum.AabbVisible:
                        InitAabbVisibleBefore(config);
                        break;
                    case UpdateModeEnum.RendererVisible:
                        InitRendererVisible(config);
                        break;
                }
            }

            if (NewMathRequired)
            {
                //we need to create new math object 

                var mathWasNull = math == null;

                if (mathWasNull) ChangedParams += InitMath;
                else
                {
                    math.ChangeRequested -= MathOnChangeRequested;
                    math.Changed -= MathWasChanged;
                    math.Dispose();
                }

                switch (MathType)
                {
                    case MathTypeEnum.Base:
                        math = new BGCurveBaseMath(Curve, config);
                        break;
                    default:
                        //                    case MathTypeEnum.Adaptive:
                        math = new BGCurveAdaptiveMath(Curve, (BGCurveAdaptiveMath.ConfigAdaptive) config);
                        break;
                }


                math.Changed += MathWasChanged;

                if (!mathWasNull) MathWasChanged(this, null);
            }
            else
            {
                //we can reuse existing math object

                math.ChangeRequested -= MathOnChangeRequested;
                //reinit math with new params (it will be recalculated as result)
                math.Init(config);
            }

            //init AABB
            if (updateMode == UpdateModeEnum.AabbVisible) InitAabbVisibleAfter();
        }

        //before math was created (or inited)
        private void InitAabbVisibleBefore(BGCurveBaseMath.Config config)
        {
            if (!Application.isPlaying) return;

            if (meshFilter != null) Destroy(meshFilter.gameObject);

            var aabbGameObject = new GameObject("AabbBox");
            aabbGameObject.transform.parent = transform;
            var renderer = aabbGameObject.AddComponent<MeshRenderer>();
            meshFilter = aabbGameObject.AddComponent<MeshFilter>();
            meshFilter.mesh = new Mesh();
            InitVisibilityCheck(config, renderer);

            MathOnChangeRequested(this, null);
        }

        //after math was created (or inited)
        private void InitAabbVisibleAfter()
        {
            if (!Application.isPlaying) return;

            math.ChangeRequested += MathOnChangeRequested;
        }

        //math received change request (but not changed yet). This callback is called only if  updateMode=UpdateModeEnum.AabbVisible
        private void MathOnChangeRequested(object sender, EventArgs eventArgs)
        {
            if (!Application.isPlaying) return;

            // if visibilityCheck is not inited or the mesh is already visible, we dont need to recalculate the mesh
            if (visibilityCheck == null || visibilityCheck.Visible) return;

            // here we should construct a mesh with 2 points (min to max), so Unity calls its  OnBecameVisible OnBecameInvisible callbacks
            // I'm not sure how fast it is, but it (probably) should be faster than calculating it manually without this mesh
            var points = Curve.Points;
            var mesh = meshFilter.sharedMesh;

            switch (points.Length)
            {
                case 0:
                    mesh.vertices = EmptyVertices;
                    break;
                case 1:
                    vertices[0] = points[0].PositionWorld;
                    vertices[1] = vertices[0];
                    mesh.vertices = vertices;
                    break;
                default:
                    var matrix = transform.localToWorldMatrix;
                    var firstPoint = points[0].PositionWorld;
                    var pointsCount = points.Length;
                    var lastPointIndex = pointsCount - 1;
                    var closed = Curve.Closed;

                    var min = firstPoint;
                    var max = firstPoint;
                    for (var i = 0; i < pointsCount; i++)
                    {
                        var point = points[i];
                        var posLocal = point.PositionLocal;
                        var posWorld = matrix.MultiplyPoint(posLocal);

                        //copy/pasted for the sake of performance
                        if (min.x > posWorld.x) min.x = posWorld.x;
                        if (min.y > posWorld.y) min.y = posWorld.y;
                        if (min.z > posWorld.z) min.z = posWorld.z;

                        if (max.x < posWorld.x) max.x = posWorld.x;
                        if (max.y < posWorld.y) max.y = posWorld.y;
                        if (max.z < posWorld.z) max.z = posWorld.z;

                        if (point.ControlType != BGCurvePoint.ControlTypeEnum.Absent)
                        {
                            if (closed || i != 0)
                            {
                                var controlWorld = matrix.MultiplyPoint(point.ControlFirstLocal + posLocal);

                                //copy/pasted for the sake of performance
                                if (min.x > controlWorld.x) min.x = controlWorld.x;
                                if (min.y > controlWorld.y) min.y = controlWorld.y;
                                if (min.z > controlWorld.z) min.z = controlWorld.z;

                                if (max.x < controlWorld.x) max.x = controlWorld.x;
                                if (max.y < controlWorld.y) max.y = controlWorld.y;
                                if (max.z < controlWorld.z) max.z = controlWorld.z;
                            }
                            if (closed || i != lastPointIndex)
                            {
                                var controlWorld = matrix.MultiplyPoint(point.ControlSecondLocal + posLocal);

                                //copy/pasted for the sake of performance
                                if (min.x > controlWorld.x) min.x = controlWorld.x;
                                if (min.y > controlWorld.y) min.y = controlWorld.y;
                                if (min.z > controlWorld.z) min.z = controlWorld.z;

                                if (max.x < controlWorld.x) max.x = controlWorld.x;
                                if (max.y < controlWorld.y) max.y = controlWorld.y;
                                if (max.z < controlWorld.z) max.z = controlWorld.z;
                            }
                        }
                    }

                    vertices[0] = min;
                    vertices[1] = max;

                    mesh.vertices = vertices;
                    break;
            }
        }


        //setup renderer visible option for update mode
        private void InitRendererVisible(BGCurveBaseMath.Config config)
        {
            InitVisibilityCheck(config, RendererForUpdateCheck);
        }

        //attach visibility check component to target GameObject (the one, the renderer is attached to)
        private void InitVisibilityCheck(BGCurveBaseMath.Config config, Renderer renderer)
        {
            if (visibilityCheck != null)
            {
                visibilityCheck.BecameVisible -= BecameVisible;
                Destroy(visibilityCheck);
            }

            if (renderer == null) return;

            visibilityCheck = renderer.gameObject.AddComponent<VisibilityCheck>();

            visibilityCheck.BecameVisible += BecameVisible;

            config.ShouldUpdate = () => visibilityCheck.Visible;
        }

        //became visible callback for use with VisibilityCheck Component
        private void BecameVisible(object sender, EventArgs e)
        {
            math.Configuration.FireUpdate();
        }

        //math was changed callback
        private void MathWasChanged(object sender, EventArgs e)
        {
            if (ChangedMath != null) ChangedMath(this, null);

            if (mathChangedEvent.GetPersistentEventCount() > 0) mathChangedEvent.Invoke();
        }

        //===============================================================================================
        //                                                    Private classes 
        //===============================================================================================
        //it's used for visibility checks
        private sealed class VisibilityCheck : MonoBehaviour
        {
            //Fired when became visible
            public event EventHandler BecameVisible;

            /// <summary> Is currently visible </summary>
            public bool Visible { get; private set; }

            //================================== Unity Callbacks
            private void OnBecameVisible()
            {
                Visible = true;
                if (BecameVisible != null) BecameVisible(this, null);
            }

            private void OnBecameInvisible()
            {
                Visible = false;
            }
        }

        //===============================================================================================
        //                                                    Unity Persistent Event
        //===============================================================================================
        [Serializable]
        public class MathChangedEvent : UnityEvent
        {
        }
    }
}