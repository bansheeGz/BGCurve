using System;
using UnityEngine;

namespace BansheeGz.BGSpline.Curve
{
    /// <summary>Point, attached to separate GameObject </summary>
    [DisallowMultipleComponent]
    // Note, some code was copy pasted from BGCurvePoint class
    public class BGCurvePointGO : MonoBehaviour, BGCurvePointI
    {
        #region fields

        //control type
        [SerializeField] private BGCurvePoint.ControlTypeEnum controlType;

        //relative to curve position
        [SerializeField] private Vector3 positionLocal;

        //relative to point position
        [SerializeField] private Vector3 controlFirstLocal;
        [SerializeField] private Vector3 controlSecondLocal;

        //transform for using as point position
        [SerializeField] private Transform pointTransform;


        //point's curve
        [SerializeField] private BGCurve curve;

        //custom fields values for all points. it's an array with only one element. the reason why we store it like this- is to reduce storage and serialization costs.
        [SerializeField] private BGCurvePoint.FieldsValues[] fieldsValues;

        /// <summary>The curve, point's belong to</summary>
        public BGCurve Curve
        {
            get { return curve; }
        }

        /// <summary>This field is not meant for use outside of BGCurve package </summary>
        //all fields values
        public BGCurvePoint.FieldsValues PrivateValuesForFields
        {
            get
            {
                if (fieldsValues == null || fieldsValues.Length < 1 || fieldsValues[0] == null) fieldsValues = new[] {new BGCurvePoint.FieldsValues()};
                return fieldsValues[0];
            }
            set
            {
                if (fieldsValues == null || fieldsValues.Length < 1 || fieldsValues[0] == null) fieldsValues = new[] {new BGCurvePoint.FieldsValues()};
                fieldsValues[0] = value;
            }
        }


        // =============================================== Position

        //see interface for comments
        public Vector3 PositionLocal
        {
            get
            {
                if (pointTransform != null) return curve.transform.InverseTransformPoint(pointTransform.position);

                switch (Curve.PointsMode)
                {
                    case BGCurve.PointsModeEnum.GameObjectsNoTransform:
                        return positionLocal;
                    case BGCurve.PointsModeEnum.GameObjectsTransform:
                        return curve.transform.InverseTransformPoint(transform.position);
                    default:
                        throw WrongMode();
                }
            }
            set { SetPosition(value); }
        }

        //see interface for comments
        public Vector3 PositionLocalTransformed
        {
            get
            {
                if(pointTransform != null) return pointTransform.position - curve.transform.position;

                switch (Curve.PointsMode)
                {
                    case BGCurve.PointsModeEnum.GameObjectsNoTransform:
                        return curve.transform.TransformPoint(positionLocal) - curve.transform.position;
                    case BGCurve.PointsModeEnum.GameObjectsTransform:
                        return transform.position - curve.transform.position; 
                    default:
                        throw WrongMode();
                }
            }
            set { SetPosition(value + curve.transform.position, true); }
        }

        //see interface for comments
        public Vector3 PositionWorld
        {
            get
            {
                if (pointTransform != null) return pointTransform.position;

                switch (Curve.PointsMode)
                {
                    case BGCurve.PointsModeEnum.GameObjectsNoTransform:
                        return curve.transform.TransformPoint(positionLocal);
                    case BGCurve.PointsModeEnum.GameObjectsTransform:
                        return transform.position;
                    default:
                        throw WrongMode();
                }
            }
            set { SetPosition(value, true); }
        }


        // =============================================== First Handle
        //see interface for comments
        public Vector3 ControlFirstLocal
        {
            get { return controlFirstLocal; }
            set { SetControlFirstLocal(value); }
        }

        //see interface for comments
        public Vector3 ControlFirstLocalTransformed
        {
            get { return TargetTransform.TransformVector(controlFirstLocal); }
            set { SetControlFirstLocal(TargetTransform.InverseTransformVector(value)); }
        }


        //see interface for comments
        public Vector3 ControlFirstWorld
        {
            get
            {
                if (pointTransform != null) return pointTransform.position + pointTransform.TransformVector(controlFirstLocal);

                switch (Curve.PointsMode)
                {
                    case BGCurve.PointsModeEnum.GameObjectsNoTransform:
                        return curve.transform.TransformPoint(new Vector3(positionLocal.x + controlFirstLocal.x, positionLocal.y + controlFirstLocal.y, positionLocal.z + controlFirstLocal.z));
                    case BGCurve.PointsModeEnum.GameObjectsTransform:
                        return transform.position + transform.TransformVector(controlFirstLocal);
                    default:
                        throw WrongMode();
                }
            }
            set
            {
                Vector3 localPos;
                if (pointTransform != null) localPos = pointTransform.InverseTransformVector(value - pointTransform.position);
                else
                {
                    switch (Curve.PointsMode)
                    {
                        case BGCurve.PointsModeEnum.GameObjectsNoTransform:
                            localPos = curve.transform.InverseTransformPoint(value) - PositionLocal;
                            break;
                        case BGCurve.PointsModeEnum.GameObjectsTransform:
                            localPos = transform.InverseTransformVector(value - transform.position);
                            break;
                        default:
                            throw WrongMode();
                    }
                }
                SetControlFirstLocal(localPos);
            }
        }


        // =============================================== Second Handle
        //see interface for comments
        public Vector3 ControlSecondLocal
        {
            get { return controlSecondLocal; }
            set { SetControlSecondLocal(value); }
        }

        //see interface for comments
        public Vector3 ControlSecondLocalTransformed
        {
            get { return TargetTransform.TransformVector(controlSecondLocal); }
            set { SetControlSecondLocal(TargetTransform.InverseTransformVector(value)); }
        }


        //see interface for comments
        public Vector3 ControlSecondWorld
        {
            get
            {
                if (pointTransform != null) return pointTransform.position + pointTransform.TransformVector(controlSecondLocal);

                switch (Curve.PointsMode)
                {
                    case BGCurve.PointsModeEnum.GameObjectsNoTransform:
                        return curve.transform.TransformPoint(new Vector3(positionLocal.x + controlSecondLocal.x, positionLocal.y + controlSecondLocal.y, positionLocal.z + controlSecondLocal.z));
                    case BGCurve.PointsModeEnum.GameObjectsTransform:
                        return transform.position + transform.TransformVector(controlSecondLocal);
                    default:
                        throw WrongMode();
                }
            }
            set
            {
                Vector3 localPos;
                if (pointTransform != null) localPos = pointTransform.InverseTransformVector(value - pointTransform.position);
                else
                {
                    switch (Curve.PointsMode)
                    {
                        case BGCurve.PointsModeEnum.GameObjectsNoTransform:
                            localPos = curve.transform.InverseTransformPoint(value) - PositionLocal;
                            break;
                        case BGCurve.PointsModeEnum.GameObjectsTransform:
                            localPos = transform.InverseTransformVector(value - transform.position);
                            break;
                        default:
                            throw WrongMode();
                    }
                }
                SetControlSecondLocal(localPos);
            }
        }

        // =============================================== Control type
        //see interface for comments
        public BGCurvePoint.ControlTypeEnum ControlType
        {
            get { return controlType; }
            set
            {
                if (controlType == value) return;

                curve.FireBeforeChange(BGCurve.EventPointControlType);

                controlType = value;

                if (controlType == BGCurvePoint.ControlTypeEnum.BezierSymmetrical) controlSecondLocal = -controlFirstLocal;

                curve.FireChange(curve.UseEventsArgs ? BGCurveChangedArgs.GetInstance(Curve, this, BGCurve.EventPointControlType) : null, sender: this);
            }
        }

        // =============================================== Transform
        public Transform PointTransform
        {
            get { return pointTransform; }
            set
            {
                if (pointTransform == value) return;

                curve.FireBeforeChange(BGCurve.EventPointTransform);

                var oldTransformNull = pointTransform == null && value != null;
                var newTransformNull = value == null && pointTransform != null;

                //we need to transfer system fields 
                var control1 = ControlFirstLocalTransformed;
                var control2 = ControlSecondLocalTransformed;
                var positionWorld = PositionWorld;

                pointTransform = value;

                // transfer system fields
                if (pointTransform != null)
                {
                    pointTransform.position = positionWorld;
                    controlFirstLocal = pointTransform.InverseTransformVector(control1);
                    controlSecondLocal = pointTransform.InverseTransformVector(control2);
                }
                else
                {
                    switch (curve.PointsMode)
                    {
                        case BGCurve.PointsModeEnum.GameObjectsNoTransform:
                            positionLocal = curve.transform.InverseTransformPoint(positionWorld);
                            controlFirstLocal = curve.transform.InverseTransformVector(control1);
                            controlSecondLocal = curve.transform.InverseTransformVector(control2);
                            break;
                        case BGCurve.PointsModeEnum.GameObjectsTransform:
                            transform.position = positionWorld;
                            controlFirstLocal = transform.InverseTransformVector(control1);
                            controlSecondLocal = transform.InverseTransformVector(control2);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException("curve.PointsMode");
                    }
                }


                // inform curve
                if (oldTransformNull) curve.PrivateTransformForPointAdded(curve.IndexOf(this));
                else if (newTransformNull) curve.PrivateTransformForPointRemoved(curve.IndexOf(this));

                curve.FireChange(curve.UseEventsArgs ? BGCurveChangedArgs.GetInstance(Curve, this, BGCurve.EventPointTransform) : null, sender: this);
            }
        }


        //target transform (used to calculate control positions)
        private Transform TargetTransform
        {
            get
            {
                if (pointTransform != null) return pointTransform;

                switch (Curve.PointsMode)
                {
                    case BGCurve.PointsModeEnum.GameObjectsNoTransform:
                        return curve.transform;
                    case BGCurve.PointsModeEnum.GameObjectsTransform:
                        return transform;
                    default:
                        throw WrongMode();
                }
            }
        }

        #endregion

        #region custom fields

        //===============================================================================================
        //                                                    Custom Fields (see interface for comments)
        //===============================================================================================
        //----------------------------------- Getters
        public T GetField<T>(string name)
        {
            var type = typeof(T);
            var value = GetField(name, type);
            var field = (T) value;
            return field;
        }

        public float GetFloat(string name)
        {
            return PrivateValuesForFields.floatValues[curve.IndexOfFieldValue(name)];
        }

        public bool GetBool(string name)
        {
            return PrivateValuesForFields.boolValues[curve.IndexOfFieldValue(name)];
        }

        public int GetInt(string name)
        {
            return PrivateValuesForFields.intValues[curve.IndexOfFieldValue(name)];
        }

        public Vector3 GetVector3(string name)
        {
            return PrivateValuesForFields.vector3Values[curve.IndexOfFieldValue(name)];
        }

        public Quaternion GetQuaternion(string name)
        {
            return PrivateValuesForFields.quaternionValues[curve.IndexOfFieldValue(name)];
        }

        public Bounds GetBounds(string name)
        {
            return PrivateValuesForFields.boundsValues[curve.IndexOfFieldValue(name)];
        }

        public Color GetColor(string name)
        {
            return PrivateValuesForFields.colorValues[curve.IndexOfFieldValue(name)];
        }

        public object GetField(string name, Type type)
        {
            return BGCurvePoint.FieldTypes.GetField(curve, type, name, PrivateValuesForFields);
        }

        //----------------------------------- Setters
        public void SetField<T>(string name, T value)
        {
            SetField(name, value, typeof(T));
        }

        public void SetField(string name, object value, Type type)
        {
            curve.FireBeforeChange(BGCurve.EventPointField);

            BGCurvePoint.FieldTypes.SetField(curve, type, name, value, PrivateValuesForFields);

            curve.FireChange(curve.UseEventsArgs ? BGCurveChangedArgs.GetInstance(Curve, this, BGCurve.EventPointField) : null, sender: this);
        }

        public void SetFloat(string name, float value)
        {
            curve.FireBeforeChange(BGCurve.EventPointField);

            PrivateValuesForFields.floatValues[curve.IndexOfFieldValue(name)] = value;

            curve.FireChange(curve.UseEventsArgs ? BGCurveChangedArgs.GetInstance(Curve, this, BGCurve.EventPointField) : null, sender: this);
        }

        public void SetBool(string name, bool value)
        {
            curve.FireBeforeChange(BGCurve.EventPointField);

            PrivateValuesForFields.boolValues[curve.IndexOfFieldValue(name)] = value;

            curve.FireChange(curve.UseEventsArgs ? BGCurveChangedArgs.GetInstance(Curve, this, BGCurve.EventPointField) : null, sender: this);
        }

        public void SetInt(string name, int value)
        {
            curve.FireBeforeChange(BGCurve.EventPointField);

            PrivateValuesForFields.intValues[curve.IndexOfFieldValue(name)] = value;

            curve.FireChange(curve.UseEventsArgs ? BGCurveChangedArgs.GetInstance(Curve, this, BGCurve.EventPointField) : null, sender: this);
        }

        public void SetVector3(string name, Vector3 value)
        {
            curve.FireBeforeChange(BGCurve.EventPointField);

            PrivateValuesForFields.vector3Values[curve.IndexOfFieldValue(name)] = value;

            curve.FireChange(curve.UseEventsArgs ? BGCurveChangedArgs.GetInstance(Curve, this, BGCurve.EventPointField) : null, sender: this);
        }

        public void SetQuaternion(string name, Quaternion value)
        {
            curve.FireBeforeChange(BGCurve.EventPointField);

            PrivateValuesForFields.quaternionValues[curve.IndexOfFieldValue(name)] = value;

            curve.FireChange(curve.UseEventsArgs ? BGCurveChangedArgs.GetInstance(Curve, this, BGCurve.EventPointField) : null, sender: this);
        }

        public void SetBounds(string name, Bounds value)
        {
            curve.FireBeforeChange(BGCurve.EventPointField);

            PrivateValuesForFields.boundsValues[curve.IndexOfFieldValue(name)] = value;

            curve.FireChange(curve.UseEventsArgs ? BGCurveChangedArgs.GetInstance(Curve, this, BGCurve.EventPointField) : null, sender: this);
        }

        public void SetColor(string name, Color value)
        {
            curve.FireBeforeChange(BGCurve.EventPointField);

            PrivateValuesForFields.colorValues[curve.IndexOfFieldValue(name)] = value;

            curve.FireChange(curve.UseEventsArgs ? BGCurveChangedArgs.GetInstance(Curve, this, BGCurve.EventPointField) : null, sender: this);
        }

        #endregion

        #region Misc public methods

        //================================================================================
        //                                                    Misc public functions
        //================================================================================

        public override string ToString()
        {
            return "Point [localPosition=" + positionLocal + "]";
        }

        #endregion

        #region private methods

        //set position
        private void SetPosition(Vector3 value, bool worldSpaceIsUsed = false)
        {
            curve.FireBeforeChange(BGCurve.EventPointPosition);

            //snapping
            if (curve.SnapType != BGCurve.SnapTypeEnum.Off)
            {
                if (worldSpaceIsUsed) curve.ApplySnapping(ref value);
                else
                {
                    //we need to transfer space before applying snapping
                    var pos = curve.transform.TransformPoint(value);
                    if (curve.ApplySnapping(ref pos)) value = curve.transform.InverseTransformPoint(pos);
                }
            }


            //assign position
            if (pointTransform != null)
            {
                //2d mode with curve's transform changed is not working correctly
                if (curve.Mode2D != BGCurve.Mode2DEnum.Off) value = curve.Apply2D(value);
                pointTransform.position = worldSpaceIsUsed ? value : curve.transform.TransformPoint(value);
            }
            else
            {
                switch (Curve.PointsMode)
                {
                    case BGCurve.PointsModeEnum.GameObjectsNoTransform:
                        if (worldSpaceIsUsed)
                        {
                            var localPos = curve.transform.InverseTransformPoint(value);
                            if (curve.Mode2D != BGCurve.Mode2DEnum.Off) localPos = curve.Apply2D(localPos);
                            positionLocal = localPos;
                        }
                        else
                        {
                            if (curve.Mode2D != BGCurve.Mode2DEnum.Off) value = curve.Apply2D(value);
                            positionLocal = value;
                        }
                        break;
                    case BGCurve.PointsModeEnum.GameObjectsTransform:
                        if (worldSpaceIsUsed)
                        {
                            if (curve.Mode2D != BGCurve.Mode2DEnum.Off) value = curve.transform.TransformPoint(curve.Apply2D(curve.transform.InverseTransformPoint(value)));
                            transform.position = value;
                        }
                        else
                        {
                            if (curve.Mode2D != BGCurve.Mode2DEnum.Off) value = curve.Apply2D(value);
                            transform.position = curve.transform.TransformPoint(value);
                        }
                        break;
                    default:
                        throw WrongMode();
                }
            }

            curve.FireChange(curve.UseEventsArgs ? BGCurveChangedArgs.GetInstance(Curve, this, BGCurve.EventPointPosition) : null, sender: this);
        }


        //set local control 1
        private void SetControlFirstLocal(Vector3 value)
        {
            curve.FireBeforeChange(BGCurve.EventPointControl);

            if (curve.Mode2D != BGCurve.Mode2DEnum.Off) value = curve.Apply2D(value);

            if (controlType == BGCurvePoint.ControlTypeEnum.BezierSymmetrical) controlSecondLocal = -value;

            controlFirstLocal = value;
            curve.FireChange(curve.UseEventsArgs ? BGCurveChangedArgs.GetInstance(Curve, this, BGCurve.EventPointControl) : null, sender: this);
        }

        //set local control 2 (it's basically copy/paste from SetControlFirstLocal, but we can not use delegates here because of performance)
        private void SetControlSecondLocal(Vector3 value)
        {
            curve.FireBeforeChange(BGCurve.EventPointControl);

            if (curve.Mode2D != BGCurve.Mode2DEnum.Off) value = curve.Apply2D(value);

            if (controlType == BGCurvePoint.ControlTypeEnum.BezierSymmetrical) controlFirstLocal = -value;

            controlSecondLocal = value;
            curve.FireChange(curve.UseEventsArgs ? BGCurveChangedArgs.GetInstance(Curve, this, BGCurve.EventPointControl) : null, sender: this);
        }

        #endregion

        #region Not copy pasted

        //=================================================================================
        //                                                    This is not copy/pasted part
        //=================================================================================

        /// <summary>all methods, prefixed with Private, are not meant to be called from outside of BGCurve package </summary>
        //Init with data. No events are fired. point==null for pointsMode switching. 
        public void PrivateInit(BGCurvePoint point, BGCurve.PointsModeEnum pointsMode)
        {
            if (point != null)
            {
                // init from new point
                curve = point.Curve;
                controlType = point.ControlType;
                pointTransform = point.PointTransform;

                switch (pointsMode)
                {
                    case BGCurve.PointsModeEnum.GameObjectsNoTransform:
                        positionLocal = point.PositionLocal;
                        controlFirstLocal = point.ControlFirstLocal;
                        controlSecondLocal = point.ControlSecondLocal;
                        break;
                    case BGCurve.PointsModeEnum.GameObjectsTransform:
                        transform.localPosition = point.PositionLocal;

                        //transformed locals are always the same
                        var targetTransform = pointTransform != null ? pointTransform : transform;
                        controlFirstLocal = targetTransform.InverseTransformVector(point.ControlFirstLocalTransformed);
                        controlSecondLocal = targetTransform.InverseTransformVector(point.ControlSecondLocalTransformed);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("pointsMode", pointsMode, null);
                }
            }
            else
            {
                // change pointsMode
                Transform targetTransform;
                switch (pointsMode)
                {
                    case BGCurve.PointsModeEnum.GameObjectsNoTransform:
                    {
                        if (Curve.PointsMode != BGCurve.PointsModeEnum.GameObjectsTransform)
                            throw new ArgumentOutOfRangeException("Curve.PointsMode", "Curve points mode should be equal to GameObjectsTransform");

                        positionLocal = transform.localPosition;

                        //transformed locals are always the same
                        targetTransform = pointTransform != null ? pointTransform : curve.transform;
                        break;
                    }
                    case BGCurve.PointsModeEnum.GameObjectsTransform:
                    {
                        if (Curve.PointsMode != BGCurve.PointsModeEnum.GameObjectsNoTransform)
                            throw new ArgumentOutOfRangeException("Curve.PointsMode", "Curve points mode should be equal to GameObjectsNoTransform");

                        transform.position = PositionWorld;

                        //transformed locals are always the same
                        targetTransform = pointTransform != null ? pointTransform : transform;
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException("pointsMode", pointsMode, null);
                }

                controlFirstLocal = targetTransform.InverseTransformVector(ControlFirstLocalTransformed);
                controlSecondLocal = targetTransform.InverseTransformVector(ControlSecondLocalTransformed);

            }
        }


        //creates wrong pointMode exception
        private static ArgumentOutOfRangeException WrongMode()
        {
            return new ArgumentOutOfRangeException("Curve.PointsMode");
        }

        #endregion
    }
}