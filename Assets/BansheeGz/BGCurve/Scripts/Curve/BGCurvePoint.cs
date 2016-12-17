using System;
using System.Collections.Generic;
using UnityEngine;

namespace BansheeGz.BGSpline.Curve
{
    /// <summary>One inlined point data</summary>
    // !!! Note, there is a  BGCurvePointGO class, partially copied from this class
    [Serializable]
    public class BGCurvePoint : BGCurvePointI
    {

        #region enums

        /// <summary>possible point's control types</summary>
        public enum ControlTypeEnum
        {
            /// <summary>no control point</summary>
            Absent,

            /// <summary>2 points, symmetrical to each other</summary>
            BezierSymmetrical,

            /// <summary>2 points, independant</summary>
            BezierIndependant
        }

        /// <summary>helper enum for system fields</summary>
        public enum FieldEnum
        {
            PositionWorld,
            PositionLocal,
            ControlFirstWorld,
            ControlFirstLocal,
            ControlSecondWorld,
            ControlSecondLocal
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
            : this(curve, null , position, controlType, controlFirst, controlSecond, useWorldCoordinates)
        {
        }

        /// <summary> All coordinates are Local by default. positionLocal relative to curve's transform, controls are relative to positionLocal. Set useWorldCoordinates to true to use world coordinates</summary>
        public BGCurvePoint(BGCurve curve, Transform pointTransform, Vector3 position, ControlTypeEnum controlType, Vector3 controlFirst, Vector3 controlSecond, bool useWorldCoordinates = false )
        {
            this.curve = curve;
            this.controlType = controlType;
            this.pointTransform = pointTransform;

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

        #region fields

        //control type
        [SerializeField] private ControlTypeEnum controlType;

        //relative to curve position
        [SerializeField] private Vector3 positionLocal;

        //relative to point position
        [SerializeField] private Vector3 controlFirstLocal;
        [SerializeField] private Vector3 controlSecondLocal;

        [SerializeField] private Transform pointTransform;


        //point's curve
        [SerializeField] private BGCurve curve;

        //custom fields values for all points. it's an array with only one element. the reason why we store it like this- is to reduce storage and serialization costs.
        [SerializeField] private FieldsValues[] fieldsValues;

        /// <summary>The curve, point's belong to</summary>
        public BGCurve Curve
        {
            get { return curve; }
        }

        /// <summary>This field is not meant for use outside of BGCurve package </summary>
        //all fields values
        public FieldsValues PrivateValuesForFields
        {
            get
            {
                if (fieldsValues == null || fieldsValues.Length < 1 || fieldsValues[0] == null) fieldsValues = new[] {new FieldsValues()};
                return fieldsValues[0];
            }
            set
            {
                if (fieldsValues == null || fieldsValues.Length < 1 || fieldsValues[0] == null) fieldsValues = new[] {new FieldsValues()};
                fieldsValues[0] = value;
            }
        }

        // =============================================== Position
        //see interface for comments
        public Vector3 PositionLocal
        {
            get { return pointTransform == null ? positionLocal : curve.transform.InverseTransformPoint(pointTransform.position); }
            set { SetPosition(value); }
        }

        //see interface for comments
        public Vector3 PositionLocalTransformed
        {
            get
            {
                return pointTransform == null
                    ? curve.transform.TransformPoint(positionLocal) - curve.transform.position
                    : pointTransform.position - curve.transform.position;
            }
            set { SetPosition(value + curve.transform.position, true); }
        }

        //see interface for comments
        public Vector3 PositionWorld
        {
            get { return pointTransform == null ? curve.transform.TransformPoint(positionLocal) : pointTransform.position; }
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
            get { return (pointTransform == null ? curve.transform : pointTransform).TransformVector(controlFirstLocal); }
            set
            {
                var transform = pointTransform == null ? curve.transform : pointTransform;
                SetControlFirstLocal(transform.InverseTransformVector(value));
            }
        }

        //see interface for comments
        public Vector3 ControlFirstWorld
        {
            get
            {
                if (pointTransform == null)
                    return curve.transform.TransformPoint(new Vector3(positionLocal.x + controlFirstLocal.x, positionLocal.y + controlFirstLocal.y, positionLocal.z + controlFirstLocal.z));

                return pointTransform.position + pointTransform.TransformVector(controlFirstLocal);
            }
            set
            {
                var pos = pointTransform == null ? curve.transform.InverseTransformPoint(value) - positionLocal : pointTransform.InverseTransformVector(value - pointTransform.position);
                SetControlFirstLocal(pos);
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
            get { return (pointTransform == null ? curve.transform : pointTransform).TransformVector(controlSecondLocal); }
            set
            {
                var transform = pointTransform == null ? curve.transform : pointTransform;
                SetControlSecondLocal(transform.InverseTransformVector(value));
            }
        }


        //see interface for comments
        public Vector3 ControlSecondWorld
        {
            get
            {
                if (pointTransform == null)
                    return curve.transform.TransformPoint(new Vector3(positionLocal.x + controlSecondLocal.x, positionLocal.y + controlSecondLocal.y, positionLocal.z + controlSecondLocal.z));

                return pointTransform.position + pointTransform.TransformVector(controlSecondLocal);
            }
            set
            {
                var pos = pointTransform == null ? curve.transform.InverseTransformPoint(value) - positionLocal : pointTransform.InverseTransformVector(value - pointTransform.position);
                SetControlSecondLocal(pos);
            }
        }


        // =============================================== Control type
        //see interface for comments
        public ControlTypeEnum ControlType
        {
            get { return controlType; }
            set
            {
                if (controlType == value) return;

                curve.FireBeforeChange(BGCurve.EventPointControlType);

                controlType = value;

                if (controlType == ControlTypeEnum.BezierSymmetrical) controlSecondLocal = -controlFirstLocal;

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
                    positionLocal = curve.transform.InverseTransformPoint(positionWorld);
                    controlFirstLocal = curve.transform.InverseTransformVector(control1);
                    controlSecondLocal = curve.transform.InverseTransformVector(control2);
                }


                // inform curve
                if (oldTransformNull) curve.PrivateTransformForPointAdded(curve.IndexOf(this));
                else if (newTransformNull) curve.PrivateTransformForPointRemoved(curve.IndexOf(this));

                curve.FireChange(curve.UseEventsArgs ? BGCurveChangedArgs.GetInstance(Curve, this, BGCurve.EventPointTransform) : null, sender: this);
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
            return FieldTypes.GetField(curve, type, name, PrivateValuesForFields);
        }

        //----------------------------------- Setters
        public void SetField<T>(string name, T value)
        {
            SetField(name, value, typeof(T));
        }

        public void SetField(string name, object value, Type type)
        {
            curve.FireBeforeChange(BGCurve.EventPointField);

            FieldTypes.SetField(curve, type, name, value, PrivateValuesForFields);

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

        //see interface for comments

        public override string ToString()
        {
            return "Point [localPosition=" + positionLocal + "]";
        }

        #endregion

        #region private methods

        //set local position
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
            if (pointTransform == null)
            {
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
            }
            else
            {
                //2d mode with curve's transform changed is not working correctly
                if (curve.Mode2D != BGCurve.Mode2DEnum.Off) value = curve.Apply2D(value);
                pointTransform.position = worldSpaceIsUsed ? value : curve.transform.TransformPoint(value);
            }

            curve.FireChange(curve.UseEventsArgs ? BGCurveChangedArgs.GetInstance(Curve, this, BGCurve.EventPointPosition) : null, sender: this);
        }

        //set local control 1
        private void SetControlFirstLocal(Vector3 value)
        {
            curve.FireBeforeChange(BGCurve.EventPointControl);

            if (curve.Mode2D != BGCurve.Mode2DEnum.Off) value = curve.Apply2D(value);

            if (controlType == ControlTypeEnum.BezierSymmetrical) controlSecondLocal = -value;

            controlFirstLocal = value;
            curve.FireChange(curve.UseEventsArgs ? BGCurveChangedArgs.GetInstance(Curve, this, BGCurve.EventPointControl) : null, sender: this);
        }

        //set local control 2 (it's basically copy/paste from SetControlFirstLocal, but we can not use delegates here because of performance)
        private void SetControlSecondLocal(Vector3 value)
        {
            curve.FireBeforeChange(BGCurve.EventPointControl);

            if (curve.Mode2D != BGCurve.Mode2DEnum.Off) value = curve.Apply2D(value);

            if (controlType == ControlTypeEnum.BezierSymmetrical) controlFirstLocal = -value;

            controlSecondLocal = value;
            curve.FireChange(curve.UseEventsArgs ? BGCurveChangedArgs.GetInstance(Curve, this, BGCurve.EventPointControl) : null, sender: this);
        }

        /// <summary>all methods, prefixed with Private, are not meant to be called from outside of BGCurve package </summary>
        // field deleted callback
        public static void PrivateFieldDeleted(BGCurvePointField field, int indexOfField, FieldsValues fieldsValues)
        {
            switch (field.Type)
            {
                case BGCurvePointField.TypeEnum.Bool:
                    Ensure(ref fieldsValues.boolValues);
                    fieldsValues.boolValues = BGCurve.Remove(fieldsValues.boolValues, indexOfField);
                    break;
                case BGCurvePointField.TypeEnum.Int:
                    Ensure(ref fieldsValues.intValues);
                    fieldsValues.intValues = BGCurve.Remove(fieldsValues.intValues, indexOfField);
                    break;
                case BGCurvePointField.TypeEnum.Float:
                    Ensure(ref fieldsValues.floatValues);
                    fieldsValues.floatValues = BGCurve.Remove(fieldsValues.floatValues, indexOfField);
                    break;
                case BGCurvePointField.TypeEnum.Vector3:
                    Ensure(ref fieldsValues.vector3Values);
                    fieldsValues.vector3Values = BGCurve.Remove(fieldsValues.vector3Values, indexOfField);
                    break;
                case BGCurvePointField.TypeEnum.Bounds:
                    Ensure(ref fieldsValues.boundsValues);
                    fieldsValues.boundsValues = BGCurve.Remove(fieldsValues.boundsValues, indexOfField);
                    break;
                case BGCurvePointField.TypeEnum.Color:
                    Ensure(ref fieldsValues.colorValues);
                    fieldsValues.colorValues = BGCurve.Remove(fieldsValues.colorValues, indexOfField);
                    break;
                case BGCurvePointField.TypeEnum.String:
                    Ensure(ref fieldsValues.stringValues);
                    fieldsValues.stringValues = BGCurve.Remove(fieldsValues.stringValues, indexOfField);
                    break;
                case BGCurvePointField.TypeEnum.Quaternion:
                    Ensure(ref fieldsValues.quaternionValues);
                    fieldsValues.quaternionValues = BGCurve.Remove(fieldsValues.quaternionValues, indexOfField);
                    break;
                case BGCurvePointField.TypeEnum.AnimationCurve:
                    Ensure(ref fieldsValues.animationCurveValues);
                    fieldsValues.animationCurveValues = BGCurve.Remove(fieldsValues.animationCurveValues, indexOfField);
                    break;
                case BGCurvePointField.TypeEnum.GameObject:
                    Ensure(ref fieldsValues.gameObjectValues);
                    fieldsValues.gameObjectValues = BGCurve.Remove(fieldsValues.gameObjectValues, indexOfField);
                    break;
                case BGCurvePointField.TypeEnum.Component:
                    Ensure(ref fieldsValues.componentValues);
                    fieldsValues.componentValues = BGCurve.Remove(fieldsValues.componentValues, indexOfField);
                    break;
                case BGCurvePointField.TypeEnum.BGCurve:
                    Ensure(ref fieldsValues.bgCurveValues);
                    fieldsValues.bgCurveValues = BGCurve.Remove(fieldsValues.bgCurveValues, indexOfField);
                    break;
                case BGCurvePointField.TypeEnum.BGCurvePointComponent:
                    Ensure(ref fieldsValues.bgCurvePointComponentValues);
                    fieldsValues.bgCurvePointComponentValues = BGCurve.Remove(fieldsValues.bgCurvePointComponentValues, indexOfField);
                    break;
                case BGCurvePointField.TypeEnum.BGCurvePointGO:
                    Ensure(ref fieldsValues.bgCurvePointGOValues);
                    fieldsValues.bgCurvePointGOValues = BGCurve.Remove(fieldsValues.bgCurvePointGOValues, indexOfField);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("field.Type", field.Type, "Unsupported type " + field.Type);
            }
        }

        /// <summary>all methods, prefixed with Private, are not meant to be called from outside of BGCurve package </summary>
        // field added callback
        public static void PrivateFieldAdded(BGCurvePointField field, FieldsValues fieldsValues)
        {
            var type = FieldTypes.GetType(field.Type);
            var item = BGReflectionAdapter.IsValueType(type) ? Activator.CreateInstance(type) : null;

            switch (field.Type)
            {
                case BGCurvePointField.TypeEnum.Bool:
                    Ensure(ref fieldsValues.boolValues);
                    fieldsValues.boolValues = BGCurve.Insert(fieldsValues.boolValues, fieldsValues.boolValues.Length, (bool) item);
                    break;
                case BGCurvePointField.TypeEnum.Int:
                    Ensure(ref fieldsValues.intValues);
                    fieldsValues.intValues = BGCurve.Insert(fieldsValues.intValues, fieldsValues.intValues.Length, (int) item);
                    break;
                case BGCurvePointField.TypeEnum.Float:
                    Ensure(ref fieldsValues.floatValues);
                    fieldsValues.floatValues = BGCurve.Insert(fieldsValues.floatValues, fieldsValues.floatValues.Length, (float) item);
                    break;
                case BGCurvePointField.TypeEnum.Vector3:
                    Ensure(ref fieldsValues.vector3Values);
                    fieldsValues.vector3Values = BGCurve.Insert(fieldsValues.vector3Values, fieldsValues.vector3Values.Length, (Vector3) item);
                    break;
                case BGCurvePointField.TypeEnum.Bounds:
                    Ensure(ref fieldsValues.boundsValues);
                    fieldsValues.boundsValues = BGCurve.Insert(fieldsValues.boundsValues, fieldsValues.boundsValues.Length, (Bounds) item);
                    break;
                case BGCurvePointField.TypeEnum.Color:
                    Ensure(ref fieldsValues.colorValues);
                    fieldsValues.colorValues = BGCurve.Insert(fieldsValues.colorValues, fieldsValues.colorValues.Length, (Color) item);
                    break;
                case BGCurvePointField.TypeEnum.String:
                    Ensure(ref fieldsValues.stringValues);
                    fieldsValues.stringValues = BGCurve.Insert(fieldsValues.stringValues, fieldsValues.stringValues.Length, (string) item);
                    break;
                case BGCurvePointField.TypeEnum.Quaternion:
                    Ensure(ref fieldsValues.quaternionValues);
                    fieldsValues.quaternionValues = BGCurve.Insert(fieldsValues.quaternionValues, fieldsValues.quaternionValues.Length, (Quaternion) item);
                    break;
                case BGCurvePointField.TypeEnum.AnimationCurve:
                    Ensure(ref fieldsValues.animationCurveValues);
                    fieldsValues.animationCurveValues = BGCurve.Insert(fieldsValues.animationCurveValues, fieldsValues.animationCurveValues.Length, (AnimationCurve) item);
                    break;
                case BGCurvePointField.TypeEnum.GameObject:
                    Ensure(ref fieldsValues.gameObjectValues);
                    fieldsValues.gameObjectValues = BGCurve.Insert(fieldsValues.gameObjectValues, fieldsValues.gameObjectValues.Length, (GameObject) item);
                    break;
                case BGCurvePointField.TypeEnum.Component:
                    Ensure(ref fieldsValues.componentValues);
                    fieldsValues.componentValues = BGCurve.Insert(fieldsValues.componentValues, fieldsValues.componentValues.Length, (Component) item);
                    break;
                case BGCurvePointField.TypeEnum.BGCurve:
                    Ensure(ref fieldsValues.bgCurveValues);
                    fieldsValues.bgCurveValues = BGCurve.Insert(fieldsValues.bgCurveValues, fieldsValues.bgCurveValues.Length, (BGCurve) item);
                    break;
                case BGCurvePointField.TypeEnum.BGCurvePointComponent:
                    Ensure(ref fieldsValues.bgCurvePointComponentValues);
                    fieldsValues.bgCurvePointComponentValues = BGCurve.Insert(fieldsValues.bgCurvePointComponentValues, fieldsValues.bgCurvePointComponentValues.Length, (BGCurvePointComponent) item);
                    break;
                case BGCurvePointField.TypeEnum.BGCurvePointGO:
                    Ensure(ref fieldsValues.bgCurvePointGOValues);
                    fieldsValues.bgCurvePointGOValues = BGCurve.Insert(fieldsValues.bgCurvePointGOValues, fieldsValues.bgCurvePointGOValues.Length, (BGCurvePointGO) item);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("field.Type", field.Type, "Unsupported type " + field.Type);
            }
        }

        //ensure list is not null
        private static void Ensure<T>(ref T[] array)
        {
            if (array == null) array = new T[0];
        }

        #endregion

        #region helper classes

        //================================================================================
        //                                                    Values for the fields
        //================================================================================
        /// <summary> All values for custom fields </summary>
        //the reason we store it like this- is to reduce memory and serialization cost
        [Serializable]
        public sealed class FieldsValues
        {
            // c#
            [SerializeField] public bool[] boolValues;
            [SerializeField] public int[] intValues;
            [SerializeField] public float[] floatValues;
            [SerializeField] public string[] stringValues;

            // Unity structs
            [SerializeField] public Vector3[] vector3Values;
            [SerializeField] public Bounds[] boundsValues;
            [SerializeField] public Color[] colorValues;
            [SerializeField] public Quaternion[] quaternionValues;

            // Unity objects
            [SerializeField] public AnimationCurve[] animationCurveValues;
            [SerializeField] public GameObject[] gameObjectValues;
            [SerializeField] public Component[] componentValues;

            // BGCurve
            [SerializeField] public BGCurve[] bgCurveValues;
            [SerializeField] public BGCurvePointComponent[] bgCurvePointComponentValues;
            [SerializeField] public BGCurvePointGO[] bgCurvePointGOValues;
        }

        //================================================================================
        //                                                    Types for the fields
        //================================================================================
        /// <summary> Types for custom fields </summary>
        public static class FieldTypes
        {
            private static readonly Dictionary<Type, Func<FieldsValues, int, object>> type2fieldGetter = new Dictionary<Type, Func<FieldsValues, int, object>>();
            private static readonly Dictionary<Type, Action<FieldsValues, int, object>> type2fieldSetter = new Dictionary<Type, Action<FieldsValues, int, object>>();
            private static readonly Dictionary<BGCurvePointField.TypeEnum, Type> type2Type = new Dictionary<BGCurvePointField.TypeEnum, Type>();

            static FieldTypes()
            {
                // All these getters/setters are used only for classes now. For structs and primitives there is overriden getXXX setXXX methods (to get rid of boxing/unboxing).
                //primitives
                Register(BGCurvePointField.TypeEnum.Bool, typeof(bool), (value, index) => value.boolValues[index], (value, index, o) => value.boolValues[index] = Convert.ToBoolean((object) o));
                Register(BGCurvePointField.TypeEnum.Int, typeof(int), (value, index) => value.intValues[index], (value, index, o) => value.intValues[index] = Convert.ToInt32((object) o));
                Register(BGCurvePointField.TypeEnum.Float, typeof(float), (value, index) => value.floatValues[index], (value, index, o) => value.floatValues[index] = Convert.ToSingle((object) o));

                //string
                Register(BGCurvePointField.TypeEnum.String, typeof(string), (value, index) => value.stringValues[index], (value, index, o) => value.stringValues[index] = (string) o);

                //unity structs and classes
                Register(BGCurvePointField.TypeEnum.Vector3, typeof(Vector3), (value, index) => value.vector3Values[index], (value, index, o) => value.vector3Values[index] = (Vector3) o);
                Register(BGCurvePointField.TypeEnum.Bounds, typeof(Bounds), (value, index) =>
                {
                    var r = value.boundsValues[index];
                    return r;
                }, (value, index, o) => value.boundsValues[index] = (Bounds) o);
                Register(BGCurvePointField.TypeEnum.Quaternion, typeof(Quaternion), (value, index) => value.quaternionValues[index],
                    (value, index, o) => value.quaternionValues[index] = (Quaternion) o);
                Register(BGCurvePointField.TypeEnum.Color, typeof(Color), (value, index) => value.colorValues[index], (value, index, o) => value.colorValues[index] = (Color) o);
                Register(BGCurvePointField.TypeEnum.AnimationCurve, typeof(AnimationCurve), (value, index) => value.animationCurveValues[index],
                    (value, index, o) => value.animationCurveValues[index] = (AnimationCurve) o);

                //unity GO and components
                Register(BGCurvePointField.TypeEnum.GameObject, typeof(GameObject), (value, index) => value.gameObjectValues[index],
                    (value, index, o) => value.gameObjectValues[index] = (GameObject) o);
                Register(BGCurvePointField.TypeEnum.Component, typeof(Component), (value, index) => value.componentValues[index],
                    (value, index, o) => value.componentValues[index] = (Component) o);

                //bg curve related
                Register(BGCurvePointField.TypeEnum.BGCurve, typeof(BGCurve), (value, index) => value.bgCurveValues[index], (value, index, o) => value.bgCurveValues[index] = (BGCurve) o);
                Register(BGCurvePointField.TypeEnum.BGCurvePointComponent, typeof(BGCurvePointComponent), (value, index) => value.bgCurvePointComponentValues[index],
                    (value, index, o) => value.bgCurvePointComponentValues[index] = (BGCurvePointComponent) o);
                Register(BGCurvePointField.TypeEnum.BGCurvePointGO, typeof(BGCurvePointGO), (value, index) => value.bgCurvePointGOValues[index],
                    (value, index, o) => value.bgCurvePointGOValues[index] = (BGCurvePointGO) o);
            }

            // register data about one type
            private static void Register(BGCurvePointField.TypeEnum typeEnum, Type type, Func<FieldsValues, int, object> getter, Action<FieldsValues, int, object> setter)
            {
                type2Type[typeEnum] = type;
                type2fieldGetter[type] = getter;
                type2fieldSetter[type] = setter;
            }

            /// <summary> Get c# type(class), used for value of custom field with type "type"</summary>
            public static Type GetType(BGCurvePointField.TypeEnum type)
            {
                return type2Type[type];
            }

            /// <summary> retrieve value for particular field with name "name" and type "type"</summary>
            public static object GetField(BGCurve curve, Type type, string name, FieldsValues values)
            {
                Func<FieldsValues, int, object> getter;
                if (!type2fieldGetter.TryGetValue(type, out getter)) throw new UnityException("Unsupported type for a field, type= " + type);

                return getter(values, IndexOfFieldRelative(curve, name));
            }

            /// <summary> set value for particular field with name "name" and type "type"</summary>
            public static void SetField(BGCurve curve, Type type, string name, object value, FieldsValues values)
            {
                Action<FieldsValues, int, object> setter;
                if (!type2fieldSetter.TryGetValue(type, out setter)) throw new UnityException("Unsupported type for a field, type= " + type);

                setter(values, IndexOfFieldRelative(curve, name), value);
            }

            //get the index of field's value within array of values
            private static int IndexOfFieldRelative(BGCurve curve, string name)
            {
                var result = curve.IndexOfFieldValue(name);

                if (result < 0) throw new UnityException("Can not find a field with name " + name);

                return result;
            }
        }

        #endregion
    }
}