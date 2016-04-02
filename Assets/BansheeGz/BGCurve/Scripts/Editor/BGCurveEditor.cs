using System;
using UnityEngine;
using System.Collections.Generic;
using BansheeGz.BGSpline.Curve;
using BansheeGz.BGSpline.EditorHelpers;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
    [CustomEditor(typeof (BGCurve))]
    public class BGCurveEditor : UnityEditor.Editor
    {
        private BGCurve curve;

        internal static readonly BGEditorPopup EditorPopup = new BGEditorPopup();

        private static Texture2D headerTexture;


        private BGCurveEditorTab[] editors;

        private Texture2D[] headers;

        private SerializedObject settingsObject;

        protected int tab;

        public BGCurve Curve
        {
            get { return curve; }
        }

        protected void OnEnable()
        {
            curve = (BGCurve) target;

            var settings = BGPrivateField.GetSettings(curve);

            if (!settings.Existing)
            {
                //newly created
                settings.Existing = true;

                var defaultSettings = BGCurveSettingsOperations.LoadDefault();
                if (defaultSettings != null)
                {
                    BGPrivateField.SetSettings(curve, defaultSettings);
                }
            }


            //load header texture
            if (headerTexture == null)
            {
                headerTexture = (Texture2D) Resources.Load("BGCurveLogo123");
            }

            // editors
            editors = GetEditors();

            var list = new List<Texture2D>();
            foreach (var editor in editors)
            {
                list.Add(editor.GetHeader());
            }
            headers = list.ToArray();



            //do it every frame 
            EditorApplication.update -= EditorPopup.Check;
            EditorApplication.update += EditorPopup.Check;
        }

        protected virtual BGCurveEditorTab[] GetEditors()
        {
            return new BGCurveEditorTab[] {new BGCurveEditorPoints(this, serializedObject), new BGCurveEditorSettings(this, serializedObject)};
        }

        private void OnDisable()
        {
            EditorApplication.update -= EditorPopup.Check;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // =========== Header
            DrawHeader();

            // =========== Tabs
            tab = GUILayout.Toolbar(tab, headers);
            editors[tab].OnInspectorGUI();


            if (!GUI.changed) return; // if no change- return


            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }

        protected virtual void DrawHeader()
        {
            DrawHeader(headerTexture);
        }

        protected static void DrawHeader(Texture2D logo)
        {
            var rect = GUILayoutUtility.GetRect(0, 0);
            rect.width = logo.width;
            rect.height = logo.height;
            rect.y += 1;
            GUILayout.Space(rect.height + 1);
            GUI.DrawTexture(rect, logo);
        }

        public void OnSceneGUI()
        {
            EditorPopup.Show();

            // process all editors
            foreach (var editor in editors)
            {
                editor.OnSceneGUI();
            }
        }

        [MenuItem("GameObject/Create Other/BG Curve")]
        public static void CreateDecorator(MenuCommand command)
        {
            var curveObject = new GameObject("BGCurve");
            Undo.RecordObject(curveObject, "Undo Create BGCurve");
            curveObject.AddComponent<BGCurve>();
            Selection.activeGameObject = curveObject;
        }

        public virtual BGCurvePainterGizmo NewPainter()
        {
            return new BGCurvePainterHandles(curve);
        }
    }
}