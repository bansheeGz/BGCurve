using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using BansheeGz.BGSpline.Curve;

namespace BansheeGz.BGSpline.Editor
{
    public static class BGNewPointPositionManager
    {
        //multiply tangent by this constant for new points, inserted after all points.
        private const float DistanceToControlMultiplier = .2f;

        //multiply tangent by this constant for points, inserted between 2 existing points.
        private const float DistanceToTangentMultiplier = .05f;

        //multiply tangent by this constant for points, which positions can not be derived
        private const float DistanceToTangentNoDataMultiplier = 1.5f;

        private static BGCurvePoint point;

        public static BGCurvePoint CreatePoint(Vector3 position, BGCurve curve, BGCurvePoint.ControlTypeEnum controlType, int parts, bool ensureNew)
        {
            float distanceToPreviousPoint;
            float distanceToNextPoint;
            return CreatePoint(position, curve, controlType, parts, out distanceToPreviousPoint, out distanceToNextPoint, ensureNew);
        }

        public static BGCurvePoint CreatePoint(Vector3 position, BGCurve curve, BGCurvePoint.ControlTypeEnum controlType, int parts, out float distanceToPreviousPoint, out float distanceToNextPoint,
            bool ensureNew)
        {
            distanceToPreviousPoint = -1;
            distanceToNextPoint = -1;

            if (curve.PointsCount == 0)
            {
                //first point
                Vector3 control;
                switch (curve.Mode2D)
                {
                    case BGCurve.Mode2DEnum.YZ:
                        control = Vector3.forward;
                        break;
                    default:
                        // BGCurve.Mode2DEnum.XY:
                        // BGCurve.Mode2DEnum.Off:
                        // BGCurve.Mode2DEnum.XZ:
                        control = Vector3.right;
                        break;
                }
                return curve.CreatePointFromLocalPosition(curve.ToLocal(position), controlType, control, -control);
            }

            parts = Mathf.Clamp(parts, 1, 50);

            //we no need no events (maybe check if point was actually added to a curve for events firing?)
            var oldSuppress = curve.SupressEvents;
            curve.SupressEvents = true;

            //create a point with no controls first
            BGCurvePoint newPoint;
            if (ensureNew)
            {
                newPoint = curve.CreatePointFromWorldPosition(position, BGCurvePoint.ControlTypeEnum.Absent);
            }
            else
            {
                if (point == null || point.Curve != curve) point = curve.CreatePointFromWorldPosition(position, BGCurvePoint.ControlTypeEnum.Absent);
                newPoint = point;
                newPoint.PositionWorld = position;
                newPoint.ControlFirstLocal = Vector3.zero;
                newPoint.ControlSecondLocal = Vector3.zero;
            }

            if (curve.Mode2DOn) curve.Apply2D(newPoint);

            //adjacent points
            var previousPoint = curve[curve.PointsCount - 1];
            var nextPoint = curve.Closed ? curve[0] : null;

            //direction
            var tangent = BGEditorUtility.CalculateTangent(newPoint, previousPoint, nextPoint, 1/(float) parts);
            if (tangent.sqrMagnitude < 0.0001f)
            {
                //whatever
                switch (curve.Mode2D)
                {
                    case BGCurve.Mode2DEnum.Off:
                    case BGCurve.Mode2DEnum.XY:
                    case BGCurve.Mode2DEnum.XZ:
                        tangent = Vector3.right;
                        break;
                    case BGCurve.Mode2DEnum.YZ:
                        tangent = Vector3.up;
                        break;
                }
            }

            //length
            distanceToPreviousPoint = BGEditorUtility.CalculateDistance(previousPoint, newPoint, parts);
            float minDistance;
            if (nextPoint != null)
            {
                distanceToNextPoint = BGEditorUtility.CalculateDistance(newPoint, nextPoint, parts);
                minDistance = Math.Min(distanceToPreviousPoint, distanceToNextPoint);
            }
            else
            {
                minDistance = distanceToPreviousPoint;
            }
            var length = minDistance*DistanceToControlMultiplier;


            //we need local tangent for controls
            tangent = curve.ToLocalDirection(tangent);


            newPoint.ControlSecondLocal = tangent*length;

            newPoint.ControlFirstLocal = -newPoint.ControlSecondLocal;


            newPoint.ControlType = controlType;

            curve.SupressEvents = oldSuppress;
            return newPoint;
        }

        public static BGCurvePoint InsertBefore(BGCurve curve, int index, BGCurvePoint.ControlTypeEnum controlType, int parts)
        {
            var newPoint = CreatePointByPointsCount(curve, controlType);
            if (newPoint != null) return newPoint;

            if (index == 0 && !curve.Closed)
                return InsertNoData(curve, controlType, BGEditorUtility.CalculateTangent(curve[0], curve[1], 0f), curve[0].ControlFirstLocal, curve[0].PositionWorld, false);

            return CreatePointBetween(curve, index == 0 ? curve[curve.PointsCount - 1] : curve[index - 1], curve[index], parts, controlType);
        }


        public static BGCurvePoint InsertAfter(BGCurve curve, int index, BGCurvePoint.ControlTypeEnum controlType, int parts)
        {
            var newPoint = CreatePointByPointsCount(curve, controlType);
            if (newPoint != null) return newPoint;

            var pointsCount = curve.PointsCount;
            if (index == pointsCount - 1 && !curve.Closed)
            {
                var lastPoint = curve[pointsCount - 1];
                return InsertNoData(curve, controlType, BGEditorUtility.CalculateTangent(curve[pointsCount - 2], lastPoint, 1f), lastPoint.ControlSecondLocal, lastPoint.PositionWorld, true);
            }

            return CreatePointBetween(curve, curve[index], index == pointsCount - 1 ? curve[0] : curve[index + 1], parts, controlType);
        }

        public static BGCurvePoint CreatePointBetween(BGCurve curve, BGCurvePointI previousPoint, BGCurvePointI nextPoint, int parts, BGCurvePoint.ControlTypeEnum controlType, 
            Vector3 position, Vector3 tangent)
        {
            var scaledTangent = tangent * DistanceToTangentMultiplier * BGEditorUtility.CalculateDistance(previousPoint, nextPoint, parts);

            return curve.CreatePointFromLocalPosition(curve.ToLocal(position), controlType, curve.ToLocalDirection(-scaledTangent), curve.ToLocalDirection(scaledTangent));
        }


        private static BGCurvePoint CreatePointByPointsCount(BGCurve curve, BGCurvePoint.ControlTypeEnum controlType)
        {
            var pointsCount = curve.PointsCount;

            switch (pointsCount)
            {
                case 0:
                    throw new UnityException("You can not use this method with no points on the curve. pointsCount==0");
                case 1:
                    return curve.CreatePointFromLocalPosition(Vector3.forward, controlType, Vector3.right, Vector3.left);
            }
            return null;
        }

        private static BGCurvePoint InsertNoData(BGCurve curve, BGCurvePoint.ControlTypeEnum controlType, Vector3 tangent, Vector3 control, Vector3 positionWorld, bool inverseTangent)
        {
            var controlMagnitude = control.magnitude;
            if (controlMagnitude < 1) controlMagnitude = 1;

            var pos = positionWorld - tangent*controlMagnitude*DistanceToTangentNoDataMultiplier*(inverseTangent ? -1 : 1);

            return curve.CreatePointFromWorldPosition(pos, controlType, pos - tangent, pos + tangent);
        }

        private static BGCurvePoint CreatePointBetween(BGCurve curve, BGCurvePointI previousPoint, BGCurvePointI nextPoint, int parts, BGCurvePoint.ControlTypeEnum controlType)
        {
            var newPos = BGEditorUtility.CalculatePosition(previousPoint, nextPoint, .5f);
            var tangent = BGEditorUtility.CalculateTangent(previousPoint, nextPoint, .5f);

            return CreatePointBetween(curve, previousPoint, nextPoint, parts, controlType, newPos, tangent);
        }

    }
}