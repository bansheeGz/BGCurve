using System;
using System.Collections.Generic;
using UnityEngine;

namespace BansheeGz.BGSpline.Curve
{
#if UNITY_EDITOR
    // ========================== This class is supposed to work in Editor ONLY

    /// <summary> Warning!! This class is for Editor ONLY. It contains curve settings  </summary>
    // many tooltips and range attributes are currently not used
    [Serializable]
    public class BGCurveSettings
    {
        #region Fields & enums

        public enum HandlesTypeEnum
        {
            Configurable,
            Standard,
            FreeMove
        }

        public enum ShowCurveModeEnum
        {
            CurveSelected,
            CurveOrParentSelected,
            Always
        }

        public enum ShowCurveOptionsEnum
        {
            ThisCurveSelected,
            AnyCurveSelected,
        }


        //===============================================================  Curve itself
        [SerializeField] [Tooltip("Hide Game Object's handles")]
        private bool hideHandles;

        [SerializeField] [Tooltip("Distance from the camera, at which new points are created")]
        private float newPointDistance = 2;

        [SerializeField] [Tooltip("Show curve in the scene or not. If not, all handles are disabled as well")]
        private bool showCurve = true;

        [Obsolete] [SerializeField] [Tooltip("Show curve mode")]
        private ShowCurveModeEnum showCurveMode = ShowCurveModeEnum.CurveOrParentSelected;

        [SerializeField] [Tooltip("Show curve mode")]
        private ShowCurveOptionsEnum showCurveOption = ShowCurveOptionsEnum.ThisCurveSelected;


        [SerializeField] [Range(1, 50)] [Tooltip("Number of sections between two curves points.\r\n It's used for displaying in editor only")]
        private int sections = 20;


        [SerializeField] [Tooltip("Show Points Menu buttons in the editor (for Points tab)")]
        private bool showPointMenu = true;

        //tangents
        [SerializeField] [Tooltip("Show points tangents in the scene")]
        private bool showTangents;

        [SerializeField] [Tooltip("Point tangent arrow size in the scene")] [Range(.3f, 2)]
        private float tangentsSize = .7f;

        [SerializeField] [Tooltip("Point tangent color in the scene")]
        private Color tangentsColor = Color.white;

        [SerializeField] [Range(1, 3)] [Tooltip("Number of tangents for every section")]
        private int tangentsPerSection = 1;

        // control type for new points
        [SerializeField] [Tooltip("Control type for new points")]
        private BGCurvePoint.ControlTypeEnum controlType;

        [SerializeField] [Tooltip("Curve is drawn on top of objects")]
        private bool vRay;

        [SerializeField] [Tooltip("Curve's color in the scene")]
        private Color lineColor = Color.red;

        //restrict gizmoz
        [SerializeField] [Tooltip("Show gizmoz for selected points only. Format: 1-2,5,8 (Use comma to separate values, use hyphen to define a range)")]
        private string restrictGizmoz;

        //===============================================================  Control Type field

        [SerializeField] [Tooltip("Show points control types in the editor (for Points tab)")]
        private bool showPointControlType = true;

        //===============================================================  Position field

        [SerializeField] [Tooltip("Show points positions in the editor (for Points tab)")]
        private bool showPointPosition = true;

        [SerializeField] [Tooltip("Show points handles in the scene")]
        private bool showHandles = true;


        [SerializeField]
        [Tooltip("Points handles type" +
                 "\r\n 1)FreeMove- standard Unity freemove handles" +
                 "\r\n 2)Standard-standard handles" +
                 "\r\n 3)Configurable- configurable handles")]
        private HandlesTypeEnum handlesType = HandlesTypeEnum.Configurable;


        [SerializeField] private SettingsForHandles handlesSettings = new SettingsForHandles();

        [SerializeField] [Tooltip("Show points positions labels in the scene ")]
        private bool showLabels = true;

        [SerializeField] [Tooltip("Point's labels color in the scene")]
        private Color labelColor = Color.white;


        [SerializeField] [Tooltip("Show points positions in the scene")]
        private bool showPositions;

        [SerializeField] [Tooltip("Point's labels color when selected in the scene")]
        private Color labelColorSelected = Color.green;

        [SerializeField] [Tooltip("Show spheres at points locations in the scene")]
        private bool showSpheres = true;

        [SerializeField] [Range(.01f, 1)] [Tooltip("Point's sphere radius in the scene")]
        private float sphereRadius = .1f;

        [SerializeField] [Tooltip("Point's sphere color in the scene")]
        private Color sphereColor = Color.red;


        //===============================================================  Controls fields

        [SerializeField] [Tooltip("Show points controls positions in the editor (for Points tab)")]
        private bool showPointControlPositions = true;

        [SerializeField] [Tooltip("Show points control handles in the scene")]
        private bool showControlHandles = true;

        [SerializeField]
        [Tooltip("Points control handles type\r\n 1)FreeMove- standard Unity freemove handles\r\n " +
                 "2)Standard-standard handles\r\n 3)Configurable- configurable handles")]
        private HandlesTypeEnum controlHandlesType = HandlesTypeEnum.Configurable;

        [SerializeField] private SettingsForHandles controlHandlesSettings = new SettingsForHandles {AxisScale = .7f, PlanesScale = .7f, Alpha = .7f};

        [SerializeField] [Tooltip("Points control handles color")]
        private Color controlHandlesColor = Color.cyan;

        [SerializeField] [Tooltip("Show points control labels in the scene ")]
        private bool showControlLabels = true;

        [SerializeField] [Tooltip("Show points control positions in the scene")]
        private bool showControlPositions;

        [SerializeField] [Tooltip("Control point's labels color in the scene")]
        private Color labelControlColor = Color.yellow;

        //===============================================================  Transform field

        [SerializeField] [Tooltip("Show points transform field in the editor (for Points tab)")]
        private bool showTransformField;

        //===============================================================  Misc

        [SerializeField] private bool existing;

        private RestrictGizmozSetting restrictGizmozSettings = new RestrictGizmozSetting(null);

        #endregion

        #region Props

        public bool HideHandles
        {
            get { return hideHandles; }
            set { hideHandles = value; }
        }

        public float NewPointDistance
        {
            get { return newPointDistance; }
            set { newPointDistance = value; }
        }

        public bool ShowPointControlType
        {
            get { return showPointControlType; }
            set { showPointControlType = value; }
        }

        public bool ShowPointPosition
        {
            get { return showPointPosition; }
            set { showPointPosition = value; }
        }

        public bool ShowPointControlPositions
        {
            get { return showPointControlPositions; }
            set { showPointControlPositions = value; }
        }

        public bool ShowPointMenu
        {
            get { return showPointMenu; }
            set { showPointMenu = value; }
        }

        public bool ShowCurve
        {
            get { return showCurve; }
            set { showCurve = value; }
        }

        public string RestrictGizmoz
        {
            get { return restrictGizmoz; }
            set { restrictGizmoz = value; }
        }

        public RestrictGizmozSetting RestrictGizmozSettings
        {
            get { return restrictGizmozSettings = restrictGizmozSettings.Comply(restrictGizmoz); }
        }

        [Obsolete]
        public ShowCurveModeEnum ShowCurveMode
        {
            get { return showCurveMode; }
            set { showCurveMode = value; }
        }

        public ShowCurveOptionsEnum ShowCurveOption
        {
            get { return showCurveOption; }
            set { showCurveOption = value; }
        }

        public bool ShowHandles
        {
            get { return showHandles; }
            set { showHandles = value; }
        }

        public bool ShowTangents
        {
            get { return showTangents; }
            set { showTangents = value; }
        }

        public float TangentsSize
        {
            get { return tangentsSize; }
            set { tangentsSize = value; }
        }

        public Color TangentsColor
        {
            get { return tangentsColor; }
            set { tangentsColor = value; }
        }

        public int TangentsPerSection
        {
            get { return Mathf.Clamp(tangentsPerSection, 1, 3); }
            set { tangentsPerSection = Mathf.Clamp(value, 1, 3); }
        }

        public HandlesTypeEnum HandlesType
        {
            get { return handlesType; }
            set { handlesType = value; }
        }

        public SettingsForHandles HandlesSettings
        {
            get { return handlesSettings; }
            set { handlesSettings = value; }
        }

        public int Sections
        {
            get { return Mathf.Clamp(sections, 1, 50); }
            set { sections = Mathf.Clamp(value, 1, 50); }
        }

        public bool VRay
        {
            get { return vRay; }
            set { vRay = value; }
        }

        public Color LineColor
        {
            get { return lineColor; }
            set { lineColor = value; }
        }

        public bool ShowControlHandles
        {
            get { return showControlHandles; }
            set { showControlHandles = value; }
        }

        public HandlesTypeEnum ControlHandlesType
        {
            get { return controlHandlesType; }
            set { controlHandlesType = value; }
        }

        public SettingsForHandles ControlHandlesSettings
        {
            get { return controlHandlesSettings; }
            set { controlHandlesSettings = value; }
        }

        public Color ControlHandlesColor
        {
            get { return controlHandlesColor; }
            set { controlHandlesColor = value; }
        }

        public bool ShowLabels
        {
            get { return showLabels; }
            set { showLabels = value; }
        }

        public bool ShowPositions
        {
            get { return showPositions; }
            set { showPositions = value; }
        }

        public bool ShowControlPositions
        {
            get { return showControlPositions; }
            set { showControlPositions = value; }
        }

        public Color LabelColor
        {
            get { return labelColor; }
            set { labelColor = value; }
        }

        public Color LabelColorSelected
        {
            get { return labelColorSelected; }
            set { labelColorSelected = value; }
        }

        public bool ShowSpheres
        {
            get { return showSpheres; }
            set { showSpheres = value; }
        }

        public float SphereRadius
        {
            get { return sphereRadius; }
            set { sphereRadius = value; }
        }

        public Color SphereColor
        {
            get { return sphereColor; }
            set { sphereColor = value; }
        }

        public BGCurvePoint.ControlTypeEnum ControlType
        {
            get { return controlType; }
            set { controlType = value; }
        }

        public bool Existing
        {
            get { return existing; }
            set { existing = value; }
        }

        public bool ShowControlLabels
        {
            get { return showControlLabels; }
            set { showControlLabels = value; }
        }

        public Color LabelControlColor
        {
            get { return labelControlColor; }
            set { labelControlColor = value; }
        }

        public bool ShowTransformField
        {
            get { return showTransformField; }
            set { showTransformField = value; }
        }

        #endregion

        #region classes

        [Serializable]
        public class SettingsForHandles
        {
            public bool RemoveX;
            public bool RemoveY;
            public bool RemoveZ;

            public bool RemoveXZ;
            public bool RemoveXY;
            public bool RemoveYZ;

            [Range(.5f, 1.5f)] public float AxisScale = 1;

            [Range(.5f, 1.5f)] public float PlanesScale = 1;

            [Range(.5f, 1f)] public float Alpha = 1;


            public bool Disabled
            {
                get { return RemoveX && RemoveY && RemoveZ && RemoveXY && RemoveXZ && RemoveYZ; }
            }
        }

        public class RestrictGizmozSetting
        {
            private readonly List<KeyValuePair<int, int>> fromToList = new List<KeyValuePair<int, int>>();
            private readonly HashSet<int> numbersList = new HashSet<int>();

            private readonly string value;
            private bool valid;

            public bool HasValue
            {
                get { return !string.IsNullOrEmpty(value); }
            }

            public bool Valid
            {
                get { return valid; }
            }

            public RestrictGizmozSetting(string value)
            {
                this.value = value;
                try
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        var tokens = value.Split(',');
                        foreach (var token in tokens)
                        {
                            if (string.IsNullOrEmpty(token)) continue;
                            var range = token.Split('-');
                            switch (range.Length)
                            {
                                case 1:
                                {
                                    numbersList.Add(int.Parse(range[0]));
                                    break;
                                }
                                case 2:
                                {
                                    var token1 = range[0];
                                    var token2 = range[1];
                                    if (string.IsNullOrEmpty(token1) && string.IsNullOrEmpty(token2)) continue;

                                    if (string.IsNullOrEmpty(token1)) numbersList.Add(int.Parse(token2));
                                    else if (string.IsNullOrEmpty(token2)) numbersList.Add(int.Parse(token1));
                                    else fromToList.Add(new KeyValuePair<int, int>(int.Parse(token1), int.Parse(token2)));

                                    break;
                                }
                                default:
                                {
                                    throw new Exception();
                                }
                            }
                        }

                        valid = true;
                    }
                }
                catch
                {
                    //ignore
                }
            }

            public bool IsShowing(int point)
            {
                if (!valid) return true;
                if (numbersList.Contains(point)) return true;

                for (var i = 0; i < fromToList.Count; i++)
                {
                    var pair = fromToList[i];
                    if (pair.Key <= point && pair.Value >= point) return true;
                }

                return false;
            }

            public RestrictGizmozSetting Comply(string value)
            {
                return string.Equals(this.value, value) ? this : new RestrictGizmozSetting(value);
            }
        }

        #endregion
    }
#endif
}