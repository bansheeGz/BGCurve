using System;

namespace BansheeGz.BGSpline.Curve
{
    /// <summary>
    /// THIS NEED TO BE REWORKED.
    /// 
    /// Curve's change information. It's used only if Use curve.UseEventsArgs=true
    /// 
    /// Multiple- muliple changes
    /// CurveTransform- curve transform changed
    /// Points- point(s) was added or removed or swapped, or 'closed' attribute changed
    /// Point - point position changed
    /// PointControl - point's controls changed
    /// PointControlType -point's control type changed
    /// Fields -point's fields changed
    /// </summary>
    public class BGCurveChangedArgs : EventArgs, ICloneable
    {
        //reusable event instance
        private static readonly BGCurveChangedArgs Instance = new BGCurveChangedArgs();
       

        //all possible types of changes
        public enum ChangeTypeEnum
        {
            Multiple,
            CurveTransform,
            Points,
            Point,
            Fields,
            Snap,
            Curve
        }

        //type of the change
        private ChangeTypeEnum changeType;

        //changed curve
        private BGCurve curve;
        //changed point
        private BGCurvePointI point;
        //event message
        private string message;
        //multiple changes
        private BGCurveChangedArgs[] multipleChanges;

        /// <summary>Type of the change </summary>
        public ChangeTypeEnum ChangeType
        {
            get { return changeType; }
        }

        /// <summary>Changed curve</summary>
        public BGCurve Curve
        {
            get { return curve; }
        }

        /// <summary>Change message</summary>
        public string Message
        {
            get { return message; }
        }

        /// <summary>Multiple changes</summary>
        public BGCurveChangedArgs[] MultipleChanges
        {
            get { return multipleChanges; }
        }

        /*
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
        */

        private BGCurveChangedArgs()
        {
        }

        /// <summary>Init and get event instance</summary>
        public static BGCurveChangedArgs GetInstance(BGCurve curve, ChangeTypeEnum type, string message)
        {
            Instance.curve = curve;
            Instance.changeType = type;
            Instance.message = message;
            Instance.multipleChanges = null;
            Instance.point = null;
            return Instance;
        }

        /// <summary>Init and get event instance</summary>
        public static BGCurveChangedArgs GetInstance(BGCurve curve, BGCurveChangedArgs[] changes, string changesInTransaction)
        {
            Instance.curve = curve;
            Instance.changeType = ChangeTypeEnum.Multiple;
            Instance.message = BGCurve.EventTransaction;
            Instance.multipleChanges = changes;
            Instance.point = null;
            return Instance;
        }

        /// <summary>Init and get event instance</summary>
        public static BGCurveChangedArgs GetInstance(BGCurve curve, BGCurvePointI point, string changesInTransaction)
        {
            Instance.curve = curve;
            Instance.changeType = ChangeTypeEnum.Point;
            Instance.message = BGCurve.EventTransaction;
            Instance.point = point;
            return Instance;
        }

        /// <summary>Clone event</summary>
        public object Clone()
        {
            return new BGCurveChangedArgs
            {
                changeType = changeType,
                curve = curve,
                multipleChanges = multipleChanges,
                message = message,
                point = point
            };
        }

        protected bool Equals(BGCurveChangedArgs other)
        {
            return changeType == other.changeType && Equals(curve, other.curve) ;
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
            var hashCode = (int) changeType;
            hashCode = (hashCode*397) ^ (curve != null ? curve.GetHashCode() : 0);
            return hashCode;
        }

        /// <summary>Before change event</summary>
        public class BeforeChange : EventArgs
        {
            public static readonly BeforeChange BeforeChangeInstance = new BeforeChange();

            public string Operation;

            private BeforeChange()
            {
            }

            public static BeforeChange GetInstance(string operation)
            {
                BeforeChangeInstance.Operation = operation;
                return BeforeChangeInstance;
            }
        }
    }
}