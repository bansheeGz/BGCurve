using System;
using UnityEngine;
using BansheeGz.BGSpline.Components;
using BansheeGz.BGSpline.Curve;
using Random = UnityEngine.Random;

namespace BansheeGz.BGSpline.Example
{
    //check math's CalcPositionByClosestPoint method.  (this test can be  automated with CheckResults set to true)
    public class BGTestCurveClosestPoint : MonoBehaviour
    {
        [Tooltip("Line renderer material")] public Material LineRendererMaterial;

        [Tooltip("Object to use for point's indication")] public GameObject PointIndicator;

        [Range(1, 100)] [Tooltip("How much points to use with search")] public int NumberOfPointsToSeek = 10;

        [Range(2, 100)] [Tooltip("How much points to add to the curve")] public int NumberOfCurvePoints = 100;

        [Range(1, 30)] [Tooltip("How much sections to use for splitting each curve's segment")] public int NumberOfSplits = 30;

        [Range(1, 5)] [Tooltip("Transition period")] public int Period = 4;

        [Tooltip("Use slow check method to validate results")] public bool CheckResults = false;


        private BGCurve curve;
        private BGCcMath math;

        //this is an area, defined by camera frustum (so we could see curve and points)
        private static Vector3 min = new Vector3(-10, 0, -2);
        private static Vector3 max = new Vector3(10, 10, 2);


        //indicator objects
        private GameObject[] objects;

        //curve's points positions
        private Vector3[] oldCurvePointPos;
        private Vector3[] newCurvePointPos;

        //point's positions
        private Vector3[] oldPointPos;
        private Vector3[] newPointPos;

        //for transitions
        private float startTime = -100000;

        private int ErrorPointIndex = -1;
        private GUIStyle style;

        private bool HasError
        {
            get { return ErrorPointIndex >= 0; }
        }

        // Use this for initialization
        private void Start()
        {
            //init components
            curve = gameObject.AddComponent<BGCurve>();
            curve.Closed = true;
            math = gameObject.AddComponent<BGCcMath>();
            gameObject.AddComponent<BGCcVisualizationLineRenderer>();
            var lineRenderer = gameObject.GetComponent<LineRenderer>();
            lineRenderer.sharedMaterial = LineRendererMaterial;
            var color = new Color(.2f, .2f, .2f, 1f);
#if UNITY_5_5 || UNITY_5_6 || UNITY_5_6_OR_NEWER
            lineRenderer.startWidth = lineRenderer.endWidth = .03f;
            lineRenderer.startColor = lineRenderer.endColor = color;
#else
            lineRenderer.SetWidth(.03f, .03f);
            lineRenderer.SetColors(color, color);
#endif
            math.SectionParts = NumberOfSplits;

            //create curve's points
            for (var i = 0; i < NumberOfCurvePoints; i++)
            {
                var controlRandom = Random.Range(0, 3);
                var controlType = BGCurvePoint.ControlTypeEnum.Absent;
                switch (controlRandom)
                {
                    case 1:
                        controlType = BGCurvePoint.ControlTypeEnum.BezierIndependant;
                        break;
                    case 2:
                        controlType = BGCurvePoint.ControlTypeEnum.BezierSymmetrical;
                        break;
                }
                curve.AddPoint(new BGCurvePoint(curve,
                    Vector3.zero,
                    controlType,
                    RandomVector()*.3f,
                    RandomVector()*.3f));
            }

            //init arrays
            oldPointPos = new Vector3[NumberOfPointsToSeek];
            newPointPos = new Vector3[NumberOfPointsToSeek];
            oldCurvePointPos = new Vector3[NumberOfCurvePoints];
            newCurvePointPos = new Vector3[NumberOfCurvePoints];

            InitArray(newCurvePointPos, oldCurvePointPos);
            InitArray(newPointPos, oldPointPos);

            //create objects
            objects = new GameObject[NumberOfPointsToSeek];
            for (var i = 0; i < NumberOfPointsToSeek; i++)
            {
                var clone = Instantiate(PointIndicator);
                clone.transform.parent = transform;
                objects[i] = clone;
            }
            PointIndicator.SetActive(false);

            //init cycle
            InitCycle();
        }

        private void OnGUI()
        {
            if (style == null) style = new GUIStyle(GUI.skin.label) {fontSize = 20};
            GUI.Label(new Rect(0, 30, 600, 30), "Turn on Gizmos to see Debug lines", style);
        }


        private void InitCycle()
        {
            InitArray(oldCurvePointPos, newCurvePointPos);
            InitArray(oldPointPos, newPointPos);
        }

        // Update is called once per frame
        private void Update()
        {
            if (HasError)
            {
                //use it for debugging
                Process(ErrorPointIndex, true);
            }
            else
            {
                Calculate(null, null);


                var elapsed = Time.time - startTime;
                if (elapsed > Period)
                {
                    startTime = Time.time;
                    InitCycle();
                    elapsed = 0;
                }

                //lerp
                var ratio = elapsed/Period;
                for (var i = 0; i < NumberOfCurvePoints; i++)
                {
                    curve[i].PositionLocal = Vector3.Lerp(oldCurvePointPos[i], newCurvePointPos[i], ratio);
                }
                for (var i = 0; i < NumberOfPointsToSeek; i++)
                {
                    objects[i].transform.localPosition = Vector3.Lerp(oldPointPos[i], newPointPos[i], ratio);
                }
            }
        }


        private void Calculate(object sender, EventArgs e)
        {
            for (var i = 0; i < NumberOfPointsToSeek; i++)
            {
                Process(i);
                if (HasError) break;
            }
        }

        private void Process(int i, bool suppressWarning = false)
        {
            var point = objects[i].transform.position;
            float distanceUsingMath;
            var posUsingMath = math.CalcPositionByClosestPoint(point, out distanceUsingMath);

            Debug.DrawLine(point, posUsingMath, Color.yellow);

            if (!CheckResults) return;

            float distanceUsingCheckMethod;
            var posUsingCheckMethod = CalcPositionByClosestPoint(math, point, out distanceUsingCheckMethod);
            Debug.DrawLine(point, posUsingCheckMethod, Color.blue);

            var distanceCheck = Math.Abs(distanceUsingMath - distanceUsingCheckMethod) > .01f;
            var pointCheck = Vector3.Magnitude(posUsingMath - posUsingCheckMethod) > 0.001f;
            if ((distanceCheck || pointCheck) && Mathf.Abs((point - posUsingMath).magnitude - (point - posUsingCheckMethod).magnitude) > BGCurve.Epsilon)
            {
                ErrorPointIndex = i;
                if (!suppressWarning)
                {
                    Debug.Log("Error detected. Simulation stopped, but erroneous iteration's still running. Use debugger to debug the issue.");
                    Debug.Log("!!! Discrepancy detected while calculating pos by closest point: 1) [Using math] pos=" + posUsingMath + ", distance=" + distanceUsingMath
                              + "  2) [Using check method] pos=" + posUsingCheckMethod + ", distance=" + distanceUsingCheckMethod);


                    if (pointCheck)
                    {
                        Debug.Log("Reason: Result points varies more than " + BGCurve.Epsilon + ". Difference=" + Vector3.Magnitude(posUsingMath - posUsingCheckMethod));
                    }
                    if (distanceCheck)
                    {
                        Debug.Log("Reason: Distances varies more than 1cm. Difference=" + Math.Abs(distanceUsingMath - distanceUsingCheckMethod));
                    }

                    var mathPos = math.CalcByDistance(BGCurveBaseMath.Field.Position, distanceUsingMath);
                    var checkMethodPos = math.CalcByDistance(BGCurveBaseMath.Field.Position, distanceUsingCheckMethod);
                    Debug.Log("Distance check: 1) [Using math] check=" + (Vector3.SqrMagnitude(mathPos - posUsingMath) < BGCurve.Epsilon ? "passed" : "failed")
                              + "  2) [Using check method] check=" + (Vector3.SqrMagnitude(checkMethodPos - posUsingCheckMethod) < BGCurve.Epsilon ? "passed" : "failed"));


                    var actualDistUsingMath = Vector3.Distance(point, posUsingMath);
                    var actualDistanceUsingCheckMethod = Vector3.Distance(point, posUsingCheckMethod);
                    Debug.Log("Actual distance: 1) [Using math] Dist=" + actualDistUsingMath
                              + "  2) [Using check method] Dist=" + actualDistanceUsingCheckMethod
                              +
                              (Math.Abs(actualDistUsingMath - actualDistanceUsingCheckMethod) > BGCurve.Epsilon
                                  ? (". And the winner is " + (actualDistUsingMath < actualDistanceUsingCheckMethod ? "math" : "check method"))
                                  : ""));
                }
            }
        }

        //Bruteforce method for checking results only. Do not use it.
        private static Vector3 CalcPositionByClosestPoint(BGCcMath math, Vector3 targetPoint, out float distance)
        {
            var sections = math.Math.SectionInfos;
            var sectionsCount = sections.Count;
            var result = sections[0][0].Position;
            var minDistance = Vector3.SqrMagnitude(sections[0][0].Position - targetPoint);
            distance = 0;
            for (var i = 0; i < sectionsCount; i++)
            {
                var currentSection = sections[i];


                var points = currentSection.Points;
                var pointsCount = points.Count;
                for (var j = 1; j < pointsCount; j++)
                {
                    var point = points[j];
                    float ratio;

                    var closestPoint = CalcClosestPointToLine(points[j - 1].Position, point.Position, targetPoint, out ratio);

                    var sqrMagnitude = Vector3.SqrMagnitude(targetPoint - closestPoint);
                    if (!(minDistance > sqrMagnitude)) continue;


                    minDistance = sqrMagnitude;
                    result = closestPoint;

                    if (ratio == 1)
                    {
                        var sectionIndex = i;
                        var pointIndex = j;
                        if (j == pointsCount - 1 && i < sectionsCount - 1)
                        {
                            sectionIndex = i + 1;
                            pointIndex = 0;
                        }
                        distance = sections[sectionIndex].DistanceFromStartToOrigin + sections[sectionIndex][pointIndex].DistanceToSectionStart;
                    }
                    else
                    {
                        distance = sections[i].DistanceFromStartToOrigin + Mathf.Lerp(currentSection[j - 1].DistanceToSectionStart, point.DistanceToSectionStart, ratio);
                    }
                }
            }
            return result;
        }


        private static Vector3 RandomVector()
        {
            return new Vector3(Random.Range(min.x, max.x), Random.Range(min.y, max.y), Random.Range(min.z, max.z));
        }

        private static void InitArray(Vector3[] oldArray, Vector3[] newArray)
        {
            for (var i = 0; i < oldArray.Length; i++)
            {
                oldArray[i] = newArray[i];
                newArray[i] = RandomVector();
            }
        }

        private static Vector3 CalcClosestPointToLine(Vector3 a, Vector3 b, Vector3 p, out float ratio)
        {
            var ap = p - a;
            var ab = b - a;
            var sqrtMagnitude = ab.sqrMagnitude;
            if (Math.Abs(sqrtMagnitude) < BGCurve.Epsilon)
            {
                ratio = 1;
                return b;
            }

            var distance = Vector3.Dot(ap, ab)/sqrtMagnitude;
            if (distance < 0)
            {
                ratio = 0;
                return a;
            }
            if (distance > 1)
            {
                ratio = 1;
                return b;
            }
            ratio = distance;
            return a + ab*distance;
        }
    }
}