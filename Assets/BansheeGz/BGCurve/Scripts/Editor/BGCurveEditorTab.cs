using UnityEngine;

namespace BansheeGz.BGSpline.Editor
{

    public interface BGCurveEditorTab
    {
        Texture2D GetHeader();

        void OnInspectorGUI();
        void OnSceneGUI();

    }
}