using System.Collections.Generic;
using BansheeGz.BGSpline.Curve;
using BansheeGz.BGSpline.EditorHelpers;
using UnityEditor;
using UnityEngine;

namespace BansheeGz.BGSpline.Editor
{

//helper class for points selection inside editor
    public class BGCurveEditorPointsSelection
    {
        private readonly Texture2D tickNoTexture;
        private readonly Texture2D tickYesTexture;
        private readonly Texture2D whiteTexture;
        private readonly Texture2D deleteTexture;
        private readonly Texture2D selectAllTexture;
        private readonly Texture2D deselectAllTexture;
        private readonly Texture2D convertAll2D;


        private readonly List<BGCurvePoint> points = new List<BGCurvePoint>();
        private bool changed;

        private bool groupSelectionStarted;
        private bool groupSelectionIsSelecting;

        private BGCurveSettings settings;

        public bool Changed
        {
            get { return changed; }
        }

        private BGCurve curve;
        private BGCurveEditorPoints editor;
        private BGCurvePoint.ControlTypeEnum controlType = BGCurvePoint.ControlTypeEnum.Absent;

        public BGCurveEditorPointsSelection(BGCurve curve, BGCurveEditorPoints editor)
        {
            this.curve = curve;
            this.editor = editor;

            tickNoTexture = BGEditorUtility.LoadTexture2D("BGTickNo123"); 
            tickYesTexture = BGEditorUtility.LoadTexture2D("BGTickYes123");
            whiteTexture = BGEditorUtility.LoadTexture2D("BGWhite123"); 
            deleteTexture = BGEditorUtility.LoadTexture2D("BGDelete123"); 
            selectAllTexture = BGEditorUtility.LoadTexture2D("BGSelectAll123");
            deselectAllTexture = BGEditorUtility.LoadTexture2D("BGDeSelectAll123");
            convertAll2D = BGEditorUtility.LoadTexture2D("BGConvertAll123"); 

        }

        public bool HasSelected()
        {
            return points.Count > 0;
        }


        private void Clear()
        {
            changed = changed || points.Count != 0;
            points.Clear();
        }

        public bool Contains(BGCurvePoint point)
        {
            return points.Contains(point);
        }

        //if we selecting or removing selection
        public void GroupSelection(BGCurvePoint point)
        {
            if (groupSelectionIsSelecting)
            {
                if (!Contains(point))
                {
                    changed = true;
                    points.Add(point);
                }
            }
            else
            {
                Remove(point);
            }
        }

        public void Remove(BGCurvePoint point)
        {
            if (Contains(point))
            {
                changed = true;
                points.Remove(point);
            }
        }

        public void SetX(float x)
        {
            foreach (var point in points)
            {
                var positionWorld = point.PositionWorld;
                point.PositionWorld = new Vector3(x, positionWorld.y, positionWorld.z);
            }
        }

        public void SetY(float y)
        {
            foreach (var point in points)
            {
                var positionWorld = point.PositionWorld;
                point.PositionWorld = new Vector3(positionWorld.x, y, positionWorld.z);
            }
        }

        public void SetZ(float z)
        {
            foreach (var point in points)
            {
                var positionWorld = point.PositionWorld;
                point.PositionWorld = new Vector3(positionWorld.x, positionWorld.y, z);
            }
        }

        public void Reset()
        {
            changed = false;
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
                else if (groupSelectionStarted && currentEvent.type == EventType.MouseUp)
                {
                    groupSelectionStarted = false;
                }
                else if (groupSelectionStarted && currentEvent.type == EventType.MouseDrag)
                {
                    if (rect.Contains(currentEvent.mousePosition))
                    {
                        GroupSelection(point);
                    }
                }
            }

            var selected = Contains(point);
            var selectedTexture = selected ? tickYesTexture : tickNoTexture;
            var labelStyle = selected ? new GUIStyle("Label") { normal = new GUIStyleState { textColor = settings.LabelColorSelected } } : EditorStyles.label;
            EditorGUI.LabelField(rect, new GUIContent(selectedTexture, "Click to (de)select a point, or click and drag to (de)select multiple points"), labelStyle);
        }

        //OnInspectorGui for selection
        public void InspectorSelectionOperations()
        {
            BGEditorUtility.Vertical("Box", () =>
            {
                // ================================================ Global operations
                BGEditorUtility.Horizontal("Box", () =>
                {
                    EditorGUIUtility.labelWidth = 80;
                    EditorGUILayout.LabelField("Selected (" + points.Count + ")");
                    EditorGUIUtility.labelWidth = 0;

                    if (BGEditorUtility.ButtonWithIcon(16, 16, deleteTexture, "Delete selected points"))
                    {
                        if (EditorUtility.DisplayDialog("Delete points confirmation", "Are you sure you want to remove " + points.Count + " points?", "Delete", "Cancel"))
                        {
                            Undo.RecordObject(curve, "Delete " + points.Count + " Points");
                            foreach (var point in points)
                            {
                                curve.Delete(point);
                            }
                            Clear();
                        }
                    }

                    GUILayout.Space(4);
                    if (BGEditorUtility.ButtonWithIcon(35, 16, selectAllTexture, "Select all points"))
                    {
                        changed = changed || points.Count != curve.Points.Length;

                        points.Clear();
                        foreach (var point1 in curve.Points)
                        {
                            points.Add(point1);
                        }
                    }

                    GUILayout.Space(4);
                    if (BGEditorUtility.ButtonWithIcon(35, 16, deselectAllTexture, "Deselect all points"))
                    {
                        Clear();
                    }
                });


                // ================================================ Selections operations
                // skip mouse buttons events which change selection
                if (changed) return;


                GUILayout.Space(5);
                if (HasSelected())
                {
                    var color = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(157/255f, 246/255f, 246/255f, 40/255f);

                    BGEditorUtility.Horizontal(new GUIStyle("Box") {normal = {background = whiteTexture}}, () =>
                    {
                        BGEditorUtility.Vertical("Box", () =>
                        {
                            var averagePositionSelected = GetAveragePosition();

                            // =====================================================  Control handles
                            BGEditorUtility.Horizontal(() =>
                            {
                                controlType = (BGCurvePoint.ControlTypeEnum) EditorGUILayout.EnumPopup("Controls", controlType);
                                if (BGEditorUtility.ButtonWithIcon(44, 16, convertAll2D, "Set control type for all selected points"))
                                {
                                    foreach (var point in points)
                                    {
                                        point.ControlType = controlType;
                                    }
                                }
                            });

                            // =====================================================  Average positions & delete
                            BGEditorUtility.Vector3Field("Average position", "Average points position. Change several points positions at once, keeping distance difference intact",
                                averagePositionSelected,
                                newAverage =>
                                {
                                    Undo.RecordObject(curve, "Move " + points.Count + " Points");
                                    var delta = newAverage - averagePositionSelected;
                                    foreach (var point in points)
                                    {
                                        point.PositionWorld += delta;
                                    }
                                });
                            // =====================================================  Set position directly
                            BGEditorUtility.Vector3Field("Set position", "Set points position directly",
                                averagePositionSelected,
                                newPosition =>
                                {
                                    Undo.RecordObject(curve, "Move " + points.Count + " Points");
                                    if (BGEditorUtility.AnyChange(averagePositionSelected.x, newPosition.x))
                                    {
                                        SetX(newPosition.x);
                                    }
                                    if (BGEditorUtility.AnyChange(averagePositionSelected.y, newPosition.y))
                                    {
                                        SetY(newPosition.y);
                                    }
                                    if (BGEditorUtility.AnyChange(averagePositionSelected.z, newPosition.z))
                                    {
                                        SetZ(newPosition.z);
                                    }
                                });

                            // =====================================================  Set control positions directly
                            var count = 0;
                            var averageControl1Sum = Vector3.zero;
                            var averageControl2Sum = Vector3.zero;
                            foreach (var point in points)
                            {
                                if (point.ControlType == BGCurvePoint.ControlTypeEnum.Absent) continue;

                                count++;
                                averageControl1Sum += point.ControlFirstLocal;
                                averageControl2Sum += point.ControlSecondLocal;
                            }
                            if (count > 0)
                            {
                                //has points with bezier controls
                                BGEditorUtility.Vector3Field("Set Control 1", "Set 1st control position directly",
                                    averageControl1Sum/count,
                                    newPosition =>
                                    {
                                        Undo.RecordObject(curve, "Move " + count + " Controls");
                                        foreach (var point in points)
                                        {
                                            if (point.ControlType == BGCurvePoint.ControlTypeEnum.Absent) continue;
                                            point.ControlFirstLocal = newPosition;
                                        }
                                    });

                                BGEditorUtility.Vector3Field("Set Control 2", "Set 2nd control position directly",
                                    averageControl2Sum/count,
                                    newPosition =>
                                    {
                                        Undo.RecordObject(curve, "Move " + count + " Controls");
                                        foreach (var point in points)
                                        {
                                            if (point.ControlType == BGCurvePoint.ControlTypeEnum.Absent) continue;
                                            point.ControlSecondLocal = newPosition;
                                        }
                                    });
                            }
                        });

                    });


                    GUI.backgroundColor = color;
                }
                else
                {
                    if (curve.PointsCount > 0)
                    {
                        EditorGUILayout.HelpBox("Click or click+drag over tick icons to (de)select points", MessageType.Info);
                    }
                }
            });
        }

        private Vector3 GetAveragePosition()
        {
            var sum = Vector3.zero;
            foreach (var point in points)
            {
                sum += point.PositionWorld;
            }
            return sum/points.Count;
        }

        // OnSceneGui
        public void Scene(Quaternion rotation)
        {
            if (!HasSelected()) return;


            //group operation for selected points
            var text = points.Count == 1 ? "Selected Point " + curve.IndexOf(points[0]) : "Selected " + points.Count + " points";
            var average = GetAveragePosition();
            Handles.Label(editor.GetLabelPosition(average), text, new GUIStyle("Label") {normal = new GUIStyleState {textColor = Color.blue}});

            var newAverage = editor.Handle(-10, settings.HandlesType, average, rotation, settings.HandlesSettings);

            if (BGEditorUtility.AnyChange(average, newAverage))
            {
                var delta = newAverage - average;
                foreach (var selectedPoint in points)
                {
                    selectedPoint.PositionWorld += delta;
                }
            }
        }

        public bool SingleSelected(BGCurvePoint point)
        {
            return points.Count == 1 && Contains(point);
        }
    }
}