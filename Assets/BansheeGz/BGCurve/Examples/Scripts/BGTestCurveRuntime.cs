using BansheeGz.BGSpline.Components;
using UnityEngine;
using BansheeGz.BGSpline.Curve;

namespace BansheeGz.BGSpline.Example
{
    // test class for example only (adding moving deleting points at runtime)
    [RequireComponent(typeof (BGCcMath))]
    public class BGTestCurveRuntime : MonoBehaviour
    {
        // the height between 2 points
        private const float OneTierHeight = 2;
        //number of points to add (so total is 9). Should be even.
        private const int PointsCountToAdd = 8;
        //point's move time
        private const float PointMoveTime = 1.5f;
        //spread for control points
        private const int MaximumControlSpread = 4;


        public GameObject ObjectToMove;

        private BGCurve curve;

        private BGCcMath math;

        // section spawn start time
        private float started;

        // current point's pointer (index)
        private int counter;
        // position for new point. The point will be moved slowly towards it.
        private Vector3 nextPosition;
        // position for new point's 1st control
        private Vector3 nextControl1;
        // position for previous point's 2nd control.
        private Vector3 nextControl2;

        // Use this for initialization
        private void Start()
        {
            curve = GetComponent<BGCurve>();
            math = GetComponent<BGCcMath>();
            Reset();
        }

        //new cycle
        private void Reset()
        {
            //delete all points (use curve.Delete to remove a single point)
            curve.Clear();

            //add very first point
            curve.AddPoint(new BGCurvePoint(curve, Vector3.zero) {ControlType = BGCurvePoint.ControlTypeEnum.BezierIndependant});

            started = Time.time;
            counter = 0;
        }

        // Update is called once per frame
        private void Update()
        {
            var elapsed = Time.time - started;

            if (elapsed >= PointMoveTime || curve.PointsCount < 2)
            {
                if (counter == PointsCountToAdd)
                {
                    //=========================================== Reset (end of cycle)
                    Reset();
                }
                else
                {
                    //=========================================== New point spawning
                    //spawn new point at the same position as the last one
                    var position = curve[curve.PointsCount - 1].PositionLocal;
                    curve.AddPoint(new BGCurvePoint(curve, position)
                    {
                        ControlType = BGCurvePoint.ControlTypeEnum.BezierIndependant,
                        //unfortunately by default ControlFirstLocal and ControlSecondLocal are not equal to Vector3.zero. That was a mistake.
                        ControlFirstLocal = Vector3.zero,
                        ControlSecondLocal = Vector3.zero
                    });

                    // are we going up or down
                    var up = counter < (PointsCountToAdd >> 1);

                    //assign next positions to drag a point (and controls) to 
                    nextPosition = position + new Vector3(0, up ? OneTierHeight : -OneTierHeight, 0);
                    var controlPosition = new Vector3(
                        Random.Range(-MaximumControlSpread, MaximumControlSpread),
                        (up ? OneTierHeight : -OneTierHeight)*.5f,
                        Random.Range(-MaximumControlSpread, MaximumControlSpread));
                    nextControl1 = Vector3.Lerp(position - nextPosition, controlPosition, .8f);
                    nextControl2 = Vector3.Lerp(nextPosition - position, controlPosition, .8f);
                    started = Time.time;

                    counter++;
                    if (curve.PointsCount > 2) curve.Delete(0);
                }
            }
            else
            {
                var ratio = elapsed/PointMoveTime;
                //=========================================== Moving a current point (and adjacent controls) slowly to their future positions
                // changing curve's attributes, like point's positions or controls fires curve.Changed event (in curve's LateUpdate)
                // math object uses this event to recalculate it's cached data (and it's relatively expensive operation).
                // we also use this event to call UpdateLineRenderer to update UI.
                var lastPoint = curve[curve.PointsCount - 1];
                lastPoint.PositionLocal = Vector3.Lerp(lastPoint.PositionLocal, nextPosition, ratio);
                lastPoint.ControlFirstLocal = Vector3.Lerp(lastPoint.PositionLocal, nextControl1, ratio);
                lastPoint.ControlFirstLocal = Vector3.Lerp(lastPoint.PositionLocal, nextControl2, ratio);

                // move particles. we can use math's cached data directly, if we want to
                var section = math.Math[0];
                ObjectToMove.transform.position = math.Math.CalcByDistance(BGCurveBaseMath.Field.Position, section.DistanceFromStartToOrigin + section.Distance*ratio);
            }
        }
    }
}