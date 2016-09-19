using System;
using UnityEngine;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
    //idea.. code is very messy even after refactoring
    //there are currently 2 strategies to draw- 1) by using labelRect + controlRect. 2) By using cursors. This is a total mess.
    public class BGPropertyDrawer : PropertyDrawer
    {
        private const int Space = 5;

        //one line height
        protected float Height;

        //space to use for property GUI
        protected Rect Rect;
        //space used for show Control calls
        protected Rect ControlRect;

        //additional cursors for *ByCursor calls. 
        protected float CursorX;
        protected float CursorY;

        // startUp (Required!)
        protected void SetUp(Rect position, SerializedProperty property, GUIContent label, Action action)
        {
            Rect = position;
            //do not remove base ref
            Height = base.GetPropertyHeight(property, label);
            EditorGUI.BeginProperty(position, label, property);
            action();
            EditorGUI.EndProperty();
        }

        //helper with indents
        protected void Indent(int indent, Action action)
        {
            var oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = indent;
            action();
            EditorGUI.indentLevel = oldIndent;
        }


        //=======================================================   By labelRect + controlRect

        protected void PrefixLabel(string message, string tooltip = null)
        {
            ControlRect = EditorGUI.PrefixLabel(Rect, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(message, tooltip));
        }

        protected void DrawProperty(SerializedProperty property)
        {
//            PrefixLabel(property.displayName, property.tooltip);
            EditorGUI.PropertyField(Rect, property);
        }


        protected bool RelativeProperty(SerializedProperty property, string name)
        {
            return EditorGUI.PropertyField(ControlRect, property.FindPropertyRelative(name), GUIContent.none);
        }

        protected void DrawRelativeProperty(SerializedProperty property, string label, string propertyName, string tooltip = null)
        {
            PrefixLabel(label, tooltip);
            Indent(0, () => RelativeProperty(property, propertyName));
        }


        protected void SetHeight(float height)
        {
            Rect.height = height;
        }

        protected void NextLine(int linesToSkip = 1)
        {
            Rect.y += Height*linesToSkip;
        }


        //=======================================================   By Cursor
        protected void RelativePropertyByCursor(int width, float height, SerializedProperty property, string name)
        {
            EditorGUI.PropertyField(new Rect(CursorX, CursorY, width, height), property.FindPropertyRelative(name), GUIContent.none);
            CursorX += width + Space;
        }

        protected void LabelByCursor(int width, float height, string label)
        {
            EditorGUI.LabelField(new Rect(CursorX, CursorY, width, height), label);
            CursorX += width + Space;
        }

        protected void SetCursor(float x, float y)
        {
            CursorX = x;
            CursorY = y;
        }
    }
}