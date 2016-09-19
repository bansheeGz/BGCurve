using System;
using UnityEngine;
using BansheeGz.BGSpline.Curve;

namespace BansheeGz.BGSpline.Components
{
    /// <summary> Rotates an object according to the tangent at cursor's position</summary>
    [HelpURL("http://www.bansheegz.com/BGCurve/Cc/BGCcCursorObjectRotate")]
    [
        CcDescriptor(
            Description = "Align the object's rotation with curve's tangent at the point, the Cursor provides.",
            Name = "Rotate Object By Cursor",
            Image = "Assets/BansheeGz/BGCurve/Icons/Components/BGCcCursorObjectRotate123.png")
    ]
    [AddComponentMenu("BansheeGz/BGCurve/Components/BGCcRotateObject", 2)]
    public class BGCcCursorObjectRotate : BGCcWithCursorObject
    {
        public enum RotationInterpolationEnum
        {
            None = 0,
            Lerp = 1,
            Slerp = 2,
        }

        public enum RotationUpEnum
        {
            WorldUp = 0,
            WorldCustom = 1,

            LocalUp = 2,
            LocalCustom = 3,

            TargetParentUp = 4,
            TargetParentUpCustom = 5,
        }

        /// <summary>object was rotated </summary>
        public event EventHandler ChangedObjectRotation;


        [SerializeField] [Tooltip("Rotation interpolation mode.")] private RotationInterpolationEnum rotationInterpolation;

        [SerializeField] [Tooltip("Rotation Lerp rotationSpeed. (Quaternion.Lerp(from,to, lerpSpeed * Time.deltaTime)) ")] private float lerpSpeed = 5;

        [SerializeField] [Tooltip("Rotation Slerp rotationSpeed. (Quaternion.Slerp(from,to, slerpSpeed * Time.deltaTime)) ")] private float slerpSpeed = 5;

        [SerializeField] [Tooltip("Up mode for Quaternion.LookRotation. "
                                  + "\r\n1) WorldUp - use Vector.up in world coordinates"
                                  + "\r\n2) WorldCustom - use custom Vector in world coordinates"
                                  + "\r\n3) LocalUp - use Vector.up in local coordinates "
                                  + "\r\n4) LocalCustom - use custom Vector in local coordinates"
                                  + "\r\n5) TargetParentUp - use Vector.up in target object parent's local coordinates"
                                  + "\r\n6) TargetParentUpCustom- use custom Vector in target object parent's local coordinates"
            )] private RotationUpEnum upMode = RotationUpEnum.WorldUp;

        [SerializeField] [Tooltip("Custom Up vector for Quaternion.LookRotation")] private Vector3 upCustom = Vector3.up;

        public override string Error
        {
            get { return ChoseMessage(base.Error, () => (!Cursor.Math.IsCalculated(BGCurveBaseMath.Field.Tangent) ? "Math does not calculate tangents." : null)); }
        }

        public override string Warning
        {
            get
            {
                return (upMode == RotationUpEnum.TargetParentUp || upMode == RotationUpEnum.TargetParentUpCustom)
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

        public RotationInterpolationEnum RotationInterpolation
        {
            get { return rotationInterpolation; }
            set { ParamChanged(ref rotationInterpolation, value); }
        }

        public float LerpSpeed
        {
            get { return lerpSpeed; }
            set { ParamChanged(ref lerpSpeed, value); }
        }

        public float SlerpSpeed
        {
            get { return slerpSpeed; }
            set { ParamChanged(ref slerpSpeed, value); }
        }

        public Vector3 UpCustom
        {
            get { return upCustom; }
            set { ParamChanged(ref upCustom, value); }
        }

        public RotationUpEnum UpMode
        {
            get { return upMode; }
            set { ParamChanged(ref upMode, value); }
        }

        // Update is called once per frame
        private void Update()
        {
            var cursor = Cursor;
            var math = cursor.Math;

            var targetTransform = ObjectToManipulate;
            if (targetTransform == null || math == null || !math.IsCalculated(BGCurveBaseMath.Field.Tangent)) return;

            var tangent = cursor.CalculateTangent();
            if (Vector3.SqrMagnitude(tangent) < 0.01) return;

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
                default:
                    //TargetParentUp or TargetParentUpCustom
                    if (targetTransform.parent != null)
                    {
                        upwards = targetTransform.parent.InverseTransformDirection(upMode == RotationUpEnum.TargetParentUp ? Vector3.up : upCustom);
                    }
                    else
                    {
                        upwards = upMode == RotationUpEnum.TargetParentUp ? Vector3.up : upCustom;
                    }
                    break;
            }

            var quaternion = Quaternion.LookRotation(tangent, upwards);

            // interpolation (rotation speed)
            switch (rotationInterpolation)
            {
                case RotationInterpolationEnum.Lerp:
                    quaternion = Quaternion.Lerp(targetTransform.rotation, quaternion, lerpSpeed*Time.deltaTime);
                    break;
                case RotationInterpolationEnum.Slerp:
                    quaternion = Quaternion.Slerp(targetTransform.rotation, quaternion, slerpSpeed*Time.deltaTime);
                    break;
            }

            targetTransform.rotation = quaternion;

            if (ChangedObjectRotation != null) ChangedObjectRotation(this, null);
        }
    }
}