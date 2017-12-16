using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BansheeGz.BGSpline.Curve
{
    /// <summary>Basic class for spline's points data</summary>
    [HelpURL("http://www.bansheegz.com/BGCurve/")]
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [Serializable]
    [AddComponentMenu("BansheeGz/BGCurve/BGCurve")]
    public class BGCurve : MonoBehaviour
    {
        #region static

        //===============================================================================================
        //                                                    Static 
        //===============================================================================================

        //Package version
        public const float Version = 1.24f;
        //Epsilon value (very small value, that can be ignored). Assuming 1=1 meter (Unity's recommendation), it equals to (1*10^-5)=10 micrometers 
        public const float Epsilon = 0.00001f;

        //min snapping distance
        public const float MinSnapDistance = 0.1f;
        //max snapping distance
        public const float MaxSnapDistance = 100;

        //Some private methods names  (editor may invoke private methods and fields to properly handle Undo/Redo operations and points mode conversion)
        public const string MethodAddPoint = "AddPoint";
        public const string MethodDeletePoint = "Delete";
        public const string MethodSetPointsNames = "SetPointsNames";
        public const string MethodAddField = "AddField";
        public const string MethodDeleteField = "DeleteField";
        public const string MethodConvertPoints = "ConvertPoints";


        //Event names (these events are fired at any rate)
        public const string EventClosed = "closed is changed";
        public const string EventSnapType = "snapType is changed";
        public const string EventSnapAxis = "snapAxis is changed";
        public const string EventSnapDistance = "snapDistance is changed";
        public const string EventSnapTrigger = "snapTriggerInteraction is changed";
        public const string EventSnapBackfaces = "snapToBackFaces is changed";
        public const string EventSnapLayerMask = "snapLayerMask is changed";
        public const string EventAddField = "add a field";
        public const string EventDeleteField = "delete a field";
        public const string EventFieldName = "field name is changed";
        public const string Event2D = "2d mode is changed";
        public const string EventForceUpdate = "force update is changed";

        public const string EventPointsMode = "points mode is changed";
        public const string EventClearAllPoints = "clear all points";
        public const string EventAddPoint = "add a point";
        public const string EventAddPoints = "add points";
        public const string EventDeletePoints = "delete points";
        public const string EventSwapPoints = "swap points";
        public const string EventReversePoints = "reverse points";


        //Event names (these events are fired only if events args are enabled). this is done for performance sake
        public const string EventTransaction = "changes in transaction";
        public const string EventTransform = "transform is changed";
        public const string EventForcedUpdate = "forced update";
        public const string EventPointPosition = "point position is changed";
        public const string EventPointTransform = "point transform is changed";
        public const string EventPointControl = "point control is changed";
        public const string EventPointControlType = "point control type is changed";
        public const string EventPointField = "point field value is changed";

        //static reusable array for snapping double sided
        private static readonly RaycastHit[] raycastHitArray = new RaycastHit[50];
        //static reusable array for deleting points
        private static readonly BGCurvePointI[] pointArray = new BGCurvePointI[1];
        //static reusable list for storing Unity objects
        private static readonly List<BGCurvePointI> pointsList = new List<BGCurvePointI>();
        //static reusable list for storing points index
        private static readonly List<int> pointsIndexesList = new List<int>();

        #endregion

        #region enums

        //===============================================================================================
        //                                                    Enums 
        //===============================================================================================

        /// <summary> 2D mode for a curve. If mode is on, only 2 points and controls coordinates matter, the third one is always 0</summary>
        public enum Mode2DEnum
        {
            Off,
            XY,
            XZ,
            YZ
        }

        /// <summary> 
        /// Snap mode for a curve. If mode is on, every curve's point (and every approximation point, if mode=Curve) will be snapped to the closest collider if any.
        /// With 'Curve' mode, Base Math gives better result, than Adaptive Math, cause it splits the curve into equal parts.
        /// Note, 'Curve' mode will add a huge overhead if you are changing the curve at runtime, cause for every approximation point 2 raycasts will be cast
        /// </summary>
        public enum SnapTypeEnum
        {
            /// <summary>No snapping </summary>
            Off,

            /// <summary>Only curve's points are snapped</summary>
            Points,

            /// <summary>Both curve's points and approximation points are snapped</summary>
            Curve
        }

        /// <summary> Snapping axis. For terrain use Y</summary>
        public enum SnapAxisEnum
        {
            X,
            Y,
            Z
        }

        /// <summary> Then events are fired</summary>
        public enum EventModeEnum
        {
            /// <summary>events are fired once per frame in Update</summary>
            Update,

            /// <summary>events are fired once per frame in LateUpdate. Use it if you are using animations</summary>
            LateUpdate,

            /// <summary>events are suppressed</summary>
            NoEvents
        }

        /// <summary> Force Changed event firing </summary>
        public enum ForceChangedEventModeEnum
        {
            /// <summary>Changed event is fired only if some change is detected</summary>
            Off,

            /// <summary>Forced Changed event is fired only in Editor</summary>
            EditorOnly,

            /// <summary>Forced Changed event is fired in Editor and every frame at Runtime</summary>
            EditorAndRuntime
        }

        /// <summary>How points are stored</summary>
        public enum PointsModeEnum
        {
            /// <summary>Points are stored right inside curve's components.</summary>
            Inlined,

            /// <summary>Points are stored as separate components(MonoBehaviours) attached to curve's GameObject.</summary>
            Components,

            /// <summary>Points are stored as separate components(MonoBehaviours) attached to their own GameObjects. Unity's transform is not used.</summary>
            GameObjectsNoTransform,

            /// <summary>
            /// Points are stored as separate components(MonoBehaviours) attached to their own GameObjects. Unity's transform position is used as point position. 
            /// Rotation and Scale affects controls positions.
            /// </summary>
            GameObjectsTransform
        }

        #endregion

        #region Fields & Events & Props & Delegates

        //===============================================================================================
        //                                                    Fields
        //===============================================================================================


#if UNITY_EDITOR
        // ============================================== !!! This is editor ONLY field
#pragma warning disable 0414
        [SerializeField] private BGCurveSettings settings = new BGCurveSettings();
#pragma warning restore 0414
#endif

        // ======================================= Delegates

        /// <summary>Iteration method callback, used by ForEach method for iterating over each point</summary>
        public delegate void IterationCallback(BGCurvePointI point, int index, int count);

        // ======================================= Events

        /// <summary>Any curve's change. By default events are fired once per frame in Update, set ImmediateChangeEvents to fire event as soon as any change</summary>
        public event EventHandler<BGCurveChangedArgs> Changed;

        /// <summary>Before any change.</summary>
        public event EventHandler<BGCurveChangedArgs.BeforeChange> BeforeChange;

        // ======================================= Fields (persistent)

        // 2D mode
        [Tooltip("2d Mode for a curve. In 2d mode, only 2 coordinates matter, the third will always be 0 (including controls). " +
                 "Handles in Editor will also be switched to 2d mode")] [SerializeField] private Mode2DEnum mode2D = Mode2DEnum.Off;

        //if curve is closed (e.g. if last and first point are connected)
        [Tooltip("If curve is closed")] [SerializeField] private bool closed;


        //points inlined
        [SerializeField] private BGCurvePoint[] points = new BGCurvePoint[0];

        //points components
        [SerializeField] private BGCurvePointComponent[] pointsComponents = new BGCurvePointComponent[0];

        //points as separate game objects
        [SerializeField] private BGCurvePointGO[] pointsGameObjects = new BGCurvePointGO[0];

        //custom fields
        [SerializeField] private BGCurvePointField[] fields = new BGCurvePointField[0];

        //snapping mode
        [Tooltip("Snap type. A collider should exists for points to snap to." +
                 "\r\n 1) Off - snaping is off" +
                 "\r\n 2) Points - only curve's points will be snapped." +
                 "\r\n 3) Curve - both curve's points and split points will be snapped. " +
                 "With 'Curve' mode Base Math type gives better results, than Adaptive Math, cause snapping occurs after approximation." +
                 "Also, 'Curve' mode can add a huge overhead if you are changing curve's points at runtime.")] [SerializeField] private SnapTypeEnum snapType = SnapTypeEnum.Off;

        //snapping axis
        [Tooltip("Axis for snapping points")] [SerializeField] private SnapAxisEnum snapAxis = SnapAxisEnum.Y;

        //snapping distance
        [Tooltip("Snapping distance.")] [SerializeField] [Range(MinSnapDistance, MaxSnapDistance)] private float snapDistance = 10;

        //snapping layer mask
        [Tooltip("Layer mask for snapping")] [SerializeField] private LayerMask snapLayerMask = -1;

        //should snapping take triggers into account
        [Tooltip("Should snapping takes triggers into account")] [SerializeField] private QueryTriggerInteraction snapTriggerInteraction = QueryTriggerInteraction.UseGlobal;

        //should snapping take triggers into account
        [Tooltip("Should snapping takes backfaces of colliders into account")] [SerializeField] private bool snapToBackFaces;

        //events mode
        [Tooltip("Event mode for runtime")] [SerializeField] private EventModeEnum eventMode = EventModeEnum.Update;

        //points mode
        [Tooltip("Points mode, how points are stored. " +
                 "\r\n 1) Inline - points stored inlined with the curve's component." +
                 "\r\n 2) Component - points are stored as MonoBehaviour scripts attached to the curve's GameObject." +
                 "\r\n 3) GameObject - points are stored as MonoBehaviour scripts attached to separate GameObject for each point.")] [SerializeField] private PointsModeEnum pointsMode =
            PointsModeEnum.Inlined;

        //force firing Update event 
        [Tooltip("Force firing of Changed event. This can be useful if you use Unity's Animation. Do not use it unless you really need it.")] [SerializeField] private ForceChangedEventModeEnum
            forceChangedEventMode;


        // ======================================= Props
        /// <summary>Curve's points</summary>
        public BGCurvePointI[] Points
        {
            get
            {
                switch (pointsMode)
                {
                    case PointsModeEnum.Inlined:
                        return points;
                    case PointsModeEnum.Components:
                        return pointsComponents;
                    case PointsModeEnum.GameObjectsNoTransform:
                    case PointsModeEnum.GameObjectsTransform:
                        return pointsGameObjects;
                    default:
                        throw new ArgumentOutOfRangeException("pointsMode");
                }
            }
        }

        /// <summary>Number of curve's points</summary>
        public int PointsCount
        {
            get
            {
                switch (pointsMode)
                {
                    case PointsModeEnum.Inlined:
                        return points.Length;
                    case PointsModeEnum.Components:
                        return pointsComponents.Length;
                    case PointsModeEnum.GameObjectsNoTransform:
                    case PointsModeEnum.GameObjectsTransform:
                        return pointsGameObjects.Length;
                    default:
                        throw new ArgumentOutOfRangeException("pointsMode");
                }
            }
        }

        /// <summary>Curve's custom fields</summary>
        public BGCurvePointField[] Fields
        {
            get { return fields; }
        }

        /// <summary>Number of curve's custom fields</summary>
        public int FieldsCount
        {
            get { return fields.Length; }
        }

        /// <summary>If curve is closed, e.g. last and first points are connected</summary>
        public bool Closed
        {
            get { return closed; }
            set
            {
                if (value == closed) return;
                FireBeforeChange(EventClosed);
                closed = value;
                FireChange(BGCurveChangedArgs.GetInstance(this, BGCurveChangedArgs.ChangeTypeEnum.Points, EventClosed));
            }
        }

        /// <summary>How points are stored</summary>
        public PointsModeEnum PointsMode
        {
            get { return pointsMode; }
            set
            {
                if (pointsMode == value) return;
                ConvertPoints(value);
            }
        }


        /// <summary>In 2d mode only 2 coordinates matter, the 3rd one will always be 0 (including control)</summary>
        public Mode2DEnum Mode2D
        {
            get { return mode2D; }
            set
            {
                if (mode2D == value) return;

                Apply2D(value);
            }
        }

        /// <summary>Is 2D mode On?</summary>
        public bool Mode2DOn
        {
            get { return mode2D != Mode2DEnum.Off; }
        }

        /// <summary>Snapping mode</summary>
        public SnapTypeEnum SnapType
        {
            get { return snapType; }
            set
            {
                if (snapType == value) return;
                FireBeforeChange(EventSnapType);
                snapType = value;
                if (snapType != SnapTypeEnum.Off) ApplySnapping();
                FireChange(BGCurveChangedArgs.GetInstance(this, BGCurveChangedArgs.ChangeTypeEnum.Snap, EventSnapType));
            }
        }

        /// <summary>Snapping axis. For terrain use Y</summary>
        public SnapAxisEnum SnapAxis
        {
            get { return snapAxis; }
            set
            {
                if (snapAxis == value) return;
                FireBeforeChange(EventSnapAxis);
                snapAxis = value;
                if (snapType != SnapTypeEnum.Off) ApplySnapping();
                FireChange(BGCurveChangedArgs.GetInstance(this, BGCurveChangedArgs.ChangeTypeEnum.Snap, EventSnapAxis));
            }
        }

        /// <summary>Snapping distance</summary>
        public float SnapDistance
        {
            get { return snapDistance; }
            set
            {
                if (Math.Abs(snapDistance - value) < Epsilon) return;

                FireBeforeChange(EventSnapDistance);
                snapDistance = Mathf.Clamp(value, MinSnapDistance, MaxSnapDistance);
                if (snapType != SnapTypeEnum.Off) ApplySnapping();
                FireChange(BGCurveChangedArgs.GetInstance(this, BGCurveChangedArgs.ChangeTypeEnum.Snap, EventSnapDistance));
            }
        }

        /// <summary>Should snapping take triggers into account</summary>
        public QueryTriggerInteraction SnapTriggerInteraction
        {
            get { return snapTriggerInteraction; }
            set
            {
                if (snapTriggerInteraction == value) return;

                FireBeforeChange(EventSnapTrigger);
                snapTriggerInteraction = value;
                if (snapType != SnapTypeEnum.Off) ApplySnapping();
                FireChange(BGCurveChangedArgs.GetInstance(this, BGCurveChangedArgs.ChangeTypeEnum.Snap, EventSnapTrigger));
            }
        }

        /// <summary>Should snapping take colliders backfaces into account</summary>
        public bool SnapToBackFaces
        {
            get { return snapToBackFaces; }
            set
            {
                if (snapToBackFaces == value) return;

                FireBeforeChange(EventSnapBackfaces);
                snapToBackFaces = value;
                if (snapType != SnapTypeEnum.Off) ApplySnapping();
                FireChange(BGCurveChangedArgs.GetInstance(this, BGCurveChangedArgs.ChangeTypeEnum.Snap, EventSnapBackfaces));
            }
        }

        /// <summary>Snapping layer mask</summary>
        public LayerMask SnapLayerMask
        {
            get { return snapLayerMask; }
            set
            {
                if (snapLayerMask == value) return;

                FireBeforeChange(EventSnapLayerMask);
                snapLayerMask = value;
                if (snapType != SnapTypeEnum.Off) ApplySnapping();
                FireChange(BGCurveChangedArgs.GetInstance(this, BGCurveChangedArgs.ChangeTypeEnum.Snap, EventSnapLayerMask));
            }
        }

        /// <summary>Force firing of Changed event. This can be useful if you use Unity's Animation. Do not use it unless you really need it. </summary>
        public ForceChangedEventModeEnum ForceChangedEventMode
        {
            get { return forceChangedEventMode; }
            set
            {
                if (forceChangedEventMode == value) return;

                FireBeforeChange(EventForceUpdate);
                forceChangedEventMode = value;
                FireChange(BGCurveChangedArgs.GetInstance(this, BGCurveChangedArgs.ChangeTypeEnum.Curve, EventForceUpdate));
            }
        }

        //------------------------------ these fields are not persistent
        //for batch operation (to avoid event firing for every operation)
        private int transactionLevel;

        //list of events grouped by transaction (this is used only if UseEventsArgs is set to true) 
        private List<BGCurveChangedArgs> changeList;

        //store custom fields structure for values. see FieldsTree comments for more info
        private FieldsTree fieldsTree;

        //changed flag is reset every frame.
        private bool changed;

        //fire Changed event as soon as change is made. By default they are grouped within a frame (for performance sake)
        private bool immediateChangeEvents;

        //stores event mode for reset after SupressEvent is set to false.
        private EventModeEnum eventModeOld = EventModeEnum.Update;

        //keeps last used eventType to use in FireFinalEvent. (if UseEventsArgs is false, we dont keep events list, fired during one single frame, and use only the last one) 
        private BGCurveChangedArgs.ChangeTypeEnum lastEventType;

        //keeps last event message to use in FireFinalEvent. (if UseEventsArgs is false, we dont keep events list, fired during one single frame, and use only the last one) 
        private string lastEventMessage;

        //list of points with Transforms attached ()
        private List<int> pointsWithTransforms;


        [Obsolete("It is not used anymore and should be removed")]
        public bool TraceChanges
        {
            get { return Changed != null; }
            set { }
        }

        /// <summary> Disable events firing temporarily </summary>
        public bool SupressEvents
        {
            get { return eventMode == EventModeEnum.NoEvents; }
            set
            {
                if (value && eventMode != EventModeEnum.NoEvents) eventModeOld = eventMode;
                eventMode = value ? EventModeEnum.NoEvents : eventModeOld;
            }
        }

        /// <summary> Use events args (no need for them if you do not use them). </summary>
        public bool UseEventsArgs { get; set; }

        /// <summary> How events are fired</summary>
        public EventModeEnum EventMode
        {
            get { return eventMode; }
            set { eventMode = value; }
        }

        /// <summary> Should events be fired immediately after the change. This mode is used by Editors and not recommended for runtime.</summary>
        public bool ImmediateChangeEvents
        {
            get { return immediateChangeEvents; }
            set { immediateChangeEvents = value; }
        }

        //the list of events accumulated during transaction. (this is used only if UseEventsArgs is set to true) 
        private List<BGCurveChangedArgs> ChangeList
        {
            get { return changeList ?? (changeList = new List<BGCurveChangedArgs>()); }
        }

        #endregion

        #region Public Functions

        //===============================================================================================
        //                                                    Public functions
        //===============================================================================================
        // only required ones- no math. to use math use BGCurveBaseMath or BGCurveAdaptiveMath or your own class with provided classes as an example

        // ======================================= Points construction
        /// <summary>Create a point, world coordinate are used</summary>
        public BGCurvePoint CreatePointFromWorldPosition(Vector3 worldPos, BGCurvePoint.ControlTypeEnum controlType)
        {
            return new BGCurvePoint(this, worldPos, controlType, true);
        }

        /// <summary>Create a point, world coordinate are used</summary>
        public BGCurvePoint CreatePointFromWorldPosition(Vector3 worldPos, BGCurvePoint.ControlTypeEnum controlType,
            Vector3 control1WorldPos, Vector3 control2WorldPos)
        {
            return new BGCurvePoint(this, worldPos, controlType, control1WorldPos, control2WorldPos, true);
        }

        /// <summary>Create a point, local coordinate are used</summary>
        public BGCurvePoint CreatePointFromLocalPosition(Vector3 localPos, BGCurvePoint.ControlTypeEnum controlType)
        {
            return new BGCurvePoint(this, localPos, controlType);
        }

        /// <summary>Create a point, local coordinate are used</summary>
        public BGCurvePoint CreatePointFromLocalPosition(Vector3 localPos, BGCurvePoint.ControlTypeEnum controlType,
            Vector3 control1LocalPos, Vector3 control2LocalPos)
        {
            return new BGCurvePoint(this, localPos, controlType, control1LocalPos, control2LocalPos);
        }


        // ======================================= Points handling
        /// <summary>Remove all points</summary>
        public void Clear()
        {
            var pointsCount = PointsCount;
            if (pointsCount == 0) return;

            FireBeforeChange(EventClearAllPoints);

            switch (pointsMode)
            {
                case PointsModeEnum.Inlined:
                    points = new BGCurvePoint[0];
                    break;
                case PointsModeEnum.Components:
                    if (pointsCount > 0) for (var i = pointsCount - 1; i >= 0; i--) DestroyIt(pointsComponents[i]);
                    pointsComponents = new BGCurvePointComponent[0];
                    break;
                case PointsModeEnum.GameObjectsNoTransform:
                case PointsModeEnum.GameObjectsTransform:
                    if (pointsCount > 0) for (var i = pointsCount - 1; i >= 0; i--) DestroyIt(pointsGameObjects[i].gameObject);
                    pointsGameObjects = new BGCurvePointGO[0];
                    break;
            }
            FireChange(BGCurveChangedArgs.GetInstance(this, BGCurveChangedArgs.ChangeTypeEnum.Points, EventClearAllPoints));
        }

        /// <summary>Returns a point's index</summary>
        public int IndexOf(BGCurvePointI point)
        {
            return IndexOf(Points, point);
        }


        /// <summary>Add a point to the end</summary>
        public BGCurvePointI AddPoint(BGCurvePoint point)
        {
            return AddPoint(point, PointsCount, null);
        }

        /// <summary>Add a point at specified index</summary>
        public BGCurvePointI AddPoint(BGCurvePoint point, int index)
        {
            return AddPoint(point, index, null);
        }

        /// <summary>Add several points. </summary>
        public void AddPoints(BGCurvePoint[] points)
        {
            AddPoints(points, PointsCount, false);
        }

        /// <summary>Add several points at specified index.</summary>
        public void AddPoints(BGCurvePoint[] points, int index)
        {
            AddPoints(points, index, false);
        }

        /// <summary>Removes a point</summary>
        public void Delete(BGCurvePointI point)
        {
            Delete(IndexOf(point), null);
        }

        /// <summary>Removes a point at specified index</summary>
        public void Delete(int index)
        {
            Delete(index, null);
        }

        /// <summary>Removes several points</summary>
        public void Delete(BGCurvePointI[] points)
        {
            Delete(points, null);
        }

        /// <summary>Swaps 2 points</summary>
        public void Swap(int index1, int index2)
        {
            if (index1 < 0 || index1 >= PointsCount || index2 < 0 || index2 >= PointsCount) throw new UnityException("Unable to remove a point. Invalid indexes: " + index1 + ", " + index2);

            FireBeforeChange(EventSwapPoints);

            var points = Points;

            var point1 = points[index1];
            var point2 = points[index2];

            var hasTransform = point1.PointTransform != null || point2.PointTransform != null;

            points[index2] = point1;
            points[index1] = point2;

            if (IsGoMode(pointsMode)) SetPointsNames();

            if (hasTransform) CachePointsWithTransforms();

            FireChange(BGCurveChangedArgs.GetInstance(this, BGCurveChangedArgs.ChangeTypeEnum.Points, EventSwapPoints));
        }

        /// <summary>Reverse points order, keeping curve intact</summary>
        public void Reverse()
        {
            var pointsCount = PointsCount;

            if (pointsCount < 2) return;
            FireBeforeChange(EventReversePoints);

            var points = Points;
            var hasFields = FieldsCount > 0;
            var pointsMode = PointsMode;

            var mid = pointsCount >> 1;
            var countMinusOne = pointsCount - 1;
            for (var i = 0; i < mid; i++)
            {
                var point1 = points[i];
                var point2 = points[countMinusOne - i];

                var position = point2.PositionLocal;
                var controlType = point2.ControlType;
                var control1 = point2.ControlFirstLocal;
                var control2 = point2.ControlSecondLocal;
                var fields2 = hasFields ? GetFieldsValues(point2, pointsMode) : null;

                point2.PositionLocal = point1.PositionLocal;
                point2.ControlType = point1.ControlType;
                point2.ControlFirstLocal = point1.ControlSecondLocal;
                point2.ControlSecondLocal = point1.ControlFirstLocal;
                if (hasFields) SetFieldsValues(point2, pointsMode, GetFieldsValues(point1, pointsMode));

                point1.PositionLocal = position;
                point1.ControlType = controlType;
                point1.ControlFirstLocal = control2;
                point1.ControlSecondLocal = control1;
                if (hasFields) SetFieldsValues(point1, pointsMode, fields2);
            }

            if (pointsCount%2 != 0)
            {
                //point at the center- we need to swap controls
                var point = points[mid];
                var control = point.ControlFirstLocal;
                point.ControlFirstLocal = point.ControlSecondLocal;
                point.ControlSecondLocal = control;
            }

            if (IsGoMode(pointsMode)) SetPointsNames();

            CachePointsWithTransforms();

            FireChange(BGCurveChangedArgs.GetInstance(this, BGCurveChangedArgs.ChangeTypeEnum.Points, EventReversePoints));
        }

        /// <summary>Access a point by index </summary>
        public BGCurvePointI this[int i]
        {
            get
            {
                switch (pointsMode)
                {
                    case PointsModeEnum.Inlined:
                        return points[i];
                    case PointsModeEnum.Components:
                        return pointsComponents[i];
                    case PointsModeEnum.GameObjectsNoTransform:
                    case PointsModeEnum.GameObjectsTransform:
                        return pointsGameObjects[i];
                    default:
                        throw new ArgumentOutOfRangeException("pointsMode");
                }
            }
            set
            {
                switch (pointsMode)
                {
                    case PointsModeEnum.Inlined:
                        points[i] = (BGCurvePoint) value;
                        break;
                    case PointsModeEnum.Components:
                        pointsComponents[i] = (BGCurvePointComponent) value;
                        break;
                    case PointsModeEnum.GameObjectsNoTransform:
                    case PointsModeEnum.GameObjectsTransform:
                        pointsGameObjects[i] = (BGCurvePointGO) value;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("pointsMode");
                }
            }
        }

        // ============================================== Custom Fields
        /// <summary>Add a custom field</summary>
        public BGCurvePointField AddField(string name, BGCurvePointField.TypeEnum type)
        {
            return AddField(name, type, null);
        }


        /// <summary>Delete a custom field </summary>
        public void DeleteField(BGCurvePointField field)
        {
            DeleteField(field, null);
        }

        /// <summary>Get number of field</summary>
        public int IndexOf(BGCurvePointField field)
        {
            return IndexOf(fields, field);
        }

        /// <summary>is curve has a field with provided name</summary>
        public bool HasField(string name)
        {
            if (FieldsCount == 0) return false;
            foreach (var field in fields) if (String.Equals(name, field.FieldName)) return true;
            return false;
        }

        /// <summary>
        /// get the index of field's value within array of values 
        /// this method is meant to be private, but we can not keep it private cause of performance
        /// </summary>
        public int IndexOfFieldValue(string name)
        {
            if (fieldsTree == null || !fieldsTree.Comply(fields)) PrivateUpdateFieldsValuesIndexes();
            return fieldsTree.GetIndex(name);
        }

        /// <summary>all methods, prefixed with Private, are not meant to be called from outside of BGCurve package </summary>
        // updates indexes for custom fields values. See FieldsTree for details.
        public void PrivateUpdateFieldsValuesIndexes()
        {
            fieldsTree = fieldsTree ?? new FieldsTree();
            fieldsTree.Update(fields);
        }

        // ============================================== 2D mode.
        /// <summary>apply 2D mode to all points</summary>
        public void Apply2D(Mode2DEnum value)
        {
            FireBeforeChange(Event2D);
            mode2D = value;

            if (mode2D != Mode2DEnum.Off && PointsCount > 0)
            {
                Transaction(() =>
                {
                    var points = Points;
                    var count = points.Length;
                    for (var i = 0; i < count; i++) Apply2D(points[i]);
                });
            }
            else FireChange(BGCurveChangedArgs.GetInstance(this, BGCurveChangedArgs.ChangeTypeEnum.Points, Event2D));
        }

        /// <summary>change point's coordinate to comply to 2d mode</summary>
        public virtual void Apply2D(BGCurvePointI point)
        {
            point.PositionLocal = point.PositionLocal;
            point.ControlFirstLocal = point.ControlFirstLocal;
            point.ControlSecondLocal = point.ControlSecondLocal;
        }

        /// <summary>change vector coordinates to comply to 2d mode</summary>
        public virtual Vector3 Apply2D(Vector3 point)
        {
            switch (mode2D)
            {
                case Mode2DEnum.XY:
                    return new Vector3(point.x, point.y, 0);
                case Mode2DEnum.XZ:
                    return new Vector3(point.x, 0, point.z);
                case Mode2DEnum.YZ:
                    return new Vector3(0, point.y, point.z);
            }
            return point;
        }

        // ============================================== Snapping.
        /// <summary>apply snapping to all points</summary>
        public void ApplySnapping()
        {
            if (snapType == SnapTypeEnum.Off) return;

            var points = Points;
            var length = points.Length;
            for (var i = 0; i < length; i++) ApplySnapping(points[i]);
        }

        /// <summary>apply snapping to one single point</summary>
        public void ApplySnapping(BGCurvePointI point)
        {
            if (snapType == SnapTypeEnum.Off) return;
            var pos = point.PositionWorld;

            if (ApplySnapping(ref pos)) point.PositionWorld = pos;
        }

        /// <summary>apply snapping to one position and returns true if position was changed</summary>
        public bool ApplySnapping(ref Vector3 pos)
        {
            if (snapType == SnapTypeEnum.Off) return false;

            //assign direction
            Vector3 direction;
            switch (snapAxis)
            {
                case SnapAxisEnum.Y:
                    direction = Vector3.up;
                    break;
                case SnapAxisEnum.X:
                    direction = Vector3.right;
                    break;
                default:
                    //                case SnapAxisEnum.Z:
                    direction = Vector3.forward;
                    break;
            }

            //cast front side
            var resultPos = new Vector3();
            float minDistance = -1;
            for (var k = 0; k < 2; k++)
            {
                var ray = new Ray(pos, k == 0 ? direction : -direction);

                RaycastHit hit;
                if (!Physics.Raycast(ray, out hit, snapDistance, snapLayerMask, snapTriggerInteraction)) continue;

                if (!(minDistance < 0) && !(minDistance > hit.distance)) continue;

                minDistance = hit.distance;
                resultPos = hit.point;
            }

            //cast back side
            if (snapToBackFaces)
            {
                for (var j = 0; j < 2; j++)
                {
                    var count = Physics.RaycastNonAlloc(j == 0
                            ? new Ray(new Vector3(pos.x + direction.x*snapDistance, pos.y + direction.y*snapDistance, pos.z + direction.z*snapDistance), -direction)
                            : new Ray(new Vector3(pos.x - direction.x*snapDistance, pos.y - direction.y*snapDistance, pos.z - direction.z*snapDistance), direction),
                        raycastHitArray, snapDistance, snapLayerMask, snapTriggerInteraction);
                    if (count == 0) continue;

                    //iterate all unsorted results
                    for (var i = 0; i < count; i++)
                    {
                        var raycastHit = raycastHitArray[i];
                        var distanceToCheck = snapDistance - raycastHit.distance;
                        if (!(minDistance < 0) && !(minDistance > distanceToCheck)) continue;

                        minDistance = distanceToCheck;
                        resultPos = raycastHit.point;
                    }
                }
            }

            if (minDistance < 0) return false;

            //assign new pos
            pos = resultPos;

            return true;
        }


        // ============================================== batch handling (to avoid event firing for every operation).
        /// <summary>
        /// Executes a batch operation.
        /// During batch operation events firing will be suppressed, and event will be fired only after Transaction.
        /// it is for event handling only .
        /// </summary>
        public void Transaction(Action action)
        {
            FireBeforeChange(EventTransaction);
            transactionLevel++;
            if (UseEventsArgs && transactionLevel == 1) ChangeList.Clear();
            try
            {
                action();
            }
            finally
            {
                transactionLevel--;
                if (transactionLevel == 0)
                {
                    FireChange(UseEventsArgs
                        ? BGCurveChangedArgs.GetInstance(this, ChangeList.ToArray(), EventTransaction)
                        : null);
                    if (UseEventsArgs) ChangeList.Clear();
                }
            }
        }

        /// <summary> Current transaction level.</summary>
        public int TransactionLevel
        {
            get { return transactionLevel; }
        }

        // search for index within array
        protected internal static int IndexOf<T>(T[] array, T item)
        {
            return Array.IndexOf(array, item);
        }


        // ============================================== event handling
        /// <summary> Fires BeforeChange event if not within transaction and events are not suppressed.</summary>
        public void FireBeforeChange(string operation)
        {
            if (eventMode == EventModeEnum.NoEvents || transactionLevel > 0 || BeforeChange == null) return;

            BeforeChange(this, UseEventsArgs ? BGCurveChangedArgs.BeforeChange.GetInstance(operation) : null);
        }

        /// <summary> Fires Changed event (if some conditions are met)</summary>
        public void FireChange(BGCurveChangedArgs change, bool ignoreEventsGrouping = false, object sender = null)
        {
            if (eventMode == EventModeEnum.NoEvents || Changed == null) return;

            if (transactionLevel > 0 || (!immediateChangeEvents && !ignoreEventsGrouping))
            {
                changed = true;
                if (change != null)
                {
                    lastEventType = change.ChangeType;
                    lastEventMessage = change.Message;
                }
                if (UseEventsArgs && !ChangeList.Contains(change)) ChangeList.Add((BGCurveChangedArgs) change.Clone());
                return;
            }

            Changed(sender ?? this, change);
        }

        // ============================================== Unity callbacks
        public void Start()
        {
            CachePointsWithTransforms();
        }

        // Unity's Update callback
        protected virtual void Update()
        {
            // is transform.hasChanged is true by default?
            if (Time.frameCount == 1) transform.hasChanged = false;
            if (eventMode != EventModeEnum.Update || Changed == null) return;
            FireFinalEvent();
        }

        // Unity's LateUpdate callback
        protected virtual void LateUpdate()
        {
            if (eventMode != EventModeEnum.LateUpdate || Changed == null) return;
            FireFinalEvent();
        }

        // ============================================== Utility methods
        /// <summary> world position to local position</summary>
        public Vector3 ToLocal(Vector3 worldPoint)
        {
            return transform.InverseTransformPoint(worldPoint);
        }

        /// <summary> local position to world position</summary>
        public Vector3 ToWorld(Vector3 localPoint)
        {
            return transform.TransformPoint(localPoint);
        }

        /// <summary> direction from world to local </summary>
        public Vector3 ToLocalDirection(Vector3 direction)
        {
            return transform.InverseTransformDirection(direction);
        }

        /// <summary> direction from local to world </summary>
        public Vector3 ToWorldDirection(Vector3 direction)
        {
            return transform.TransformDirection(direction);
        }

        /// <summary> execute Action for each point. Params are Point,index,length. </summary>
        public void ForEach(IterationCallback iterationCallback)
        {
            //do not replace PointsCount with local var, cause it can be changed while iteration
            for (var i = 0; i < PointsCount; i++) iterationCallback(Points[i], i, PointsCount);
        }

        //update points GameObjects names and sort
        private void SetPointsNames()
        {
            try
            {
                if (gameObject == null) return;
            }
            catch (MissingReferenceException)
            {
                return;
            }

            var name = gameObject.name;
            var length = pointsGameObjects.Length;
            for (var i = 0; i < length; i++)
            {
                var go = pointsGameObjects[i].gameObject;

                //set name
                go.name = name + "[" + i + "]";

                //move to bottom
                go.transform.SetSiblingIndex(go.transform.parent.childCount - 1);
            }
        }

        // copy arrays and add a new element
        public static T[] Insert<T>(T[] oldArray, int index, T[] newElements)
        {
            var newArray = new T[oldArray.Length + newElements.Length];

            //copy before index
            if (index > 0) Array.Copy(oldArray, newArray, index);

            //copy after index
            if (index < oldArray.Length) Array.Copy(oldArray, index, newArray, index + newElements.Length, oldArray.Length - index);

            //copy new elements
            Array.Copy(newElements, 0, newArray, index, newElements.Length);

            return newArray;
        }

        // copy arrays and add a new element
        public static T[] Insert<T>(T[] oldArray, int index, T newElement)
        {
            var newArray = new T[oldArray.Length + 1];

            //copy before index
            if (index > 0) Array.Copy(oldArray, newArray, index);

            //copy after index
            if (index < oldArray.Length) Array.Copy(oldArray, index, newArray, index + 1, oldArray.Length - index);

            newArray[index] = newElement;

            return newArray;
        }

        // copy arrays and removes an element
        public static T[] Remove<T>(T[] oldArray, int index)
        {
            var newArray = new T[oldArray.Length - 1];
            if (index > 0) Array.Copy(oldArray, newArray, index);

            if (index < oldArray.Length - 1) Array.Copy(oldArray, index + 1, newArray, index, oldArray.Length - 1 - index);

            return newArray;
        }

        /// <summary>standard c# toString override</summary>
        public override string ToString()
        {
            return "BGCurve [id=" + GetInstanceID() + "], points=" + PointsCount;
        }

        /// <summary>if this mode uses separate GameObjects for points </summary>
        public static bool IsGoMode(PointsModeEnum pointsMode)
        {
            return pointsMode == PointsModeEnum.GameObjectsNoTransform || pointsMode == PointsModeEnum.GameObjectsTransform;
        }


        // ============================================== Point's transforms
        /// <summary>all methods, prefixed with Private, are not meant to be called from outside of BGCurve package </summary>
        //if a transform was added to a point
        public void PrivateTransformForPointAdded(int index)
        {
            if (index < 0 || PointsCount >= index) return;
            if (pointsWithTransforms == null) pointsWithTransforms = new List<int>();

            if (pointsWithTransforms.IndexOf(index) == -1) pointsWithTransforms.Add(index);
        }

        /// <summary>all methods, prefixed with Private, are not meant to be called from outside of BGCurve package </summary>
        //if a transform was removed from a point
        public void PrivateTransformForPointRemoved(int index)
        {
            if (pointsWithTransforms == null || pointsWithTransforms.IndexOf(index) == -1) return;

            pointsWithTransforms.Remove(index);
        }

        #endregion

        #region Private Functions

        //===============================================================================================
        //                                                    Private functions
        //===============================================================================================

        // ============================================== Points
        //add a point. this method is not meant to be public, Editor uses it via reflection
        private BGCurvePointI AddPoint(BGCurvePoint point, int index, Func<BGCurvePointI> provider = null)
        {
            if (index < 0 || index > PointsCount) throw new UnityException("Unable to add a point. Invalid index: " + index);

            FireBeforeChange(EventAddPoint);

            BGCurvePointI result;

            //insert a point
            switch (pointsMode)
            {
                case PointsModeEnum.Inlined:
                    points = Insert(points, index, point);
                    result = point;
                    break;

                case PointsModeEnum.Components:
                    var pointComponent = (BGCurvePointComponent) Convert(point, PointsModeEnum.Inlined, pointsMode, provider);
                    pointsComponents = Insert(pointsComponents, index, pointComponent);
                    result = pointComponent;
                    break;

                case PointsModeEnum.GameObjectsNoTransform:
                case PointsModeEnum.GameObjectsTransform:
                    var pointGameObject = (BGCurvePointGO) Convert(point, PointsModeEnum.Inlined, pointsMode, provider);
                    pointsGameObjects = Insert(pointsGameObjects, index, pointGameObject);

                    SetPointsNames();

                    result = pointGameObject;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("pointsMode");
            }

            //fields
            if (FieldsCount > 0)
            {
                var fieldValues = GetFieldsValues(result, pointsMode);
                foreach (var field in fields) BGCurvePoint.PrivateFieldAdded(field, fieldValues);
            }

            //point transforms
            if ((point.PointTransform != null) || (pointsWithTransforms != null && pointsWithTransforms.Count > 0)) CachePointsWithTransforms();

            FireChange(BGCurveChangedArgs.GetInstance(this, BGCurveChangedArgs.ChangeTypeEnum.Point, EventAddPoint));

            return result;
        }

        //retrieve fields values from point
        private BGCurvePoint.FieldsValues GetFieldsValues(BGCurvePointI point, PointsModeEnum pointsMode)
        {
            BGCurvePoint.FieldsValues fieldValues;
            switch (pointsMode)
            {
                case PointsModeEnum.Inlined:
                    fieldValues = ((BGCurvePoint) point).PrivateValuesForFields;
                    break;
                case PointsModeEnum.Components:
                    fieldValues = ((BGCurvePointComponent) point).Point.PrivateValuesForFields;
                    break;
                case PointsModeEnum.GameObjectsNoTransform:
                case PointsModeEnum.GameObjectsTransform:
                    fieldValues = ((BGCurvePointGO) point).PrivateValuesForFields;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("pointsMode");
            }
            return fieldValues;
        }

        //set fields values for point
        private void SetFieldsValues(BGCurvePointI point, PointsModeEnum pointsMode, BGCurvePoint.FieldsValues fieldsValues)
        {
            switch (pointsMode)
            {
                case PointsModeEnum.Inlined:
                    ((BGCurvePoint) point).PrivateValuesForFields = fieldsValues;
                    break;
                case PointsModeEnum.Components:
                    ((BGCurvePointComponent) point).Point.PrivateValuesForFields = fieldsValues;
                    break;
                case PointsModeEnum.GameObjectsNoTransform:
                case PointsModeEnum.GameObjectsTransform:
                    ((BGCurvePointGO) point).PrivateValuesForFields = fieldsValues;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("pointsMode");
            }
        }

        // add several points. this method is not meant to be public, Editor uses it via reflection
        private void AddPoints(BGCurvePoint[] points, int index, bool skipFieldsProcessing = false, Func<BGCurvePointI> provider = null)
        {
            if (points == null) return;

            var pointsLength = points.Length;

            if (pointsLength == 0) return;

            if (index < 0 || index > PointsCount) throw new UnityException("Unable to add points. Invalid index: " + index);


            FireBeforeChange(EventAddPoints);

            BGCurvePointI[] addedPoints;
            var hasPointTransform = pointsWithTransforms != null && pointsWithTransforms.Count > 0;

            //add points
            switch (pointsMode)
            {
                case PointsModeEnum.Inlined:

                    this.points = Insert(this.points, index, points);
                    if (!hasPointTransform)
                    {
                        for (var i = 0; i < pointsLength; i++)
                        {
                            if (points[i].PointTransform == null) continue;
                            hasPointTransform = true;
                            break;
                        }
                    }
                    addedPoints = points;
                    break;
                case PointsModeEnum.Components:

                    var toAdd = new BGCurvePointComponent[pointsLength];
                    for (var i = 0; i < pointsLength; i++)
                    {
                        var point = points[i];
                        hasPointTransform = hasPointTransform || point.PointTransform != null;
                        toAdd[i] = (BGCurvePointComponent) Convert(point, PointsModeEnum.Inlined, pointsMode, provider);
                    }
                    pointsComponents = Insert(pointsComponents, index, toAdd);

                    addedPoints = points;
                    break;
                case PointsModeEnum.GameObjectsNoTransform:
                case PointsModeEnum.GameObjectsTransform:

                    var pointsToAdd = new BGCurvePointGO[pointsLength];
                    for (var i = 0; i < pointsLength; i++)
                    {
                        var point = points[i];
                        hasPointTransform = hasPointTransform || point.PointTransform != null;
                        pointsToAdd[i] = (BGCurvePointGO) Convert(point, PointsModeEnum.Inlined, pointsMode, provider);
                    }
                    pointsGameObjects = Insert(pointsGameObjects, index, pointsToAdd);

                    SetPointsNames();

                    addedPoints = pointsToAdd;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("pointsMode");
            }


            //process fields
            if (!skipFieldsProcessing && FieldsCount > 0) AddFields(pointsMode, addedPoints);

            //point transforms
            if (hasPointTransform) CachePointsWithTransforms();


            FireChange(BGCurveChangedArgs.GetInstance(this, BGCurveChangedArgs.ChangeTypeEnum.Points, EventAddPoints));
        }

        // removes a point. this method is not meant to be public, Editor uses it via reflection
        private void Delete(int index, Action<BGCurvePointI> destroyer)
        {
            if (index < 0 || index >= PointsCount) throw new UnityException("Unable to remove a point. Invalid index: " + index);

            switch (pointsMode)
            {
                case PointsModeEnum.Inlined:
                    pointArray[0] = points[index];
                    break;
                case PointsModeEnum.Components:
                    pointArray[0] = pointsComponents[index];
                    break;
                case PointsModeEnum.GameObjectsNoTransform:
                case PointsModeEnum.GameObjectsTransform:
                    pointArray[0] = pointsGameObjects[index];
                    break;
                default:
                    throw new ArgumentOutOfRangeException("pointsMode");
            }
            Delete(pointArray, destroyer);
        }

        // removes points. this method is not meant to be public, Editor uses it via reflection
        private void Delete(BGCurvePointI[] pointsToDelete, Action<BGCurvePointI> destroyer)
        {
            if (pointsToDelete == null || pointsToDelete.Length == 0 || PointsCount == 0) return;

            pointsList.Clear();
            pointsIndexesList.Clear();

            var oldPoints = Points;

            //find point indexes
            var length = pointsToDelete.Length;
            for (var i = 0; i < length; i++)
            {
                var index = Array.IndexOf(oldPoints, pointsToDelete[i]);
                if (index >= 0) pointsIndexesList.Add(index);
            }

            //no luck
            if (pointsIndexesList.Count == 0) return;

            //at this point we know for sure we have to delete at least one point
            FireBeforeChange(EventDeletePoints);

            //assign new array
            var newLength = oldPoints.Length - pointsIndexesList.Count;
            BGCurvePointI[] newPoints;
            switch (pointsMode)
            {
                case PointsModeEnum.Inlined:
                    points = new BGCurvePoint[newLength];
                    newPoints = points;
                    break;
                case PointsModeEnum.Components:
                    pointsComponents = new BGCurvePointComponent[newLength];
                    newPoints = pointsComponents;
                    break;
                case PointsModeEnum.GameObjectsNoTransform:
                case PointsModeEnum.GameObjectsTransform:
                    pointsGameObjects = new BGCurvePointGO[newLength];
                    newPoints = pointsGameObjects;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("pointsMode");
            }

            //sort indexes to delete
            pointsIndexesList.Sort();

            //fill new array
            var cursor = 0;
            var count = pointsIndexesList.Count;
            for (var i = 0; i < count; i++)
            {
                var indexToRemove = pointsIndexesList[i];

                if (indexToRemove > cursor) Array.Copy(oldPoints, cursor, newPoints, cursor - i, indexToRemove - cursor);

                cursor = indexToRemove + 1;

                switch (pointsMode)
                {
                    case PointsModeEnum.Components:
                    case PointsModeEnum.GameObjectsNoTransform:
                    case PointsModeEnum.GameObjectsTransform:
                        pointsList.Add(oldPoints[indexToRemove]);
                        break;
                }
            }
            if (cursor < oldPoints.Length) Array.Copy(oldPoints, cursor, newPoints, cursor - count, oldPoints.Length - cursor);


            //sort and set new names for GO points
            switch (pointsMode)
            {
                case PointsModeEnum.GameObjectsNoTransform:
                case PointsModeEnum.GameObjectsTransform:
                    SetPointsNames();
                    break;
            }

            //destroy 
            if (pointsList.Count > 0)
            {
                var pointsCount = pointsList.Count;
                for (var i = 0; i < pointsCount; i++)
                {
                    var toDelete = pointsList[i];

                    if (destroyer != null) destroyer(toDelete);
                    else
                    {
                        switch (pointsMode)
                        {
                            case PointsModeEnum.Components:
                                DestroyIt((Object) toDelete);
                                break;
                            case PointsModeEnum.GameObjectsNoTransform:
                            case PointsModeEnum.GameObjectsTransform:
                                DestroyIt(((BGCurvePointGO) toDelete).gameObject);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException("pointsMode");
                        }
                    }
                }
            }

            CachePointsWithTransforms();

            pointsList.Clear();
            pointsIndexesList.Clear();

            FireChange(BGCurveChangedArgs.GetInstance(this, BGCurveChangedArgs.ChangeTypeEnum.Points, EventDeletePoints));
        }

        // ============================================== Point conversion
        //convert between points store options, attached to GameObject. it's also accessed via reflection by Editor
        // !! NOTE: do not change the order of different actions- it affects Undo/Redo operations
        private void ConvertPoints(PointsModeEnum pointsMode, Func<BGCurvePointI> provider = null, Action<BGCurvePointI> destroyer = null)
        {
            var oldMode = PointsMode;
            if (oldMode == pointsMode) return;

            FireBeforeChange(EventPointsMode);

            switch (oldMode)
            {
                case PointsModeEnum.Inlined:

                    if (points.Length > 0)
                    {
                        //-------------------------------------------------------  from inlined

                        switch (pointsMode)
                        {
                            case PointsModeEnum.Components:
                            {
                                // to components
                                var toAdd = new BGCurvePointComponent[points.Length];
                                for (var i = 0; i < points.Length; i++) toAdd[i] = (BGCurvePointComponent) Convert(points[i], oldMode, pointsMode, provider);

                                this.pointsMode = pointsMode;
                                pointsComponents = Insert(pointsComponents, 0, toAdd);

                                //no need to process fields, cause the same point is reused
                            }
                                break;
                            case PointsModeEnum.GameObjectsNoTransform:
                            case PointsModeEnum.GameObjectsTransform:
                            {
                                // to gameobjects
                                var toAdd = new BGCurvePointGO[points.Length];

                                for (var i = 0; i < points.Length; i++) toAdd[i] = (BGCurvePointGO) Convert(points[i], oldMode, pointsMode, provider);

                                this.pointsMode = pointsMode;
                                pointsGameObjects = Insert(pointsGameObjects, 0, toAdd);

                                SetPointsNames();

                                //fields
                                if (FieldsCount > 0) AddFields(pointsMode, toAdd);
                            }
                                break;
                        }
                        points = new BGCurvePoint[0];
                    }
                    break;
                case PointsModeEnum.Components:
                case PointsModeEnum.GameObjectsNoTransform:
                case PointsModeEnum.GameObjectsTransform:

                    var hasAnyPoint = oldMode == PointsModeEnum.Components && pointsComponents.Length > 0 || (IsGoMode(oldMode) && pointsGameObjects.Length > 0);

                    if (hasAnyPoint)
                    {
                        //------------------------------------------------------- from components OR  from GO
                        BGCurvePointI[] toRemove = null;
                        if (oldMode == PointsModeEnum.Components)
                        {
                            //-------------------------------------------------------  from components
                            switch (pointsMode)
                            {
                                case PointsModeEnum.Inlined:
                                    var pointsToAdd = new BGCurvePoint[pointsComponents.Length];

                                    for (var i = 0; i < pointsComponents.Length; i++) pointsToAdd[i] = (BGCurvePoint) Convert(pointsComponents[i], oldMode, pointsMode, provider);

                                    points = Insert(points, 0, pointsToAdd);

                                    //no need to process fields, cause the same point is reused
                                    break;
                                case PointsModeEnum.GameObjectsNoTransform:
                                case PointsModeEnum.GameObjectsTransform:
                                    var toAdd = new BGCurvePointGO[pointsComponents.Length];

                                    for (var i = 0; i < pointsComponents.Length; i++) toAdd[i] = (BGCurvePointGO) Convert(pointsComponents[i], oldMode, pointsMode, provider);

                                    pointsGameObjects = Insert(pointsGameObjects, 0, toAdd);

                                    SetPointsNames();

                                    //fields
                                    if (FieldsCount > 0) AddFields(pointsMode, toAdd);

                                    break;
                            }

                            toRemove = pointsComponents;
                            pointsComponents = new BGCurvePointComponent[0];
                        }
                        else
                        {
                            //-------------------------------------------------------  from GO

                            switch (pointsMode)
                            {
                                case PointsModeEnum.Inlined:

                                    var pointsToAdd = new BGCurvePoint[pointsGameObjects.Length];

                                    for (var i = 0; i < pointsGameObjects.Length; i++) pointsToAdd[i] = (BGCurvePoint) Convert(pointsGameObjects[i], oldMode, pointsMode, provider);

                                    points = Insert(points, 0, pointsToAdd);

                                    toRemove = pointsGameObjects;
                                    break;

                                case PointsModeEnum.Components:

                                    var toAdd = new BGCurvePointComponent[pointsGameObjects.Length];

                                    for (var i = 0; i < pointsGameObjects.Length; i++) toAdd[i] = (BGCurvePointComponent) Convert(pointsGameObjects[i], oldMode, pointsMode, provider);

                                    pointsComponents = Insert(pointsComponents, 0, toAdd);

                                    toRemove = pointsGameObjects;
                                    break;

                                case PointsModeEnum.GameObjectsNoTransform:
                                case PointsModeEnum.GameObjectsTransform:

                                    for (var i = 0; i < pointsGameObjects.Length; i++) Convert(pointsGameObjects[i], oldMode, pointsMode, provider);
                                    break;
                            }

                            if (toRemove != null) pointsGameObjects = new BGCurvePointGO[0];
                        }

                        //------------------------------------------------------- from components OR  from GO
                        //do not remove (it's here for undo/redo)
                        this.pointsMode = pointsMode;
                        if (toRemove != null)
                        {
                            //here we need to destroy Unity's persistent objects we dont use anymore (it should be the last action)
                            var useComponents = oldMode == PointsModeEnum.Components;
                            for (var i = 0; i < toRemove.Length; i++)
                            {
                                var pointComponent = toRemove[i];

                                if (destroyer != null) destroyer(pointComponent);
                                else DestroyIt(useComponents ? (Object) pointComponent : ((BGCurvePointGO) pointComponent).gameObject);
                            }
                        }
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException("pointsMode");
            }

            this.pointsMode = pointsMode;
            FireChange(BGCurveChangedArgs.GetInstance(this, BGCurveChangedArgs.ChangeTypeEnum.Points, EventPointsMode));
        }

        //convert point's mode 
        private static BGCurvePointI Convert(BGCurvePointI point, PointsModeEnum from, PointsModeEnum to, Func<BGCurvePointI> provider)
        {
            BGCurvePointI result;
            switch (from)
            {
                case PointsModeEnum.Inlined:
                    //------------------------------------------ From inline
                    switch (to)
                    {
                        case PointsModeEnum.Components:

                            //---------- To components

                            result = provider == null ? point.Curve.gameObject.AddComponent<BGCurvePointComponent>() : (BGCurvePointComponent) provider();
                            ((BGCurvePointComponent) result).PrivateInit((BGCurvePoint) point);
                            break;
                        case PointsModeEnum.GameObjectsNoTransform:
                        case PointsModeEnum.GameObjectsTransform:

                            //---------- To GO
                            result = ConvertInlineToGo((BGCurvePoint) point, to, provider);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException("to", to, null);
                    }
                    break;
                case PointsModeEnum.Components:
                    //------------------------------------------ From Component
                    switch (to)
                    {
                        case PointsModeEnum.Inlined:

                            //---------- To inline

                            result = ((BGCurvePointComponent) point).Point;
                            break;
                        case PointsModeEnum.GameObjectsNoTransform:
                        case PointsModeEnum.GameObjectsTransform:

                            //---------- To GO
                            result = ConvertInlineToGo(((BGCurvePointComponent) point).Point, to, provider);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException("to", to, null);
                    }
                    break;
                case PointsModeEnum.GameObjectsNoTransform:
                case PointsModeEnum.GameObjectsTransform:
                    //------------------------------------------ From GO

                    switch (to)
                    {
                        case PointsModeEnum.Inlined:

                            //---------- To inline

                            result = ConvertGoToInline((BGCurvePointGO) point, from);
                            break;

                        case PointsModeEnum.Components:

                            //---------- To Component

                            result = provider != null ? (BGCurvePointComponent) provider() : point.Curve.gameObject.AddComponent<BGCurvePointComponent>();
                            ((BGCurvePointComponent) result).PrivateInit(ConvertGoToInline((BGCurvePointGO) point, from));
                            break;
                        case PointsModeEnum.GameObjectsNoTransform:
                        case PointsModeEnum.GameObjectsTransform:
                            ((BGCurvePointGO) point).PrivateInit(null, to);
                            result = point;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException("to", to, null);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException("from", @from, null);
            }

            //point references
            var pointTransform = point.PointTransform;
            if (pointTransform != null)
            {
                BGCurveReferenceToPoint reference = null;

                if (from != PointsModeEnum.Inlined) reference = BGCurveReferenceToPoint.GetReferenceToPoint(point);

                if (to != PointsModeEnum.Inlined)
                {
                    //not inlined
                    if (reference == null) reference = pointTransform.gameObject.AddComponent<BGCurveReferenceToPoint>();
                    reference.Point = result;
                }
                else
                {
                    //inlined
                    if (reference != null) DestroyIt(reference);
                }
            }

            return result;
        }

        //convert point from Inline to GameObject
        private static BGCurvePointGO ConvertInlineToGo(BGCurvePoint point, PointsModeEnum to, Func<BGCurvePointI> provider)
        {
            BGCurvePointGO pointGO;
            if (provider != null) pointGO = (BGCurvePointGO) provider();
            else
            {
                var gameObjectForPoint = new GameObject();
                var pointTransform = gameObjectForPoint.transform;
                pointTransform.parent = point.Curve.transform;
                pointTransform.localRotation = Quaternion.identity;
                pointTransform.localPosition = Vector3.zero;
                pointTransform.localScale = Vector3.one;
                pointGO = gameObjectForPoint.AddComponent<BGCurvePointGO>();
            }

            pointGO.PrivateInit(point, to);

            //transfer fields
            pointGO.PrivateValuesForFields = point.PrivateValuesForFields;

            return pointGO;
        }

        //convert point from GameObject to Inline
        private static BGCurvePoint ConvertGoToInline(BGCurvePointGO pointGO, PointsModeEnum @from)
        {
            BGCurvePoint result;
            switch (@from)
            {
                case PointsModeEnum.GameObjectsNoTransform:
                    result = new BGCurvePoint(pointGO.Curve, pointGO.PointTransform, pointGO.PositionLocal, pointGO.ControlType, pointGO.ControlFirstLocal, pointGO.ControlSecondLocal);
                    break;
                case PointsModeEnum.GameObjectsTransform:
                    var transform = pointGO.PointTransform != null ? pointGO.PointTransform : pointGO.Curve.transform;
                    var control1 = transform.InverseTransformVector(pointGO.ControlFirstLocalTransformed);
                    var control2 = transform.InverseTransformVector(pointGO.ControlSecondLocalTransformed);
                    result = new BGCurvePoint(pointGO.Curve, pointGO.PointTransform, pointGO.PositionLocal, pointGO.ControlType, control1, control2);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("PointsModeEnum");
            }


            //transfer fields
            if (pointGO.Curve.FieldsCount > 0) result.PrivateValuesForFields = pointGO.PrivateValuesForFields;

            return result;
        }


        // ============================================== Events
        //fires a "final" event. By default events are grouped within single frame and one single event is fired at Update or LateUpdate
        private void FireFinalEvent()
        {
            var transformChanged = transform.hasChanged;

            var forceUpdateEveryFrame = forceChangedEventMode == ForceChangedEventModeEnum.EditorAndRuntime;

            if (!transformChanged && immediateChangeEvents && !forceUpdateEveryFrame) return;

            //check additionally for point's changes
            if (pointsMode == PointsModeEnum.GameObjectsTransform)
            {
                var points = (BGCurvePointGO[]) Points;
                var length = points.Length;
                for (var i = 0; i < length; i++)
                {
                    var point = points[i];
                    var pointTransform = point.gameObject.transform;
                    if (!pointTransform.hasChanged) continue;

                    pointTransform.hasChanged = false;
                    changed = true;
                    lastEventType = BGCurveChangedArgs.ChangeTypeEnum.Points;
                    lastEventMessage = EventPointPosition;
                }
            }

            //check points transforms
            if (pointsWithTransforms != null)
            {
                var pointsWithTransformsCount = pointsWithTransforms.Count;
                if (pointsWithTransformsCount > 0)
                {
                    var points = Points;
                    var pointCount = points.Length;
                    for (var i = 0; i < pointsWithTransformsCount; i++)
                    {
                        var index = pointsWithTransforms[i];
                        if (index >= pointCount) continue;

                        var point = points[index];
                        var pointTransform = point.PointTransform;
                        if (pointTransform == null || !pointTransform.hasChanged) continue;

                        pointTransform.hasChanged = false;
                        changed = true;
                        lastEventType = BGCurveChangedArgs.ChangeTypeEnum.Points;
                        lastEventMessage = EventPointPosition;
                    }
                }
            }

            if (!transformChanged && !changed && !forceUpdateEveryFrame) return;

            if (changed) FireChange(UseEventsArgs ? BGCurveChangedArgs.GetInstance(this, lastEventType, lastEventMessage) : null, true);
            else if (transformChanged) FireChange(UseEventsArgs ? BGCurveChangedArgs.GetInstance(this, BGCurveChangedArgs.ChangeTypeEnum.CurveTransform, EventTransform) : null, true);
            else FireChange(UseEventsArgs ? BGCurveChangedArgs.GetInstance(this, BGCurveChangedArgs.ChangeTypeEnum.Curve, EventForcedUpdate) : null, true);

            transform.hasChanged = changed = false;
        }


        // ============================================== Fields

        //add fields to provided points
        private void AddFields(PointsModeEnum pointsMode, BGCurvePointI[] addedPoints)
        {
            foreach (var point in addedPoints)
            {
                var fieldsValues = GetFieldsValues(point, pointsMode);
                foreach (var field in fields) BGCurvePoint.PrivateFieldAdded(field, fieldsValues);
            }
        }

        //add a custom field. this method is not meant to be public, Editor uses it via reflection
        private BGCurvePointField AddField(string name, BGCurvePointField.TypeEnum type, Func<BGCurvePointField> provider = null)
        {
            //check if name is ok
            BGCurvePointField.CheckName(this, name, true);

            FireBeforeChange(EventAddField);

            //at this point the change is certainly will occur
            //add a field
            var field = provider == null ? gameObject.AddComponent<BGCurvePointField>() : provider();
            field.hideFlags = HideFlags.HideInInspector;
            field.Init(this, name, type);

            fields = Insert(fields, fields.Length, field);

            PrivateUpdateFieldsValuesIndexes();

            if (PointsCount > 0)
            {
                //update all points
                var points = Points;
                foreach (var point in points) BGCurvePoint.PrivateFieldAdded(field, GetFieldsValues(point, pointsMode));
            }

            FireChange(BGCurveChangedArgs.GetInstance(this, BGCurveChangedArgs.ChangeTypeEnum.Fields, EventAddField));

            return field;
        }

        // delete a custom field. this method is not meant to be public, Editor uses it via reflection
        private void DeleteField(BGCurvePointField field, Action<BGCurvePointField> destroyer = null)
        {
            //find field index
            var deletedIndex = IndexOf(fields, field);
            if (deletedIndex < 0 || deletedIndex >= fields.Length) throw new UnityException("Unable to remove a fields. Invalid index: " + deletedIndex);

            //get the index for field's value
            var indexOfField = IndexOfFieldValue(field.FieldName);

            FireBeforeChange(EventDeleteField);

            //at this point the change is certainly will occur
            //remove field from list
            fields = Remove(fields, deletedIndex);

            //update fields values indexes. See  FieldsTree commments for more details
            PrivateUpdateFieldsValuesIndexes();

            if (PointsCount > 0)
            {
                //update all points
                var points = Points;
                foreach (var point in points) BGCurvePoint.PrivateFieldDeleted(field, indexOfField, GetFieldsValues(point, pointsMode));
            }

            //destroy field component
            if (destroyer == null) DestroyIt(field);
            else destroyer(field);


            FireChange(BGCurveChangedArgs.GetInstance(this, BGCurveChangedArgs.ChangeTypeEnum.Fields, EventDeleteField));
        }

        // ============================================== Point's transforms
        //find points with Transforms attached
        private void CachePointsWithTransforms()
        {
            if (pointsWithTransforms != null) pointsWithTransforms.Clear();
            var points = Points;
            var count = points.Length;
            for (var i = 0; i < count; i++)
            {
                if (points[i].PointTransform != null)
                {
                    if (pointsWithTransforms == null) pointsWithTransforms = new List<int>();
                    pointsWithTransforms.Add(i);
                }
            }
        }

        // ============================================== Utils
        //destroy Unity object
        public static void DestroyIt(Object obj)
        {
            if (Application.isEditor) DestroyImmediate(obj);
            else Destroy(obj);
        }

        #endregion

        #region Private Classes

        //===============================================================================================
        //                                                    Private classes
        //===============================================================================================

        // stores info about how fields values should be stored.
        // fields values are stored in arrays, so we need to keep track of index within this array for each field 
        private sealed class FieldsTree
        {
            private readonly Dictionary<string, int> fieldName2Index = new Dictionary<string, int>();

            /// <summary>if current index is up-to-date? </summary>
            public bool Comply(BGCurvePointField[] fields)
            {
                return fields == null ? fieldName2Index.Count == 0 : fieldName2Index.Count == fields.Length;
            }

            /// <summary>get index by field's name</summary>
            public int GetIndex(string name)
            {
                int index;
                if (fieldName2Index.TryGetValue(name, out index)) return index;
                throw new UnityException("Can not find a index of field " + name);
            }

            /// <summary>update whole index</summary>
            public void Update(BGCurvePointField[] fields)
            {
                fieldName2Index.Clear();
                int boolIndex = 0,
                    intIndex = 0,
                    floatIndex = 0,
                    vector3Index = 0,
                    boundsIndex = 0,
                    colorIndex = 0,
                    stringIndex = 0,
                    quaternionIndex = 0,
                    animationCurveIndex = 0,
                    gameObjectIndex = 0,
                    componentIndex = 0,
                    bgCurveIndex = 0,
                    bgCurvePointComponentIndex = 0,
                    bgCurvePointGOIndex = 0;

                foreach (var field in fields)
                {
                    int index;
                    switch (field.Type)
                    {
                        case BGCurvePointField.TypeEnum.Bool:
                            index = boolIndex++;
                            break;
                        case BGCurvePointField.TypeEnum.Int:
                            index = intIndex++;
                            break;
                        case BGCurvePointField.TypeEnum.Float:
                            index = floatIndex++;
                            break;
                        case BGCurvePointField.TypeEnum.Vector3:
                            index = vector3Index++;
                            break;
                        case BGCurvePointField.TypeEnum.Bounds:
                            index = boundsIndex++;
                            break;
                        case BGCurvePointField.TypeEnum.Color:
                            index = colorIndex++;
                            break;
                        case BGCurvePointField.TypeEnum.String:
                            index = stringIndex++;
                            break;
                        case BGCurvePointField.TypeEnum.Quaternion:
                            index = quaternionIndex++;
                            break;
                        case BGCurvePointField.TypeEnum.AnimationCurve:
                            index = animationCurveIndex++;
                            break;
                        case BGCurvePointField.TypeEnum.GameObject:
                            index = gameObjectIndex++;
                            break;
                        case BGCurvePointField.TypeEnum.Component:
                            index = componentIndex++;
                            break;
                        case BGCurvePointField.TypeEnum.BGCurve:
                            index = bgCurveIndex++;
                            break;
                        case BGCurvePointField.TypeEnum.BGCurvePointComponent:
                            index = bgCurvePointComponentIndex++;
                            break;
                        case BGCurvePointField.TypeEnum.BGCurvePointGO:
                            index = bgCurvePointGOIndex++;
                            break;
                        default:
                            throw new UnityException("Unknown type " + field.Type);
                    }

                    fieldName2Index[field.FieldName] = index;
                }
            }
        }

        #endregion
    }
}