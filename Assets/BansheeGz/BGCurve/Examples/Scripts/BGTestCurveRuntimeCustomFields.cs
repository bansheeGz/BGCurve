using BansheeGz.BGSpline.Components;
using BansheeGz.BGSpline.Curve;
using UnityEngine;

namespace BansheeGz.BGSpline.Example
{
    // Testing creating custom fields from scratch at runtime
    public class BGTestCurveRuntimeCustomFields : MonoBehaviour
    {
        private const string SpeedFieldName = "speed";
        private const string DelayFieldName = "delay";
        private const float Width = .02f;


        public Transform ObjectToMove;
        public Material LineRendererMaterial;

        // Use this for initialization
        void Start()
        {
            //move object
            var translateObject = gameObject.AddComponent<BGCcCursorObjectTranslate>();
            translateObject.ObjectToManipulate = ObjectToMove;

            //move cursor
            var changeCursor = gameObject.AddComponent<BGCcCursorChangeLinear>();

            //add line renderer
            gameObject.AddComponent<BGCcVisualizationLineRenderer>();
            var lineRenderer = gameObject.GetComponent<LineRenderer>();
            lineRenderer.sharedMaterial = LineRendererMaterial;
#if UNITY_5_5 || UNITY_5_6 || UNITY_5_6_OR_NEWER
            lineRenderer.startWidth = lineRenderer.endWidth = Width;
#else
            lineRenderer.SetWidth(Width, Width);
#endif

            //set up curve
            var curve = changeCursor.Curve;
            curve.Closed = true;
            curve.Mode2D = BGCurve.Mode2DEnum.XY;
            curve.PointsMode = BGCurve.PointsModeEnum.GameObjectsTransform;

            //add points
            curve.AddPoint(new BGCurvePoint(curve, new Vector2(-5, 0)));
            curve.AddPoint(new BGCurvePoint(curve, new Vector2(0, 5), BGCurvePoint.ControlTypeEnum.BezierSymmetrical, new Vector2(-5, 0), new Vector2(5, 0)));
            curve.AddPoint(new BGCurvePoint(curve, new Vector2(5, 0)));

            //setup custom fields
            //---speed
            changeCursor.SpeedField = NewFloatField(changeCursor, SpeedFieldName, 5, 10, 15);
            //---delay
            changeCursor.DelayField = NewFloatField(changeCursor, DelayFieldName, 3, 1, 2); 
        }

        //Add field with values
        private static BGCurvePointField NewFloatField(BGCcCursorChangeLinear changeCursor, string fieldName, params float[] values)
        {
            var curve = changeCursor.Curve;
            var field = curve.AddField(fieldName, BGCurvePointField.TypeEnum.Float);
            for (var i = 0; i < values.Length; i++) curve[i].SetFloat(fieldName, values[i]);
            return field;
        }
    }
}