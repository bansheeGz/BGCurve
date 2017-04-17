using System;
using UnityEditor;
using UnityEngine;

namespace BansheeGz.BGSpline.Editor
{
    internal sealed class BGTableView
    {
        private const float Offset = 10;
        private const int TitleOffset = 10;

        private readonly string[] headers; //headers
        private readonly int[] sizes; //column sizes percentages
        private readonly float height; //line height

        private readonly GUIStyle headerStyle;
        private readonly GUIStyle cellStyle;
        private readonly GUIStyle titleStyle;
        private readonly GUIStyle centeredLabelStyle;

        private readonly string title;

        public GUIStyle CenteredLabelStyle
        {
            get { return centeredLabelStyle; }
        }


        private Rect lastRect; //last rect from Layout manager

        public float Width { get; private set; }

        public float Height
        {
            get { return rows*height + Offset; }
        }

        public int[] Sizes
        {
            get { return sizes; }
        }

        private float rows; //not data, but ui lines 
        private Vector2 cursor;
        private int currentColumn;
        private Action onGuiAction;

        public BGTableView(string title, string[] headers, int[] sizes, Action onGuiAction)
        {
            this.headers = headers;
            this.sizes = sizes;
            this.title = title;
            this.onGuiAction = onGuiAction;

            headerStyle = new GUIStyle(GetStyle(BGEditorUtility.Image.BGBoxWithBorder123));
            centeredLabelStyle = new GUIStyle("Label") {alignment = TextAnchor.MiddleCenter};

            cellStyle = GetStyle(BGEditorUtility.Image.BGTableCell123);
            titleStyle = GetStyle(BGEditorUtility.Image.BGTableTitle123);
            titleStyle.border = titleStyle.padding = new RectOffset(TitleOffset, TitleOffset, 2, 2);

            height = cellStyle.CalcSize(new GUIContent("Test")).y;
        }

        private void Init()
        {
            //---------------------------------- init sizes
            lastRect = GUILayoutUtility.GetLastRect();
            Width = lastRect.width;
            cursor = new Vector2(lastRect.xMin, lastRect.yMax + Offset);
            rows = 0;
            currentColumn = 0;

            //---------------------------------- title
            EditorGUI.LabelField(new Rect(cursor.x, cursor.y, titleStyle.CalcSize(new GUIContent(title)).x + TitleOffset*2, height), title, titleStyle);
            NextRow();
        }

        //---------------------------------- headers
        public void DrawHeaders()
        {
            for (var i = 0; i < headers.Length; i++)
            {
                NextColumn(rect => EditorGUI.LabelField(rect, headers[i]), sizes[i], true);
            }
            NextRow();
        }

        private static GUIStyle GetStyle(BGEditorUtility.Image background)
        {
            return new GUIStyle("Label")
            {
                padding = new RectOffset(2, 2, 2, 2),
                border = new RectOffset(2, 2, 2, 2),
                normal = new GUIStyleState
                {
                    background = BGEditorUtility.LoadTexture2D(background)
                }
            };
        }

        public void NextColumn(string label, Action<Rect> action, int widthInPercent = 0, GUIStyle labelStyle = null, bool header = false)
        {
            var columnWidth = Width*(widthInPercent > 0 ? widthInPercent : sizes[currentColumn])/100f;

            var rect = new Rect(cursor.x, cursor.y, columnWidth, height);
            EditorGUI.LabelField(rect, "", header ? headerStyle : cellStyle);

            if (label != null)
            {
                rect.width /= 2;
                EditorGUI.LabelField(rect, label, labelStyle ?? GUI.skin.label);
                rect.x += rect.width;
            }

            if (action != null) action(rect);

            cursor.x += columnWidth;
            currentColumn++;
        }

        public void NextColumn(Action<Rect> action, int widthInPercent = 0, bool header = false)
        {
            NextColumn(null, action, widthInPercent, header: header);
        }

        public void NextColumn(string label, string description, int widthInPercent = 0, bool header = false)
        {
            NextColumn(rect => EditorGUI.LabelField(rect, new GUIContent(label, description)), widthInPercent, header);
        }

        public void NextRow()
        {
            cursor.x = lastRect.xMin;
            cursor.y += height;
            currentColumn = 0;
            rows++;
        }

        public void NextRow(string message)
        {
            EditorGUI.LabelField(new Rect(cursor.x, cursor.y, Width, height), message, cellStyle);
            NextRow();
        }

        public void OnGui(bool drawHeader = false)
        {
            Init();

            if(drawHeader) DrawHeaders();

            onGuiAction();

            //inform layout manager
            GUILayoutUtility.GetRect(Width, Height);

        }
    }
}