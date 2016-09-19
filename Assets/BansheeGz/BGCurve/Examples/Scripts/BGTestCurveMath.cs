using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Security.Policy;
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

        private MyCurveData myCurveData;

        private GUIStyle style;

        // Use this for initialization
        private void Start()
        {
            myCurveData = new MyCurveData(GetComponent<BGCurve>(), new BGCurveBaseMath.Config(BGCurveBaseMath.Fields.PositionAndTangent), ObjectToMove, LineRendererMaterial);

            //UsePointPositionsToCalcTangents
            myCurveData.Add(new SomeCurveData(myCurveData, "BGRuntimePos2TangentsWorld", transform.position + new Vector3(4, 3),
                new BGCurveBaseMath.Config(BGCurveBaseMath.Fields.PositionAndTangent) {UsePointPositionsToCalcTangents = true}));

            //OptimizeStraightLines
            myCurveData.Add(new SomeCurveData(myCurveData, "BGRuntimeStraightLinesWorld", transform.position + new Vector3(-4, 3),
                new BGCurveBaseMath.Config(BGCurveBaseMath.Fields.PositionAndTangent) {OptimizeStraightLines = true}));
        }


        // Update is called once per frame
        private void Update()
        {
            myCurveData.Update();
            if (Input.GetKeyDown(KeyCode.LeftArrow)) myCurveData.MoveLeft();
            if (Input.GetKeyDown(KeyCode.RightArrow)) myCurveData.MoveRight();
        }

        private void OnGUI()
        {
            if (style == null) style = new GUIStyle(GUI.skin.label) {fontSize = 20};
            GUI.Label(new Rect(0, 30, 500, 30), "BGTestCurveMath: Left Arrow - move left, Right Arrow - move right", style);
        }


        //==================================== abstract curve's data
        private abstract class CurveDataAbstract
        {
            private readonly List<GameObject> objectsToMove = new List<GameObject>();
            private readonly Material objectToMoveMaterial;
            private readonly LineRenderer lineRenderer;
            public readonly Material LineRendererMaterial;
            public readonly GameObject GameObject;

            protected BGCurveBaseMath Math;

            private BGCurve curve;

            public BGCurve Curve
            {
                get { return curve; }
                set
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
                lineRenderer.SetWidth(0.05f, 0.05f);
                lineRenderer.SetColors(color, color);
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
                lineRenderer.SetVertexCount(count);
                lineRenderer.SetPositions(positions);
            }

            //=================== objects
            protected void AddObjects(int count, MeshRenderer pattern, Transform parent)
            {
                for (var i = 0; i < count; i++)
                {
                    var clone = Instantiate(pattern.gameObject);
                    clone.transform.localScale = pattern.transform.localScale;
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
        private sealed class MyCurveData : CurveDataAbstract
        {
            public readonly List<float> DistanceRatios = new List<float>();
            public readonly MeshRenderer ObjectToMove;
            private readonly List<SomeCurveData> curves = new List<SomeCurveData>();

            private float startTime = -Period*2;
            private Quaternion fromRotation;
            private Quaternion toRotation;
            private int currentCurveIndex = -1;


            public MyCurveData(BGCurve curve, BGCurveBaseMath.Config config, MeshRenderer objectToMove, Material lineRendererMaterial)
                : base(curve.gameObject, lineRendererMaterial, Color.green)
            {
                Curve = curve;
                Math = new BGCurveBaseMath(curve, config);
                ObjectToMove = objectToMove;

                AddObject(objectToMove.gameObject);
                AddObjects(ObjectsCount - 1, objectToMove, curve.transform.parent);

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

                curves.ForEach(data => data.Update());
            }

            public bool IsCurrent(SomeCurveData someCurve)
            {
                return currentCurveIndex >= 0 && currentCurveIndex < curves.Count && curves[currentCurveIndex] == someCurve;
            }

            public void Add(SomeCurveData someCurveData)
            {
                curves.Add(someCurveData);
            }
        }

        //==================================== Some Curve
        private sealed class SomeCurveData : CurveDataAbstract
        {
            private readonly Vector3 origin;
            private readonly MyCurveData referenceCurve;
            private readonly Vector3 localScale = new Vector3(.7f, .7f, .7f);

            public SomeCurveData(MyCurveData referenceCurve, string name, Vector3 position, BGCurveBaseMath.Config config)
                : base(new GameObject(name), referenceCurve.LineRendererMaterial, Color.magenta)
            {
                this.referenceCurve = referenceCurve;

                //game object
                GameObject.transform.parent = referenceCurve.GameObject.transform.parent;
                GameObject.transform.localScale = localScale;
                GameObject.transform.position = position;
                origin = position;

                //curve
                Curve = GameObject.AddComponent<BGCurve>();
                Curve.Closed = referenceCurve.Curve.Closed;

                //add points
                for (var i = 0; i < referenceCurve.Curve.PointsCount; i++) Curve.AddPoint(referenceCurve.Curve[i].CloneTo(Curve));

                //init math after points are added
                Math = new BGCurveBaseMath(Curve, config);

                AddObjects(ObjectsCount, referenceCurve.ObjectToMove, referenceCurve.Curve.transform.parent);
            }

            public override void Update()
            {
                var curveTransform = Curve.gameObject.transform;

                var referenceTransform = referenceCurve.Curve.transform;

                curveTransform.rotation = referenceTransform.rotation;

                var moveTime = 10*Time.deltaTime;
                curveTransform.position = Vector3.MoveTowards(curveTransform.position, referenceCurve.IsCurrent(this) ? referenceTransform.position : origin, moveTime);
                curveTransform.localScale = Vector3.MoveTowards(curveTransform.localScale, referenceCurve.IsCurrent(this) ? referenceTransform.transform.localScale : localScale, moveTime/5);

                UpdateObjects(referenceCurve.DistanceRatios);
            }
        }
    }
}