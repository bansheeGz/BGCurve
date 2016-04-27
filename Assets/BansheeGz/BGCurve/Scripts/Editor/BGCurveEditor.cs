using UnityEngine;
using System.Linq;
using BansheeGz.BGSpline.Curve;
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

        protected static int tab;

        public BGCurve Curve
        {
            get { return curve; }
        }

        protected void OnEnable()
        {
            curve = (BGCurve) target;

            curve.TraceChanges = true;
            curve.BeforeChange += BeforeCurveChange;

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
                headerTexture = BGEditorUtility.LoadTexture2D("BGCurveLogo123");
            }

            // editors
            editors = GetEditors();

            headers = editors.Select(editor => editor.GetHeader()).ToArray();


            foreach (var editor in editors)
            {
                editor.OnEnable();
            }

            //do it every frame 
            EditorApplication.update -= EditorPopup.Check;
            EditorApplication.update += EditorPopup.Check;
        }

        private void BeforeCurveChange(object sender, BGCurveChangedArgs.BeforeChange e)
        {
            Undo.RecordObject(curve, e.Operation);
        }

        protected virtual BGCurveEditorTab[] GetEditors()
        {
            return new BGCurveEditorTab[] {new BGCurveEditorPoints(this, serializedObject), new BGCurveEditorSettings(this, serializedObject)};
        }

        void OnDestroy()
        {
            foreach (var editor in editors)
            {
                editor.OnDestroy();
            }

            curve.BeforeChange -= BeforeCurveChange;

            Tools.hidden = false;

            EditorApplication.update -= EditorPopup.Check;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // =========== Header
            DrawHeader();

            // =========== Tabs
            if (tab < 0 || tab > headers.Length - 1) tab = 0;
            tab = GUILayout.Toolbar(tab, headers);
            editors[tab].OnInspectorGUI();


            if (!GUI.changed) return; // if no change- return

            foreach (var editor in editors)
            {
                editor.OnBeforeApply();
            }
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(Curve);

            foreach (var editor in editors)
            {
                editor.OnApply();
            }
        }

        protected virtual void DrawHeader()
        {
            DrawHeader(headerTexture);
        }

        protected static void DrawHeader(Texture2D logo)
        {
            var rect = GUILayoutUtility.GetRect(0, 0);
            rect.width = logo.width * .5f;
            rect.height = logo.height * .5f;
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

        [MenuItem("GameObject/Create Other/BansheeGz/BG Curve")]
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

        [DrawGizmo(GizmoType.NotInSelectionHierarchy | GizmoType.Selected | GizmoType.InSelectionHierarchy)]
        public static void DrawGizmos(BGCurve curve, GizmoType gizmoType)
        {
            var settings = BGPrivateField.GetSettings(curve);
            if (!ComplyForDrawGizmos(curve, gizmoType, settings)) return;

            new BGCurvePainterGizmo(curve).DrawCurve();
        }

        public static bool ComplyForDrawGizmos(BGCurve curve, GizmoType gizmoType, BGCurveSettings settings)
        {
            if (curve.PointsCount == 0) return false;

            if (!settings.ShowCurve) return false;
            if (Selection.Contains(curve.gameObject) && settings.VRay) return false;
            if (settings.ShowCurveMode == BGCurveSettings.ShowCurveModeEnum.CurveSelected && !Comply(gizmoType, GizmoType.Selected)) return false;
            if (settings.ShowCurveMode == BGCurveSettings.ShowCurveModeEnum.CurveOrParentSelected && !Comply(gizmoType, GizmoType.InSelectionHierarchy)) return false;
            return true;
        }

        public static bool Comply(GizmoType gizmoType, GizmoType toCompare)
        {
            return (gizmoType & toCompare)!=0;
        }
    }
}