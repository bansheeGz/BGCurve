using System;
using System.Collections.Generic;
using UnityEngine;

namespace BansheeGz.BGSpline.Curve
{
    /// <summary>
    /// Abstract superclass for components. 
    /// 
    /// Current Requirements: 
    ///     1) editor should be extended from BGCcEditor 
    ///     2) Should have a descriptor.
    ///     3) Only one RequireComponent of type BGCc (parent). Parent is another Cc component, this component is totally depends on and can not operate without it.
    /// 
    /// Cc stands for "Curve's component" 
    /// </summary>
    [RequireComponent(typeof(BGCurve))]
    public abstract class BGCc : MonoBehaviour
    {
        /// <summary>if component's parameters changed </summary>
        public event EventHandler ChangedParams;

        /// <summary>Any information Cc wants to provide (for editor)</summary>
        public virtual string Info
        {
            get { return null; }
        }

        /// <summary>Warning (for editor)</summary>
        public virtual string Warning
        {
            get { return null; }
        }

        /// <summary>Error if something is wrong (for editor)</summary>
        public virtual string Error
        {
            get { return null; }
        }


#if UNITY_EDITOR
        // ============================================== !!! This is editor only field
#pragma warning disable 0414
        //should CC's handles be shown in SceneView
        [SerializeField] private bool showHandles = true;

        [SerializeField] private bool hidden;

        public bool Hidden
        {
            get { return hidden; }
            set { hidden = value; }
        }

#pragma warning restore 0414
#endif

        /// <summary>Does this Cc supports handles in SceneView?</summary>
        public virtual bool SupportHandles
        {
            get { return false; }
        }

        /// <summary>Does this Cc supports some adjustable settings for handles in SceneView?</summary>
        public virtual bool SupportHandlesSettings
        {
            get { return false; }
        }

        public virtual bool HideHandlesInInspector
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
        //parent is another Cc component, this component is totally depends on and can not operate without it
        [SerializeField] private BGCc parent;

        public void SetParent(BGCc parent)
        {
            this.parent = parent;
        }

        public T GetParent<T>() where T : BGCc
        {
            return (T) GetParent(typeof(T));
        }

        public BGCc GetParent(Type type)
        {
            if (parent != null) return parent;
            parent = (BGCc) GetComponent(type);
            return parent;
        }


        //=============================================== Name
        //you can name your Cc
        [SerializeField] private string ccName;

        public string CcName
        {
            get { return string.IsNullOrEmpty(ccName) ? "" + GetInstanceID() : ccName; }
            set { ParamChanged(ref ccName, value); }
        }

        //=============================================== Transaction
        //transaction is for events grouping only
        private int transactionLevel;

        //=============================================== Descriptor
        //descriptor is used to add icon, name and description to Cc
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

        // www page, containing help info
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
        //in case any param changed
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

        /// <summary> if component has a warning</summary>
        public bool HasWarning()
        {
            return !string.IsNullOrEmpty(Warning);
        }

        //utility method for chosing error message
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

        /// <summary> get parent Cc class</summary>
        public Type GetParentClass()
        {
            return GetParentClass(GetType());
        }

        /// <summary> get parent Cc class</summary>
        public static Type GetParentClass(Type ccType)
        {
            //gather required
            var requiredList = BGReflectionAdapter.GetCustomAttributes(ccType, typeof(RequireComponent), true);
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

        //add class if it's not abstract and a child of BGCc
        private static void CheckRequired(Type type, List<Type> result)
        {
            if (type == null || BGReflectionAdapter.IsAbstract(type) || !BGReflectionAdapter.IsClass(type) || !BGReflectionAdapter.IsSubclassOf(type, typeof(BGCc))) return;

            result.Add(type);
        }

        /// <summary> Check standard Unity's DisallowMultipleComponent attribute </summary>
        public static bool IsSingle(Type ccType)
        {
            return BGReflectionAdapter.GetCustomAttributes(ccType, typeof(DisallowMultipleComponent), true).Length > 0;
        }

        /// <summary> This is used to group events. Use it to change several params and fire one single event</summary>
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
        /// <summary>this descriptor is used by editor</summary>
        [AttributeUsage(AttributeTargets.Class)]
        public class CcDescriptor : Attribute
        {
            /// <summary>Component's name</summary>
            public string Name { get; set; }

            /// <summary>Component's desciption</summary>
            public string Description { get; set; }

            /// <summary>Component's icon</summary>
            public string Image { get; set; }
        }

        /// <summary>Component will be excluded from Cc menu and Inspector menu</summary>
        [AttributeUsage(AttributeTargets.Class)]
        public class CcExcludeFromMenu : Attribute
        {
        }

        /// <summary>Retrieves the descriptor from "type"</summary>
        public static CcDescriptor GetDescriptor(Type type)
        {
            var propertyInfos = BGReflectionAdapter.GetCustomAttributes(type, typeof(CcDescriptor), false);
            if (propertyInfos.Length > 0) return (CcDescriptor) propertyInfos[0];
            return null;
        }

        // get Unity's HelpURLAttribute attrubute
        private static HelpURLAttribute GetHelpUrl(Type type)
        {
            var propertyInfos = BGReflectionAdapter.GetCustomAttributes(type, typeof(HelpURLAttribute), false);
            if (propertyInfos.Length > 0) return (HelpURLAttribute) propertyInfos[0];
            return null;
        }

        //======================== Exception
        /// <summary>Exception if something is wrong with Cc related stuff</summary>
        public class CcException : Exception
        {
            public CcException(string message) : base(message)
            {
            }
        }
    }
}