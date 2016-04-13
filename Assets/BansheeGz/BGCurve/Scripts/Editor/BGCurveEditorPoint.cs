using BansheeGz.BGSpline.Curve;
using BansheeGz.BGSpline.EditorHelpers;
using UnityEditor;
using UnityEngine;

namespace BansheeGz.BGSpline.Editor
{
    public class BGCurveEditorPoint
    {
        private readonly Texture2D deleteTexture;
        private readonly Texture2D addBeforeTexture;
        private readonly Texture2D moveUpTexture;
        private readonly Texture2D moveDownTexture;

        private readonly BGCurveEditorPointsSelection editorSelection;
        private BGCurveBaseMath math;

        public BGCurveEditorPoint(BGCurve curve, BGCurveEditorPointsSelection editorSelection)
        {
            this.editorSelection = editorSelection;
            //textures
            deleteTexture = BGEditorUtility.LoadTexture2D("BGDelete123");
            addBeforeTexture = BGEditorUtility.LoadTexture2D("BGAdd123");
            moveUpTexture = BGEditorUtility.LoadTexture2D("BGMoveUp123");
            moveDownTexture = BGEditorUtility.LoadTexture2D("BGMoveDown123");

            math = new BGCurveBaseMath(curve, false, 30);
        }


        internal void OnInspectorGUI(BGCurvePoint point, int index, BGCurveSettings settings)
        {
            var curve = point.Curve;

            BGEditorUtility.Horizontal("Box", () =>
            {
                editorSelection.InspectorSelectionRect(point);
                BGEditorUtility.Vertical("Box", () =>
                {
                    EditorGUIUtility.labelWidth = 60;
                    if (!settings.ShowPointPosition && !settings.ShowPointControlType)
                    {
                        BGEditorUtility.Horizontal(() =>
                        {
                            EditorGUILayout.LabelField("Point " + index);
                            PointButtons(point, index, settings);
                        });
                        BGEditorUtility.StartIndent(1);
                    }
                    else
                    {
                        //control type
                        if (settings.ShowPointControlType)
                        {
                            BGEditorUtility.Horizontal(() =>
                            {
                                point.ControlType = (BGCurvePoint.ControlTypeEnum) EditorGUILayout.EnumPopup("Point " + index, point.ControlType);
                                PointButtons(point, index, settings);
                            });
                            BGEditorUtility.StartIndent(1);
                        }

                        //position
                        if (settings.ShowPointPosition)
                        {
                            if (!settings.ShowPointControlType)
                            {
                                BGEditorUtility.Horizontal(() =>
                                {
                                    BGEditorUtility.Vector3Field("Point " + index, "Point's position in world space", point.PositionWorld, vector3 =>
                                    {
                                        Undo.RecordObject(curve, "Move Point");
                                        point.PositionWorld = vector3;
                                    });
                                    PointButtons(point, index, settings);
                                });
                                BGEditorUtility.StartIndent(1);
                            }
                            else
                            {
                                BGEditorUtility.Vector3Field("Pos", "Point's position in world space", point.PositionWorld, vector3 =>
                                {
                                    Undo.RecordObject(curve, "Move Point");
                                    point.PositionWorld = vector3;
                                });
                            }
                        }
                    }
                    EditorGUIUtility.labelWidth = 0;

                    // control positions
                    if (point.ControlType != BGCurvePoint.ControlTypeEnum.Absent && settings.ShowPointControlPositions)
                    {
                        // 1st
                        BGEditorUtility.Vector3Field("Control 1", "Point 1st control position (local)", point.ControlFirstLocal, vector3 =>
                        {
                            Undo.RecordObject(curve, "Move Control1 position");
                            point.ControlFirstLocal = vector3;
                        });

                        // 2nd
                        BGEditorUtility.Vector3Field("Control 2", "Point 2nd control position (local)", point.ControlSecondLocal, vector3 =>
                        {
                            Undo.RecordObject(curve, "Move Control2 position");
                            point.ControlSecondLocal = vector3;
                        });
                    }

                    BGEditorUtility.EndIndent(1);
                });
            });
        }

        private void PointButtons(BGCurvePoint point, int index, BGCurveSettings settings)
        {
            if (!settings.ShowPointMenu) return;

            var curve = point.Curve;

            //================== Add before
            if (BGEditorUtility.ButtonWithIcon(16, 16, addBeforeTexture, "Insert a point before this point"))
            {
                Undo.RecordObject(curve, "Insert a point");

                Vector3 newPos;
                if (index == 0)
                {
                    newPos = point.PositionWorld - Vector3.forward;
                }
                else
                {
                    newPos = math.CalcPositionByT(curve.Points[index - 1], curve.Points[index], .5f);
                }

                curve.AddPoint(curve.CreatePointFromWorldPosition(newPos, settings.ControlType), index);
                EditorUtility.SetDirty(curve);
            }

            GUILayout.Space(2);


            //=========================== Move Up
            if (index > 0)
            {
                if (BGEditorUtility.ButtonWithIcon(16, 16, moveUpTexture, "Move the point up"))
                {
                    Undo.RecordObject(curve, "Swap Points");
                    curve.Swap(index - 1, index);
                    EditorUtility.SetDirty(curve);
                }
            }
            GUILayout.Space(2);

            //=========================== Move Down
            if (index < curve.PointsCount - 1)
            {
                if (BGEditorUtility.ButtonWithIcon(16, 16, moveDownTexture, "Move the point down"))
                {
                    Undo.RecordObject(curve, "Swap Points");
                    curve.Swap(index, index + 1);
                    EditorUtility.SetDirty(curve);
                }
            }
            GUILayout.Space(2);


            //=========================== Delete
            if (BGEditorUtility.ButtonWithIcon(16, 16, deleteTexture, "Delete the point"))
            {
                Undo.RecordObject(curve, "Delete Point");
                curve.Delete(index);
                editorSelection.Remove(point);
                EditorUtility.SetDirty(curve);
            }
        }


        public void OnSceneGUI(BGCurveEditorPoints editor, BGCurvePoint point, int index, BGCurveSettings settings, Quaternion rotation)
        {
            var curve = point.Curve;

            var positionWorld = point.PositionWorld;
            var normalLabelStyle = new GUIStyle("Label") {normal = new GUIStyleState {textColor = settings.LabelColor}};

            if ((!editorSelection.HasSelected() || editorSelection.SingleSelected(point)) && settings.ShowControlHandles)
            {
                // ============================================== Controls Handles
                if (point.ControlType != BGCurvePoint.ControlTypeEnum.Absent)
                {
                    var handlesColor = Handles.color;
                    Handles.color = settings.ControlHandlesColor;

                    var handleFirstWorld = point.ControlFirstWorld;
                    var handleSecondWorld = point.ControlSecondWorld;


                    Handles.DrawLine(positionWorld, handleFirstWorld);
                    Handles.DrawLine(positionWorld, handleSecondWorld);

                    // control handles different types
                    var newPositionFirst = editor.Handle(GetUniqueNumber(index) - 1, settings.ControlHandlesType, handleFirstWorld, rotation, settings.ControlHandlesSettings);
                    var newPositionSecond = editor.Handle(GetUniqueNumber(index) - 2, settings.ControlHandlesType, handleSecondWorld, rotation, settings.ControlHandlesSettings);
                    if (BGEditorUtility.AnyChange(handleFirstWorld, newPositionFirst))
                    {
                        Undo.RecordObject(curve, "Move first handle");
                        point.ControlFirstWorld = newPositionFirst;
                    }
                    if (BGEditorUtility.AnyChange(handleSecondWorld, newPositionSecond))
                    {
                        Undo.RecordObject(curve, "Move second handle");
                        point.ControlSecondWorld = newPositionSecond;
                    }

                    Handles.color = handlesColor;

                    if (settings.ShowLabels)
                    {
                        Handles.Label(point.ControlFirstWorld, "1" + (settings.ShowControlPositions ? " " + point.ControlFirstLocal : ""), normalLabelStyle);
                        Handles.Label(point.ControlSecondWorld, "2" + (settings.ShowControlPositions ? " " + point.ControlSecondLocal : ""), normalLabelStyle);
                    }
                }
            }

            if (editorSelection.HasSelected() && editorSelection.SingleSelected(point))
            {
                //if only one point is selected and this is the selected point- do not print anything further
                return;
            }

            // ============================================== Labels & Positions
            if (settings.ShowLabels)
            {
                var style = editorSelection.Contains(point)
                    ? new GUIStyle("Label") {normal = new GUIStyleState {textColor = settings.LabelColorSelected}}
                    : normalLabelStyle;

                var text = "Point " + index;
                if (settings.ShowPositions)
                {
                    text += " " + point.PositionWorld;
                }

                Handles.Label(editor.GetLabelPosition(positionWorld), text, style);
            }

            // ============================================== Move Handles
            if (!editorSelection.HasSelected() && settings.ShowCurve && settings.ShowHandles)
            {
                var newPos = editor.Handle(GetUniqueNumber(index), settings.HandlesType, positionWorld, rotation, settings.HandlesSettings);
                if (BGEditorUtility.AnyChange(positionWorld, newPos))
                {
                    point.PositionWorld = newPos;
                }
            }
        }


        private int GetUniqueNumber(int index)
        {
            return (-index - 1)*3;
        }
    }
}