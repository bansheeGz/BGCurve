using UnityEngine;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
    //scene view context actions (Ctrl pressed)
    //idea.. whole BGSceneViewOverlay thing needs to be refactored
    public sealed class BGSceneViewOverlay
    {
        private static readonly string HeaderColor = BGEditorUtility.ToHex(new Color32(255, 255, 255, 255));
//        private static readonly string ErrorColor = ToHex(new Color32(122, 0, 0, 255));
        private static readonly string ErrorColor = BGEditorUtility.ToHex(new Color32(250, 207, 207, 255));
//        private static readonly string OkColor = ToHex(new Color32(0, 122, 0, 255));
        private static readonly string OkColor = BGEditorUtility.ToHex(new Color32(207, 250, 209, 255));
//        private static readonly string ActionColor = ToHex(new Color32(46, 143, 168, 255));
        private static readonly string ActionColor = BGEditorUtility.ToHex(Color.white);

        internal readonly BGCurveEditorPoints Editor;
        private GUIStyle style;

        private readonly SceneAction[] actions;

        //some kind of standard trick
        public BGEditorUtility.EventCanceller EventCanceller;

        private bool On;

        public BGSceneViewOverlay(BGCurveEditorPoints editor, BGCurveEditorPointsSelection editorSelection)
        {
            Editor = editor;

            actions = new SceneAction[]
            {
                new BGSceneViewOverlayMenuSelection(this, editorSelection),
                new BGSceneViewOverlayMenuPoint(this, editorSelection),
                new BGSceneViewOverlayPointInsert(this),
                new BGSceneViewOverlayPointAddAtDistance(this),
                new BGSceneViewOverlayPointAddSnap3D(this),
                new BGSceneViewOverlayPointAddSnap2D(this),
            };
        }

        private void Message(SceneAction action, Vector3 position, string message)
        {
            BGEditorUtility.SwapHandlesColor(new Color32(46, 143, 168, 255), () =>
            {
                // idea.. some hacker's work that needs to be redone
                var error = message.IndexOf(ErrorColor) != -1;

                Handles.Label(position, "      "
                                        + "<size=16><b>"
                                        + BGEditorUtility.ColorIt("Action[", HeaderColor)
                                        + BGEditorUtility.ColorIt(action.Name, ActionColor)
                                        + BGEditorUtility.ColorIt("] ", HeaderColor)
                                        + (error ? ToError("Error") : ToOk("Ok"))
                                        + "</b></size>\r\n"
                                        + message, style);
            });
        }


        public void Process(Event currentEvent)
        {
            if (!On && currentEvent.control) On = true;

            if (!On) return;

            if (On && !currentEvent.control) On = false;

            if (currentEvent.type == EventType.MouseUp) BGEditorUtility.Release(ref EventCanceller);

            if (currentEvent.shift && !currentEvent.control) return;

            if(!BGEditorUtility.IsMouseInsideSceneView()) return;

            BGEditorUtility.Assign(ref style, () => new GUIStyle("Label")
            {
                padding = new RectOffset(4, 4, 4, 4),
                border = new RectOffset(4, 4, 4, 4),
                fontStyle = FontStyle.Bold,
                richText = true,
                normal = new GUIStyleState
                {
                    textColor = Color.white,
                    background = BGEditorUtility.LoadTexture2D(BGEditorUtility.Image.BGBoxWithBorder123)
                }
            });


            foreach (var action in actions)
            {
                var position = Vector3.zero;
                string message = null;

                if (!action.Seize(currentEvent, ref position, ref message)) continue;

                if (message != null) Message(action, position, message);
                break;
            }

            if (currentEvent.control && currentEvent.type != EventType.Repaint) SceneView.RepaintAll();
        }

        public static string ToError(string error)
        {
            return BGEditorUtility.ColorIt(error, ErrorColor);
        }

        public static string ToOk(string okMessage)
        {
            return BGEditorUtility.ColorIt(okMessage, OkColor);
        }

        //identify particular action
        public abstract class SceneAction
        {
            protected readonly BGSceneViewOverlay overlay;

            public abstract string Name { get; }

            protected SceneAction(BGSceneViewOverlay overlay)
            {
                this.overlay = overlay;
            }

            //return true if event is seized by this action. if seized, Position and message should also be set in this case
            internal abstract bool Seize(Event currentEvent, ref Vector3 position, ref string message);
        }

        public static void DrawHandlesGuiTexture(Vector2 screenPoint, BGTransition.SwayTransition pointIndicatorTransition, Texture2D pointSelectedTexture)
        {
            pointIndicatorTransition.Tick();

            var shift = pointIndicatorTransition.Value*.5f;

            BGEditorUtility.HandlesGui(() =>
            {
                GUI.DrawTexture(
                    new Rect(screenPoint - new Vector2(shift, shift), new Vector2(pointIndicatorTransition.Value, pointIndicatorTransition.Value)),
                    pointSelectedTexture, ScaleMode.StretchToFill);
            });
        }
    }
}