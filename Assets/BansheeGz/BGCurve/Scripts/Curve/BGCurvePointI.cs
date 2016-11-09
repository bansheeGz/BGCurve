using System;
using UnityEngine;

namespace BansheeGz.BGSpline.Curve
{
    /// <summary> This is interface for one single curve's point</summary>
    public interface BGCurvePointI
    {
        /// <summary> The Curve, this point is attached to </summary>
        BGCurve Curve { get; }

        //====================================================================
        //                                                    system fields
        //====================================================================
        /// <summary> Local position. It's relative to curve's origin. All transformations are ignored</summary>
        Vector3 PositionLocal { get; set; }
        /// <summary> Local position. It's relative to curve's origin. All transformations are applied</summary>
        Vector3 PositionLocalTransformed { get; set; }
        /// <summary> World position.</summary>
        Vector3 PositionWorld { get; set; }

        /// <summary> Local position for first control (inbound). It's relative to point's position, all transformations are ignored</summary>
        Vector3 ControlFirstLocal { get; set; }
        /// <summary> Local position for first control (inbound). It's relative to point's position with all transformations applied</summary>
        Vector3 ControlFirstLocalTransformed { get; set; }
        /// <summary> World position for first control (inbound)</summary>
        Vector3 ControlFirstWorld { get; set; }

        /// <summary> Local position for second control (outbound). It's relative to point's position, all transformations are ignored</summary>
        Vector3 ControlSecondLocal { get; set; }
        /// <summary> Local position for second control (outbound). It's relative to point's position with all transformations applied</summary>
        Vector3 ControlSecondLocalTransformed { get; set; }
        /// <summary> World position for second control (outbound)</summary>
        Vector3 ControlSecondWorld { get; set; }

        /// <summary> Point's controls type.</summary>
        BGCurvePoint.ControlTypeEnum ControlType { get; set; }

        /// <summary> Use this transform as point's position.</summary>
        Transform PointTransform { get; set; }

        //====================================================================
        //                                                    custom fields 
        //====================================================================

        // we need overriden getters/setters for structs/primitives to avoid boxing/unboxing

        //------------------------------- Getters
        /// <summary> Get custom field value.</summary>
        T GetField<T>(string name);
        /// <summary> Get custom field value.</summary>
        object GetField(string name, Type type);

        /// <summary> Get float custom field value.</summary>
        float GetFloat(string name);

        /// <summary> Get bool custom field value.</summary>
        bool GetBool(string name);

        /// <summary> Get int custom field value.</summary>
        int GetInt(string name);

        /// <summary> Get Vector3 custom field value.</summary>
        Vector3 GetVector3(string name);

        /// <summary> Get Quaternion custom field value.</summary>
        Quaternion GetQuaternion(string name);

        /// <summary> Get Bounds custom field value.</summary>
        Bounds GetBounds(string name);

        /// <summary> Get Color custom field value.</summary>
        Color GetColor(string name);


        //------------------------------- Setters
        /// <summary> Set custom field value.</summary>
        void SetField<T>(string name, T value);
        /// <summary> Set custom field value.</summary>
        void SetField(string name, object value, Type type);

        /// <summary> Set float custom field value.</summary>
        void SetFloat(string name, float value);

        /// <summary> Set bool custom field value.</summary>
        void SetBool(string name, bool value);

        /// <summary> Set int custom field value.</summary>
        void SetInt(string name, int value);

        /// <summary> Set Vector3 custom field value.</summary>
        void SetVector3(string name, Vector3 value);

        /// <summary> Set Quaternion custom field value.</summary>
        void SetQuaternion(string name, Quaternion value);

        /// <summary> Set Bounds custom field value.</summary>
        void SetBounds(string name, Bounds value);

        /// <summary> Set Color custom field value.</summary>
        void SetColor(string name, Color value);
    }
}