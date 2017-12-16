using System;
using UnityEngine;
using UnityEngine.Events;
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
    [AddComponentMenu("BansheeGz/BGCurve/Components/BGCcCursorChangeLinear")]

    //idea.. refactor it.. the whole class looks like a nightmare
    public class BGCcCursorChangeLinear : BGCcWithCursor
    {
        //===============================================================================================
        //                                                    Static & Enums
        //===============================================================================================
        /// <summary>Speed, that is so small, that it can be ignored </summary>
        public const float SpeedThreshold = 0.00001f;

        /// <summary>What to do, then cursor reaches first or last points</summary>
        public enum OverflowControlEnum
        {
            /// <summary>Cycle in the same direction. For example, if it reaches the end point, start from the first point again</summary>
            Cycle = 0,

            /// <summary>Change speed to -speed, and go in opposite direction</summary>
            PingPong = 1,

            /// <summary>Stop</summary>
            Stop = 2,
        }

        //===============================================================================================
        //                                                    Events (Not persistent)
        //===============================================================================================
        /// <summary>point was reached </summary>
        public event EventHandler<PointReachedArgs> PointReached;

        //===============================================================================================
        //                                                    Fields (persistent)
        //===============================================================================================

        [SerializeField] [Tooltip("Cursor will be moved in FixedUpdate instead of Update")] private bool useFixedUpdate;

        [SerializeField] [Tooltip("Constant movement speed along the curve (Speed * Time.deltaTime)." +
                                  "You can override this value for each point with speedField")] private float speed = 5;

        [SerializeField] [Tooltip("How to change speed, then curve's end reached.")] private OverflowControlEnum overflowControl;

        [SerializeField] [Tooltip("If curve's length changed, " +
                                  "cursor position be adjusted with curve's length to ensure visually constant speed along the curve. ")] private bool adjustByTotalLength;

        [SerializeField] [Tooltip("Field to store the speed between each point. It should be a float field.")] private BGCurvePointField speedField;


        [SerializeField] [Tooltip("Delay at each point. You can override this value for each point with delayField")] private float delay;

        [SerializeField] [Tooltip("Field to store the delays at points. It should be a float field.")] private BGCurvePointField delayField;

        [SerializeField] [Tooltip("Event is fired, then point is reached")] private PointReachedEvent pointReachedEvent = new PointReachedEvent();


        /// <summary>What to do, then cursor reaches first or last points</summary>
        public OverflowControlEnum OverflowControl
        {
            get { return overflowControl; }
            set
            {
                if (ParamChanged(ref overflowControl, value)) Stopped = false;
            }
        }

        /// <summary>Speed. It can be overriden for each point by SpeedField. The result speed is calculated as Speed * Time.deltaTime </summary>
        public float Speed
        {
            get { return speed; }
            set { ParamChanged(ref speed, value); }
        }

        /// <summary>Should we adjust cursor position by the total length of the curve</summary>
        public bool AdjustByTotalLength
        {
            get { return adjustByTotalLength; }
            set { ParamChanged(ref adjustByTotalLength, value); }
        }

        /// <summary>Custom field to get speed value. It should be a float field. If it's null, "Speed" property is used. The result speed is calculated as Speed * Time.deltaTime </summary>
        public BGCurvePointField SpeedField
        {
            get { return speedField; }
            set { ParamChanged(ref speedField, value); }
        }

        /// <summary>Delay at each point. It can be overriden for each point by DelayField. </summary>
        public float Delay
        {
            get { return delay; }
            set { ParamChanged(ref delay, value); }
        }

        /// <summary>Custom field to get delay value. It should be a float field. If it's null, "Delay" property is used. </summary>
        public BGCurvePointField DelayField
        {
            get { return delayField; }
            set { ParamChanged(ref delayField, value); }
        }

        //===============================================================================================
        //                                                    Fields (Not persistent)
        //===============================================================================================
        //curve's length at last calculation (only if adjustByTotalLength=true)
        private float oldLength;
        //is speed is reversed (if speed field is present)
        private bool speedReversed;
        //current section index (it's calculated only if it's required)
        private int currentSectionIndex;
        //delay started time 
        private float delayStarted = -1;
        //if speed was positive while delay occured
        private bool speedWasPositiveWhileDelayed;
        //this is a part of overall nightmare, this class is. 
        private bool skipZeroPoint;


        /// <summary>If it's stopped or moving</summary>
        public bool Stopped { get; set; }

        /// <summary>If speed field is present and actual speed is reversed at the moment. It can be the case if PingPong is used.</summary>
        public bool SpeedReversed
        {
            get { return speedReversed; }
        }

        /// <summary>Speed at current cursor position</summary>
        public float CurrentSpeed
        {
            get
            {
                if (Curve.PointsCount < 2) return 0;

                //no field
                if (speedField == null) return speed;

                //by field
                var speedAtPoint = Curve[Cursor.CalculateSectionIndex()].GetFloat(speedField.FieldName);
                return speedReversed ? -speedAtPoint : speedAtPoint;
            }
        }

        //===============================================================================================
        //                                                    Unity Callbacks
        //===============================================================================================
        public override void Start()
        {
            oldLength = Cursor.Math.GetDistance();
            if (Application.isPlaying && Curve.PointsCount > 1 && (delay > 0 || delayField != null || pointReachedEvent.GetPersistentEventCount() > 0 || PointReached != null))
                currentSectionIndex = Cursor.Math.Math.CalcSectionIndexByDistance(Cursor.Distance);
        }

        // Update is called once per frame
        private void Update()
        {
            if (useFixedUpdate) return;
            Step();
        }

        // fixed update may be called several times per frame or once per several frames 
        private void FixedUpdate()
        {
            if (!useFixedUpdate) return;
            Step();
        }

        private void Step()
        {
            if (Stopped || (speedField == null && Mathf.Abs(speed) < SpeedThreshold)) return;

            var pointsCount = Curve.PointsCount;
            if (pointsCount < 2) return;

            var cursor = Cursor;
            var math = cursor.Math.Math;
            var isPlaying = Application.isPlaying;

            //===================================================== cursor is delayed at point
            if (isPlaying && delayStarted >= 0 && !CheckIfDelayIsOver(math, cursor)) return;

            //we are delayed so no need to process movement- return
            if (delayStarted >= 0) return;


            //===================================================== cursor is moving
            //calculate adjustment if needed
            var distance = cursor.Distance;
            var newLength = 0f;
            if (adjustByTotalLength)
            {
                newLength = math.GetDistance();
                if (Math.Abs(newLength) > BGCurve.Epsilon && Math.Abs(oldLength) > BGCurve.Epsilon && Math.Abs(newLength - oldLength) > BGCurve.Epsilon) distance = distance*newLength/oldLength;
            }

            //--------------------------------  Check for new Delay
            var newSectionIndex = -1;

            //check for new delays (and fire events)
            var firingEvents = pointReachedEvent.GetPersistentEventCount() > 0 || PointReached != null;
            var checkDelay = isPlaying && (delay > 0 || delayField != null);
            if ((checkDelay || firingEvents) && CheckForNewDelay(math, distance, ref newSectionIndex, checkDelay, firingEvents)) return;

            //--------------------------------  calculate speed
            var currentSpeed = speed;
            if (speedField != null)
            {
                //we need to retrieve speed from a field value
                if (newSectionIndex == -1) newSectionIndex = math.CalcSectionIndexByDistance(distance);
                currentSpeed = Curve[newSectionIndex].GetFloat(speedField.FieldName);
                if (speedReversed) currentSpeed = -currentSpeed;
            }

            //--------------------------------  change distance
            var newDistance = distance + currentSpeed*Time.deltaTime;


            //--------------------------------  Check Overflows
            if (newDistance < 0 || newDistance > math.GetDistance()) Overflow(math, ref newDistance, currentSpeed >= 0, checkDelay, firingEvents);

            //assign new value
            cursor.Distance = newDistance;
            oldLength = newLength;
        }

        //===============================================================================================
        //                                                    Public functions
        //===============================================================================================
        /// <summary>Delay at specified point </summary>
        public float GetDelayAtPoint(int point)
        {
            return delayField == null ? delay : Curve[point].GetFloat(delayField.FieldName);
        }

        /// <summary>Speed at specified point </summary>
        public float GetSpeedAtPoint(int point)
        {
            return speedField == null ? speed : Curve[point].GetFloat(speedField.FieldName);
        }

        //===============================================================================================
        //                                                    Private functions
        //===============================================================================================
        //if delay required at given point
        private bool IsDelayRequired(int pointIndex)
        {
            var hasDelayField = delayField != null;
            return (!hasDelayField && delay > 0) || (hasDelayField && Curve[pointIndex].GetFloat(delayField.FieldName) > BGCurve.Epsilon);
        }

        //start delay at current point
        private void StartDelay(bool speedIsPositive)
        {
            delayStarted = Time.time;
            speedWasPositiveWhileDelayed = speedIsPositive;
        }

        //checks if cursor passed a point (current section is changed) and if new delay occured. Also fires events if needed
        private bool CheckForNewDelay(BGCurveBaseMath math, float distance, ref int newSectionIndex, bool checkDelay, bool firingEvents)
        {
            if (currentSectionIndex == 0 && skipZeroPoint) return false;
            if (!math.Curve.Closed && currentSectionIndex == math.Curve.PointsCount - 1) return false;

            newSectionIndex = math.CalcSectionIndexByDistance(distance);
            if (currentSectionIndex != newSectionIndex)
            {
                //section is changed (there could be several points between)

                //if speed was positive or negative?
                bool speedPositive;
                if (speedField == null) speedPositive = speed > 0;
                else
                {
                    speedPositive = Curve[currentSectionIndex].GetFloat(speedField.FieldName) > 0;
                    if (speedReversed) speedPositive = !speedPositive;
                }

                if (CheckDelayAtSectionChanged(newSectionIndex, checkDelay, firingEvents, speedPositive)) return true;
            }
            //no delay
            delayStarted = -1;

            return false;
        }

        //section is changed- check if delay required 
        private bool CheckDelayAtSectionChanged(int newSectionIndex, bool checkDelay, bool firingEvents, bool speedPositive)
        {
            var cursor = Cursor;
            var math = cursor.Math.Math;

            var lastPointIndex = Curve.PointsCount - 1;

            //now we need to iterate all passed points
            if (speedPositive)
            {
                //check for infinite loop (just in case)
                if (newSectionIndex > currentSectionIndex)
                {
                    for (var i = currentSectionIndex + 1; i <= newSectionIndex; i++)
                    {
                        if (firingEvents) FirePointReachedEvent(i);

                        if (checkDelay && CheckDelayAtPoint(math, cursor, i, speedPositive)) return true;
                    }
                }
            }
            else
            {
                //if speed is negative, the processing is slightly different

                if (currentSectionIndex == 0 && !Curve.Closed) currentSectionIndex = lastPointIndex;

                //check for infinite loop (just in case)
                if (newSectionIndex < currentSectionIndex)
                {
                    for (var i = currentSectionIndex; i > newSectionIndex; i--)
                    {
                        if (firingEvents) FirePointReachedEvent(i);

                        if (checkDelay && CheckDelayAtPoint(math, cursor, i, speedPositive)) return true;
                    }
                }
            }

            currentSectionIndex = newSectionIndex;
            return false;
        }

        //points was passed and we check if delay is required at this point
        private bool CheckDelayAtPoint(BGCurveBaseMath math, BGCcCursor cursor, int pointIndex, bool speedPositive)
        {
            if (IsDelayRequired(pointIndex))
            {
                //we gotta delay at this point
                currentSectionIndex = pointIndex;
                //move cursor straight to the point
                cursor.Distance = Curve.PointsCount - 1 == pointIndex && !Curve.Closed ? math.GetDistance() : math[pointIndex].DistanceFromStartToOrigin;

                // !!!!!!!  start delay 
                StartDelay(speedPositive);
                //we does not need to process movement (it will be ignored at any rate, cause we stick to the particular point)
                return true;
            }
            return false;
        }

        // overflow occurred
        private void Overflow(BGCurveBaseMath math, ref float newDistance, bool currentSpeedPositive, bool checkDelay, bool firingEvents)
        {
            var lessThanZero = newDistance < 0;
            var totalDistance = math.GetDistance();
            var lastPointIndex = Curve.PointsCount - 1;

            //we need to check delays. (all points up to boundary point)
            if (checkDelay || firingEvents)
            {
                if (currentSpeedPositive)
                {
                    //process all passed points
//                    var lastPointIndexToCheck = Curve.Closed ? lastPointIndex : lastPointIndex - 1;
                    var lastPointIndexToCheck = lastPointIndex;
                    if (currentSectionIndex != lastPointIndexToCheck) if (CheckDelayAtSectionChanged(lastPointIndexToCheck, checkDelay, firingEvents, true)) return;
                }
                else
                {
                    if (currentSectionIndex > 0)
                    {
                        if (CheckDelayAtSectionChanged(0, checkDelay, firingEvents, false)) return;
                    }

                    //this needs to be refactored
                    if (!skipZeroPoint)
                    {
                        if (checkDelay && CheckDelayAtPoint(math, Cursor, 0, false))
                        {
                            if (firingEvents) FirePointReachedEvent(0);
                            skipZeroPoint = true;
                            return;
                        }
                    }
                }
            }


            //overflow
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
                    if (speedField == null) speed = -speed;
                    speedReversed = !speedReversed;
                    currentSpeedPositive = !currentSpeedPositive;
                    newDistance = lessThanZero ? -newDistance : totalDistance*2 - newDistance;
                    break;
            }

            if (newDistance < 0) newDistance = 0;
            else if (newDistance > totalDistance) newDistance = totalDistance;


            //ok, we need to check delays.. once again. (single boundary point) 
            if (checkDelay || firingEvents)
            {
                if (Curve.Closed)
                {
                    if (skipZeroPoint)
                    {
                        skipZeroPoint = false;
                    }
                    else
                    {
                        if (firingEvents) FirePointReachedEvent(0);

                        currentSectionIndex = currentSpeedPositive ? 0 : lastPointIndex;

                        if (checkDelay && CheckDelayAtPoint(math, Cursor, 0, currentSpeedPositive)) return;
                    }
                }
                else
                {
                    if (lessThanZero)
                    {
                        //original speed was negative
                        if (skipZeroPoint)
                        {
                            skipZeroPoint = false;
                        }
                        else
                        {
                            currentSectionIndex = 0;
                            if (firingEvents) FirePointReachedEvent(0);
                            if (checkDelay && CheckDelayAtPoint(math, Cursor, 0, currentSpeedPositive)) return;
                        }
                    }
                    else
                    {
                        //original speed was positive
                        if (!Curve.Closed)
                        {
                            if (currentSpeedPositive)
                            {
                                //last->first
                                currentSectionIndex = 0;
                                if (firingEvents) FirePointReachedEvent(0);
                                if (checkDelay && CheckDelayAtPoint(math, Cursor, 0, currentSpeedPositive)) return;
                            }
                            else
                            {
                                //last->last (pingpong)
                                currentSectionIndex = lastPointIndex - 1;
                            }
                        }
                    }
                }
            }
        }

        //check if delay is over
        private bool CheckIfDelayIsOver(BGCurveBaseMath math, BGCcCursor cursor)
        {
            var pointsCountMinusOne = Curve.PointsCount - 1;

            if (adjustByTotalLength) oldLength = math.GetDistance();

            //curve is not closed and delayed at last point
            var delayAtLastPoint = !Curve.Closed && currentSectionIndex == pointsCountMinusOne;

            //curve may be changing, so we need to adjust a position anyway
            cursor.Distance = delayAtLastPoint ? math.GetDistance() : math[currentSectionIndex].DistanceFromStartToOrigin;
            var delayValue = GetDelayAtPoint(currentSectionIndex);

            // we are still delayed 
            if (!(Time.time - delayStarted > delayValue)) return false;

            var currentSpeed = speed;
            if (speedField != null)
            {
                //we need to retrieve speed from a field value
                currentSpeed = Curve[currentSectionIndex].GetFloat(speedField.FieldName);
            }

            // delay is over, start moving
            delayStarted = -1;
            if (speedWasPositiveWhileDelayed)
            {
                //                if (delayAtLastPoint) cursor.Distance = 0;
                //                else cursor.Distance += BGCurve.Epsilon;
                cursor.Distance += Mathf.Abs(currentSpeed * Time.deltaTime);
            }
            else
            {
                if (currentSectionIndex > 0)
                {
                    currentSectionIndex--;
                    cursor.Distance -= Mathf.Abs(currentSpeed * Time.deltaTime);
                }
                else
                {
                    if (!skipZeroPoint)
                    {
                        currentSectionIndex = pointsCountMinusOne;
                        cursor.Distance = math.GetDistance() - Mathf.Abs(currentSpeed * Time.deltaTime);
                    }
                }
            }
            return true;
        }

        //fire "point is reached" event
        private void FirePointReachedEvent(int pointIndex)
        {
            if (PointReached != null) PointReached(this, PointReachedArgs.GetInstance(pointIndex));

            //probably we could skip pointPassed.GetPersistentEventCount() check
            if (pointReachedEvent.GetPersistentEventCount() > 0) pointReachedEvent.Invoke(pointIndex);
        }

        //===============================================================================================
        //                                                    Unity Persistent event
        //===============================================================================================
        /// <summary>Fired when cursor reaches a point </summary>
        [Serializable]
        public class PointReachedEvent : UnityEvent<int>
        {
        }

        //===============================================================================================
        //                                                    C# not persistent event
        //===============================================================================================
        /// <summary>Fired when cursor reaches a point </summary>
        public class PointReachedArgs : EventArgs
        {
            private static readonly PointReachedArgs Instance = new PointReachedArgs();

            //point's index
            public int PointIndex { get; private set; }

            private PointReachedArgs()
            {
            }

            public static PointReachedArgs GetInstance(int index)
            {
                Instance.PointIndex = index;
                return Instance;
            }
        }
    }
}