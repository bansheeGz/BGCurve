using System;
using UnityEngine;
using BansheeGz.BGSpline.Curve;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
    public class BGRectangularSelection
    {
        private const int BorderTextureSize = 8;
        private static readonly Color BorderColor = new Color32(255, 255, 255, 100);

        private Texture2D back;
        private readonly Texture2D borderHorizontal;
        private readonly Texture2D borderVertical;

        private readonly BGCurve curve;
        private readonly BGCurveEditorPointsSelection selection;
        private readonly BGCurveEditor editor;

        private Vector2 start;
        private bool selectionWasMade;
        public bool IsOn { get; private set; }

        private Color backColor = BGCurveSettingsForEditor.I.Get<Color32>(BGCurveSettingsForEditor.ColorForRectangularSelectionKey);

        public BGRectangularSelection(BGCurveEditor editor, BGCurveEditorPointsSelection selection)
        {
            this.editor = editor;
            this.selection = selection;
            curve = editor.Curve;

            borderHorizontal = CreateBorder(false);
            borderVertical = CreateBorder(true);
        }

        private Texture2D CreateBorder(bool vertical)
        {
            var texture = vertical ? new Texture2D(1, BorderTextureSize) : new Texture2D(BorderTextureSize, 1);

            var noColor = new Color();
            for (var i = 0; i < BorderTextureSize; i++)
            {
                var color = i > 3 ? noColor : BorderColor;

                if (vertical) texture.SetPixel(0, i, color);
                else texture.SetPixel(i, 0, color);
            }
            texture.Apply(false);
            return texture;
        }


        public void On()
        {
            IsOn = true;
            selectionWasMade = false;
            start = Event.current.mousePosition;
            SceneView.RepaintAll();

            //colors and texture
            var currentColor = BGCurveSettingsForEditor.I.Get<Color32>(BGCurveSettingsForEditor.ColorForRectangularSelectionKey);
            if (back == null || backColor != currentColor)
            {
                backColor = currentColor;
                back = BGEditorUtility.Texture1X1(backColor);
            }
        }

        public void Off()
        {
            if (!IsOn) return;
            IsOn = false;

            if (selectionWasMade) return;

            if (selection.HasSelected())
            {
                selection.Clear();
                SceneView.RepaintAll();
            }
        }

        public void Process(Event currentEvent)
        {
            if (!IsOn) return;

            var end = currentEvent.mousePosition;

            //we check if mouse pointer was moved
            if (!selectionWasMade && (end - start).sqrMagnitude > 1) selectionWasMade = true;

            if (!selectionWasMade) return;

            var rect = new Rect(Math.Min(start.x, end.x), Math.Min(start.y, end.y), Math.Abs(start.x - end.x), Math.Abs(start.y - end.y));

            //========================= Model
            //update points
            UpdatePoints(rect);

            //========================= UI
            Ui(rect);

            SceneView.RepaintAll();
            editor.Repaint();
        }

        private void UpdatePoints(Rect rect)
        {
            var sceneViewHeight = BGEditorUtility.GetSceneViewHeight();

            var math = editor.Math;

            curve.ForEach((point, index, count) =>
            {
                //add or remove from selection
                if (rect.Contains(BGEditorUtility.GetSceneViewPosition(math.GetPosition(index), sceneViewHeight))) selection.Add(point);
                else selection.Remove(point);
            });

            if (!selection.Changed) return;

            selection.Reset();
        }


        private void Ui(Rect rect)
        {
            BGEditorUtility.HandlesGui(() =>
            {
                var size = rect.size;
                var width = Mathf.FloorToInt(Math.Abs(size.x));
                var height = Mathf.FloorToInt(Math.Abs(size.y));
                if (width == 0 || height == 0) return;

                //------ back
                GUI.DrawTexture(rect, back, ScaleMode.StretchToFill);

                //------ borders
                //top
                DrawBorder(new Rect(rect) { width = width, height = height == 1 ? 1 : 2 }, false);
                if (height <= 2) return;

                //bottom
                DrawBorder(new Rect(rect) { y = rect.yMax - 2, width = width, height = 2 }, false);

                if (width <= 4 || height <= 4) return;

                //left
                DrawBorder(new Rect(rect) { width = 2 }, true);
                //right
                DrawBorder(new Rect(rect) { x = rect.xMax - 2, width = 2 }, true);
            });
        }

        private void DrawBorder(Rect rect, bool vertical)
        {
            float widthScale = 1, heightScale = 1;
            Texture2D texture;
            if (vertical)
            {
                texture = borderVertical;
                heightScale = rect.height/BorderTextureSize;
            }
            else
            {
                texture = borderHorizontal;
                widthScale = rect.width/BorderTextureSize;
            }

            GUI.DrawTextureWithTexCoords(rect, texture, new Rect(0, 0, widthScale, heightScale));
        }
    }
}