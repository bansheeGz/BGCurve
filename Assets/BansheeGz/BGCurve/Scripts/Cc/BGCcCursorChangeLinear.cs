using System;
using UnityEngine;
using BansheeGz.BGSpline.Curve;

namespace BansheeGz.BGSpline.Components
{
    /// <summary>Change cursor position linearly</summary>
    [HelpURL("http://www.bansheegz.com/BGCurve/Cc/BGCcCursorChangeLinear")]
    [
        CcDescriptor(
            Description = "Change cursor position linearly",
            Name = "Cursor Change Linear",
            Image = "Assets/BansheeGz/BGCurve/Icons/Components/BGCcCursorChangeLinear123.png")
    ]
    [AddComponentMenu("BansheeGz/BGCurve/Components/BGCcCursorChangeLinear", 2)]
    public class BGCcCursorChangeLinear : BGCcWithCursor
    {
        public const float SpeedThreshold = 0.00001f;

        public enum OverflowControlEnum
        {
            Cycle = 0,
            PingPong = 1,
            Stop = 2,
        }


        [SerializeField] [Tooltip("Constant movement speed along the curve (Speed * Time.deltaTime).")] private float speed = 5;

        [SerializeField] [Tooltip("How to change speed, then curve's end reached.")] private OverflowControlEnum overflowControl;

        [SerializeField] [Tooltip("If curve's length changed, " +
                                  "cursor position be adjusted with curve's length to ensure visually constant speed along the curve. ")] private bool adjustByTotalLength;

        //misc
        private float oldLength;

        public OverflowControlEnum OverflowControl
        {
            get { return overflowControl; }
            set { if (ParamChanged(ref overflowControl, value)) Stopped = false; }
        }

        public float Speed
        {
            get { return speed; }
            set { ParamChanged(ref speed, value); }
        }

        public bool AdjustByTotalLength
        {
            get { return adjustByTotalLength; }
            set { ParamChanged(ref adjustByTotalLength, value); }
        }

        public bool Stopped { get; set; }

        public override void Start()
        {
            oldLength = Cursor.Math.GetDistance();
        }

        // Update is called once per frame
        private void Update()
        {
            if (Stopped || Mathf.Abs(speed) < SpeedThreshold) return;

            var cursor = Cursor;

            var distance = cursor.Distance;
            var newLength = 0f;
            if (adjustByTotalLength)
            {
                var math = cursor.Math;
                newLength = math.GetDistance();

                if (Math.Abs(newLength) > BGCurve.Epsilon && Math.Abs(oldLength) > BGCurve.Epsilon && Math.Abs(newLength - oldLength) > BGCurve.Epsilon) distance = distance*newLength/oldLength;
            }

            //change distance
            var newDistance = distance + speed*Time.deltaTime;

            // Check Overflows
            var totalDistance = cursor.Math.GetDistance();
            var lessThanZero = newDistance < 0;
            if (lessThanZero || newDistance > totalDistance)
            {
                switch (overflowControl)
                {
                    case OverflowControlEnum.Stop:
                        newDistance = lessThanZero ? 0 : totalDistance;
                        Stopped = true;
                        break;

                    case OverflowControlEnum.Cycle:
                        newDistance = lessThanZero ? totalDistance + newDistance : newDistance - totalDistance;
                        break;

                    case OverflowControlEnum.PingPong:
                        speed = -speed;
                        newDistance = lessThanZero ? -newDistance : totalDistance*2 - newDistance;
                        break;
                }

                if (newDistance < 0) newDistance = 0;
                else if (newDistance > totalDistance) newDistance = totalDistance;
            }

            cursor.Distance = newDistance;

            oldLength = newLength;
        }
    }
}