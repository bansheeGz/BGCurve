using UnityEngine;

namespace BansheeGz.BGSpline.Example
{
    // rotate and scale an object 
    public class BGTestCurveChange : MonoBehaviour
    {
        private const float RotationSpeed = 40;
        private const float ScaleUpperLimit = 1.25f;
        private const float ScaleLowerLimit = .5f;

        private Vector3 scaleSpeed = Vector3.one*.1f;

        // Update is called once per frame
        private void Update()
        {
            //rotate
            transform.RotateAround(transform.position, Vector3.up, RotationSpeed*Time.deltaTime);

            //scale
            var localScale = transform.localScale;
            var upperLimit = localScale.x > ScaleUpperLimit;
            var lowerLimit = localScale.x < ScaleLowerLimit;
            if (upperLimit || lowerLimit)
            {
                scaleSpeed = -scaleSpeed;
                localScale = upperLimit ? new Vector3(ScaleUpperLimit, ScaleUpperLimit, ScaleUpperLimit) : new Vector3(ScaleLowerLimit, ScaleLowerLimit, ScaleLowerLimit);
            }
            transform.localScale = localScale + scaleSpeed*Time.deltaTime;
        }
    }
}