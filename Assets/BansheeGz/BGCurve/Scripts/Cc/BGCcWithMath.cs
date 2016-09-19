using BansheeGz.BGSpline.Curve;
using UnityEngine;

namespace BansheeGz.BGSpline.Components
{
    /// <summary> BGCc + math </summary>
    [RequireComponent(typeof (BGCcMath))]
    public abstract class BGCcWithMath : BGCc
    {
        private BGCcMath math;

        public override string Error
        {
            get { return Math == null ? "Math is null" : null; }
        }


        public BGCcMath Math
        {
            get
            {
                //do not replace with ??
                if (math == null) math = GetComponent<BGCcMath>();
                return math;
            }
        }
    }
}