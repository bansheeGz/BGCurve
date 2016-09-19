using System;
using UnityEngine;
using BansheeGz.BGSpline.Curve;

namespace BansheeGz.BGSpline.Components
{
    /// <summary> Math solver for the curve.</summary>
    [HelpURL("http://www.bansheegz.com/BGCurve/Cc/BGCcMath")]
    [DisallowMultipleComponent]
    [
        CcDescriptor(
            Description = "Math solver for the curve (position, tangent and total distance). With this component you can use math functions.",
            Name = "Math",
            Image = "Assets/BansheeGz/BGCurve/Icons/Components/BGCcMath123.png")
    ]
    [AddComponentMenu("BansheeGz/BGCurve/Components/BGCcMath", 0)]
    public class BGCcMath : BGCc
    {
        private const int PartsMax = 100;

        private static readonly Vector3[] EmptyVertices = new Vector3[0];


/*
        public enum MathTypeEnum
        {
            Base = 0,
            AdaptiveSplit = 1
        }
*/

        public enum UpdateModeEnum
        {
            Always = 0,
            AabbVisible = 1,
            RendererVisible = 2
        }

        /// <summary>if underlying math changed</summary>
        public event EventHandler ChangedMath;


/*
        [SerializeField] [Tooltip("Math type to use.\r\n" +
                                  "Base - robust, poorly optimized. Uses even sections to approximate data;\r\n " +
                                  "AdaptiveSplit -Optimized, but (currently) may fail in some rare cases with twisted curve segments. Uses adaptive split algorithm.")] private MathTypeEnum mathType;
*/

        [SerializeField] [Tooltip("Which fields you want to use.")] private BGCurveBaseMath.Fields fields = BGCurveBaseMath.Fields.Position;

        [SerializeField] [Tooltip("The number of parts (equal) for each section.")] [Range(1, PartsMax)] private int sectionParts = 30;

        [SerializeField] [Tooltip("Points position will be used for tangent calculation. This can gain some performance")] private bool usePositionToCalculateTangents;

        [SerializeField] [Tooltip("Use only 2 points for straight lines. Tangents may be calculated slightly different")] private bool optimizeStraightLines;

        [SerializeField] [Tooltip("Updating math takes some resources. You can fine-tune in which cases math is updated."
                                  + "\r\n1) Always- always update"
                                  + "\r\n2) AabbVisible- update only if AABB (Axis Aligned Bounding Box) around points and controls is visible"
                                  + "\r\n3) RendererVisible- update only if some renderer is visible"
            )] private UpdateModeEnum updateMode;

        [SerializeField] [Tooltip("Renderer to check for updating math. Math will be updated only if renderer is visible")] private Renderer rendererForUpdateCheck;

/*
        public MathTypeEnum MathType
        {
            get { return mathType; }
        }
*/

        public override string Error
        {
            get
            {
                return updateMode == UpdateModeEnum.RendererVisible && RendererForUpdateCheck == null
                    ? "Update mode is set to " + updateMode + ", however the RendererForUpdateCheck is null"
                    : null;
            }
        }

        private BGCurveBaseMath math;
        
        private VisibilityCheck visibilityCheck;
        private MeshFilter meshFilter;
        private readonly Vector3[] vertices = new Vector3[2];

        public BGCurveBaseMath Math
        {
            get
            {
                if (math == null) InitMath(null, null);
                return math;
            }
        }

        public BGCurveBaseMath.Fields Fields
        {
            get { return fields; }
            set { ParamChanged(ref fields, value); }
        }


        public int SectionParts
        {
            get { return Mathf.Clamp(sectionParts, 1, PartsMax); }
            set { ParamChanged(ref sectionParts, Mathf.Clamp(value, 1, PartsMax)); }
        }

        public bool UsePositionToCalculateTangents
        {
            get { return usePositionToCalculateTangents; }
            set { ParamChanged(ref usePositionToCalculateTangents, value); }
        }

        public bool OptimizeStraightLines
        {
            get { return optimizeStraightLines; }
            set { ParamChanged(ref optimizeStraightLines, value); }
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


        private void InitMath(object sender, EventArgs e)
        {
            //New math
            var config = new BGCurveBaseMath.Config(fields)
            {
                Parts = sectionParts,
                UsePointPositionsToCalcTangents = usePositionToCalculateTangents,
                OptimizeStraightLines = optimizeStraightLines,
                Fields = fields
            };

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

            if (math == null)
            {
/*
                math = mathType == MathTypeEnum.Base
                    ? new BGCurveBaseMath(Curve, config)
                    : new BGCurveAdaptiveSplitMath(Curve, config);
*/
                math = new BGCurveBaseMath(Curve, config);

                ChangedParams += InitMath;
                math.Changed += MathWasChanged;
            }
            else
            {
                math.ChangeRequested -= MathOnChangeRequested;
                math.Init(config);
            }

            if(updateMode==UpdateModeEnum.AabbVisible) InitAabbVisibleAfter(config);
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
            MathOnChangeRequested(null, null);
            InitVisibilityCheck(config, renderer);
        }

        //after math was created (or inited)
        private void InitAabbVisibleAfter(BGCurveBaseMath.Config config)
        {
            if (!Application.isPlaying) return;

            math.ChangeRequested += MathOnChangeRequested;
        }

        private void MathOnChangeRequested(object sender, EventArgs eventArgs)
        {
            if (!Application.isPlaying) return;

            if (visibilityCheck==null || visibilityCheck.Visible) return;

            var points = Curve.Points;
            var mesh = meshFilter.mesh;

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

        private void InitRendererVisible(BGCurveBaseMath.Config config)
        {
            InitVisibilityCheck(config, RendererForUpdateCheck);
        }

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

        private void BecameVisible(object sender, EventArgs e)
        {
            math.Configuration.FireUpdate();
        }


        private void MathWasChanged(object sender, EventArgs e)
        {
            if (ChangedMath != null) ChangedMath(this, null);
        }

        /// <summary>Recalculate internal caches. this is a costly operation </summary>
        /// <param name="force">Ignore all checks and force recalculation at any rate</param>
        public void Recalculate(bool force = false)
        {
            Math.Recalculate(force);
        }

        //Unity callback
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

        //================================ Misc
        public bool IsCalculated(BGCurveBaseMath.Field field)
        {
            var math = Math;
            return math != null && math.IsCalculated(field);
        }

        public float ClampDistance(float distance)
        {
            var math = Math;
            return distance < 0 ? 0 : (distance > math.GetDistance() ? math.GetDistance() : distance);
        }


        //================================ Total Distance
        public float GetDistance()
        {
            return Math.GetDistance();
        }


        //================================ Calc by ratio
        /// <summary> Calculate curve's field (position or tangent) by distance ratio [Range(0,1)]. Using local coordinates is significantly slower. </summary>
        public Vector3 CalcByDistanceRatio(BGCurveBaseMath.Field field, float ratio, bool useLocal = false)
        {
            return Math.CalcByDistanceRatio(field, ratio, useLocal);
        }

        /// <summary> Calculate curve's position by distance ratio [Range(0,1)]. Using local coordinates is significantly slower. </summary>
        public Vector3 CalcPositionByDistanceRatio(float ratio, bool useLocal = false)
        {
            return Math.CalcByDistanceRatio(BGCurveBaseMath.Field.Position, ratio, useLocal);
        }

        /// <summary> Calculate curve's tangent by distance ratio [Range(0,1)]. Using local coordinates is significantly slower. </summary>
        public Vector3 CalcTangentByDistanceRatio(float ratio, bool useLocal = false)
        {
            return Math.CalcByDistanceRatio(BGCurveBaseMath.Field.Tangent, ratio, useLocal);
        }

        /// <summary> Calculate curve's point and tangent by distance ratio [Range(0,1)]. Using local coordinates is significantly slower. </summary>
        public Vector3 CalcPositionAndTangentByDistanceRatio(float distanceRatio, out Vector3 tangent, bool useLocal = false)
        {
            return Math.CalcPositionAndTangentByDistanceRatio(distanceRatio, out tangent, useLocal);
        }

        //================================ Calc by distance
        /// <summary> Calculate curve's position by distance. Using local coordinates is significantly slower. </summary>
        public Vector3 CalcByDistance(BGCurveBaseMath.Field field, float distance, bool useLocal = false)
        {
            return Math.CalcByDistance(field, distance, useLocal);
        }

        /// <summary> Calculate curve's position by distance. Using local coordinates is significantly slower. </summary>
        public Vector3 CalcPositionByDistance(float distance, bool useLocal = false)
        {
            return Math.CalcByDistance(BGCurveBaseMath.Field.Position, distance, useLocal);
        }

        /// <summary> Calculate curve's tangent by distance. Using local coordinates is significantly slower. </summary>
        public Vector3 CalcTangentByDistance(float distance, bool useLocal = false)
        {
            return Math.CalcByDistance(BGCurveBaseMath.Field.Tangent, distance, useLocal);
        }

        /// <summary> Calculate curve's point and tangent by distance. Using local coordinates is significantly slower. </summary>
        public Vector3 CalcPositionAndTangentByDistance(float distance, out Vector3 tangent, bool useLocal = false)
        {
            return Math.CalcPositionAndTangentByDistance(distance, out tangent, useLocal);
        }


        //================================ Calc by closest point
        /// <summary> Calculate curve's world position, distance and tangent, which is closest to a given point.</summary>
        public Vector3 CalcPositionByClosestPoint(Vector3 point, out float distance, out Vector3 tangent, bool skipSectionsOptimization = false, bool skipPointsOptimization = false)
        {
            return Math.CalcPositionByClosestPoint(point, out distance, out tangent, skipSectionsOptimization, skipPointsOptimization);
        }

        /// <summary> Calculate curve's world position and distance, which is closest to given a point.</summary>
        public Vector3 CalcPositionByClosestPoint(Vector3 point, out float distance, bool skipSectionsOptimization = false, bool skipPointsOptimization = false)
        {
            return Math.CalcPositionByClosestPoint(point, out distance, skipSectionsOptimization, skipPointsOptimization);
        }

        /// <summary> Calculate curve's world position, which is closest to a given point.</summary>
        public Vector3 CalcPositionByClosestPoint(Vector3 point, bool skipSectionsOptimization = false, bool skipPointsOptimization = false)
        {
            return Math.CalcPositionByClosestPoint(point, skipSectionsOptimization, skipPointsOptimization);
        }


        //used for visibility checks
        private class VisibilityCheck : MonoBehaviour
        {
            public event EventHandler BecameVisible;

            public bool Visible { get; private set; }

            private void OnBecameVisible()
            {
                Visible = true;
                if (BecameVisible != null) BecameVisible(this,null);
            }

            private void OnBecameInvisible()
            {
                Visible = false;
            }

        }
    }
}