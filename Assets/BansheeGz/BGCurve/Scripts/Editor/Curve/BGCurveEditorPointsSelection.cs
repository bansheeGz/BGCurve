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


        private readonly Texture2D tickNoTexture;
        private readonly Texture2D tickYesTexture;
        private readonly Texture2D deleteTexture;
        private readonly Texture2D selectAllTexture;
        private readonly Texture2D deselectAllTexture;
        private readonly Texture2D convertAll2D;


        private readonly List<BGCurvePoint> points = new List<BGCurvePoint>();

        private bool groupSelectionStarted;
        private bool groupSelectionIsSelecting;

        private BGCurveSettings settings;

        public bool Changed { get; private set; }

        public int CountSelected
        {
            get { return points.Count; }
        }

        private readonly BGCurve curve;
        private readonly BGCurveEditorPoints editor;
        private BGCurvePoint.ControlTypeEnum controlType = BGCurvePoint.ControlTypeEnum.Absent;

        private readonly BGRectangularSelection selectionRectangle;

        private BGEditorUtility.EventCanceller eventCanceller;

        //do not want to use events
        private int lastCurveCount;

        public BGCurveEditorPointsSelection(BGCurve curve, BGCurveEditorPoints editor)
        {
            this.curve = curve;
            this.editor = editor;

            tickNoTexture = BGEditorUtility.LoadTexture2D(BGEditorUtility.Image.BGTickNo123);
            tickYesTexture = BGEditorUtility.LoadTexture2D(BGEditorUtility.Image.BGTickYes123);
            deleteTexture = BGEditorUtility.LoadTexture2D(BGEditorUtility.Image.BGDelete123);
            selectAllTexture = BGEditorUtility.LoadTexture2D(BGEditorUtility.Image.BGSelectAll123);
            deselectAllTexture = BGEditorUtility.LoadTexture2D(BGEditorUtility.Image.BGDeSelectAll123);
            convertAll2D = BGEditorUtility.LoadTexture2D(BGEditorUtility.Image.BGConvertAll123);

            selectionRectangle = new BGRectangularSelection(editor, this);
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

        public bool Contains(BGCurvePoint point)
        {
            return points.Contains(point);
        }

        //if we selecting or removing selection
        public void GroupSelection(BGCurvePoint point)
        {
            if (groupSelectionIsSelecting) Add(point);
            else Remove(point);
        }

        public bool Add(BGCurvePoint point)
        {
            if (Contains(point)) return false;

            Changed = true;
            points.Add(point);
            return true;
        }

        public bool Remove(BGCurvePoint point)
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
        public void InspectorSelectionRect(BGCurvePoint point)
        {
            var currentEvent = Event.current;
            var rect = GUILayoutUtility.GetRect(24, 24, 24, 24, new GUIStyle {fixedWidth = 24, fixedHeight = 24, stretchWidth = false, stretchHeight = false});
            if (currentEvent.isMouse)
            {
                if (currentEvent.type == EventType.mouseDown)
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
            var selectedTexture = selected ? tickYesTexture : tickNoTexture;
            var labelStyle = selected ? new GUIStyle("Label") {normal = new GUIStyleState {textColor = settings.LabelColorSelected}} : EditorStyles.label;
            EditorGUI.LabelField(rect, new GUIContent(selectedTexture, "Click to (de)select a point, or click and drag to (de)select multiple points. " +
                                                                       "Hold shift to use rectangular selection"), labelStyle);
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

                    if (BGEditorUtility.ButtonWithIcon(deleteTexture, "Delete selected points")) if (!DeleteSelected()) return;

                    GUILayout.Space(4);
                    if (BGEditorUtility.ButtonWithIcon(selectAllTexture, "Select all points", 35))
                    {
                        Changed = Changed || points.Count != curve.PointsCount;

                        points.Clear();

                        foreach (var point1 in curve.Points) points.Add(point1);
                    }

                    GUILayout.Space(4);

                    if (BGEditorUtility.ButtonWithIcon(deselectAllTexture, "Deselect all points", 35)) Clear();
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
                                if (!BGEditorUtility.ButtonWithIcon(convertAll2D, "Set control type for all selected points", 44)) return;

                                SetControlTypeForSelected(controlType);
                            });

                            // =====================================================  Average positions & delete
                            BGEditorUtility.Vector3Field("Average position", "Average points position. Change several points positions at once, keeping distance difference intact",
                                averagePositionSelected,
                                newAverage =>
                                {
                                    var delta = newAverage - averagePositionSelected;
                                    curve.Transaction(() => { foreach (var point in points) point.PositionWorld += delta; });
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

                            if (count == 0) return;

                            //has points with bezier controls
                            BGEditorUtility.Vector3Field("Set Control 1", "Set 1st control position directly",
                                averageControl1Sum/count,
                                newPosition =>
                                {
                                    curve.Transaction(
                                        () => { foreach (var point in points.Where(point => point.ControlType != BGCurvePoint.ControlTypeEnum.Absent)) point.ControlFirstLocal = newPosition; });
                                });

                            BGEditorUtility.Vector3Field("Set Control 2", "Set 2nd control position directly",
                                averageControl2Sum/count,
                                newPosition =>
                                {
                                    curve.Transaction(
                                        () => { foreach (var point in points.Where(point => point.ControlType != BGCurvePoint.ControlTypeEnum.Absent)) point.ControlSecondLocal = newPosition; });
                                });
                        });
                    });
                }
                else
                {
                    BGEditorUtility.HelpBox("Hold shift to use rectangular selection\r\nClick or click+drag over tick icons to (de)select points", MessageType.Info, curve.PointsCount > 0);
                }
            });
        }

        public void SetControlTypeForSelected(BGCurvePoint.ControlTypeEnum controlType)
        {
            curve.Transaction(() => { foreach (var point in points) point.ControlType = controlType; });
        }

        public bool DeleteSelected()
        {
            if (points.Count == 0)
            {
                BGEditorUtility.Inform("Error", "Chose at least one point to delete");
                return false;
            }

            if (!BGEditorUtility.Confirm("Delete points confirmation", "Are you sure you want to remove " + points.Count + " point(s)?", "Delete")) return false;

            curve.Transaction(() => { foreach (var point in points) curve.Delete(point); });

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

            BGEditorUtility.Assign(ref settings, () => editor.Settings);

            //group operation for selected points
            var text = " Selected points [" + points.Count + "]";
            var average = GetAveragePosition();
            Handles.Label(editor.GetLabelPosition(settings, average), text, new GUIStyle("Label") {normal = new GUIStyleState {textColor = settings.LabelColorSelected}});

            var newAverage = editor.Handle(-10, settings.HandlesType, average, rotation, settings.HandlesSettings);

            if (BGEditorUtility.AnyChange(average, newAverage))
            {
                curve.Transaction(() =>
                {
                    var delta = newAverage - average;
                    foreach (var selectedPoint in points) selectedPoint.PositionWorld += delta;
                });
            }
        }

        public bool SingleSelected(BGCurvePoint point)
        {
            return points.Count == 1 && Contains(point);
        }

        public void Process(Event currentEvent)
        {
            if (currentEvent.type == EventType.mouseDown)
            {
                if (currentEvent.button == 0)
                {
                    if ((!currentEvent.control && currentEvent.shift) || BGCurveSettingsForEditor.LockView)
                    {
                        eventCanceller = new BGEditorUtility.EventCanceller();

                        if (!BGCurveSettingsForEditor.DisableRectangularSelection) selectionRectangle.On();
                    }
                }
                else if (currentEvent.button == 1 && selectionRectangle.IsOn)
                {
                    //glitch with right click
                    GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
                }
            }
            else if (currentEvent.type == EventType.mouseUp)
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

            for (var i = points.Count - 1; i >= 0; i--)
            {
                if (curve.IndexOf(points[i]) < 0) points.RemoveAt(i);
            }
        }

        public void ForEach(Func<BGCurvePoint, bool> action)
        {
            if (CountSelected == 0) return;

            foreach (var point in points) if (action(point)) return;
        }
    }
}