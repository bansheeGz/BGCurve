using System;
using UnityEngine;
using BansheeGz.BGSpline.EditorHelpers;
using UnityEditor;
using UnityEditor.AnimatedValues;

namespace BansheeGz.BGSpline.Editor
{
    public static class BGEditorUtility
    {
        private const float Tolerance = 0.00001f;
        private static DragSession dragSession = new DragSession();


        // ==============================================  OnInspectorGui utils
        public static void FadeGroup(BoolAnimatedProperty boolProperty, Action callback)
        {
            Vertical("Box", () =>
            {
                boolProperty.Property.boolValue = boolProperty.AnimBool.target = EditorGUILayout.ToggleLeft(boolProperty.Property.displayName, boolProperty.Property.boolValue);
                if (EditorGUILayout.BeginFadeGroup(boolProperty.AnimBool.faded))
                {
                    StartIndent(1);
                    callback();
                    EndIndent(1);
                }
                EditorGUILayout.EndFadeGroup();
            });
        }

        public static bool Foldout(bool foldout, string content)
        {
            var position = GUILayoutUtility.GetRect(40f, 40f, 16f, 16f, EditorStyles.foldout);
            return EditorGUI.Foldout(position, foldout, content, true, EditorStyles.foldout);
        }

        public static void EndIndent(int steps = 2)
        {
            EditorGUI.indentLevel -= steps;
        }

        public static void StartIndent(int steps = 2)
        {
            EditorGUI.indentLevel += steps;
        }

        // ==============================================  Layout
        public static void Vertical(Action callback)
        {
            EditorGUILayout.BeginVertical();
            callback();
            EditorGUILayout.EndVertical();
        }

        public static void Vertical(GUIStyle style, Action callback)
        {
            EditorGUILayout.BeginVertical(style);
            callback();
            EditorGUILayout.EndVertical();
        }

        public static void Horizontal(Action callback)
        {
            EditorGUILayout.BeginHorizontal();
            callback();
            EditorGUILayout.EndVertical();
        }

        public static void Horizontal(GUIStyle style, Action callback)
        {
            EditorGUILayout.BeginHorizontal(style);
            callback();
            EditorGUILayout.EndVertical();
        }

        // ==============================================  Fields
        public static void Vector3Field(string name, string tooltip, Vector3 value, Action<Vector3> action)
        {
            var newValue = EditorGUILayout.Vector3Field(new GUIContent(name) {tooltip = tooltip}, value);
            if (AnyChange(newValue, value))
            {
                action(newValue);
            }
        }

        //loads a texture2d
        public static Texture2D LoadTexture2D(string name, string path = "Assets/BansheeGz/BGCurve/Icons/", string ext = ".png")
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path + name + ext);
        }

        // ==============================================  Buttons with texture
        // texture is not getting scaled!
        public static bool ButtonWithIcon(int width, int height, Texture2D icon, string tooltip)
        {
            return GUI.Button(
                GUILayoutUtility.GetRect(width, width, height, height,
                    new GUIStyle {fixedWidth = width, fixedHeight = height, stretchWidth = false, stretchHeight = false}),
                new GUIContent(icon, tooltip), GUIStyle.none);
        }


        // ==============================================  Custom handles

        public static Vector3 ControlHandleCustom(int number, Vector3 position, Quaternion rotation, BGHandlesSettings handlesSettings)
        {
            var handleSize = HandleUtility.GetHandleSize(position);
            var axisSize = handleSize*handlesSettings.AxisScale;

            var color = Handles.color;


            if (!handlesSettings.RemoveX)
            {
                position = AxisHandle(position, rotation, axisSize, Handles.xAxisColor, "xAxis", Vector3.right, "MoveSnapX", handlesSettings.Alpha);
            }

            if (!handlesSettings.RemoveY)
            {
                position = AxisHandle(position, rotation, axisSize, Handles.yAxisColor, "yAxis", Vector3.up, "MoveSnapY", handlesSettings.Alpha);
            }

            if (!handlesSettings.RemoveZ)
            {
                position = AxisHandle(position, rotation, axisSize, Handles.zAxisColor, "zAxis", Vector3.forward, "MoveSnapZ", handlesSettings.Alpha);
            }

            if (!handlesSettings.RemoveXZ)
            {
                position = PlanarHandle("xz" + number, position, rotation, handleSize*.3f*handlesSettings.PlanesScale, Vector3.right, Vector3.forward, Handles.yAxisColor);
            }
            if (!handlesSettings.RemoveYZ)
            {
                position = PlanarHandle("yz" + number, position, rotation, handleSize*.3f*handlesSettings.PlanesScale, Vector3.up, Vector3.forward, Handles.xAxisColor);
            }
            if (!handlesSettings.RemoveXY)
            {
                position = PlanarHandle("xy" + number, position, rotation, handleSize*.3f*handlesSettings.PlanesScale, Vector3.up, Vector3.right, Handles.zAxisColor);
            }

            Handles.color = color;
            return position;
        }

        private static Vector3 AxisHandle(Vector3 position, Quaternion rotation, float handleSize, Color color, string controlName, Vector3 direction, string snapKey, float alpha)
        {
            Handles.color = new Color(color.r, color.g, color.b, alpha);
            GUI.SetNextControlName(controlName);
            return Handles.Slider(position, rotation*direction, handleSize, Handles.ArrowCap, EditorPrefs.GetFloat(snapKey, 1f));
        }

        // =======================================================================================
        //  This is a hell lot of a hackery & bad code & probably bugs. 
        //  todo rewrite
        // =======================================================================================
        private static Vector3 PlanarHandle(string name, Vector3 position, Quaternion rotation, float size, Vector3 direction1, Vector3 direction2, Color color)
        {
            var c = Handles.color;
            Handles.color = color;
            var v1 = rotation*(direction1*size);
            var v2 = rotation*(direction2*size);
            var verts = new[] {position + v1 + v2, position + v1 - v2, position - v1 - v2, position - v1 + v2};

            //adjustments
            var cameraPos = Camera.current.transform.position;
            var min = float.MaxValue;


            var minIndex = -1;
            var dragCheck = dragSession.Active(name);
            if (dragCheck > 0)
            {
                minIndex = dragCheck;
            }
            else
            {
                for (var i = 0; i < verts.Length; i++)
                {
                    var magnitude = (cameraPos - verts[i]).sqrMagnitude;
                    if ((min < magnitude)) continue;

                    minIndex = i;
                    min = magnitude;
                }

                dragSession.NextMin(minIndex, name);
            }

            for (var i = 0; i < verts.Length; i++)
            {
                if (i != minIndex)
                {
                    verts[i] = Vector3.Lerp(verts[i], verts[minIndex], .5f);
                }
            }

            var center = (verts[0] + verts[1] + verts[2] + verts[3])*.25f;
            Handles.DrawSolidRectangleWithOutline(verts, new Color(color.r, color.g, color.r, .1f), new Color(0.0f, 0.0f, 0.0f, 0.0f));

            GUI.SetNextControlName(name);

            var newCenter = Handles.Slider2D(center, rotation*Vector3.Cross(direction1, direction2), rotation*direction1, rotation*direction2, size*.5f, Handles.RectangleCap, 0);

            if (AnyChange(center, newCenter))
            {
                position = position + (newCenter - center);
            }
            Handles.color = c;
            return position;
        }


        // ============================================= Misc functions
        public static bool AnyChange(Vector3 vector1, Vector3 vector2)
        {
            return Math.Abs(vector1.x - vector2.x) > Tolerance || Math.Abs(vector1.y - vector2.y) > Tolerance || Math.Abs(vector1.z - vector2.z) > Tolerance;
        }

        public static bool AnyChange(float value1, float value2)
        {
            return Math.Abs(value1 - value2) > Tolerance;
        }


        // ============================================= Helper classes
        public class BoolAnimatedProperty
        {
            public readonly SerializedProperty Property;
            public readonly AnimBool AnimBool = new AnimBool();

            public BoolAnimatedProperty(UnityEditor.Editor editor, SerializedProperty obj, string name)
            {
                Property = obj.FindPropertyRelative(name);
                AnimBool.target = Property.boolValue;
                AnimBool.valueChanged.AddListener(editor.Repaint);
            }
        }

        private class DragSession
        {
            private bool dragging;

            private int minIndex;
            private bool needtoRecalculate;
            private string lastActiveName;


            public int Active(string name)
            {
                var currentEvent = Event.current;
                if (currentEvent.type == EventType.MouseUp || currentEvent.type == EventType.MouseDown)
                {
                    needtoRecalculate = true;
                }

                if (needtoRecalculate || GUIUtility.hotControl == 0 || !string.Equals(GUI.GetNameOfFocusedControl(), name) || !string.Equals(lastActiveName,name))
                {
                    return -1;
                }
                return minIndex;
            }

            public void NextMin(int minIndex, string name)
            {
                if (!string.Equals(GUI.GetNameOfFocusedControl(), name) || GUIUtility.hotControl == 0)
                {
                    return;
                }
                lastActiveName = name;
                needtoRecalculate = false;
                this.minIndex = minIndex;
            }
        }
    }
}