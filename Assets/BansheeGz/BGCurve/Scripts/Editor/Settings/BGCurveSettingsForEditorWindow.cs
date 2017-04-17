using System;
using UnityEngine;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
    public class BGCurveSettingsForEditorWindow : EditorWindow
    {
        private const int Padding = 20;
        private static readonly Vector2 WindowSize = new Vector2(600, 400);

        private static BGAbstractSettingsForEditor settings;

        private Vector2 scrollPos;


        internal static void Open(BGAbstractSettingsForEditor settings)
        {
            BGCurveSettingsForEditorWindow.settings = settings;
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
                    EditorGUILayout.LabelField(settings.Name, new GUIStyle("Box") {fontSize = 22});

                    if (GUILayout.Button(new GUIContent("Reset to defaults", "Reset all editor settings to their defaults."))
                        && BGEditorUtility.Confirm("Reset settings", "Reset All Editor settings to defaults? It does not affect local settings.", "Reset"))
                    {
                        settings.Reset();
                    }

                    var keys = settings.Keys;
                    foreach (var key in keys)
                    {
                        var descriptor = settings.GetSetting(key);
                        if (descriptor.Name == null) continue;

                        Action ui;
                        if (descriptor is BGAbstractSettingsForEditor.SettingEnum)
                        {
                            var setting = (BGAbstractSettingsForEditor.SettingEnum) descriptor;
                            ui = () =>
                            {
                                setting.Value = setting.Ui(setting.Value);
                            };
                        }
                        else if (descriptor is BGAbstractSettingsForEditor.SettingBool)
                        {
                            var setting = (BGAbstractSettingsForEditor.SettingBool) descriptor;
                            ui = () => { BGEditorUtility.ToggleField(setting.Value, descriptor.Name, b => setting.Value = b); };
                        }
                        else if (descriptor is BGAbstractSettingsForEditor.SettingInt)
                        {
                            var setting = (BGAbstractSettingsForEditor.SettingInt) descriptor;
                            ui = () => { BGEditorUtility.IntField(descriptor.Name, setting.Value, b => setting.Value = b); };
                        }
                        else if (descriptor is BGAbstractSettingsForEditor.SettingString)
                        {
                            var setting = (BGAbstractSettingsForEditor.SettingString) descriptor;
                            ui = () => { BGEditorUtility.TextField(descriptor.Name, setting.Value, b => setting.Value = b, false); };
                        }
                        else if (descriptor is BGAbstractSettingsForEditor.SettingFloat)
                        {
                            var setting = (BGAbstractSettingsForEditor.SettingFloat) descriptor;
                            ui = () => { BGEditorUtility.FloatField(descriptor.Name, setting.Value, b => setting.Value = b); };
                        }
                        else if (descriptor is BGAbstractSettingsForEditor.SettingColor)
                        {
                            var setting = (BGAbstractSettingsForEditor.SettingColor) descriptor;
                            ui = () => { BGEditorUtility.ColorField(descriptor.Name, setting.Value, b => setting.Value = b); };
                        }
                        else throw new UnityException("Unsupported type");

                        BGEditorUtility.VerticalBox(() =>
                        {
                            ui();
                            EditorGUILayout.HelpBox(descriptor.Description, MessageType.Info);
                        });
                    }
                });
            });
        }
    }
}