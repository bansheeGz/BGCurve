using System.Collections.Generic;
using BansheeGz.BGSpline.Curve;
using BansheeGz.BGSpline.EditorHelpers;
using UnityEditor;
using UnityEngine;

namespace BansheeGz.BGSpline.Editor
{
    public class BGCurveEditorSettings : BGCurveEditorTab
    {

        //points
        private readonly SerializedProperty newPointDistanceProperty;
        private readonly SerializedProperty showPointMenuProperty;
        private readonly SerializedProperty showPointControlTypeProperty;
        private readonly SerializedProperty showPointPositionProperty;
        private readonly SerializedProperty showPointControlPositionsProperty;

        //curve
        private readonly BGEditorUtility.BoolAnimatedProperty showCurveProperty;
        private readonly SerializedProperty showEvenNotSelectedProperty;
        private readonly SerializedProperty showHandlesProperty;
        private readonly SerializedProperty handlesTypeProperty;
        private readonly SerializedProperty handlesSettingsProperty;
        private readonly SerializedProperty sectionsProperty;
        private readonly SerializedProperty vRayProperty;
        private readonly SerializedProperty lineColorProperty;

        //control handles
        private readonly BGEditorUtility.BoolAnimatedProperty showControlHandlesProperty;
        private readonly SerializedProperty showControlPositionsProperty;
        private readonly SerializedProperty controlHandlesColorProperty;
        private readonly SerializedProperty controlHandlesTypeProperty;
        private readonly SerializedProperty controlHandlesSettingsProperty;


        //lables
        private readonly BGEditorUtility.BoolAnimatedProperty showLabelsProperty;
        private readonly SerializedProperty showPositionsProperty;
        private readonly SerializedProperty labelColorProperty;
        private readonly SerializedProperty labelColorSelectedProperty;

        //spheres
        private readonly BGEditorUtility.BoolAnimatedProperty showSpheresProperty;
        private readonly SerializedProperty sphereRadiusProperty;
        private readonly SerializedProperty sphereColorProperty;

        //------------------------------------------- misc
        private static bool showSaveLoad;
        private static bool showPointsOptions = true;
        private readonly BGCurve curve;
        private readonly Texture2D header2D;
        private string newAssetName;
        private string lastOperation;

        public BGCurveEditorSettings(BGCurveEditor editor, SerializedObject serializedObject)
        {
            curve = editor.Curve;


            var settings = serializedObject.FindProperty("settings");


            //points
            newPointDistanceProperty = settings.FindPropertyRelative("newPointDistance");
            showPointMenuProperty = settings.FindPropertyRelative("showPointMenu");
            showPointControlTypeProperty = settings.FindPropertyRelative("showPointControlType");
            showPointPositionProperty = settings.FindPropertyRelative("showPointPosition");
            showPointControlPositionsProperty = settings.FindPropertyRelative("showPointControlPositions");


            //curve
            showCurveProperty = new BGEditorUtility.BoolAnimatedProperty(editor, settings, "showCurve");
            showEvenNotSelectedProperty = settings.FindPropertyRelative("showEvenNotSelected");
            showHandlesProperty = settings.FindPropertyRelative("showHandles");
            handlesTypeProperty = settings.FindPropertyRelative("handlesType");
            handlesSettingsProperty = settings.FindPropertyRelative("handlesSettings");

            sectionsProperty = settings.FindPropertyRelative("sections");
            vRayProperty = settings.FindPropertyRelative("vRay");
            lineColorProperty = settings.FindPropertyRelative("lineColor");

            //control handles
            showControlHandlesProperty = new BGEditorUtility.BoolAnimatedProperty(editor, settings, "showControlHandles");
            controlHandlesColorProperty = settings.FindPropertyRelative("controlHandlesColor");
            controlHandlesTypeProperty = settings.FindPropertyRelative("controlHandlesType");
            controlHandlesSettingsProperty = settings.FindPropertyRelative("controlHandlesSettings");

            //lables
            showLabelsProperty = new BGEditorUtility.BoolAnimatedProperty(editor, settings, "showLabels");
            showPositionsProperty = settings.FindPropertyRelative("showPositions");
            showControlPositionsProperty = settings.FindPropertyRelative("showControlPositions");
            labelColorProperty = settings.FindPropertyRelative("labelColor");
            labelColorSelectedProperty = settings.FindPropertyRelative("labelColorSelected");

            //spheres
            showSpheresProperty = new BGEditorUtility.BoolAnimatedProperty(editor, settings, "showSpheres");
            sphereRadiusProperty = settings.FindPropertyRelative("sphereRadius");
            sphereColorProperty = settings.FindPropertyRelative("sphereColor");

            header2D = BGEditorUtility.LoadTexture2D("BGSettings123");
        }

        public void OnInspectorGUI()
        {
            var settings = BGPrivateField.GetSettings(curve);

            showSaveLoad = EditorGUILayout.Foldout(showSaveLoad, "Save and load settings");
            if (showSaveLoad)
            {
                BGEditorUtility.Vertical("Box", () =>
                {
                    var path = BGCurveSettingsOperations.GetPath();
                    if (path == null)
                    {
                        EditorGUILayout.HelpBox("Folder is not set", MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Folder", path);
                        if (!BGCurveSettingsOperations.IsValid(path))
                        {
                            EditorGUILayout.HelpBox("Folder is not found", MessageType.Warning);
                        }
                        else
                        {
                            // =================================  Load settings
                            var all = BGCurveSettingsOperations.GetAll();
                            if (all == null || all.Length == 0)
                            {
                                EditorGUILayout.HelpBox("Folder does not have any settings", MessageType.Warning);
                            }
                            else
                            {
                                BGEditorUtility.Horizontal(() =>
                                {
                                    var options = new List<GUIContent> { new GUIContent("") };
                                    foreach (var setting in all)
                                    {
                                        options.Add(new GUIContent(setting));
                                    }
                                    var selected = EditorGUILayout.Popup(new GUIContent("Load", "Load a specified setting"), 0, options.ToArray());
                                    if (selected > 0)
                                    {
                                        var newSettings = BGCurveSettingsOperations.Load(options[selected].text);
                                        if (newSettings != null)
                                        {
                                            BGPrivateField.SetSettings(curve, newSettings);
                                            EditorUtility.SetDirty(curve);
                                            lastOperation = options[selected].text + " was loaded";
                                        }
                                        else
                                        {
                                            lastOperation = "Unable to load a settings " + options[selected].text;
                                        }
                                    }
                                    if (GUILayout.Button(new GUIContent("Reload", "Reload settings from disk")))
                                    {
                                        BGCurveSettingsOperations.Reload(BGCurveSettingsOperations.GetPath());
                                        lastOperation = "Settings was reloaded from disk";
                                    }
                                });
                            }

                            // =================================  Save settings
                            BGEditorUtility.Horizontal(() =>
                            {
                                newAssetName = EditorGUILayout.TextField(new GUIContent("Save", "Save current setting on disk" ), newAssetName);
                                if (GUILayout.Button(new GUIContent("Save", "Save current setting on disk")))
                                {
                                    if (newAssetName == null || newAssetName.Trim().Equals(""))
                                    {
                                        EditorUtility.DisplayDialog("Invalid asset name", "Please, enter the name for new asset", "Ok");
                                    }
                                    else
                                    {
                                        if (BGCurveSettingsOperations.Save(settings, newAssetName))
                                        {
                                            lastOperation = newAssetName + " was saved on disk";
                                        }
                                        else
                                        {
                                            lastOperation = "Unable to save " + newAssetName + " on disk";
                                        }
                                        
                                    }
                                }
                            });

                            if (lastOperation != null)
                            {
                                EditorGUILayout.HelpBox(lastOperation, MessageType.Info);
                            }
                        }
                    }
                    BGEditorUtility.Horizontal(() =>
                    {
                        if (GUILayout.Button(new GUIContent("Save as default", "Save current settings as default for future curves")))
                        {
                            if (BGCurveSettingsOperations.SaveDefault(settings))
                            {
                                lastOperation = "Current settings was saved as default";
                            }
                            else
                            {
                                lastOperation = "Unable to save settings on disk as default";
                            }
                        }
                        if (GUILayout.Button(new GUIContent("Chose a folder", "Chose a folder where to store settings files")))
                        {
                            BGCurveSettingsOperations.ChoseDir();
                        }
                    });
                });
            }

            showPointsOptions = EditorGUILayout.Foldout(showPointsOptions, "Points options");
            if (showPointsOptions)
            {
                BGEditorUtility.Vertical("Box", () =>
                {
                    EditorGUILayout.PropertyField(newPointDistanceProperty);
                    EditorGUILayout.PropertyField(showPointMenuProperty);
                    EditorGUILayout.PropertyField(showPointControlTypeProperty);
                    EditorGUILayout.PropertyField(showPointPositionProperty);
                    EditorGUILayout.PropertyField(showPointControlPositionsProperty);
                });
            }
            //curve
            BGEditorUtility.FadeGroup(showCurveProperty, () =>
            {
                EditorGUILayout.PropertyField(showEvenNotSelectedProperty);
                EditorGUILayout.PropertyField(showHandlesProperty);
                BGEditorUtility.Vertical("Box", () =>
                {
                    EditorGUILayout.PropertyField(handlesTypeProperty);
                    if (settings.HandlesType == BGCurveSettings.HandlesTypeEnum.Configurable)
                    {
                        BGEditorUtility.StartIndent(1);
                        EditorGUILayout.PropertyField(handlesSettingsProperty);
                        if (settings.HandlesSettings.Disabled)
                        {
                            EditorGUILayout.HelpBox("All handles are disabled.", MessageType.Warning);
                        }
                        BGEditorUtility.EndIndent(1);
                    }
                });

                EditorGUILayout.PropertyField(sectionsProperty);
                EditorGUILayout.PropertyField(vRayProperty);
                EditorGUILayout.PropertyField(lineColorProperty);
            });

            //control handles
            BGEditorUtility.FadeGroup(showControlHandlesProperty, () =>
            {
                EditorGUILayout.PropertyField(controlHandlesColorProperty);

                BGEditorUtility.Vertical("Box", () =>
                {
                    EditorGUILayout.PropertyField(controlHandlesTypeProperty);
                    if (settings.ControlHandlesType == BGCurveSettings.HandlesTypeEnum.Configurable)
                    {
                        BGEditorUtility.StartIndent(1);
                        EditorGUILayout.PropertyField(controlHandlesSettingsProperty);
                        if (settings.ControlHandlesSettings.Disabled)
                        {
                            EditorGUILayout.HelpBox("All handles are disabled.", MessageType.Warning);
                        }
                        BGEditorUtility.EndIndent(1);
                    }
                });
            });


            //labels 
            BGEditorUtility.FadeGroup(showLabelsProperty, () =>
            {
                EditorGUILayout.PropertyField(showPositionsProperty);
                EditorGUILayout.PropertyField(showControlPositionsProperty);
                EditorGUILayout.PropertyField(labelColorProperty);
                EditorGUILayout.PropertyField(labelColorSelectedProperty);
            });


            //sphere
            BGEditorUtility.FadeGroup(showSpheresProperty, () =>
            {
                EditorGUILayout.PropertyField(sphereRadiusProperty);
                EditorGUILayout.PropertyField(sphereColorProperty);
            });
        }

        public Texture2D GetHeader()
        {
            return header2D;
        }

        public void OnSceneGUI()
        {
        }
    }
}