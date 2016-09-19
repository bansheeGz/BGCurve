using System.Linq;
using UnityEngine;
using BansheeGz.BGSpline.Curve;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
    //GUI for all points
    public class BGCurveEditorPoints : BGCurveEditorTab
    {
        // ====================================== Static const
        private readonly Texture2D convertAll2D;
        private readonly Texture2D addPointIcon;

        private static readonly Color32 HiddenPointMenuColor = new Color32(144, 195, 212, 255);

        // ====================================== Fields
        private readonly SerializedProperty closedProperty;
        private readonly SerializedProperty controlTypeProperty;
        private readonly SerializedProperty mode2DProperty;

        //selected points
        private readonly BGCurveEditorPointsSelection editorSelection;
        //point
        private readonly BGCurveEditorPoint editorPoint;
        //painting a curve in the scene
        private BGCurvePainterGizmo painter;

        private bool closeChanged;
        private bool mode2DChanged;

        private readonly BGSceneViewOverlay overlay;

        public BGCurveEditorPoints(BGCurveEditor editor, SerializedObject curveObject) : base(editor, curveObject, BGEditorUtility.LoadTexture2D(BGEditorUtility.Image.BGPoints123))
        {
            //textures
            convertAll2D = BGEditorUtility.LoadTexture2D(BGEditorUtility.Image.BGConvertAll123);
            addPointIcon = BGEditorUtility.LoadTexture2D(BGEditorUtility.Image.BGAdd123);

            //selection
            editorSelection = new BGCurveEditorPointsSelection(Curve, this);

            //point
            editorPoint = new BGCurveEditorPoint(this, editorSelection);

            //closed or not
            closedProperty = curveObject.FindProperty("closed");

            //2d mode
            mode2DProperty = curveObject.FindProperty("mode2D");

            //settings
            controlTypeProperty = curveObject.FindProperty("settings").FindPropertyRelative("controlType");

            //Context menu
            overlay = new BGSceneViewOverlay(this, editorSelection);
        }

        // ================================================================================ Inspector
        public override void OnInspectorGui()
        {
            var settings = Settings;

            editorSelection.Reset();

            // ======================================== Top section
            InspectorTopSection();

            // ======================================== Points
            GUILayout.Space(5);

            if (Curve.PointsCount > 0)
            {
                var temp = BGCurveSettingsForEditor.DisableInspectorPointMenu;
                BGCurveSettingsForEditor.DisableInspectorPointMenu = BGEditorUtility.ButtonOnOff(ref temp, "Points menu [" + Curve.PointsCount + "]", "Show points in Editor inspector",
                    HiddenPointMenuColor,
                    new GUIContent("Show", "Click to show points menu"),
                    new GUIContent("Hide", "Click to hide points menu"), () =>
                    {
                        const string title = "Reverse points";
                        if (GUILayout.Button(new GUIContent(title, "Reverse all points, but keep curve intact")))
                        {
                            if (Curve.PointsCount < 2)
                            {
                                BGEditorUtility.Inform(title, "There should be at least 2 points. Curve has " + Curve.PointsCount);
                                return;
                            }
                            if (!BGEditorUtility.Confirm(title, "Are you sure you want to reverse the order of " + Curve.PointsCount + " points? Curve will remain intact.", "Reverse")) return;

                            Curve.Reverse();
                            EditorUtility.SetDirty(Curve);
                        }
                    });

                if (!BGCurveSettingsForEditor.DisableInspectorPointMenu) BGEditorUtility.VerticalBox(() => Curve.ForEach((point, index, count) => editorPoint.OnInspectorGUI(point, index, settings)));

                // ======================================== Selections operations
                editorSelection.InspectorSelectionOperations();

                //warning
                BGEditorUtility.HelpBox("Selection mode is on", MessageType.Warning, !editorSelection.Changed && editorSelection.HasSelected());
            }
            else
            {
                BGEditorUtility.HorizontalBox(() =>
                {
                    EditorGUILayout.LabelField("No points!");
                    if (BGEditorUtility.ButtonWithIcon(addPointIcon, "Add new point at (0,0,0) local coordinates"))
                        Curve.AddPoint(new BGCurvePoint(Curve, Vector3.zero, settings.ControlType, Vector3.right, Vector3.left));
                });
            }

            if (editorSelection.Changed) EditorUtility.SetDirty(Curve);
        }


        private void InspectorTopSection()
        {
            if (Curve.PointsCount == 0)
                EditorGUILayout.HelpBox(
                    "1) Ctrl + LeftClick in scene view to add a point and snap it to  "
                    + "\r\n    a) 3D mode: mesh with collider"
                    + "\r\n    b) 2D mode: curve's 2D plane."
                    + "\r\n"
                    + "\r\n2) Ctrl + Shift + LeftClick in Scene View to add a point unconditionally at some distance, specified in the settings."
                    + "\r\n"
                    + "\r\n3) Hold control over existing point or selection to access Scene View menu"
                    + "\r\n"
                    + "\r\n4) Hold shift + drag to use rectangular selection in Scene View"
                    , MessageType.Info);

            EditorGUILayout.PropertyField(closedProperty);
            EditorGUILayout.PropertyField(mode2DProperty);

            BGEditorUtility.Horizontal(() =>
            {
                EditorGUILayout.PropertyField(controlTypeProperty);

                if (!BGEditorUtility.ButtonWithIcon(convertAll2D, "Convert control types for all existing points ", 44)) return;

                var settings = Settings;

                foreach (var point in Curve.Points.Where(point => point.ControlType != settings.ControlType)) point.ControlType = settings.ControlType;
            });
        }

        // ================================================================================ Scene
        public override void OnSceneGui()
        {
            var settings = Settings;

            var rotation = Tools.pivotRotation == PivotRotation.Global ? Quaternion.identity : Curve.transform.rotation;

            if (Curve.PointsCount != 0 && settings.VRay)
            {
                painter = painter ?? new BGCurvePainterHandles(Editor.Math);
                painter.DrawCurve();
            }

            editorPoint.OnSceneGUIStart(settings);

            var frustum = GeometryUtility.CalculateFrustumPlanes(SceneView.currentDrawingSceneView.camera);

            Curve.ForEach((point, index, count) => editorPoint.OnSceneGUI(point, index, settings, rotation, frustum));

            //tangents
            if (settings.ShowCurve && settings.ShowTangents && Editor.Math.SectionsCount > 0 && Editor.Math.IsCalculated(BGCurveBaseMath.Field.Tangent))
            {
                BGEditorUtility.SwapHandlesColor(settings.TangentsColor, () =>
                {
                    var math = Editor.Math;
                    var sectionsCount = math.SectionsCount;
                    var sections = math.SectionInfos;

                    for (var i = 0; i < sectionsCount; i++)
                    {
                        var section = sections[i];
                        var points = section.Points;
                        ShowTangent(points[0].Position, points[0].Tangent, settings.TangentsSize);
                        if (settings.TangentsPerSection > 1)
                        {
                            var sectionLength = section.Distance;
                            var part = sectionLength/settings.TangentsPerSection;
                            for (var j = 1; j < settings.TangentsPerSection; j++)
                            {
                                var distanceWithinSection = part*j;
                                Vector3 position;
                                Vector3 tangent;
                                section.CalcByDistance(distanceWithinSection, out position, out tangent, true, true);
                                ShowTangent(position, tangent, settings.TangentsSize);
                            }
                        }
                    }
                });
            }


            editorSelection.Scene(rotation);

            var currentEvent = Event.current;

            overlay.Process(currentEvent);

            editorSelection.Process(currentEvent);
        }

        private static void ShowTangent(Vector3 position, Vector3 tangent, float size)
        {
            if (tangent.sqrMagnitude > 0.0001f) Handles.ArrowCap(0, position, Quaternion.LookRotation(tangent), BGEditorUtility.GetHandleSize(position, size));
        }


        public override void OnBeforeApply()
        {
            closeChanged = false;
            if (Editor.Curve.Closed != closedProperty.boolValue)
            {
                closeChanged = true;
                Curve.FireBeforeChange("closed changed");
            }

            mode2DChanged = false;
            if ((int) Editor.Curve.Mode2D != mode2DProperty.enumValueIndex)
            {
                mode2DChanged = true;
                Curve.FireBeforeChange("2d mode changed");
            }
        }

        public override void OnApply()
        {
            if (closeChanged) Curve.FireChange(Curve.UseEventsArgs ? new BGCurveChangedArgs(Curve, BGCurveChangedArgs.ChangeTypeEnum.Points) : null);

            if (mode2DChanged)
            {
                //force points recalc
                Curve.Apply2D(Curve.Mode2D);

                if (BGEditorUtility.Confirm("Editor handles change", "Do you want to adjust configurable Editor handles (in Scene View) to chosen mode? This affects only current curve.", "Yes"))
                {
                    var settings = Settings;
                    if (Curve.Mode2D != BGCurve.Mode2DEnum.Off)
                    {
                        Apply2D(settings.HandlesSettings);
                        Apply2D(settings.ControlHandlesSettings);
                    }
                    else
                    {
                        Apply3D(settings.HandlesSettings);
                        Apply3D(settings.ControlHandlesSettings);
                    }
                }
            }
        }

        private void Apply3D(BGCurveSettings.SettingsForHandles handlesSettings)
        {
            handlesSettings.RemoveX = handlesSettings.RemoveY = handlesSettings.RemoveZ = handlesSettings.RemoveXY = handlesSettings.RemoveXZ = handlesSettings.RemoveYZ = false;
        }

        private void Apply2D(BGCurveSettings.SettingsForHandles handlesSettings)
        {
            handlesSettings.RemoveX = Curve.Mode2D == BGCurve.Mode2DEnum.YZ;
            handlesSettings.RemoveY = Curve.Mode2D == BGCurve.Mode2DEnum.XZ;
            handlesSettings.RemoveZ = Curve.Mode2D == BGCurve.Mode2DEnum.XY;

            handlesSettings.RemoveXY = Curve.Mode2D == BGCurve.Mode2DEnum.XZ || Curve.Mode2D == BGCurve.Mode2DEnum.YZ;
            handlesSettings.RemoveXZ = Curve.Mode2D == BGCurve.Mode2DEnum.XY || Curve.Mode2D == BGCurve.Mode2DEnum.YZ;
            handlesSettings.RemoveYZ = Curve.Mode2D == BGCurve.Mode2DEnum.XZ || Curve.Mode2D == BGCurve.Mode2DEnum.XY;
        }


        internal Vector3 Handle(int number, BGCurveSettings.HandlesTypeEnum type, Vector3 position, Quaternion rotation, BGCurveSettings.SettingsForHandles handlesSettings)
        {
            switch (type)
            {
                case BGCurveSettings.HandlesTypeEnum.FreeMove:
                    position = Handles.FreeMoveHandle(position, rotation, BGEditorUtility.GetHandleSize(position, .2f), Vector3.zero, Handles.CircleCap);
                    break;
                case BGCurveSettings.HandlesTypeEnum.Standard:
                    position = Handles.PositionHandle(position, rotation);
                    break;
                case BGCurveSettings.HandlesTypeEnum.Configurable:
                    position = BGEditorUtility.ControlHandleCustom(number, position, rotation, handlesSettings);
                    break;
            }
            return position;
        }


        internal Vector3 GetLabelPosition(BGCurveSettings settings, Vector3 positionWorld)
        {
            return settings.ShowSpheres ? positionWorld + Vector3.up*settings.SphereRadius : positionWorld + Vector3.up*0.2f;
        }

        public override string GetStickerMessage(ref MessageType type)
        {
            return "" + Curve.PointsCount;
        }

        public override void OnUndoRedo()
        {
            editorSelection.OnUndoRedo();
        }
    }
}