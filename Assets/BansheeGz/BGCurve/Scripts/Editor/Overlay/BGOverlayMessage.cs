using UnityEditor;
using UnityEngine;

namespace BansheeGz.BGSpline.Editor
{
    public class BGOverlayMessage
    {
        private const float ShowTime = 1.5f;

        private string message;
        private double started;
        private bool showing;

        private GUIStyle style;
        private readonly Rect rect = new Rect(10, 10, 400, 60);


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
            if (showing && EditorApplication.timeSinceStartup - started > ShowTime) SceneView.RepaintAll();
        }

        // shows a message in the scene view
        public void OnSceneGui()
        {
            if (!showing) return;

            if (EditorApplication.timeSinceStartup - started > ShowTime) showing = false;

            BGEditorUtility.Assign(ref style, () => new GUIStyle("Label")
            {
                alignment = TextAnchor.MiddleCenter,
                richText = true,
                normal = {textColor = Color.white, background = BGEditorUtility.Texture1X1(new Color32(0, 0, 0, 100))}
            });

            BGEditorUtility.HandlesGui(() => GUI.Label(rect, message, style));
        }
    }
}