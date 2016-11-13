using System;
using UnityEngine;
using System.Collections.Generic;
using BansheeGz.BGSpline.Components;
using BansheeGz.BGSpline.Curve;

namespace BansheeGz.BGSpline.Example
{
    //testing BGCcCursorChangeLinear (this test is automated)
    /*
     * Abbreviations for GameObjects names
     * F- Forward (speed positive)
     * B- Backward (speed negative)
     * Cycle- Cycle mode
     * PP- PingPong mode
     * Closed- closed curve
     * Open- Not closed curve
     * D0- delay at point 0
     * DL- delay at last point 
     * DA- delay at all points
     * SF- speed field is present
     * */

    public class BGTestCcChangeCursorLinear : MonoBehaviour
    {
        private readonly List<Sequence> sequences = new List<Sequence>(10);

        // Use this for initialization
        void Start()
        {
            var changeCursors = GetComponentsInChildren<BGCcCursorChangeLinear>(true);

            // for this to work we need to rename curves
//            Array.Sort(changeCursors, (o1, o2) => string.Compare(o1.gameObject.name, o2.gameObject.name));

            for (var i = 0; i < changeCursors.Length; i++) Register(changeCursors[i]);
        }

        private void Register(BGCcCursorChangeLinear curve)
        {
            sequences.Add(new Sequence(curve));
        }

        // Update is called once per frame
        void Update()
        {
            foreach (var sequence in sequences)
                if (sequence.Curve.gameObject.activeInHierarchy)
                {
                    sequence.Check();
                }
        }


        // no luck here. There shoould be some way to pass curve's number and get rid of this? 
        /*
                public void PointReached(int point, int curve)
                {
                    Process(curve, point);
                }
            */

        //Nigthmare start (idea.. we need to switch to non persistent events for this test, which send reference to a curve)
        public void PointReached0(int point)
        {
            Process(0, point);
        }

        public void PointReached1(int point)
        {
            Process(1, point);
        }

        public void PointReached2(int point)
        {
            Process(2, point);
        }

        public void PointReached3(int point)
        {
            Process(3, point);
        }

        public void PointReached4(int point)
        {
            Process(4, point);
        }

        public void PointReached5(int point)
        {
            Process(5, point);
        }

        public void PointReached6(int point)
        {
            Process(6, point);
        }

        public void PointReached7(int point)
        {
            Process(7, point);
        }

        public void PointReached8(int point)
        {
            Process(8, point);
        }

        public void PointReached9(int point)
        {
            Process(9, point);
        }

        public void PointReached10(int point)
        {
            Process(10, point);
        }

        public void PointReached11(int point)
        {
            Process(11, point);
        }

        public void PointReached12(int point)
        {
            Process(12, point);
        }

        public void PointReached13(int point)
        {
            Process(13, point);
        }

        public void PointReached14(int point)
        {
            Process(14, point);
        }

        public void PointReached15(int point)
        {
            Process(15, point);
        }

        public void PointReached16(int point)
        {
            Process(16, point);
        }

        public void PointReached17(int point)
        {
            Process(17, point);
        }

        public void PointReached18(int point)
        {
            Process(18, point);
        }

        public void PointReached19(int point)
        {
            Process(19, point);
        }

        public void PointReached20(int point)
        {
            Process(20, point);
        }

        public void PointReached21(int point)
        {
            Process(21, point);
        }

        public void PointReached22(int point)
        {
            Process(22, point);
        }

        public void PointReached23(int point)
        {
            Process(23, point);
        }

        public void PointReached24(int point)
        {
            Process(24, point);
        }

        public void PointReached25(int point)
        {
            Process(25, point);
        }

        public void PointReached26(int point)
        {
            Process(26, point);
        }

        public void PointReached27(int point)
        {
            Process(27, point);
        }

        public void PointReached28(int point)
        {
            Process(28, point);
        }

        public void PointReached29(int point)
        {
            Process(29, point);
        }

        public void PointReached30(int point)
        {
            Process(30, point);
        }

        public void PointReached31(int point)
        {
            Process(31, point);
        }

        public void PointReached32(int point)
        {
            Process(32, point);
        }


        private void Process(int curve, int pointIndex)
        {
            var sequence = sequences[curve];
            if (!sequence.Curve.gameObject.activeSelf) return;

//            print("Reached {" + pointIndex + "} for curve =" + curve);

            sequence.Reached(pointIndex);
        }

        private sealed class Sequence
        {
            //precision does not matter too much idea.. lower the value, something wrong here
            public const float Epsilon = 0.1f;

            private readonly List<ExpectedPoint> expectedPoints = new List<ExpectedPoint>();
            private BGCcCursorChangeLinear changeCursor;

            private int pointCursor;
            private bool valid = true;
            private float lastPoint;

            private float started;

            public BGCurve Curve;


            private float Elapsed
            {
                get { return Time.time - started; }
            }

            public Sequence(BGCcCursorChangeLinear changeCursor)
            {
                this.changeCursor = changeCursor;
                var cursor = changeCursor.Cursor;
                var curve = changeCursor.Curve;
                Curve = curve;
                started = Time.time;


                if (!Curve.gameObject.activeInHierarchy) return;

                ThrowIf("Stop overflow control is not supported", changeCursor.OverflowControl == BGCcCursorChangeLinear.OverflowControlEnum.Stop);


                var pointsCount = curve.PointsCount;
                ThrowIf("Curve should have at least 2 points.", pointsCount < 2);

                var math = changeCursor.Cursor.Math.Math;

                var sectionIndex = cursor.CalculateSectionIndex();
                var speed = changeCursor.CurrentSpeed;
                var speedPositive = speed > 0;

                if (speedPositive)
                {
                    //=================================== Positive speed
                    //first point
                    if (curve.Closed && sectionIndex == pointsCount - 1)
                    {
                        expectedPoints.Add(new ExpectedPoint(0, math.GetDistance() - cursor.Distance, speed, 0));
                    }
                    else if (!curve.Closed && sectionIndex == pointsCount - 2)
                    {
                        expectedPoints.Add(new ExpectedPoint(pointsCount - 1, math.GetDistance() - cursor.Distance, speed, 0));
                    }
                    else
                    {
                        expectedPoints.Add(new ExpectedPoint(sectionIndex + 1, math[sectionIndex + 1].DistanceFromStartToOrigin - cursor.Distance, speed, 0));
                    }

                    //go towards end
                    for (var i = sectionIndex + 2; i < pointsCount; i++)
                    {
                        expectedPoints.Add(new ExpectedPoint(i, math[i - 1].Distance, changeCursor.GetSpeedAtPoint(i - 1), changeCursor.GetDelayAtPoint(i - 1)));
                    }

                    //add last point
                    if (curve.Closed && sectionIndex != pointsCount)
                    {
                        expectedPoints.Add(new ExpectedPoint(0, math[pointsCount - 1].Distance, changeCursor.GetSpeedAtPoint(pointsCount - 1), changeCursor.GetDelayAtPoint(pointsCount - 1)));
                    }


                    if (changeCursor.OverflowControl == BGCcCursorChangeLinear.OverflowControlEnum.PingPong)
                    {
                        if (curve.Closed)
                            expectedPoints.Add(new ExpectedPoint(pointsCount - 1, math[pointsCount - 1].Distance,
                                changeCursor.GetSpeedAtPoint(pointsCount - 1), changeCursor.GetDelayAtPoint(0)));

                        //go all the way down
                        for (var i = pointsCount - 2; i >= 0; i--)
                        {
                            expectedPoints.Add(new ExpectedPoint(i, math[i].Distance, changeCursor.GetSpeedAtPoint(i), changeCursor.GetDelayAtPoint(i + 1)));
                        }
                    }
                    else
                    {
                        if (!curve.Closed)
                        {
                            expectedPoints.Add(new ExpectedPoint(0, math[pointsCount - 2].Distance, 0, changeCursor.GetDelayAtPoint(pointsCount - 1)));
                        }
                    }

                    //go up to initial position
                    for (var i = 1; i <= sectionIndex; i++)
                    {
                        expectedPoints.Add(new ExpectedPoint(i, math[i - 1].Distance, changeCursor.GetSpeedAtPoint(i - 1), changeCursor.GetDelayAtPoint(i - 1)));
                    }

                    //last point
                    expectedPoints.Add(new ExpectedPoint(-1, cursor.Distance - math[sectionIndex].DistanceFromStartToOrigin, speed, changeCursor.GetDelayAtPoint(sectionIndex)));
                }
                else
                {
                    //=================================== Negative speed
                    //first point
                    expectedPoints.Add(new ExpectedPoint(sectionIndex, cursor.Distance - math[sectionIndex].DistanceFromStartToOrigin, speed, 0));

                    //go towards start
                    for (var i = sectionIndex - 1; i >= 0; i--)
                    {
                        expectedPoints.Add(new ExpectedPoint(i, math[i].Distance, changeCursor.GetSpeedAtPoint(i), changeCursor.GetDelayAtPoint(i + 1)));
                    }

                    if (changeCursor.OverflowControl == BGCcCursorChangeLinear.OverflowControlEnum.PingPong)
                    {
                        for (var i = 1; i < pointsCount; i++)
                        {
                            expectedPoints.Add(new ExpectedPoint(i, math[i - 1].Distance, changeCursor.GetSpeedAtPoint(i - 1), changeCursor.GetDelayAtPoint(i - 1)));
                        }

                        if (curve.Closed)
                        {
                            expectedPoints.Add(new ExpectedPoint(0, math[pointsCount - 1].Distance, changeCursor.GetSpeedAtPoint(pointsCount - 1), changeCursor.GetDelayAtPoint(pointsCount - 1)));
                            expectedPoints.Add(new ExpectedPoint(pointsCount - 1, math[pointsCount - 1].Distance,
                                changeCursor.GetSpeedAtPoint(pointsCount - 1), changeCursor.GetDelayAtPoint(0)));
                        }
                    }
                    else
                    {
                        if (curve.Closed)
                        {
                            expectedPoints.Add(new ExpectedPoint(pointsCount - 1, math[pointsCount - 1].Distance,
                                changeCursor.GetSpeedAtPoint(pointsCount - 1), changeCursor.GetDelayAtPoint(0)));
                        }
                        else
                        {
                            expectedPoints.Add(new ExpectedPoint(pointsCount - 1, 0, 0, changeCursor.GetDelayAtPoint(0)));
                        }
                    }


                    //go from end to initial section
                    for (var i = pointsCount - 2; i > sectionIndex; i--)
                    {
                        expectedPoints.Add(new ExpectedPoint(i, math[i].Distance, changeCursor.GetSpeedAtPoint(i), changeCursor.GetDelayAtPoint(i + 1)));
                    }

                    //last point
                    expectedPoints.Add(new ExpectedPoint(-1, math[sectionIndex].DistanceFromEndToOrigin - cursor.Distance, changeCursor.GetSpeedAtPoint(sectionIndex),
                        changeCursor.GetDelayAtPoint(sectionIndex + 1)));
                }
            }

            private void ThrowIf(string message, bool condition)
            {
                if (condition) throw GetException(message);
            }

            private UnityException GetException(string message)
            {
                return new UnityException(message + ". Curve=" + changeCursor.Curve.gameObject.name);
            }

            public void Check()
            {
                if (!valid) return;

                var toCheck = expectedPoints[pointCursor];

                if (toCheck.PointIndex == -1)
                {
                    if (toCheck.ExpectedDelay < Elapsed)
                    {
                        MoveNext();
                    }
                }
                else if (toCheck.ExpectedDelay < Elapsed - Epsilon)
                {
                    valid = false;
                    Debug.LogException(GetException("Missing event: expected " + toCheck + " event did not occur"));
                    return;
                }
            }

            public void Reached(int point)
            {
                if (!valid) return;

                var toCompare = expectedPoints[pointCursor];
                if (toCompare.PointIndex >= 0 && toCompare.PointIndex != point)
                {
                    valid = false;
                    Debug.LogException(GetException("Points indexes mismatch: expected " + toCompare.PointIndex + ", actual=" + point));
                    return;
                }

                var expectedDelay = toCompare.ExpectedDelay;
                var actualDelay = Elapsed;
                if (Math.Abs(expectedDelay - actualDelay) > Epsilon)
                {
                    valid = false;
                    Debug.LogException(GetException("Timing mismatch at point {" + toCompare.PointIndex + "}: expected " + expectedDelay + ", actual=" + actualDelay));
                    return;
                }

                MoveNext();
            }

            private void MoveNext()
            {
                started = Time.time;
                pointCursor = pointCursor == expectedPoints.Count - 1 ? 0 : pointCursor + 1;
            }
        }

        private sealed class ExpectedPoint
        {
            public readonly float Distance;
            public readonly int PointIndex;
            public readonly float Speed;
            public readonly float Delay;

            public ExpectedPoint(int pointIndex, float distance, float speed, float delay)
            {
                Speed = speed;
                Distance = distance;
                PointIndex = pointIndex;
                Delay = delay;
            }

            public double ExpectedDelay
            {
                get
                {
                    var speed = Math.Abs(Speed);
                    var delay = Mathf.Clamp(Delay, 0, float.MaxValue);
                    return delay + (speed < Sequence.Epsilon ? Sequence.Epsilon : Distance/speed);
                }
            }

            public override string ToString()
            {
                return "Point " + PointIndex + " after " + ExpectedDelay + " delay.";
            }
        }
    }
}