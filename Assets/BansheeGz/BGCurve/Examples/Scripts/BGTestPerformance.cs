using UnityEngine;
using BansheeGz.BGSpline.Components;
using BansheeGz.BGSpline.Curve;

namespace BansheeGz.BGSpline.Example
{
    //for testing performance
    //DO NOT USE Unity Handles (for curve's points) with this test class! 
    [RequireComponent(typeof (BGCcMath))]
    public class BGTestPerformance : MonoBehaviour
    {
        public enum ControlTypeForNewPoints
        {
            Random,
            Absent,
            Bezier
        }

        //speed range for objects
        private const float SpeedRange = 5f;
        //transition period for points
        private const int Period = 10;

        //===========================Public
        [Tooltip("Object's prefab")] public GameObject ObjectToMove;
        [Tooltip("Limits for points positions and transitions")] public Bounds Bounds = new Bounds(Vector3.zero, Vector3.one);
        [Tooltip("Number of points to spawn")] [Range(2, 2000)] public int PointsCount = 100;
        [Tooltip("Number of objects to spawn")] [Range(1, 500)] public int ObjectsCount = 100;
        [Tooltip("Control Type")] public ControlTypeForNewPoints ControlType;

        //===========================Private

        private float startTime = -1000;
        private BGCurve curve;
        private BGCurveBaseMath math;

        //points
        private Vector3[] oldPos;
        private Vector3[] newPos;

        //objs
        private GameObject[] objects;
        private float[] speed;
        private float[] distances;

        private float oldDistance;

        //Unity callback
        private void Start()
        {
            curve = GetComponent<BGCurve>();
            var ccMath = GetComponent<BGCcMath>();
            math = ccMath.Math;
            curve = ccMath.Curve;

            //init arrays
            oldPos = new Vector3[PointsCount];
            newPos = new Vector3[PointsCount];
            speed = new float[ObjectsCount];
            distances = new float[ObjectsCount];
            objects = new GameObject[ObjectsCount];


            //--------------------------- init from points
            for (var i = 0; i < PointsCount; i++)
            {
                var controlTypeEnum = BGCurvePoint.ControlTypeEnum.BezierIndependant;
                switch (ControlType)
                {
                    case ControlTypeForNewPoints.Absent:
                        controlTypeEnum = BGCurvePoint.ControlTypeEnum.Absent;
                        break;
                    case ControlTypeForNewPoints.Random:
                        controlTypeEnum = (BGCurvePoint.ControlTypeEnum) Random.Range(0, 3);
                        break;
                }
                curve.AddPoint(new BGCurvePoint(curve, RandomVector(), controlTypeEnum, RandomVector(), RandomVector()));
            }

            //Recalculate manually after points were added (normally you would not do it)
            math.Recalculate();

            //---------------------------- init objects
            if (ObjectToMove != null)
            {
                var totalDistance = oldDistance = math.GetDistance();
                for (var i = 0; i < ObjectsCount; i++)
                {
                    var obj = Instantiate(ObjectToMove, Vector3.zero, Quaternion.identity) as GameObject;
                    obj.transform.parent = transform;
                    objects[i] = obj;
                    distances[i] = Random.Range(0, totalDistance);
                }
                ObjectToMove.SetActive(false);
                //--------------------------- init speed
                for (var i = 0; i < ObjectsCount; i++)
                {
                    speed[i] = Random.Range(0, 2) == 0 ? Random.Range(-SpeedRange, -SpeedRange*0.3f) : Random.Range(SpeedRange*0.3f, SpeedRange);
                }
            }
        }

        //Unity callback
        private void Update()
        {
            //reset transitions
            if (Time.time - startTime > Period)
            {
                startTime = Time.time;
                for (var i = 0; i < PointsCount; i++)
                {
                    oldPos[i] = newPos[i];
                    newPos[i] = RandomVector();
                }
            }

            //move points
            var ratio = (Time.time - startTime)/Period;
            var points = curve.Points;
            for (var i = 0; i < PointsCount; i++) points[i].PositionLocal = Vector3.Lerp(oldPos[i], newPos[i], ratio);

            //move objects
            var totalDistance = math.GetDistance();
            if (ObjectToMove != null)
            {
                var remapRatio = totalDistance/oldDistance;
                for (var i = 0; i < ObjectsCount; i++)
                {
                    var distance = distances[i];

                    //since curve's length changed-remap
                    distance = distance*remapRatio;

                    distance = distance + speed[i]*Time.deltaTime;
                    if (distance < 0)
                    {
                        speed[i] = -speed[i];
                        distance = 0;
                    }
                    else if (distance > totalDistance)
                    {
                        speed[i] = -speed[i];
                        distance = totalDistance;
                    }
                    distances[i] = distance;

                    objects[i].transform.position = math.CalcByDistance(BGCurveBaseMath.Field.Position, distance);
                }
            }
            oldDistance = totalDistance;
        }

        //misc utility methods
        private Vector3 RandomVector()
        {
            return new Vector3(Range(0), Range(1), Range(2));
        }

        private float Range(int index)
        {
            return Random.Range(Bounds.min[index], Bounds.max[index]);
        }
    }
}


