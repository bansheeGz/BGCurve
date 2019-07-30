using System;
using UnityEditor;
using UnityEngine;

namespace BansheeGz.BGSpline.Editor
{
    internal sealed class BGTableView
    {
        private const float Offset = 10;
        private const int TitleOffset = 16;

        private readonly string[] headers; //headers
        private readonly int[] sizes; //column sizes percentages
        private readonly float height; //line height

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
            get { return rows * height + Offset; }
        }

        public int[] Sizes
        {
            get { return sizes; }
        }

        private float rows; //not data, but ui lines 
        private Vector2 cursor;
        private int currentColumn;
        private readonly Action onGuiAction;

        private readonly bool darkTheme;

        private GUIStyle TableCellStyle
        {
            get
            {
                return GetStyle(darkTheme ? BGBinaryResources.BGTableCellDark123 : BGBinaryResources.BGTableCellLight123);
            }
        }
        private GUIStyle TableHeaderStyle
        {
            get
            {
                return GetStyle(darkTheme ? BGBinaryResources.BGTableHeaderDark123 : BGBinaryResources.BGTableHeaderLight123);
            }
        }
        private GUIStyle TableTitleStyle
        {
            get
            {
                var style = GetStyle(darkTheme ? BGBinaryResources.BGTableTitleDark123 : BGBinaryResources.BGTableTitleLight123);
                style.border = style.padding = new RectOffset(TitleOffset, TitleOffset, 2, 2);
                return style;
            }
        }
        
        public BGTableView(string title, string[] headers, int[] sizes, Action onGuiAction)
        {
            this.headers = headers;
            this.sizes = sizes;
            this.title = title;
            this.onGuiAction = onGuiAction;

            centeredLabelStyle = new GUIStyle("Label") {alignment = TextAnchor.MiddleCenter};
            var normalTextColor = centeredLabelStyle.normal.textColor;
            darkTheme = normalTextColor.r > .5 && normalTextColor.g > .5 && normalTextColor.b > .5;

            height = TableCellStyle.CalcSize(new GUIContent("Test")).y;
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
            var titleStyle = TableTitleStyle;
            EditorGUI.LabelField(new Rect(cursor.x, cursor.y, titleStyle.CalcSize(new GUIContent(title)).x + TitleOffset * 2, height), title, titleStyle);
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

        private static GUIStyle GetStyle(Texture2D background)
        {
            var baseStyle = EditorStyles.label;
            return new GUIStyle(baseStyle)
            {
                padding = new RectOffset(2, 2, 2, 2),
                border = new RectOffset(2, 2, 2, 2),
                normal = new GUIStyleState
                {
                    background = background,
                    textColor = baseStyle.normal.textColor,
                }
            };
        }

        public void NextColumn(string label, Action<Rect> action, int widthInPercent = 0, GUIStyle labelStyle = null, bool header = false)
        {
            var columnWidth = Width * (widthInPercent > 0 ? widthInPercent : sizes[currentColumn]) / 100f;

            var rect = new Rect(cursor.x, cursor.y, columnWidth, height);

            EditorGUI.LabelField(rect, "", header ? TableHeaderStyle : TableCellStyle);

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
            EditorGUI.LabelField(new Rect(cursor.x, cursor.y, Width, height), message, TableCellStyle);
            NextRow();
        }

        public void OnGui(bool drawHeader = false)
        {
            Init();

            if (drawHeader) DrawHeaders();

            onGuiAction();

            //inform layout manager
            GUILayoutUtility.GetRect(Width, Height);
        }
    }
}