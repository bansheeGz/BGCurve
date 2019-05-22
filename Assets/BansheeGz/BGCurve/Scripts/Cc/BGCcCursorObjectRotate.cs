using System;
using UnityEngine;
using BansheeGz.BGSpline.Curve;

namespace BansheeGz.BGSpline.Components
{
    /// <summary> 
    /// Rotates an object.
    /// if no rotation field is defined, object rotation is based on the curve's tangent at current Cursor position.
    /// If rotation field is used, 2 rotation values, taken from boundary points, are lerped by current Cursor position.
    /// </summary>
    [HelpURL("http://www.bansheegz.com/BGCurve/Cc/BGCcCursorObjectRotate")]
    [
        CcDescriptor(
            Description = "Align the object's rotation with curve's tangent or 'rotation' field values at the point, the Cursor provides.",
            Name = "Rotate Object By Cursor",
            Icon = "BGCcCursorObjectRotate123")
    ]
    [AddComponentMenu("BansheeGz/BGCurve/Components/BGCcRotateObject")]
    [ExecuteInEditMode]
    public class BGCcCursorObjectRotate : BGCcWithCursorObject
    {
        //===============================================================================================
        //                                                    Enums
        //===============================================================================================
        /// <summary> Rotation interpolation between current rotation and target rotation </summary>
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
            WorldCustom = 1,

            LocalUp = 2,
            LocalCustom = 3,

            TargetParentUp = 4,
            TargetParentUpCustom = 5,
            
//            CustomField = 6,
        }

        //===============================================================================================
        //                                                    Events
        //===============================================================================================

        /// <summary>object was rotated </summary>
        public event EventHandler ChangedObjectRotation;


        //===============================================================================================
        //                                                    Fields (persistent)
        //===============================================================================================

        //============================================= Common
        [SerializeField] [Tooltip("Rotation interpolation mode.")] private RotationInterpolationEnum rotationInterpolation;

        [SerializeField] [Tooltip("Rotation Lerp rotationSpeed. (Quaternion.Lerp(from,to, lerpSpeed * Time.deltaTime)) ")] private float lerpSpeed = 5;

        [SerializeField] [Tooltip("Rotation Slerp rotationSpeed. (Quaternion.Slerp(from,to, slerpSpeed * Time.deltaTime)) ")] private float slerpSpeed = 5;

        [SerializeField] [Tooltip("Angle to add to final result.")] private Vector3 offsetAngle;

        //============================================= Tangent rotation
        [SerializeField] [Tooltip("Up mode for tangent Quaternion.LookRotation. It's used only if rotationField is not assigned."
                                  + "\r\n1) WorldUp - use Vector.up in world coordinates"
                                  + "\r\n2) WorldCustom - use custom Vector in world coordinates"
                                  + "\r\n3) LocalUp - use Vector.up in local coordinates "
                                  + "\r\n4) LocalCustom - use custom Vector in local coordinates"
                                  + "\r\n5) TargetParentUp - use Vector.up in target object parent's local coordinates"
                                  + "\r\n6) TargetParentUpCustom- use custom Vector in target object parent's local coordinates"
                          )] private RotationUpEnum upMode = RotationUpEnum.WorldUp;

        [SerializeField] [Tooltip("Custom Up vector for tangent Quaternion.LookRotation. It's used only if rotationField is not assigned.")] private Vector3 upCustom = Vector3.up;

//        [SerializeField] [Tooltip("Custom Up vector field for tangent Quaternion.LookRotation. It should be Vector3 field. It's used only if rotationField is not assigned.")] 
//        private BGCurvePointField upCustomField;
        
//        [SerializeField] [Tooltip("Custom Up vector angle offset field for tangent Quaternion.LookRotation. It should be float field. It's used only if rotationField is not assigned.")] 
//        private BGCurvePointField upOffsetField;
        
        //============================================= By field rotation
        [SerializeField] [Tooltip("Field to store the rotation between each point. It should be a Quaternion field.")] private BGCurvePointField rotationField;

        [SerializeField] [Tooltip("Additional 360 degree revolutions around tangent. It's used only if rotationField is assigned. " +
                                  "It can be overriden with 'int' revolutionsAroundTangentField field.")] private int revolutionsAroundTangent;

        [SerializeField] [Tooltip("Field to store additional 360 degree revolutions around tangent for each point. It's used only if rotationField is assigned. " +
                                  "It should be an int field.")] private BGCurvePointField revolutionsAroundTangentField;

        [SerializeField] [Tooltip("By default revolutions around tangent is counter-clockwise. Set it to true to reverse direction. It's used only if rotationField is assigned." +
                                  "It can be overriden with bool field")] private bool revolutionsClockwise;

        [SerializeField] [Tooltip("Field to store direction for revolutions around tangent. It should be an bool field.  It's used only if rotationField is assigned.")] private BGCurvePointField
            revolutionsClockwiseField;


        /// <summary> Rotation interpolation between current rotation and target rotation </summary>
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

        /// <summary> Custom Up Vector(direction) for target rotaion</summary>
        public Vector3 UpCustom
        {
            get { return upCustom; }
            set { ParamChanged(ref upCustom, value); }
        }

        /// <summary> Up mode for tangent rotation. It's used only if rotationField is not assigned.</summary>
        public RotationUpEnum UpMode
        {
            get { return upMode; }
            set { ParamChanged(ref upMode, value); }
        }

        /// <summary> Rotation field to get rotation values from. It should be a Quaternion field</summary>
        public BGCurvePointField RotationField
        {
            get { return rotationField; }
            set { ParamChanged(ref rotationField, value); }
        }

        /// <summary> Field for a number of full turnovers around tangent. It's used only if rotationField is assigned. It should be an int field</summary>
        public BGCurvePointField RevolutionsAroundTangentField
        {
            get { return revolutionsAroundTangentField; }
            set { ParamChanged(ref revolutionsAroundTangentField, value); }
        }

        /// <summary> Number of full turnovers around tangent for each section.It's used only if rotationField is assigned. It can be overriden with RevolutionsAroundTangentField</summary>
        public int RevolutionsAroundTangent
        {
            get { return revolutionsAroundTangent; }
            set { ParamChanged(ref revolutionsAroundTangent, value); }
        }

        /// <summary> Field for identifying if RevolutionsAroundTangent should be clockwise or counter clockwise.It's used only if rotationField is assigned. It should be an bool field</summary>
        public BGCurvePointField RevolutionsClockwiseField
        {
            get { return revolutionsClockwiseField; }
            set { ParamChanged(ref revolutionsClockwiseField, value); }
        }

        /// <summary> Should RevolutionsAroundTangent be clockwise or counter clockwise. It's used only if rotationField is assigned. It can be overriden by RevolutionsClockwiseField</summary>
        public bool RevolutionsClockwise
        {
            get { return revolutionsClockwise; }
            set { ParamChanged(ref revolutionsClockwise, value); }
        }

        /// <summary>offset to apply to final rotation </summary>
        public Vector3 OffsetAngle
        {
            get { return offsetAngle; }
            set { ParamChanged(ref offsetAngle, value); }
        }

        //===============================================================================================
        //                                                    Editor stuff
        //===============================================================================================
        public override string Error
        {
            get
            {
                return ChoseMessage(base.Error,
                    () =>
                    {
//                        if(upMode==RotationUpEnum.CustomField && upCustomField==null) return "upMode is set to customField but upCustomField is null.";
                        
                        if (!Cursor.Math.IsCalculated(BGCurveBaseMath.Field.Tangent))
                        {
                            if (rotationField == null) return "Math should calculate tangents if rotation field is null.";

                            if (RevolutionsAroundTangent != 0 || RevolutionsAroundTangentField != null) return "Math should calculate tangents if revolutions are used.";
                        }

                        return null;
                    });
            }
        }

        public override string Warning
        {
            get
            {
                return rotationField == null && (upMode == RotationUpEnum.TargetParentUp || upMode == RotationUpEnum.TargetParentUpCustom)
                       && ObjectToManipulate != null && ObjectToManipulate.parent == null
                    ? "Up Mode is set to " + upMode + ", however object's parent is null"
                    : null;
            }
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
        [Range(.5f, 1.5f)] [SerializeField] private float handlesScale = 1;
        [SerializeField] private Color handlesColor = Color.white;

        public float HandlesScale
        {
            get { return handlesScale; }
            set { handlesScale = value; }
        }

        public Color HandlesColor
        {
            get { return handlesColor; }
            set { handlesColor = value; }
        }
#endif

        //===============================================================================================
        //                                                    Fields (Not persistent)
        //===============================================================================================
        //latest successfull rotation that was used
        private Quaternion rotation = Quaternion.identity;

        /// <summary> latest successfull rotation that was used </summary>
        public Quaternion Rotation
        {
            get { return rotation; }
        }

        //===============================================================================================
        //                                                    Unity Callbacks
        //===============================================================================================
        // Update is called once per frame
        private void Update()
        {
            if (Curve.PointsCount == 0) return;

            var targetTransform = ObjectToManipulate;
            if (targetTransform == null) return;

            if (!TryToCalculateRotation(ref rotation)) return;

            targetTransform.rotation = rotation;

            if (ChangedObjectRotation != null) ChangedObjectRotation(this, null);
        }

        //===============================================================================================
        //                                                    Public Functions
        //===============================================================================================
        /// <summary>Try to calculate target rotation at current Cursor positon</summary>
        /// <returns>true if result was actually calculated and changed, false if calculation is failed</returns>
        public bool TryToCalculateRotation(ref Quaternion result)
        {
            var pointsCount = Curve.PointsCount;
            if (pointsCount == 0) return false;

            var cursor = Cursor;
            var math = cursor.Math;

            if (rotationField == null)
            {
                // =============================================== By tangent
                if (math == null || !math.IsCalculated(BGCurveBaseMath.Field.Tangent)) return false;

                if (pointsCount == 1) result = Quaternion.identity;
                else
                {
                    var tangent = cursor.CalculateTangent();
                    if (Vector3.SqrMagnitude(tangent) < 0.01) return false;

                    // up vector
                    Vector3 upwards;
                    switch (upMode)
                    {
                        case RotationUpEnum.WorldUp:
                            upwards = Vector3.up;
                            break;
                        case RotationUpEnum.WorldCustom:
                            upwards = upCustom;
                            break;
                        case RotationUpEnum.LocalUp:
                            upwards = transform.InverseTransformDirection(Vector3.up);
                            break;
                        case RotationUpEnum.LocalCustom:
                            upwards = transform.InverseTransformDirection(upCustom);
                            break;
                        case RotationUpEnum.TargetParentUp:
                        case RotationUpEnum.TargetParentUpCustom:
                            //TargetParentUp or TargetParentUpCustom
                            var targetTransform = ObjectToManipulate;
                            if (targetTransform.parent != null)
                            {
                                upwards = targetTransform.parent.InverseTransformDirection(upMode == RotationUpEnum.TargetParentUp ? Vector3.up : upCustom);
                            }
                            else
                            {
                                upwards = upMode == RotationUpEnum.TargetParentUp ? Vector3.up : upCustom;
                            }
                            break;
/*
                        case RotationUpEnum.CustomField:
                            if (upCustomField == null)
                            {
                                result = Quaternion.identity;
                                return false;
                            }
                            upwards = Cursor.LerpVector(upCustomField.FieldName);
                            break;
*/
                        default:
                        {
                            throw new Exception("Unsupported upMode:" + upMode);
                        }
                    }
                    
                    result = Quaternion.LookRotation(tangent, upwards);
                }
            }
            else
            {
                // =============================================== By field
                if (pointsCount == 1) result = Curve[0].GetQuaternion(rotationField.FieldName);
                else
                {
                    if (revolutionsAroundTangentField == null && revolutionsAroundTangent == 0) result = LerpQuaternion(rotationField.FieldName); //no compications
                    else
                    {
                        // there is possible revolutions involved- we need to check field if it exists

                        //we need currentSection only if field is present
                        var currentSection = revolutionsAroundTangentField != null || revolutionsClockwiseField != null ? cursor.CalculateSectionIndex() : -1;
                        //rotation without revolutions
                        result = LerpQuaternion(rotationField.FieldName, currentSection);

                        //do we have revolutions?
                        var additionalRevolutions = Mathf.Clamp(
                            revolutionsAroundTangentField != null
                                ? Curve[currentSection].GetInt(revolutionsAroundTangentField.FieldName)
                                : revolutionsAroundTangent
                            , 0, int.MaxValue);

                        if (additionalRevolutions > 0 && math.IsCalculated(BGCurveBaseMath.Field.Tangent))
                        {
                            //additional rotation around tangent is needed
                            var tangent = cursor.CalculateTangent();

                            if (Vector3.SqrMagnitude(tangent) > 0.01)
                            {
                                // targetAngle- target angle at t=1
                                var targetAngle = 360*additionalRevolutions;

                                //change if Clockwise
                                if (revolutionsClockwiseField != null ? Curve[currentSection].GetBool(revolutionsClockwiseField.FieldName) : revolutionsClockwise) targetAngle = -targetAngle;

                                //calculate t ratio for lerping. 0- at start (from point), 1= at the end (to point)
                                int indexFrom, indexTo;
                                var t = GetT(out indexFrom, out indexTo, currentSection);
                                var angle = Mathf.Lerp(0, targetAngle, t);

                                //sum up rotaitons
                                result = result*Quaternion.AngleAxis(angle, tangent);
                            }
                        }
                    }
                }
            }


            // sum up rotations
            result *= Quaternion.Euler(offsetAngle);

            // interpolation (rotation speed)
            switch (rotationInterpolation)
            {
                case RotationInterpolationEnum.Lerp:
                    result = Quaternion.Lerp(ObjectToManipulate.rotation, rotation, lerpSpeed*Time.deltaTime);
                    break;
                case RotationInterpolationEnum.Slerp:
                    result = Quaternion.Slerp(ObjectToManipulate.rotation, rotation, slerpSpeed*Time.deltaTime);
                    break;
            }

            //success
            return true;
        }
    }
}