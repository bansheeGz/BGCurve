using UnityEngine;
using BansheeGz.BGSpline.Curve;

namespace BansheeGz.BGSpline.Editor
{
    public class BGTransformMonitor
    {
        private Vector3 position;
        private Quaternion rotation;
        private Vector3 scale;

        private readonly BGCurve curve;

        public BGTransformMonitor(BGCurve curve)
        {
            this.curve = curve;
            Update();
        }


        private void Update()
        {
            var transform = curve.transform;
            position = transform.position;
            rotation = transform.rotation;
            scale = transform.lossyScale;
        }

        public void Check()
        {
            if (Application.isPlaying) return;

            var transform = curve.transform;
            if (position != transform.position || rotation != transform.rotation || scale != transform.lossyScale)
            {
                Update();
                curve.FireChange(null);
            }
        }
    }
}