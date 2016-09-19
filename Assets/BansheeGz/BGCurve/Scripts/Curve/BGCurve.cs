using System;
using System.Collections.Generic;
using UnityEngine;

namespace BansheeGz.BGSpline.Curve
{
    /// <summary>Basic class for curve points data</summary>
    [HelpURL("http://www.bansheegz.com/BGCurve/")]
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [Serializable]
    [AddComponentMenu("BansheeGz/BGCurve/BGCurve", 0)]
    public class BGCurve : MonoBehaviour
    {
        public const float Version = 1.1f;

        public const float Epsilon = 0.00001f;

        #region Fields & Events & Props & enums

#if UNITY_EDITOR
        // ============================================== !!! This is editor ONLY field
#pragma warning disable 0414
        [SerializeField] private BGCurveSettings settings = new BGCurveSettings();
#pragma warning restore 0414
#endif

        public enum Mode2DEnum
        {
            Off,
            XY,
            XZ,
            YZ
        }

        public enum EventModeEnum
        {
            //events are fired once per frame in Update
            Update,
            //events are fired once per frame in LateUpdate
            LateUpdate,
            //events are fired immediately after change
            Immediate,
            //events are suppressed
            NoEvents
        }

        public delegate void IterationCallback(BGCurvePoint point, int index, int count);

        // ======================================= Events

        /// <summary>Any curve's change. By default events are fired once per frame in Update, set ImmediateChangeEvents to fire event as soon as any change</summary>
        public event EventHandler<BGCurveChangedArgs> Changed;

        /// <summary>Before any change.</summary>
        public event EventHandler<BGCurveChangedArgs.BeforeChange> BeforeChange;

        // ======================================= Fields

        [Tooltip("2d Mode for a curve. In 2d mode, only 2 coordinates matter, the third will always be 0 (including controls). Handles in Editor will also be switched to 2d mode")] [SerializeField] private Mode2DEnum mode2D = Mode2DEnum.Off;

        //if curve is closed (e.g. if last and first point are connected)
        [Tooltip("If curve is closed")] [SerializeField] private bool closed;


        //actual points
        [SerializeField] private BGCurvePoint[] points = new BGCurvePoint[0];


        //for batch operation (to avoid event firing for every operation)
        private int transactionLevel;
        private List<BGCurveChangedArgs> changeList;

        // ======================================= Props
        /// <summary>Curve's points</summary>
        public BGCurvePoint[] Points
        {
            get { return points; }
        }

        /// <summary>Number of curve's points</summary>
        public int PointsCount
        {
            get { return points.Length; }
        }


        /// <summary>If curve is closed, e.g. last and first points are connected</summary>
        public bool Closed
        {
            get { return closed; }
            set
            {
                if (value == closed) return;
                FireBeforeChange("closed is changed");
                closed = value;
                FireChange(UseEventsArgs ? new BGCurveChangedArgs(this, BGCurveChangedArgs.ChangeTypeEnum.Points) : null);
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

        private List<BGCurveChangedArgs> ChangeList
        {
            get { return changeList ?? (changeList = new List<BGCurveChangedArgs>()); }
        }

        //------------------------------ these fields are not persistent

        [Obsolete("It is not used anymore and should be removed")]
        public bool TraceChanges
        {
            get { return Changed != null; }
            set { }
        }


        //changed flag is reset every frame. 
        private bool changed;
        private EventModeEnum eventMode = EventModeEnum.Update;
        private EventModeEnum eventModeOld = EventModeEnum.Update;

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

        public EventModeEnum EventMode
        {
            get { return eventMode; }
            set { eventMode = value; }
        }

        #endregion

        #region Public Functions

        // ============================================== Public functions (only required ones- no math. to use math use BGCurveBaseMath or your own extension with BGCurveBaseMath as an example)

        /// <summary>Curve's point creation</summary>
        public BGCurvePoint CreatePointFromWorldPosition(Vector3 worldPos, BGCurvePoint.ControlTypeEnum controlType)
        {
            return new BGCurvePoint(this, transform.InverseTransformPoint(worldPos), controlType);
        }

        /// <summary>Curve's point creation</summary>
        public BGCurvePoint CreatePointFromWorldPosition(Vector3 worldPos, BGCurvePoint.ControlTypeEnum controlType, Vector3 control1WorldPos, Vector3 control2WorldPos)
        {
            return new BGCurvePoint(this,
                transform.InverseTransformPoint(worldPos), controlType,
                transform.InverseTransformDirection(control1WorldPos - worldPos),
                transform.InverseTransformDirection(control2WorldPos - worldPos));
        }

        /// <summary>Curve's point creation</summary>
        public BGCurvePoint CreatePointFromLocalPosition(Vector3 localPos, BGCurvePoint.ControlTypeEnum controlType)
        {
            return new BGCurvePoint(this, localPos, controlType);
        }

        /// <summary>Curve's point creation</summary>
        public BGCurvePoint CreatePointFromLocalPosition(Vector3 localPos, BGCurvePoint.ControlTypeEnum controlType, Vector3 control1LocalPos, Vector3 control2LocalPos)
        {
            return new BGCurvePoint(this, localPos, controlType, control1LocalPos, control2LocalPos);
        }


        //----------------- points handling
        /// <summary>Remove all points</summary>
        public void Clear()
        {
            FireBeforeChange("clear all points");
            points = new BGCurvePoint[0];
            FireChange(UseEventsArgs ? new BGCurveChangedArgs(this, BGCurveChangedArgs.ChangeTypeEnum.Points) : null);
        }

        /// <summary>Returns a point's index</summary>
        public int IndexOf(BGCurvePoint point)
        {
            return IndexOf(points, point);
        }


        /// <summary>Add a point to the end</summary>
        public void AddPoint(BGCurvePoint point)
        {
            AddPoint(point, points.Length);
        }

        /// <summary>Add a point at specified index</summary>
        public void AddPoint(BGCurvePoint point, int index)
        {
            if (index < 0 || index > points.Length)
            {
                print("Unable to add a point. Invalid index: " + index);
                return;
            }
            FireBeforeChange("insert a point");

            points = Insert(points, index, point);


            FireChange(UseEventsArgs ? new BGCurveChangedArgs(this, BGCurveChangedArgs.ChangeTypeEnum.Points) : null);
        }

        /// <summary>Add several points. </summary>
        public void AddPoints(BGCurvePoint[] points)
        {
            AddPoints(points, this.points.Length);
        }

        /// <summary>Add several points at specified index.</summary>
        public void AddPoints(BGCurvePoint[] points, int index)
        {
            if (points == null || points.Length == 0) return;
            if (index < 0 || index > this.points.Length)
            {
                print("Unable to add points. Invalid index: " + index);
                return;
            }
            FireBeforeChange("insert points");

            this.points = Insert(this.points, index, points);

            FireChange(UseEventsArgs ? new BGCurveChangedArgs(this, BGCurveChangedArgs.ChangeTypeEnum.Points) : null);
        }

        /// <summary>Removes a point</summary>
        public void Delete(BGCurvePoint point)
        {
            Delete(IndexOf(point));
        }

        /// <summary>Removes a point at specified index</summary>
        public void Delete(int index)
        {
            if (index < 0 || index >= points.Length)
            {
                print("Unable to remove a point. Invalid index: " + index);
                return;
            }

            FireBeforeChange("delete a point");

            points = Remove(points, index);

            FireChange(UseEventsArgs ? new BGCurveChangedArgs(this, BGCurveChangedArgs.ChangeTypeEnum.Points) : null);
        }

        /// <summary>Swaps 2 points</summary>
        public void Swap(int index1, int index2)
        {
            if (index1 < 0 || index1 >= points.Length || index2 < 0 || index2 >= points.Length)
            {
                print("Unable to remove a point. Invalid indexes: " + index1 + ", " + index2);
                return;
            }
            FireBeforeChange("swap points");

            var point = points[index2];
            points[index2] = points[index1];
            points[index1] = point;

            FireChange(UseEventsArgs ? new BGCurveChangedArgs(this, BGCurveChangedArgs.ChangeTypeEnum.Points) : null);
        }

        /// <summary>reverse points, keeping curve intact</summary>
        public void Reverse()
        {
            var pointsCount = PointsCount;

            if (pointsCount < 2) return;
            FireBeforeChange("reverse points");

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

                point2.PositionLocal = point1.PositionLocal;
                point2.ControlType = point1.ControlType;
                point2.ControlFirstLocal = point1.ControlSecondLocal;
                point2.ControlSecondLocal = point1.ControlFirstLocal;

                point1.PositionLocal = position;
                point1.ControlType = controlType;
                point1.ControlFirstLocal = control2;
                point1.ControlSecondLocal = control1;
            }

            if (pointsCount%2 != 0)
            {
                var point = points[mid];
                var control = point.ControlFirstLocal;
                point.ControlFirstLocal = point.ControlSecondLocal;
                point.ControlSecondLocal = control;
            }

            FireChange(UseEventsArgs ? new BGCurveChangedArgs(this, BGCurveChangedArgs.ChangeTypeEnum.Points) : null);
        }

        //------------------------------ 2D mode. 
        public void Apply2D(Mode2DEnum value)
        {
            FireBeforeChange("2d mode changed");
            mode2D = value;

            if (mode2D != Mode2DEnum.Off && PointsCount > 0)
            {
                Transaction(() => { foreach (var point in points) Apply2D(point); });
            }
            else
            {
                FireChange(UseEventsArgs ? new BGCurveChangedArgs(this, BGCurveChangedArgs.ChangeTypeEnum.Points) : null);
            }
        }

        /// <summary>change point's coordinate to comply to 2d mode</summary>
        public virtual void Apply2D(BGCurvePoint point)
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


        //------------------------------ batch handling (to avoid event firing for every operation). 
        /// <summary>
        /// Executes a batch operation.
        /// During batch operation events firing will be suppressed, and event will be fired only after TransactionCommit is invoked.
        /// it is for event handling only .
        /// </summary>
        public void Transaction(Action action)
        {
            FireBeforeChange("Changes in transaction");
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
                    FireChange(UseEventsArgs ? new BGCurveChangedArgs(this, ChangeList.ToArray()) : null);
                    if (UseEventsArgs) ChangeList.Clear();
                }
            }
        }

        public int TransactionLevel
        {
            get { return transactionLevel; }
        }


        protected internal static int IndexOf<T>(T[] array, T item)
        {
            return Array.IndexOf(array, item);
        }


        //------------------------------ event handling
        public void FireBeforeChange(string operation)
        {
            if (eventMode==EventModeEnum.NoEvents || transactionLevel > 0 || BeforeChange == null) return;

            BeforeChange(this, UseEventsArgs ? new BGCurveChangedArgs.BeforeChange(operation) : null);
        }

        /// <summary> Fires Changed event if some conditions are met </summary>
        public void FireChange(BGCurveChangedArgs change, bool ignoreEventsGrouping = false)
        {
            if (eventMode == EventModeEnum.NoEvents || Changed == null) return;

            if (transactionLevel > 0 || (eventMode != EventModeEnum.Immediate && !ignoreEventsGrouping))
            {
                changed = true;
                if (UseEventsArgs && !ChangeList.Contains(change)) ChangeList.Add(change);
                return;
            }

            Changed(this, UseEventsArgs ? change : null);
        }

        //------------------------------ Unity callbacks

        protected virtual void Update()
        {
            // is transform.hasChanged is true by default?
            if (Time.frameCount == 1) transform.hasChanged = false;
            if (eventMode != EventModeEnum.Update || Changed == null) return;
            FireFinalEvent();
        }

        protected virtual void LateUpdate()
        {
            if (eventMode != EventModeEnum.LateUpdate || Changed == null) return;
            FireFinalEvent();
        }

        //------------------------------ Utility methods
        /// <summary> world pos to local </summary>
        public Vector3 ToLocal(Vector3 worldPoint)
        {
            return transform.InverseTransformPoint(worldPoint);
        }

        /// <summary> local pos to world </summary>
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
            for (var i = 0; i < PointsCount; i++) iterationCallback(points[i], i, PointsCount);
        }

        public BGCurvePoint this[int i]
        {
            get { return points[i]; }
            set { points[i] = value; }
        }


        public override string ToString()
        {
            return "BGCurve [id=" + GetInstanceID() + "], points=" + PointsCount;
        }

        #endregion

        #region Private Functions

        // ============================================== private functions
        private void FireFinalEvent()
        {
            var transformChanged = transform.hasChanged;

            if (!transformChanged && eventMode == EventModeEnum.Immediate) return;

            if (!transformChanged && !changed) return;

            //one final event at the end of the frame if MultipleEventsPerFrame=false.
            FireChange(UseEventsArgs ? new BGCurveChangedArgs(this, BGCurveChangedArgs.ChangeTypeEnum.CurveTransform) : null, true);
            transform.hasChanged = changed = false;
        }


        // copy arrays and add a new element
        protected internal static T[] Insert<T>(T[] oldArray, int index, T[] newElements)
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
        protected internal static T[] Insert<T>(T[] oldArray, int index, T newElement)
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
        protected internal static T[] Remove<T>(T[] oldArray, int index)
        {
            var newArray = new T[oldArray.Length - 1];
            if (index > 0) Array.Copy(oldArray, newArray, index);

            if (index < oldArray.Length - 1) Array.Copy(oldArray, index + 1, newArray, index, oldArray.Length - 1 - index);

            return newArray;
        }

        #endregion
    }
}