using UnityEngine;
using BansheeGz.BGSpline.Curve;

namespace BansheeGz.BGSpline.Components
{
    /// <summary>CC + cursor</summary>
    [RequireComponent(typeof (BGCcCursor))]
    public abstract class BGCcWithCursor : BGCc
    {
        //===============================================================================================
        //                                                    Fields (Not persistent)
        //===============================================================================================
        //cursor Cc component
        private BGCcCursor cursor;

        public BGCcCursor Cursor
        {
            get
            {
                //do not replace with ??
                if (cursor == null) cursor = GetParent<BGCcCursor>();
                return cursor;
            }
        }

        //===============================================================================================
        //                                                    Editor stuff
        //===============================================================================================

        public override string Error
        {
            get { return Cursor == null ? "Cursor is null" : null; }
        }


        //===============================================================================================
        //                                                    Public methods
        //===============================================================================================
        /// <summary> Lerp 2 Quaternion field values by current cursor position (optionally currentSection is provided to reduce required calculation)</summary>
        public Quaternion LerpQuaternion(string fieldName, int currentSection = -1)
        {
            int indexFrom, indexTo;
            var t = GetT(out indexFrom, out indexTo, currentSection);

            //get values
            var from = Curve[indexFrom].GetQuaternion(fieldName);
            var to = Curve[indexTo].GetQuaternion(fieldName);

            //not sure how to handle zero cases
            if (from.x == 0 && from.y == 0 && from.z == 0 && from.w == 0) from = Quaternion.identity;
            if (to.x == 0 && to.y == 0 && to.z == 0 && to.w == 0) to = Quaternion.identity;

            //lerp
            var result = Quaternion.Lerp(@from, to, t);
            return float.IsNaN(result.x) || float.IsNaN(result.y) || float.IsNaN(result.z) || float.IsNaN(result.w) ? Quaternion.identity : result;
        }

        /// <summary> Lerp 2 Vector3 field values by current cursor position (optionally currentSection is provided to reduce required calculation)</summary>
        public Vector3 LerpVector(string name, int currentSection = -1)
        {
            int indexFrom, indexTo;
            var t = GetT(out indexFrom, out indexTo, currentSection);

            //get values
            var from = Curve[indexFrom].GetVector3(name);
            var to = Curve[indexTo].GetVector3(name);

            //lerp
            return Vector3.Lerp(@from, to, t);
        }

        /// <summary> get T value for interpolation (optionally currentSection is provided to reduce required calculation)</summary>
        public float GetT(out int indexFrom, out int indexTo, int currentSection = -1)
        {
            var math = Cursor.Math.Math;
            var distance = Cursor.Distance;

            GetFromToIndexes(out indexFrom, out indexTo, currentSection);

            //get t value
            var section = math[indexFrom];
            var t = (distance - section.DistanceFromStartToOrigin)/section.Distance;

            return t;
        }

        //===============================================================================================
        //                                                    Private methods
        //===============================================================================================
        // get points indexes by cursor position  (optionally currentSection is provided to reduce required calculation)
        protected void GetFromToIndexes(out int indexFrom, out int indexTo, int currentSection = -1)
        {
            indexFrom = currentSection < 0 ? Cursor.CalculateSectionIndex() : currentSection;
            indexTo = indexFrom == Curve.PointsCount - 1 ? 0 : indexFrom + 1;
        }
    }
}