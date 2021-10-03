using System;
using BansheeGz.BGSpline.Curve;
using UnityEngine;

namespace BansheeGz.BGSpline.Components
{
    /// <summary>Translate + rotate + scale an object with one single component. </summary>
    [HelpURL("http://www.bansheegz.com/BGCurve/Cc/BGCcTrs")]
    [
        CcDescriptor(
            Description = "Translate + rotate + scale an object with one single component. " +
                          "It's 5 components in one (Cursor+CursorChangeLinear+MoveByCursor+RotateByCursor+ScaleByCursor) with basic functionality",
            Name = "TRS",
            Icon = "BGCcTrs123")
    ]
    [AddComponentMenu("BansheeGz/BGCurve/Components/BGCcTrs")]
    public class BGCcTrs : BGCcCursor
    {
        //===============================================================================================
        //                                                    Enums
        //===============================================================================================

        /// <summary>What to do, then cursor reaches first or last points</summary>
        public enum OverflowControlEnum
        {
            /// <summary>Cycle in the same direction. For example, if it reaches the end point, start from the first point again</summary>
            Cycle = 0,

            /// <summary>Change speed to -speed, and go in opposite direction</summary>
            PingPong = 1,

            /// <summary>Stop</summary>
            Stop = 2,
        }

        /// <summary>Mode for changing cursor position.</summary>
        public enum CursorChangeModeEnum
        {
            /// <summary>Speed is a constant value</summary>
            Constant,

            /// <summary>Speed is defined by a point field value</summary>
            LinearField,

            /// <summary>Speed is defined by 2 point field values</summary>
            LinearFieldInterpolate
        }

        public enum RotationInterpolationEnum
        {
            None = 0,
            Lerp = 1,
            Slerp = 2,
        }

        /// <summary> Up direction for target rotation. It's used only if rotationField is not assigned.</summary>
        public enum RotationUpEnum
        {
            WorldUp = 0,
            WorldDown = 1,
            WorldRight = 2,
            WorldLeft = 3,
            WorldForward = 4,
            WorldBack = 5,
        }
        //===============================================================================================
        //                                                    Fields (persistent)
        //===============================================================================================
        [SerializeField] [Tooltip("Object to manipulate.\r\n")]
        private Transform objectToManipulate;


        //===================================== Cursor Change
        [SerializeField]
        [Tooltip("Modes for changing cursor position.\n" +
                 "1)Constant- speed value is constant.\n" +
                 "2)LinearField- each point has its own speed value.\n" +
                 "3)LinearFieldInterpolate- each point has its own speed value and the final speed is linear interpolation based on the distance between 2 points values")]
        private CursorChangeModeEnum cursorChangeMode;

        [SerializeField] [Tooltip("Constant movement speed along the curve (Speed * Time.deltaTime). You can override this value for each point with speedField")]
        private float speed = 5;

        [SerializeField] [Tooltip("Field to store the speed between each point. It should be a float field.")]
        private BGCurvePointField speedField;

        [SerializeField] [Tooltip("Cursor will be moved in FixedUpdate instead of Update")]
        private bool useFixedUpdate;

        [SerializeField] [Tooltip("How to change speed, when curve reaches the end.")]
        private OverflowControlEnum overflowControl;

        //===================================== Move
        [SerializeField] [Tooltip("Object should be translated.\r\n")]
        private bool moveObject = true;

        //===================================== Rotate
        [SerializeField] [Tooltip("Object should be rotated.\r\n")]
        private bool rotateObject;

        [SerializeField] [Tooltip("Rotation interpolation mode.\r\n")]
        private RotationInterpolationEnum rotationInterpolation;

        [SerializeField] [Tooltip("Rotation Lerp rotationSpeed. (Quaternion.Lerp(from,to, lerpSpeed * Time.deltaTime)) ")]
        private float lerpSpeed = 5;

        [SerializeField] [Tooltip("Rotation Slerp rotationSpeed. (Quaternion.Slerp(from,to, slerpSpeed * Time.deltaTime)) ")]
        private float slerpSpeed = 5;

        [SerializeField] [Tooltip("Angle to add to final result.")]
        private Vector3 offsetAngle;

        [SerializeField] [Tooltip("Up vector to be used with Quaternion.LookRotation to determine rotation")]
        private RotationUpEnum upVector;

        [SerializeField] [Tooltip("Field to store the rotation between each point. It should be a Quaternion field.")]
        private BGCurvePointField rotationField;

        //===================================== Scale
        [SerializeField] [Tooltip("Object should be scaled.\r\n")]
        private bool scaleObject;

        [SerializeField] [Tooltip("Field to store the scale value at points. It should be a Vector3 field.")]
        private BGCurvePointField scaleField;

        //===============================================================================================
        //                                                    Fields (non persistent)
        //===============================================================================================
        public bool SpeedIsReversed { get; set; }

        //===============================================================================================
        //                                                    Properties
        //===============================================================================================
        public override string Error
        {
            get
            {
                if (objectToManipulate == null) return "Object To Manipulate is not set.";
                switch (cursorChangeMode)
                {
                    case CursorChangeModeEnum.LinearField:
                    case CursorChangeModeEnum.LinearFieldInterpolate:
                        if (speedField == null) return "Speed field is not set.";
                        if (speedField.Type != BGCurvePointField.TypeEnum.Float) return "Speed field should have float type.";
                        break;
                }

                if (rotateObject)
                {
                    var math = Math;
                    if (math == null || !math.IsCalculated(BGCurveBaseMath.Field.Tangent)) return "Math does not calculate tangents.";
                    if (rotationField != null && rotationField.Type != BGCurvePointField.TypeEnum.Quaternion) return "Rotate field should have Quaternion type.";
                }

                if (scaleObject)
                {
                    if (scaleField == null) return "Scale field is not set.";
                    if (scaleField.Type != BGCurvePointField.TypeEnum.Vector3) return "Scale field should have Vector3 type.";
                }

                return null;
            }
        }

        /// <summary>What to do, then cursor reaches first or last points</summary>
        public OverflowControlEnum OverflowControl
        {
            get { return overflowControl; }
            set { ParamChanged(ref overflowControl, value); }
        }

        /// <summary>Modes for changing cursor position </summary>
        public CursorChangeModeEnum CursorChangeMode
        {
            get { return cursorChangeMode; }
            set
            {
                if (cursorChangeMode == value) return;
                cursorChangeMode = value;
                FireChangedParams();
            }
        }

        /// <summary>Speed for Constant mode. The result speed is calculated as Speed * Time.deltaTime </summary>
        public float Speed
        {
            get { return speed; }
            set { ParamChanged(ref speed, value); }
        }

        /// <summary>Custom field to get speed value. It should be a float field. The result speed is calculated as Speed * Time.deltaTime </summary>
        public BGCurvePointField SpeedField
        {
            get { return speedField; }
            set { ParamChanged(ref speedField, value); }
        }


        public Transform ObjectToManipulate
        {
            get { return objectToManipulate; }
            set { ParamChanged(ref objectToManipulate, value); }
        }

        /// <summary>Cursor will be moved in FixedUpdate instead of Update</summary>
        public bool UseFixedUpdate
        {
            get { return useFixedUpdate; }
            set { ParamChanged(ref useFixedUpdate, value); }
        }

        /// <summary>object should be moved</summary>
        public bool MoveObject
        {
            get { return moveObject; }
            set { ParamChanged(ref moveObject, value); }
        }

        /// <summary>object should be rotateObject</summary>
        public bool RotateObject
        {
            get { return rotateObject; }
            set { ParamChanged(ref rotateObject, value); }
        }

        /// <summary>object should be scaled</summary>
        public bool ScaleObject
        {
            get { return scaleObject; }
            set { ParamChanged(ref scaleObject, value); }
        }

        /// <summary>offset to apply to final rotation </summary>
        public Vector3 OffsetAngle
        {
            get { return offsetAngle; }
            set { ParamChanged(ref offsetAngle, value); }
        }
        /// <summary>Up vector to be used with Quaternion.LookRotation to determine rotation</summary>
        public RotationUpEnum UpVector
        {
            get { return upVector; }
            set { ParamChanged(ref upVector, value); }
        }

        /// <summary>Rotation interpolation between current rotation and target rotation </summary>
        public RotationInterpolationEnum RotationInterpolation
        {
            get { return rotationInterpolation; }
            set { ParamChanged(ref rotationInterpolation, value); }
        }

        /// <summary> Rotation speed for Lerp rotation interpolation </summary>
        public float LerpSpeed
        {
            get { return lerpSpeed; }
            set { ParamChanged(ref lerpSpeed, value); }
        }

        /// <summary> Rotation speed for Slerp rotation interpolation </summary>
        public float SlerpSpeed
        {
            get { return slerpSpeed; }
            set { ParamChanged(ref slerpSpeed, value); }
        }

        /// <summary>Custom field to get rotation value. It should be a Quaternion field.</summary>
        public BGCurvePointField RotationField
        {
            get { return rotationField; }
            set { ParamChanged(ref rotationField, value); }
        }

        public BGCurvePointField ScaleField
        {
            get { return scaleField; }
            set { ParamChanged(ref scaleField, value); }
        }

        //===============================================================================================
        //                                                    Methods
        //===============================================================================================
        // Update is called once per frame
        private void Update()
        {
            if (useFixedUpdate) return;
            Step();
        }

        // fixed update may be called several times per frame or once per several frames 
        private void FixedUpdate()
        {
            if (!useFixedUpdate) return;
            Step();
        }

        private void Step()
        {
            if (cursorChangeMode == CursorChangeModeEnum.Constant && System.Math.Abs(speed) < BGCurve.Epsilon) return;
            if (Curve.PointsCount < 2 || Error != null) return;

            //calculate movement delta
            float delta;
            var sectionIndex = -1;
            switch (cursorChangeMode)
            {
                case CursorChangeModeEnum.Constant:
                    delta = speed * Time.deltaTime;
                    break;
                case CursorChangeModeEnum.LinearField:
                    sectionIndex = CalculateSectionIndex();
                    delta = Curve[sectionIndex].GetFloat(speedField.FieldName) * Time.deltaTime;
                    break;
                case CursorChangeModeEnum.LinearFieldInterpolate:
                    BGCurvePointI fromPoint;
                    BGCurvePointI toPoint;
                    float ratio;
                    FillInterpolationInfo(ref sectionIndex, out fromPoint, out toPoint, out ratio);
                    delta = Mathf.Lerp(fromPoint.GetFloat(speedField.FieldName), toPoint.GetFloat(speedField.FieldName), ratio) * Time.deltaTime;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            //change the distance
            if (SpeedIsReversed) delta = -delta;
            distance = distance + delta;

            //overflow check
            if (distance < 0)
            {
                switch (overflowControl)
                {
                    case OverflowControlEnum.Cycle:
                        distance = Math.GetDistance();
                        break;
                    case OverflowControlEnum.PingPong:
                        SpeedIsReversed = !SpeedIsReversed;
                        distance = 0;
                        break;
                    case OverflowControlEnum.Stop:
                        Speed = 0;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else
            {
                var totalDistance = Math.GetDistance();
                if (distance > totalDistance)
                {
                    switch (overflowControl)
                    {
                        case OverflowControlEnum.Cycle:
                            distance = 0;
                            break;
                        case OverflowControlEnum.PingPong:
                            SpeedIsReversed = !SpeedIsReversed;
                            distance = totalDistance;
                            break;
                        case OverflowControlEnum.Stop:
                            Speed = 0;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            // action!
            Trs(sectionIndex);
        }

        public void Trs(int sectionIndex = -1)
        {
            if (objectToManipulate == null) return;
            
            //move
            if (moveObject) objectToManipulate.position = Math.CalcPositionByDistance(distance);

            //rotate
            if (rotateObject)
            {
                Quaternion rotation;
                if (rotationField == null)
                {
                    Vector3 up;
                    switch (upVector)
                    {
                        case RotationUpEnum.WorldUp:
                            up = Vector3.up;
                            break;
                        case RotationUpEnum.WorldDown:
                            up = Vector3.down;
                            break;
                        case RotationUpEnum.WorldRight:
                            up = Vector3.right;
                            break;
                        case RotationUpEnum.WorldLeft:
                            up = Vector3.left;
                            break;
                        case RotationUpEnum.WorldForward:
                            up = Vector3.forward;
                            break;
                        case RotationUpEnum.WorldBack:
                            up = Vector3.back;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException("upVector");
                    }
                    rotation = Quaternion.LookRotation(CalculateTangent(), up);
                }
                else
                {
                    rotation = LerpQuaternion(ref sectionIndex, rotationField.FieldName);                    
                }

                //not sure how to handle it
                if (!(rotation.x == 0 && rotation.y == 0 && rotation.z == 0 && rotation.w == 0))
                {
                    rotation = rotation * Quaternion.Euler(offsetAngle);

                    // interpolation (rotation speed)
                    switch (rotationInterpolation)
                    {
                        case RotationInterpolationEnum.None:
                            ObjectToManipulate.rotation = rotation;
                            break;
                        case RotationInterpolationEnum.Lerp:
                            ObjectToManipulate.rotation = Quaternion.Lerp(ObjectToManipulate.rotation, rotation, lerpSpeed * Time.deltaTime);
                            break;
                        case RotationInterpolationEnum.Slerp:
                            ObjectToManipulate.rotation = Quaternion.Slerp(ObjectToManipulate.rotation, rotation, slerpSpeed * Time.deltaTime);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            //scale
            if (scaleObject && scaleField!=null) ObjectToManipulate.localScale = LerpVector3(ref sectionIndex, scaleField.FieldName);
        }

        private Vector3 LerpVector3(ref int sectionIndex, string fieldName)
        {
            BGCurvePointI fromPoint;
            BGCurvePointI toPoint;
            float ratio;
            FillInterpolationInfo(ref sectionIndex, out fromPoint, out toPoint, out ratio);
            return Vector3.Lerp(fromPoint.GetVector3(fieldName), toPoint.GetVector3(fieldName), ratio);
        }

        private Quaternion LerpQuaternion(ref int sectionIndex, string fieldName)
        {
            BGCurvePointI fromPoint;
            BGCurvePointI toPoint;
            float ratio;
            FillInterpolationInfo(ref sectionIndex, out fromPoint, out toPoint, out ratio);
            return Quaternion.Lerp(fromPoint.GetQuaternion(fieldName), toPoint.GetQuaternion(fieldName), ratio);
        }

        private void FillInterpolationInfo(ref int sectionIndex, out BGCurvePointI fromPoint, out BGCurvePointI toPoint, out float ratio)
        {
            if (sectionIndex == -1) sectionIndex = CalculateSectionIndex();

            fromPoint = Curve[sectionIndex];
            toPoint = sectionIndex == Curve.PointsCount - 1 ? Curve[0] : Curve[sectionIndex + 1];
            var sectionInfo = Math[sectionIndex];
            ratio = (Distance - sectionInfo.DistanceFromStartToOrigin) / (sectionInfo.DistanceFromEndToOrigin - sectionInfo.DistanceFromStartToOrigin);
        }

        private void OnDrawGizmosSelected()
        {
            if (Application.isPlaying) return;
            if (objectToManipulate == null) return;
            
            Trs();
            
        }
    }
}