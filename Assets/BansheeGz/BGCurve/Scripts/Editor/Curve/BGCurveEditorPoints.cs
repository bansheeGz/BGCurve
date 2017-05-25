using System;
using System.Collections.Generic;
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
        private static readonly Color32 HiddenPointMenuColor = new Color32(144, 195, 212, 255);

        private static readonly GUIContent[] XZLabels = {new GUIContent("X"), new GUIContent("Z")};
        private static readonly GUIContent[] YZLabels = {new GUIContent("Y"), new GUIContent("Z")};

        // ====================================== Fields
        private readonly Texture2D convertAll2D;
        private readonly Texture2D addPointIcon;

        private readonly SerializedProperty closedProperty;
        private readonly SerializedProperty pointsModeProperty;
        private readonly SerializedProperty controlTypeProperty;
        private readonly SerializedProperty mode2DProperty;
        private readonly SerializedProperty snapTypeProperty;
        private readonly SerializedProperty snapDistanceProperty;
        private readonly SerializedProperty snapAxisProperty;
        private readonly SerializedProperty snapTriggerInteractionProperty;
        private readonly SerializedProperty snapToBackFacesProperty;
        private readonly SerializedProperty eventModeProperty;
        private readonly SerializedProperty forceChangedEventModeProperty;

        //point
        private readonly BGCurveEditorPoint editorPoint;
        //painting a curve in the scene
        private BGCurvePainterGizmo painter;

        private readonly BGSceneViewOverlay overlay;
        private readonly BGCurveEditorPointsSelection editorSelection;
        private readonly SerializedObject serializedObject;
        private readonly List<BGTransformMonitor> pointTransformTrackers = new List<BGTransformMonitor>();

        private GUIContent syncContent;


        public BGCurveEditorPoints(BGCurveEditor editor, SerializedObject serializedObject, BGCurveEditorPointsSelection editorSelection)
            : base(editor, serializedObject, BGEditorUtility.LoadTexture2D(BGEditorUtility.Image.BGPoints123))
        {
            this.serializedObject = serializedObject;
            this.editorSelection = editorSelection;

            //textures
            convertAll2D = BGEditorUtility.LoadTexture2D(BGEditorUtility.Image.BGConvertAll123);
            addPointIcon = BGEditorUtility.LoadTexture2D(BGEditorUtility.Image.BGAdd123);

            //point
            editorPoint = new BGCurveEditorPoint(() => Editor.Math, editorSelection);

            //closed or not
            closedProperty = serializedObject.FindProperty("closed");

            //how points are stored
            pointsModeProperty = serializedObject.FindProperty("pointsMode");

            //2d mode
            mode2DProperty = serializedObject.FindProperty("mode2D");

            //snapping
            snapTypeProperty = serializedObject.FindProperty("snapType");
            snapAxisProperty = serializedObject.FindProperty("snapAxis");
            snapDistanceProperty = serializedObject.FindProperty("snapDistance");
            snapTriggerInteractionProperty = serializedObject.FindProperty("snapTriggerInteraction");
            snapToBackFacesProperty = serializedObject.FindProperty("snapToBackFaces");

            //force update
            forceChangedEventModeProperty = serializedObject.FindProperty("forceChangedEventMode");

            //event type
            eventModeProperty = serializedObject.FindProperty("eventMode");

            //settings
            controlTypeProperty = serializedObject.FindProperty("settings").FindPropertyRelative("controlType");

            //Context menu
            overlay = new BGSceneViewOverlay(this, editorSelection);

            //for GameObjects points which use transforms
            UpdatePointsTrackers();
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
                BGEditorUtility.VerticalBox(() =>
                {
                    var temp = BGCurveSettingsForEditor.DisableInspectorPointMenu;
                    BGCurveSettingsForEditor.DisableInspectorPointMenu = BGEditorUtility.ButtonOnOff(ref temp, "Points menu [" + Curve.PointsCount + "]", "Show points in Editor inspector",
                        HiddenPointMenuColor,
                        new GUIContent("Show", "Click to show points menu"),
                        new GUIContent("Hide", "Click to hide points menu"), () =>
                        {
                            const string title = "Reverse points";

                            if (!GUILayout.Button(new GUIContent(title, "Reverse all points, but keep curve intact"))) return;

                            if (Curve.PointsCount < 2)
                            {
                                BGEditorUtility.Inform(title, "There should be at least 2 points. Curve has " + Curve.PointsCount);
                                return;
                            }
                            if (!BGEditorUtility.Confirm(title, "Are you sure you want to reverse the order of " + Curve.PointsCount + " points? Curve will remain intact.", "Reverse")) return;

                            Curve.Reverse();
                        });

                    //show points!
                    if (!BGCurveSettingsForEditor.DisableInspectorPointMenu)
                        SwapVector2Labels(Curve.Mode2D, () => Curve.ForEach((point, index, count) => editorPoint.OnInspectorGui(point, index, settings)));
                });

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
                        BGCurveEditor.AddPoint(Curve, new BGCurvePoint(Curve, Vector3.zero, settings.ControlType, Vector3.right, Vector3.left), 0);
                });
            }

            if (!editorSelection.Changed) return;

            Editor.Repaint();
            SceneView.RepaintAll();
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
                    + "\r\n"
                    + "\r\n5) Ctrl + LeftClick over existing spline to insert a point"
                    , MessageType.Info);


            try
            {
                // Curve's block
                BGEditorUtility.VerticalBox(() =>
                {
                    //closed
                    EditorGUILayout.PropertyField(closedProperty);


                    //point's store mode
                    BGEditorUtility.Horizontal(() =>
                    {
                        EditorGUILayout.PropertyField(pointsModeProperty);

                        BGEditorUtility.DisableGui(() =>
                        {
                            BGEditorUtility.Assign(ref syncContent, () => new GUIContent("Sync", "Sort points Game Objects and update names"));

                            if (!GUILayout.Button(syncContent)) return;

                            BGPrivateField.Invoke(Curve, BGCurve.MethodSetPointsNames);
                        }, !BGCurve.IsGoMode(Curve.PointsMode));
                    });


                    //2D mode
                    BGEditorUtility.Horizontal(() =>
                    {
                        EditorGUILayout.PropertyField(mode2DProperty);
                        BGEditorUtility.DisableGui(() =>
                        {
                            if (!GUILayout.Button("Apply", GUI.skin.button, GUILayout.Width(80))) return;

                            Curve.FireBeforeChange(BGCurve.Event2D);
                            Curve.Apply2D(Curve.Mode2D);
                            Curve.FireChange(BGCurveChangedArgs.GetInstance(Curve, BGCurveChangedArgs.ChangeTypeEnum.Points, BGCurve.Event2D));
                        }, mode2DProperty.enumValueIndex == 0);
                    });

                    //snapping
                    BGEditorUtility.VerticalBox(() =>
                    {
                        BGEditorUtility.Horizontal(() =>
                        {
                            EditorGUILayout.PropertyField(snapTypeProperty);

                            BGEditorUtility.DisableGui(() =>
                            {
                                if (!GUILayout.Button("Apply", GUI.skin.button, GUILayout.Width(80))) return;

                                Curve.FireBeforeChange(BGCurve.EventSnapType);
                                Curve.ApplySnapping();
                                Curve.FireChange(BGCurveChangedArgs.GetInstance(Curve, BGCurveChangedArgs.ChangeTypeEnum.Snap, BGCurve.EventSnapType));
                            }, snapTypeProperty.enumValueIndex == 0);
                        });

                        if (snapTypeProperty.enumValueIndex == 0) return;

                        EditorGUILayout.PropertyField(snapAxisProperty);
                        EditorGUILayout.PropertyField(snapDistanceProperty);
                        EditorGUILayout.PropertyField(snapTriggerInteractionProperty);
                        EditorGUILayout.PropertyField(snapToBackFacesProperty);

                        BGEditorUtility.LayerMaskField("Snap Layer Mask", Curve.SnapLayerMask, i =>
                        {
                            Curve.FireBeforeChange(BGCurve.EventSnapTrigger);
                            Curve.SnapLayerMask = i;
                            Curve.ApplySnapping();
                            Curve.FireChange(BGCurveChangedArgs.GetInstance(Curve, BGCurveChangedArgs.ChangeTypeEnum.Snap, BGCurve.EventSnapTrigger));
                        });
                    });

                    //event mode
                    EditorGUILayout.PropertyField(eventModeProperty);

                    //force update
                    EditorGUILayout.PropertyField(forceChangedEventModeProperty);

                    //convert control type
                    BGEditorUtility.Horizontal(() =>
                    {
                        EditorGUILayout.PropertyField(controlTypeProperty);

                        if (!BGEditorUtility.ButtonWithIcon(convertAll2D, "Convert control types for all existing points ", 44)) return;

                        var settings = Settings;

                        foreach (var point in Curve.Points.Where(point => point.ControlType != settings.ControlType)) point.ControlType = settings.ControlType;
                    });
                });
            }
            catch (BGEditorUtility.ExitException)
            {
                GUIUtility.ExitGUI();
            }
        }

        // ================================================================================ Scene
        public override void OnSceneGui(Plane[] frustum)
        {
            var settings = Settings;

            var curveRotation = GetRotation(Curve.transform);

            if (Curve.PointsCount != 0 && settings.VRay)
            {
                painter = painter ?? new BGCurvePainterHandles(Editor.Math);
                painter.DrawCurve();
            }

            Curve.ForEach((point, index, count) => editorPoint.OnSceneGUI(point, index, settings, curveRotation, frustum));

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


            editorSelection.Scene(curveRotation);

            overlay.Process(Event.current);

            CheckPointsTransforms();
        }

        private void CheckPointsTransforms()
        {
            var skipAction = false;
            foreach (var tracker in pointTransformTrackers) skipAction |= tracker.CheckForChange(skipAction);
        }

        private static void ShowTangent(Vector3 position, Vector3 tangent, float size)
        {
	        if (tangent.sqrMagnitude > 0.0001f)
	        {
#if UNITY_5_6_OR_NEWER
				Handles.ArrowHandleCap(0, position, Quaternion.LookRotation(tangent), BGEditorUtility.GetHandleSize(position, size), EventType.Repaint);
#else
				Handles.ArrowCap(0, position, Quaternion.LookRotation(tangent), BGEditorUtility.GetHandleSize(position, size));
#endif
	        }
        }

        public static Quaternion GetRotation(Transform transform)
        {
            return Tools.pivotRotation == PivotRotation.Global ? Quaternion.identity : transform.rotation;
        }


        public override void OnApply()
        {
            var curve = Editor.Curve;
            var settings = Settings;

            // ==============================================    Closed
            if (curve.Closed != closedProperty.boolValue)
            {
                Curve.FireBeforeChange(BGCurve.EventClosed);
                serializedObject.ApplyModifiedProperties();
                Curve.FireChange(BGCurveChangedArgs.GetInstance(Curve, BGCurveChangedArgs.ChangeTypeEnum.Points, BGCurve.EventClosed));
            }

            if ((int) curve.ForceChangedEventMode != forceChangedEventModeProperty.enumValueIndex)
            {
                Curve.FireBeforeChange(BGCurve.EventForceUpdate);
                serializedObject.ApplyModifiedProperties();
                Curve.FireChange(BGCurveChangedArgs.GetInstance(Curve, BGCurveChangedArgs.ChangeTypeEnum.Curve, BGCurve.EventForceUpdate));
            }

            // ==============================================    Points store mode
            if ((int) Curve.PointsMode != pointsModeProperty.enumValueIndex)
            {
                var newPointsMode = (BGCurve.PointsModeEnum) pointsModeProperty.enumValueIndex;

                //ask for confirmation in case changes may affect something else
                if ((Curve.PointsMode == BGCurve.PointsModeEnum.Components) && !BGEditorUtility.Confirm("Convert Points",
                        "Are you sure you want to convert points? All existing references to these points will be lost.", "Convert")) return;

                if ((Curve.PointsMode == BGCurve.PointsModeEnum.GameObjectsNoTransform && newPointsMode != BGCurve.PointsModeEnum.GameObjectsTransform ||
                     Curve.PointsMode == BGCurve.PointsModeEnum.GameObjectsTransform && newPointsMode != BGCurve.PointsModeEnum.GameObjectsNoTransform)
                    && !BGEditorUtility.Confirm("Convert Points", "Are you sure you want to convert points? All existing GameObjects for points will be deleted.", "Convert")) return;

                editorSelection.Clear();

                //invoke convert
                BGPrivateField.Invoke(Curve, BGCurve.MethodConvertPoints, newPointsMode,
                    BGCurveEditor.GetPointProvider(newPointsMode, Curve),
                    BGCurveEditor.GetPointDestroyer(Curve.PointsMode, Curve));

                //this call is not required
                //                serializedObject.ApplyModifiedProperties();
            }

            // ==============================================    2D mode
            if ((int) curve.Mode2D != mode2DProperty.enumValueIndex)
            {
                Curve.FireBeforeChange(BGCurve.Event2D);
                serializedObject.ApplyModifiedProperties();

                var oldEventMode = Curve.EventMode;
                Curve.EventMode = BGCurve.EventModeEnum.NoEvents;

                //force points recalc
                Curve.Apply2D(Curve.Mode2D);

                if (BGEditorUtility.Confirm("Editor handles change", "Do you want to adjust configurable Editor handles (in Scene View) to chosen mode? This affects only current curve.", "Yes"))
                {
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

                Curve.EventMode = oldEventMode;

                Curve.FireChange(BGCurveChangedArgs.GetInstance(Curve, BGCurveChangedArgs.ChangeTypeEnum.Points, BGCurve.Event2D));
            }

            // ==============================================    Snapping
            if ((int) curve.SnapType != snapTypeProperty.enumValueIndex) SnappingChanged(BGCurve.EventSnapType);

            if ((int) curve.SnapAxis != snapAxisProperty.enumValueIndex) SnappingChanged(BGCurve.EventSnapAxis);

            if (Math.Abs((int) curve.SnapDistance - snapDistanceProperty.floatValue) > BGCurve.Epsilon) SnappingChanged(BGCurve.EventSnapDistance);

            if ((int) curve.SnapTriggerInteraction != snapTriggerInteractionProperty.enumValueIndex) SnappingChanged(BGCurve.EventSnapTrigger);

            if (curve.SnapToBackFaces != snapToBackFacesProperty.boolValue) SnappingChanged(BGCurve.EventSnapBackfaces);

            // ==============================================    Event mode
            if ((int) curve.EventMode != eventModeProperty.enumValueIndex) serializedObject.ApplyModifiedProperties();

            // ==============================================    Control Type
            if ((int) settings.ControlType != controlTypeProperty.enumValueIndex) serializedObject.ApplyModifiedProperties();
        }

        private void SnappingChanged(string eventMessage)
        {
            Curve.FireBeforeChange(eventMessage);

            serializedObject.ApplyModifiedProperties();

            Curve.ApplySnapping();

            Curve.FireChange(BGCurveChangedArgs.GetInstance(Curve, BGCurveChangedArgs.ChangeTypeEnum.Snap, eventMessage));
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


        public override string GetStickerMessage(ref MessageType type)
        {
            return "" + Curve.PointsCount;
        }

        public override void OnUndoRedo()
        {
            editorSelection.OnUndoRedo();
        }

        public override void OnCurveChanged(BGCurveChangedArgs args)
        {
            if (args == null || string.IsNullOrEmpty(args.Message) || Curve.PointsMode != BGCurve.PointsModeEnum.GameObjectsTransform) return;

            if (args.ChangeType != BGCurveChangedArgs.ChangeTypeEnum.Point && args.ChangeType != BGCurveChangedArgs.ChangeTypeEnum.Points) return;

            if (!(
                args.Message.Equals(BGCurve.EventAddPoint) || args.Message.Equals(BGCurve.EventAddPoints)
                || args.Message.Equals(BGCurve.EventClearAllPoints) || args.Message.Equals(BGCurve.EventDeletePoints)
            )) return;

            UpdatePointsTrackers();
        }

        private void UpdatePointsTrackers()
        {
            if (pointTransformTrackers.Count > 0) foreach (var tracker in pointTransformTrackers) tracker.Release();

            pointTransformTrackers.Clear();

            //for GameObjectsTransform mode
            if (Curve.PointsMode == BGCurve.PointsModeEnum.GameObjectsTransform)
                Curve.ForEach((point, index, count) => pointTransformTrackers.Add(BGTransformMonitor.GetMonitor(((BGCurvePointGO) point).transform, transform => Curve.FireChange(null))));

            //for points transforms
            Curve.ForEach((point, index, count) =>
            {
                if (point.PointTransform != null) pointTransformTrackers.Add(BGTransformMonitor.GetMonitor(point.PointTransform, transform => Curve.FireChange(null)));
            });
        }

        public static void SwapVector2Labels(BGCurve.Mode2DEnum mode2D, Action action)
        {
            var needToSwap = mode2D != BGCurve.Mode2DEnum.Off && mode2D != BGCurve.Mode2DEnum.XY;
            GUIContent[] oldLabels = null;
            if (needToSwap)
            {
                oldLabels = BGPrivateField.Get<GUIContent[]>(typeof(EditorGUI), "s_XYLabels");
                GUIContent[] newLabels;
                switch (mode2D)
                {
                    case BGCurve.Mode2DEnum.XZ:
                        newLabels = XZLabels;
                        break;
                    case BGCurve.Mode2DEnum.YZ:
                        newLabels = YZLabels;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("mode2D", mode2D, null);
                }
                BGPrivateField.Set(typeof(EditorGUI), "s_XYLabels", newLabels);
            }

            try
            {
                action();
            }
            finally
            {
                if (needToSwap) BGPrivateField.Set(typeof(EditorGUI), "s_XYLabels", oldLabels);
            }
        }
    }
}