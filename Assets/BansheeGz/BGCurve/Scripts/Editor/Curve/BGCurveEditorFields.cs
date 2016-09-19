using System;
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

        private TableUi systemUi;
        private TableUi customUi;

        private SystemField[] systemFields;

        public BGCurveEditorFields(BGCurveEditor editor, SerializedObject curveObject) : base(editor, curveObject, BGEditorUtility.LoadTexture2D(BGEditorUtility.Image.BGFields123))
        {
        }


        // ================================================================================ Inspector
        public override void OnInspectorGui()
        {
            var settings = BGPrivateField.GetSettings(Curve);

            BGEditorUtility.HelpBox("Curve UI is disabled in settings. All handles are disabled too.", MessageType.Warning, !settings.ShowCurve);

            BGEditorUtility.Assign(ref systemUi, () => new TableUi("System fields", new[] {"Name", "Value"}, new[] {LabelWidth, 100 - LabelWidth}));
            BGEditorUtility.Assign(ref customUi, () => new TableUi("Custom fields", new[] {"Name", "Value"}, new[] {LabelWidth, 100 - LabelWidth}));

            BGEditorUtility.Assign(ref systemFields, () => new[]
            {
                (SystemField) new SystemFieldPosition(settings),
                new SystemFieldControls(settings),
                new SystemFieldControlsType(settings),
            });

            //warnings
            BGEditorUtility.HelpBox("All handles for positions are disabled.", MessageType.Warning, settings.HandlesSettings.Disabled);
            BGEditorUtility.HelpBox("All handles for controls are disabled.", MessageType.Warning, settings.ControlHandlesSettings.Disabled);

            systemUi.Init();

            foreach (var field in systemFields) field.Ui(systemUi);

            //inform layout manager
            GUILayoutUtility.GetRect(systemUi.Width, systemUi.Height);

            customUi.Init();
            customUi.NextRow("Custom fields are not supported in this version.");

            GUILayoutUtility.GetRect(customUi.Width, customUi.Height);

            GUILayout.Space(4);
        }


        public override string GetStickerMessage(ref MessageType type)
        {
            return null;
        }

        //===================================================   UI Builder (idea.. refactor it later)
        private sealed class TableUi
        {
            private const float Offset = 10;
            private const int TitleOffset = 10;

//            private readonly string[] headers; //headers
            private readonly int[] sizes; //column sizes percentages
            private readonly float height; //line height

            private readonly GUIStyle headerStyle;
            private readonly GUIStyle cellStyle;
            private readonly GUIStyle titleStyle;
            private readonly GUIStyle centeredLabelStyle;

            private readonly string title;

            public GUIStyle CenteredLabelStyle
            {
                get { return centeredLabelStyle; }
            }


            private Rect lastRect; //last rect from Layout manager

            public float Width { get; private set; }

            public float Height
            {
                get { return rows*height + Offset; }
            }

            private float rows; //not data, but ui lines 
            private Vector2 cursor;
            private int currentColumn;

            public TableUi(string title, string[] headers, int[] sizes)
            {
//                this.headers = headers;
                this.sizes = sizes;
                this.title = title;

                headerStyle = new GUIStyle(GetStyle(BGEditorUtility.Image.BGBoxWithBorder123));
                centeredLabelStyle = new GUIStyle("Label") {alignment = TextAnchor.MiddleCenter};

                cellStyle = GetStyle(BGEditorUtility.Image.BGTableCell123);
                titleStyle = GetStyle(BGEditorUtility.Image.BGTableTitle123);
                titleStyle.border = titleStyle.padding = new RectOffset(TitleOffset, TitleOffset, 2, 2);

                height = cellStyle.CalcSize(new GUIContent("Test")).y;

                Init();
            }

            public void Init()
            {
                //---------------------------------- init sizes
                lastRect = GUILayoutUtility.GetLastRect();
                Width = lastRect.width;
                cursor = new Vector2(lastRect.xMin, lastRect.yMax + Offset);
                rows = 0;
                currentColumn = 0;

                //---------------------------------- title
                EditorGUI.LabelField(new Rect(cursor.x, cursor.y, titleStyle.CalcSize(new GUIContent(title)).x + TitleOffset*2, height), title, titleStyle);
                NextRow();
            }

/*
            //---------------------------------- headers
            private void DrawHeaders()
            {
                foreach (var header in headers) NextColumn(rect => EditorGUI.LabelField(rect, header), true);

                NextRow();
            }
*/

            private static GUIStyle GetStyle(BGEditorUtility.Image background)
            {
                return new GUIStyle("Label")
                {
                    padding = new RectOffset(2, 2, 2, 2),
                    border = new RectOffset(2, 2, 2, 2),
                    normal = new GUIStyleState
                    {
                        background = BGEditorUtility.LoadTexture2D(background)
                    }
                };
            }

            public void NextColumn(string label, Action<Rect> action, bool header = false, int widthInPercent = 0, GUIStyle labelStyle = null)
            {
                var columnWidth = Width*(widthInPercent > 0 ? widthInPercent : sizes[currentColumn])/100f;

                var rect = new Rect(cursor.x, cursor.y, columnWidth, height);
                EditorGUI.LabelField(rect, "", header ? headerStyle : cellStyle);

                if (label != null)
                {
                    rect.width /= 2;
                    EditorGUI.LabelField(rect, label, labelStyle ?? GUI.skin.label);
                    rect.x += rect.width;
                }

                if (action != null) action(rect);

                cursor.x += columnWidth;
                currentColumn++;
            }

            public void NextColumn(Action<Rect> action, bool header = false, int widthInPercent = 0)
            {
                NextColumn(null, action, header, widthInPercent);
            }

            public void NextColumn(string label, string description, bool header = false, int widthInPercent = 0)
            {
                NextColumn(rect => EditorGUI.LabelField(rect, new GUIContent(label, description)), header, widthInPercent);
            }

            public void NextRow()
            {
                cursor.x = lastRect.xMin;
                cursor.y += height;
                currentColumn = 0;
                rows++;
            }

            public void NextRow(string message)
            {
                EditorGUI.LabelField(new Rect(cursor.x, cursor.y, Width, height), message, cellStyle);
                NextRow();
            }
        }

        //===========================================================================================  Abstract system field
        private abstract class SystemField
        {
            protected readonly BGCurveSettings Settings;

            public virtual bool ShowInPointsMenu { get; set; }

            private readonly string name;

            protected SystemField(BGCurveSettings settings, string name)
            {
                this.name = name;
                Settings = settings;
            }

            public virtual void Ui(TableUi ui)
            {
                ui.NextColumn(rect => EditorGUI.LabelField(rect, "   " + name), true, 100);
                ui.NextRow();

                // show in point menu
                NextBoolRow(ui, "Show in Points Menu", "Show in Points Menu (Points Tab)", ShowInPointsMenu, newValue => ShowInPointsMenu = newValue);
            }

            protected void NextBoolRow(TableUi ui, string label, string tooltip, bool value, Action<bool> valueSetter)
            {
                ui.NextColumn(label, tooltip);
                ui.NextColumn(rect => BGEditorUtility.ToggleField(rect, value, valueSetter));
                ui.NextRow();
            }

            protected void NextEnumRow(TableUi ui, string label, string tooltip, Enum value, Action<Enum> valueSetter)
            {
                ui.NextColumn(label, tooltip);
                ui.NextColumn(rect => BGEditorUtility.PopupField(rect, value, valueSetter));
                ui.NextRow();
            }

            protected void NextSliderRow(TableUi ui, string label, string tooltip, float value, float from, float to, Action<float> valueSetter)
            {
                ui.NextColumn(label, tooltip);
                ui.NextColumn(rect => BGEditorUtility.SliderField(rect, value, from, to, valueSetter));
                ui.NextRow();
            }

            protected void NextColorRow(TableUi ui, string label, string tooltip, Color color, Action<Color> func)
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

            protected SystemVectorField(BGCurveSettings settings, string name)
                : base(settings, name)
            {
            }

            private void Next3BoolRow(TableUi ui, string label, string tooltip,
                string xLabel, string yLabel, string zLabel,
                bool xValue, bool yValue, bool zValue,
                Action<bool> xSetter, Action<bool> ySetter, Action<bool> zSetter)
            {
                ui.NextColumn(label, tooltip);
                const int widthInPercent = (100 - LabelWidth)/3;
                ui.NextColumn(xLabel, rect => BGEditorUtility.ToggleField(rect, xValue, xSetter), widthInPercent: widthInPercent, labelStyle: ui.CenteredLabelStyle);
                ui.NextColumn(yLabel, rect => BGEditorUtility.ToggleField(rect, yValue, ySetter), widthInPercent: widthInPercent, labelStyle: ui.CenteredLabelStyle);
                ui.NextColumn(zLabel, rect => BGEditorUtility.ToggleField(rect, zValue, zSetter), widthInPercent: widthInPercent, labelStyle: ui.CenteredLabelStyle);
                ui.NextRow();
            }

            public override void Ui(TableUi ui)
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

            protected virtual void AdditionalLabelFields(TableUi ui)
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
                : base(settings, "Positions")
            {
            }

            protected override void AdditionalLabelFields(TableUi ui)
            {
                NextColorRow(ui, "Labels color for selected", "Color for labels in Scene View, when they are selected", LabelColorSelected, newValue => LabelColorSelected = newValue);
            }

            public override void Ui(TableUi ui)
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
                : base(settings, "Controls")
            {
            }

            public override void Ui(TableUi ui)
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
                : base(settings, "Control Type")
            {
            }
        }
    }
}