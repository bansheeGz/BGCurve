using System;
using UnityEngine;

namespace BansheeGz.BGSpline.Curve
{
    /// <summary>One point data</summary>
    [Serializable]
    public class BGCurvePoint
    {
        #region fields

        //possible point's control types
        public enum ControlTypeEnum
        {
            //no control point
            Absent,
            //2 points, symmetrical to each other
            BezierSymmetrical,
            //2 points, independant
            BezierIndependant
        }

        //helper enum for fields
        public enum FieldEnum
        {
            PositionWorld,
            PositionLocal,
            ControlFirstWorld,
            ControlFirstLocal,
            ControlSecondWorld,
            ControlSecondLocal
        }


        //control type
        [SerializeField] private ControlTypeEnum controlType;

        //relative to curve position
        [SerializeField] private Vector3 positionLocal;

        //relative to point position
        [SerializeField] private Vector3 controlFirstLocal;
        [SerializeField] private Vector3 controlSecondLocal;


        //point's curve
        [SerializeField] private BGCurve curve;



        /// <summary>The curve, point's belong to</summary>
        public BGCurve Curve
        {
            get { return curve; }
        }

        // =============================================== Position
        /// <summary>Local position, relative to curve's location</summary>
        public Vector3 PositionLocal
        {
            get { return positionLocal; }
            set
            {
                curve.FireBeforeChange("point's position change");

                if (curve.Mode2D != BGCurve.Mode2DEnum.Off) value = curve.Apply2D(value);

                positionLocal = value;
                curve.FireChange(curve.UseEventsArgs ? new BGCurveChangedArgs(Curve, this, BGCurveChangedArgs.ChangeTypeEnum.Point) : null);
            }
        }

        /// <summary>World absolute position</summary>
        public Vector3 PositionWorld
        {
            get { return curve.transform.TransformPoint(positionLocal); }
            set
            {
                curve.FireBeforeChange("point's position change");
                positionLocal = curve.transform.InverseTransformPoint(value);

                if (curve.Mode2D != BGCurve.Mode2DEnum.Off) positionLocal = curve.Apply2D(positionLocal);

                curve.FireChange(curve.UseEventsArgs ? new BGCurveChangedArgs(Curve, this, BGCurveChangedArgs.ChangeTypeEnum.Point) : null);
            }
        }


        // =============================================== First Handle
        /// <summary>Local position for 1st control (In), relative to point's location</summary>
        public Vector3 ControlFirstLocal
        {
            get { return controlFirstLocal; }
            set
            {
                curve.FireBeforeChange("point's control change");

                if (curve.Mode2D != BGCurve.Mode2DEnum.Off) value = curve.Apply2D(value);

                if (controlType == ControlTypeEnum.BezierSymmetrical) controlSecondLocal = -value;

                controlFirstLocal = value;
                curve.FireChange(curve.UseEventsArgs ? new BGCurveChangedArgs(Curve, this, BGCurveChangedArgs.ChangeTypeEnum.PointControl) : null);
            }
        }

        /// <summary>World position for 1st control (In)</summary>
        public Vector3 ControlFirstWorld
        {
            get { return curve.transform.TransformPoint(new Vector3(positionLocal.x + controlFirstLocal.x, positionLocal.y + controlFirstLocal.y, positionLocal.z + controlFirstLocal.z)); }
            set
            {
                curve.FireBeforeChange("point's control change");

                controlFirstLocal = curve.transform.InverseTransformPoint(value) - PositionLocal;

                if (curve.Mode2D != BGCurve.Mode2DEnum.Off) controlFirstLocal = curve.Apply2D(controlFirstLocal);

                if (controlType == ControlTypeEnum.BezierSymmetrical) controlSecondLocal = -controlFirstLocal;

                curve.FireChange(curve.UseEventsArgs ? new BGCurveChangedArgs(Curve, this, BGCurveChangedArgs.ChangeTypeEnum.PointControl) : null);
            }
        }


        // =============================================== Second Handle
        /// <summary>Local position for 2nd control (Out), relative to point's position</summary>
        public Vector3 ControlSecondLocal
        {
            get { return controlSecondLocal; }
            set
            {
                curve.FireBeforeChange("point's control change");

                if (curve.Mode2D != BGCurve.Mode2DEnum.Off) value = curve.Apply2D(value);

                if (controlType == ControlTypeEnum.BezierSymmetrical) controlFirstLocal = -value;

                controlSecondLocal = value;
                curve.FireChange(curve.UseEventsArgs ? new BGCurveChangedArgs(Curve, this, BGCurveChangedArgs.ChangeTypeEnum.PointControl) : null);
            }
        }

        /// <summary>World position for 2nd control (Out)</summary>
        public Vector3 ControlSecondWorld
        {
            get { return curve.transform.TransformPoint(new Vector3(positionLocal.x + controlSecondLocal.x, positionLocal.y + controlSecondLocal.y, positionLocal.z + controlSecondLocal.z)); }
            set
            {
                curve.FireBeforeChange("point's control change");

                controlSecondLocal = curve.transform.InverseTransformPoint(value) - PositionLocal;


                if (curve.Mode2D != BGCurve.Mode2DEnum.Off) controlSecondLocal = curve.Apply2D(controlSecondLocal);

                if (controlType == ControlTypeEnum.BezierSymmetrical) controlFirstLocal = -controlSecondLocal;

                curve.FireChange(curve.UseEventsArgs ? new BGCurveChangedArgs(Curve, this, BGCurveChangedArgs.ChangeTypeEnum.PointControl) : null);
            }
        }


        // =============================================== Control type
        /// <summary>Control type for the point</summary>
        public ControlTypeEnum ControlType
        {
            get { return controlType; }
            set
            {
                if (controlType == value) return;

                curve.FireBeforeChange("point's control type change");
                controlType = value;

                if (controlType == ControlTypeEnum.BezierSymmetrical) controlSecondLocal = -controlFirstLocal;

                curve.FireChange(curve.UseEventsArgs ? new BGCurveChangedArgs(Curve, this, BGCurveChangedArgs.ChangeTypeEnum.PointControlType) : null);
            }
        }

        #endregion

        #region constructors

        /// <summary> All coordinates are Local by default. positionLocal relative to curve's transform, controls are relative to positionLocal. Set useWorldCoordinates to true to use world coordinates</summary>
        public BGCurvePoint(BGCurve curve, Vector3 position, bool useWorldCoordinates = false) : this(curve, position, ControlTypeEnum.Absent, useWorldCoordinates)
        {
        }

        /// <summary> All coordinates are Local by default. positionLocal relative to curve's transform, controls are relative to positionLocal. Set useWorldCoordinates to true to use world coordinates</summary>
        public BGCurvePoint(BGCurve curve, Vector3 position, ControlTypeEnum controlType, bool useWorldCoordinates = false)
            : this(curve, position, controlType, Vector3.zero, Vector3.zero, useWorldCoordinates)
        {
        }

        /// <summary> All coordinates are Local by default. positionLocal relative to curve's transform, controls are relative to positionLocal. Set useWorldCoordinates to true to use world coordinates</summary>
        public BGCurvePoint(BGCurve curve, Vector3 position, ControlTypeEnum controlType, Vector3 controlFirst, Vector3 controlSecond, bool useWorldCoordinates = false)
        {
            this.curve = curve;
            this.controlType = controlType;

            if (useWorldCoordinates)
            {
                positionLocal = curve.transform.InverseTransformPoint(position);
                controlFirstLocal = curve.transform.InverseTransformDirection(controlFirst - position);
                controlSecondLocal = curve.transform.InverseTransformDirection(controlSecond - position);
            }
            else
            {
                positionLocal = position;
                controlFirstLocal = controlFirst;
                controlSecondLocal = controlSecond;
            }
        }

        #endregion

        #region public methods

        // =============================================== Public functions
        /// <summary>return system Vector 3 field </summary>
        public Vector3 Get(FieldEnum field)
        {
            Vector3 result;
            switch (field)
            {
                //position
                case FieldEnum.PositionWorld:
                    result = PositionWorld;
                    break;
                case FieldEnum.PositionLocal:
                    result = positionLocal;
                    break;

                //first control
                case FieldEnum.ControlFirstWorld:
                    result = ControlFirstWorld;
                    break;
                case FieldEnum.ControlFirstLocal:
                    result = controlFirstLocal;
                    break;

                //second control
                case FieldEnum.ControlSecondWorld:
                    result = ControlSecondWorld;
                    break;
                default:
                    result = controlSecondLocal;
                    break;
            }
            return result;
        }

        public BGCurvePoint CloneTo(BGCurve curve)
        {
            return new BGCurvePoint(curve, PositionLocal, ControlType, ControlFirstLocal, ControlSecondLocal);
        }

        #endregion



        #region Object methods overrides

        public override string ToString()
        {
            return "Point [localPosition=" + positionLocal + "]";
        }

        #endregion
    }
}