using UnityEngine;
using BansheeGz.BGSpline.Curve;

namespace BansheeGz.BGSpline.Components
{
    /// <summary>CC + cursor</summary>
    [RequireComponent(typeof (BGCcCursor))]
    public abstract class BGCcWithCursor : BGCc
    {
        private BGCcCursor cursor;

        public override string Error
        {
            get { return Cursor == null ? "Cursor is null" : null; }
        }


        public BGCcCursor Cursor
        {
            get
            {
                //do not replace with ??
                if (cursor == null) cursor = GetParent<BGCcCursor>();
                return cursor;
            }
        }
    }
}