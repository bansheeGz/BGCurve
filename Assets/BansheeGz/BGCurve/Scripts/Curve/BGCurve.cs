using System;
using System.Collections.Generic;
using BansheeGz.BGSpline.EditorHelpers;
using UnityEngine;

namespace BansheeGz.BGSpline.Curve
{
    /// <summary>Basic class for curve points data</summary>
    [HelpURL("http://www.bansheegz.com/BGCurve/")]
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [Serializable]
    public class BGCurve : MonoBehaviour
    {
        public enum Mode2DEnum
        {
            Off,
            XY,
            XZ,
            YZ
        }

        public delegate void IterationCallback(BGCurvePoint point, int index, int count);

        #region Fields & Events & Props

        // ======================================= Events

        /// <summary>Any curve's change. This will be fired only if TraceChanges is set to true</summary>
        public event EventHandler<BGCurveChangedArgs> Changed;

        /// <summary>Before any change. This will be fired only if TraceChanges is set to true</summary>
        public event EventHandler BeforeChange;

        // ======================================= Fields

        [Tooltip("2d Mode for a curve. In 2d mode, only 2 coordinates matter, the third will always be 0 (including controls). Handles in Editor will also be switched to 2d mode")] 
        [SerializeField] private Mode2DEnum mode2D = Mode2DEnum.Off;

        //id curve is closed (e.g. if last and first point are connected)
        [Tooltip("If curve is closed")] [SerializeField] private bool closed;


        //actual points
        [SerializeField] private BGCurvePoint[] points = new BGCurvePoint[0];


        //for batch operation (to avoid event firing for every operation)
        private bool isInTransaction;
        private List<BGCurveChangedArgs> changeList;

        //set it to true to trace curve changes (pos, rot, scale and any points changes )
        //!!! keep in mind it will reset Transform.hasChanged variable!
        private bool traceChanges;

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
                FireChange(new BGCurveChangedArgs(this, BGCurveChangedArgs.ChangeTypeEnum.Points));
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

        public void Apply2D(Mode2DEnum value)
        {
            FireBeforeChange("2d mode changed");
            mode2D = value;

            if (mode2D != Mode2DEnum.Off && PointsCount > 0)
            {
                Transaction(() =>
                {
                    foreach (var point in points)
                    {
                        Apply2D(point);
                    }
                });
            }
            else
            {
                FireChange(new BGCurveChangedArgs(this, BGCurveChangedArgs.ChangeTypeEnum.Points));
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
                case Mode2DEnum.XY: return new Vector3(point.x,point.y, 0);
                case Mode2DEnum.XZ: return new Vector3(point.x, 0, point.z);
                case Mode2DEnum.YZ: return new Vector3(0, point.y, point.z);
            }
            return point;
        }

        /// <summary>
        /// Should the curve detect its own changes, including position, rotation, scale or any points changes and fire Changed event if any change happens. 
        /// This will add some overhead, so if you are sure, the curve will not change, do not use it.
        /// Also keep in mind it will be resetting Transform.hasChanged variable.
        /// </summary>
        public bool TraceChanges
        {
            get { return traceChanges; }
            set { traceChanges = value; }
        }

        public bool SupressEvents { get; set; }

        #endregion

        #region Unity Editor 

        // ============================================== !!! This region is supposed to work in editor ONLY

#if UNITY_EDITOR
        private BGCurvePainterGizmo painter;
        [SerializeField] private BGCurveSettings settings = new BGCurveSettings();


        private void OnDrawGizmos()
        {
            if (!settings.ShowEvenNotSelected || UnityEditor.Selection.Contains(gameObject)) return;

            OnDrawGizmosSelected();
        }

        private void OnDrawGizmosSelected()
        {
            if (points.Length < 2 || !settings.ShowCurve || settings.VRay) return;
            Draw();
        }

        private void Draw()
        {
            if (painter == null)
            {
                painter = GetPainter();
            }

            painter.DrawCurve();
        }

        protected virtual BGCurvePainterGizmo GetPainter()
        {
            return new BGCurvePainterGizmo(this);
        }
#endif

        #endregion

        // ============================================== Public functions (only required ones- no math. to use math use BGCurveBaseMath or your own extension with BGCurveBaseMath as an example)

        #region Public Functions

        /// <summary>Curve's point creation</summary>
        public BGCurvePoint CreatePointFromWorldPosition(Vector3 worldPos, BGCurvePoint.ControlTypeEnum controlType)
        {
            return new BGCurvePoint(this, ToLocal(ref worldPos), controlType);
        }

        /// <summary>Curve's point creation</summary>
        public BGCurvePoint CreatePointFromWorldPosition(Vector3 worldPos, BGCurvePoint.ControlTypeEnum controlType, Vector3 control1WorldPos, Vector3 control2WorldPos)
        {
            return new BGCurvePoint(this, ToLocal(ref worldPos), controlType, control1WorldPos - worldPos, control2WorldPos - worldPos);
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
            FireChange(new BGCurveChangedArgs(this, BGCurveChangedArgs.ChangeTypeEnum.Points));
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

            FireChange(new BGCurveChangedArgs(this, BGCurveChangedArgs.ChangeTypeEnum.Points));
        }


        /// <summary>Removes a point</summary>
        public void Delete(BGCurvePoint point)
        {
            Delete(IndexOf(point));
        }

        /// <summary>Returns a point's index</summary>
        public int IndexOf(BGCurvePoint point)
        {
            return Array.IndexOf(points, point);
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

            FireChange(new BGCurveChangedArgs(this, BGCurveChangedArgs.ChangeTypeEnum.Points));
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

            FireChange(new BGCurveChangedArgs(this, BGCurveChangedArgs.ChangeTypeEnum.Points));
        }


        //------------------------------ batch handling (to avoid event firing for every operation). 
        /// <summary>
        /// Executes a batch operation.
        /// During batch operation events firing will be suppressed, and event will be fired only after TransactionCommit is invoked.
        /// it is for event handling ONLY.
        /// </summary>
        public void Transaction(Action action)
        {
            TransactionStart();
            try
            {
                action();
            }
            finally
            {
                TransactionCommit();
            }
        }

        /// <summary>
        /// Starts a batch operation. TransactionCommit should be invoked to finish a transaction and fire an Changed event (if any)
        /// During batch operation events firing will be suppressed, and event will be fired only after TransactionCommit is invoked.
        /// it is for event handling ONLY.
        /// </summary>
        public void TransactionStart()
        {
            isInTransaction = true;
            changeList = new List<BGCurveChangedArgs>();
        }

        /// <summary>
        /// Ends a batch operation. TransactionStart should be called prior to this method.
        /// During batch operation events firing will be suppressed, and event will be fired only after TransactionCommit is invoked.
        /// it is for event handling ONLY.
        /// </summary>
        public void TransactionCommit()
        {
            isInTransaction = false;
            if (changeList == null || changeList.Count == 0) return;

            FireChange(new BGCurveChangedArgs(this, changeList.ToArray()));
            changeList = null;
        }

        //------------------------------ event handling
        public void FireBeforeChange(string operation)
        {
            if (SupressEvents) return;

            if (isInTransaction) return;

#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(this, operation);
#endif
            if (traceChanges && BeforeChange != null)
            {
                BeforeChange(this, null);
            }
        }

        /// <summary>
        /// If traceChanges is set to true, it fires Changed event, or mark the event should be fired later if it's currently in transaction
        /// </summary>
        public void FireChange(BGCurveChangedArgs change)
        {
            if (SupressEvents) return;

            if (!traceChanges || Changed == null) return;

            if (isInTransaction)
            {
                if (!changeList.Contains(change))
                {
                    changeList.Add(change);
                }
                return;
            }

            Changed(this, change);
        }

        /// <summary>
        /// Unity callback
        /// </summary>
        protected virtual void Update()
        {
            if (!traceChanges) return;

            if (transform.hasChanged)
            {
                FireChange(new BGCurveChangedArgs(this, BGCurveChangedArgs.ChangeTypeEnum.CurveTransform));
                transform.hasChanged = false;
            }
        }
        /// <summary>
        /// world pos to local
        /// </summary>
        public Vector3 ToLocal(ref Vector3 worldPoint)
        {
            return transform.InverseTransformPoint(worldPoint);
        }

        /// <summary>
        /// local pos to world
        /// </summary>
        public Vector3 ToWorld(ref Vector3 localPoint)
        {
            return transform.TransformPoint(localPoint);
        }

        /// <summary>
        /// execute Action for each point. Params are Point,index,length.
        /// </summary>
        public void ForEach(IterationCallback iterationCallback)
        {
            var length = Points.Length;
            for (var i = 0; i < length; i++)
            {
                iterationCallback(Points[i], i, length);
            }
        }

        #endregion

        // ============================================== private functions

        #region Private Functions

        // copy arrays and add a new element
        protected static T[] Insert<T>(T[] oldArray, int index, T newElement)
        {
            T[] newArray = new T[oldArray.Length + 1];
            if (index > 0)
            {
                //copy before index
                Array.Copy(oldArray, newArray, index);
            }
            if (index < oldArray.Length)
            {
                //copy after index
                Array.Copy(oldArray, index, newArray, index + 1, oldArray.Length - index);
            }

            newArray[index] = newElement;

            return newArray;
        }

        // copy arrays and removes an element
        protected static T[] Remove<T>(T[] oldArray, int index)
        {
            var newArray = new T[oldArray.Length - 1];
            if (index > 0)
            {
                Array.Copy(oldArray, newArray, index);
            }

            if (index < oldArray.Length - 1)
            {
                Array.Copy(oldArray, index + 1, newArray, index, oldArray.Length - 1 - index);
            }
            return newArray;
        }

        #endregion
    }
}