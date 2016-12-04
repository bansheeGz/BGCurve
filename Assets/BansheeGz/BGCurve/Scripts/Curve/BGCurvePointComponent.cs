using System;
using UnityEngine;

namespace BansheeGz.BGSpline.Curve
{
    /// <summary>Point, attached to a separate Component (MonoBehaviour)</summary>
    // this class uses composition pattern
    public class BGCurvePointComponent : MonoBehaviour, BGCurvePointI
    {
        [SerializeField] private BGCurvePoint point;

        public BGCurve Curve
        {
            get { return point.Curve; }
        }

        public Vector3 PositionLocal
        {
            get { return point.PositionLocal; }
            set { point.PositionLocal = value; }
        }

        public Vector3 PositionLocalTransformed
        {
            get { return point.PositionLocalTransformed; }
            set { point.PositionLocalTransformed = value; }
        }

        public Vector3 PositionWorld
        {
            get { return point.PositionWorld; }
            set { point.PositionWorld = value; }
        }

        public Vector3 ControlFirstLocal
        {
            get { return point.ControlFirstLocal; }
            set { point.ControlFirstLocal = value; }
        }

        public Vector3 ControlFirstLocalTransformed
        {
            get { return point.ControlFirstLocalTransformed; }
            set { point.ControlFirstLocalTransformed = value; }
        }

        public Vector3 ControlFirstWorld
        {
            get { return point.ControlFirstWorld; }
            set { point.ControlFirstWorld = value; }
        }

        public Vector3 ControlSecondLocal
        {
            get { return point.ControlSecondLocal; }
            set { point.ControlSecondLocal = value; }
        }

        public Vector3 ControlSecondLocalTransformed
        {
            get { return point.ControlSecondLocalTransformed; }
            set { point.ControlSecondLocalTransformed = value; }
        }

        public Vector3 ControlSecondWorld
        {
            get { return point.ControlSecondWorld; }
            set { point.ControlSecondWorld = value; }
        }

        public BGCurvePoint.ControlTypeEnum ControlType
        {
            get { return point.ControlType; }
            set { point.ControlType = value; }
        }

        public Transform PointTransform
        {
            get { return point.PointTransform; }
            set { point.PointTransform = value; }
        }

        public float GetFloat(string name)
        {
            return point.GetFloat(name);
        }

        public bool GetBool(string name)
        {
            return point.GetBool(name);
        }

        public int GetInt(string name)
        {
            return point.GetInt(name);
        }

        public Vector3 GetVector3(string name)
        {
            return point.GetVector3(name);
        }

        public Quaternion GetQuaternion(string name)
        {
            return point.GetQuaternion(name);
        }

        public Bounds GetBounds(string name)
        {
            return point.GetBounds(name);
        }

        public Color GetColor(string name)
        {
            return point.GetColor(name);
        }

        public T GetField<T>(string name)
        {
            return point.GetField<T>(name);
        }

        public object GetField(string name, Type type)
        {
            return point.GetField(name, type);
        }

        public void SetField(string name, object value, Type type)
        {
            point.SetField(name, value, type);
        }

        public void SetField<T>(string name, T value)
        {
            point.SetField(name, value);
        }

        public void SetFloat(string name, float value)
        {
            point.SetFloat(name, value);
        }

        public void SetBool(string name, bool value)
        {
            point.SetBool(name, value);
        }

        public void SetInt(string name, int value)
        {
            point.SetInt(name, value);
        }

        public void SetVector3(string name, Vector3 value)
        {
            point.SetVector3(name, value);
        }

        public void SetQuaternion(string name, Quaternion value)
        {
            point.SetQuaternion(name, value);
        }

        public void SetBounds(string name, Bounds value)
        {
            point.SetBounds(name, value);
        }

        public void SetColor(string name, Color value)
        {
            point.SetColor(name, value);
        }

        public BGCurvePoint Point
        {
            get { return point; }
        }

        /// <summary>all methods, prefixed with Private, are not meant to be called from outside of BGCurve package </summary>
        // this should be called once at creating time
        public void PrivateInit(BGCurvePoint point)
        {
            this.point = point;
            hideFlags = HideFlags.HideInInspector;
        }

        public override string ToString()
        {
            return point == null ? "no data" : (point +" as Component");
        }
    }
}