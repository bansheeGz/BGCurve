using System;
using System.Collections.Generic;
using System.Linq;
using BansheeGz.BGSpline.Components;
using BansheeGz.BGSpline.Curve;
using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;

namespace BansheeGz.BGSpline.Editor
{
    public static class BGEditorUtility
    {
        private const string DefaultIconPath = "Assets/BansheeGz/BGCurve/Icons/";

        //exact names for icons
        public enum Image
        {
            BGCurveLogo123,
            BGDelete123,
            BGAdd123,
            BGMoveUp123,
            BGMoveDown123,
            BGPoints123,
            BGConvertAll123,
            BGTickNo123,
            BGTickYes123,
            BGSelectAll123,
            BGDeSelectAll123,
            BGSettings123,
            BGComponents123,
            BGFields123,
            BGTableHeader123,
            BGTableCell123,
            BGTableTitle123,
            BGBoxWithBorder123,
            BGPointSelected123,
            BGPointMenu123,
            BGControlAbsent123,
            BGControlBezierSymmetrical123,
            BGControlBezierIndependent123,
            BGPointDelete123,
            BGLockOff123,
            BGLockOn123,
            BGCcNoImage123,
            BGCurveComponents123,
            BGHelp123,
            BGBoxRed123,
            BGBoxWhite123,
            BGCollapseAll123,
            BGExpandAll123,
            BGHierarchyIcon123,
            BGHierarchySelectedIcon123,
            BGHierarchyErrorIcon123,
            BGHierarchyErrorSelectedIcon123,
            BGOn123,
            BGOff123,
            BGHierarchyWarningSelectedIcon123,
            BGHierarchyWarningIcon123,
            BGMenuItemBackground123,
            BGPointInsertAfter123,
            BGPointInsertBefore123,
            BGSelectionAdd123,
            BGSelectionRemove123,
            BGExpanded123,
            BGCollapsed123,
            BGSettingsIcon123,
            BGCcEditName123
        }

        private static readonly DragSession dragSession = new DragSession();
        private static Texture2D whiteTexture1x1;

        private static float xSnap = EditorPrefs.GetFloat("MoveSnapX", 1f);
        private static float ySnap = EditorPrefs.GetFloat("MoveSnapY", 1f);
        private static float zSnap = EditorPrefs.GetFloat("MoveSnapZ", 1f);

        private static string currentIconPath = DefaultIconPath;
        private static string additionalCcIconPath;

        static BGEditorUtility()
        {
            ReloadSnapSettings();
        }

        public static void ReloadSnapSettings()
        {
            xSnap = EditorPrefs.GetFloat("MoveSnapX", 1f);
            ySnap = EditorPrefs.GetFloat("MoveSnapY", 1f);
            zSnap = EditorPrefs.GetFloat("MoveSnapZ", 1f);
        }

        // ==============================================  OnSceneGui utils

        public static void SwapHandlesColor(Color color, Action action)
        {
            var oldColor = Handles.color;
            Handles.color = color;
            action();
            Handles.color = oldColor;
        }

        public static void SwapGizmosColor(Color color, Action action)
        {
            var oldColor = Gizmos.color;
            Gizmos.color = color;
            action();
            Gizmos.color = oldColor;
        }

        public static Vector2 GetSceneViewPosition(Vector3 position, float sceneViewHeight)
        {
            var screenPoint = SceneView.currentDrawingSceneView.camera.WorldToScreenPoint(position);
            return new Vector2(screenPoint.x, sceneViewHeight - screenPoint.y);
        }

        public static Vector2 GetSceneViewPosition(Vector3 position)
        {
            var screenPoint = SceneView.currentDrawingSceneView.camera.WorldToScreenPoint(position);
            return new Vector2(screenPoint.x, GetSceneViewHeight() - screenPoint.y);
        }

        public static float GetSceneViewHeight()
        {
            return SceneView.currentDrawingSceneView.camera.pixelHeight;
        }

        public static float GetHandleSize(Vector3 position, float scale = 1)
        {
            return HandleUtility.GetHandleSize(position)*scale;
        }

        // ==============================================  OnInspectorGui utils
        public static void SwapGuiBackgroundColor(Color color, Action action)
        {
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = color;

            Assign(ref whiteTexture1x1, () => Texture1X1(Color.white));

            try
            {
                var temp = whiteTexture1x1.width;
                temp.Equals(1);
            }
            catch (MissingReferenceException e)
            {
                e.HelpLink = e.HelpLink;
                //have no darn idea about any other way to reload the texture
                whiteTexture1x1 = Texture1X1(Color.white);
            }

            Horizontal(new GUIStyle(GUIStyle.none) {normal = {background = whiteTexture1x1}}, action);
            GUI.backgroundColor = oldColor;
        }

        public static void FadeGroup(BoolAnimatedProperty boolProperty, Action callback)
        {
            VerticalBox(() =>
            {
                boolProperty.Property.boolValue = boolProperty.AnimBool.target = EditorGUILayout.ToggleLeft(boolProperty.Property.displayName, boolProperty.Property.boolValue);
                if (EditorGUILayout.BeginFadeGroup(boolProperty.AnimBool.faded)) Indent(1, callback);
                EditorGUILayout.EndFadeGroup();
            });
        }

        public static void Indent(int steps, Action action)
        {
            StartIndent(steps);
            action();
            EndIndent(steps);
        }

        public static void EndIndent(int steps = 2)
        {
            EditorGUI.indentLevel -= steps;
        }

        public static void StartIndent(int steps = 2)
        {
            EditorGUI.indentLevel += steps;
        }

        public static bool ButtonOnOff(ref bool value, string name, string tooltip, Color onColor, GUIContent onContent, GUIContent offContent, Action additionalAction = null)
        {
            var myValue = value;
            Action action = () =>
            {
                EditorGUILayout.PrefixLabel(new GUIContent(name, tooltip));
                if (GUILayout.Button(myValue ? onContent : offContent)) myValue = !myValue;

                if (additionalAction != null) additionalAction();
            };

            if (myValue) SwapGuiBackgroundColor(onColor, action);
            else HorizontalBox(action);

            value = myValue;
            return myValue;
        }


        // ==============================================  Layout
        public static void Vertical(Action callback)
        {
            Vertical(GUIStyle.none, callback);
        }

        public static void Vertical(GUIStyle style, Action callback)
        {
            EditorGUILayout.BeginVertical(style);
            callback();
            EditorGUILayout.EndVertical();
        }

        public static void VerticalBox(Action callback)
        {
            Vertical("Box", callback);
        }

        public static void Horizontal(Action callback)
        {
            Horizontal(GUIStyle.none, callback);
        }

        public static void Horizontal(GUIStyle style, Action callback)
        {
            EditorGUILayout.BeginHorizontal(style);
            callback();
            EditorGUILayout.EndHorizontal();
        }

        public static void HorizontalBox(Action callback)
        {
            Horizontal("Box", callback);
        }


        // ==============================================  Fields
        public static void Vector3Field(string name, string tooltip, Vector3 value, Action<Vector3> action)
        {
            var newValue = EditorGUILayout.Vector3Field(new GUIContent(name) {tooltip = tooltip}, value);
            if (AnyChange(newValue, value)) action(newValue);
        }

        public static void ToggleField(bool value, string label, Action<bool> action)
        {
            var newValue = EditorGUILayout.Toggle(label, value);
            if (value != newValue) action(newValue);
        }

        public static void ToggleField(Rect rect, bool value, Action<bool> action)
        {
            var newValue = EditorGUI.Toggle(rect, value);
            if (value ^ newValue && action != null) action(newValue);
        }

        public static void ColorField(Rect rect, Color color, Action<Color> action)
        {
            var newValue = EditorGUI.ColorField(rect, color);
            if (AnyChange(color, newValue)) action(newValue);
        }

        public static void ColorField(Color32 color, string label, Action<Color> action)
        {
            var newValue = EditorGUILayout.ColorField(label, color);
            if (AnyChange(color, newValue)) action(newValue);
        }

        public static void SliderField(Rect rect, float value, float min, float max, Action<float> action)
        {
            var newValue = GUI.HorizontalSlider(rect, value, min, max);
            if (AnyChange(value, newValue)) action(newValue);
        }

        public static void PopupField(Rect rect, Enum value, Action<Enum> action)
        {
            var values = Enum.GetValues(value.GetType());

            var options = new string[values.Length];
            var selected = 0;
            for (var i = 0; i < values.Length; i++)
            {
                var val = values.GetValue(i);

                options[i] = val.ToString();
                if (Equals(value, val)) selected = i;
            }
            var newIndex = EditorGUI.Popup(rect, selected, options);

            if (newIndex != selected) action((Enum) values.GetValue(newIndex));
        }


        // ==============================================  Textures
        //loads a texture2d
        public static Texture2D LoadTexture2D(Image image, string path = DefaultIconPath, string ext = ".png", bool critical = true)
        {
            if (String.Equals(path, DefaultIconPath)) path = currentIconPath;

            var icon = LoadTexture(path + image + ext);
            if (icon != null) return icon;

            //try to find in assets
            var newPath = FindByName(Image.BGDelete123.ToString());
            if (newPath == null)
            {
                if (critical)
                {
                    // no luck
                    Debug.LogException(new UnityException(
                        "Can not find BGCurve Editors icons folder. The icons folder, located by default at '" + DefaultIconPath +
                        "', should be included in your assets, otherwise the package will not work correctly in Editor."));
                }
                return null;
            }

            currentIconPath = FolderFromFullPath(newPath);
            return LoadTexture(currentIconPath + image + ext);
        }

        public static Texture2D LoadCcTexture2D(string path)
        {
            if (String.IsNullOrEmpty(path)) return null;

            var icon = LoadTexture(path);

            if (icon == null)
            {
                string fileName;
                string fileNameNoExtension;
                try
                {
                    fileName = FileFromFullPath(path);
                    fileNameNoExtension = StripExtension(fileName);
                }
                catch (Exception e)
                {
                    e.HelpLink = e.HelpLink;
                    return null;
                }

                if (String.IsNullOrEmpty(fileName) || String.IsNullOrEmpty(fileNameNoExtension)) return null;

                //try to find in another place
                if (additionalCcIconPath != null) icon = LoadTexture(additionalCcIconPath + fileName);

                if (icon == null)
                {
                    var newPath = FindByName(fileNameNoExtension);
                    if (newPath != null)
                    {
                        var newFolder = FolderFromFullPath(newPath);
                        if (additionalCcIconPath == null) additionalCcIconPath = newFolder;
                        icon = LoadTexture(newFolder + fileName);
                    }
                }
            }
            return icon;
        }

        public static Texture2D LoadTexture(string path)
        {
            //we do hope LoadAssetAtPath is optimized by Unity
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static string FindByName(string fileName)
        {
            var files = AssetDatabase.FindAssets(fileName);
            return files == null || files.Length == 0 ? null : AssetDatabase.GUIDToAssetPath(files[0]);
        }

        private static string FolderFromFullPath(string path)
        {
            try
            {
                var maxSlashIndex = Mathf.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));
                return maxSlashIndex >= 0 ? path.Substring(0, maxSlashIndex + 1) : path;
            }
            catch (Exception e)
            {
                e.HelpLink = e.HelpLink;
                return null;
            }
        }

        private static string FileFromFullPath(string path)
        {
            try
            {
                var maxSlashIndex = Mathf.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));
                return maxSlashIndex >= 0 ? path.Substring(maxSlashIndex + 1, path.Length - maxSlashIndex - 1) : path;
            }
            catch (Exception e)
            {
                e.HelpLink = e.HelpLink;
                return null;
            }
        }

        private static string StripExtension(string fileName)
        {
            try
            {
                var dotIndex = Mathf.Max(fileName.LastIndexOf('.'));
                return dotIndex >= 0 ? fileName.Substring(0, dotIndex) : fileName;
            }
            catch (Exception e)
            {
                e.HelpLink = e.HelpLink;
                return null;
            }
        }


        // texture is not getting scaled!
        public static bool ButtonWithIcon(Texture2D icon, string tooltip, int width = 16, int height = 16)
        {
            return GUI.Button(
                GUILayoutUtility.GetRect(width, width, height, height,
                    new GUIStyle {fixedWidth = width, fixedHeight = height, stretchWidth = false, stretchHeight = false}),
                new GUIContent(icon, tooltip), GUIStyle.none);
        }

        public static Texture2D Texture1X1(Color color)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply(false);
            return texture;
        }

        // ==============================================  Dialogs
        public static void Inform(string title, string message)
        {
            EditorUtility.DisplayDialog(title, message, "Ok");
        }

        public static bool Confirm(string title, string message, string ok)
        {
            return EditorUtility.DisplayDialog(title, message, ok, "Cancel");
        }

        public static void HelpBox(string message, MessageType messageType, bool condition = true, Action elseAction = null)
        {
            if (condition) EditorGUILayout.HelpBox(message, messageType);
            else if (elseAction != null) elseAction();
        }


        // ==============================================  Custom handles

        public static Vector3 ControlHandleCustom(int number, Vector3 position, Quaternion rotation, BGCurveSettings.SettingsForHandles handlesSettings)
        {
            var handleSize = GetHandleSize(position);

            var axisSize = handleSize*handlesSettings.AxisScale;

            var color = Handles.color;

            if (!handlesSettings.RemoveX) position = AxisHandle(position, rotation, axisSize, Handles.xAxisColor, "xAxis", Vector3.right, xSnap, handlesSettings.Alpha);

            if (!handlesSettings.RemoveY) position = AxisHandle(position, rotation, axisSize, Handles.yAxisColor, "yAxis", Vector3.up, ySnap, handlesSettings.Alpha);

            if (!handlesSettings.RemoveZ) position = AxisHandle(position, rotation, axisSize, Handles.zAxisColor, "zAxis", Vector3.forward, zSnap, handlesSettings.Alpha);

            if (!handlesSettings.RemoveXZ) position = PlanarHandle("xz" + number, position, rotation, handleSize*.3f*handlesSettings.PlanesScale, Vector3.right, Vector3.forward, Handles.yAxisColor);

            if (!handlesSettings.RemoveYZ) position = PlanarHandle("yz" + number, position, rotation, handleSize*.3f*handlesSettings.PlanesScale, Vector3.up, Vector3.forward, Handles.xAxisColor);

            if (!handlesSettings.RemoveXY) position = PlanarHandle("xy" + number, position, rotation, handleSize*.3f*handlesSettings.PlanesScale, Vector3.up, Vector3.right, Handles.zAxisColor);

            Handles.color = color;
            return position;
        }

        private static Vector3 AxisHandle(Vector3 position, Quaternion rotation, float handleSize, Color color, string controlName, Vector3 direction, float snap, float alpha)
        {
            Handles.color = new Color(color.r, color.g, color.b, alpha);
            GUI.SetNextControlName(controlName);
            return Handles.Slider(position, rotation*direction, handleSize, Handles.ArrowCap, snap);
        }

        // =======================================================================================
        //  This is a hell lot of a hackery & bad code & probably bugs. 
        //  idea.. to rewrite
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
            var min = Single.MaxValue;


            var minIndex = -1;
            var dragCheck = dragSession.Active(name);

            if (dragCheck > 0) minIndex = dragCheck;
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

            for (var i = 0; i < verts.Length; i++) if (i != minIndex) verts[i] = Vector3.Lerp(verts[i], verts[minIndex], .5f);

            var center = (verts[0] + verts[1] + verts[2] + verts[3])*.25f;

            Handles.DrawSolidRectangleWithOutline(verts, new Color(color.r, color.g, color.r, .1f), new Color(0.0f, 0.0f, 0.0f, 0.0f));

            GUI.SetNextControlName(name);

            var newCenter = Handles.Slider2D(center, rotation*Vector3.Cross(direction1, direction2), rotation*direction1, rotation*direction2, size*.5f, Handles.RectangleCap, 0);

            if (AnyChange(center, newCenter)) position = position + (newCenter - center);

            Handles.color = c;
            return position;
        }


        // ============================================= Misc functions
        public static bool AnyChange(Vector3 vector1, Vector3 vector2)
        {
            return Math.Abs(vector1.x - vector2.x) > BGCurve.Epsilon || Math.Abs(vector1.y - vector2.y) > BGCurve.Epsilon || Math.Abs(vector1.z - vector2.z) > BGCurve.Epsilon;
        }

        public static bool AnyChange(float value1, float value2)
        {
            return Math.Abs(value1 - value2) > BGCurve.Epsilon;
        }

        private static bool AnyChange(Color32 color1, Color32 color2)
        {
            return color1.r != color2.r || color1.g != color2.g || color1.b != color2.b || color1.a != color2.a;
        }

        public static T Assign<T>(ref T nullable, Func<T> valueProvider) where T : class
        {
            return nullable ?? (nullable = valueProvider());
        }

        public static void Iterate<T>(T[] array, Action<int, T> action)
        {
            if (array == null || array.Length == 0) return;
            for (var i = 0; i < array.Length; i++) action(i, array[i]);
        }

        public static bool IterateInterruptable<T>(T[] array, Func<int, T, bool> action)
        {
            if (array == null || array.Length == 0) return false;
            if (array.Where((t, i) => action(i, t)).Any()) return true;
            return false;
        }

        public static TV Ensure<TK, TV>(Dictionary<TK, TV> key2Value, TK key, Func<TV> valueProvider)
        {
            if (key2Value.ContainsKey(key)) return key2Value[key];

            var newValue = valueProvider();
            key2Value[key] = newValue;
            return newValue;
        }

        public static List<TV> Ensure<TK, TV>(Dictionary<TK, List<TV>> key2Value, TK key)
        {
            if (key2Value.ContainsKey(key)) return key2Value[key];

            var newValue = new List<TV>();
            key2Value[key] = newValue;
            return newValue;
        }


        public static void SwapGuiColor(Color color, Action action)
        {
            var oldColor = GUI.color;
            GUI.color = color;
            action();
            GUI.color = oldColor;
        }

        public static void SwapLabelWidth(int width, Action action)
        {
            var oldValue = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = width;
            action();
            EditorGUIUtility.labelWidth = oldValue;
        }

        public static void DisableGui(Action action, bool condition = true)
        {
            var oldValue = false;
            if (condition)
            {
                oldValue = GUI.enabled;
                GUI.enabled = false;
            }
            action();
            if (condition) GUI.enabled = oldValue;
        }

        public static bool Empty<T>(T[] array)
        {
            return array == null || array.Length == 0;
        }

        public static bool Empty<T>(List<T> list)
        {
            return list == null || list.Count == 0;
        }

        public static string Trim(string value, int maxWidth)
        {
            if (value == null) return null;

            string result;

            if (value.Length < maxWidth)
            {
                result = value.Substring(0, value.Length);
            }
            else
            {
                result = value.Substring(0, maxWidth - "..".Length) + "..";
            }
            return result;
        }

        public static T ShowPopupWindow<T>(Vector2 size) where T : EditorWindow
        {
            //            var window = EditorWindow.GetWindow<T>();
            var window = ScriptableObject.CreateInstance<T>();

            var screenPoint = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
            window.ShowAsDropDown(new Rect(screenPoint.x, screenPoint.y, 16, 16), size);
            return window;
        }

        public static void DrawBound(Bounds bounds, Color color, Color bColor)
        {
            var min = bounds.min;
            var max = bounds.max;

            var xMin = min.x;
            var xMax = max.x;
            var yMin = min.y;
            var yMax = max.y;
            var zMin = min.z;
            var zMax = max.z;

            var p1 = new Vector3(xMin, yMin, zMin);
            var p2 = new Vector3(xMin, yMax, zMin);
            var p3 = new Vector3(xMax, yMax, zMin);
            var p4 = new Vector3(xMax, yMin, zMin);
            var p5 = new Vector3(xMin, yMin, zMax);
            var p6 = new Vector3(xMin, yMax, zMax);
            var p7 = new Vector3(xMax, yMax, zMax);
            var p8 = new Vector3(xMax, yMin, zMax);

            DrawRect(p1, p2, p3, p4, color, bColor);
            DrawRect(p1, p5, p6, p2, color, bColor);
            DrawRect(p2, p6, p7, p3, color, bColor);
            DrawRect(p3, p7, p8, p4, color, bColor);
            DrawRect(p4, p8, p5, p1, color, bColor);
            DrawRect(p5, p6, p7, p8, color, bColor);
        }


        public static void DrawRect(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Color color, Color bColor)
        {
            var verts = new[] {v1, v2, v3, v4};
            Handles.DrawSolidRectangleWithOutline(verts, color, bColor);
        }

        // ============================================= Curve related
        //this is very slow
        public static void Split(BGCurvePoint @from, BGCurvePoint to, int parts, Action<Vector3, Vector3> action)
        {
            var noControls = @from.ControlType == BGCurvePoint.ControlTypeEnum.Absent && to.ControlType == BGCurvePoint.ControlTypeEnum.Absent;
            if (noControls)
            {
                action(@from.PositionWorld, to.PositionWorld);
            }
            else
            {
                var fromPos = @from.PositionWorld;
                var toPos = to.PositionWorld;
                var control1 = @from.ControlSecondWorld;
                var control2 = to.ControlFirstWorld;
                var bothControls = @from.ControlType != BGCurvePoint.ControlTypeEnum.Absent && to.ControlType != BGCurvePoint.ControlTypeEnum.Absent;
                if (!bothControls && @from.ControlType == BGCurvePoint.ControlTypeEnum.Absent) control1 = control2;


                var prev = fromPos;
                for (var i = 1; i < parts + 1; i++)
                {
                    var ratio = i/(float) parts;

                    var currentPosition = bothControls
                        ? BGCurveFormulas.BezierCubic(ratio, fromPos, control1, control2, toPos)
                        : BGCurveFormulas.BezierQuadratic(ratio, fromPos, control1, toPos);
                    action(prev, currentPosition);
                    prev = currentPosition;
                }
            }
        }

        //copy paste from math, not sure how to refactor it 
        public static Vector3 CalculatePosition(BGCurvePoint @from, BGCurvePoint to, float t)
        {
            if (@from.ControlType == BGCurvePoint.ControlTypeEnum.Absent && to.ControlType == BGCurvePoint.ControlTypeEnum.Absent)
            {
                return Vector3.Lerp(@from.PositionWorld, to.PositionWorld, t);
            }
            if (@from.ControlType != BGCurvePoint.ControlTypeEnum.Absent && to.ControlType != BGCurvePoint.ControlTypeEnum.Absent)
            {
                return BGCurveFormulas.BezierCubic(t, @from.PositionWorld, @from.ControlSecondWorld, to.ControlFirstWorld, to.PositionWorld);
            }

            return BGCurveFormulas.BezierQuadratic(t, @from.PositionWorld, @from.ControlType == BGCurvePoint.ControlTypeEnum.Absent ? to.ControlFirstWorld : @from.ControlSecondWorld,
                to.PositionWorld);
        }

        /// <summary>Tangent in World coordinates </summary>
        public static Vector3 CalculateTangent(BGCurvePoint @from, BGCurvePoint to, float t)
        {
            if (@from.ControlType == BGCurvePoint.ControlTypeEnum.Absent && to.ControlType == BGCurvePoint.ControlTypeEnum.Absent)
            {
                return (to.PositionWorld - @from.PositionWorld).normalized;
            }
            if (@from.ControlType != BGCurvePoint.ControlTypeEnum.Absent && to.ControlType != BGCurvePoint.ControlTypeEnum.Absent)
            {
                return BGCurveFormulas.BezierCubicDerivative(t, @from.PositionWorld, @from.ControlSecondWorld, to.ControlFirstWorld, to.PositionWorld).normalized;
            }

            return BGCurveFormulas.BezierQuadraticDerivative(t, @from.PositionWorld, @from.ControlType == BGCurvePoint.ControlTypeEnum.Absent ? to.ControlFirstWorld : @from.ControlSecondWorld,
                to.PositionWorld).normalized;
        }

        /// <summary>Tangent in World coordinates </summary>
        public static Vector3 CalculateTangent(BGCurvePoint point, BGCurvePoint previous, BGCurvePoint next, float precision)
        {
            var prevTangent = previous != null ? CalculateTangent(previous, point, 1 - precision) : Vector3.zero;
            var nextTangent = next != null ? CalculateTangent(point, next, precision) : Vector3.zero;

            var tangent = (previous != null && next != null)
                ? Vector3.Lerp(prevTangent, nextTangent, .5f)
                : next == null ? prevTangent : nextTangent;

            return tangent.normalized;
        }

/*
        public static Vector3 CalculateTangent(BGCurvePoint point, float precision)
        {
            var curve = point.Curve;

            //not enough points
            if (curve.PointsCount < 2) return Vector3.zero;

            var myIndex = curve.IndexOf(point);

            //have no idea why it can happen
            if (myIndex < 0) return Vector3.zero;

            //prev point
            BGCurvePoint prev = null;
            if (myIndex != 0 || curve.Closed)
            {
                prev = myIndex == 0 ? curve[curve.PointsCount - 1] : curve[myIndex - 1];
            }

            //next point
            BGCurvePoint next = null;
            if (myIndex != curve.PointsCount - 1 || curve.Closed)
            {
                next = myIndex == curve.PointsCount - 1 ? curve[0] : curve[myIndex + 1];
            }
            return CalculateTangent(point, prev, next, precision);
        }
*/

        public static float CalculateDistance(BGCurvePoint from, BGCurvePoint to, int parts)
        {
            var distance = 0f;
            Split(@from, to, parts, (fromPos, toPos) => distance += Vector3.Distance(fromPos, toPos));
            return distance;
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

        private sealed class DragSession
        {
            private int minIndex;
            private bool needtoRecalculate;
            private string lastActiveName;


            public int Active(string name)
            {
                var currentEvent = Event.current;

                if (currentEvent.type == EventType.MouseUp || currentEvent.type == EventType.MouseDown) needtoRecalculate = true;

                if (needtoRecalculate || GUIUtility.hotControl == 0 || !String.Equals(lastActiveName, name) || !String.Equals(GUI.GetNameOfFocusedControl(), name)) return -1;

                return minIndex;
            }

            public void NextMin(int minIndex, string name)
            {
                if (GUIUtility.hotControl == 0 || !String.Equals(GUI.GetNameOfFocusedControl(), name)) return;

                lastActiveName = name;
                needtoRecalculate = false;
                this.minIndex = minIndex;
            }
        }

        public static void Release(ref EventCanceller eventCanceller)
        {
            if (eventCanceller != null)
            {
                eventCanceller.Release();
                //bye
                eventCanceller = null;
            }
        }

        public class EventCanceller
        {
            public EventCanceller()
            {
                Event.current.Use();
                GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
            }

            internal void Release()
            {
                GUIUtility.hotControl = 0;
            }
        }
    }
}