using System;

namespace BansheeGz.BGSpline.Curve
{
    /// <summary>
    /// Curve's change information. It's used only if Use curve.UseEventsArgs=true
    /// 
    /// Multiple- muliple changes
    /// CurveTransform- curve transform changed
    /// Points- point(s) was added or removed or swapped, or 'closed' attribute changed
    /// Point - point position changed
    /// PointControl - point's controls changed
    /// PointControlType -point's control type changed
    /// </summary>
    public class BGCurveChangedArgs : EventArgs
    {
        public enum ChangeTypeEnum
        {
            Multiple,
            CurveTransform,
            Points,
            Point,
            PointControl,
            PointControlType,
            Fields,
        }

        private readonly ChangeTypeEnum changeType;

        private readonly BGCurve curve;
        private readonly BGCurvePoint point;
        private readonly BGCurveChangedArgs[] multipleChanges;

        public ChangeTypeEnum ChangeType
        {
            get { return changeType; }
        }

        public BGCurve Curve
        {
            get { return curve; }
        }

        public BGCurvePoint Point
        {
            get { return point; }
        }

        public BGCurveChangedArgs[] MultipleChanges
        {
            get { return multipleChanges; }
        }

        public BGCurveChangedArgs(BGCurve curve, ChangeTypeEnum changeType)
        {
            this.curve = curve;
            this.changeType = changeType;
        }

        public BGCurveChangedArgs(BGCurve curve, BGCurvePoint point, ChangeTypeEnum changeType) : this(curve, changeType)
        {
            this.point = point;
        }

        public BGCurveChangedArgs(BGCurve curve, BGCurveChangedArgs[] multipleChanges)
        {
            this.curve = curve;
            changeType = ChangeTypeEnum.Multiple;
            this.multipleChanges = multipleChanges;
        }

        protected bool Equals(BGCurveChangedArgs other)
        {
            return changeType == other.changeType && Equals(curve, other.curve) && Equals(point, other.point);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((BGCurveChangedArgs) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int) changeType;
                hashCode = (hashCode*397) ^ (curve != null ? curve.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (point != null ? point.GetHashCode() : 0);
                return hashCode;
            }
        }

        public class BeforeChange : EventArgs
        {
            public string Operation;

            public BeforeChange(string operation)
            {
                Operation = operation;
            }
        }
    }
}