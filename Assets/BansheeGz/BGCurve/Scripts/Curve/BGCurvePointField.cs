using System;
using UnityEngine;

namespace BansheeGz.BGSpline.Curve
{
    /// <summary>Meta data (name, type and settings) for custom point's field</summary>
    [Serializable]
    public class BGCurvePointField : MonoBehaviour
    {
        // all possible types for the field's value. 
        // Note, even if your desired type is not supported, 
        // you still can use it via Unity's standard Component type (assign any MonoBehaviour derived script to it) or via GameObject
        public enum TypeEnum
        {
            //C#
            Bool = 0,
            Int = 1,
            Float = 2,
            String = 3,

            //Unity structs
            Vector3 = 100,
            Bounds = 101,
            Color = 102,
            Quaternion = 103,

            //Unity classes
            AnimationCurve = 200,
            GameObject = 201,
            Component = 202,

            //BG related
            BGCurve = 300,
            BGCurvePointComponent = 301,
            BGCurvePointGO= 302
        }

        //BGCurve
        [SerializeField] private BGCurve curve;
        //field's name. It should be unique, not null, English chars only, 16 chars max
        [SerializeField] private string fieldName;
        //field's type
        [SerializeField] private TypeEnum type;

#if UNITY_EDITOR
        // ============================================== !!! This is editor ONLY fields
#pragma warning disable 0414

        [SerializeField] private bool showHandles = true;
        [SerializeField] private int handlesType;
        [SerializeField] private Color handlesColor = Color.white;
        [SerializeField] private bool showInPointsMenu = true;
#pragma warning restore 0414
#endif
        /// <summary>Field's name. It should be unique, not null, English chars only, 16 chars max</summary>
        public string FieldName
        {
            get { return fieldName; }
            set
            {
                if (string.Equals(FieldName, value)) return;
                CheckName(curve, value, true);

                curve.FireBeforeChange(BGCurve.EventFieldName);

                fieldName = value;
                curve.PrivateUpdateFieldsValuesIndexes();

                curve.FireChange(BGCurveChangedArgs.GetInstance(curve, BGCurveChangedArgs.ChangeTypeEnum.Fields, BGCurve.EventFieldName), sender: this);
            }
        }

        /// <summary>Field's type</summary>
        public TypeEnum Type
        {
            //type can not be changed
            get { return type; }
        }

        /// <summary>Owning curve</summary>
        public BGCurve Curve
        {
            get { return curve; }
        }


        /// <summary>Init the field. It should be called once at creation</summary>
        public void Init(BGCurve curve, string fieldName, TypeEnum type)
        {
            if (!string.IsNullOrEmpty(this.fieldName)) throw new UnityException("You can not init twice.");

            CheckName(curve, fieldName, true);
            this.curve = curve;
            this.fieldName = fieldName;
            this.type = type;
        }

        /// <summary>Check if name is ok. It should be unique, not null, English chars only, 16 chars max</summary>
        public static string CheckName(BGCurve curve, string name, bool throwException = false)
        {
            string error = null;
            if (string.IsNullOrEmpty(name)) error = "Field's name can not be null";
            else if (name.Length > 16) error = "Name should be 16 chars max. Current name has " + name.Length + " chars.";
            else
            {
                var firstChar = name[0];
                if (!((firstChar >= 'A' && firstChar <= 'Z') || (firstChar >= 'a' && firstChar <= 'z'))) error = "Name should start with a English letter.";
                else
                {
                    for (var i = 1; i < name.Length; i++)
                    {
                        var @char = name[i];
                        if ((@char >= 'A' && @char <= 'Z') || (@char >= 'a' && @char <= 'z') || (@char >= '0' && @char <= '9')) continue;

                        error = "Name should contain English letters or numbers only.";
                        break;
                    }
                    if (error==null && curve.HasField(name)) error = "Field with name '" + name + "' already exists.";
                }
            }

            if (throwException && error != null) throw new UnityException(error);

            return error;
        }

        //---------------------------------- Object overrides

        protected bool Equals(BGCurvePointField other)
        {
            return Equals(curve, other.curve) && string.Equals(fieldName, other.fieldName);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((BGCurvePointField) obj);
        }

        public override int GetHashCode()
        {
            return (curve != null ? curve.GetHashCode() : 0) ^ (fieldName != null ? fieldName.GetHashCode() : 0);
        }

        public override string ToString()
        {
            return fieldName;
        }
    }
}