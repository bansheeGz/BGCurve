using System.Collections.Generic;
using System.IO;
using BansheeGz.BGSpline.Curve;
using UnityEngine;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
    public static class BGCurveSettingsOperations
    {
        private const string DefaultFileName = "BGDefaultSettings123";

        private const string EditorKey = "BansheeGZ.BGCurve.settingsDir";

        private static string[] allSettings;
        private static bool dirty;

        public static BGCurveSettings LoadDefault()
        {
            return Load(DefaultFileName);
        }

        public static BGCurveSettings Load(string asset)
        {
            var dir = GetPath();
            if (dir == null || !IsValid(dir)) return null;

            var loaded = AssetDatabase.LoadAssetAtPath<BGCurveSettingsSO>(GetFullPath(dir, asset));
            return loaded == null ? null : Object.Instantiate(loaded).Settings;
        }


        public static bool SaveDefault(BGCurveSettings settings)
        {
            return Save(settings, DefaultFileName);
        }

        public static bool Save(BGCurveSettings settings, string name)
        {
            var dir = GetPath();
            if (!IsValid(dir)) dir = null;

            if (dir == null) dir = ChoseFolder();

            if (dir == null) return false;

            var settingsSo = ScriptableObject.CreateInstance<BGCurveSettingsSO>();
            settingsSo.Settings = settings;

            var fullPath = GetFullPath(dir, name);
            AssetDatabase.CreateAsset(settingsSo, fullPath);
            AssetDatabase.SaveAssets();
            dirty = true;
            return true;
        }

        private static string GetFullPath(string dir, string asset)
        {
            return dir + Path.DirectorySeparatorChar + asset + ".asset";
        }

        private static string ChoseFolder()
        {
            var dir = EditorUtility.OpenFolderPanel("Chose a folder to store default settings", Application.dataPath, "");

            if (dir == null || dir.Equals("")) return null;


            if (!dir.StartsWith(Application.dataPath))
            {
                Debug.Log("Failed. Path should be relative to project folder");
                dir = null;
            }
            else
            {
                //this was a stupid idea to include "Assets" to the path
                dir = "Assets" + dir.Substring(Application.dataPath.Length);
                EditorPrefs.SetString(EditorKey, dir);
                dirty = true;
            }
            return dir;
        }

        public static void ChoseDir()
        {
            ChoseFolder();
        }

        public static string GetPath()
        {
            var path = EditorPrefs.GetString(EditorKey);
            return path == null || path.Equals("") ? null : path;
        }

        public static bool IsValid(string dir)
        {
            //this was a stupid idea to include "Assets" to the path
            return dir != null && !dir.Equals("") && Directory.Exists(Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length) + dir);
        }

        public static string[] GetAll()
        {
            var path = GetPath();
            if (!IsValid(path)) return new string[0];

            if (allSettings == null || dirty)
            {
                dirty = false;
                Reload(path);
            }

            return allSettings;
        }

        public static void Reload(string path)
        {
            var guids = AssetDatabase.FindAssets("t:BGCurveSettingsSO", new[] {path});
            if (guids != null && guids.Length > 0)
            {
                var pathes = new List<string>();
                foreach (var guid in guids)
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    var separatorIndex = assetPath.LastIndexOf(Path.DirectorySeparatorChar);

                    if (separatorIndex > 0) assetPath = assetPath.Substring(separatorIndex + 1);

                    separatorIndex = assetPath.LastIndexOf(Path.AltDirectorySeparatorChar);

                    if (separatorIndex > 0) assetPath = assetPath.Substring(separatorIndex + 1);

                    if (assetPath.EndsWith(".asset")) assetPath = assetPath.Substring(0, assetPath.Length - 6);

                    pathes.Add(assetPath);
                }
                allSettings = pathes.ToArray();
            }
            else
            {
                allSettings = new string[0];
            }
        }
    }
}