using UnityEngine;
using BansheeGz.BGSpline.Curve;

namespace BansheeGz.BGSpline.Components
{
    /// <summary> Cc + math </summary>
    [RequireComponent(typeof (BGCcMath))]
    public abstract class BGCcWithMath : BGCc
    {
        //===============================================================================================
        //                                                    Fields (Not persistent)
        //===============================================================================================
        private BGCcMath math;

        public BGCcMath Math
        {
            get
            {
                //do not replace with ??
                if (math == null) math = GetComponent<BGCcMath>();
                return math;
            }
            set
            {
                if (value == null) return;
                math = value;
                SetParent(value);
            }
        }

        //===============================================================================================
        //                                                    Editor stuff
        //===============================================================================================
        public override string Error
        {
            get { return Math == null ? "Math is null" : null; }
        }
    }
}