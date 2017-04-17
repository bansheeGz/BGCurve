using System;
using UnityEngine;

namespace BansheeGz.BGSpline.Editor
{
    public class BGCurveSettingsForEditor : BGAbstractSettingsForEditor
    {
        public static readonly BGCurveSettingsForEditor I = new BGCurveSettingsForEditor();

        public enum CoordinateSpaceEnum
        {
            Local = 0,
            LocalTransformed = 1,
            World = 2,
        }


        //Keys
        public const string InspectorPointsCoordinatesKey = "BansheeGZ.BGCurve.inspectorPointsCoordinates";
        public const string InspectorControlsCoordinatesKey = "BansheeGZ.BGCurve.inspectorControlsCoordinates";

        public const string DisableRectangularSelectionKey = "BansheeGZ.BGCurve.disableRectangularSelection";
        public const string DisableSceneViewPointMenuKey = "BansheeGZ.BGCurve.disableSceneViewPointMenu";
        public const string DisableSceneViewSelectionMenuKey = "BansheeGZ.BGCurve.disableSceneViewSelectionMenu";

        private const string CcInspectorHandlesOffKey = "BansheeGZ.BGCurve.inspectorHandlesOff";
        private const string DisableInspectorPointMenuKey = "BansheeGZ.BGCurve.disableInspectorPointMenu";
        private const string LockViewKey = "BansheeGZ.BGCurve.lockView";
        private const string CurrentTabKey = "BansheeGZ.BGCurve.currentTab";

        //colors
        public const string HandleColorForAddAndSnap3DKey = "BansheeGZ.BGCurve.handleColorForAddAndSnap3D";
        public const string HandleColorForAddAndSnap2DKey = "BansheeGZ.BGCurve.handleColorForAddAndSnap2D";
        public const string ColorForRectangularSelectionKey = "BansheeGZ.BGCurve.colorForRectangularSelection";
        public const string ColorForLabelBackgroundKey = "BansheeGZ.BGCurve.colorForLabelBackground";
        public const string ColorForNewSectionPreviewKey = "BansheeGZ.BGCurve.colorForNewSectionPreview";

        public override string Name
        {
            get { return "BG Curve Editor Settings"; }
        }


        public static bool CcInspectorHandlesOff
        {
            get { return I.Get<bool>(CcInspectorHandlesOffKey); }
            set { I.Set(CcInspectorHandlesOffKey, value); }
        }
        public static bool DisableInspectorPointMenu
        {
            get { return I.Get<bool>(DisableInspectorPointMenuKey); }
            set { I.Set(DisableInspectorPointMenuKey, value); }
        }

        public static bool LockView
        {
            get { return I.Get<bool>(LockViewKey); }
            set { I.Set(LockViewKey, value); }
        }
        public static int CurrentTab
        {
            get { return I.Get<int>(CurrentTabKey); }
            set { I.Set(CurrentTabKey, value); }
        }


        private BGCurveSettingsForEditor()
        {
            AddSetting(new SettingEnum(InspectorPointsCoordinatesKey,
                "Point's Coordinate Space",
                "Coordinate Space for points (for Inspector's fields inder Points tab.)",
                (int) CoordinateSpaceEnum.World,
                oldValue =>
                {
                    var newValue1 = oldValue;
                    BGEditorUtility.PopupField((CoordinateSpaceEnum) oldValue, "Point's Coordinate Space", b => newValue1 = Convert.ToInt32(b));
                    return newValue1;
                }
            ));
            AddSetting(new SettingEnum(InspectorControlsCoordinatesKey,
                "Point Controls Coordinates Space",
                "Coordinate Space for points controls (for Inspector's fields inder Points tab.)",
                (int) CoordinateSpaceEnum.Local,
                oldValue =>
                {
                    var newValue1 = oldValue;
                    BGEditorUtility.PopupField((CoordinateSpaceEnum)oldValue, "Point Controls Coordinates Space", b => newValue1 = Convert.ToInt32(b));
                    return newValue1;
                }
            ));

            AddSetting(new SettingBool(DisableRectangularSelectionKey,
                "Disable Rectangular Selection",
                "Disable rectangular selection in Scene View, which is activated by holding shift and mouse dragging.",
                false
            ));
            AddSetting(new SettingBool(DisableSceneViewPointMenuKey,
                "Disable SV Point Menu",
                "Disable point's menu, which is activated in Scene View by holding Ctrl over a point.",
                false
            ));
            AddSetting(new SettingBool(DisableSceneViewSelectionMenuKey,
                "Disable SV Selection Menu",
                "Disable selection's menu, which is activated in Scene View by holding Ctrl over a selection handles.",
                false
            ));

            AddSetting(new SettingBool(CcInspectorHandlesOffKey,
                null,
                null,
                false
            ));
            AddSetting(new SettingBool(DisableInspectorPointMenuKey,
                null,
                null,
                false
            ));
            AddSetting(new SettingBool(LockViewKey,
                null,
                null,
                false
            ));
            AddSetting(new SettingInt(CurrentTabKey,
                null,
                null,
                0
            ));

            AddSetting(new SettingColor(HandleColorForAddAndSnap3DKey,
                "Add and Snap 3D Handles Color",
                "Color for handles, shown for 3D curve in Scene View when new point is previewed.",
                new Color32(46, 143, 168, 20)
            ));
            AddSetting(new SettingColor(HandleColorForAddAndSnap2DKey,
                "Add and Snap 2D Handles Color",
                "Color for handles, shown for 2D curve in Scene View when new point is previewed.",
                new Color32(255, 255, 255, 10)
            ));
            AddSetting(new SettingColor(ColorForRectangularSelectionKey,
                "Rectangular Selection Color",
                "Color for Rectangular Selection background",
                new Color32(46, 143, 168, 25)
            ));
            AddSetting(new SettingColor(ColorForLabelBackgroundKey,
                "Points labels back color",
                "Background color for points labels in Scene View.",
                new Color32(255, 255, 255, 25)
            ));
            AddSetting(new SettingColor(ColorForNewSectionPreviewKey,
                "New section preview color",
                "Color for new section preview in Scene View.",
                new Color32(255, 0, 0, 255)
            ));
        }
    }
}