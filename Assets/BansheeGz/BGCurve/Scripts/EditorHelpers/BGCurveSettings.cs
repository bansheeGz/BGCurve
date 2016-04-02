using System;
using UnityEngine;
using BansheeGz.BGSpline.Curve;

namespace BansheeGz.BGSpline.EditorHelpers
{
#if UNITY_EDITOR
    // ========================== This class is supposed to work in Editor ONLY

// editor related props
    [Serializable]
    public class BGCurveSettings
    {
        public enum HandlesTypeEnum
        {
            Configurable,
            Standard,
            FreeMove
        }

        #region Fields

        //===============================================================  Points options
        [SerializeField]
        [Tooltip("Distance from the camera, at which new points are created")]
        private float newPointDistance = 2;

        [SerializeField]
        [Tooltip("Show points control types in the editor (for Points tab)")]
        private bool showPointControlType = true;

        [SerializeField]
        [Tooltip("Show points positions in the editor (for Points tab)")]
        private bool showPointPosition = true;

        [SerializeField]
        [Tooltip("Show points controls positions in the editor (for Points tab)")]
        private bool showPointControlPositions = true;

        [SerializeField]
        [Tooltip("Show points menu in the editor (for Points tab)")]
        private bool showPointMenu = true;


        //===============================================================  Show Curve
        [SerializeField] [Tooltip("Show curve in the scene")] private bool showCurve = true;

        [SerializeField] [Tooltip("Show points handlers in the scene")] private bool showHandles = true;

        [SerializeField] [Tooltip("Points handles type\r\n 1)FreeMove- standard Unity freemove handlers\r\n 2)Standard-standard handlers\r\n 3)Configurable- configurable handlers")] private
            HandlesTypeEnum
            handlesType = HandlesTypeEnum.Configurable;

        [SerializeField] private BGHandlesSettings handlesSettings = new BGHandlesSettings();

        [SerializeField] [Range(1, 50)] [Tooltip("Number of sections between two curves points.\r\n It's used for displaying in editor only")] private int sections = 20;

        [SerializeField] [Tooltip("Curve is drawn on top of objects")] private bool vRay;

        [SerializeField] [Tooltip("Curve's color in the scene")] private Color lineColor = Color.red;


        //===============================================================  Control Handles
        [SerializeField] [Tooltip("Show points control handles in the scene")] private bool showControlHandles = true;

        [SerializeField] [Tooltip("Points control handles type\r\n 1)FreeMove- standard Unity freemove handlers\r\n 2)Standard-standard handlers\r\n 3)Configurable- configurable handlers")] private
            HandlesTypeEnum controlHandlesType = HandlesTypeEnum.Configurable;

        [SerializeField] private BGHandlesSettings controlHandlesSettings = new BGHandlesSettings {AxisScale = .7f, PlanesScale = .7f, Alpha = .7f};

        [SerializeField] [Tooltip("Points control handles color")] private Color controlHandlesColor = Color.cyan;


        //===============================================================  Labels
        [SerializeField] [Tooltip("Show points labels in the scene ")] private bool showLabels = true;

        [SerializeField] [Tooltip("Show points positions in the scene")] private bool showPositions;

        [SerializeField] [Tooltip("Show points control positions in the scene")] private bool showControlPositions;

        [SerializeField] [Tooltip("Point's labels color in the scene")] private Color labelColor = Color.black;

        [SerializeField] [Tooltip("Point's labels color when selected in the scene")] private Color labelColorSelected = Color.green;


        //===============================================================  Spheres at points positions
        [SerializeField] [Tooltip("Show spheres at points locations in the scene")] private bool showSpheres = true;

        [SerializeField] [Range(.1f, 1)] [Tooltip("Point's sphere radius in the scene")] private float sphereRadius = .1f;

        [SerializeField] [Tooltip("Point's sphere color in the scene")] private Color sphereColor = Color.red;

        //===============================================================  Misc
        // control type for new points
        [SerializeField] [Tooltip("Control type for new points")] private BGCurvePoint.ControlTypeEnum controlType;

        [SerializeField] private bool existing;

        #endregion

        #region Props

        public bool ShowSpheres
        {
            get { return showSpheres; }
        }

        public float SphereRadius
        {
            get { return sphereRadius; }
        }

        public Color LineColor
        {
            get { return lineColor; }
        }

        public Color SphereColor
        {
            get { return sphereColor; }
        }


        public int Sections
        {
            get { return sections; }
        }

        public Color LabelColorSelected
        {
            get { return labelColorSelected; }
        }

        public Color LabelColor
        {
            get { return labelColor; }
        }

        public bool ShowLabels
        {
            get { return showLabels; }
        }

        public bool ShowPositions
        {
            get { return showPositions; }
        }


        public BGCurvePoint.ControlTypeEnum ControlType
        {
            get { return controlType; }
            set { controlType = value; }
        }

        public Color ControlHandlesColor
        {
            get { return controlHandlesColor; }
        }

        public HandlesTypeEnum ControlHandlesType
        {
            get { return controlHandlesType; }
        }

        public bool ShowControlHandles
        {
            get { return showControlHandles; }
        }

        public bool ShowCurve
        {
            get { return showCurve; }
        }

        public bool VRay
        {
            get { return vRay; }
        }

        public bool ShowHandles
        {
            get { return showHandles; }
        }

        public bool ShowControlPositions
        {
            get { return showControlPositions; }
        }


        public BGHandlesSettings ControlHandlesSettings
        {
            get { return controlHandlesSettings; }
        }

        public BGHandlesSettings HandlesSettings
        {
            get { return handlesSettings; }
        }

        public HandlesTypeEnum HandlesType
        {
            get { return handlesType; }
        }

        public bool ShowPointControlType
        {
            get { return showPointControlType; }
        }

        public bool ShowPointPosition
        {
            get { return showPointPosition; }
        }

        public bool ShowPointControlPositions
        {
            get { return showPointControlPositions; }
        }

        public bool Existing
        {
            get { return existing; }
            set { existing = value; }
        }

        public float NewPointDistance
        {
            get { return newPointDistance; }
        }

        public bool ShowPointMenu
        {
            get { return showPointMenu; }
        }

        #endregion
    }
#endif
}