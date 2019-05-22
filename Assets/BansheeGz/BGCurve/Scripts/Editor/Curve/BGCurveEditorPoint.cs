using System;
using System.Linq;
using BansheeGz.BGSpline.Curve;
using UnityEditor;
using UnityEngine;

namespace BansheeGz.BGSpline.Editor
{
    //one single point's GUI
    public class BGCurveEditorPoint
    {
        private readonly BGCurveEditorPointsSelection editorSelection;
        private readonly Func<BGCurveBaseMath> mathProvider;

        private GUIStyle controlLabelStyle;

        public BGCurveEditorPoint(Func<BGCurveBaseMath> mathProvider, BGCurveEditorPointsSelection editorSelection)
        {
            this.mathProvider = mathProvider;
            this.editorSelection = editorSelection;
        }

        internal void OnInspectorGui(BGCurvePointI point, int index, BGCurveSettings settings)
        {
            var mode2D = point.Curve.Mode2D;


            //point transform
            if (point.Curve.PointsMode != BGCurve.PointsModeEnum.Inlined && point.PointTransform != null)
            {
                var referenceToPoint = BGCurveReferenceToPoint.GetReferenceToPoint(point);
                if (referenceToPoint == null) point.PointTransform.gameObject.AddComponent<BGCurveReferenceToPoint>().Point = point;
            }

            BGEditorUtility.HorizontalBox(() =>
            {
                if (editorSelection != null) editorSelection.InspectorSelectionRect(point);

                BGEditorUtility.VerticalBox(() =>
                {
                    BGEditorUtility.SwapLabelWidth(60, () =>
                    {
                        if (!settings.ShowPointPosition && !settings.ShowPointControlType)
                        {
                            BGEditorUtility.Horizontal(() =>
                            {
                                //nothing to show- only label
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
                                        PositionField("Point " + index, point, mode2D, index);
                                        PointButtons(point, index, settings);
                                    });
                                    BGEditorUtility.StartIndent(1);
                                }
                                else PositionField("Pos", point, mode2D, index);
                            }
                        }
                    });

                    // control positions
                    if (point.ControlType != BGCurvePoint.ControlTypeEnum.Absent && settings.ShowPointControlPositions)
                    {
                        // 1st
                        ControlField(point, mode2D, 1);

                        // 2nd
                        ControlField(point, mode2D, 2);
                    }

                    //transform
                    if (settings.ShowTransformField)
                        BGEditorUtility.ComponentField("Transform", point.PointTransform, transform =>
                        {
                            if (transform != null)
                            {
                                Undo.RecordObject(transform, "Object moved");

                                if (point.Curve.PointsMode != BGCurve.PointsModeEnum.Inlined) Undo.AddComponent<BGCurveReferenceToPoint>(transform.gameObject).Point = point;
                            }

                            if (point.PointTransform != null)
                            {
                                var referenceToPoint = BGCurveReferenceToPoint.GetReferenceToPoint(point);
                                if (referenceToPoint != null) Undo.DestroyObjectImmediate(referenceToPoint);
                            }

                            point.PointTransform = transform;
                        });


                    //fields
                    if (point.Curve.FieldsCount > 0) ShowFields(point);

                    BGEditorUtility.EndIndent(1);
                });
            });
        }

        private void ControlField(BGCurvePointI point, BGCurve.Mode2DEnum mode2D, int index)
        {
            Vector3 pos;
            string tooltipDetails, details;
            Action<Vector3> newValueAction;
            switch ((BGCurveSettingsForEditor.CoordinateSpaceEnum) BGCurveSettingsForEditor.I.Get<int>(BGCurveSettingsForEditor.InspectorControlsCoordinatesKey))
            {
                case BGCurveSettingsForEditor.CoordinateSpaceEnum.Local:
                    tooltipDetails = "local";
                    details = "L";
                    if (index == 1)
                    {
                        pos = point.ControlFirstLocal;
                        newValueAction = vector3 => point.ControlFirstLocal = vector3;
                    }
                    else
                    {
                        pos = point.ControlSecondLocal;
                        newValueAction = vector3 => point.ControlSecondLocal = vector3;
                    }
                    break;
                case BGCurveSettingsForEditor.CoordinateSpaceEnum.LocalTransformed:
                    tooltipDetails = "local transformed";
                    details = "LT";

                    if (index == 1)
                    {
                        pos = point.ControlFirstLocalTransformed;
                        newValueAction = vector3 => point.ControlFirstLocalTransformed = vector3;
                    }
                    else
                    {
                        pos = point.ControlSecondLocalTransformed;
                        newValueAction = vector3 => point.ControlSecondLocalTransformed = vector3;
                    }
                    break;
                case BGCurveSettingsForEditor.CoordinateSpaceEnum.World:
                    tooltipDetails = "world";
                    details = "W";

                    if (index == 1)
                    {
                        pos = point.ControlFirstWorld;
                        newValueAction = vector3 => point.ControlFirstWorld = vector3;
                    }
                    else
                    {
                        pos = point.ControlSecondWorld;
                        newValueAction = vector3 => point.ControlSecondWorld = vector3;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException("BGCurveSettingsForEditor.InspectorPointControlsCoordinates");
            }

            var label = "Control " + index + " (" + details + ")";
            var tooltip = "Point " + (index == 0 ? "1st" : "2nd") + " control position in " + tooltipDetails + " space. You can change points space in BGCurve Editor settings (gear icon)";

            

            if (mode2D == BGCurve.Mode2DEnum.Off) BGEditorUtility.Vector3Field(label, tooltip, pos, newValueAction);
            else Vector2Field(label, tooltip, pos, mode2D, newValueAction);

        }

        private void PositionField(string name, BGCurvePointI point, BGCurve.Mode2DEnum mode2D, int index)
        {
            var math = mathProvider();

            Vector3 pos;
            Action<Vector3> newValueAction;
            string tooltipDetails;
            string details;
            switch ((BGCurveSettingsForEditor.CoordinateSpaceEnum) BGCurveSettingsForEditor.I.Get<int>(BGCurveSettingsForEditor.InspectorPointsCoordinatesKey))
            {
                case BGCurveSettingsForEditor.CoordinateSpaceEnum.World:
                    pos = math == null ? point.PositionWorld : math.GetPosition(index);
                    newValueAction = vector3 => point.PositionWorld = vector3;
                    tooltipDetails = "world";
                    details = "W";
                    break;
                case BGCurveSettingsForEditor.CoordinateSpaceEnum.LocalTransformed:
                    pos = point.PositionLocalTransformed;
                    newValueAction = vector3 => point.PositionLocalTransformed = vector3;
                    tooltipDetails = "local transformed";
                    details = "LT";
                    break;
                case BGCurveSettingsForEditor.CoordinateSpaceEnum.Local:
                    pos = point.PositionLocal;
                    newValueAction = vector3 => point.PositionLocal = vector3;
                    tooltipDetails = "local";
                    details = "L";
                    break;
                default:
                    throw new ArgumentOutOfRangeException("BGCurveSettingsForEditor.InspectorPointCoordinates");
            }

            BGEditorUtility.SwapLabelWidth(80, () =>
            {
                var label = name + "(" + details + ")";
                var tooltip = "Point's position in " + tooltipDetails + " space. You can change points space in BGCurve Editor settings (gear icon)";

                if (mode2D == BGCurve.Mode2DEnum.Off) BGEditorUtility.Vector3Field(label, tooltip, pos, newValueAction);
                else Vector2Field(label, tooltip, pos, mode2D, newValueAction);
            });
        }

        private void Vector2Field(string label, string tooltip, Vector3 value, BGCurve.Mode2DEnum mode2D, Action<Vector3> newValueAction)
        {
            Vector2 val;
            switch (mode2D)
            {
                case BGCurve.Mode2DEnum.XY:
                    val = value;
                    break;
                case BGCurve.Mode2DEnum.XZ:
                    val = new Vector2(value.x, value.z);
                    break;
                case BGCurve.Mode2DEnum.YZ:
                    val = new Vector2(value.y, value.z);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("mode2D", mode2D, null);
            }

            BGEditorUtility.Vector2Field(label, tooltip, val, vector2 =>
            {
                Vector3 newValue;
                switch (mode2D)
                {
                    case BGCurve.Mode2DEnum.XY:
                        newValue = vector2;
                        break;
                    case BGCurve.Mode2DEnum.XZ:
                        newValue = new Vector3(vector2.x, 0, vector2.y);
                        break;
                    case BGCurve.Mode2DEnum.YZ:
                        newValue = new Vector3(0, vector2.x, vector2.y);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("mode2D", mode2D, null);
                }
                newValueAction(newValue);
            });
        }


        private static void ShowFields(BGCurvePointI point)
        {
            foreach (var field in point.Curve.Fields.Where(BGPrivateField.GetShowInPointsMenu)) ShowField(point, field);
        }

        public static void ShowField(BGCurvePointI point, BGCurvePointField field, Action<string, AnimationCurve> animationCurveCallback = null)
        {
            var name = point.Curve.IndexOf(field) + ") " + field.FieldName;
            switch (field.Type)
            {
                case BGCurvePointField.TypeEnum.Bool:
                    BGEditorUtility.BoolField(name, point.GetField<bool>(field.FieldName), v => point.SetField(field.FieldName, v));
                    break;
                case BGCurvePointField.TypeEnum.Int:
                    BGEditorUtility.IntField(name, point.GetField<int>(field.FieldName), v => point.SetField(field.FieldName, v));
                    break;
                case BGCurvePointField.TypeEnum.Float:
                    BGEditorUtility.FloatField(name, point.GetField<float>(field.FieldName), v => point.SetField(field.FieldName, v));
                    break;
                case BGCurvePointField.TypeEnum.Vector3:
                    BGEditorUtility.Vector3Field(name, null, point.GetField<Vector3>(field.FieldName), v => point.SetField(field.FieldName, v));
                    break;
                case BGCurvePointField.TypeEnum.Bounds:
                    BGEditorUtility.BoundsField(name, point.GetField<Bounds>(field.FieldName), v => point.SetField(field.FieldName, v));
                    break;
                case BGCurvePointField.TypeEnum.Color:
                    BGEditorUtility.ColorField(name, point.GetField<Color>(field.FieldName), v => point.SetField(field.FieldName, v));
                    break;
                case BGCurvePointField.TypeEnum.String:
                    BGEditorUtility.TextField(name, point.GetField<string>(field.FieldName), v => point.SetField(field.FieldName, v), false);
                    break;
                case BGCurvePointField.TypeEnum.AnimationCurve:
                    BGEditorUtility.Horizontal(() =>
                    {
                        BGEditorUtility.AnimationCurveField(name, point.GetField<AnimationCurve>(field.FieldName), v => point.SetField(field.FieldName, v));

                        if (animationCurveCallback != null && GUILayout.Button("Set", GUILayout.Width(40))) animationCurveCallback(field.FieldName, point.GetField<AnimationCurve>(field.FieldName));
                    });

                    break;
                case BGCurvePointField.TypeEnum.Quaternion:
                    BGEditorUtility.QuaternionField(name, point.GetField<Quaternion>(field.FieldName), v => point.SetField(field.FieldName, v));
                    break;
                case BGCurvePointField.TypeEnum.GameObject:
                    BGEditorUtility.GameObjectField(name, point.GetField<GameObject>(field.FieldName), v => point.SetField(field.FieldName, v));
                    break;
                case BGCurvePointField.TypeEnum.Component:
                    BGEditorUtility.ComponentChoosableField(name, point.GetField<Component>(field.FieldName), v => point.SetField(field.FieldName, v));
                    break;
                case BGCurvePointField.TypeEnum.BGCurve:
                    BGEditorUtility.BGCurveField(name, point.GetField<BGCurve>(field.FieldName), v => point.SetField(field.FieldName, v));
                    break;
                case BGCurvePointField.TypeEnum.BGCurvePointComponent:
                    BGEditorUtility.Horizontal(() =>
                    {
                        BGEditorUtility.BGCurvePointComponentField(name, point.GetField<BGCurvePointComponent>(field.FieldName), v => point.SetField(field.FieldName, v));
                        var currentPoint = point.GetField<BGCurvePointComponent>(field.FieldName);

                        if (currentPoint == null || currentPoint.Curve.PointsCount < 2) return;

                        var indexOfField = currentPoint.Curve.IndexOf(currentPoint);

                        if (GUILayout.Button("" + indexOfField, GUILayout.Width(40))) BGCurveChosePointWindow.Open(indexOfField, currentPoint.Curve, c => point.SetField(field.FieldName, c));
                    });
                    break;
                case BGCurvePointField.TypeEnum.BGCurvePointGO:
                    BGEditorUtility.BGCurvePointGOField(name, point.GetField<BGCurvePointGO>(field.FieldName), v => point.SetField(field.FieldName, v));
                    break;
            }
        }


        private void PointButtons(BGCurvePointI point, int index, BGCurveSettings settings)
        {
            if (!settings.ShowPointMenu) return;

            var curve = point.Curve;

            //================== Copy
            if (BGEditorUtility.ButtonWithIcon(BGBinaryResources.BGCopy123, PointCopyPaste.Instance.CopyTooltip)) PointCopyPaste.Instance.Copy(point);
            GUILayout.Space(2);

            //================== Paste
            if (BGEditorUtility.ButtonWithIcon(BGBinaryResources.BGPaste123, PointCopyPaste.Instance.PasteTooltip)) PointCopyPaste.Instance.Paste(point);
            GUILayout.Space(2);

            //================== Add before
            if (BGEditorUtility.ButtonWithIcon(BGBinaryResources.BGAdd123, "Insert a point before this point"))
                BGCurveEditor.AddPoint(curve, BGNewPointPositionManager.InsertBefore(curve, index, settings.ControlType, settings.Sections), index);
            GUILayout.Space(2);


            //=========================== Move Up
            if (index > 0 && BGEditorUtility.ButtonWithIcon(BGBinaryResources.BGMoveUp123, "Move the point up")) curve.Swap(index - 1, index);
            GUILayout.Space(2);

            //=========================== Move Down
            if (index < curve.PointsCount - 1 && BGEditorUtility.ButtonWithIcon(BGBinaryResources.BGMoveDown123, "Move the point down")) curve.Swap(index, index + 1);
            GUILayout.Space(2);


            //=========================== Delete
            if (BGEditorUtility.ButtonWithIcon(BGBinaryResources.BGDelete123, "Delete the point"))
            {
                BGCurveEditor.DeletePoint(curve, index);
                if (editorSelection != null) editorSelection.Remove(point);
                GUIUtility.ExitGUI();
            }
        }


        public void OnSceneGUI(BGCurvePointI point, int index, BGCurveSettings settings, Quaternion rotation, Plane[] frustum)
        {
            var math = mathProvider();
            var positionWorld = math == null ? point.Curve[index].PositionWorld : math.GetPosition(index);

            //adjust rotation
            if (point.PointTransform != null) rotation = BGCurveEditorPoints.GetRotation(point.PointTransform);
            else if (point.Curve.PointsMode == BGCurve.PointsModeEnum.GameObjectsTransform) rotation = BGCurveEditorPoints.GetRotation(((BGCurvePointGO) point).transform);

            var isShowingGizmoz = settings.RestrictGizmozSettings.IsShowing(index);

            if (settings.ShowControlHandles && settings.ShowCurve && (editorSelection == null || !editorSelection.HasSelected() || editorSelection.SingleSelected(point)))
            {
                // ============================================== Controls Handles
                if (point.ControlType != BGCurvePoint.ControlTypeEnum.Absent)
                {
                    if (isShowingGizmoz)
                    {
                        var controlFirstWorld = math == null ? point.Curve[index].ControlFirstWorld : math.GetControlFirst(index);
                        var controlSecondWorld = math == null ? point.Curve[index].ControlSecondWorld : math.GetControlSecond(index);

                        BGEditorUtility.SwapHandlesColor(settings.ControlHandlesColor, () =>
                        {
                            Handles.DrawLine(positionWorld, controlFirstWorld);
                            Handles.DrawLine(positionWorld, controlSecondWorld);

                            if (ShowingHandles)
                            {
                                // control handles different types
                                var newPositionFirst = BGEditorUtility.Handle(GetUniqueNumber(index) - 1, settings.ControlHandlesType, controlFirstWorld, rotation, settings.ControlHandlesSettings);
                                var newPositionSecond = BGEditorUtility.Handle(GetUniqueNumber(index) - 2, settings.ControlHandlesType, controlSecondWorld, rotation, settings.ControlHandlesSettings);

                                if (BGEditorUtility.AnyChange(controlFirstWorld, newPositionFirst)) point.ControlFirstWorld = newPositionFirst;
                                if (BGEditorUtility.AnyChange(controlSecondWorld, newPositionSecond)) point.ControlSecondWorld = newPositionSecond;
                            }
                        });

                        if (settings.ShowControlLabels)
                        {
                            ShowControlLabel(settings, frustum, controlFirstWorld, point.ControlFirstLocal, "1");
                            ShowControlLabel(settings, frustum, controlSecondWorld, point.ControlSecondLocal, "2");
                        }
                    }
                }
            }

            //if only one point is selected and this is the selected point- do not print anything further
            if (editorSelection != null && editorSelection.HasSelected() && editorSelection.SingleSelected(point)) return;

            // ============================================== Move Handles
            if ((editorSelection == null || !editorSelection.HasSelected()) && settings.ShowCurve && settings.ShowHandles && ShowingHandles && isShowingGizmoz)
            {
                var newPos = BGEditorUtility.Handle(GetUniqueNumber(index), settings.HandlesType, positionWorld, rotation, settings.HandlesSettings);

                if (BGEditorUtility.AnyChange(positionWorld, newPos)) point.PositionWorld = newPos;
            }
        }

        private static bool ShowingHandles
        {
            get { return Tools.current != Tool.View; }
        }

        private void ShowControlLabel(BGCurveSettings settings, Plane[] frustum, Vector3 positionWorld, Vector3 positionLocal, string label)
        {
            BGEditorUtility.Assign(ref controlLabelStyle, () => new GUIStyle("Label") {normal = new GUIStyleState {textColor = settings.LabelControlColor}});

            if (GeometryUtility.TestPlanesAABB(frustum, new Bounds(positionWorld, Vector3.one)))
                Handles.Label(positionWorld, label + (settings.ShowControlPositions ? " " + positionLocal : ""), controlLabelStyle);
        }


        //this is to generate Unity UGUI ids (for handles).
        private static int GetUniqueNumber(int index)
        {
            return (-index - 1)*3;
        }

        private sealed class PointCopyPaste
        {
            public static readonly PointCopyPaste Instance = new PointCopyPaste();

            public readonly string CopyTooltip = "Copy " +
                                                 "\r\n1) ControlType, " +
                                                 "\r\n2) Position (Local), " +
                                                 "\r\n3) Control1 (LocalTransformed), " +
                                                 "\r\n4) Control2 (LocalTransformed), " +
                                                 "\r\n5) PointTransform";

            public string PasteTooltip;

            private BGCurvePoint.ControlTypeEnum controlType;
            private Vector3 positionLocal;
            private Vector3 control1LocalTransformed;
            private Vector3 control2LocalTransformed;
            private Transform pointTransform;

            private PointCopyPaste()
            {
                InitPasteTooltip();
            }

            public void Copy(BGCurvePointI point)
            {
                controlType = point.ControlType;
                positionLocal = point.PositionLocal;
                control1LocalTransformed = point.ControlFirstLocalTransformed;
                control2LocalTransformed = point.ControlSecondLocalTransformed;
                pointTransform = point.PointTransform;

                InitPasteTooltip();
            }

            private void InitPasteTooltip()
            {
                PasteTooltip = "Paste " +
                               "\r\n1) ControlType=" + controlType +
                               "\r\n2) Position (Local)=" + positionLocal +
                               "\r\n3) Control1 (LocalTransformed)=" + control1LocalTransformed +
                               "\r\n4) Control2 (LocalTransformed)=" + control2LocalTransformed +
                               "\r\n5) PointTransform=" + pointTransform;
            }

            public void Paste(BGCurvePointI point)
            {
                point.Curve.Transaction(() =>
                {
                    point.ControlType = controlType;
                    point.PositionLocal = positionLocal;
                    point.ControlFirstLocalTransformed = control1LocalTransformed;
                    point.ControlSecondLocalTransformed = control2LocalTransformed;
                    point.PointTransform = pointTransform;
                });
            }
        }
    }
}