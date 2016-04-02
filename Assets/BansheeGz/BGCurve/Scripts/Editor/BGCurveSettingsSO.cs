using System;
using UnityEngine;

namespace BansheeGz.BGSpline.EditorHelpers
{
    // ========================== This class is supposed to work in Editor ONLY
#if UNITY_EDITOR

    [Serializable]
    public class BGCurveSettingsSO : ScriptableObject
    {
        public BGCurveSettings Settings;
    }
#endif
}