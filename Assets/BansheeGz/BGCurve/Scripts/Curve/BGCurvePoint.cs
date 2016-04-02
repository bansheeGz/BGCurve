using System;
using UnityEngine;

namespace BansheeGz.BGSpline.Curve
{
    /// <summary>Basic class for one point data</summary>
    [Serializable]
    public class BGCurvePoint
    {
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
                positionLocal = value;
                curve.FireChange(new BGCurveChangedArgs(Curve, this, BGCurveChangedArgs.ChangeTypeEnum.Point));
            }
        }

        /// <summary>World absolute position</summary>
        public Vector3 PositionWorld
        {
            get { return curve.transform.TransformPoint(positionLocal); }
            set
            {
                positionLocal = curve.transform.InverseTransformPoint(value);
                curve.FireChange(new BGCurveChangedArgs(Curve, this, BGCurveChangedArgs.ChangeTypeEnum.Point));
            }
        }


        // =============================================== First Handle
        /// <summary>Local position for 1st control, relative to point's location</summary>
        public Vector3 ControlFirstLocal
        {
            get { return controlFirstLocal; }
            set
            {
                if (controlType == ControlTypeEnum.BezierSymmetrical)
                {
                    controlSecondLocal = -value;
                }
                controlFirstLocal = value;
                curve.FireChange(new BGCurveChangedArgs(Curve, this, BGCurveChangedArgs.ChangeTypeEnum.PointControl));
            }
        }

        /// <summary>World position for 1st control</summary>
        public Vector3 ControlFirstWorld
        {
            get { return curve.transform.TransformPoint(positionLocal + controlFirstLocal); }
            set
            {
                controlFirstLocal = value - PositionWorld;
                if (controlType == ControlTypeEnum.BezierSymmetrical)
                {
                    controlSecondLocal = -controlFirstLocal;
                }
                curve.FireChange(new BGCurveChangedArgs(Curve, this, BGCurveChangedArgs.ChangeTypeEnum.PointControl));
            }
        }


        // =============================================== Second Handle
        /// <summary>Local position for 2nd control, relative to point's position</summary>
        public Vector3 ControlSecondLocal
        {
            get { return controlSecondLocal; }
            set
            {
                if (controlType == ControlTypeEnum.BezierSymmetrical)
                {
                    controlFirstLocal = -value;
                }
                controlSecondLocal = value;
                curve.FireChange(new BGCurveChangedArgs(Curve, this, BGCurveChangedArgs.ChangeTypeEnum.PointControl));
            }
        }

        /// <summary>World position for 2nd control</summary>
        public Vector3 ControlSecondWorld
        {
            get { return curve.transform.TransformPoint(positionLocal + ControlSecondLocal); }
            set
            {
                controlSecondLocal = value - PositionWorld; 
                if (controlType == ControlTypeEnum.BezierSymmetrical)
                {
                    controlFirstLocal = -controlSecondLocal;
                }
                curve.FireChange(new BGCurveChangedArgs(Curve, this, BGCurveChangedArgs.ChangeTypeEnum.PointControl));
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

                controlType = value;
                if (controlType == ControlTypeEnum.BezierSymmetrical)
                {
                        controlSecondLocal = -controlFirstLocal;
                }
                curve.FireChange(new BGCurveChangedArgs(Curve, this, BGCurveChangedArgs.ChangeTypeEnum.PointControlType));
            }
        }


        // =============================================== Constructors (use Curve.Create** helper functions)
        protected internal BGCurvePoint(BGCurve curve, Vector3 positionLocal)
            : this(curve, positionLocal, ControlTypeEnum.Absent)
        {
        }

        protected internal BGCurvePoint(BGCurve curve, Vector3 positionLocal, ControlTypeEnum controlType)
        {
            this.curve = curve;
            this.positionLocal = positionLocal;
            this.controlType = controlType;

            controlFirstLocal = Vector3.right;
            controlSecondLocal = -Vector3.right;
        }


        protected internal BGCurvePoint(BGCurve curve, Vector3 positionLocal, ControlTypeEnum controlType, Vector3 controlFirstLocal, Vector3 controlSecondLocal)
        {
            this.positionLocal = positionLocal;
            this.curve = curve;
            this.controlType = controlType;
            this.controlFirstLocal = controlFirstLocal;
            this.controlSecondLocal = controlSecondLocal;
        }
    }
}