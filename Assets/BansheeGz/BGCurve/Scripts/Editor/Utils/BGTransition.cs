using UnityEngine;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
    public abstract class BGTransition
    {
        private readonly bool cycle;
//        private readonly double startTime;
        private readonly double period;

        private double cycleStartTime;
        private float ratio;

        private bool completed;

        public float Ratio
        {
            get { return ratio; }
        }


        protected BGTransition(double period, bool cycle)
        {
            this.period = period;
            this.cycle = cycle;
//            startTime = cycleStartTime = EditorApplication.timeSinceStartup;
        }

        public virtual bool Tick()
        {
            if (completed) return true;

            var elapsed = (float) (EditorApplication.timeSinceStartup - cycleStartTime);
            var cycleEnded = elapsed > period;

            if (cycleEnded)
            {
                elapsed = 0;
                cycleStartTime = EditorApplication.timeSinceStartup;

                if (!cycle) completed = true;
            }
            ratio = (float) (elapsed/period);

            return cycleEnded;
        }

        public class SimpleTransition : BGTransition
        {
            public SimpleTransition(double period, bool cycle)
                : base(period, cycle)
            {
            }
        }

        public class SwayTransition : BGTransition
        {
            private readonly float from;
            private readonly float to;

            private bool up = true;

            public float Value { get; set; }

            public SwayTransition(float @from, float to, double period)
                : base(period, true)
            {
                this.from = from;
                this.to = to;
                Value = from;
            }

            public override bool Tick()
            {
                if (base.Tick()) up = !up;

                Value = up ? Mathf.Lerp(from, to, Ratio) : Mathf.Lerp(to, from, Ratio);

                return true;
            }
        }
    }
}