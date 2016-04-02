﻿using UnityEngine;
using BansheeGz.BGSpline.Curve;

namespace BansheeGz.BGSpline.Example
{
    // test class for example only
    [RequireComponent(typeof (BGCurve))]
    [RequireComponent(typeof (LineRenderer))]
    public class BGTestCurveStatic : MonoBehaviour
    {
        private const int TimeToMoveUp = 3;

        public GameObject ObjectToMove;

        private BGCurve curve;
        private BGCurveBaseMath curveBaseMath;

        private float started;
        private float ratio;
        private LineRenderer lineRenderer;


        // Use this for initialization
        private void Start()
        {
            curve = GetComponent<BGCurve>();
            lineRenderer = GetComponent<LineRenderer>();

            curveBaseMath = new BGCurveBaseMath(curve, false, 30);
            started = Time.time;

            ResetLineRenderer();
        }

        private void ResetLineRenderer()
        {
            const int points = 100;

            var positions = new Vector3[points];
            for (var i = 0; i < 100; i++)
            {
                positions[i] = curveBaseMath.CalcPositionByDistanceRatio(((float) i/(points - 1)));
            }

            lineRenderer.SetVertexCount(points);
            lineRenderer.SetPositions(positions);
        }

        // Update is called once per frame
        private void Update()
        {
            ratio = (Time.time - started)/TimeToMoveUp;
            if (ratio >= 1)
            {
                started = Time.time;
                ratio = 0;
            }
            else
            {
                ObjectToMove.transform.position = curveBaseMath.CalcPositionByDistanceRatio(ratio);
            }
        }
    }
}