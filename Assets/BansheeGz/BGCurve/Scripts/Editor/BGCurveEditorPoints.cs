using System.Linq;
using UnityEngine;
using BansheeGz.BGSpline.Curve;
using BansheeGz.BGSpline.EditorHelpers;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
    public class BGCurveEditorPoints : BGCurveEditorTab
    {
        // ====================================== Static const
        private readonly Texture2D header2D;
        private readonly Texture2D convertAll2D;

        // ====================================== Fields
        private readonly SerializedProperty closedProperty;
        private readonly SerializedProperty controlTypeProperty;
        private readonly SerializedProperty mode2DProperty;

        private readonly BGCurve curve;

        //selected points
        private readonly BGCurveEditorPointsSelection editorSelection;
        //point
        private readonly BGCurveEditorPoint editorPoint;
        //painting a curve in scene
        private BGCurvePainterGizmo painter;

        //disable og chosing other object in scene
        private static bool lockView;

        private BGCurveSettings settings;

        private readonly BGCurveEditor editor;

        private bool cancelEvent;
        private bool closeChanged;
        private bool mode2DChanged;

        public BGCurveEditorPoints(BGCurveEditor editor, SerializedObject curveObject)
        {
            this.editor = editor;
            curve = editor.Curve;

            //textures
            header2D = BGEditorUtility.LoadTexture2D("BGPoints123");
            convertAll2D = BGEditorUtility.LoadTexture2D("BGConvertAll123");

            //selection
            editorSelection = new BGCurveEditorPointsSelection(curve, this);

            //point
            editorPoint = new BGCurveEditorPoint(curve, editorSelection);

            //closed or not
            closedProperty = curveObject.FindProperty("closed");
            //2d mode
            mode2DProperty = curveObject.FindProperty("mode2D");

            //settings
            controlTypeProperty = curveObject.FindProperty("settings").FindPropertyRelative("controlType");
        }

        public Texture2D GetHeader()
        {
            return header2D;
        }

        // ================================================================================ Inspector
        public void OnInspectorGUI()
        {
            settings = BGPrivateField.GetSettings(curve);

            editorSelection.Reset();

            // ======================================== Top section
            InspectorTopSection();

            // ======================================== Points
            GUILayout.Space(5);

            if (curve.PointsCount > 0)
            {
                BGEditorUtility.Vertical("Box", () =>
                {
                    for (var i = 0; i < curve.PointsCount; i++)
                    {
                        editorPoint.OnInspectorGUI(curve.Points[i], i,  settings);
                    }
                });

                // ======================================== Selections operations
                editorSelection.InspectorSelectionOperations();

                if (!editorSelection.Changed && editorSelection.HasSelected())
                {
                    //warning
                    EditorGUILayout.HelpBox("Selection mode is on", MessageType.Warning);
                }
            }
            else
            {
                BGEditorUtility.Horizontal("Box", () => { EditorGUILayout.LabelField("No points!"); });
            }

            if (editorSelection.Changed)
            {
//                SceneView.RepaintAll();
                EditorUtility.SetDirty(curve);
            }
        }


        private void InspectorTopSection()
        {
            lockView = EditorGUILayout.Toggle(new GUIContent("Lock view", "Disable selection of other objects in the scene"), lockView);
            if (lockView)
            {
                EditorGUILayout.HelpBox("You can not chose another objects in the scene, except points", MessageType.Warning);
            }

            if (curve.PointsCount == 0)
            {
                EditorGUILayout.HelpBox("Ctrl+LeftClick in scene view to add a point. There should be a mesh to snap a point to" +
                                        "\r\nCtrl+Shift+LeftClick in scene view to add a point at distance, specified in the settings. No snapping", MessageType.Info);
            }

            EditorGUILayout.PropertyField(closedProperty);
            EditorGUILayout.PropertyField(mode2DProperty);
            
            BGEditorUtility.Horizontal(() =>
            {
                EditorGUILayout.PropertyField(controlTypeProperty);

                if(BGEditorUtility.ButtonWithIcon(44,16,convertAll2D,"Convert control types for all existing points "))
                {
                    foreach (var point in curve.Points.Where(point => point.ControlType != settings.ControlType))
                    {
                        point.ControlType = settings.ControlType;
                    }
                }
            });
        }

        // ================================================================================ Scene
        public void OnSceneGUI()
        {
            settings = BGPrivateField.GetSettings(curve);

            var rotation = Tools.pivotRotation == PivotRotation.Global ? Quaternion.identity : curve.transform.rotation;

            if (settings.ShowCurve && settings.VRay)
            {
                if (painter == null)
                {
                    painter = editor.NewPainter();
                }
                painter.DrawCurve();
            }

            for (var i = 0; i < curve.PointsCount; i++)
            {
                editorPoint.OnSceneGUI(this, curve.Points[i], i, settings, rotation);
            }

            editorSelection.Scene(rotation);

            var currentEvent = Event.current;

            if (currentEvent.type == EventType.mouseDown && currentEvent.button == 0)
            {
                if (currentEvent.control)
                {
                    if (currentEvent.shift)
                    {
                        //no snapping
                        cancelEvent = true;
                        GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);

                        var ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
                        var position = ray.GetPoint(settings.NewPointDistance);
                        curve.AddPoint(curve.CreatePointFromWorldPosition(position, settings.ControlType));
                    }
                    else
                    {
                        //snap new point to a scene object
                        var ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
                        RaycastHit hit;
                        if (Physics.Raycast(ray, out hit))
                        {
                            curve.AddPoint(curve.CreatePointFromWorldPosition(hit.point, settings.ControlType));

                            currentEvent.Use();
                            cancelEvent = true;
                            GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
                            EditorUtility.SetDirty(curve);
                        }
                        else
                        {
                            BGCurveEditor.EditorPopup.Display("No mesh to snap a point to! \r\n Use Ctrl+Shift+Click to spawn a point at the distance,\r\n which is set in settings");
                        }
                    }
                }
                else if (lockView)
                {
                    cancelEvent = true;
                    GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
                }
            }
            else if (currentEvent.type == EventType.mouseUp && cancelEvent)
            {
                cancelEvent = false;
                GUIUtility.hotControl = 0;
            }
        }

        public void OnEnable()
        {
        }

        public void OnBeforeApply()
        {
            closeChanged = false; 
            if (editor.Curve.Closed != closedProperty.boolValue)
            {
                closeChanged = true; 
                curve.FireBeforeChange("closed changed");
            }

            mode2DChanged = false;
            if ((int) editor.Curve.Mode2D != mode2DProperty.enumValueIndex)
            {
                mode2DChanged = true;
                curve.FireBeforeChange("2d mode changed");
            }
        }

        public void OnApply()
        {
            if (closeChanged) curve.FireChange(new BGCurveChangedArgs(curve, BGCurveChangedArgs.ChangeTypeEnum.Points));
            
            if (mode2DChanged)
            {
                if (curve.Mode2D != BGCurve.Mode2DEnum.Off)
                {
                    //apply settings
                    Apply2D(settings.HandlesSettings);
                    Apply2D(settings.ControlHandlesSettings);
                }

                //force points recalc
                curve.Apply2D(curve.Mode2D);

                curve.FireChange(new BGCurveChangedArgs(curve, BGCurveChangedArgs.ChangeTypeEnum.Points));
            }
        }

        private void Apply2D(BGHandlesSettings handlesSettings)
        {
            handlesSettings.RemoveX = curve.Mode2D == BGCurve.Mode2DEnum.YZ;
            handlesSettings.RemoveY = curve.Mode2D == BGCurve.Mode2DEnum.XZ;
            handlesSettings.RemoveZ = curve.Mode2D == BGCurve.Mode2DEnum.XY;

            handlesSettings.RemoveXY = curve.Mode2D == BGCurve.Mode2DEnum.XZ || curve.Mode2D == BGCurve.Mode2DEnum.YZ;
            handlesSettings.RemoveXZ = curve.Mode2D == BGCurve.Mode2DEnum.XY || curve.Mode2D == BGCurve.Mode2DEnum.YZ;
            handlesSettings.RemoveYZ = curve.Mode2D == BGCurve.Mode2DEnum.XZ || curve.Mode2D == BGCurve.Mode2DEnum.XY;

        }


        internal Vector3 Handle(int number, BGCurveSettings.HandlesTypeEnum type, Vector3 position, Quaternion rotation, BGHandlesSettings handlesSettings)
        {
            switch (type)
            {
                case BGCurveSettings.HandlesTypeEnum.FreeMove:
                    position = Handles.FreeMoveHandle(position, rotation, HandleUtility.GetHandleSize(position)*.1f, Vector3.zero, Handles.CircleCap);
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


        internal Vector3 GetLabelPosition(Vector3 positionWorld)
        {
            return settings.ShowSpheres ? positionWorld + Vector3.up*settings.SphereRadius : positionWorld + Vector3.up*0.2f;
        }
    }
}