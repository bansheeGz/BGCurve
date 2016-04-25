using UnityEngine;

namespace BansheeGz.BGSpline.Editor
{

    public interface BGCurveEditorTab
    {
        // texture for the tab
        Texture2D GetHeader();

        // standard onInspector call
        void OnInspectorGUI();

        //standard onscene
        void OnSceneGUI();

        // standard onEnable
        void OnEnable();

        //before applying the changes
        void OnBeforeApply();

        //after applying the changes
        void OnApply();
    }
}