using UnityEngine;
using BansheeGz.BGSpline.Components;
using BansheeGz.BGSpline.Curve;

namespace BansheeGz.BGSpline.Example
{
    public class BGTestCurveSnapping : MonoBehaviour
    {
        public BGCurve Curve;

        public float XChange = 5;
        public float YChange = .5f;
        
        private bool goingUp = true;
        private Vector3 @from;
        private Vector3 to;
        private float initialY;
        
        void Start()
        {
            from = new Vector3(-XChange, transform.position.y, transform.position.z);
            to = new Vector3(XChange, transform.position.y, transform.position.z);
            initialY = transform.position.y;
        }

        void Update()
        {
            var transformPosition = transform.position;

            // up down
            var upChange = Vector3.up * Time.deltaTime * 2;
            if (goingUp) transformPosition += upChange;
            else transformPosition -= upChange;
            if (transformPosition.y > initialY + YChange) goingUp = false;
            else if (transformPosition.y < initialY - YChange) goingUp = true;

            //move
            var pos = Vector3.MoveTowards(transformPosition, to, Time.deltaTime * 2);
            if (Mathf.Abs(pos.x - to.x) < .1 && Mathf.Abs(pos.z - to.z) < .1)
            {
                var temp = to;
                to = @from;
                @from = temp;
            }

            transform.position = pos;

            //apply snapping (if auto monitoring is off)
            if (!Curve.SnapMonitoring)
            {
                switch (Curve.SnapType)
                {
                    case BGCurve.SnapTypeEnum.Points:
                        Curve.ApplySnapping();
                        break;
                    case BGCurve.SnapTypeEnum.Curve:
                        if (!Curve.ApplySnapping()) Curve.GetComponent<BGCcMath>().Recalculate();
                        break;
                }
            }
        }
    }
}