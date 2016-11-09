using System;
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


        //===============================================================  Curve itself
        [SerializeField] [Tooltip("Hide Game Object's handles")] private bool hideHandles;

        [SerializeField] [Tooltip("Distance from the camera, at which new points are created")] private float newPointDistance = 2;

        [SerializeField] [Tooltip("Show curve in the scene or not. If not, all handles are disabled as well")] private bool showCurve = true;

        [SerializeField] [Tooltip("Show curve mode")] private ShowCurveModeEnum showCurveMode = ShowCurveModeEnum.CurveOrParentSelected;

        [SerializeField] [Range(1, 50)] [Tooltip("Number of sections between two curves points.\r\n It's used for displaying in editor only")] private int sections = 20;


        [SerializeField] [Tooltip("Show Points Menu buttons in the editor (for Points tab)")] private bool showPointMenu = true;

        //tangents
        [SerializeField] [Tooltip("Show points tangents in the scene")] private bool showTangents;
        [SerializeField] [Tooltip("Point tangent arrow size in the scene")] [Range(.3f, 2)] private float tangentsSize = .7f;
        [SerializeField] [Tooltip("Point tangent color in the scene")] private Color tangentsColor = Color.white;
        [SerializeField] [Range(1, 3)] [Tooltip("Number of tangents for every section")] private int tangentsPerSection = 1;

        // control type for new points
        [SerializeField] [Tooltip("Control type for new points")] private BGCurvePoint.ControlTypeEnum controlType;

        [SerializeField] [Tooltip("Curve is drawn on top of objects")] private bool vRay;

        [SerializeField] [Tooltip("Curve's color in the scene")] private Color lineColor = Color.red;


        //===============================================================  Control Type field

        [SerializeField] [Tooltip("Show points control types in the editor (for Points tab)")] private bool showPointControlType = true;

        //===============================================================  Position field

        [SerializeField] [Tooltip("Show points positions in the editor (for Points tab)")] private bool showPointPosition = true;

        [SerializeField] [Tooltip("Show points handles in the scene")] private bool showHandles = true;


        [SerializeField] [Tooltip("Points handles type" +
                                  "\r\n 1)FreeMove- standard Unity freemove handles" +
                                  "\r\n 2)Standard-standard handles" +
                                  "\r\n 3)Configurable- configurable handles")] private HandlesTypeEnum handlesType = HandlesTypeEnum.Configurable;


        [SerializeField] private SettingsForHandles handlesSettings = new SettingsForHandles();

        [SerializeField] [Tooltip("Show points positions labels in the scene ")] private bool showLabels = true;

        [SerializeField] [Tooltip("Point's labels color in the scene")] private Color labelColor = Color.white;


        [SerializeField] [Tooltip("Show points positions in the scene")] private bool showPositions;

        [SerializeField] [Tooltip("Point's labels color when selected in the scene")] private Color labelColorSelected = Color.green;

        [SerializeField] [Tooltip("Show spheres at points locations in the scene")] private bool showSpheres = true;

        [SerializeField] [Range(.01f, 1)] [Tooltip("Point's sphere radius in the scene")] private float sphereRadius = .1f;

        [SerializeField] [Tooltip("Point's sphere color in the scene")] private Color sphereColor = Color.red;


        //===============================================================  Controls fields

        [SerializeField] [Tooltip("Show points controls positions in the editor (for Points tab)")] private bool showPointControlPositions = true;

        [SerializeField] [Tooltip("Show points control handles in the scene")] private bool showControlHandles = true;

        [SerializeField] [Tooltip("Points control handles type\r\n 1)FreeMove- standard Unity freemove handles\r\n " +
                                  "2)Standard-standard handles\r\n 3)Configurable- configurable handles")] private HandlesTypeEnum controlHandlesType = HandlesTypeEnum.Configurable;

        [SerializeField] private SettingsForHandles controlHandlesSettings = new SettingsForHandles {AxisScale = .7f, PlanesScale = .7f, Alpha = .7f};

        [SerializeField] [Tooltip("Points control handles color")] private Color controlHandlesColor = Color.cyan;

        [SerializeField] [Tooltip("Show points control labels in the scene ")] private bool showControlLabels = true;

        [SerializeField] [Tooltip("Show points control positions in the scene")] private bool showControlPositions;

        [SerializeField] [Tooltip("Control point's labels color in the scene")] private Color labelControlColor = Color.yellow;

        //===============================================================  Transform field

        [SerializeField] [Tooltip("Show points transform field in the editor (for Points tab)")] private bool showTransformField;

        //===============================================================  Misc

        [SerializeField] private bool existing;

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

        public ShowCurveModeEnum ShowCurveMode
        {
            get { return showCurveMode; }
            set { showCurveMode = value; }
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

        #endregion
    }
#endif
}