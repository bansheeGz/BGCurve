using System;
using UnityEngine;
using System.Collections.Generic;
using BansheeGz.BGSpline.Curve;
using Random = UnityEngine.Random;

namespace BansheeGz.BGSpline.Example
{
    // math visual testing (for internal use)
    public class BGTestCurveMath : MonoBehaviour
    {
        [Tooltip("Material to use with LineRenderer")] public Material LineRendererMaterial;

        [Tooltip("Object to move along a curve")] public MeshRenderer ObjectToMove;

        private const float Period = 3;
        private const int ObjectsCount = 4;
        private const float ObjectsSpeed = .3f;

        private TestCurves testCurves;

        private GUIStyle style;

        // Use this for initialization
        private void Start()
        {
            testCurves = new TestCurves(GetComponent<BGCurve>(), new BGCurveBaseMath.Config(BGCurveBaseMath.Fields.PositionAndTangent), ObjectToMove, LineRendererMaterial);

            //Base, OptimizeStraightLines
            testCurves.Add(new CurveData(testCurves, "BGBaseStraightLines", "Base, OptimizeStraightLines = true", transform.position + new Vector3(-4, 1),
                new BGCurveBaseMath.Config(BGCurveBaseMath.Fields.PositionAndTangent) {OptimizeStraightLines = true}, CurveData.MathTypeEnum.Base));

            //Base, UsePointPositionsToCalcTangents
            testCurves.Add(new CurveData(testCurves, "BGBasePos2Tangents", "Base, UsePointPositionsToCalcTangents = true", transform.position + new Vector3(-4, 4),
                new BGCurveBaseMath.Config(BGCurveBaseMath.Fields.PositionAndTangent) {UsePointPositionsToCalcTangents = true}, CurveData.MathTypeEnum.Base));

            //Adaptive
            testCurves.Add(new CurveData(testCurves, "BGAdaptive", "Adaptive", transform.position + new Vector3(4, 4),
                new BGCurveAdaptiveMath.ConfigAdaptive(BGCurveBaseMath.Fields.PositionAndTangent) /* { Tolerance = .2f }*/, CurveData.MathTypeEnum.Adaptive));

            //Formula
            testCurves.Add(new CurveData(testCurves, "BGFormula", "Formula", transform.position + new Vector3(4, 1),
                new BGCurveBaseMath.Config(BGCurveBaseMath.Fields.PositionAndTangent), CurveData.MathTypeEnum.Formula));
        }


        // Update is called once per frame
        private void Update()
        {
            testCurves.Update();
            if (Input.GetKeyDown(KeyCode.LeftArrow)) testCurves.MoveLeft();
            if (Input.GetKeyDown(KeyCode.RightArrow)) testCurves.MoveRight();
        }

        private void OnGUI()
        {
            if (style == null) style = new GUIStyle(GUI.skin.label) {fontSize = 18};
            GUI.Label(new Rect(0, 24, 800, 30), "Left Arrow - move left, Right Arrow - move right", style);
            GUI.Label(new Rect(0, 48, 800, 30), "Comparing with: " + testCurves.CurrentToString(), style);
        }


        //==================================== abstract curve's data
        private abstract class CurveDataAbstract
        {
            private readonly List<GameObject> objectsToMove = new List<GameObject>();
            private readonly Material objectToMoveMaterial;
            private readonly LineRenderer lineRenderer;
            public readonly Material LineRendererMaterial;
            protected readonly GameObject GameObject;

            protected BGCurveBaseMath Math;

            private BGCurve curve;

            public BGCurve Curve
            {
                get { return curve; }
                protected set
                {
                    curve = value;
                    curve.Changed += (sender, args) => UpdateLineRenderer();
                }
            }


            protected CurveDataAbstract(GameObject gameObject, Material lineRendererMaterial, Color color)
            {
                GameObject = gameObject;
                LineRendererMaterial = lineRendererMaterial;

                //change material
                objectToMoveMaterial = Instantiate(lineRendererMaterial);
                objectToMoveMaterial.SetColor("_TintColor", color);


                //add lineRenderer
                lineRenderer = gameObject.AddComponent<LineRenderer>();
                lineRenderer.material = lineRendererMaterial;
#if UNITY_5_5 || UNITY_5_6 || UNITY_5_6_OR_NEWER
                lineRenderer.startWidth = lineRenderer.endWidth = 0.05f;
                lineRenderer.startColor = lineRenderer.endColor = color;
#else
                lineRenderer.SetWidth(0.05f, 0.05f);
                lineRenderer.SetColors(color, color);
#endif

            }

            //=================== line renderer
            private void UpdateLineRenderer()
            {
                const int count = 100;
                var positions = new Vector3[100];
                const float countMinusOne = count - 1;
                for (var i = 0; i < 100; i++)
                {
                    var distanceRatio = i/countMinusOne;
                    positions[i] = Math.CalcByDistanceRatio(BGCurveBaseMath.Field.Position, distanceRatio);
                }
#if UNITY_5_5
                lineRenderer.numPositions = count;
#elif UNITY_5_6_OR_NEWER
                lineRenderer.positionCount = count;
#else
                lineRenderer.SetVertexCount(count);
#endif
                lineRenderer.SetPositions(positions);
            }

            //=================== objects
            protected void AddObjects(int count, MeshRenderer pattern, Transform parent)
            {
                for (var i = 0; i < count; i++)
                {
                    var clone = Instantiate(pattern.gameObject);
                    clone.transform.parent = parent;
                    AddObject(clone);
                }
            }

            protected void AddObject(GameObject obj)
            {
                obj.GetComponent<MeshRenderer>().sharedMaterial = objectToMoveMaterial;
                objectsToMove.Add(obj.gameObject);
            }

            protected void UpdateObjects(List<float> distanceRatios)
            {
                for (var i = 0; i < objectsToMove.Count; i++)
                {
                    var position = Math.CalcByDistanceRatio(BGCurveBaseMath.Field.Position, distanceRatios[i]);
                    var tangent = Math.CalcByDistanceRatio(BGCurveBaseMath.Field.Tangent, distanceRatios[i]);

                    objectsToMove[i].transform.position = position;
                    objectsToMove[i].transform.LookAt(position + tangent);
                }
            }

            public abstract void Update();
        }

        //==================================== Reference Curve
        private sealed class TestCurves : CurveDataAbstract
        {
            public readonly List<float> DistanceRatios = new List<float>();
            public readonly MeshRenderer ObjectToMove;
            private readonly List<CurveData> curves = new List<CurveData>();

            private float startTime = -Period*2;
            private Quaternion fromRotation;
            private Quaternion toRotation;
            private int currentCurveIndex = -1;


            public TestCurves(BGCurve curve, BGCurveBaseMath.Config config, MeshRenderer objectToMove, Material lineRendererMaterial)
                : base(curve.gameObject, lineRendererMaterial, Color.green)
            {
                Curve = curve;
                Math = new BGCurveBaseMath(curve, config);
                ObjectToMove = objectToMove;

                AddObject(objectToMove.gameObject);
                AddObjects(ObjectsCount - 1, objectToMove, curve.transform);

                const float offset = 1/(float) ObjectsCount;
                for (var i = 0; i < ObjectsCount; i++) DistanceRatios.Add(i*offset);
            }

            public void MoveRight()
            {
                currentCurveIndex++;
                if (currentCurveIndex == curves.Count) currentCurveIndex = 0;
            }

            public void MoveLeft()
            {
                currentCurveIndex--;
                if (currentCurveIndex < 0) currentCurveIndex = curves.Count - 1;
            }

            public override void Update()
            {
                if (Time.time - startTime > Period)
                {
                    startTime = Time.time;

                    fromRotation = Curve.transform.rotation;
                    toRotation = Quaternion.Euler(Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f));
                }

                var ratio = (Time.time - startTime)/Period;
                Curve.transform.rotation = Quaternion.Lerp(fromRotation, toRotation, ratio);
                for (var i = 0; i < DistanceRatios.Count; i++)
                {
                    DistanceRatios[i] += ObjectsSpeed*Time.deltaTime;
                    if (DistanceRatios[i] > 1) DistanceRatios[i] = 0;
                }
                UpdateObjects(DistanceRatios);

                foreach (var data in curves) data.Update();
            }

            public bool IsCurrent(CurveData curve)
            {
                return currentCurveIndex >= 0 && currentCurveIndex < curves.Count && curves[currentCurveIndex] == curve;
            }

            public void Add(CurveData curveData)
            {
                curves.Add(curveData);
            }

            public string CurrentToString()
            {
                return currentCurveIndex < 0 ? "None" : curves[currentCurveIndex].Description;
            }
        }

        //==================================== Some Curve
        private sealed class CurveData : CurveDataAbstract
        {
            public enum MathTypeEnum
            {
                Base,
                Formula,
                Adaptive
            }

            private readonly Vector3 origin;
            private readonly TestCurves testCurves;
            private readonly Vector3 originalScale = new Vector3(.7f, .7f, .7f);

            private readonly string description;

            public string Description
            {
                get { return description; }
            }

            public CurveData(TestCurves testCurves, string name, string description, Vector3 position, BGCurveBaseMath.Config config, MathTypeEnum mathType)
                : base(new GameObject(name), testCurves.LineRendererMaterial, Color.magenta)
            {
                this.testCurves = testCurves;
                this.description = description;

                //game object
                GameObject.transform.position = position;
                origin = position;

                //curve
                Curve = GameObject.AddComponent<BGCurve>();
                Curve.Closed = testCurves.Curve.Closed;

                //add points
                for (var i = 0; i < testCurves.Curve.PointsCount; i++)
                {
                    var point = testCurves.Curve[i];
                    var clonePoint = new BGCurvePoint(Curve, point.PositionLocal, point.ControlType, point.ControlFirstLocal, point.ControlSecondLocal);
                    Curve.AddPoint(clonePoint);
                }

                //init math after points are added
                switch (mathType)
                {
                    case MathTypeEnum.Base:
                        Math = new BGCurveBaseMath(Curve, config);
                        break;
                    case MathTypeEnum.Formula:
#pragma warning disable 0618
                        Math = new BGCurveFormulaMath(Curve, config);
#pragma warning restore 0618

                        break;
                    case MathTypeEnum.Adaptive:
                        Math = new BGCurveAdaptiveMath(Curve, (BGCurveAdaptiveMath.ConfigAdaptive) config);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("mathType", mathType, null);
                }

                AddObjects(ObjectsCount, testCurves.ObjectToMove, GameObject.transform);

                //scale down
                GameObject.transform.localScale = originalScale;
            }

            public override void Update()
            {
                var curveTransform = Curve.gameObject.transform;

                var referenceTransform = testCurves.Curve.transform;

                curveTransform.rotation = referenceTransform.rotation;

                var moveTime = 10*Time.deltaTime;

                var current = testCurves.IsCurrent(this);
                curveTransform.position = Vector3.MoveTowards(curveTransform.position, current ? referenceTransform.position : origin, moveTime);
                curveTransform.localScale = Vector3.MoveTowards(curveTransform.localScale, current ? referenceTransform.transform.localScale : originalScale, moveTime/4);

                UpdateObjects(testCurves.DistanceRatios);
            }
        }
    }
}