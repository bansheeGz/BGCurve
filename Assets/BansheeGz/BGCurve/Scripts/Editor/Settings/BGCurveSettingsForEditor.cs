using System;
using UnityEngine;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
    public static class BGCurveSettingsForEditor
    {
        //Keys
        private const string DisableRectangularSelectionKey = "BansheeGZ.BGCurve.disableRectangularSelection";
        private const string DisableSceneViewPointMenuKey = "BansheeGZ.BGCurve.disableSceneViewPointMenu";
        private const string DisableSceneViewSelectionMenuKey = "BansheeGZ.BGCurve.disableSceneViewSelectionMenu";
        private const string DisableInspectorPointMenuKey = "BansheeGZ.BGCurve.disableInspectorPointMenu";

        private const string LockViewKey = "BansheeGZ.BGCurve.lockView";
        private const string CurrentTabKey = "BansheeGZ.BGCurve.currentTab";

        private const string HandleColorForAddAndSnap3DKey = "BansheeGZ.BGCurve.handleColorForAddAndSnap3D";
        private const string HandleColorForAddAndSnap2DKey = "BansheeGZ.BGCurve.handleColorForAddAndSnap2D";
        private const string ColorForRectangularSelectionKey = "BansheeGZ.BGCurve.colorForRectangularSelection";

        //Default values
        private static readonly Color32 HandleColorForAddAndSnap3DDefault = new Color32(46, 143, 168, 20);
        private static readonly Color32 HandleColorForAddAndSnap2DDefault = new Color32(255, 255, 255, 10);
        private static readonly Color32 ColorForRectangularSelectionDefault = new Color32(46, 143, 168, 25);


        private static bool disableRectangularSelection;
        private static bool disableSceneViewPointMenu;
        private static bool disableSceneViewSelectionMenu;
        private static bool disableInspectorPointMenu;
        private static bool lockView;

        private static Color32 handleColorForAddAndSnap3D;
        private static Color32 handleColorForAddAndSnap2D;
        private static Color32 colorForRectangularSelection;

        private static int currentTab;

        public static int CurrentTab
        {
            get { return currentTab; }
            set { SaveInt(ref currentTab, value, CurrentTabKey); }
        }

        public static bool LockView
        {
            get { return lockView; }
            set { SaveBool(ref lockView, value, LockViewKey); }
        }


        public static bool DisableRectangularSelection
        {
            get { return disableRectangularSelection; }
            set { SaveBool(ref disableRectangularSelection, value, DisableRectangularSelectionKey); }
        }

        public static bool DisableSceneViewPointMenu
        {
            get { return disableSceneViewPointMenu; }
            set { SaveBool(ref disableSceneViewPointMenu, value, DisableSceneViewPointMenuKey); }
        }

        public static bool DisableSceneViewSelectionMenu
        {
            get { return disableSceneViewSelectionMenu; }
            set { SaveBool(ref disableSceneViewSelectionMenu, value, DisableSceneViewSelectionMenuKey); }
        }

        public static bool DisableInspectorPointMenu
        {
            get { return disableInspectorPointMenu; }
            set { SaveBool(ref disableInspectorPointMenu, value, DisableInspectorPointMenuKey); }
        }


        public static Color32 HandleColorForAddAndSnap3D
        {
            get { return handleColorForAddAndSnap3D; }
            set { SaveColor(ref handleColorForAddAndSnap3D, value, HandleColorForAddAndSnap3DKey); }
        }

        public static Color32 HandleColorForAddAndSnap2D
        {
            get { return handleColorForAddAndSnap2D; }
            set { SaveColor(ref handleColorForAddAndSnap2D, value, HandleColorForAddAndSnap2DKey); }
        }

        public static Color32 ColorForRectangularSelection
        {
            get { return colorForRectangularSelection; }
            set { SaveColor(ref colorForRectangularSelection, value, ColorForRectangularSelectionKey); }
        }

        static BGCurveSettingsForEditor()
        {
            Init();
        }

        private static void SaveBool(ref bool oldValue, bool newValue, string key)
        {
            CheckAndSave(ref oldValue, newValue, () => EditorPrefs.SetBool(key, newValue));
        }

        private static void SaveInt(ref int oldValue, int newValue, string key)
        {
            CheckAndSave(ref oldValue, newValue, () => EditorPrefs.SetInt(key, newValue));
        }

        private static void SaveColor(ref Color32 oldValue, Color32 newValue, string key)
        {
            CheckAndSave(ref oldValue, newValue, () => EditorPrefs.SetString(key, ColorToString(newValue)));
        }

        private static void CheckAndSave<T>(ref T oldValue, T newValue, Action notEqualAction)
        {
            if (oldValue.Equals(newValue)) return;
            oldValue = newValue;
            notEqualAction();
        }

        private static void Init()
        {
            disableRectangularSelection = EditorPrefs.GetBool(DisableRectangularSelectionKey);
            disableSceneViewPointMenu = EditorPrefs.GetBool(DisableSceneViewPointMenuKey);
            disableSceneViewSelectionMenu = EditorPrefs.GetBool(DisableSceneViewSelectionMenuKey);
            disableInspectorPointMenu = EditorPrefs.GetBool(DisableInspectorPointMenuKey);

            lockView = EditorPrefs.GetBool(LockViewKey);
            currentTab = EditorPrefs.GetInt(CurrentTabKey);


            handleColorForAddAndSnap3D = StringToColor(EditorPrefs.GetString(HandleColorForAddAndSnap3DKey), HandleColorForAddAndSnap3DDefault);
            handleColorForAddAndSnap2D = StringToColor(EditorPrefs.GetString(HandleColorForAddAndSnap2DKey), HandleColorForAddAndSnap2DDefault);
            colorForRectangularSelection = StringToColor(EditorPrefs.GetString(ColorForRectangularSelectionKey), ColorForRectangularSelectionDefault);
        }

        //resets to default
        public static void Reset()
        {
            var constants = (typeof (BGCurveSettingsForEditor)).GetFields(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(c => c.IsLiteral && !c.IsInitOnly && c.Name.EndsWith("Key")).ToList();

            foreach (var constant in constants) EditorPrefs.DeleteKey(constant.Name);

            Init();
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
}