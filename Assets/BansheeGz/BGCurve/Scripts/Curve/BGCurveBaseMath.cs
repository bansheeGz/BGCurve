using System;
using UnityEngine;

namespace BansheeGz.BGSpline.Curve
{
    /// <summary>  Basic math operations for curves (Distance, Position, Tangent, Closest point ). It caches some data for quick access and recalculate it if curve changes. </summary>
    public class BGCurveBaseMath : IDisposable
    {
        #region events

        /// <summary> Change was requested by changed curve's data </summary>
        public event EventHandler ChangeRequested;
        /// <summary> Math was changed</summary>
        public event EventHandler Changed;

        #endregion

        #region fields

        /// <summary> all possible point's attributes </summary>
        public enum Field
        {
            Position = 1,
            Tangent = 2,
        }

        /// <summary> which fields to precalculate </summary>
        public enum Fields
        {
            Position = 1,
            PositionAndTangent = 3,
        }

        protected readonly BGCurve curve;
        //what and how to calc
        protected Config config;

        //cached data for length calculations
        protected SectionInfo[] cachedSectionInfos = new SectionInfo[0];

        //curve's length
        protected float cachedLength;

        //parsed target fields
        protected bool cachePosition;
        protected bool cacheTangent;

        //closest point
        protected BGCurveCalculatorClosestPoint closestPointCalculator;

        // some useless system stuff 
        private int lastFrameCount = -1;
        private int createdFrameCount;

        // cached data (t=some ratio [0,1], tr=1-t, t2=t*t, etc.).
        //position
        private float[] bakedT;
        private float[] bakedT2;
        private float[] bakedTr2;
        private float[] bakedT3;
        private float[] bakedTr3;
        private float[] bakedTr2xTx3;
        private float[] bakedT2xTrx3;
        private float[] bakedTxTrx2;

        //tangent related
        private float[] bakedTr2x3;
        private float[] bakedTxTrx6;
        private float[] bakedT2x3;
        private float[] bakedTx2;
        private float[] bakedTrx2;
        private bool ignoreSectionChangedCheck;

        //suppress all warnings, printed to the log
        public bool SuppressWarning { get; set; }

        public BGCurve Curve
        {
            get { return curve; }
        }

        public SectionInfo[] SectionInfos
        {
            get { return cachedSectionInfos; }
        }

        public SectionInfo this[int i]
        {
            get { return cachedSectionInfos[i]; }
//            private set { sections[i] = value; }
        }

        public int SectionsCount
        {
            get { return cachedSectionInfos.Length; }
        }

        public Config Configuration
        {
            get { return config; }
        }

        private bool NeedTangentFormula
        {
            get { return !config.UsePointPositionsToCalcTangents && cacheTangent; }
        }

        #endregion

        #region constructors

        /// <summary> Math with default config settings </summary>
        public BGCurveBaseMath(BGCurve curve) : this(curve, new Config(Fields.Position))
        {
        }

        /// <summary> Math with config settings </summary>
        public BGCurveBaseMath(BGCurve curve, Config config)
        {
            this.curve = curve;
            curve.Changed += CurveChanged;
            Init(config ?? new Config(Fields.Position));
        }

        /// <summary>This is an old constructor, left for compatibility. Only Fields.PositionWorld is calculated. 
        /// Use another constructor to specify which fields and how need to be calculated. </summary>
        [Obsolete("Use another constructors")]
        public BGCurveBaseMath(BGCurve curve, bool traceChanges, int parts = 30, bool usePointPositionsToCalcTangents = false)
            : this(curve, new Config(Fields.Position) {Parts = parts, UsePointPositionsToCalcTangents = usePointPositionsToCalcTangents})
        {
        }

        #endregion

        #region Init

        public void Init(Config config)
        {
            //if we calculated positions before (but not tangent) we should skip the check if section was changed or not and force recalculation (cause positions may not change)
            if (this.config != null)
            {
                ignoreSectionChangedCheck = this.config.Fields == Fields.Position && config.Fields == Fields.PositionAndTangent;
                this.config.Update -= ConfigOnUpdate;
            }
            else
            {
                ignoreSectionChangedCheck = false;
            }
            

            this.config = config;
            config.Parts = Mathf.Clamp(config.Parts, 1, 1000);
            this.config.Update += ConfigOnUpdate;

            createdFrameCount = Time.frameCount;
            cachePosition = Field.Position.In(config.Fields.Val());
            cacheTangent = Field.Tangent.In(config.Fields.Val());

            //No fields- no need to calculate or trace anything.
            if (!cachePosition && !cacheTangent)
                throw new UnityException("No fields were chosen. Create math like this: new BGCurveBaseMath(curve, new BGCurveBaseMath.Config(BGCurveBaseMath.Fields.Position))");


            //let's bake some data
            var parts = config.Parts;
            var sectionPointsCount = parts + 1;

            Array.Resize(ref bakedT, sectionPointsCount);

            Array.Resize(ref bakedT2, sectionPointsCount);
            Array.Resize(ref bakedTr2, sectionPointsCount);
            Array.Resize(ref bakedT3, sectionPointsCount);
            Array.Resize(ref bakedTr3, sectionPointsCount);

            Array.Resize(ref bakedTr2xTx3, sectionPointsCount);
            Array.Resize(ref bakedT2xTrx3, sectionPointsCount);
            Array.Resize(ref bakedTxTrx2, sectionPointsCount);

            if (NeedTangentFormula)
            {
                Array.Resize(ref bakedTr2x3, sectionPointsCount);
                Array.Resize(ref bakedTxTrx6, sectionPointsCount);
                Array.Resize(ref bakedT2x3, sectionPointsCount);
                Array.Resize(ref bakedTx2, sectionPointsCount);
                Array.Resize(ref bakedTrx2, sectionPointsCount);
            }

            for (var i = 0; i <= parts; i++)
            {
                var t = i/(float) parts;
                var tr = 1 - t;
                var t2 = t*t;
                var tr2 = tr*tr;

                bakedT[i] = t;

                bakedT2[i] = t2;
                bakedTr2[i] = tr2;
                bakedT3[i] = t2*t;
                bakedTr3[i] = tr2*tr;

                bakedTr2xTx3[i] = 3*tr2*t;
                bakedT2xTrx3[i] = 3*tr*t2;
                bakedTxTrx2[i] = 2*tr*t;

                if (!NeedTangentFormula) continue;


                bakedTr2x3[i] = 3*tr2;
                bakedTxTrx6[i] = 6*tr*t;
                bakedT2x3[i] = 3*t2;
                bakedTx2[i] = 2*t;
                bakedTrx2[i] = 2*tr;
            }

            Recalculate(true);
        }

        #endregion

        #region Public methods

        //=========================================== Calculate by formula

        /// <summary> Calculate point world position between 2 points by a formula.</summary>
        /// <param name="t">Ratio between (0,1)</param>
        /// <param name="useLocal">Use local coordinates instead of world</param>
        public virtual Vector3 CalcPositionByT(BGCurvePoint @from, BGCurvePoint to, float t, bool useLocal = false)
        {
            t = Mathf.Clamp01(t);

            var fromPos = useLocal ? from.PositionLocal : from.PositionWorld;
            var toPos = useLocal ? to.PositionLocal : to.PositionWorld;

            Vector3 result;
            if (from.ControlType == BGCurvePoint.ControlTypeEnum.Absent && to.ControlType == BGCurvePoint.ControlTypeEnum.Absent)
            {
                //lerp
                result = fromPos + ((toPos - fromPos)*t);
            }
            else
            {
                var fromPosHandle = useLocal ? from.ControlSecondLocal + fromPos : from.ControlSecondWorld;
                var toPosHandle = useLocal ? to.ControlFirstLocal + toPos : to.ControlFirstWorld;

                result = (from.ControlType != BGCurvePoint.ControlTypeEnum.Absent && to.ControlType != BGCurvePoint.ControlTypeEnum.Absent)
                    ? BGCurveFormulas.BezierCubic(t, fromPos, fromPosHandle, toPosHandle, toPos)
                    : BGCurveFormulas.BezierQuadratic(t, fromPos, (@from.ControlType == BGCurvePoint.ControlTypeEnum.Absent) ? toPosHandle : fromPosHandle, toPos);
            }
            return result;
        }

        /// <summary> Calculate a tangent between 2 points by a formula. </summary>
        /// <param name="t">Ratio between (0,1)</param>
        /// <param name="useLocal">Use local coordinates instead of world</param>
        public virtual Vector3 CalcTangentByT(BGCurvePoint @from, BGCurvePoint to, float t, bool useLocal = false)
        {
            if (Curve.PointsCount < 2) return Vector3.zero;

            t = Mathf.Clamp01(t);

            var fromPos = useLocal ? from.PositionLocal : from.PositionWorld;
            var toPos = useLocal ? to.PositionLocal : to.PositionWorld;

            Vector3 result;
            if (from.ControlType == BGCurvePoint.ControlTypeEnum.Absent && to.ControlType == BGCurvePoint.ControlTypeEnum.Absent)
            {
                result = toPos - fromPos;
            }
            else
            {
                var fromPosHandle = useLocal ? from.ControlSecondLocal + fromPos : from.ControlSecondWorld;
                var toPosHandle = useLocal ? to.ControlFirstLocal + toPos : to.ControlFirstWorld;

                result = (from.ControlType != BGCurvePoint.ControlTypeEnum.Absent && to.ControlType != BGCurvePoint.ControlTypeEnum.Absent)
                    ? BGCurveFormulas.BezierCubicDerivative(t, fromPos, fromPosHandle, toPosHandle, toPos)
                    : BGCurveFormulas.BezierQuadraticDerivative(t, fromPos, (@from.ControlType == BGCurvePoint.ControlTypeEnum.Absent) ? toPosHandle : fromPosHandle, toPos);
            }

            return result.normalized;
        }


        //=========================================== Generic methods

        /// <summary> Calculate both curve's fields (position and tangent) by distance ratio. Using local coordinates is significantly slower.  </summary>
        /// <param name="distanceRatio">Ratio between (0,1)</param>
        /// <param name="tangent">result tangent</param>
        /// <param name="useLocal">Use local coordinates instead of world (significantly slower)</param>
        /// <returns>result position</returns>
        public virtual Vector3 CalcByDistanceRatio(float distanceRatio, out Vector3 tangent, bool useLocal = false)
        {
            return CalcByDistance(cachedLength*distanceRatio, out tangent, useLocal);
        }

        /// <summary> Calculate both curve's fields (position and tangent) by distance. Using local coordinates is significantly slower.  </summary>
        /// <param name="distance">distance from the curve's start between (0, GetDistance())</param>
        /// <param name="tangent">result tangent</param>
        /// <param name="useLocal">Use local coordinates instead of world (significantly slower)</param>
        /// <returns>result position</returns>
        public virtual Vector3 CalcByDistance(float distance, out Vector3 tangent, bool useLocal = false)
        {
            if (distance < 0f) distance = 0f;
            else if (distance > cachedLength) distance = cachedLength;

            Vector3 position;
            BinarySearchByDistance(distance, out position, out tangent, true, true);

            if (useLocal)
            {
                position = curve.transform.InverseTransformPoint(position);
                tangent = curve.transform.InverseTransformDirection(tangent);
            }

            return position;
        }

        /// <summary> Calculate curve's field value by distance ratio. Using local coordinates is significantly slower.  </summary>
        /// <param name="field">field to retrieve (like position or tangent etc.)</param>
        /// <param name="distanceRatio">Ratio between (0,1)</param>
        /// <param name="useLocal">Use local coordinates instead of world (significantly slower)</param>
        public virtual Vector3 CalcByDistanceRatio(Field field, float distanceRatio, bool useLocal = false)
        {
            return CalcByDistance(field, cachedLength*distanceRatio, useLocal);
        }

        /// <summary> Calculate curve's field value by distance. Using local coordinates is significantly slower.</summary>
        /// <param name="field">field to retrieve</param>
        /// <param name="distance">distance from the curve's start between (0, GetDistance())</param>
        /// <param name="useLocal">Use local coordinates instead of world (significantly slower)</param>
        public virtual Vector3 CalcByDistance(Field field, float distance, bool useLocal = false)
        {
            if (distance < 0f) distance = 0f;
            else if (distance > cachedLength) distance = cachedLength;

            var calcPosition = field == Field.Position;
            Vector3 position, tangent;
            BinarySearchByDistance(distance, out position, out tangent, calcPosition, !calcPosition);

            if (useLocal)
            {
                switch (field)
                {
                    case Field.Position:
                        position = curve.transform.InverseTransformPoint(position);
                        break;
                    case Field.Tangent:
                        tangent = curve.transform.InverseTransformDirection(tangent);
                        break;
                }
            }

            return calcPosition ? position : tangent;
        }

        //=========================================== Position and Tangent

        /// <summary> Calculate approximate curve's point position using distance ratio. Using local coordinates is significantly slower. </summary>
        /// <param name="distanceRatio">Ratio between (0,1)</param>
        /// <param name="tangent">result tangent</param>
        /// <param name="useLocal">Use local coordinates instead of world (significantly slower)</param>
        /// <returns>result position</returns>
        public virtual Vector3 CalcPositionAndTangentByDistanceRatio(float distanceRatio, out Vector3 tangent, bool useLocal = false)
        {
            return CalcByDistanceRatio(distanceRatio, out tangent, useLocal);
        }

        /// <summary> Calculate approximate curve's point position using distance. Using local coordinates is significantly slower. </summary>
        /// <param name="distance">distance from curve's start between (0, GetDistance())</param>
        /// <param name="tangent">result tangent</param>
        /// <param name="useLocal">Use local coordinates instead of world (significantly slower)</param>
        /// <returns>result position</returns>
        public virtual Vector3 CalcPositionAndTangentByDistance(float distance, out Vector3 tangent, bool useLocal = false)
        {
            return CalcByDistance(distance, out tangent, useLocal);
        }

        //=========================================== Position 

        /// <summary> Calculate approximate curve's point position using distance ratio. Using local coordinates is significantly slower. </summary>
        /// <param name="distanceRatio">Ratio between (0,1)</param>
        /// <param name="useLocal">Use local coordinates instead of world (significantly slower)</param>
        public virtual Vector3 CalcPositionByDistanceRatio(float distanceRatio, bool useLocal = false)
        {
            return CalcByDistanceRatio(Field.Position, distanceRatio, useLocal);
        }

        /// <summary> Calculate approximate curve's point position using distance. Using local coordinates is significantly slower. </summary>
        /// <param name="distance">distance from curve's start between (0, GetDistance())</param>
        /// <param name="useLocal">Use local coordinates instead of world (significantly slower)</param>
        public virtual Vector3 CalcPositionByDistance(float distance, bool useLocal = false)
        {
            return CalcByDistance(Field.Position, distance, useLocal);
        }

        //=========================================== Tangent

        /// <summary> Calculate approximate curve's tangent using distance ratio. Using local coordinates is significantly slower. </summary>
        /// <param name="distanceRatio">Ratio between (0,1)</param>
        /// <param name="useLocal">Use local coordinates instead of world (significantly slower)</param>
        public virtual Vector3 CalcTangentByDistanceRatio(float distanceRatio, bool useLocal = false)
        {
            return CalcByDistanceRatio(Field.Tangent, distanceRatio, useLocal);
        }

        /// <summary> Calculate approximate curve's tangent using distance. Using local coordinates is significantly slower. </summary>
        /// <param name="distance">distance from curve's start between (0, GetDistance())</param>
        /// <param name="useLocal">Use local coordinates instead of world (significantly slower)</param>
        public virtual Vector3 CalcTangentByDistance(float distance, bool useLocal = false)
        {
            return CalcByDistance(Field.Tangent, distance, useLocal);
        }


        //=========================================== Find Secton's index

        /// <summary> Calculate curve's section using distance. </summary>
        public int CalcSectionIndexByDistance(float distance)
        {
            return FindSectionIndexByDistance(ClampDistance(distance));
        }

        /// <summary> Calculate curve's section using distance ratio. </summary>
        public int CalcSectionIndexByDistanceRatio(float ratio)
        {
            return FindSectionIndexByDistance(DistanceByRatio(ratio));
        }

        //=========================================== Closest Point

        /// <summary> Calculate curve's world point position by a point, which is closest to a given point.</summary>
        public Vector3 CalcPositionByClosestPoint(Vector3 point, bool skipSectionsOptimization = false, bool skipPointsOptimization = false)
        {
            if (closestPointCalculator == null) closestPointCalculator = new BGCurveCalculatorClosestPoint(this);
            float distance;
            Vector3 tangent;
            return closestPointCalculator.CalcPositionByClosestPoint(point, out distance, out tangent, skipSectionsOptimization, skipPointsOptimization);
        }

        /// <summary> Calculate curve's world point position and distance by a point, which is closest to a given point.</summary>
        public Vector3 CalcPositionByClosestPoint(Vector3 point, out float distance, bool skipSectionsOptimization = false, bool skipPointsOptimization = false)
        {
            if (closestPointCalculator == null) closestPointCalculator = new BGCurveCalculatorClosestPoint(this);
            Vector3 tangent;
            return closestPointCalculator.CalcPositionByClosestPoint(point, out distance, out tangent, skipSectionsOptimization, skipPointsOptimization);
        }

        /// <summary> Calculate curve's world point position, distance and tangent by a point, which is closest to a given point.</summary>
        public Vector3 CalcPositionByClosestPoint(Vector3 point, out float distance, out Vector3 tangent, bool skipSectionsOptimization = false, bool skipPointsOptimization = false)
        {
            if (closestPointCalculator == null) closestPointCalculator = new BGCurveCalculatorClosestPoint(this);
            return closestPointCalculator.CalcPositionByClosestPoint(point, out distance, out tangent, skipSectionsOptimization, skipPointsOptimization);
        }


        //=========================================== Total Distance

        /// <summary>Get curve's approximate distance</summary>
        public virtual float GetDistance()
        {
            return cachedLength;
        }

        //=========================================== Curve's point world coordinates (faster then using point.positionWord etc.)

        /// <summary> Get point's world position </summary>
        public Vector3 GetPosition(int pointIndex)
        {
            if (cachedSectionInfos.Length == 0 || cachedSectionInfos.Length <= pointIndex) return curve[pointIndex].PositionWorld;

            return pointIndex < cachedSectionInfos.Length ? cachedSectionInfos[pointIndex].OriginalFrom : cachedSectionInfos[pointIndex - 1].OriginalTo;
        }

        /// <summary> Get point's world first control position </summary>
        public Vector3 GetControlFirst(int pointIndex)
        {
            if (cachedSectionInfos.Length == 0) return curve[pointIndex].ControlFirstWorld;

            if (pointIndex == 0) return curve.Closed ? cachedSectionInfos[cachedSectionInfos.Length - 1].OriginalToControl : curve[0].ControlFirstWorld;

            return cachedSectionInfos[pointIndex - 1].OriginalToControl;
        }

        /// <summary> Get point's world second control position </summary>
        public Vector3 GetControlSecond(int pointIndex)
        {
            if (cachedSectionInfos.Length == 0) return curve[pointIndex].ControlSecondWorld;

            if (pointIndex == cachedSectionInfos.Length) return curve.Closed ? cachedSectionInfos[cachedSectionInfos.Length - 1].OriginalFromControl : curve[pointIndex].ControlSecondWorld;

            return cachedSectionInfos[pointIndex].OriginalFromControl;
        }


        //=========================================== Misc

        /// <summary>Returns true, if given field is Calculated and Cached for usage</summary>
        public virtual bool IsCalculated(Field field)
        {
            return ((int) field & (int) config.Fields) != 0;
        }

        public void Dispose()
        {
            curve.Changed -= CurveChanged;
            config.Update -= ConfigOnUpdate;
            cachedSectionInfos = new SectionInfo[0];

            var emptyArray = new float[0];
            bakedT = emptyArray;
            bakedT2 = emptyArray;
            bakedTr2 = emptyArray;
            bakedT3 = emptyArray;
            bakedTr3 = emptyArray;
            bakedTr2xTx3 = emptyArray;
            bakedT2xTrx3 = emptyArray;
            bakedTxTrx2 = emptyArray;

            bakedTr2x3 = emptyArray;
            bakedTxTrx6 = emptyArray;
            bakedT2x3 = emptyArray;
            bakedTx2 = emptyArray;
            bakedTrx2 = emptyArray;
        }


        //=========================================== Calculate and cache required data

        /// <summary>Calculates and cache all required data for performance reason. It is an expensive operation. </summary>
        public virtual void Recalculate(bool force = false)
        {
            if (ChangeRequested != null) ChangeRequested(this, null);

            if (!force && config.ShouldUpdate != null && !config.ShouldUpdate()) return;

            if (curve.PointsCount < 2)
            {
                cachedLength = 0;
                if (cachedSectionInfos.Length > 0) cachedSectionInfos = new SectionInfo[0];
                if (Changed != null) Changed(this, null);
                return;
            }

            //we should at least warn about non-optimal usage of Recalculate (with more than 1 update per frame)
            if (lastFrameCount != Time.frameCount || Time.frameCount == createdFrameCount) lastFrameCount = Time.frameCount;
            else
                Warning("We noticed you are updating math more than once per frame. This is not optimal. " +
                        "If you use curve.ImmediateChangeEvents by some reason, try to use curve.Transaction to wrap all the changes to one single event.");


            var pointsCount = curve.PointsCount;
            var sectionsCount = curve.Closed ? pointsCount : pointsCount - 1;

            //here we try to reuse existing objects to reduce GC and objects memory allocation overhead (pooling could be a better idea..)
            Resize(ref cachedSectionInfos, sectionsCount);

            //calculate fields for each section
            for (var i = 0; i < pointsCount - 1; i++) CalculateSection(i, ref cachedSectionInfos[i], i == 0 ? null : cachedSectionInfos[i - 1], curve[i], curve[i + 1]);

            if (curve.Closed)
                CalculateSection(sectionsCount - 1, ref cachedSectionInfos[cachedSectionInfos.Length - 1], cachedSectionInfos[cachedSectionInfos.Length - 2], curve[pointsCount - 1], curve[0]);

            cachedLength = cachedSectionInfos[cachedSectionInfos.Length - 1].DistanceFromEndToOrigin;

            if (Changed != null) Changed(this, null);
        }

        #endregion

        #region protected methods

        //print a message to console if condition is met and calls callback method
        protected virtual void Warning(string message, bool condition = true, Action callback = null)
        {
            if (!condition || Application.isEditor) return;

            if (!SuppressWarning) Debug.Log("BGCurve[BGCurveBaseMath] Warning! " + message + ". You can suppress all warnings by using BGCurveBaseMath.SuppressWarning=true;");

            if (callback != null) callback();
        }


        //calculates one single section data
        // * this method may contain some intentional 1) copy/paste 2) methods inlining 3) operators inlining- to increase performance 
        // * for example: ~100 000 section points (1000 curve's points with 100 parts each) with both controls
        // ~10 ms for Position
        // ~18 ms for PositionAndTangent
        protected virtual void CalculateSection(int index, ref SectionInfo section, SectionInfo prevSection, BGCurvePoint @from, BGCurvePoint to)
        {
            if (section == null) section = new SectionInfo();

            section.DistanceFromStartToOrigin = prevSection == null ? 0 : prevSection.DistanceFromEndToOrigin;

            var straightAndOptimized = config.OptimizeStraightLines && @from.ControlType == BGCurvePoint.ControlTypeEnum.Absent && to.ControlType == BGCurvePoint.ControlTypeEnum.Absent;
            var pointsCount = straightAndOptimized ? 2 : config.Parts + 1;

            //do we need to recalc points?
            if (section.Reset(from, to, pointsCount, ignoreSectionChangedCheck))
            {
                if (straightAndOptimized)
                {
                    // =====================================================  Straight section with 2 points
                    //try to reuse existing objects to reduce GC
                    var points = Resize(ref section.points, pointsCount);

                    var startPoint = EnsurePoint(points, 0);
                    var endPoint = EnsurePoint(points, 1);

                    startPoint.Position = section.OriginalFrom;
                    endPoint.Position = section.OriginalTo;

                    //distance
                    endPoint.DistanceToSectionStart = Vector3.Distance(section.OriginalFrom, section.OriginalTo);

                    //tangents
                    if (cacheTangent) startPoint.Tangent = endPoint.Tangent = (endPoint.Position - startPoint.Position).normalized;
                }
                else
                {
                    // =====================================================  Section with {parts} smaller sections
                    //try to reuse existing objects to reduce GC
                    Resize(ref section.points, pointsCount);


                    //*****************************************************************************************************************************
                    //======================================== 
                    //                    Calculate points
                    //========================================
                    //-----------section data
                    var fromPos = section.OriginalFrom;
                    var toPos = section.OriginalTo;
                    var control1 = section.OriginalFromControl;
                    var control2 = section.OriginalToControl;

                    var controlFromAbsent = @from.ControlType == BGCurvePoint.ControlTypeEnum.Absent;
                    var controlToAbsent = to.ControlType == BGCurvePoint.ControlTypeEnum.Absent;
                    var noControls = controlFromAbsent && controlToAbsent;
                    var bothControls = !controlFromAbsent && !controlToAbsent;
                    if (!noControls && !bothControls && controlFromAbsent) control1 = control2;


                    //-----------calc some data

                    // no controls
                    Vector3 fromTo = Vector3.zero, fromToTangentWorld = Vector3.zero;
                    if (noControls)
                    {
                        fromTo = toPos - fromPos;
                        if (cacheTangent) fromToTangentWorld = (to.PositionWorld - @from.PositionWorld).normalized;
                    }

                    // tangent related
                    Vector3 control1MinusFrom = Vector3.zero, control2MinusControl1 = Vector3.zero, toMinusControl2 = Vector3.zero, toMinusControl1 = Vector3.zero;
                    if (!config.UsePointPositionsToCalcTangents && cacheTangent)
                    {
                        control1MinusFrom = control1 - fromPos;
                        if (bothControls)
                        {
                            control2MinusControl1 = control2 - control1;
                            toMinusControl2 = toPos - control2;
                        }
                        else
                        {
                            toMinusControl1 = toPos - control1;
                        }
                    }


                    //-----------  Critical block starts
                    for (var i = 0; i < bakedT.Length; i++)
                    {
                        var point = section.points[i] ?? (section.points[i] = new SectionPointInfo());

                        Vector3 pos;

                        if (noControls)
                        {
                            // =================  NoControls
                            // ---------- position 
                            var t = bakedT[i];
                            pos = new Vector3(fromPos.x + fromTo.x*t, fromPos.y + fromTo.y*t, fromPos.z + fromTo.z*t);

                            point.Position = pos;

                            //---------- tangents
                            if (cacheTangent) point.Tangent = fromToTangentWorld;
                        }
                        else
                        {
                            // =================  At least One control
                            //---------- position  

                            if (bothControls)
                            {
                                var tr3 = bakedTr3[i];
                                var tr2xTx3 = bakedTr2xTx3[i];
                                var t2xTrx3 = bakedT2xTrx3[i];
                                var t3 = bakedT3[i];

                                pos = new Vector3(tr3*fromPos.x + tr2xTx3*control1.x + t2xTrx3*control2.x + t3*toPos.x,
                                    tr3*fromPos.y + tr2xTx3*control1.y + t2xTrx3*control2.y + t3*toPos.y,
                                    tr3*fromPos.z + tr2xTx3*control1.z + t2xTrx3*control2.z + t3*toPos.z);
                            }
                            else
                            {
                                var tr2 = bakedTr2[i];
                                var txTrx2 = bakedTxTrx2[i];
                                var t2 = bakedT2[i];

                                pos = new Vector3(tr2*fromPos.x + txTrx2*control1.x + t2*toPos.x,
                                    tr2*fromPos.y + txTrx2*control1.y + t2*toPos.y,
                                    tr2*fromPos.z + txTrx2*control1.z + t2*toPos.z);
                            }

                            point.Position = pos;


                            //---------- tangents 
                            if (cacheTangent)
                            {
                                if (config.UsePointPositionsToCalcTangents)
                                {
                                    //-------- Calc by point's positions
                                    //we skip 1st point, cause we do not have enough info for it. we'll set it at the next step
                                    if (i != 0)
                                    {
                                        var prevPoint = section[i - 1];

                                        var prevPosition = prevPoint.Position;

                                        var tangent = new Vector3(pos.x - prevPosition.x, pos.y - prevPosition.y, pos.z - prevPosition.z);
                                        //Vector3.normalized inlined (tangent=tangent.normalized)
                                        var marnitude = (float) Math.Sqrt((double) tangent.x*(double) tangent.x + (double) tangent.y*(double) tangent.y + (double) tangent.z*(double) tangent.z);
                                        tangent = ((double) marnitude > 9.99999974737875E-06) ? new Vector3(tangent.x/marnitude, tangent.y/marnitude, tangent.z/marnitude) : Vector3.zero;

                                        prevPoint.Tangent = tangent;

                                        //we will adjust it later (if there is another section after this one , otherwise- no more data for more precise calculation)
                                        if (i == config.Parts) point.Tangent = prevPoint.Tangent;
                                    }
                                }
                                else
                                {
                                    //-------- Calc by a formula
                                    Vector3 tangent;
                                    if (bothControls)
                                    {
                                        var tr2x3 = bakedTr2x3[i];
                                        var txTrx6 = bakedTxTrx6[i];
                                        var t2x3 = bakedT2x3[i];
                                        tangent = new Vector3(tr2x3*control1MinusFrom.x + txTrx6*control2MinusControl1.x + t2x3*toMinusControl2.x,
                                            tr2x3*control1MinusFrom.y + txTrx6*control2MinusControl1.y + t2x3*toMinusControl2.y,
                                            tr2x3*control1MinusFrom.z + txTrx6*control2MinusControl1.z + t2x3*toMinusControl2.z);
                                    }
                                    else
                                    {
                                        var trx2 = bakedTrx2[i];
                                        var tx2 = bakedTx2[i];
                                        tangent = new Vector3(trx2*control1MinusFrom.x + tx2*toMinusControl1.x,
                                            trx2*control1MinusFrom.y + tx2*toMinusControl1.y,
                                            trx2*control1MinusFrom.z + tx2*toMinusControl1.z);
                                    }

                                    //Vector3.normalized inlined (tangent=tangent.normalized)
                                    var marnitude = (float) Math.Sqrt((double) tangent.x*(double) tangent.x + (double) tangent.y*(double) tangent.y + (double) tangent.z*(double) tangent.z);
                                    tangent = ((double) marnitude > 9.99999974737875E-06) ? new Vector3(tangent.x/marnitude, tangent.y/marnitude, tangent.z/marnitude) : Vector3.zero;

                                    // set tangent
                                    point.Tangent = tangent;
                                }
                            }
                        }


                        if (i == 0) continue;

                        // ---------- distance to section start (Vector3.Distance inlined)
                        var prevPos = section[i - 1].Position;
                        double x = pos.x - prevPos.x;
                        double y = pos.y - prevPos.y;
                        double z = pos.z - prevPos.z;
                        point.DistanceToSectionStart = section[i - 1].DistanceToSectionStart + ((float) Math.Sqrt(x*x + y*y + z*z));
                    }
                    //-----------  Critical block ends
                    //========================================
                    //                  Calculate points ends
                    //========================================
                    //*****************************************************************************************************************************
                }
                if (cacheTangent)
                {
                    section.OriginalFirstPointTangent = section[0].Tangent;
                    section.OriginalLastPointTangent = section[section.PointsCount - 1].Tangent;
                }
            }

            // we should adjust tangents for previous section's last point and first point of the current section
            if (cacheTangent && prevSection != null)
            {
                //both tangents get adjusted 
                section[0].Tangent = prevSection[prevSection.PointsCount - 1].Tangent = new Vector3(
                    section.OriginalFirstPointTangent.x + (prevSection.OriginalLastPointTangent.x - section.OriginalFirstPointTangent.x)*.5f,
                    section.OriginalFirstPointTangent.y + (prevSection.OriginalLastPointTangent.y - section.OriginalFirstPointTangent.y)*.5f,
                    section.OriginalFirstPointTangent.z + (prevSection.OriginalLastPointTangent.z - section.OriginalFirstPointTangent.z)*.5f);
            }

            section.DistanceFromEndToOrigin = section.DistanceFromStartToOrigin + section[section.PointsCount - 1].DistanceToSectionStart;
        }


        //search cached data and returns point's position or tangent at given distance from curve's start
        // * for example (1000 sections with 100 points each, so 100 000 points) with 10000 Random searches ~7ms
        protected virtual void BinarySearchByDistance(float distance, out Vector3 position, out Vector3 tangent, bool calculatePosition, bool calculateTangent)
        {
            var pointsCount = Curve.PointsCount;
            if (pointsCount < 2 || cachedSectionInfos.Length==0)
            {
                position = Vector3.zero;
                tangent = Vector3.zero;
                if (pointsCount == 1 && calculatePosition) position = Curve[0].PositionWorld;
            }

            // field was not set in the constructor, so it was not calculated and can not be accessed. 
            // Example, use new BGCurveBaseMath(new BGCurveBaseMath.Params(GetComponent<BGCurve>(), BGCurveBaseMath.Fields.PositionAndTangent)) 
            // to calculate world's position and tangent
            if (calculateTangent && ((int) Field.Tangent & (int) config.Fields) == 0)
                throw new UnityException("Can not calculate tangent, cause it was not included in the 'fields' constructor parameter. " +
                                         "For example, use new BGCurveBaseMath(curve, new BGCurveBaseMath.Config(" +
                                         "BGCurveBaseMath.Fields.PositionAndTangent))" +
                                         "to calculate world's position and tangent");


            var targetSection = cachedSectionInfos[FindSectionIndexByDistance(distance)];

            //after we found a section, let's search within this section 
            targetSection.CalcByDistance(distance - targetSection.DistanceFromStartToOrigin, out position, out tangent, calculatePosition, calculateTangent);
        }

        //find section by distance
        protected int FindSectionIndexByDistance(float distance)
        {
            // ----- critical section start 
            int low = 0, mid = 0, high = cachedSectionInfos.Length, i = 0;
            while (low < high)
            {
                mid = (low + high) >> 1;
                var item = cachedSectionInfos[mid];

                //success
                if (distance >= item.DistanceFromStartToOrigin && distance <= item.DistanceFromEndToOrigin) break;

                if (distance < item.DistanceFromStartToOrigin) high = mid;
                else low = mid + 1;

                //just in case
                if (i++ > 100) throw new UnityException("Something wrong: more than 100 iterations inside BinarySearch");
            }
            // ----- critical section end

            return mid;
        }


        //convert ratio to distance
        protected float DistanceByRatio(float distanceRatio)
        {
            return GetDistance()*Mathf.Clamp01(distanceRatio);
        }

        //ensure distance is within proper range
        protected float ClampDistance(float distance)
        {
            return Mathf.Clamp(distance, 0, GetDistance());
        }

        public override string ToString()
        {
            return "Math for curve (" + Curve + "), sections=" + SectionsCount;
        }

        #endregion

        #region private methods

        private static SectionPointInfo EnsurePoint(SectionPointInfo[] points, int index)
        {
            return points[index] ?? (points[index] = new SectionPointInfo());
        }

        private static T[] Resize<T>(ref T[] array, int size)
        {
            if (array == null || array.Length != size) Array.Resize(ref array, size);
            return array;
        }

        private void CurveChanged(object sender, BGCurveChangedArgs e)
        {
            Recalculate();
        }

        private void ConfigOnUpdate(object sender, EventArgs eventArgs)
        {
            Recalculate(true);
        }


        #endregion

        #region Helper model classes

        /// <summary>params for calculations </summary>
        public class Config
        {
            /// <summary>Which fields to calculate and cache. Do not use more fields then needed.</summary>
            public Fields Fields = Fields.Position;

            /// <summary>number of parts each curve's section will be devided to. Range[1, 1000]</summary>
            public int Parts = 30;

            /// <summary>Use points position instead of formula to calc tangents. This can increase performace.</summary>
            public bool UsePointPositionsToCalcTangents;

            /// <summary>Do not split straight lines during precalculation (use only 2 points per section).Tangents may suffer</summary>
            public bool OptimizeStraightLines;

            /// <summary>If not null it can control if math should update it's data. Updating cached data takes some resources, so this param can control updating strategy </summary>
            public Func<bool> ShouldUpdate;

            /// <summary>If not null math will update on this event </summary>
            public event EventHandler Update;


            public Config()
            {
            }

            /// <param name="fields">Fields (from BGCurveBaseMath.Field) you want to precalculate and cache. 
            /// 'None' means- no precalculation and caching will occur, but as a result you will be able to use CalcPositionByT and CalcTangentByT methods only.
            /// See class documentation for more info.</param>
            public Config(Fields fields)
            {
                Fields = fields;
            }

            protected bool Equals(Config other)
            {
                return Fields == other.Fields && Parts == other.Parts && UsePointPositionsToCalcTangents == other.UsePointPositionsToCalcTangents &&
                       OptimizeStraightLines == other.OptimizeStraightLines;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((Config) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = (int) Fields;
                    hashCode = (hashCode*397) ^ Parts;
                    hashCode = (hashCode*397) ^ UsePointPositionsToCalcTangents.GetHashCode();
                    hashCode = (hashCode*397) ^ OptimizeStraightLines.GetHashCode();
                    return hashCode;
                }
            }

            public void FireUpdate()
            {
                if (Update!=null) Update(this, null);
            }
        }

        /// <summary>information for one single section (between 2 points) of the curve</summary>
        public class SectionInfo
        {
            //distance from section start to curve start  (real one, not "local")
            public float DistanceFromStartToOrigin;

            //distance from section end to curve start  (real one, not "local")
            public float DistanceFromEndToOrigin;

            //all the points in this section
            protected internal SectionPointInfo[] points = new SectionPointInfo[0];


            //save the data it used to calculate points 
            public Vector3 OriginalFrom;
            public Vector3 OriginalTo;
            public BGCurvePoint.ControlTypeEnum OriginalFromControlType;
            public BGCurvePoint.ControlTypeEnum OriginalToControlType;
            public Vector3 OriginalFromControl;
            public Vector3 OriginalToControl;

            //we need it cause sections can get skipped while calculation
            public Vector3 OriginalFirstPointTangent;
            public Vector3 OriginalLastPointTangent;

            public SectionPointInfo[] Points
            {
                get { return points; }
            }

            public int PointsCount
            {
                get { return points == null ? 0 : points.Length; }
            }

            public float Distance
            {
                get { return DistanceFromEndToOrigin - DistanceFromStartToOrigin; }
            }

            public override string ToString()
            {
                return "Section distance=(" + Distance + ")";
            }

            public SectionPointInfo this[int i]
            {
                get { return points[i]; }
                set { points[i] = value; }
            }

            protected internal bool Reset(BGCurvePoint fromPoint, BGCurvePoint toPoint, int pointsCount, bool skipCheck)
            {
                var newFrom = fromPoint.PositionWorld;
                var newTo = toPoint.PositionWorld;
                var newFromControl = fromPoint.ControlSecondWorld;
                var newToControl = toPoint.ControlFirstWorld;

                const float epsilon = 0.000001f;
                if (
                    !skipCheck
                    && points.Length == pointsCount
                    && OriginalFromControlType == fromPoint.ControlType
                    && OriginalToControlType == toPoint.ControlType
                    && Vector3.SqrMagnitude(new Vector3(OriginalFrom.x - newFrom.x, OriginalFrom.y - newFrom.y, OriginalFrom.z - newFrom.z)) < epsilon
                    && Vector3.SqrMagnitude(new Vector3(OriginalTo.x - newTo.x, OriginalTo.y - newTo.y, OriginalTo.z - newTo.z)) < epsilon
                    && Vector3.SqrMagnitude(new Vector3(OriginalFromControl.x - newFromControl.x, OriginalFromControl.y - newFromControl.y, OriginalFromControl.z - newFromControl.z)) < epsilon
                    && Vector3.SqrMagnitude(new Vector3(OriginalToControl.x - newToControl.x, OriginalToControl.y - newToControl.y, OriginalToControl.z - newToControl.z)) < epsilon
                    )
                    return false;

                OriginalFrom = newFrom;
                OriginalTo = newTo;
                OriginalFromControlType = fromPoint.ControlType;
                OriginalToControlType = toPoint.ControlType;
                OriginalFromControl = newFromControl;
                OriginalToControl = newToControl;
                return true;
            }

            //copy pasted binary search algorithm
            public int FindPointIndexByDistance(float distanceWithinSection)
            {
                var pointsCountMinusOne = points.Length - 1;

                // ----- critical section start (copy paste)
                int low = 0, mid = 0, high = points.Length, i = 0;
                while (low < high)
                {
                    mid = (low + high) >> 1;
                    var item = points[mid];

                    //success
                    if (!(distanceWithinSection < item.DistanceToSectionStart) && (mid == pointsCountMinusOne || points[mid + 1].DistanceToSectionStart >= distanceWithinSection)) break;

                    if (distanceWithinSection < item.DistanceToSectionStart) high = mid;
                    else low = mid + 1;

                    //just in case
                    if (i++ > 100) throw new UnityException("Something wrong: more than 100 iterations inside BinarySearch");
                }
                // ----- critical section end
                return mid;
            }

            public void CalcByDistance(float distanceWithinSection, out Vector3 position, out Vector3 tangent, bool calculatePosition, bool calculateTangent)
            {
                position = Vector3.zero;
                tangent = Vector3.zero;

                if (points.Length == 2)
                {
                    //linear
                    var first = points[0];

                    if (Math.Abs(Distance) < BGCurve.Epsilon)
                    {
                        if (calculatePosition) position = first.Position;
                        if (calculateTangent) tangent = first.Tangent;
                    }
                    else
                    {
                        var ratio = distanceWithinSection/Distance;
                        var second = points[1];
                        if (calculatePosition) position = Vector3.Lerp(first.Position, second.Position, ratio);
                        if (calculateTangent) tangent = Vector3.Lerp(first.Tangent, second.Tangent, ratio);
                    }
                }
                else
                {
                    var pointIndex = FindPointIndexByDistance(distanceWithinSection);
                    var targetPoint = points[pointIndex];

                    //the very last point within section
                    if (pointIndex == points.Length - 1)
                    {
                        if (calculatePosition) position = targetPoint.Position;
                        if (calculateTangent) tangent = targetPoint.Tangent;
                    }
                    else
                    {
                        var nextPoint = points[pointIndex + 1];

                        //this is the distance between 2 points
                        var distanceBetweenTwoPoints = (nextPoint.DistanceToSectionStart - targetPoint.DistanceToSectionStart);
                        //this is smaller distance between 1st point and target distance. distanceWithinSection= the distance from section start to target distance
                        var distanceWithinTwoPoints = (distanceWithinSection - targetPoint.DistanceToSectionStart);
                        //zero division check
                        var ratio = Math.Abs(distanceBetweenTwoPoints) < BGCurve.Epsilon ? 0 : distanceWithinTwoPoints/distanceBetweenTwoPoints;

                        //lerp target's field between 1st and 2nd points using a ratio, which is based on the distance between them 
                        if (calculatePosition) position = Vector3.Lerp(targetPoint.Position, nextPoint.Position, ratio);
                        if (calculateTangent) tangent = Vector3.Lerp(targetPoint.Tangent, nextPoint.Tangent, ratio);
                    }
                }
            }
        }

        /// <summary>information for one point within a section (BGCurveBaseMath.Section)</summary>
        public class SectionPointInfo
        {
            //point's world position
            public Vector3 Position;

            //distance from the start of the section (real one, not "local")
            public float DistanceToSectionStart;

            //point's world tangent
            public Vector3 Tangent;

            internal Vector3 GetField(Field field)
            {
                Vector3 result;
                switch (field)
                {
                    case Field.Position:
                        result = Position;
                        break;
                    case Field.Tangent:
                        result = Tangent;
                        break;
                    default:
                        throw new UnityException("Unknown field=" + field);
                }
                return result;
            }

            internal Vector3 LerpTo(Field field, SectionPointInfo to, float ratio)
            {
                return Vector3.Lerp(GetField(field), to.GetField(field), ratio);
            }


            public override string ToString()
            {
                return "Point at (" + Position + ")";
            }
        }

        #endregion
    }

    #region Helper extensions for Field and Fields enums

    //helper extension class for Field enum 
    public static class FieldExtensions
    {
        /// <summary> if given fieldEnum is contained in the mask </summary>
        public static bool In(this BGCurveBaseMath.Field field, int mask)
        {
            return (field.Val() & mask) != 0;
        }

        /// <summary> cast to int </summary>
        public static int Val(this BGCurveBaseMath.Field field)
        {
            return (int) field;
        }
    }

    public static class FieldsExtensions
    {
        /// <summary> cast to int </summary>
        public static int Val(this BGCurveBaseMath.Fields fields)
        {
            return (int) fields;
        }
    }

    #endregion
}