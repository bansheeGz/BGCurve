using System;
using UnityEngine;

namespace BansheeGz.BGSpline.EditorHelpers
{
    // ========================== This class is supposed to work in Editor ONLY
#if UNITY_EDITOR

    [Serializable]
    public class BGHandlesSettings
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
#endif
}