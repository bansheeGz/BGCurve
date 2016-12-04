using System;
using System.Collections.Generic;
using UnityEngine;
using BansheeGz.BGSpline.Curve;

namespace BansheeGz.BGSpline.Editor
{
    public class BGTransformMonitor
    {
        private static readonly Queue<BGTransformMonitor> Pool = new Queue<BGTransformMonitor>();

        private Vector3 position;
        private Quaternion rotation;
        private Vector3 scale;

        private Transform transform;
        private Action<Transform> changed;

        private BGTransformMonitor(Transform transform, Action<Transform> changed)
        {
            Update(transform, changed);
        }

        public static BGTransformMonitor GetMonitor(Transform transform, Action<Transform> changed)
        {
            if (Pool.Count == 0) return new BGTransformMonitor(transform, changed);

            var monitor = Pool.Dequeue();
            monitor.transform = transform;
            monitor.changed = changed;

            return monitor;
        }

        public static BGTransformMonitor GetMonitor(BGCurve curve)
        {
            return GetMonitor(curve.transform, transform1 => { curve.FireChange(null); });
        }


        public bool CheckForChange(bool skipAction = false)
        {
            if (Application.isPlaying || changed == null || transform == null) return false;

            if (position == transform.position && rotation == transform.rotation && scale == transform.lossyScale) return false;

            Update();

            if (!skipAction) changed(transform);

            return true;
        }

        public void Release()
        {
            transform = null;
            changed = null;

            Pool.Enqueue(this);
        }


        private void Update(Transform transform, Action<Transform> changed)
        {
            this.transform = transform;
            this.changed = changed;
            Update();
        }

        private void Update()
        {
            position = transform.position;
            rotation = transform.rotation;
            scale = transform.lossyScale;
        }
    }
}