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

        private BGCurve curve;
        //selected points
        private readonly BGCurveEditorPointsSelection editorSelection;
        //point
        private readonly BGCurveEditorPoint editorPoint;
        //painting a curve in scene
        private BGCurvePainterGizmo painter;

        //disable og chosing other object in scene
        private static bool lockView;

        private BGCurveSettings settings;

        private BGCurveEditor editor;

        private bool cancelEvent;

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

            //settings
            var settings = curveObject.FindProperty("settings");
            controlTypeProperty = settings.FindPropertyRelative("controlType");
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
                EditorUtility.SetDirty(curve);
            }
        }


        private void InspectorTopSection()
        {
            if (curve.PointsCount == 0)
            {
                EditorGUILayout.HelpBox("Ctrl+LeftClick in scene view to add a point. There should be a mesh to snap a point to" +
                                        "\r\nCtrl+Shift+LeftClick in scene view to add a point at distance, specified in the settings. No snapping", MessageType.Info);
            }

            EditorGUILayout.PropertyField(closedProperty);
            

            lockView = EditorGUILayout.Toggle(new GUIContent("Lock view", "Disable selection of other objects in the scene"), lockView);
            if (lockView)
            {
                EditorGUILayout.HelpBox("You can not chose another objects in the scene, except points", MessageType.Warning);
            }

            BGEditorUtility.Horizontal(() =>
            {
                EditorGUILayout.PropertyField(controlTypeProperty);

                if(BGEditorUtility.ButtonWithIcon(44,16,convertAll2D,"Convert control types for all existing points "))
                {
                    foreach (var point in curve.Points)
                    {
                        if (point.ControlType != settings.ControlType)
                        {
                            point.ControlType = settings.ControlType;
                        }
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
                        Undo.RecordObject(curve, "Add Point");


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
                            Undo.RecordObject(curve, "Add Point");
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