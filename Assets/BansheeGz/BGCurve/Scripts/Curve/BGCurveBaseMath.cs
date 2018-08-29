using System;
using System.Collections.Generic;
using UnityEngine;

namespace BansheeGz.BGSpline.Curve
{
    /// <summary>  
    /// Basic math operations for curves (Distance, Position, Tangent, Closest point ). 
    /// It caches some data for quick access and recalculate it if curve changes. 
    /// It uses uniform split with forward differencing algorithm, which is the fastest available (for uniform split).
    /// </summary>
    public class BGCurveBaseMath : BGCurveMathI, IDisposable
    {
        #region events

        //===============================================================================================
        //                                                    Events 
        //===============================================================================================

        /// <summary> Change was requested by changed curve's data </summary>
        public event EventHandler ChangeRequested;

        /// <summary> Math was changed</summary>
        public event EventHandler Changed;

        #endregion

        #region fields

        //===============================================================================================
        //                                                    Fields 
        //===============================================================================================

        /// <summary> all possible attributes (fields) to calculate</summary>
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

        //curve
        protected readonly BGCurve curve;
        //what and how to calc
        protected Config config;

        //cached data for length calculations
        protected readonly List<SectionInfo> cachedSectionInfos = new List<SectionInfo>();

        // pools for sections
        protected readonly List<SectionInfo> poolSectionInfos = new List<SectionInfo>();
        // pools for points
        protected readonly List<SectionPointInfo> poolPointInfos = new List<SectionPointInfo>();

        //curve's length
        protected float cachedLength;

        //should we calculate position? (This is probably always true)
        protected bool cachePosition;
        //should we calculate tangent?
        protected bool cacheTangent;

        //closest point calculator
        protected BGCurveCalculatorClosestPoint closestPointCalculator;

        // frame when data was recalculated
        private int recalculatedAtFrame = -1;
        // frame when math was created
        private int createdAtFrame;

        // force section recalculation, even if it's not changed
        protected bool ignoreSectionChangedCheck;

        //some coefficients (these are used by approximation algorithm)
        private double h;
        private double h2;
        private double h3;

        /// <summary>Suppress all warnings, printed to the log </summary>
        public bool SuppressWarning { get; set; }

        /// <summary>get Curve</summary>
        public BGCurve Curve
        {
            get { return curve; }
        }

        /// <summary>Get calculated sections data</summary>
        public List<SectionInfo> SectionInfos
        {
            get { return cachedSectionInfos; }
        }

        /// <summary>Access calculated section's data by index</summary>
        public SectionInfo this[int i]
        {
            get { return cachedSectionInfos[i]; }
        }

        /// <summary>How much sections in total</summary>
        public int SectionsCount
        {
            get { return cachedSectionInfos.Count; }
        }

        /// <summary>Get last used config</summary>
        public Config Configuration
        {
            get { return config; }
        }

        // if tangent is calculated and formula is needed
        protected bool NeedTangentFormula
        {
            get { return !config.UsePointPositionsToCalcTangents && cacheTangent; }
        }

        /// <summary> How much approximation points are used in total </summary>
        public int PointsCount
        {
            get
            {
                if (SectionsCount == 0) return 0;
                var count = 0;
                var length = cachedSectionInfos.Count;
                for (var i = 0; i < length; i++) count += cachedSectionInfos[i].PointsCount;
                return count;
            }
        }

        #endregion

        #region constructors

        //===============================================================================================
        //                                                    Constructors 
        //===============================================================================================

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

        //===============================================================================================
        //                                                    Init 
        //===============================================================================================
        /// <summary>Init math with new config and recalculate it's data </summary>
        public virtual void Init(Config config)
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

            //assign new config
            this.config = config;
            config.Parts = Mathf.Clamp(config.Parts, 1, 1000);
            this.config.Update += ConfigOnUpdate;

            createdAtFrame = Time.frameCount;
            cachePosition = Field.Position.In(config.Fields.Val());
            cacheTangent = Field.Tangent.In(config.Fields.Val());

            //No fields- no need to calculate or trace anything.
            if (!cachePosition && !cacheTangent)
                throw new UnityException("No fields were chosen. Create math like this: new BGCurveBaseMath(curve, new BGCurveBaseMath.Config(BGCurveBaseMath.Fields.Position))");

            //some additional init steps for subclasses
            AfterInit(config);

            //calculate data, based on new config
            Recalculate(true);
        }

        //some additional initialization for subclasses
        protected virtual void AfterInit(Config config)
        {
        }

        #endregion

        #region Public methods

        //===============================================================================================
        //                                                    Public methods 
        //===============================================================================================

        //=========================================== Calculate by formula

        /// <summary> Calculate point world position between 2 points by a formula. This is a slow method</summary>
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

        /// <summary> Calculate a tangent between 2 points by a formula. This is a slow method</summary>
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
        //using useLocal is significantly slower. See interface for more comments
        public virtual Vector3 CalcByDistanceRatio(float distanceRatio, out Vector3 tangent, bool useLocal = false)
        {
            return CalcByDistance(cachedLength*distanceRatio, out tangent, useLocal);
        }

        //using useLocal is significantly slower. See interface for more comments
        public virtual Vector3 CalcByDistance(float distance, out Vector3 tangent, bool useLocal = false)
        {
            Vector3 position;
            BinarySearchByDistance(distance, out position, out tangent, true, true);

            if (useLocal)
            {
                position = curve.transform.InverseTransformPoint(position);
                tangent = curve.transform.InverseTransformDirection(tangent);
            }

            return position;
        }

        //using useLocal is significantly slower. See interface for more comments
        public virtual Vector3 CalcByDistanceRatio(Field field, float distanceRatio, bool useLocal = false)
        {
            return CalcByDistance(field, cachedLength*distanceRatio, useLocal);
        }

        //using useLocal is significantly slower. See interface for more comments
        public virtual Vector3 CalcByDistance(Field field, float distance, bool useLocal = false)
        {
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
        //using useLocal is significantly slower. See interface for more comments
        public virtual Vector3 CalcPositionAndTangentByDistanceRatio(float distanceRatio, out Vector3 tangent, bool useLocal = false)
        {
            return CalcByDistanceRatio(distanceRatio, out tangent, useLocal);
        }

        //using useLocal is significantly slower. See interface for more comments
        public virtual Vector3 CalcPositionAndTangentByDistance(float distance, out Vector3 tangent, bool useLocal = false)
        {
            return CalcByDistance(distance, out tangent, useLocal);
        }

        //=========================================== Position 
        //using useLocal is significantly slower. See interface for more comments
        public virtual Vector3 CalcPositionByDistanceRatio(float distanceRatio, bool useLocal = false)
        {
            return CalcByDistanceRatio(Field.Position, distanceRatio, useLocal);
        }

        //using useLocal is significantly slower. See interface for more comments
        public virtual Vector3 CalcPositionByDistance(float distance, bool useLocal = false)
        {
            return CalcByDistance(Field.Position, distance, useLocal);
        }

        //=========================================== Tangent
        //using useLocal is significantly slower. See interface for more comments
        public virtual Vector3 CalcTangentByDistanceRatio(float distanceRatio, bool useLocal = false)
        {
            return CalcByDistanceRatio(Field.Tangent, distanceRatio, useLocal);
        }

        //using useLocal is significantly slower. See interface for more comments
        public virtual Vector3 CalcTangentByDistance(float distance, bool useLocal = false)
        {
            return CalcByDistance(Field.Tangent, distance, useLocal);
        }


        //=========================================== Find Secton's index

        //see interface for comments
        public int CalcSectionIndexByDistance(float distance)
        {
            return FindSectionIndexByDistance(ClampDistance(distance));
        }

        //see interface for comments
        public int CalcSectionIndexByDistanceRatio(float ratio)
        {
            return FindSectionIndexByDistance(DistanceByRatio(ratio));
        }

        //=========================================== Closest Point

        //see interface for comments
        public Vector3 CalcPositionByClosestPoint(Vector3 point, bool skipSectionsOptimization = false, bool skipPointsOptimization = false)
        {
            if (closestPointCalculator == null) closestPointCalculator = new BGCurveCalculatorClosestPoint(this);
            float distance;
            Vector3 tangent;
            return closestPointCalculator.CalcPositionByClosestPoint(point, out distance, out tangent, skipSectionsOptimization, skipPointsOptimization);
        }

        //see interface for comments
        public Vector3 CalcPositionByClosestPoint(Vector3 point, out float distance, bool skipSectionsOptimization = false, bool skipPointsOptimization = false)
        {
            if (closestPointCalculator == null) closestPointCalculator = new BGCurveCalculatorClosestPoint(this);
            Vector3 tangent;
            return closestPointCalculator.CalcPositionByClosestPoint(point, out distance, out tangent, skipSectionsOptimization, skipPointsOptimization);
        }

        //see interface for comments
        public Vector3 CalcPositionByClosestPoint(Vector3 point, out float distance, out Vector3 tangent, bool skipSectionsOptimization = false, bool skipPointsOptimization = false)
        {
            if (closestPointCalculator == null) closestPointCalculator = new BGCurveCalculatorClosestPoint(this);
            return closestPointCalculator.CalcPositionByClosestPoint(point, out distance, out tangent, skipSectionsOptimization, skipPointsOptimization);
        }


        //=========================================== Total Distance
        //see interface for comments
        public virtual float GetDistance(int pointIndex = -1)
        {
            if (pointIndex < 0) return cachedLength;
            if (pointIndex == 0) return 0;

            return cachedSectionInfos[pointIndex - 1].DistanceFromEndToOrigin;
        }

        //=========================================== Curve's point world coordinates (faster then using point.positionWord etc.)

        /// <summary> Get point's world position </summary>
        public Vector3 GetPosition(int pointIndex)
        {
            var count = cachedSectionInfos.Count;

            if (count == 0 || count <= pointIndex) return curve[pointIndex].PositionWorld;

            return pointIndex < count ? cachedSectionInfos[pointIndex].OriginalFrom : cachedSectionInfos[pointIndex - 1].OriginalTo;
        }

        /// <summary> Get point's world first control position </summary>
        public Vector3 GetControlFirst(int pointIndex)
        {
            var count = cachedSectionInfos.Count;

            if (count == 0) return curve[pointIndex].ControlFirstWorld;

            if (pointIndex == 0) return curve.Closed ? cachedSectionInfos[count - 1].OriginalToControl : curve[0].ControlFirstWorld;

            return cachedSectionInfos[pointIndex - 1].OriginalToControl;
        }

        /// <summary> Get point's world second control position </summary>
        public Vector3 GetControlSecond(int pointIndex)
        {
            var count = cachedSectionInfos.Count;

            if (count == 0) return curve[pointIndex].ControlSecondWorld;

            if (pointIndex == count) return curve.Closed ? cachedSectionInfos[count - 1].OriginalFromControl : curve[pointIndex].ControlSecondWorld;

            return cachedSectionInfos[pointIndex].OriginalFromControl;
        }


        //=========================================== Misc
        /// <summary>Is given field is calculated?</summary>
        /// <returns>true if field is calculated</returns>
        public virtual bool IsCalculated(Field field)
        {
            return ((int) field & (int) config.Fields) != 0;
        }

        // C# standard dispose
        public virtual void Dispose()
        {
            curve.Changed -= CurveChanged;
            config.Update -= ConfigOnUpdate;
            cachedSectionInfos.Clear();
            poolSectionInfos.Clear();
        }

        /// <summary>returns BoundingBox for a section </summary>
        public Bounds GetBoundingBox(int sectionIndex, SectionInfo section)
        {
            var fromAbsent = section.OriginalFromControlType == BGCurvePoint.ControlTypeEnum.Absent;
            var toAbsent = section.OriginalToControlType == BGCurvePoint.ControlTypeEnum.Absent;

            var originalFrom = section.OriginalFrom;
            var originalTo = section.OriginalTo;
            var originalToControl = section.OriginalToControl;

            var fromToMinX = originalFrom.x > originalTo.x ? originalTo.x : originalFrom.x;
            var fromToMinY = originalFrom.y > originalTo.y ? originalTo.y : originalFrom.y;
            var fromToMinZ = originalFrom.z > originalTo.z ? originalTo.z : originalFrom.z;

            var fromToMaxX = originalFrom.x < originalTo.x ? originalTo.x : originalFrom.x;
            var fromToMaxY = originalFrom.y < originalTo.y ? originalTo.y : originalFrom.y;
            var fromToMaxZ = originalFrom.z < originalTo.z ? originalTo.z : originalFrom.z;

            float minX, minY, minZ, maxX, maxY, maxZ;
            if (fromAbsent)
            {
                if (toAbsent)
                {
                    //No controls
                    minX = fromToMinX;
                    minY = fromToMinY;
                    minZ = fromToMinZ;
                    maxX = fromToMaxX;
                    maxY = fromToMaxY;
                    maxZ = fromToMaxZ;
                }
                else
                {
                    //To Control Present
                    minX = fromToMinX > originalToControl.x ? originalToControl.x : fromToMinX;
                    minY = fromToMinY > originalToControl.y ? originalToControl.y : fromToMinY;
                    minZ = fromToMinZ > originalToControl.z ? originalToControl.z : fromToMinZ;

                    maxX = fromToMaxX < originalToControl.x ? originalToControl.x : fromToMaxX;
                    maxY = fromToMaxY < originalToControl.y ? originalToControl.y : fromToMaxY;
                    maxZ = fromToMaxZ < originalToControl.z ? originalToControl.z : fromToMaxZ;
                }
            }
            else
            {
                var originalFromControl = section.OriginalFromControl;
                if (toAbsent)
                {
                    //From Control Present
                    minX = fromToMinX > originalFromControl.x ? originalFromControl.x : fromToMinX;
                    minY = fromToMinY > originalFromControl.y ? originalFromControl.y : fromToMinY;
                    minZ = fromToMinZ > originalFromControl.z ? originalFromControl.z : fromToMinZ;

                    maxX = fromToMaxX < originalFromControl.x ? originalFromControl.x : fromToMaxX;
                    maxY = fromToMaxY < originalFromControl.y ? originalFromControl.y : fromToMaxY;
                    maxZ = fromToMaxZ < originalFromControl.z ? originalFromControl.z : fromToMaxZ;
                }
                else
                {
                    //Both Controls
                    var fromToControlToMinX = fromToMinX > originalToControl.x ? originalToControl.x : fromToMinX;
                    var fromToControlToMinY = fromToMinY > originalToControl.y ? originalToControl.y : fromToMinY;
                    var fromToControlToMinZ = fromToMinZ > originalToControl.z ? originalToControl.z : fromToMinZ;

                    var fromToControlToMaxX = fromToMaxX < originalToControl.x ? originalToControl.x : fromToMaxX;
                    var fromToControlToMaxY = fromToMaxY < originalToControl.y ? originalToControl.y : fromToMaxY;
                    var fromToControlToMaxZ = fromToMaxZ < originalToControl.z ? originalToControl.z : fromToMaxZ;

                    minX = fromToControlToMinX > originalFromControl.x ? originalFromControl.x : fromToControlToMinX;
                    minY = fromToControlToMinY > originalFromControl.y ? originalFromControl.y : fromToControlToMinY;
                    minZ = fromToControlToMinZ > originalFromControl.z ? originalFromControl.z : fromToControlToMinZ;

                    maxX = fromToControlToMaxX < originalFromControl.x ? originalFromControl.x : fromToControlToMaxX;
                    maxY = fromToControlToMaxY < originalFromControl.y ? originalFromControl.y : fromToControlToMaxY;
                    maxZ = fromToControlToMaxZ < originalFromControl.z ? originalFromControl.z : fromToControlToMaxZ;
                }
            }

            var deltaX = maxX - minX;
            var deltaY = maxY - minY;
            var deltaZ = maxZ - minZ;
            var extents = new Vector3(deltaX * .5f, deltaY * .5f, deltaZ * .5f);

            var bounds = new Bounds
            {
                extents = extents,
                center = new Vector3(minX + extents.x, minY + extents.y, minZ + extents.z)
            };

            return bounds;
        }

        //=========================================== Calculate and cache required data

        /// <summary>Calculates and cache all required data for performance reason. It is an expensive operation. </summary>
        public virtual void Recalculate(bool force = false)
        {
            if (ChangeRequested != null) ChangeRequested(this, null);

            force = force || Curve.SnapType == BGCurve.SnapTypeEnum.Curve; 
            
            if (!force && config.ShouldUpdate != null && !config.ShouldUpdate()) return;

            var currentSectionsCount = cachedSectionInfos.Count;
            if (curve.PointsCount < 2)
            {
                cachedLength = 0;
                if (currentSectionsCount > 0) cachedSectionInfos.Clear();
                if (Changed != null) Changed(this, null);
                return;
            }

            //we should at least warn about non-optimal usage of Recalculate (with more than 1 update per frame)
            var frameCount = Time.frameCount;
            if (recalculatedAtFrame == frameCount && frameCount != createdAtFrame) 
                Warning("We noticed you are updating math more than once per frame. This is not optimal. " +
                        "If you use curve.ImmediateChangeEvents by some reason, try to use curve.Transaction to wrap all the changes to one single event.");
            
            recalculatedAtFrame = frameCount;

            var pointsCount = curve.PointsCount;
            var sectionsCount = curve.Closed ? pointsCount : pointsCount - 1;

            //coefficients
            h = 1.0/config.Parts;
            h2 = h*h;
            h3 = h2*h;

            //ensure all sections inited
            if (currentSectionsCount != sectionsCount)
            {
                if (currentSectionsCount < sectionsCount)
                {
                    //not enough sections
                    var toAddCount = sectionsCount - currentSectionsCount;

                    //try to get from the pool
                    var poolCount = poolSectionInfos.Count;
                    var toAddBeforePoolCount = toAddCount;
                    for (var i = poolCount - 1; i >= 0 && toAddCount > 0; i--, toAddCount--) cachedSectionInfos.Add(poolSectionInfos[i]);

                    //pool was used
                    var usedPoolCount = toAddBeforePoolCount - toAddCount;
                    if (usedPoolCount != 0) poolSectionInfos.RemoveRange(poolSectionInfos.Count - usedPoolCount, usedPoolCount);

                    // we need to create new sections (pool is empty)
                    if (toAddCount > 0) for (var i = 0; i < toAddCount; i++) cachedSectionInfos.Add(new SectionInfo());
                }
                else
                {
                    //too many sections
                    var toRemoveCount = currentSectionsCount - sectionsCount;
                    for (var i = sectionsCount; i < currentSectionsCount; i++) poolSectionInfos.Add(cachedSectionInfos[i]);
                    cachedSectionInfos.RemoveRange(currentSectionsCount - toRemoveCount, toRemoveCount);
                }
            }

            //calculate fields for each section
            for (var i = 0; i < pointsCount - 1; i++) CalculateSection(i, cachedSectionInfos[i], i == 0 ? null : cachedSectionInfos[i - 1], curve[i], curve[i + 1]);

            var lastSection = cachedSectionInfos[sectionsCount - 1];
            if (curve.Closed)
            {
                CalculateSection(sectionsCount - 1, lastSection, cachedSectionInfos[sectionsCount - 2], curve[pointsCount - 1], curve[0]);
                if (cacheTangent) AdjustBoundaryPointsTangents(cachedSectionInfos[0], lastSection);
            }

            cachedLength = lastSection.DistanceFromEndToOrigin;

            if (Changed != null) Changed(this, null);
        }

        #endregion

        #region protected methods

        //print a message to console if condition is met and calls callback method
        protected virtual void Warning(string message, bool condition = true, Action callback = null)
        {
            if (!condition || !Application.isPlaying) return;

            if (!SuppressWarning) Debug.Log("BGCurve[BGCurveBaseMath] Warning! " + message + ". You can suppress all warnings by using BGCurveBaseMath.SuppressWarning=true;");

            if (callback != null) callback();
        }


        //calculates one single section data
        // Performance:
        // * for example: ~100 000 section points (1000 curve's points with 100 parts each) with both controls
        // ~7.8 ms for Position
        // ~13.5 ms for PositionAndTangent
        protected virtual void CalculateSection(int index, SectionInfo section, SectionInfo prevSection, BGCurvePointI @from, BGCurvePointI to)
        {
            if (section == null) section = new SectionInfo();

            section.DistanceFromStartToOrigin = prevSection == null ? 0 : prevSection.DistanceFromEndToOrigin;

            var straightAndOptimized = config.OptimizeStraightLines && @from.ControlType == BGCurvePoint.ControlTypeEnum.Absent && to.ControlType == BGCurvePoint.ControlTypeEnum.Absent;
            var pointsCount = straightAndOptimized ? 2 : config.Parts + 1;

            // do we need to recalc points?
            if (Reset(section, @from, to, pointsCount) || Curve.SnapType == BGCurve.SnapTypeEnum.Curve)
            {
                if (straightAndOptimized)
                {
                    // =====================================================  Straight section with 2 points
                    Resize(section.points, 2);

                    var startPoint = section.points[0];
                    var endPoint = section.points[1];

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
                    CalculateSplitSection(section, @from, to);
                }
                if (cacheTangent)
                {
                    section.OriginalFirstPointTangent = section[0].Tangent;
                    section.OriginalLastPointTangent = section[section.PointsCount - 1].Tangent;
                }
            }

            // we should adjust tangents for previous section's last point and first point of the current section
            if (cacheTangent && prevSection != null) AdjustBoundaryPointsTangents(section, prevSection);

            section.DistanceFromEndToOrigin = section.DistanceFromStartToOrigin + section[section.PointsCount - 1].DistanceToSectionStart;
        }

        //adjust neighbour adjacent points tangents 
        private void AdjustBoundaryPointsTangents(SectionInfo section, SectionInfo prevSection)
        {
            if (IsUseDistanceToAdjustTangents(section, prevSection))
            {
                var distance1 = Vector3.SqrMagnitude(section[0].Position - section[1].Position);
                var distance2 = Vector3.SqrMagnitude(prevSection[prevSection.PointsCount - 1].Position - prevSection[prevSection.PointsCount - 2].Position);
                var overallDistance = distance1 + distance2;

                if (Math.Abs(overallDistance) < BGCurve.Epsilon) return;

                var ratio = distance1/overallDistance;
                var reverseRatio = 1 - ratio;

                section[0].Tangent = prevSection[prevSection.PointsCount - 1].Tangent = Vector3.Normalize(new Vector3(
                    section.OriginalFirstPointTangent.x*ratio + prevSection.OriginalLastPointTangent.x*reverseRatio,
                    section.OriginalFirstPointTangent.y*ratio + prevSection.OriginalLastPointTangent.y*reverseRatio,
                    section.OriginalFirstPointTangent.z*ratio + prevSection.OriginalLastPointTangent.z*reverseRatio));
            }
            else
            {
                //both tangents get adjusted 
                section[0].Tangent = prevSection[prevSection.PointsCount - 1].Tangent = Vector3.Normalize(new Vector3(
                    section.OriginalFirstPointTangent.x + prevSection.OriginalLastPointTangent.x,
                    section.OriginalFirstPointTangent.y + prevSection.OriginalLastPointTangent.y,
                    section.OriginalFirstPointTangent.z + prevSection.OriginalLastPointTangent.z));
            }
            }

        //should we use distance between approximation points for adjusting boundary tangents
        protected virtual bool IsUseDistanceToAdjustTangents(SectionInfo section, SectionInfo prevSection)
        {
            return config.OptimizeStraightLines && section.OriginalFromControlType == BGCurvePoint.ControlTypeEnum.Absent &&
                   (section.OriginalToControlType == BGCurvePoint.ControlTypeEnum.Absent || prevSection.OriginalFromControlType == BGCurvePoint.ControlTypeEnum.Absent);
        }

        //reset section's data and returns if recalculation is needed
        protected virtual bool Reset(SectionInfo section, BGCurvePointI @from, BGCurvePointI to, int pointsCount)
        {
            return section.Reset(@from, to, pointsCount, ignoreSectionChangedCheck);
        }


        // this method contains some intentional 1) copy/paste 2) methods inlining 3) operators inlining- to increase performance 

        /**
         * Calculates one split section's data using forward differencing algorithm
         * 
         * The technique of forward differencing allows us to compute the polynomial at t=0,
         * and then incrementally calculate points by adding (and updating) the differences.
         * 
         *  Thanks to Russel Lindsay (https://gist.github.com/rlindsay/c55be560ec41144f521f)
         * 
         * Formulas for difference between t and t+h for cubic, quadratic, and linear polynomials
         *      (notice that the difference between t and t+h for a cubic polynomial is quadratic ((3ak)t^2 + (3ak^2 + 2bk)t + ak^3 + bk^2 + ck),
         *      likewise the difference in the quadratic case is linear, and in the linear case is a constant)
         *  
         * --------------------
         * Cubic polynomial (4 points, a, b, c, d)
         * C(t) = a*t^3 + b*t^2 + c*t + d
         * 
         * Cubic polynomial difference
         * C(t + h) - C(t)      http://www.wolframalpha.com/input/?i=%28a*%28t+%2B+h%29%5E3+%2B+b*%28t+%2B+h%29%5E2+%2B+c*%28t+%2B+h%29+%2B+d%29+-+%28a*t%5E3+%2B+b*t%5E2+%2B+c*t+%2B+d%29+
         * = ah^3 + 3ah^2 t + 3aht^2 + bh^2 + 2bht + ch     
    ---> i)   = (3ah)t^2 + (3ah^2 + 2bh)t + ah^3 + bh^2 + ch     
         * 
         * --------------------
         * quadratic polynomial
         * Q(t) = at^2 + bt + c
         * 
         * quadratic polynomial difference
         * Q(t + h) - Q(t)      http://www.wolframalpha.com/input/?i=%28a%28t%2Bh%29%5E2+%2B+b%28t%2Bh%29+%2B+c%29+-+%28at%5E2+%2B+bt+%2B+c%29
    ---> ii)   = (2ah)t + ah^2 + bh
         * 
         * ------------------
         * linear polynomial
         * L(t) = at + b
         * 
         * linear polynomial difference
         * L(t + h) - L(t)     http://www.wolframalpha.com/input/?i=%28a%28t+%2B+h%29+%2B+b%29+-+%28at+%2B+b%29
    ---> iii)   = ah
         */

        protected virtual void CalculateSplitSection(SectionInfo section, BGCurvePointI @from, BGCurvePointI to)
        {
            var parts = config.Parts;
            Resize(section.points, parts + 1);

            var points = section.points;

            // all section's values
            var fromPos = section.OriginalFrom;
            var toPos = section.OriginalTo;
            var control1 = section.OriginalFromControl;
            var control2 = section.OriginalToControl;

            var controlFromAbsent = @from.ControlType == BGCurvePoint.ControlTypeEnum.Absent;
            var controlToAbsent = to.ControlType == BGCurvePoint.ControlTypeEnum.Absent;
            var noControls = controlFromAbsent && controlToAbsent;
            var bothControls = !controlFromAbsent && !controlToAbsent;
            if (!noControls && !bothControls && controlFromAbsent) control1 = control2;

            //snapping
            var snapIsOn = curve.SnapType == BGCurve.SnapTypeEnum.Curve;

            //assign first last points directly to avoid accumulation errors
            var firstPoint = points[0] ?? (section.points[0] = new SectionPointInfo());
            firstPoint.Position = fromPos;
            var lastPoint = points[parts] ?? (section.points[parts] = new SectionPointInfo());
            lastPoint.Position = toPos;


            if (noControls)
            {
                // ========================================================  NoControls

                var currentX = (double) fromPos.x;
                var currentY = (double) fromPos.y;
                var currentZ = (double) fromPos.z;

                var stepX = ((double) toPos.x - fromPos.x)/parts;
                var stepY = ((double) toPos.y - fromPos.y)/parts;
                var stepZ = ((double) toPos.z - fromPos.z)/parts;

                var fromToTangentWorld = Vector3.zero;
                if (cacheTangent) fromToTangentWorld = (toPos - fromPos).normalized;

                lastPoint.DistanceToSectionStart = Vector3.Distance(toPos, fromPos);
                var onePartDistance = lastPoint.DistanceToSectionStart/parts;

                //--------------------------------  Critical section
                for (var i = 1; i < parts; i++)
                {
                    var point = points[i];

                    currentX += stepX;
                    currentY += stepY;
                    currentZ += stepZ;

                    var pos = new Vector3((float) currentX, (float) currentY, (float) currentZ);

                    if (snapIsOn) curve.ApplySnapping(ref pos);

                    point.Position = pos;

                    //---------- tangents
                    if (cacheTangent)
                    {
                        if (config.UsePointPositionsToCalcTangents)
                        {
                            //-------- Calc by point's positions
                            var prevPoint = section[i - 1];
                            var prevPosition = prevPoint.Position;
                            var tangent = new Vector3(pos.x - prevPosition.x, pos.y - prevPosition.y, pos.z - prevPosition.z);
                            //normalized inlined
                            var marnitude = (float) Math.Sqrt((double) tangent.x*(double) tangent.x + (double) tangent.y*(double) tangent.y + (double) tangent.z*(double) tangent.z);
                            tangent = ((double) marnitude > 9.99999974737875E-06) ? new Vector3(tangent.x/marnitude, tangent.y/marnitude, tangent.z/marnitude) : Vector3.zero;

                            prevPoint.Tangent = point.Tangent = tangent;
                        }
                        else point.Tangent = fromToTangentWorld;
                    }

                    point.DistanceToSectionStart = onePartDistance*i;
                }
                //--------------------------------  Critical section ends

                //assign last point separately to get rid of accumulation errors
                if (cacheTangent) firstPoint.Tangent = lastPoint.Tangent = fromToTangentWorld;
            }
            else
            {
                //for tangents
                double tX = 0, tY = 0, tZ = 0, firstTDx = 0, firstTDy = 0, firstTDz = 0;

                if (bothControls)
                {
                    // ========================================================  Both Controls (Cubic Bezier)

                    /* coefficients for control points (it's a standard Bezier matrix)
                    * you can get these numbers by converting parametric equation to standard polinomial form
                    * 
                    * | 1  0  0  0|   |A|   |       A        |
                    * |-3  3  0  0| * |B| = |   -3A + 3B     |
                    * | 3 -6  3  0|   |C|   |  3A - 6B + 3C  |
                    * |-1  3 -3  1|   |D|   |-A + 3B - 3C + D|
                    *                     
                    * http://www.wolframalpha.com/input/?i=%7B%7B1,+0,+0,+0%7D,%7B-3,+3,+0,+0%7D,+%7B3,+-6,+3,+0%7D,+%7B-1,+3,+-3,+1%7D%7D*%7B%7Bp_0%7D,+%7Bp_1%7D,+%7Bp_2%7D,+%7Bp_3%7D%7D
                    * 
                    */
                    // the same as in above matrix (top down)
                    var cx = 3*((double) control1.x - fromPos.x);
                    var cy = 3*((double) control1.y - fromPos.y);
                    var cz = 3*((double) control1.z - fromPos.z);

                    var bx = 3*((double) control2.x - control1.x) - cx;
                    var by = 3*((double) control2.y - control1.y) - cy;
                    var bz = 3*((double) control2.z - control1.z) - cz;

                    var ax = (double) toPos.x - fromPos.x - cx - bx;
                    var ay = (double) toPos.y - fromPos.y - cy - by;
                    var az = (double) toPos.z - fromPos.z - cz - bz;

                    var pointX = (double) fromPos.x;
                    var pointY = (double) fromPos.y;
                    var pointZ = (double) fromPos.z;

                    var axH3 = ax*h3;
                    var ax6H3 = 6*axH3;
                    var ayH3 = ay*h3;
                    var ay6H3 = 6*ayH3;
                    var azH3 = az*h3;
                    var az6H3 = 6*azH3;

                    var bxH2 = bx*h2;
                    var byH2 = by*h2;
                    var bzH2 = bz*h2;

                    /* First Difference
                     * the difference between t and t+h. Since the curve is cubic, difference is (from Formula i, see header)
                     * i) (3ah)t^2 + (3ah^2 + 2bh)t + ah^3 + bh^2 + ch
                     * 
                     * Since we are calculating from the start of the curve, t = 0.                     
                     * 
                     * http://www.wolframalpha.com/input/?i=(a*(0+%2B+h)%5E3+%2B+b*(0+%2B+h)%5E2+%2B+c*(0+%2B+h)+%2B+d)+-+(a*0%5E3+%2B+b*0%5E2+%2B+c*0+%2B+d) 
                     * D1 = ah^3 + bh^2 + ch 
                     */
                    var firstDx = axH3 + bxH2 + cx*h;
                    var firstDy = ayH3 + byH2 + cy*h;
                    var firstDz = azH3 + bzH2 + cz*h;

                    /* 
                     * Second Difference:   
                     * 
                     * the difference between two successive first differences. Since the form of the first difference is quadratic, the difference
                     * between two of them is (from Formula ii, see header )
                     * ii) (2ah)t + ah^2 + bh
                     * 
                     * Substituting 
                     *      t = 0 to calculate the difference at the start of the curve,
                     *      a = 3ah, 
                     *      b = 3ah^2 + 2bh    (see D1 calculation, Formula i )
                     * we get
                     *  D2   = (6ah^2)t + 6ah^3 + 2bh^2                            http://www.wolframalpha.com/input/?i=%282*3*a*h*h%29t+%2B+3*a*h*h%5E2+%2B+%283*a*h%5E2+%2B+2*b*h%29h
                     * = 6ah^3 + 2bh^2
                     */
                    var secondDx = ax6H3 + 2*bxH2;
                    var secondDy = ay6H3 + 2*byH2;
                    var secondDz = az6H3 + 2*bzH2;

                    /*
                    * Third difference:
                    * 
                    * the difference between two successive second differences. since the form is linear
                    * iii) ah
                    * substituting
                    *      a = 6ah^2  (see D2 calculation)
                    *  D3   = 6ah^3                                               http://www.wolframalpha.com/input/?i=6*a*h%5E2+*+h
                    * 
                    */
                    var thirdDx = ax6H3;
                    var thirdDy = ay6H3;
                    var thirdDz = az6H3;


                    double secondTDx = 0, secondTDy = 0, secondTDz = 0;
                    if (cacheTangent && !config.UsePointPositionsToCalcTangents)
                    {
                        //the same thing as with positions
                        //parametric to polinomial standard
                        //3*(1-t)^2*(p1-p0) + 6*(1-t)*t*(p2-p1) + 3*t^2*(p3-p2)=(-3*p0+9*p1-9*p2+3*p3)*t^2 + (6*p0-12*p1+6*p2)*t + (3*p1-3*p0)
                        var tbx = 6*((double) fromPos.x - 2*control1.x + control2.x);
                        var tby = 6*((double) fromPos.y - 2*control1.y + control2.y);
                        var tbz = 6*((double) fromPos.z - 2*control1.z + control2.z);

                        var tax = 3*((double) -fromPos.x + 3*control1.x - 3*control2.x + toPos.x);
                        var tay = 3*((double) -fromPos.y + 3*control1.y - 3*control2.y + toPos.y);
                        var taz = 3*((double) -fromPos.z + 3*control1.z - 3*control2.z + toPos.z);

                        //temp
                        var taxH2 = tax*h2;
                        var tayH2 = tay*h2;
                        var tazH2 = taz*h2;


                        // ii)  (2ah)t + ah^2 + bh, t=0 
                        firstTDx = taxH2 + tbx*h;
                        firstTDy = tayH2 + tby*h;
                        firstTDz = tazH2 + tbz*h;

                        // iii) = ah, a=2ah
                        secondTDx = 2*taxH2;
                        secondTDy = 2*tayH2;
                        secondTDz = 2*tazH2;

                        tX = cx;
                        tY = cy;
                        tZ = cz;

                        //normalized inlined
                        var magnitude = Math.Sqrt(tX*tX + tY*tY + tZ*tZ);
                        firstPoint.Tangent = magnitude > 9.99999974737875E-06 ? new Vector3((float) (tX/magnitude), (float) (tY/magnitude), (float) (tZ/magnitude)) : Vector3.zero;
                    }

                    //--------------------------------  Critical section
                    for (var i = 1; i < parts; i++)
                    {
                        var point = points[i];

                        pointX += firstDx;
                        pointY += firstDy;
                        pointZ += firstDz;

                        firstDx += secondDx;
                        firstDy += secondDy;
                        firstDz += secondDz;

                        secondDx += thirdDx;
                        secondDy += thirdDy;
                        secondDz += thirdDz;

                        var pos = new Vector3((float) pointX, (float) pointY, (float) pointZ);

                        if (snapIsOn) curve.ApplySnapping(ref pos);

                        point.Position = pos;

                        if (cacheTangent)
                        {
                            if (config.UsePointPositionsToCalcTangents)
                            {
                                //-------- Calc by point's positions
                                var prevPoint = section[i - 1];
                                var prevPosition = prevPoint.Position;
                                var tangent = new Vector3(pos.x - prevPosition.x, pos.y - prevPosition.y, pos.z - prevPosition.z);
                                //normalized inlined
                                var marnitude = (float) Math.Sqrt((double) tangent.x*(double) tangent.x + (double) tangent.y*(double) tangent.y + (double) tangent.z*(double) tangent.z);
                                tangent = ((double) marnitude > 9.99999974737875E-06) ? new Vector3(tangent.x/marnitude, tangent.y/marnitude, tangent.z/marnitude) : Vector3.zero;

                                prevPoint.Tangent = point.Tangent = tangent;
                            }
                            else
                            {
                                tX += firstTDx;
                                tY += firstTDy;
                                tZ += firstTDz;

                                firstTDx += secondTDx;
                                firstTDy += secondTDy;
                                firstTDz += secondTDz;

                                //normalized inlined
                                var magnitude = Math.Sqrt(tX*tX + tY*tY + tZ*tZ);
                                point.Tangent = magnitude > 9.99999974737875E-06 ? new Vector3((float) (tX/magnitude), (float) (tY/magnitude), (float) (tZ/magnitude)) : Vector3.zero;
                            }
                        }

                        // ---------- distance to section start (Vector3.Distance inlined)
                        var prevPos = section[i - 1].Position;
                        double deltaX = pos.x - prevPos.x;
                        double deltaY = pos.y - prevPos.y;
                        double deltaZ = pos.z - prevPos.z;
                        point.DistanceToSectionStart = section[i - 1].DistanceToSectionStart + ((float) Math.Sqrt(deltaX*deltaX + deltaY*deltaY + deltaZ*deltaZ));
                    }
                    //--------------------------------  Critical section ends
                }
                else
                {
                    // ========================================================  One Control (Quadratic Bezier)
                    // see comments for cubic bezier for more details
                    // parametric to polinomial standard form
                    // (1-t)^2*p0 + 2(1-t)*t*p1+t^2*p2 = (p0-2p1+p2)*t^2 + (-2p0+2p1)*t + p0

                    var bx = 2*((double) control1.x - fromPos.x);
                    var by = 2*((double) control1.y - fromPos.y);
                    var bz = 2*((double) control1.z - fromPos.z);

                    var ax = (double) fromPos.x - 2*control1.x + toPos.x;
                    var ay = (double) fromPos.y - 2*control1.y + toPos.y;
                    var az = (double) fromPos.z - 2*control1.z + toPos.z;

                    // ii)   = (2ah)t + ah^2 + bh, t=0
                    var firstDx = ax*h2 + bx*h;
                    var firstDy = ay*h2 + by*h;
                    var firstDz = az*h2 + bz*h;

                    // iii) ah, a=2ah
                    var secondDx = 2*ax*h2;
                    var secondDy = 2*ay*h2;
                    var secondDz = 2*az*h2;

                    var pointX = (double) fromPos.x;
                    var pointY = (double) fromPos.y;
                    var pointZ = (double) fromPos.z;

                    if (cacheTangent && !config.UsePointPositionsToCalcTangents)
                    {
                        //the same thing as with positions
                        //parametric to polinomial standard
                        // 2*(1-t)*(p1-p0) + 2t*(p2-p1) = (2*p0-4*p1+2*p2)*t + (2*p1-2*p0)
                        var tax = 2*((double) fromPos.x - 2*control1.x + toPos.x);
                        var tay = 2*((double) fromPos.y - 2*control1.y + toPos.y);
                        var taz = 2*((double) fromPos.z - 2*control1.z + toPos.z);

                        // iii) = ah 
                        firstTDx = tax*h;
                        firstTDy = tay*h;
                        firstTDz = taz*h;

                        tX = 2*((double) control1.x - fromPos.x);
                        tY = 2*((double) control1.y - fromPos.y);
                        tZ = 2*((double) control1.z - fromPos.z);

                        //normalized inlined
                        var magnitude = Math.Sqrt(tX*tX + tY*tY + tZ*tZ);
                        firstPoint.Tangent = magnitude > 9.99999974737875E-06 ? new Vector3((float) (tX/magnitude), (float) (tY/magnitude), (float) (tZ/magnitude)) : Vector3.zero;
                    }
                    //--------------------------------  Critical section
                    for (var i = 1; i < parts; i++)
                    {
                        var point = points[i];

                        pointX += firstDx;
                        pointY += firstDy;
                        pointZ += firstDz;

                        firstDx += secondDx;
                        firstDy += secondDy;
                        firstDz += secondDz;

                        var pos = new Vector3((float) pointX, (float) pointY, (float) pointZ);

                        if (snapIsOn) curve.ApplySnapping(ref pos);

                        point.Position = pos;

                        if (cacheTangent)
                        {
                            if (config.UsePointPositionsToCalcTangents)
                            {
                                //-------- Calc by point's positions
                                var prevPoint = section[i - 1];
                                var prevPosition = prevPoint.Position;
                                var tangent = new Vector3(pos.x - prevPosition.x, pos.y - prevPosition.y, pos.z - prevPosition.z);
                                //normalized inlined
                                var marnitude = (float) Math.Sqrt((double) tangent.x*(double) tangent.x + (double) tangent.y*(double) tangent.y + (double) tangent.z*(double) tangent.z);
                                tangent = ((double) marnitude > 9.99999974737875E-06) ? new Vector3(tangent.x/marnitude, tangent.y/marnitude, tangent.z/marnitude) : Vector3.zero;

                                prevPoint.Tangent = point.Tangent = tangent;
                            }
                            else
                            {
                                tX += firstTDx;
                                tY += firstTDy;
                                tZ += firstTDz;

                                //normalized inlined
                                var magnitude = Math.Sqrt(tX*tX + tY*tY + tZ*tZ);
                                point.Tangent = magnitude > 9.99999974737875E-06 ? new Vector3((float) (tX/magnitude), (float) (tY/magnitude), (float) (tZ/magnitude)) : Vector3.zero;
                            }
                        }

                        // ---------- distance to section start (Vector3.Distance inlined)
                        var prevPos = section[i - 1].Position;
                        double deltaX = pos.x - prevPos.x;
                        double deltaY = pos.y - prevPos.y;
                        double deltaZ = pos.z - prevPos.z;
                        point.DistanceToSectionStart = section[i - 1].DistanceToSectionStart + ((float) Math.Sqrt(deltaX*deltaX + deltaY*deltaY + deltaZ*deltaZ));
                    }
                    //--------------------------------  Critical section ends
                }

                //last point's tangent
                if (cacheTangent && !config.UsePointPositionsToCalcTangents)
                {
                    tX += firstTDx;
                    tY += firstTDy;
                    tZ += firstTDz;
                    var magnitude = Math.Sqrt(tX*tX + tY*tY + tZ*tZ);
                    lastPoint.Tangent = magnitude > 9.99999974737875E-06 ? new Vector3((float) (tX/magnitude), (float) (tY/magnitude), (float) (tZ/magnitude)) : Vector3.zero;
                }
            }

            //last Point's distance
            var beforeLastPoint = section[parts - 1];
            var beforeLastPos = beforeLastPoint.Position;
            var lastPointPos = lastPoint.Position;
            double dX = lastPointPos.x - beforeLastPos.x;
            double dY = lastPointPos.y - beforeLastPos.y;
            double dZ = lastPointPos.z - beforeLastPos.z;
            lastPoint.DistanceToSectionStart = beforeLastPoint.DistanceToSectionStart + ((float) Math.Sqrt(dX*dX + dY*dY + dZ*dZ));

            //last point's tangent
            if (cacheTangent && config.UsePointPositionsToCalcTangents)
            {
                //-------- Calc by point's positions

                var tangent = new Vector3((float) dX, (float) dY, (float) dZ);
                //normalized inlined
                var marnitude = (float) Math.Sqrt((double) tangent.x*(double) tangent.x + (double) tangent.y*(double) tangent.y + (double) tangent.z*(double) tangent.z);
                tangent = ((double) marnitude > 9.99999974737875E-06) ? new Vector3(tangent.x/marnitude, tangent.y/marnitude, tangent.z/marnitude) : Vector3.zero;

                lastPoint.Tangent = tangent;
            }
        }


        //search cached data and returns point's position or tangent at given distance from curve's start
        // * for example (1000 sections with 100 points each, so 100 000 points) with 10000 Random searches ~7ms
        protected virtual void BinarySearchByDistance(float distance, out Vector3 position, out Vector3 tangent, bool calculatePosition, bool calculateTangent)
        {
            switch (curve.PointsCount)
            {
                case 0:
                    position = Vector3.zero;
                    tangent = Vector3.zero;
                    return;
                case 1:
                    position = curve[0].PositionWorld;
                    tangent = Vector3.zero;
                    return;
            }

            if (cachedSectionInfos.Count == 0)
            {
                position = Vector3.zero;
                tangent = Vector3.zero;
                return;
            }
            
            if (distance < 0f) distance = 0f;
            else if (distance > cachedLength) distance = cachedLength;


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
            int low = 0, mid = 0, high = cachedSectionInfos.Count, i = 0;
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
            return "Base Math for curve (" + Curve + "), sections=" + SectionsCount;
        }

        #endregion

        #region private methods

        //===============================================================================================
        //                                                    Private functions
        //===============================================================================================

        protected void Resize(List<SectionPointInfo> points, int size)
        {
            var pointCount = points.Count;
            if (pointCount == size) return;

            //size mismatch
            if (pointCount < size)
            {
                //we need more points
                var poolIndex = poolPointInfos.Count - 1;
                for (var i = pointCount; i < size; i++) points.Add(poolIndex >= 0 ? poolPointInfos[poolIndex--] : new SectionPointInfo());

                //pool was used
                if (poolIndex != poolPointInfos.Count - 1) poolPointInfos.RemoveRange(poolIndex + 1, poolPointInfos.Count - 1 - poolIndex);
            }
            else
            {
                //we need less points
                for (var i = size; i < pointCount; i++) poolPointInfos.Add(points[i]);

                points.RemoveRange(size, pointCount - size);
            }
        }


        private void CurveChanged(object sender, BGCurveChangedArgs e)
        {
            ignoreSectionChangedCheck = e != null && e.ChangeType == BGCurveChangedArgs.ChangeTypeEnum.Snap;
            Recalculate();
            ignoreSectionChangedCheck = false;
        }

        private void ConfigOnUpdate(object sender, EventArgs eventArgs)
        {
            Recalculate(true);
        }

        #endregion

        #region Helper model classes

        //===============================================================================================
        //                                                    Helper classes
        //===============================================================================================


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
                var hashCode = (int) Fields;
                hashCode = (hashCode*397) ^ Parts;
                hashCode = (hashCode*397) ^ UsePointPositionsToCalcTangents.GetHashCode();
                hashCode = (hashCode*397) ^ OptimizeStraightLines.GetHashCode();
                return hashCode;
            }

            /// <summary>Fire Updated event</summary>
            public void FireUpdate()
            {
                if (Update != null) Update(this, null);
            }
        }

        /// <summary>information for one single section (between 2 points) of the curve</summary>
        public class SectionInfo
        {
            /// <summary>distance from section start to curve start</summary>
            public float DistanceFromStartToOrigin;

            /// <summary>distance from section end to curve start </summary>
            public float DistanceFromEndToOrigin;

            //all the points in this section
            protected internal readonly List<SectionPointInfo> points = new List<SectionPointInfo>();


            /// <summary>From location, used by calculation</summary>
            public Vector3 OriginalFrom;

            /// <summary>To location, used by calculation</summary>
            public Vector3 OriginalTo;

            /// <summary>From control type, used by calculation</summary>
            public BGCurvePoint.ControlTypeEnum OriginalFromControlType;

            /// <summary>To control type, used by calculation</summary>
            public BGCurvePoint.ControlTypeEnum OriginalToControlType;

            /// <summary>From control position, used by calculation</summary>
            public Vector3 OriginalFromControl;

            /// <summary>To control position, used by calculation</summary>
            public Vector3 OriginalToControl;

            // we need these 2 following fields, cause this section can get skipped while calculation, but adjacent sections may change, 
            // and this will affect adjacent points tangents
            // so we need these "original tangents" to calculate final tangents in case this section will get skipped
            /// <summary>First point tangent, calculated for the first time</summary>
            public Vector3 OriginalFirstPointTangent;

            /// <summary>Last point tangent, calculated for the first time</summary>
            public Vector3 OriginalLastPointTangent;

            /// <summary>Approximation points in the section</summary>
            public List<SectionPointInfo> Points
            {
                get { return points; }
            }

            /// <summary>Approximation points count</summary>
            public int PointsCount
            {
                get { return points.Count; }
            }

            /// <summary>Section's total distance</summary>
            public float Distance
            {
                get { return DistanceFromEndToOrigin - DistanceFromStartToOrigin; }
            }

            public override string ToString()
            {
                return "Section distance=(" + Distance + ")";
            }

            /// <summary>Get section's point by index</summary>
            public SectionPointInfo this[int i]
            {
                get { return points[i]; }
                set { points[i] = value; }
            }

            /// <summary>Reset section's init data and returns true, if (re)calculation is needed</summary>
            protected internal bool Reset(BGCurvePointI fromPoint, BGCurvePointI toPoint, int pointsCount, bool skipCheck)
            {
                var newFrom = fromPoint.PositionWorld;
                var newTo = toPoint.PositionWorld;
                var newFromControl = fromPoint.ControlSecondWorld;
                var newToControl = toPoint.ControlFirstWorld;

                const float epsilon = 0.000001f;
                if (
                    !skipCheck
                    && points.Count == pointsCount
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
                var pointsCountMinusOne = points.Count - 1;

                // ----- critical section start (copy paste)
                int low = 0, mid = 0, high = points.Count, i = 0;
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

            /// <summary>Calculates posiiton and distance by distance within this section. Note, it does not apply any checking for performance's sake </summary>
            public void CalcByDistance(float distanceWithinSection, out Vector3 position, out Vector3 tangent, bool calculatePosition, bool calculateTangent)
            {
                position = Vector3.zero;
                tangent = Vector3.zero;

                if (points.Count == 2)
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
                    if (pointIndex == points.Count - 1)
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
            /// <summary>point's world position </summary>
           public Vector3 Position;


            /// <summary>distance from the start of the section to this point</summary>
            public float DistanceToSectionStart;

            /// <summary>point's world tangent</summary>
            public Vector3 Tangent;

            //get field's value (position or tangent)
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

            //lerp field's value (position or tangent) between two points by ratio
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

    //===============================================================================================
    //                                                    Extensions
    //===============================================================================================

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