using UnityEngine;

namespace BansheeGz.BGSpline.Components
{
    /// <summary>CC + cursor + object to manipulate</summary>
    public abstract class BGCcWithCursorObject : BGCcWithCursor
    {
        //===============================================================================================
        //                                                    Static 
        //===============================================================================================
        //error message to use (probably can be inlined without any effect)
        private const string ErrorObjectNotSet = "Object To Manipulate is not set.";

        //===============================================================================================
        //                                                    Fields (Persistent)
        //===============================================================================================
        [SerializeField] [Tooltip("Object to manipulate.\r\n")] private Transform objectToManipulate;

        public Transform ObjectToManipulate
        {
            get { return objectToManipulate; }
            set { ParamChanged(ref objectToManipulate, value); }
        }

        //===============================================================================================
        //                                                    Editor stuff
        //===============================================================================================
        public override string Error
        {
            get { return objectToManipulate == null ? ErrorObjectNotSet : null; }
        }
    }
}