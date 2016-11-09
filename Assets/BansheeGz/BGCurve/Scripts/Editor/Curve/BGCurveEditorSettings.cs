using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BansheeGz.BGSpline.Editor
{
    public class BGCurveEditorSettings : BGCurveEditorTab
    {
        private static bool showSaveLoad;
        private string newAssetName;
        private string lastOperation;

        //anim props
        private readonly BGEditorUtility.BoolAnimatedProperty showCurveProp;
        private SerializedProperty settings;


        public BGCurveEditorSettings(BGCurveEditor editor, SerializedObject serializedObject)
            : base(editor, serializedObject, BGEditorUtility.LoadTexture2D(BGEditorUtility.Image.BGSettings123))
        {
            //anim props
            showCurveProp = new BGEditorUtility.BoolAnimatedProperty(editor, serializedObject.FindProperty("settings"), "showCurve");
        }

        public override void OnInspectorGui()
        {
            settings = SerializedObject.FindProperty("settings");
            var settingsObj = Settings;

            // Save & Load
            showSaveLoad = EditorGUILayout.Foldout(showSaveLoad, "Save and load settings");
            if (showSaveLoad)
            {
                BGEditorUtility.VerticalBox(() =>
                {
                    var path = BGCurveSettingsOperations.GetPath();

                    BGEditorUtility.HelpBox("Folder is not set", MessageType.Info, path == null, () =>
                    {
                        EditorGUILayout.LabelField("Folder", path);

                        BGEditorUtility.HelpBox("Folder is not found", MessageType.Warning, !BGCurveSettingsOperations.IsValid(path), () =>
                        {
                            // =================================  Load settings
                            var all = BGCurveSettingsOperations.GetAll();

                            BGEditorUtility.HelpBox("Folder does not have any settings", MessageType.Warning, all == null || all.Length == 0, () =>
                            {
                                BGEditorUtility.Horizontal(() =>
                                {
                                    var options = new List<GUIContent> {new GUIContent("")};
                                    options.AddRange(all.Select(setting => new GUIContent(setting)));
                                    var selected = EditorGUILayout.Popup(new GUIContent("Load", "Load a specified settings for current object"), 0, options.ToArray());
                                    if (selected > 0)
                                    {
                                        var newSettings = BGCurveSettingsOperations.Load(options[selected].text);
                                        if (newSettings != null)
                                        {
                                            BGPrivateField.SetSettings(Curve, newSettings);
                                            EditorUtility.SetDirty(Curve);
                                            lastOperation = options[selected].text + " was loaded";
                                        }
                                        else lastOperation = "Unable to load a settings " + options[selected].text;
                                    }

                                    if (GUILayout.Button(new GUIContent("Reload", "Reload settings from disk. This operation does not change settings for the curent object.")))
                                    {
                                        BGCurveSettingsOperations.Reload(BGCurveSettingsOperations.GetPath());
                                        lastOperation = "Settings was reloaded from disk";
                                    }
                                });
                            });

                            // =================================  Save settings
                            BGEditorUtility.Horizontal(() =>
                            {
                                newAssetName = EditorGUILayout.TextField(new GUIContent("Save", "Save current setting on disk"), newAssetName);
                                if (!GUILayout.Button(new GUIContent("Save", "Save current setting on disk"))) return;

                                if (newAssetName == null || newAssetName.Trim().Equals("")) BGEditorUtility.Inform("Invalid asset name", "Please, enter the name for new asset");
                                else lastOperation = BGCurveSettingsOperations.Save(settingsObj, newAssetName) ? newAssetName + " was saved on disk" : "Unable to save " + newAssetName + " on disk";
                            });

                            BGEditorUtility.HelpBox(lastOperation, MessageType.Info, lastOperation != null);
                        });
                    });

                    BGEditorUtility.Horizontal(() =>
                    {
                        if (GUILayout.Button(new GUIContent("Save as default", "Save current settings as default for future curves")))
                        {
                            lastOperation = BGCurveSettingsOperations.SaveDefault(settingsObj) ? "Current settings was saved as default" : "Unable to save settings on disk as default";
                        }

                        if (GUILayout.Button(new GUIContent("Chose a folder", "Chose a folder where to store settings files"))) BGCurveSettingsOperations.ChoseDir();
                    });
                });
            }

            EditorGUILayout.HelpBox("All fields settings are under Fields tab", MessageType.Warning);

            BGEditorUtility.ChangeCheck(() =>
            {
                //Points
                BGEditorUtility.VerticalBox(() =>
                {
                    //Hide handles
                    EditorGUILayout.PropertyField(Find("hideHandles"));

                    EditorGUILayout.PropertyField(Find("newPointDistance"));
                    EditorGUILayout.PropertyField(Find("showPointMenu"));
                });

                var tangentProp = Find("showTangents");

                //curve
                BGEditorUtility.FadeGroup(showCurveProp, () =>
                {
                    EditorGUILayout.PropertyField(Find("showCurveMode"));
                    EditorGUILayout.PropertyField(Find("sections"));
                    EditorGUILayout.PropertyField(Find("vRay"));
                    BGEditorUtility.HelpBox("VRay will work only if object is selected.", MessageType.Warning, Find("vRay").boolValue && Find("showCurveMode").enumValueIndex != 0);
                    EditorGUILayout.PropertyField(Find("lineColor"));

                    //tangents
                    BGEditorUtility.VerticalBox(() =>
                    {
                        EditorGUILayout.PropertyField(tangentProp);
                        if (settingsObj.ShowTangents)
                        {
                            BGEditorUtility.Indent(1, () =>
                            {
                                EditorGUILayout.PropertyField(Find("tangentsSize"));
                                EditorGUILayout.PropertyField(Find("tangentsPerSection"));
                                EditorGUILayout.PropertyField(Find("tangentsColor"));
                            });
                        }
                    });
                });
            }, () =>
            {
                //if any change
                SerializedObject.ApplyModifiedProperties();
                SceneView.RepaintAll();
            });
        }

        private SerializedProperty Find(string name)
        {
            return settings.FindPropertyRelative(name);
        }


        public override void OnEnable()
        {
            Tools.hidden = Settings.HideHandles;
        }


        public override void OnApply()
        {
            Tools.hidden = Settings.HideHandles;
        }
    }
}