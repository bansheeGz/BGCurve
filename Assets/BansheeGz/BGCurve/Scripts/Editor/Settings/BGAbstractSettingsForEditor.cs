using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
    public abstract class BGAbstractSettingsForEditor
    {
        private readonly Dictionary<string, object> keyToSetting = new Dictionary<string, object>();

        public virtual string Name {get { return "N/A"; } }

        public ICollection<string> Keys
        {
            get { return keyToSetting.Keys; }
        }

        public SettingDescriptor GetSetting(string key)
        {
            return (SettingDescriptor) keyToSetting[key];
        } 

        public T Get<T>(string key)
        {
            var setting = keyToSetting[key];
            return ((Setting<T>) setting).Value;
        }

        public void Set<T>(string key, T value)
        {
            var setting = keyToSetting[key];
            ((Setting<T>) setting).Value = value;
        }

        //resets to default
        public void Reset()
        {
/*
                        //old implementation
                        var constants = typeof(BGCurveSettingsForEditor).GetFields(
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                            .Where(c => c.IsLiteral && !c.IsInitOnly && c.Name.EndsWith("Key")).ToList();
            
                        foreach (var constant in constants) EditorPrefs.DeleteKey((string)constant.GetValue(null));
            */
            var keys = keyToSetting.Keys;
            foreach (var key in keys) EditorPrefs.DeleteKey(key);

            LoadAll();
        }


        protected void AddSetting<T>(Setting<T> setting)
        {
            keyToSetting[setting.Key] = setting;
        }

        protected void LoadAll()
        {
            foreach (var setting in keyToSetting.Values) ((SettingDescriptor) setting).Load();
        }


        public abstract class SettingDescriptor
        {
            private readonly string key;
            private readonly string name;
            private readonly string description;

            public string Key
            {
                get { return key; }
            }

            public string Name
            {
                get { return name; }
            }

            public string Description
            {
                get { return description; }
            }

            protected SettingDescriptor(string key, string name, string description)
            {
                this.key = key;
                this.name = name;
                this.description = description;
            }

            public abstract void Load();

        }

        public abstract class Setting<T> : SettingDescriptor
        {
            protected readonly T DefaultValue;
            protected T value;

            public T Value
            {
                get { return value; }
                set { Save(value); }
            }

            protected Setting(string key, string name, string description, T defaultValue):base(key, name, description)
            {
                DefaultValue = defaultValue;
                Load();
            }

            protected static void Save(ref T oldValue, T newValue, Action notEqualAction)
            {
                if (oldValue.Equals(newValue)) return;
                oldValue = newValue;
                notEqualAction();
            }

            protected void Save(T value, Action newValue)
            {
                Save(ref this.value, value, newValue);
            }

            protected abstract void Save(T value);
        }

        public class SettingBool : Setting<bool>
        {
            public SettingBool(string key, string name, string description, bool defaultValue) : base(key, name, description, defaultValue)
            {
            }

            public override void Load()
            {
                value = EditorPrefs.GetBool(Key, DefaultValue);
            }

            protected override void Save(bool value)
            {
                Save(value, () => EditorPrefs.SetBool(Key, value));
            }
        }

        public class SettingInt : Setting<int>
        {
            public SettingInt(string key, string name, string description, int defaultValue) : base(key, name, description, defaultValue)
            {
            }

            public override void Load()
            {
                value = EditorPrefs.GetInt(Key, DefaultValue);
            }

            protected override void Save(int value)
            {
                Save(value, () => EditorPrefs.SetInt(Key, value));
            }
        }

        public class SettingFloat : Setting<float>
        {
            public SettingFloat(string key, string name, string description, float defaultValue) : base(key, name, description, defaultValue)
            {
            }

            public override void Load()
            {
                value = EditorPrefs.GetFloat(Key, DefaultValue);
            }

            protected override void Save(float value)
            {
                Save(value, () => EditorPrefs.SetFloat(Key, value));
            }
        }

        public class SettingString : Setting<string>
        {
            public SettingString(string key, string name, string description, string defaultValue) : base(key, name, description, defaultValue)
            {
            }

            public override void Load()
            {
                value = EditorPrefs.GetString(Key, DefaultValue);
            }

            protected override void Save(string value)
            {
                Save(value, () => EditorPrefs.SetString(Key, value));
            }
        }

        public class SettingColor : Setting<Color32>
        {
            public SettingColor(string key, string name, string description, Color32 defaultValue) : base(key, name, description, defaultValue)
            {
            }

            public override void Load()
            {
                value = StringToColor(EditorPrefs.GetString(Key, ColorToString(DefaultValue)), DefaultValue);
            }

            protected override void Save(Color32 value)
            {
                Save(value, () => EditorPrefs.SetString(Key, ColorToString(value)));
            }

            private static string ColorToString(Color32 color)
            {
                return color.r + "," + color.g + "," + color.b + "," + color.a;
            }

            private static Color32 StringToColor(string colorString, Color32 defaultColor)
            {
                if (string.IsNullOrEmpty(colorString)) return defaultColor;

                var parts = colorString.Split(',');
                if (parts.Length != 4) return defaultColor;


                try
                {
                    return new Color32(byte.Parse(parts[0]), byte.Parse(parts[1]), byte.Parse(parts[2]), byte.Parse(parts[3]));
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    return defaultColor;
                }
            }
        }

        public class SettingEnum: SettingInt
        {
            public Func<int, int> Ui;

            public SettingEnum(string key, string name, string description, int defaultValue, Func<int, int> ui) : base(key, name, description, defaultValue)
            {
                this.Ui = ui;
            }
        }

    }
}