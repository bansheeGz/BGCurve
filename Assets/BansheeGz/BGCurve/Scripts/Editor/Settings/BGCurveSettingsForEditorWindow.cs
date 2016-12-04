using UnityEngine;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
    public class BGCurveSettingsForEditorWindow : EditorWindow
    {
        private const int Padding = 20;
        private static readonly Vector2 WindowSize = new Vector2(600, 400);

        private Vector2 scrollPos;


        internal static void Open()
        {
            BGEditorUtility.ShowPopupWindow<BGCurveSettingsForEditorWindow>(WindowSize);
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            ShowSettings();
            EditorGUILayout.EndScrollView();
        }

        private static void ShowSettings()
        {
            BGEditorUtility.SwapLabelWidth(300, () =>
            {
                BGEditorUtility.Vertical(new GUIStyle("Box") {padding = new RectOffset(Padding, Padding, Padding, Padding)}, () =>
                {
                    EditorGUILayout.LabelField("BG Curve Editor Settings", new GUIStyle("Box") {fontSize = 22});

                    if (GUILayout.Button(new GUIContent("Reset to defaults", "Reset all editor settings to their defaults."))
                        && BGEditorUtility.Confirm("Reset settings", "Reset All Editor settings to defaults? It does not affect curve's settings.", "Reset"))
                    {
                        BGCurveSettingsForEditor.Reset();
                    }

                    //coordinates for points and controls
                    BGEditorUtility.VerticalBox(() =>
                    {
                        BGEditorUtility.PopupField(BGCurveSettingsForEditor.InspectorPointCoordinateSpace, "Point Coordinates",
                            b => BGCurveSettingsForEditor.InspectorPointCoordinateSpace = (BGCurveSettingsForEditor.CoordinateSpaceEnum) b);
                        EditorGUILayout.HelpBox("Coordinate Space for points (for Inspector's fields inder Points tab.)", MessageType.Info);
                    });

                    BGEditorUtility.VerticalBox(() =>
                    {
                        BGEditorUtility.PopupField(BGCurveSettingsForEditor.InspectorControlCoordinateSpace, "Point Controls Coordinates",
                            b => BGCurveSettingsForEditor.InspectorControlCoordinateSpace = (BGCurveSettingsForEditor.CoordinateSpaceEnum)b);
                        EditorGUILayout.HelpBox("Coordinate Space for points controls (for Inspector's fields inder Points tab.)", MessageType.Info);
                    });


                    // disable fields
                    BGEditorUtility.VerticalBox(() =>
                    {
                        BGEditorUtility.ToggleField(BGCurveSettingsForEditor.DisableSceneViewPointMenu, "Disable SV Point Menu", b => BGCurveSettingsForEditor.DisableSceneViewPointMenu = b);
                        EditorGUILayout.HelpBox("Disable point's menu, which is activated in Scene View by holding Ctrl over a point.", MessageType.Info);
                    });

                    BGEditorUtility.VerticalBox(() =>
                    {
                        BGEditorUtility.ToggleField(BGCurveSettingsForEditor.DisableSceneViewSelectionMenu, "Disable SV Selection Menu", b => BGCurveSettingsForEditor.DisableSceneViewSelectionMenu = b);
                        EditorGUILayout.HelpBox("Disable selection's menu, which is activated in Scene View by holding Ctrl over a selection handles.", MessageType.Info);
                    });

/*
                                                                                BGEUtil.VerticalBox(() =>
                                                                                {
                                                                                    BGEUtil.ToggleField(BGCurveEditorSettings.DisableInspectorPointMenu, "Disable Inspector Points Menu", b => BGCurveEditorSettings.DisableInspectorPointMenu = b);
                                                                                    EditorGUILayout.HelpBox("Disable points menu, which is located under Points tab in Inspector.", MessageType.Info);
                                                                                });
                                                            */

                    BGEditorUtility.VerticalBox(() =>
                    {
                        BGEditorUtility.ToggleField(BGCurveSettingsForEditor.DisableRectangularSelection, "Disable Rectangular Selection", b => BGCurveSettingsForEditor.DisableRectangularSelection = b);
                        EditorGUILayout.HelpBox("Disable rectangular selection in Scene View, which is activated by holding shift and mouse dragging.", MessageType.Info);
                    });


                    // colors
                    BGEditorUtility.VerticalBox(() =>
                    {
                        BGEditorUtility.ColorField("Rectangular Selection Color", BGCurveSettingsForEditor.ColorForRectangularSelection, b => BGCurveSettingsForEditor.ColorForRectangularSelection = b);
                        EditorGUILayout.HelpBox("Color for Rectangular Selection background", MessageType.Info);
                    });

                    BGEditorUtility.VerticalBox(() =>
                    {
                        BGEditorUtility.ColorField("Add and Snap 3D Handles Color", BGCurveSettingsForEditor.HandleColorForAddAndSnap3D, b => BGCurveSettingsForEditor.HandleColorForAddAndSnap3D = b);
                        EditorGUILayout.HelpBox("Color for handles, shown for 3D curve in Scene View when new point is previewed.", MessageType.Info);
                    });

                    BGEditorUtility.VerticalBox(() =>
                    {
                        BGEditorUtility.ColorField("Add and Snap 2D Handles Color", BGCurveSettingsForEditor.HandleColorForAddAndSnap2D, b => BGCurveSettingsForEditor.HandleColorForAddAndSnap2D = b);
                        EditorGUILayout.HelpBox("Color for handles, shown for 2D curve in Scene View when new point is previewed.", MessageType.Info);
                    });

                    BGEditorUtility.VerticalBox(() =>
                    {
                        BGEditorUtility.ColorField("Points labels back color", BGCurveSettingsForEditor.ColorForLabelBackground, b => BGCurveSettingsForEditor.ColorForLabelBackground = b);
                        EditorGUILayout.HelpBox("Background color for points labels in Scene View.", MessageType.Info);
                    });
                    BGEditorUtility.VerticalBox(() =>
                    {
                        BGEditorUtility.ColorField("New section preview color", BGCurveSettingsForEditor.ColorForNewSectionPreview, b => BGCurveSettingsForEditor.ColorForNewSectionPreview = b);
                        EditorGUILayout.HelpBox("Color for new section preview in Scene View.", MessageType.Info);
                    });
                });
            });
        }
    }
}