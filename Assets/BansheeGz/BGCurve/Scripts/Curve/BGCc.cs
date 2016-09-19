using System;
using System.Collections.Generic;
using UnityEngine;

namespace BansheeGz.BGSpline.Curve
{
    /// <summary>
    /// Beta
    /// Abstract superclass for components. 
    /// 
    /// Current Requirements: 
    ///     1) editor should be extended from BGCcEditor 
    ///     2) Should have a descriptor.
    ///     3) Only one RequireComponent of type BGCc (parent)
    /// 
    /// Cc stands for "Curve's component" 
    /// </summary>
    [RequireComponent(typeof (BGCurve))]
    [ExecuteInEditMode]
    public abstract class BGCc : MonoBehaviour
    {
        /// <summary>if component's fields changed </summary>
        public event EventHandler ChangedParams;

        public virtual string Info
        {
            get { return null; }
        }

        public virtual string Warning
        {
            get { return null; }
        }

        public virtual string Error
        {
            get { return null; }
        }


#if UNITY_EDITOR
        // ============================================== !!! This is editor only fields
#pragma warning disable 0414
        [SerializeField] private bool showHandles = true;
#pragma warning restore 0414
#endif

        public virtual bool SupportHandles
        {
            get { return false; }
        }

        public virtual bool SupportHandlesSettings
        {
            get { return false; }
        }

        //=============================================== Curve
        private BGCurve curve;

        public BGCurve Curve
        {
            get
            {
                //do not replace with ??
                if (curve == null) curve = GetComponent<BGCurve>();
                return curve;
            }
        }


        //=============================================== Parent
        [SerializeField] private BGCc parent;

        public void SetParent(BGCc parent)
        {
            this.parent = parent;
        }

        public T GetParent<T>() where T : BGCc
        {
            return (T) GetParent(typeof (T));
        }

        public BGCc GetParent(Type type)
        {
            if (parent != null) return parent;
            parent = (BGCc) GetComponent(type);
            return parent;
        }


        //=============================================== Name
        [SerializeField] private string ccName;

        public string CcName
        {
            get { return string.IsNullOrEmpty(ccName) ? "" + GetInstanceID() : ccName; }
            set { ccName = value; }
        }

        //=============================================== Transaction

        private int transactionLevel;

        //=============================================== Descriptor
        private CcDescriptor descriptor;


        public CcDescriptor Descriptor
        {
            get
            {
                //do not replace with ??
                if (descriptor == null) descriptor = GetDescriptor(GetType());
                return descriptor;
            }
        }

        public virtual string HelpURL
        {
            get
            {
                var helpUrl = GetHelpUrl(GetType());
                return helpUrl == null ? null : helpUrl.URL;
            }
        }

        //=================================================== Unity Methods
        public virtual void Start()
        {
        }

        public virtual void OnDestroy()
        {
        }

        //=================================================== Methods

        protected bool ParamChanged<T>(ref T oldValue, T newValue)
        {
            var oldValueNull = oldValue == null;
            var newValueNull = newValue == null;
            if (oldValueNull && newValueNull) return false;

            if (oldValueNull == newValueNull && oldValue.Equals(newValue)) return false;

            oldValue = newValue;
            FireChangedParams();
            return true;
        }


        /// <summary> if component has an error</summary>
        public bool HasError()
        {
            return !string.IsNullOrEmpty(Error);
        }

        public bool HasWarning()
        {
            return !string.IsNullOrEmpty(Warning);
        }


        protected string ChoseMessage(string baseError, Func<string> childError)
        {
            return !string.IsNullOrEmpty(baseError) ? baseError : childError();
        }

        /// <summary> if any  parameter changed</summary>
        public void FireChangedParams()
        {
            if (ChangedParams != null && transactionLevel == 0) ChangedParams(this, null);
        }

        /// <summary> component was added via editor menu</summary>
        public virtual void AddedInEditor()
        {
        }

        public Type GetParentClass()
        {
            return GetParentClass(GetType());
        }

        public static Type GetParentClass(Type ccType)
        {
            //gather required
            var requiredList = ccType.GetCustomAttributes(typeof (RequireComponent), true);
            if (requiredList.Length == 0) return null;

            var result = new List<Type>();
            foreach (var item  in requiredList)
            {
                var requiredComponent = (RequireComponent) item;
                CheckRequired(requiredComponent.m_Type0, result);
                CheckRequired(requiredComponent.m_Type1, result);
                CheckRequired(requiredComponent.m_Type2, result);
            }

            if (result.Count == 0) return null;
            if (result.Count > 1) throw new CcException(ccType + " has more than one parent (extended from BGCc class), calculated by RequireComponent attribute");
            return result[0];
        }

        private static void CheckRequired(Type type, List<Type> result)
        {
            if (type == null || type.IsAbstract || !type.IsClass || !type.IsSubclassOf(typeof (BGCc))) return;

            result.Add(type);
        }

        public static bool IsSingle(Type ccType)
        {
            return ccType.GetCustomAttributes(typeof (DisallowMultipleComponent), true).Length > 0;
        }

        public void Transaction(Action action)
        {
            transactionLevel++;
            try
            {
                action();
            }
            finally
            {
                transactionLevel--;
                if (transactionLevel == 0)
                {
                    if (ChangedParams != null) ChangedParams(this, null);
                }
            }
        }

        //======================== descriptor for Editor
        //for editor
        [AttributeUsage(AttributeTargets.Class)]
        public class CcDescriptor : Attribute
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string Image { get; set; }
        }

        public static CcDescriptor GetDescriptor(Type type)
        {
            var propertyInfos = type.GetCustomAttributes(typeof (CcDescriptor), false);
            if (propertyInfos.Length > 0) return (CcDescriptor) propertyInfos[0];
            return null;
        }

        private static HelpURLAttribute GetHelpUrl(Type type)
        {
            var propertyInfos = type.GetCustomAttributes(typeof (HelpURLAttribute), false);
            if (propertyInfos.Length > 0) return (HelpURLAttribute) propertyInfos[0];
            return null;
        }

        //======================== handles settings for Editor

        public class CcException : UnityException
        {
            public CcException(string message) : base(message)
            {
            }
        }
    }
}