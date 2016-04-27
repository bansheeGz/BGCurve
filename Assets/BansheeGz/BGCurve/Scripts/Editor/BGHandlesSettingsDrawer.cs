using BansheeGz.BGSpline.Curve;
using UnityEngine;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{

    [CustomPropertyDrawer(typeof (BGCurveSettings.SettingsForHandles))]
    public class BGHandlesSettingsDrawer : PropertyDrawer
    {
        private const int Space = 5;

        private float cursorX;
        private float cursorY;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return base.GetPropertyHeight(property, label)*5;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var indent = EditorGUI.indentLevel;

            var height = base.GetPropertyHeight(property, label);


            EditorGUI.BeginProperty(position, label, property);


            //===================================================   Remove handles
            var rect = position;
            rect.height = height*2;
            var controlRect = EditorGUI.PrefixLabel(rect, GUIUtility.GetControlID(FocusType.Passive), new GUIContent("Remove"));
            EditorGUI.indentLevel = 0;

            cursorX = controlRect.x;
            cursorY = controlRect.y;

            //first line
            const int checkBoxWidth = 10;
            var labelWidth = 15;

            NextField(checkBoxWidth, height, property, "RemoveX");
            NextLabel(labelWidth, height, "X");
            NextField(checkBoxWidth, height, property, "RemoveY");
            NextLabel(labelWidth, height, "Y");
            NextField(checkBoxWidth, height, property, "RemoveZ");
            NextLabel(labelWidth, height, "Z");

            //next line
            cursorX = controlRect.x;
            cursorY += height;
            labelWidth = 20;

            NextField(checkBoxWidth, height, property, "RemoveXZ");
            NextLabel(labelWidth, height, "XZ");
            NextField(checkBoxWidth, height, property, "RemoveXY");
            NextLabel(labelWidth, height, "XY");
            NextField(checkBoxWidth, height, property, "RemoveYZ");
            NextLabel(labelWidth, height, "YZ");
            EditorGUI.indentLevel = indent;


            //===================================================   Scale Axis
            rect.y += height*2;
            rect.height = height;
            controlRect = EditorGUI.PrefixLabel(rect, GUIUtility.GetControlID(FocusType.Passive), new GUIContent("Scale Axis"));
            EditorGUI.indentLevel = 0;
            EditorGUI.PropertyField(controlRect, property.FindPropertyRelative("AxisScale"), GUIContent.none);
            EditorGUI.indentLevel = indent;

            //===================================================   Scale Planes
            rect.y += height;
            controlRect = EditorGUI.PrefixLabel(rect, GUIUtility.GetControlID(FocusType.Passive), new GUIContent("Scale Planes"));
            EditorGUI.indentLevel = 0;
            EditorGUI.PropertyField(controlRect, property.FindPropertyRelative("PlanesScale"), GUIContent.none);
            EditorGUI.indentLevel = indent;

            //===================================================   Alpha
            rect.y += height;
            controlRect = EditorGUI.PrefixLabel(rect, GUIUtility.GetControlID(FocusType.Passive), new GUIContent("Alpha"));
            EditorGUI.indentLevel = 0;
            EditorGUI.PropertyField(controlRect, property.FindPropertyRelative("Alpha"), GUIContent.none);
            EditorGUI.indentLevel = indent;
            
           

            EditorGUI.EndProperty();
        }

        private void NextField(int width, float height, SerializedProperty property, string name)
        {
            EditorGUI.PropertyField(new Rect(cursorX, cursorY, width, height), property.FindPropertyRelative(name), GUIContent.none);
            cursorX += width + Space;
        }

        private void NextLabel(int width, float height, string label)
        {
            EditorGUI.LabelField(new Rect(cursorX, cursorY, width, height), label);
            cursorX += width + Space;
        }

    }
}