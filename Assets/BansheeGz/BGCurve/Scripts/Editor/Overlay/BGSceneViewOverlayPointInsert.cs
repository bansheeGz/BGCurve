using UnityEngine;
using System.Collections.Generic;
using BansheeGz.BGSpline.Curve;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
    public class BGSceneViewOverlayPointInsert : BGSceneViewOverlayPointAdd
    {
        private const float DistanceToleranceSquared = .008f;
        private static readonly float DistanceTolerance = Mathf.Sqrt(DistanceToleranceSquared) * 2;

        private static readonly Color32 PointersColor = Color.red;

        private static readonly List<int> SectionIndexes = new List<int>();
        private Vector3 intersectPosition;
        private float splineIntersectionDistance;
        private int pointIndex;

        public BGSceneViewOverlayPointInsert(BGSceneViewOverlay overlay) : base(overlay)
        {
        }

        protected override bool VisualizingSections
        {
            get { return false; }
        }

        protected override bool ShowingDistance
        {
            get { return false; }
        }

        public override string Name
        {
            get { return "Insert Point"; }
        }

        protected override BGCurvePoint CreatePointForPreview(Vector3 position, BGCurve curve, out float toLast, out float toFirst, BGCurveSettings settings)
        {
            toLast = toFirst = 0;
            return CreatePoint(curve, settings);
        }

        protected override void AddPoint(BGCurve curve, Vector3 intersectionPosition, BGCurveSettings settings)
        {
            BGCurveEditor.AddPoint(curve, CreatePoint(curve, settings), pointIndex + 1);
        }

        private BGCurvePoint CreatePoint(BGCurve curve, BGCurveSettings settings)
        {
            var math = overlay.Editor.Editor.Math;

            var from = curve[pointIndex];
            var to = pointIndex == curve.PointsCount -1 ? curve[0] : curve[pointIndex + 1];
            var ratio = (splineIntersectionDistance - math[pointIndex].DistanceFromStartToOrigin)/math[pointIndex].Distance;
            var tangent = BGEditorUtility.CalculateTangent(@from, to, ratio);

            var point = BGNewPointPositionManager.CreatePointBetween(curve, @from, to, settings.Sections, settings.ControlType, intersectPosition, tangent);
            return point;
        }

        protected override void Animate(BGTransition.SwayTransition swayTransition, Vector3 point, float distanceToCamera, Plane plane)
        {
            var verts = GetVertsByPlaneAndDistance(new Vector3(swayTransition.Value, swayTransition.Value, swayTransition.Value), point, distanceToCamera, plane);

            var size = swayTransition.Value*ScalePreviewPoint*distanceToCamera/5;

            BGEditorUtility.SwapHandlesColor(PointersColor, () =>
            {
	            foreach (var position in verts)
	            {
#if UNITY_5_6_OR_NEWER
				Handles.ConeHandleCap(0, position, Quaternion.LookRotation(point - position), size, EventType.Repaint);
#else
				Handles.ConeCap(0, position, Quaternion.LookRotation(point - position), size);
#endif
	            }
            });
        }

        protected override void Cast(Event @event, Ray ray, out Vector3 position, out string error, out Plane plane)
        {
            position = intersectPosition;
            error = null;
            plane = new Plane(ray.direction.normalized, position);
        }

        protected override bool Comply(Event currentEvent)
        {
            //comply is called at the very beginning, so we calculate required data here
            return CalculateIntersection(HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition));
        }

        private bool CalculateIntersection(Ray ray)
        {
            splineIntersectionDistance = -1;
            var curve = overlay.Editor.Curve;
            var pointsCount = curve.PointsCount;
            if (pointsCount < 2) return false;

            var math = overlay.Editor.Editor.Math;

            // bbox intersection check
            SectionIndexes.Clear();
            for (var i = 0; i < math.SectionsCount; i++)
            {
                var bbox = math.GetBoundingBox(i, math[i]);
                var bboxExtents = bbox.extents;
                if (bboxExtents.x < DistanceTolerance) bbox.extents = new Vector3(DistanceTolerance, bbox.extents.y, bbox.extents.z);
                if (bboxExtents.y < DistanceTolerance) bbox.extents = new Vector3(bbox.extents.x, DistanceTolerance, bbox.extents.z);
                if (bboxExtents.z < DistanceTolerance) bbox.extents = new Vector3(bbox.extents.x, bbox.extents.y, DistanceTolerance);

                if (bbox.IntersectRay(ray)) SectionIndexes.Add(i);
            }


            if (SectionIndexes.Count == 0) return false;

            // line to line distance
            var minDistanceSqrt = float.MaxValue;
            foreach (var sectionIndex in SectionIndexes)
            {
                var section = math[sectionIndex];
                var points = section.Points;
                var sectionPointsCount = section.PointsCount;
                for (var j = 0; j < sectionPointsCount - 1; j++)
                {
                    var from = points[j];
                    var to = points[j + 1];

                    Vector3 point1, point2;
                    var toFrom = (to.Position - @from.Position);

                    //closest points on 2 lines
                    if (!ClosestPointsOnTwoLines(out point1, out point2, @from.Position, toFrom.normalized, ray.origin, ray.direction)) continue;

                    //ensure the point lay on the segment of the line
                    if (PointOnWhichSideOfLineSegment(@from.Position, to.Position, ProjectPointOnLine(@from.Position, toFrom.normalized, point1)) != 0) continue;

                    var sqrtDistance = Vector3.SqrMagnitude(point2 - point1);
                    if (sqrtDistance > DistanceToleranceSquared || sqrtDistance > minDistanceSqrt) continue;

                    minDistanceSqrt = sqrtDistance;
                    splineIntersectionDistance = @from.DistanceToSectionStart + section.DistanceFromStartToOrigin + Vector3.Distance(@from.Position, point1);
                    intersectPosition = point1;
                    pointIndex = sectionIndex;
                }
            }
            SectionIndexes.Clear();
            return splineIntersectionDistance >= 0;
        }


        // the original code:  http://wiki.unity3d.com/index.php/3d_Math_functions
        //Two non-parallel lines which may or may not touch each other have a point on each line which are closest
        //to each other. This function finds those two points. If the lines are not parallel, the function 
        //outputs true, otherwise false.
        private static bool ClosestPointsOnTwoLines(out Vector3 closestPointLine1, out Vector3 closestPointLine2, Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2)
        {
            closestPointLine1 = Vector3.zero;
            closestPointLine2 = Vector3.zero;

//            float a = Vector3.Dot(lineVec1, lineVec1);
            float b = Vector3.Dot(lineVec1, lineVec2);
//            float e = Vector3.Dot(lineVec2, lineVec2);

            float d = 1 - b*b;

            //lines are not parallel
            if (d != 0.0f)
            {
                Vector3 r = linePoint1 - linePoint2;
                float c = Vector3.Dot(lineVec1, r);
                float f = Vector3.Dot(lineVec2, r);

                float s = (b*f - c)/d;
                float t = (f - c*b)/d;

                closestPointLine1 = linePoint1 + lineVec1*s;
                closestPointLine2 = linePoint2 + lineVec2*t;

                return true;
            }
            return false;
        }

        //This function returns a point which is a projection from a point to a line.
        //The line is regarded infinite. If the line is finite, use ProjectPointOnLineSegment() instead.
        public static Vector3 ProjectPointOnLine(Vector3 linePoint, Vector3 lineVec, Vector3 point)
        {
            //get vector from point on line to point in space
            Vector3 linePointToPoint = point - linePoint;

            float t = Vector3.Dot(linePointToPoint, lineVec);

            return linePoint + lineVec*t;
        }


        //This function finds out on which side of a line segment the point is located.
        //The point is assumed to be on a line created by linePoint1 and linePoint2. If the point is not on
        //the line segment, project it on the line using ProjectPointOnLine() first.
        //Returns 0 if point is on the line segment.
        //Returns 1 if point is outside of the line segment and located on the side of linePoint1.
        //Returns 2 if point is outside of the line segment and located on the side of linePoint2.
        public static int PointOnWhichSideOfLineSegment(Vector3 linePoint1, Vector3 linePoint2, Vector3 point)
        {
            Vector3 lineVec = linePoint2 - linePoint1;
            Vector3 pointVec = point - linePoint1;

            float dot = Vector3.Dot(pointVec, lineVec);

            //point is on side of linePoint2, compared to linePoint1
            if (dot > 0)
            {
                //point is on the line segment
                if (pointVec.magnitude <= lineVec.magnitude)
                {
                    return 0;
                }

                //point is not on the line segment and it is on the side of linePoint2
                return 2;
            }

            //Point is not on side of linePoint2, compared to linePoint1.
            //Point is not on the line segment and it is on the side of linePoint1.
            return 1;
        }
    }
}