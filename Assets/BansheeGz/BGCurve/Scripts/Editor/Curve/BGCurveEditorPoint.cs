using BansheeGz.BGSpline.Curve;
using UnityEditor;
using UnityEngine;

namespace BansheeGz.BGSpline.Editor
{
    //one single point's GUI
    public class BGCurveEditorPoint
    {
        private readonly Texture2D deleteTexture;
        private readonly Texture2D addBeforeTexture;
        private readonly Texture2D moveUpTexture;
        private readonly Texture2D moveDownTexture;
        private readonly Texture2D maskTexture;

        private readonly BGCurveEditorPointsSelection editorSelection;
        private readonly BGCurveEditorPoints editor;

        //styles
        private GUIStyle positionLabelStyle;
        private GUIStyle selectedPositionLabelStyle;
        private GUIStyle controlLabelStyle;

        public BGCurveEditorPoint(BGCurveEditorPoints editor, BGCurveEditorPointsSelection editorSelection)
        {
            this.editor = editor;
            this.editorSelection = editorSelection;

            //textures
            deleteTexture = BGEditorUtility.LoadTexture2D(BGEditorUtility.Image.BGDelete123);
            addBeforeTexture = BGEditorUtility.LoadTexture2D(BGEditorUtility.Image.BGAdd123);
            moveUpTexture = BGEditorUtility.LoadTexture2D(BGEditorUtility.Image.BGMoveUp123);
            moveDownTexture = BGEditorUtility.LoadTexture2D(BGEditorUtility.Image.BGMoveDown123);
            maskTexture = BGEditorUtility.Texture1X1(new Color(1, 0, 0, .15f));
        }

        internal void OnInspectorGUI(BGCurvePoint point, int index, BGCurveSettings settings)
        {
            var maskField = point.Curve.Mode2DOn && Event.current.type == EventType.Repaint;

            BGEditorUtility.HorizontalBox(() =>
            {
                editorSelection.InspectorSelectionRect(point);
                BGEditorUtility.VerticalBox(() =>
                {
                    BGEditorUtility.SwapLabelWidth(60, () =>
                    {
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
                                var math = editor.Editor.Math;
                                var positionWorld = math.GetPosition(index);

                                if (!settings.ShowPointControlType)
                                {
                                    BGEditorUtility.Horizontal(() =>
                                    {
                                        BGEditorUtility.Vector3Field("Point " + index, "Point's position in world space", positionWorld, vector3 => point.PositionWorld = vector3);
                                        if (maskField) MaskFieldFor2D(point);
                                        PointButtons(point, index, settings);
                                    });
                                    BGEditorUtility.StartIndent(1);
                                }
                                else
                                {
                                    BGEditorUtility.Vector3Field("Pos", "Point's position in world space", positionWorld, vector3 => point.PositionWorld = vector3);
                                    if (maskField) MaskFieldFor2D(point);
                                }
                            }
                        }
                    });

                    // control positions
                    if (point.ControlType != BGCurvePoint.ControlTypeEnum.Absent && settings.ShowPointControlPositions)
                    {
                        // 1st
                        BGEditorUtility.Vector3Field("Control 1", "Point 1st control position (local)", point.ControlFirstLocal, vector3 => { point.ControlFirstLocal = vector3; });
                        if (maskField) MaskFieldFor2D(point);

                        // 2nd
                        BGEditorUtility.Vector3Field("Control 2", "Point 2nd control position (local)", point.ControlSecondLocal, vector3 => { point.ControlSecondLocal = vector3; });
                        if (maskField) MaskFieldFor2D(point);
                    }

                    BGEditorUtility.EndIndent(1);
                });
            });
        }

        //mask disabled field with red texture
        private void MaskFieldFor2D(BGCurvePoint point)
        {
            var lastRect = GUILayoutUtility.GetLastRect();

            var twoLiner = lastRect.height > 18;

            if (twoLiner)
            {
                //I have no idea how to calculate it (bruteforce)
                var offset = (EditorGUIUtility.labelWidth/lastRect.width)*50f;
                var oneFieldWidth = (lastRect.width - offset)/3;

                var rect = new Rect(lastRect)
                {
                    x = lastRect.x + offset,
                    y = lastRect.y + 16,
                    height = 16,
                    width = oneFieldWidth
                };
                switch (point.Curve.Mode2D)
                {
                    case BGCurve.Mode2DEnum.XY:
                        rect.x += oneFieldWidth*2;
                        break;
                    case BGCurve.Mode2DEnum.XZ:
                        rect.x += oneFieldWidth;
                        break;
                    case BGCurve.Mode2DEnum.YZ:
                        break;
                }
                GUI.DrawTexture(rect, maskTexture);
            }
            else
            {
                var rect = new Rect(lastRect) {width = (lastRect.width - EditorGUIUtility.labelWidth)/3f};
                switch (point.Curve.Mode2D)
                {
                    case BGCurve.Mode2DEnum.XY:
                        rect.x = lastRect.x + EditorGUIUtility.labelWidth + rect.width*2;
                        break;
                    case BGCurve.Mode2DEnum.XZ:
                        rect.x = lastRect.x + EditorGUIUtility.labelWidth + rect.width;
                        break;
                    case BGCurve.Mode2DEnum.YZ:
                        rect.x = lastRect.x + EditorGUIUtility.labelWidth;
                        break;
                }
                GUI.DrawTexture(rect, maskTexture);
            }
        }

        private void PointButtons(BGCurvePoint point, int index, BGCurveSettings settings)
        {
            if (!settings.ShowPointMenu) return;

            var curve = point.Curve;

            //================== Add before
            if (BGEditorUtility.ButtonWithIcon(addBeforeTexture, "Insert a point before this point"))
            {
                curve.AddPoint(BGNewPointPositionManager.InsertBefore(curve, index, settings.ControlType, settings.Sections), index);
            }

            GUILayout.Space(2);


            //=========================== Move Up
            if (index > 0 && BGEditorUtility.ButtonWithIcon(moveUpTexture, "Move the point up")) curve.Swap(index - 1, index);
            GUILayout.Space(2);

            //=========================== Move Down
            if (index < curve.PointsCount - 1 && BGEditorUtility.ButtonWithIcon(moveDownTexture, "Move the point down")) curve.Swap(index, index + 1);
            GUILayout.Space(2);


            //=========================== Delete
            if (BGEditorUtility.ButtonWithIcon(deleteTexture, "Delete the point"))
            {
                curve.Delete(index);
                editorSelection.Remove(point);
                GUIUtility.ExitGUI();
            }
        }


        public void OnSceneGUI(BGCurvePoint point, int index, BGCurveSettings settings, Quaternion rotation, Plane[] frustum)
        {
            var math = editor.Editor.Math;
            var positionWorld = math.GetPosition(index);

            if (settings.ShowControlHandles && settings.ShowCurve && (!editorSelection.HasSelected() || editorSelection.SingleSelected(point)))
            {
                // ============================================== Controls Handles
                if (point.ControlType != BGCurvePoint.ControlTypeEnum.Absent)
                {
                    var controlFirstWorld = math.GetControlFirst(index);
                    var controlSecondWorld = math.GetControlSecond(index);

                    BGEditorUtility.SwapHandlesColor(settings.ControlHandlesColor, () =>
                    {
                        Handles.DrawLine(positionWorld, controlFirstWorld);
                        Handles.DrawLine(positionWorld, controlSecondWorld);

                        // control handles different types
                        var newPositionFirst = editor.Handle(GetUniqueNumber(index) - 1, settings.ControlHandlesType, controlFirstWorld, rotation, settings.ControlHandlesSettings);
                        var newPositionSecond = editor.Handle(GetUniqueNumber(index) - 2, settings.ControlHandlesType, controlSecondWorld, rotation, settings.ControlHandlesSettings);

                        if (BGEditorUtility.AnyChange(controlFirstWorld, newPositionFirst)) point.ControlFirstWorld = newPositionFirst;
                        if (BGEditorUtility.AnyChange(controlSecondWorld, newPositionSecond)) point.ControlSecondWorld = newPositionSecond;
                    });

                    if (settings.ShowControlLabels)
                    {
                        ShowControlLabel(settings, frustum, controlFirstWorld, point.ControlFirstLocal, "1");
                        ShowControlLabel(settings, frustum, controlSecondWorld, point.ControlSecondLocal, "2");
                    }
                }
            }

            //if only one point is selected and this is the selected point- do not print anything further
            if (editorSelection.HasSelected() && editorSelection.SingleSelected(point)) return;

            // ============================================== Labels & Positions
            if (settings.ShowLabels && GeometryUtility.TestPlanesAABB(frustum, new Bounds(positionWorld, Vector3.one)))
            {
                Handles.Label(editor.GetLabelPosition(settings, positionWorld), "Point " + index + (settings.ShowPositions ? " " + positionWorld : ""),
                    editorSelection.Contains(point) ? selectedPositionLabelStyle : positionLabelStyle);
            }

            // ============================================== Move Handles
            if (!editorSelection.HasSelected() && settings.ShowCurve && settings.ShowHandles)
            {
                var newPos = editor.Handle(GetUniqueNumber(index), settings.HandlesType, positionWorld, rotation, settings.HandlesSettings);

                if (BGEditorUtility.AnyChange(positionWorld, newPos)) point.PositionWorld = newPos;
            }
        }

        private void ShowControlLabel(BGCurveSettings settings, Plane[] frustum, Vector3 positionWorld, Vector3 positionLocal, string label)
        {
            if (GeometryUtility.TestPlanesAABB(frustum, new Bounds(positionWorld, Vector3.one)))
            {
                Handles.Label(positionWorld, label + (settings.ShowControlPositions ? " " + positionLocal : ""), controlLabelStyle);
            }
        }


        //this is to generate Unity UGUI ids (for handles).
        private static int GetUniqueNumber(int index)
        {
            return (-index - 1)*3;
        }

        public void OnSceneGUIStart(BGCurveSettings settings)
        {
            positionLabelStyle = new GUIStyle("Label") {normal = new GUIStyleState {textColor = settings.LabelColor}};
            selectedPositionLabelStyle = new GUIStyle("Label") {normal = new GUIStyleState {textColor = settings.LabelColorSelected}};

            controlLabelStyle = new GUIStyle("Label") {normal = new GUIStyleState {textColor = settings.LabelControlColor}};
        }
    }
}