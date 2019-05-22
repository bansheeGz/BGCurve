using System;
using System.Collections.Generic;
using System.Linq;
using BansheeGz.BGSpline.Curve;
using UnityEditor;
using UnityEngine;

namespace BansheeGz.BGSpline.Editor
{
    //helper class for points selection inside editor
    public class BGCurveEditorPointsSelection
    {
        private static readonly Color SelectedBackgroundColor = new Color32(157, 246, 246, 40);

        private readonly List<BGCurvePointI> points = new List<BGCurvePointI>();

        private bool groupSelectionStarted;
        private bool groupSelectionIsSelecting;

        private BGCurveSettings settings;

        public bool Changed { get; private set; }

        public int CountSelected
        {
            get { return points.Count; }
        }

        private readonly BGCurve curve;
        private readonly BGRectangularSelection selectionRectangle;

        private BGCurvePoint.ControlTypeEnum controlType = BGCurvePoint.ControlTypeEnum.Absent;

        private BGEditorUtility.EventCanceller eventCanceller;

        //do not want to use events
        private int lastCurveCount;
        private readonly PointsContainer pointsContainer;

        public BGCurveEditorPointsSelection(BGCurve curve, BGCurveEditor editor)
        {
            this.curve = curve;
            selectionRectangle = new BGRectangularSelection(editor, this);

            pointsContainer = new PointsContainer(this);
        }

        public bool HasSelected()
        {
            return CountSelected > 0;
        }

        internal void Clear()
        {
            Changed = Changed || HasSelected();
            points.Clear();
        }

        public bool Contains(BGCurvePointI point)
        {
            return points.Contains(point);
        }

        //if we selecting or removing selection
        public void GroupSelection(BGCurvePointI point)
        {
            if (groupSelectionIsSelecting) Add(point);
            else Remove(point);
        }

        public bool Add(BGCurvePointI point)
        {
            if (Contains(point)) return false;

            Changed = true;
            points.Add(point);
            return true;
        }

        public bool Remove(BGCurvePointI point)
        {
            if (!Contains(point)) return false;
            Changed = true;
            points.Remove(point);
            return true;
        }

        private void SetX(float x)
        {
            foreach (var point in points)
            {
                var positionWorld = point.PositionWorld;
                point.PositionWorld = new Vector3(x, positionWorld.y, positionWorld.z);
            }
        }

        private void SetY(float y)
        {
            foreach (var point in points)
            {
                var positionWorld = point.PositionWorld;
                point.PositionWorld = new Vector3(positionWorld.x, y, positionWorld.z);
            }
        }

        private void SetZ(float z)
        {
            foreach (var point in points)
            {
                var positionWorld = point.PositionWorld;
                point.PositionWorld = new Vector3(positionWorld.x, positionWorld.y, z);
            }
        }

        public void Reset()
        {
            Changed = false;
            settings = BGPrivateField.GetSettings(curve);
        }

        //draw interactible selection icon control
        public void InspectorSelectionRect(BGCurvePointI point)
        {
            var currentEvent = Event.current;
            var rect = GUILayoutUtility.GetRect(24, 24, 24, 24, new GUIStyle {fixedWidth = 24, fixedHeight = 24, stretchWidth = false, stretchHeight = false});
            if (currentEvent.isMouse)
            {
                if (currentEvent.type == EventType.MouseDown)
                {
                    if (rect.Contains(currentEvent.mousePosition))
                    {
                        groupSelectionStarted = true;
                        groupSelectionIsSelecting = !Contains(point);
                        GroupSelection(point);
                    }
                }
                else if (groupSelectionStarted)
                {
                    switch (currentEvent.type)
                    {
                        case EventType.MouseUp:
                            groupSelectionStarted = false;
                            break;
                        case EventType.MouseDrag:
                            if (rect.Contains(currentEvent.mousePosition)) GroupSelection(point);
                            break;
                    }
                }
            }

            var selected = Contains(point);
            var selectedTexture = selected ? BGBinaryResources.BGTickYes123: BGBinaryResources.BGTickNo123;
            var labelStyle = selected ? new GUIStyle("Label") {normal = new GUIStyleState {textColor = settings.LabelColorSelected}} : EditorStyles.label;
            EditorGUI.LabelField(rect, new GUIContent(selectedTexture, "Click to (de)select a point, or click and drag to (de)select multiple points. " +
                                                                       "Hold shift+drag to use rectangular selection"), labelStyle);
        }

        //OnInspectorGui for selection
        public void InspectorSelectionOperations()
        {
            BGEditorUtility.VerticalBox(() =>
            {
                // ================================================ Global operations
                BGEditorUtility.HorizontalBox(() =>
                {
                    BGEditorUtility.SwapLabelWidth(80, () => EditorGUILayout.LabelField("Selected (" + points.Count + ")"));

                    if (BGEditorUtility.ButtonWithIcon(BGBinaryResources.BGDelete123, "Delete selected points")) if (!DeleteSelected()) return;

                    GUILayout.Space(4);
                    if (BGEditorUtility.ButtonWithIcon(BGBinaryResources.BGSelectAll123, "Select all points", 35))
                    {
                        Changed = Changed || points.Count != curve.PointsCount;

                        points.Clear();

                        foreach (var point1 in curve.Points) points.Add(point1);
                    }

                    GUILayout.Space(4);

                    if (BGEditorUtility.ButtonWithIcon(BGBinaryResources.BGDeSelectAll123, "Deselect all points", 35)) Clear();
                });


                // ================================================ Selections operations
                // skip mouse buttons events which change selection
                if (Changed) return;

                GUILayout.Space(5);
                if (HasSelected())
                {
                    BGEditorUtility.SwapGuiBackgroundColor(SelectedBackgroundColor, () =>
                    {
                        BGEditorUtility.VerticalBox(() =>
                        {
                            var averagePositionSelected = GetAveragePosition();

                            // =====================================================  Control handles
                            BGEditorUtility.Horizontal(() =>
                            {
                                controlType = (BGCurvePoint.ControlTypeEnum) EditorGUILayout.EnumPopup("Controls", controlType);
                                if (!BGEditorUtility.ButtonWithIcon(BGBinaryResources.BGConvertAll123, "Set control type for all selected points", 44)) return;

                                SetControlTypeForSelected(controlType);
                            });

                            // =====================================================  Average positions & delete
                            BGEditorUtility.Vector3Field("Average position", "Average points position. Change several points positions at once, keeping distance difference intact",
                                averagePositionSelected,
                                newAverage =>
                                {
                                    var delta = newAverage - averagePositionSelected;
                                    curve.Transaction(() =>
                                    {
                                        foreach (var point in points) point.PositionWorld += delta;
                                    });
                                });
                            // =====================================================  Set position directly
                            BGEditorUtility.Vector3Field("Set position", "Set points position directly",
                                averagePositionSelected,
                                newPosition =>
                                {
                                    curve.Transaction(() =>
                                    {
                                        if (BGEditorUtility.AnyChange(averagePositionSelected.x, newPosition.x)) SetX(newPosition.x);
                                        if (BGEditorUtility.AnyChange(averagePositionSelected.y, newPosition.y)) SetY(newPosition.y);
                                        if (BGEditorUtility.AnyChange(averagePositionSelected.z, newPosition.z)) SetZ(newPosition.z);
                                    });
                                });

                            // =====================================================  Set control positions directly
                            var count = 0;
                            var averageControl1Sum = Vector3.zero;
                            var averageControl2Sum = Vector3.zero;
                            foreach (var point in points.Where(point => point.ControlType != BGCurvePoint.ControlTypeEnum.Absent))
                            {
                                count++;
                                averageControl1Sum += point.ControlFirstLocal;
                                averageControl2Sum += point.ControlSecondLocal;
                            }

                            if (count != 0)
                            {
                                //has points with bezier controls
                                BGEditorUtility.Vector3Field("Set Control 1", "Set 1st control position directly",
                                    averageControl1Sum/count,
                                    newPosition =>
                                    {
                                        curve.Transaction(
                                            () =>
                                            {
                                                foreach (var point in points.Where(point => point.ControlType != BGCurvePoint.ControlTypeEnum.Absent)) point.ControlFirstLocal = newPosition;
                                            });
                                    });

                                BGEditorUtility.Vector3Field("Set Control 2", "Set 2nd control position directly",
                                    averageControl2Sum/count,
                                    newPosition =>
                                    {
                                        curve.Transaction(
                                            () =>
                                            {
                                                foreach (var point in points.Where(point => point.ControlType != BGCurvePoint.ControlTypeEnum.Absent)) point.ControlSecondLocal = newPosition;
                                            });
                                    });
                            }


                            // =====================================================  Custom fields
                            if (curve.FieldsCount > 0)
                            {
                                var fields = curve.Fields;
                                pointsContainer.UpdateFields();
                                foreach (var field in fields) BGCurveEditorPoint.ShowField(pointsContainer, field, pointsContainer.AnimationCurveChanged);
                            }
                        });
                    });
                }
                else BGEditorUtility.HelpBox("Hold shift to use rectangular selection\r\nClick or click+drag over tick icons to (de)select points", MessageType.Info, curve.PointsCount > 0);
            });
        }

        public void SetControlTypeForSelected(BGCurvePoint.ControlTypeEnum controlType)
        {
            curve.Transaction(() =>
            {
                foreach (var point in points) point.ControlType = controlType;
            });
        }

        public bool DeleteSelected()
        {
            if (points.Count == 0)
            {
                BGEditorUtility.Inform("Error", "Chose at least one point to delete");
                return false;
            }

            if (!BGEditorUtility.Confirm("Delete points confirmation", "Are you sure you want to remove " + points.Count + " point(s)?", "Delete")) return false;

            BGCurveEditor.DeletePoints(curve, points.ToArray());

            Clear();
            return true;
        }

        public Vector3 GetAveragePosition()
        {
            return points.Aggregate(Vector3.zero, (current, point) => current + point.PositionWorld)/points.Count;
        }

        // OnSceneGui
        public void Scene(Quaternion rotation)
        {
            if (lastCurveCount != curve.PointsCount)
            {
                lastCurveCount = curve.PointsCount;
                OnUndoRedo();
            }

            if (!HasSelected()) return;

            BGEditorUtility.Assign(ref settings, () => BGPrivateField.GetSettings(curve));

            //group operation for selected points
            var text = "     Selected [" + points.Count + "]";
            var average = GetAveragePosition();
            Handles.Label(average + BGEditorUtility.GetHandleSize(average)*Vector3.up*.25f, text, new GUIStyle("Label") {normal = new GUIStyleState {textColor = settings.LabelColorSelected}});

            var newAverage = BGEditorUtility.Handle(-10, settings.HandlesType, average, rotation, settings.HandlesSettings);

            if (BGEditorUtility.AnyChange(average, newAverage))
            {
                curve.Transaction(() =>
                {
                    var delta = newAverage - average;
                    foreach (var selectedPoint in points) selectedPoint.PositionWorld += delta;
                });
            }
        }

        public bool SingleSelected(BGCurvePointI point)
        {
            return points.Count == 1 && Contains(point);
        }

        public void Process(Event currentEvent)
        {
            if (currentEvent.type == EventType.MouseDown)
            {
                if (currentEvent.button == 0)
                {
                    if ((!currentEvent.control && currentEvent.shift) || BGCurveSettingsForEditor.LockView)
                    {
                        var cancelEvent = Tools.viewTool != ViewTool.FPS && Tools.current != Tool.View /*&& Tools.current != Tool.None*/ && !currentEvent.isKey;
                        if (cancelEvent) eventCanceller = new BGEditorUtility.EventCanceller();

                        if (currentEvent.shift && !BGCurveSettingsForEditor.I.Get<bool>(BGCurveSettingsForEditor.DisableRectangularSelectionKey)) selectionRectangle.On();
                        else if (cancelEvent) BGCurveEditor.OverlayMessage.Display("The Scene view is locked.\r\n Set 'Lock View' (in the BGCurve Editor) to false to unlock.");
                    }
                }
                else if (currentEvent.button == 1 && selectionRectangle.IsOn)
                {
                    //glitch with right click
                    GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
                }
            }
            else if (currentEvent.type == EventType.MouseUp)
            {
                SceneView.RepaintAll();
                selectionRectangle.Off();
                BGEditorUtility.Release(ref eventCanceller);
            }


            selectionRectangle.Process(currentEvent);
        }

        public void OnUndoRedo()
        {
            if (CountSelected == 0) return;

            for (var i = points.Count - 1; i >= 0; i--) if (curve.IndexOf(points[i]) < 0) points.RemoveAt(i);
        }

        public void ForEach(Func<BGCurvePointI, bool> action)
        {
            if (CountSelected == 0) return;

            //not sure if it can be converted to LINQ
            foreach (var point in points) if (action(point)) return;
        }

        private sealed class PointsContainer : BGCurvePointI
        {
            private readonly Dictionary<string, object> name2Value = new Dictionary<string, object>();
            private readonly BGCurveEditorPointsSelection selection;
            //we reuse the list to reduce GC
            private readonly List<string> existingKeys = new List<string>();

            public BGCurve Curve { get; private set; }
            public Vector3 PositionLocal { get; set; }
            public Vector3 PositionLocalTransformed { get; set; }
            public Vector3 PositionWorld { get; set; }
            public Vector3 ControlFirstLocal { get; set; }
            public Vector3 ControlFirstLocalTransformed { get; set; }
            public Vector3 ControlFirstWorld { get; set; }
            public Vector3 ControlSecondLocal { get; set; }
            public Vector3 ControlSecondLocalTransformed { get; set; }
            public Vector3 ControlSecondWorld { get; set; }
            public BGCurvePoint.ControlTypeEnum ControlType { get; set; }
            public Transform PointTransform { get; set; }

            public PointsContainer(BGCurveEditorPointsSelection selection)
            {
                this.selection = selection;
                Curve = selection.curve;

//                var animationCurve = new AnimationCurve();
            }

            public T GetField<T>(string name)
            {
                var type = typeof(T);
                var value = GetField(name, type);
                var field = (T) value;
                return field;
            }

            public void SetField<T>(string name, T value)
            {
                SetField(name, value, typeof(T));
            }


            public object GetField(string name, Type type)
            {
                return name2Value[name];
            }

            public void SetField(string name, object value, Type type)
            {
                name2Value[name] = value;

                selection.ForEach(p =>
                {
                    p.SetField(name, value, type);
                    return false;
                });
            }

            public float GetFloat(string name)
            {
                throw new NotImplementedException();
            }

            public bool GetBool(string name)
            {
                throw new NotImplementedException();
            }

            public int GetInt(string name)
            {
                throw new NotImplementedException();
            }

            public Vector3 GetVector3(string name)
            {
                throw new NotImplementedException();
            }

            public Bounds GetBounds(string name)
            {
                throw new NotImplementedException();
            }

            public Quaternion GetQuaternion(string name)
            {
                throw new NotImplementedException();
            }

            public Color GetColor(string name)
            {
                throw new NotImplementedException();
            }

            public void SetFloat(string name, float value)
            {
                throw new NotImplementedException();
            }

            public void SetBool(string name, bool value)
            {
                throw new NotImplementedException();
            }

            public void SetInt(string name, int value)
            {
                throw new NotImplementedException();
            }

            public void SetVector3(string name, Vector3 value)
            {
                throw new NotImplementedException();
            }

            public void SetQuaternion(string name, Quaternion value)
            {
                throw new NotImplementedException();
            }

            public void SetBounds(string name, Bounds value)
            {
                throw new NotImplementedException();
            }

            public void SetColor(string name, Color value)
            {
                throw new NotImplementedException();
            }

            public void AnimationCurveChanged(string name, AnimationCurve animationCurve)
            {
                selection.ForEach(p =>
                {
                    var pointCurve = p.GetField<AnimationCurve>(name);
                    pointCurve.keys = animationCurve.keys;
                    pointCurve.postWrapMode = animationCurve.postWrapMode;
                    pointCurve.preWrapMode = animationCurve.preWrapMode;
                    return false;
                });
            }

            // make sure all fields are present
            public void UpdateFields()
            {
                var fields = Curve.Fields;

                existingKeys.Clear();

                if (name2Value.Count > 0) foreach (var key in name2Value.Keys) existingKeys.Add(key);

                if (fields.Length != 0)
                {
                    foreach (var field in fields)
                    {
                        var fieldName = field.FieldName;

                        existingKeys.Remove(fieldName);

                        if (name2Value.ContainsKey(field.FieldName)) continue;

                        //add new value
                        switch (field.Type)
                        {
                            case BGCurvePointField.TypeEnum.Bool:
                                name2Value[fieldName] = false;
                                break;
                            case BGCurvePointField.TypeEnum.Int:
                                name2Value[fieldName] = 0;
                                break;
                            case BGCurvePointField.TypeEnum.Float:
                                name2Value[fieldName] = 0f;
                                break;
                            case BGCurvePointField.TypeEnum.String:
                                name2Value[fieldName] = "";
                                break;
                            case BGCurvePointField.TypeEnum.Vector3:
                                name2Value[fieldName] = new Vector3();
                                break;
                            case BGCurvePointField.TypeEnum.Bounds:
                                name2Value[fieldName] = new Bounds();
                                break;
                            case BGCurvePointField.TypeEnum.Color:
                                name2Value[fieldName] = new Color();
                                break;
                            case BGCurvePointField.TypeEnum.Quaternion:
                                name2Value[fieldName] = Quaternion.identity;
                                break;
                            case BGCurvePointField.TypeEnum.AnimationCurve:
                                name2Value[fieldName] = new AnimationCurve();
                                break;
                            case BGCurvePointField.TypeEnum.GameObject:
                            case BGCurvePointField.TypeEnum.Component:
                            case BGCurvePointField.TypeEnum.BGCurve:
                            case BGCurvePointField.TypeEnum.BGCurvePointComponent:
                            case BGCurvePointField.TypeEnum.BGCurvePointGO:
                                name2Value[fieldName] = null;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException("field.Type");
                        }
                    }
                }

                //remove unused keys
                if (existingKeys.Count > 0) foreach (var key in existingKeys) name2Value.Remove(key);
            }
        }
    }
}