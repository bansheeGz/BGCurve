using UnityEditor;
using UnityEngine;

namespace BansheeGz.BGSpline.Editor
{

    public class BGEditorPopup
    {

        private const int ShowTime = 1;

        private string message;
        private double started;
        private bool showing;

        private readonly GUIStyle style = new GUIStyle();
        private readonly Rect rect = new Rect(Screen.width*.25f, Screen.height*.5f, Screen.width*.5f, 60);

        public BGEditorPopup()
        {
            style.normal.textColor = Color.white;
            style.normal.background = BGEditorUtility.LoadTexture2D("BGBlack123");
            style.alignment = TextAnchor.MiddleCenter;
        }

        //indicates the message should be shown
        public void Display(string message)
        {
            this.message = message;
            started = EditorApplication.timeSinceStartup;
            showing = true;
        }

        // call this every frame to find out if the message expired
        public void Check()
        {
            if (showing && EditorApplication.timeSinceStartup - started > ShowTime)
            {
                SceneView.RepaintAll();
            }
        }

        // shows a message in the scene view
        public void Show()
        {
            if (!showing) return;

            if (EditorApplication.timeSinceStartup - started > ShowTime)
            {
                showing = false;
            }

            Handles.BeginGUI();
            GUI.Label(rect, message, style);
            Handles.EndGUI();
        }
    }

}