using UnityEngine;
using System.Collections.Generic;
using BansheeGz.BGSpline.Components;
using BansheeGz.BGSpline.Curve;

namespace BansheeGz.BGSpline.Example
{
    //for 1.1 version Demo scene
    public class BGTestCurveShowcase : MonoBehaviour
    {
        [Header("Light")] public Light Light;

        [Header("Logo parts")] public BGCcCursorObjectTranslate B;
        public BGCcCursorObjectTranslate G;
        public BGCcMath Curve;

        [Header("Projectiles")] public GameObject ProjectileFolder;
        public TrailRenderer Projectile1;
        public TrailRenderer Projectile2;
        public BGCcCursor ProjectileCurve1;

        [Header("Particles")] public ParticleSystem BParticles;
        public ParticleSystem GParticles;
        public ParticleSystem CurveParticles1;
        public ParticleSystem CurveParticles2;

        //============== dynamic
        [Header("Dynamic")] public GameObject DynamicCurve;
        public Light Light1;
        public Light Light2;
        public Light Light3;

        //============== some useless stuff
        private readonly List<Effect> effects = new List<Effect>();


        //============== misc
        private const float ScaleMin = 0.85f;
        private const float ScaleMax = 1.15f;
        private static readonly Vector3 FromScale = new Vector3(ScaleMin, ScaleMin, ScaleMin);
        private static readonly Vector3 ToScale = new Vector3(ScaleMax, ScaleMax, ScaleMax);
        private const float ScalePeriodMin = 1;
        private const float ScalePeriodMax = 2;


        //============== Unity callbacks
        // Use this for initialization
        private void Start()
        {
            //comment the effect to disable it
            // scale the whole thing
            effects.Add(new EffectScale(gameObject, ScalePeriodMin, ScalePeriodMax, FromScale, ToScale));

            // scale parts 
            effects.Add(new EffectScale(B.gameObject, ScalePeriodMin, ScalePeriodMax, FromScale, ToScale));
            effects.Add(new EffectScale(G.gameObject, ScalePeriodMin, ScalePeriodMax, FromScale, ToScale));
            effects.Add(new EffectScale(Curve.gameObject, ScalePeriodMin, ScalePeriodMax, FromScale, ToScale));

            //rotate letters
            effects.Add(new EffectRotate(EffectRotate.CycleType.Random, B.gameObject, 1f, Vector3.zero, new Vector3(0, 360, 0), 2, 3));
            effects.Add(new EffectRotate(EffectRotate.CycleType.Random, G.gameObject, 1.6f, Vector3.zero, new Vector3(0, 360, 0), 4, 6));

            //particles
            // particles movements are done with Cc components

            //change tiling (on shared material)
            effects.Add(new EffectChangeTiling(2, B.GetComponent<LineRenderer>().sharedMaterial, 0, 0, .2f, 1));

            //rotate light
            effects.Add(new EffectRotate(EffectRotate.CycleType.Swing, Light.gameObject, 3f, new Vector3(70, -90, 0), new Vector3(110, -90, 0)));

            //projectiles
            effects.Add(new EffectRotate(EffectRotate.CycleType.FirstToLast, ProjectileFolder, 10, Vector3.zero, new Vector3(360, 0, 0)));
            effects.Add(new EffectMoveAndRotateAlongCurve(ProjectileCurve1, Projectile1.gameObject, 3, 5, .1f));
            effects.Add(new EffectMoveAndRotateAlongCurve(ProjectileCurve1, Projectile2.gameObject, 3, 5, .1f, Mathf.PI));

            //dynamic
            effects.Add(new EffectDynamicCurve(DynamicCurve, 4, Light1, Light2, Light3));
        }

        // Update is called once per frame
        private void Update()
        {
            foreach (var effect in effects) effect.Update();
        }


        //========================================== Abstract Phase
        private abstract class Phase
        {
            private readonly float periodMin;
            private readonly float periodMax;

            internal float period = -10000;
            internal float startTime;

            protected Phase(float periodMin, float periodMax)
            {
                this.periodMin = periodMin;
                this.periodMax = periodMax;
            }

            protected internal virtual void PhaseStart()
            {
                startTime = Time.time;
                period = Random.Range(periodMin, periodMax);
            }

            public abstract void Update();
        }

        private sealed class PhaseDelay : Phase
        {
            public PhaseDelay(float periodMin, float periodMax) : base(periodMin, periodMax)
            {
            }

            public override void Update()
            {
            }
        }

        //========================================== Abstract Effect (compound phase)
        private abstract class Effect : Phase
        {
            private readonly List<Phase> phases = new List<Phase>();

            private int currentPhaseIndex;

            protected Effect(float periodMin, float periodMax) : base(periodMin, periodMax)
            {
                phases.Add(this);
            }

            protected void AddPhase(Phase phase)
            {
                phases.Add(phase);
            }

            protected void AddPhase(Phase phase, int index)
            {
                phases.Insert(index, phase);
            }

            public override void Update()
            {
                var phase = phases[currentPhaseIndex];

                var newPhase = false;
                if (Time.time - phase.startTime > phase.period)
                {
                    //next phase
                    newPhase = true;
                    currentPhaseIndex++;
                    if (currentPhaseIndex == phases.Count) currentPhaseIndex = 0;

                    phase = phases[currentPhaseIndex];
                    phase.PhaseStart();
                }

                if (phase is Effect)
                {
                    var effect = (Effect) phase;

                    if (newPhase) effect.Start();

                    effect.Update((Time.time - phase.startTime)/phase.period);
                }
            }

            protected float CheckReverse(float ratio, bool reverse)
            {
                return reverse ? (1 - ratio) : ratio;
            }

            protected abstract void Update(float ratio);

            protected virtual void Start()
            {
            }

            //remap ratio value to new range (for example, if count=2, .25 and 0.75 will become 0.5)
            protected static float Scale(float ratio, float count)
            {
                var smallPeriod = 1f/count;
                return (ratio - Mathf.FloorToInt(ratio/smallPeriod)*smallPeriod)/smallPeriod;
            }
        }


        //========================================== Change Scale
        private sealed class EffectScale : Effect
        {
            private readonly GameObject target;

            private Vector3 min;
            private Vector3 max;
            private Vector3 oldScale;
            private Vector3 newScale;

            public EffectScale(GameObject target, float periodMin, float periodMax, Vector3 min, Vector3 max)
                : base(periodMin, periodMax)
            {
                this.target = target;
                newScale = target.transform.localScale;
                this.min = min;
                this.max = max;
            }

            protected override void Update(float ratio)
            {
                target.transform.localScale = Vector3.Lerp(oldScale, newScale, ratio);
            }

            protected override void Start()
            {
                oldScale = newScale;
                newScale = new Vector3(Random.Range(min.x, max.x), Random.Range(min.y, max.y), Random.Range(min.z, max.z));
            }
        }

        //========================================== Change Rotate
        private sealed class EffectRotate : Effect
        {
            internal enum CycleType
            {
                FirstToLast,
                Swing,
                Random
            }

            private readonly GameObject target;
            private readonly Vector3 min;
            private readonly Vector3 max;
            private readonly CycleType cycleType;

            private bool reverse;

            //full 360 on y
            public EffectRotate(CycleType cycleType, GameObject target, float period, Vector3 min, Vector3 max, float delayMin, float delayMax)
                : this(cycleType, target, period, min, max)
            {
                AddPhase(new PhaseDelay(delayMin, delayMax), 0);
            }

            public EffectRotate(CycleType cycleType, GameObject target, float period, Vector3 min, Vector3 max)
                : base(period, period)
            {
                this.target = target;
                this.cycleType = cycleType;
                this.min = min;
                this.max = max;
            }

            protected override void Update(float ratio)
            {
                target.transform.eulerAngles = Vector3.Lerp(min, max, CheckReverse(ratio, reverse));
            }

            protected override void Start()
            {
                switch (cycleType)
                {
                    case CycleType.FirstToLast:
                        reverse = false;
                        break;
                    case CycleType.Swing:
                        reverse = !reverse;
                        break;
                    default:
                        reverse = Random.Range(0, 2) == 0;
                        break;
                }
            }
        }

        //========================================== Change tiling
        private sealed class EffectChangeTiling : Effect
        {
            private readonly float tileXMin;
            private readonly float tileXMax;
            private readonly float tileYMin;
            private readonly float tileYMax;
            private readonly Material material;

            private bool reverse;

            public EffectChangeTiling(float period, Material material, float tileXMin, float tileXMax, float tileYMin, float tileYMax)
                : base(period, period)
            {
                this.material = material;
                this.tileXMin = tileXMin;
                this.tileXMax = tileXMax;
                this.tileYMin = tileYMin;
                this.tileYMax = tileYMax;
            }

            protected override void Update(float ratio)
            {
                ratio = CheckReverse(ratio, reverse);
                material.mainTextureScale = new Vector2(Mathf.Lerp(tileXMin, tileXMax, ratio), Mathf.Lerp(tileYMin, tileYMax, ratio));
            }

            protected override void Start()
            {
                reverse = !reverse;
            }
        }


        //==============================================================================================================
        //                                                                  Working with BGCurve
        //==============================================================================================================
        //========================================== Move object along and rotate around tangent
        private sealed class EffectMoveAndRotateAlongCurve : Effect
        {
            private readonly GameObject target;
            private readonly BGCcCursor cursor;
            private readonly float rotateCount;
            private readonly float rotationDistance;
            private readonly float initialRotationRadians;

            public EffectMoveAndRotateAlongCurve(BGCcCursor cursor, GameObject target, float period, int rotateCount, float rotationDistance, float initialRotationRadians = 0)
                : base(period, period)
            {
                this.target = target;
                this.cursor = cursor;
                this.rotateCount = rotateCount;
                this.rotationDistance = rotationDistance;
                this.initialRotationRadians = initialRotationRadians;
            }

            protected override void Update(float ratio)
            {
                var position = cursor.CalculatePosition();
                //Tangent should be included in Math calculation
                var tangent = cursor.CalculateTangent();

                var angle = initialRotationRadians + Mathf.Lerp(0, Mathf.PI*2, Scale(ratio, rotateCount));
                var pos = position + Quaternion.LookRotation(tangent)*(new Vector3(Mathf.Sin(angle), Mathf.Cos(angle))*rotationDistance);

                target.transform.position = pos;
            }
        }

        //========================================== creating BGCurve at runtime
        private sealed class EffectDynamicCurve : Effect
        {
            private const int PointsCount = 3;
            private const float SpanX = 8;
            private const float SpanZ = 4;

            private readonly BGCcMath math;
            private readonly Light[] lights;
            private readonly float[] fromDistanceRatios;
            private readonly float[] toDistanceRatios;

            public EffectDynamicCurve(GameObject target, float period, params Light[] lights)
                : base(period, period)
            {
                target.AddComponent<BGCurve>();
                math = target.AddComponent<BGCcMath>();
                math.Curve.Closed = true;
                this.lights = lights;
                fromDistanceRatios = new float[lights.Length];
                toDistanceRatios = new float[lights.Length];
            }

            protected override void Update(float ratio)
            {
                for (var i = 0; i < lights.Length; i++)
                {
                    var light = lights[i];

                    //move
                    light.gameObject.transform.position = math.Math.CalcByDistanceRatio(BGCurveBaseMath.Field.Position, Mathf.Lerp(fromDistanceRatios[i], toDistanceRatios[i], ratio));

                    if (ratio < .1)
                    {
                        //lights on
                        light.intensity = Mathf.Lerp(0, 3, ratio*10);
                    }
                    else if (ratio > .9)
                    {
                        //lights off
                        light.intensity = Mathf.Lerp(3, 0, (ratio - .9f)*10);
                    }
                }
            }

            protected override void Start()
            {
                var curve = math.Curve;
                curve.Clear();
                for (var i = 0; i < PointsCount; i++) AddPoint(curve);

                for (var i = 0; i < fromDistanceRatios.Length; i++)
                {
                    fromDistanceRatios[i] = Random.Range(0f, 1);
                    toDistanceRatios[i] = Random.Range(0f, 1);
                }
            }

            private void AddPoint(BGCurve curve)
            {
                var control = RandomVector();
                curve.AddPoint(new BGCurvePoint(curve, RandomVector(), BGCurvePoint.ControlTypeEnum.BezierSymmetrical, control, -control));
            }

            private Vector3 RandomVector()
            {
                return new Vector3(Random.Range(-SpanX, SpanX), 0, Random.Range(-SpanZ, SpanZ));
            }
        }
    }
}