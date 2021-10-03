using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BansheeGz.BGSpline.Curve;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
    public class BGCurveEditorFields : BGCurveEditorTab
    {
        //label width in percents
        private const int LabelWidth = 30;

        // ====================================== Fields

        private BGTableView systemUi;
        private BGTableView customUi;

        private SystemField[] systemFields;
        private PointField[] customFields;
        private string newFieldName;
        private BGCurvePointField.TypeEnum newFieldType;
        private readonly BGCurveEditorPointsSelection editorSelection;

        public override Texture2D Header2D
        {
            get { return BGBinaryResources.BGFields123; }
        }

        public BGCurveEditorFields(BGCurveEditor editor, SerializedObject curveObject, BGCurveEditorPointsSelection editorSelection) : base(editor, curveObject)
        {
            this.editorSelection = editorSelection;
        }


        // ================================================================================ Inspector
        public override void OnInspectorGui()
        {
            var settings = BGPrivateField.GetSettings(Curve);

            BGEditorUtility.HelpBox("Curve UI is disabled in settings. All handles are disabled too.", MessageType.Warning, !settings.ShowCurve);

            // Custom fields
            var warning = "";
            BGEditorUtility.Assign(ref customUi, () => new BGTableView("Custom fields", new[] {"#", "Name", "Type", "?", "Delete"}, new[] {5, 40, 40, 5, 10}, () =>
            {
                //add row
                customUi.NextColumn(rect => EditorGUI.LabelField(rect, "Name"), 12);
                customUi.NextColumn(rect => newFieldName = EditorGUI.TextField(rect, newFieldName), 28);
                customUi.NextColumn(rect => BGEditorUtility.PopupField(rect, newFieldType, @enum => newFieldType = (BGCurvePointField.TypeEnum)@enum), 50);
                customUi.NextColumn(rect =>
                {
                    if (!GUI.Button(rect, BGBinaryResources.BGAdd123)) return;

                    if (NameHasError(Curve, newFieldName)) return;

                    BGPrivateField.Invoke(Curve, BGCurve.MethodAddField, newFieldName, newFieldType, (Func<BGCurvePointField>)(() => Undo.AddComponent<BGCurvePointField>(Curve.gameObject)));
                    GUIUtility.hotControl = 0;
                    GUIUtility.ExitGUI();
                }, 10);

                customUi.NextRow();

                if (customFields == null || customFields.Length == 0) customUi.NextRow("Name should be 16 chars max, starts with a letter and contain English chars and numbers only.");
                else
                {
                    //header
                    customUi.DrawHeaders();

                    //fields
                    var quaternionWithHandlesCount = 0;

                    BGEditorUtility.ChangeCheck(() =>
                    {
                        foreach (var customField in customFields)
                        {
                            if (customField.Field.Type == BGCurvePointField.TypeEnum.Quaternion && BGPrivateField.GetHandlesType(customField.Field) != 0) quaternionWithHandlesCount++;
                            customField.Ui(customUi);
                        }
                    }, SceneView.RepaintAll);

                    if (quaternionWithHandlesCount > 1) warning = "You have more than one Quaternion field with Handles enabled. Only first field will be shown in Scene View";
                    //footer
                    customUi.NextRow("?- Show in Points Menu/Scene View");
                }
            }));

            // System fields
            BGEditorUtility.Assign(ref systemUi, () => new BGTableView("System fields", new[] {"Name", "Value"}, new[] {LabelWidth, 100 - LabelWidth}, () =>
            {
                BGEditorUtility.ChangeCheck(() =>
                {
                    foreach (var field in systemFields) field.Ui(systemUi);
                }, SceneView.RepaintAll);
            }));

            BGEditorUtility.Assign(ref systemFields, () => new[]
            {
                (SystemField) new SystemFieldPosition(settings),
                new SystemFieldControls(settings),
                new SystemFieldControlsType(settings),
                new SystemFieldTransform(settings),
            });

            var fields = Curve.Fields;
            var hasFields = fields != null && fields.Length > 0;

            if (hasFields && (customFields == null || customFields.Length != fields.Length) || !hasFields && customFields != null && customFields.Length != fields.Length)
            {
                customFields = new PointField[fields.Length];

                for (var i = 0; i < fields.Length; i++) customFields[i] = new PointField(fields[i], i, BGBinaryResources.BGDelete123);
            }


            //warnings
            BGEditorUtility.HelpBox("All handles for positions are disabled.", MessageType.Warning, settings.HandlesSettings.Disabled);
            BGEditorUtility.HelpBox("All handles for controls are disabled.", MessageType.Warning, settings.ControlHandlesSettings.Disabled);

            //====================== Custom fields
            customUi.OnGui();
            
            //warnings
            BGEditorUtility.HelpBox(warning, MessageType.Warning, warning.Length > 0);

            //====================== System fields
            systemUi.OnGui();
            GUILayout.Space(4);
        }

        private static bool NameHasError(BGCurve curve, string name)
        {
            var error = BGCurvePointField.CheckName(curve, name);
            if (error == null) return false;

            BGEditorUtility.Inform("Error", error);
            return true;
        }


        public override string GetStickerMessage(ref MessageType type)
        {
            return "" + Curve.FieldsCount;
        }

        public override void OnSettingsLoad()
        {
            systemFields = null;
        }


        public override void OnSceneGui(Plane[] frustum)
        {
            if (Curve.PointsCount == 0) return;

            //show handles (including labels) if any
            PointField.OnSceneGui(frustum, Curve, Settings, editorSelection);
        }

        //===================================================   UI Builder (idea.. refactor it later)

        //===========================================================================================  Custom fields
        private sealed class PointField
        {
            private enum HandlesType
            {
                None,
                Label,
                Direction,
                DistanceFromPoint,
                Bounds,
                BoundsAroundPoint,
                Link,
                Rotation
            }

            private readonly BGCurvePointField field;
            private readonly int index;
            private readonly Texture2D deleteIcon;

            private static readonly Dictionary<BGCurvePointField.TypeEnum, bool> Type2SupportHandles = new Dictionary<BGCurvePointField.TypeEnum, bool>();
            private static readonly Dictionary<BGCurvePointField.TypeEnum, Enum[]> Type2Handles = new Dictionary<BGCurvePointField.TypeEnum, Enum[]>();

            private static readonly Func<BGCurvePointField, bool> FieldWithHandlesPredicate =
                field => BGPrivateField.GetShowHandles(field) && SupportHandles(field.Type) && BGPrivateField.GetHandlesType(field) != 0;

            private static readonly Func<BGCurvePointField, bool> FieldWithLabelPredicate = field => BGPrivateField.GetHandlesType(field) == (int) HandlesType.Label;

            private static Color[] handlesColor;
            private static GUIStyle labelStyle;
            private static GUIStyle selectedlabelStyle;

            private static bool[] visiblePoints;
            private static Color32 latestLabelBackColor;

            public BGCurvePointField Field
            {
                get { return field; }
            }

            static PointField()
            {
                Register(BGCurvePointField.TypeEnum.Bool, new Enum[] {HandlesType.None, HandlesType.Label});
                Register(BGCurvePointField.TypeEnum.Int, new Enum[] {HandlesType.None, HandlesType.Label});
                Register(BGCurvePointField.TypeEnum.Float, new Enum[] {HandlesType.None, HandlesType.Label, HandlesType.DistanceFromPoint});
                Register(BGCurvePointField.TypeEnum.String, new Enum[] {HandlesType.None, HandlesType.Label});

                Register(BGCurvePointField.TypeEnum.Vector3, new Enum[] {HandlesType.None, HandlesType.Label, HandlesType.Direction, HandlesType.BoundsAroundPoint});
                Register(BGCurvePointField.TypeEnum.Bounds, new Enum[] {HandlesType.None, HandlesType.Bounds});
                Register(BGCurvePointField.TypeEnum.Quaternion, new Enum[] {HandlesType.None, HandlesType.Rotation});
                Register(BGCurvePointField.TypeEnum.Color, new Enum[] {HandlesType.None, HandlesType.Label});

                Register(BGCurvePointField.TypeEnum.GameObject, new Enum[] {HandlesType.None, HandlesType.Label, HandlesType.Link});
                Register(BGCurvePointField.TypeEnum.Component, new Enum[] {HandlesType.None, HandlesType.Label});

                Register(BGCurvePointField.TypeEnum.BGCurve, new Enum[] {HandlesType.None, HandlesType.Link});
                Register(BGCurvePointField.TypeEnum.BGCurvePointComponent, new Enum[] {HandlesType.None, HandlesType.Label, HandlesType.Link});
                Register(BGCurvePointField.TypeEnum.BGCurvePointGO, new Enum[] {HandlesType.None, HandlesType.Label, HandlesType.Link});
            }

            private static void Register(BGCurvePointField.TypeEnum typeEnum, Enum[] handlesTypes)
            {
                Type2SupportHandles[typeEnum] = true;
                Type2Handles[typeEnum] = handlesTypes;
            }

            public PointField(BGCurvePointField field, int index, Texture2D deleteIcon)
            {
                this.field = field;
                this.index = index;
                this.deleteIcon = deleteIcon;
            }

            public void Ui(BGTableView ui)
            {
                var cursor = 0;

                // ==========================   First row
                //number
                ui.NextColumn("" + index, "Field's index", GetWidth(ui, ref cursor));

                //name
                ui.NextColumn(rect =>
                {
                    BGEditorUtility.TextField(rect, field.FieldName, s =>
                    {
                        if (NameHasError(field.Curve, s)) return;
                        Change(() => field.FieldName = s);
                    }, true);
                }, GetWidth(ui, ref cursor));

                //type
                ui.NextColumn(field.Type.ToString(), "Field's Type", GetWidth(ui, ref cursor));

                //show in Points menu
                ui.NextColumn(rect => BGEditorUtility.BoolField(rect, BGPrivateField.GetShowInPointsMenu(field), b => Change(() => BGPrivateField.SetShowInPointsMenu(field, b))),
                    GetWidth(ui, ref cursor));

                //delete icon
                ui.NextColumn(rect =>
                {
                    if (!GUI.Button(rect, deleteIcon) || !BGEditorUtility.Confirm("Delete", "Are you sure you want to delete '" + field.FieldName + "' field?", "Delete")) return;

                    BGPrivateField.Invoke(field.Curve, BGCurve.MethodDeleteField, field, (Action<BGCurvePointField>) Undo.DestroyObjectImmediate);

                    GUIUtility.ExitGUI();
                }, GetWidth(ui, ref cursor));

                //\r\n
                ui.NextRow();

                // ==========================   Second row
                //does not support
                if (!SupportHandles(field.Type)) return;

                ui.NextColumn("      Handles", "Field's index", 25);

                //handles type
                ui.NextColumn(
                    rect => BGEditorUtility.PopupField(rect, (HandlesType) BGPrivateField.GetHandlesType(field), Type2Handles[field.Type],
                        b => Change(() => BGPrivateField.SetHandlesType(field, (int) ((HandlesType) b)))), 30);

                //Handles color
                ui.NextColumn(rect => BGEditorUtility.ColorField(rect, BGPrivateField.GetHandlesColor(field), b => Change(() => BGPrivateField.SetHandlesColor(field, b))), 30);

                //show handles in Scene View
                ui.NextColumn(rect => BGEditorUtility.BoolField(rect, BGPrivateField.GetShowHandles(field), b => Change(() => BGPrivateField.SetShowHandles(field, b))), 5);

                //empty column under delete button
                ui.NextColumn(rect => EditorGUI.LabelField(rect, ""), 10);

                //\r\n
                ui.NextRow();
            }

            private void Change(Action action)
            {
                Undo.RecordObject(field, "Field change");
                action();
                EditorUtility.SetDirty(field);
            }

            private static bool SupportHandles(BGCurvePointField.TypeEnum typeEnum)
            {
                return Type2SupportHandles.ContainsKey(typeEnum);
            }

            private static int GetWidth(BGTableView ui, ref int cursor)
            {
                var widthInPercent = ui.Sizes[cursor];
                cursor++;
                return widthInPercent;
            }

            public static void OnSceneGui(Plane[] frustum, BGCurve curve, BGCurveSettings settings, BGCurveEditorPointsSelection editorSelection)
            {
                Array.Resize(ref visiblePoints, curve.PointsCount);
                curve.ForEach((point, i, count) => visiblePoints[i] = GeometryUtility.TestPlanesAABB(frustum, new Bounds(point.PositionWorld, Vector3.one)));


                var fieldsCount = curve.FieldsCount;
                var fields = curve.Fields;
                var showPointsNumbers = settings.ShowLabels;

                var fieldsWithHandlesCount = 0;
                var fieldsWithLabelCount = 0;
                if (fieldsCount > 0)
                {
                    fieldsWithHandlesCount = fields.Count(FieldWithHandlesPredicate);
                    if (fieldsWithHandlesCount > 0)
                    {
                        Array.Resize(ref handlesColor, fieldsWithHandlesCount);
                        var cursor = 0;
                        for (var i = 0; i < fieldsCount; i++)
                        {
                            var f = fields[i];
                            if (!FieldWithHandlesPredicate(f)) continue;

                            if (FieldWithLabelPredicate(f)) fieldsWithLabelCount++;
                            handlesColor[cursor++] = BGPrivateField.GetHandlesColor(f);
                        }
                    }
                }

                // nothing to show
                if (!showPointsNumbers && fieldsWithHandlesCount == 0) return;


                if (fieldsWithHandlesCount > 0)
                {
                    //not a label
                    curve.ForEach((point, i, length) =>
                    {
                        if (!visiblePoints[i] || !settings.RestrictGizmozSettings.IsShowing(i)) return;

                        var pos = point.PositionWorld;

                        var quanterionShown = false;
                        var fieldCursor = 0;
                        for (var j = 0; j < fields.Length; j++)
                        {
                            var field = fields[j];
                            var handlesType = (HandlesType) BGPrivateField.GetHandlesType(field);

                            if (handlesType == 0) continue;

                            if (handlesType == HandlesType.Label)
                            {
                                fieldCursor++;
                                continue;
                            }

                            var color = handlesColor[fieldCursor++];
                            switch (handlesType)
                            {
                                case HandlesType.DistanceFromPoint:
                                    BGEditorUtility.SwapHandlesColor(color, () =>
                                    {
#if UNITY_5_6_OR_NEWER
							Handles.CircleHandleCap(0, pos, Quaternion.LookRotation(SceneView.currentDrawingSceneView.camera.transform.position - pos),
								point.GetField<float>(field.FieldName), EventType.Repaint);
#else
							Handles.CircleCap(0, pos, Quaternion.LookRotation(SceneView.currentDrawingSceneView.camera.transform.position - pos),
		                                    point.GetField<float>(field.FieldName));
#endif
                                    }
							  );
                                    break;
                                case HandlesType.BoundsAroundPoint:
                                    Bounds bounds;
                                    switch (field.Type)
                                    {
                                        case BGCurvePointField.TypeEnum.Bounds:
                                            bounds = point.GetField<Bounds>(field.FieldName);
                                            bounds.center = pos;
                                            break;
                                        default:
                                            //vector3
                                            var vector3 = point.GetField<Vector3>(field.FieldName);
                                            bounds = new Bounds(pos, vector3);
                                            break;
                                    }
                                    BGEditorUtility.DrawBound(bounds, new Color(color.r, color.g, color.b, 0.05f), color);
                                    break;
                                case HandlesType.Bounds:
                                    var boundsValue = point.GetField<Bounds>(field.FieldName);
                                    if (boundsValue.extents != Vector3.zero)
                                    {
                                        BGEditorUtility.DrawBound(boundsValue, new Color(color.r, color.g, color.b, 0.05f), color);
                                        BGEditorUtility.SwapHandlesColor(color, () => Handles.DrawDottedLine(boundsValue.center, pos, 4));
                                    }
                                    break;
                                case HandlesType.Direction:
                                    var vector3Value = point.GetField<Vector3>(field.FieldName);
                                    if (vector3Value != Vector3.zero)
                                        BGEditorUtility.SwapHandlesColor(color, () =>
                                        {
#if UNITY_5_6_OR_NEWER
	                                        Handles.ArrowHandleCap(0, pos, Quaternion.LookRotation(vector3Value),
		                                        vector3Value.magnitude, EventType.Repaint);
#else
							    Handles.ArrowCap(0, pos, Quaternion.LookRotation(vector3Value), vector3Value.magnitude);
#endif
                                        });
                                    break;
                                case HandlesType.Rotation:
                                    if (quanterionShown) break;

                                    quanterionShown = true;

                                    var quaternionValue = point.GetField<Quaternion>(field.FieldName);
                                    if (quaternionValue.x < BGCurve.Epsilon && quaternionValue.y < BGCurve.Epsilon && quaternionValue.z < BGCurve.Epsilon && quaternionValue.w < BGCurve.Epsilon)
                                        quaternionValue = Quaternion.identity;

                                    var newValue = Handles.RotationHandle(quaternionValue, pos);
                                    point.SetField(field.FieldName, newValue);


                                    BGEditorUtility.SwapHandlesColor(color, () =>
                                    {
                                        var rotated = newValue*Vector3.forward*BGEditorUtility.GetHandleSize(pos, 2);
                                        var toPos = pos + rotated;
#if UNITY_5_6_OR_NEWER
							Handles.ArrowHandleCap(0, toPos, newValue, 1, EventType.Repaint);
#else
							Handles.ArrowCap(0, toPos, newValue, 1);
#endif
                                        Handles.DrawDottedLine(pos, toPos, 10);
                                    });
                                    break;
                                case HandlesType.Link:
                                    switch (field.Type)
                                    {
                                        case BGCurvePointField.TypeEnum.GameObject:
                                            var go = point.GetField<GameObject>(field.FieldName);
                                            if (go != null) BGEditorUtility.SwapHandlesColor(color, () => Handles.DrawDottedLine(go.transform.position, pos, 4));
                                            break;
                                        case BGCurvePointField.TypeEnum.BGCurve:
                                            var bgCurve = point.GetField<BGCurve>(field.FieldName);
                                            if (bgCurve != null) BGEditorUtility.SwapHandlesColor(color, () => Handles.DrawDottedLine(bgCurve.transform.position, pos, 4));
                                            break;
                                        case BGCurvePointField.TypeEnum.BGCurvePointComponent:
                                            var pointComponent = point.GetField<BGCurvePointComponent>(field.FieldName);
                                            if (pointComponent != null) BGEditorUtility.SwapHandlesColor(color, () => Handles.DrawDottedLine(pointComponent.PositionWorld, pos, 4));
                                            break;
                                        case BGCurvePointField.TypeEnum.BGCurvePointGO:
                                            var pointGO = point.GetField<BGCurvePointGO>(field.FieldName);
                                            if (pointGO != null) BGEditorUtility.SwapHandlesColor(color, () => Handles.DrawDottedLine(pointGO.PositionWorld, pos, 4));
                                            break;
                                    }
                                    break;
                            }
                        }
                    });
                }

                // nothing more to show
                if (!showPointsNumbers && fieldsWithLabelCount == 0) return;

                //=============================== Labels

                //styles
                var labelColor = settings.LabelColor;
                var selectedColor = settings.LabelColorSelected;
                var backColor = BGCurveSettingsForEditor.I.Get<Color32>(BGCurveSettingsForEditor.ColorForLabelBackgroundKey);

                if (labelStyle == null || labelStyle.normal.textColor != labelColor || labelStyle.normal.background == null
                    || latestLabelBackColor.r != backColor.r || latestLabelBackColor.g != backColor.g || latestLabelBackColor.b != backColor.b || latestLabelBackColor.a != backColor.a)
                {
                    latestLabelBackColor = backColor;
                    labelStyle = new GUIStyle("Label")
                    {
                        richText = true,
                        border = new RectOffset(2, 2, 2, 2),
                        clipping = TextClipping.Overflow,
                        wordWrap = false,
                        normal =
                        {
                            background = BGEditorUtility.TextureWithBorder(8, 1, backColor, new Color32(backColor.r, backColor.g, backColor.b, 255)),
                            textColor = labelColor
                        }
                    };

                    selectedlabelStyle = new GUIStyle(labelStyle)
                    {
                        normal =
                        {
                            background = BGEditorUtility.TextureWithBorder(8, 1, new Color(selectedColor.r, selectedColor.g, selectedColor.b, .1f), selectedColor),
                            textColor = selectedColor
                        }
                    };
                }


                curve.ForEach((point, i, length) =>
                {
                    if (!visiblePoints[i]) return;

                    var pos = point.PositionWorld;

                    var style = !editorSelection.Contains(point) ? labelStyle : selectedlabelStyle;
                    var text = "";

                    //point numbers and pos
                    if (showPointsNumbers)
                    {
                        text += "# : " + i + "\r\n";
                        if (settings.ShowPositions) text += "P : " + pos + "\r\n";
                    }

                    //fields
                    if (fieldsWithLabelCount > 0)
                    {
                        for (var j = 0; j < fieldsCount; j++)
                        {
                            var field = fields[j];
                            if (!FieldWithHandlesPredicate(field) || !FieldWithLabelPredicate(field)) continue;

                            text += BGEditorUtility.ColorIt(field.FieldName + " : " + point.GetField(field.FieldName, BGCurvePoint.FieldTypes.GetType(field.Type)),
                                        BGEditorUtility.ToHex(BGPrivateField.GetHandlesColor(field))) + "\r\n";
                        }
                    }

                    var normalized = (SceneView.currentDrawingSceneView.camera.transform.position - pos).normalized;
                    var handleSize = BGEditorUtility.GetHandleSize(pos, .25f);
                    var shiftLeft = -Vector3.Cross(normalized, Vector3.up);
                    var shiftBottom = Vector3.Cross(normalized, Vector3.right)*.3f;
                    Handles.Label(pos + handleSize*shiftLeft + handleSize*shiftBottom, text.Substring(0, text.Length - 2), style);
                });
            }
        }

        //===========================================================================================  Abstract system field
        private abstract class SystemField
        {
            protected readonly BGCurveSettings Settings;

            public virtual bool ShowInPointsMenu { get; set; }

            private readonly GUIContent title;

            protected SystemField(BGCurveSettings settings, GUIContent title)
            {
                this.title = title;
                Settings = settings;
            }

            public virtual void Ui(BGTableView ui)
            {
                ui.NextColumn(rect => EditorGUI.LabelField(rect, title), 100, true);
                ui.NextRow();

                // show in point menu
                NextBoolRow(ui, "Show in Points Menu", "Show in Points Menu (Points Tab)", ShowInPointsMenu, newValue => ShowInPointsMenu = newValue);
            }

            protected void NextBoolRow(BGTableView ui, string label, string tooltip, bool value, Action<bool> valueSetter)
            {
                ui.NextColumn(label, tooltip);
                ui.NextColumn(rect => BGEditorUtility.ToggleField(rect, value, valueSetter));
                ui.NextRow();
            }

            protected void NextEnumRow(BGTableView ui, string label, string tooltip, Enum value, Action<Enum> valueSetter)
            {
                ui.NextColumn(label, tooltip);
                ui.NextColumn(rect => BGEditorUtility.PopupField(rect, value, valueSetter));
                ui.NextRow();
            }

            protected void NextSliderRow(BGTableView ui, string label, string tooltip, float value, float from, float to, Action<float> valueSetter)
            {
                ui.NextColumn(label, tooltip);
                ui.NextColumn(rect => BGEditorUtility.SliderField(rect, value, @from, to, valueSetter));
                ui.NextRow();
            }

            protected void NextColorRow(BGTableView ui, string label, string tooltip, Color color, Action<Color> func)
            {
                ui.NextColumn(label, tooltip);
                ui.NextColumn(rect => BGEditorUtility.ColorField(rect, color, func));
                ui.NextRow();
            }
        }

        //===========================================================================================  Abstract Vector(s) Based Field
        private abstract class SystemVectorField : SystemField
        {
            public virtual bool ShowHandles { get; set; }

            public virtual BGCurveSettings.HandlesTypeEnum HandlesType { get; set; }

            public virtual BGCurveSettings.SettingsForHandles HandlesSettings
            {
                get { return null; }
            }

            public virtual bool ShowLabels { get; set; }
            public virtual bool ShowPositions { get; set; }
            public virtual Color LabelColor { get; set; }

            protected SystemVectorField(BGCurveSettings settings, GUIContent title)
                : base(settings, title)
            {
            }

            private void Next3BoolRow(BGTableView ui, string label, string tooltip,
                string xLabel, string yLabel, string zLabel,
                bool xValue, bool yValue, bool zValue,
                Action<bool> xSetter, Action<bool> ySetter, Action<bool> zSetter)
            {
                ui.NextColumn(label, tooltip);
                const int widthInPercent = (100 - LabelWidth)/3;
                ui.NextColumn(xLabel, rect => BGEditorUtility.ToggleField(rect, xValue, xSetter), widthInPercent, ui.CenteredLabelStyle);
                ui.NextColumn(yLabel, rect => BGEditorUtility.ToggleField(rect, yValue, ySetter), widthInPercent, ui.CenteredLabelStyle);
                ui.NextColumn(zLabel, rect => BGEditorUtility.ToggleField(rect, zValue, zSetter), widthInPercent, ui.CenteredLabelStyle);
                ui.NextRow();
            }

            public override void Ui(BGTableView ui)
            {
                base.Ui(ui);

                NextBoolRow(ui, "Show Handles", "Show handles in Scene View", ShowHandles, newValue => ShowHandles = newValue);
                if (ShowHandles)
                {
                    NextEnumRow(ui, "    Handles Types", "Type for the handle", HandlesType, newValue => HandlesType = (BGCurveSettings.HandlesTypeEnum) newValue);

                    if (HandlesType == BGCurveSettings.HandlesTypeEnum.Configurable)
                    {
                        NextSliderRow(ui, "        Handles Axis Size", "Size of the Axis handles in Scene View", HandlesSettings.AxisScale, .5f, 1.5f, newValue => HandlesSettings.AxisScale = newValue);
                        NextSliderRow(ui, "        Handles Plane Size", "Size of the Plane handles in Scene View", HandlesSettings.PlanesScale, .5f, 1.5f,
                            newValue => HandlesSettings.PlanesScale = newValue);
                        NextSliderRow(ui, "        Handles Alpha", "Alpha color of handles in Scene View", HandlesSettings.Alpha, .5f, 1f, newValue => HandlesSettings.Alpha = newValue);

                        Next3BoolRow(ui, "        Remove Axis", "Remove Axis handles",
                            "X", "Y", "Z",
                            HandlesSettings.RemoveX, HandlesSettings.RemoveY, HandlesSettings.RemoveZ,
                            newValue => HandlesSettings.RemoveX = newValue, newValue => HandlesSettings.RemoveY = newValue, newValue => HandlesSettings.RemoveZ = newValue
                        );

                        Next3BoolRow(ui, "        Remove Planes", "Remove Planes handles",
                            "XZ", "XY", "YZ",
                            HandlesSettings.RemoveXZ, HandlesSettings.RemoveXY, HandlesSettings.RemoveYZ,
                            newValue => HandlesSettings.RemoveXZ = newValue, newValue => HandlesSettings.RemoveXY = newValue, newValue => HandlesSettings.RemoveYZ = newValue
                        );
                    }
                }

                NextBoolRow(ui, "Show Labels", "Show labels in Scene View", ShowLabels, newValue => ShowLabels = newValue);
                if (ShowLabels)
                {
                    NextBoolRow(ui, "    Show Positions", "Show vector positions in Scene View", ShowPositions, newValue => ShowPositions = newValue);
                    NextColorRow(ui, "    Labels color", "Color for labels in Scene View", LabelColor, newValue => LabelColor = newValue);
                    AdditionalLabelFields(ui);
                }
            }

            protected virtual void AdditionalLabelFields(BGTableView ui)
            {
            }
        }

        //===========================================================================================  Position
        private sealed class SystemFieldPosition : SystemVectorField
        {
            public override bool ShowHandles
            {
                get { return Settings.ShowHandles; }
                set { Settings.ShowHandles = value; }
            }

            public override BGCurveSettings.SettingsForHandles HandlesSettings
            {
                get { return Settings.HandlesSettings; }
            }

            public override BGCurveSettings.HandlesTypeEnum HandlesType
            {
                get { return Settings.HandlesType; }
                set { Settings.HandlesType = value; }
            }

            public override bool ShowInPointsMenu
            {
                get { return Settings.ShowPointPosition; }
                set { Settings.ShowPointPosition = value; }
            }

            public override bool ShowLabels
            {
                get { return Settings.ShowLabels; }
                set { Settings.ShowLabels = value; }
            }

            public override bool ShowPositions
            {
                get { return Settings.ShowPositions; }
                set { Settings.ShowPositions = value; }
            }

            public override Color LabelColor
            {
                get { return Settings.LabelColor; }
                set { Settings.LabelColor = value; }
            }

            private Color LabelColorSelected
            {
                get { return Settings.LabelColorSelected; }
                set { Settings.LabelColorSelected = value; }
            }

            private bool ShowSpheres
            {
                get { return Settings.ShowSpheres; }
                set { Settings.ShowSpheres = value; }
            }

            private float SpheresRadius
            {
                get { return Settings.SphereRadius; }
                set { Settings.SphereRadius = value; }
            }

            private Color SpheresColor
            {
                get { return Settings.SphereColor; }
                set { Settings.SphereColor = value; }
            }

            public SystemFieldPosition(BGCurveSettings settings)
                : base(settings, new GUIContent("#1. Positions", "Point's positions"))
            {
            }

            protected override void AdditionalLabelFields(BGTableView ui)
            {
                NextColorRow(ui, "Labels color for selected", "Color for labels in Scene View, when they are selected", LabelColorSelected, newValue => LabelColorSelected = newValue);
            }

            public override void Ui(BGTableView ui)
            {
                base.Ui(ui);

                NextBoolRow(ui, "Show spheres", "Show spheres at points positions in Scene View", ShowSpheres, b => ShowSpheres = b);
                if (ShowSpheres)
                {
                    NextSliderRow(ui, "    Sphere radius", "Sphere radius in Scene View", SpheresRadius, .01f, 1, f => SpheresRadius = f);
                    NextColorRow(ui, "    Sphere color", "Sphere color in Scene View", SpheresColor, f => SpheresColor = f);
                }
            }
        }

        //===========================================================================================  Controls
        private sealed class SystemFieldControls : SystemVectorField
        {
            public override bool ShowHandles
            {
                get { return Settings.ShowControlHandles; }
                set { Settings.ShowControlHandles = value; }
            }

            public override BGCurveSettings.HandlesTypeEnum HandlesType
            {
                get { return Settings.ControlHandlesType; }
                set { Settings.ControlHandlesType = value; }
            }


            public override BGCurveSettings.SettingsForHandles HandlesSettings
            {
                get { return Settings.ControlHandlesSettings; }
            }

            public override bool ShowInPointsMenu
            {
                get { return Settings.ShowPointControlPositions; }
                set { Settings.ShowPointControlPositions = value; }
            }

            public override bool ShowLabels
            {
                get { return Settings.ShowControlLabels; }
                set { Settings.ShowControlLabels = value; }
            }

            public override bool ShowPositions
            {
                get { return Settings.ShowControlPositions; }
                set { Settings.ShowControlPositions = value; }
            }

            private Color HandlesColor
            {
                get { return Settings.ControlHandlesColor; }
                set { Settings.ControlHandlesColor = value; }
            }

            public override Color LabelColor
            {
                get { return Settings.LabelControlColor; }
                set { Settings.LabelControlColor = value; }
            }


            public SystemFieldControls(BGCurveSettings settings)
                : base(settings, new GUIContent("#2. Controls", "Point's Bezier control positions"))
            {
            }

            public override void Ui(BGTableView ui)
            {
                base.Ui(ui);

                NextColorRow(ui, "Handles Color", "Color of handles in Scene View", HandlesColor, newValue => HandlesColor = newValue);
            }
        }


        //===========================================================================================  Control Type
        private sealed class SystemFieldControlsType : SystemField
        {
            public override bool ShowInPointsMenu
            {
                get { return Settings.ShowPointControlType; }
                set { Settings.ShowPointControlType = value; }
            }

            public SystemFieldControlsType(BGCurveSettings settings)
                : base(settings, new GUIContent("#3. Control Type", "Point's control type (Absent, Bezier)"))
            {
            }
        }

        //===========================================================================================  Transform Field
        private sealed class SystemFieldTransform : SystemField
        {
            public override bool ShowInPointsMenu
            {
                get { return Settings.ShowTransformField; }
                set { Settings.ShowTransformField = value; }
            }

            public SystemFieldTransform(BGCurveSettings settings)
                : base(settings, new GUIContent("#4. Transform", "Transform to use as point's position"))
            {
            }
        }
    }
}