using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using BansheeGz.BGSpline.Curve;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
    //idea.. do points paging
    [CustomEditor(typeof (BGCurve))]
    public class BGCurveEditor : UnityEditor.Editor
    {
        private const int ToolBarHeight = 20;

        //static
        private static readonly Color32 LockViewActiveColor = new Color32(255, 252, 58, 255);
        internal static BGOverlayMessage OverlayMessage;
        //for curves, which are not selected in hierarchy
        private static readonly Dictionary<BGCurve, BGCurvePainterGizmo> curve2Painter = new Dictionary<BGCurve, BGCurvePainterGizmo>();
        private static BGCurvePainterGizmo CurrentGizmoPainter;
        private static BGCurve CurrentCurve;
        private static Texture2D headerTexture;

        // non-static
        private BGCurveEditorTab[] editors;

        private Texture2D[] headers;

        private Rect toolBarRect;
        private Texture2D stickerTextureOk;
        private Texture2D stickerTextureActive;
        private Texture2D stickerTextureWarning;
        private Texture2D stickerTextureError;
        private Texture2D settingsTexture;

        private GUIStyle stickerStyle;

        public BGCurve Curve { get; private set; }
        public BGCurveBaseMath Math { get; private set; }

        public static bool lastPlayMode;

        private BGTransformMonitor transformMonitor;
        protected void OnEnable()
        {
            Curve = (BGCurve) target;
            CurrentCurve = Curve;
            transformMonitor = new BGTransformMonitor(Curve);

            var settings = BGPrivateField.GetSettings(Curve);


            //painter and math
            if (curve2Painter.ContainsKey(Curve))
            {
                var painterGizmo = curve2Painter[Curve];
                if (painterGizmo.Math != null)
                {
                    painterGizmo.Math.Dispose();
                }
                curve2Painter.Remove(Curve);
            }

            Math = NewMath(Curve, settings);
            CurrentGizmoPainter = new BGCurvePainterGizmo(Math);


            //overlay
            BGEditorUtility.Assign(ref OverlayMessage, () => new BGOverlayMessage());

            //probably we do not need it for play mode.. probably
            if (!Application.isPlaying)
            {
                //they are not persistent 
                Curve.EventMode = BGCurve.EventModeEnum.Immediate;
                Curve.BeforeChange += BeforeCurveChange;
                Curve.Changed += CurveChanged;
            }


            if (!settings.Existing)
            {
                //newly created
                settings.Existing = true;

                var defaultSettings = BGCurveSettingsOperations.LoadDefault();
                if (defaultSettings != null) BGPrivateField.SetSettings(Curve, defaultSettings);
            }

            //load textures
            BGEditorUtility.Assign(ref headerTexture, () => BGEditorUtility.LoadTexture2D(BGEditorUtility.Image.BGCurveLogo123));
            stickerTextureOk = BGEditorUtility.Texture1X1(new Color32(46, 143, 168, 255));
            stickerTextureError = BGEditorUtility.Texture1X1(new Color32(255, 0, 0, 255));
            stickerTextureWarning = BGEditorUtility.Texture1X1(new Color32(255, 206, 92, 255));
            stickerTextureActive = BGEditorUtility.Texture1X1(new Color32(44, 160, 90, 255));

            // editors
            editors = GetEditors();

            headers = editors.Select(editor => editor.Header2D).ToArray();


            foreach (var editor in editors) editor.OnEnable();

            //do it every frame 
            EditorApplication.update -= OverlayMessage.Check;
            EditorApplication.update += OverlayMessage.Check;

            Undo.undoRedoPerformed -= InternalOnUndoRedo;
            Undo.undoRedoPerformed += InternalOnUndoRedo;
        }

        private static BGCurveBaseMath NewMath(BGCurve curve, BGCurveSettings settings)
        {
            return new BGCurveBaseMath(curve, NewConfig(settings));
        }

        private static BGCurveBaseMath.Config NewConfig(BGCurveSettings settings)
        {
            return new BGCurveBaseMath.Config(settings.ShowTangents ? BGCurveBaseMath.Fields.PositionAndTangent : BGCurveBaseMath.Fields.Position) {Parts = settings.Sections};
        }

        private static void AdjustMath(BGCurveSettings settings, BGCurveBaseMath math)
        {
            if (settings.Sections != math.Configuration.Parts
                || (settings.ShowTangents && !math.IsCalculated(BGCurveBaseMath.Field.Tangent))
                || (!settings.ShowTangents && math.IsCalculated(BGCurveBaseMath.Field.Tangent)))
            {
                math.Init(NewConfig(settings));
            }
        }

        private void CurveChanged(object sender, BGCurveChangedArgs e)
        {
            if (Curve != null) EditorUtility.SetDirty(Curve);
        }

        private void BeforeCurveChange(object sender, BGCurveChangedArgs.BeforeChange e)
        {
            Undo.RecordObject(Curve, e != null ? e.Operation : "Curve change");
        }

        protected virtual BGCurveEditorTab[] GetEditors()
        {
            return new BGCurveEditorTab[]
            {
                new BGCurveEditorPoints(this, serializedObject), new BGCurveEditorComponents(this, serializedObject),
                new BGCurveEditorFields(this, serializedObject), new BGCurveEditorSettings(this, serializedObject)
            };
        }

        private void OnDisable()
        {
            EditorApplication.update -= OverlayMessage.Check;

            foreach (var editor in editors) editor.OnDisable();

            Dispose();
        }

        private void Dispose()
        {
            CurrentCurve = null;
            CurrentGizmoPainter = null;
            Undo.undoRedoPerformed -= InternalOnUndoRedo;

            if (Math != null) Math.Dispose();
            Math = null;
        }

        private void InternalOnUndoRedo()
        {
            foreach (var editor in editors) editor.OnUndoRedo();

            Curve.FireChange(null, true);
        }

        private void OnDestroy()
        {
            foreach (var editor in editors) editor.OnDestroy();

            Curve.BeforeChange -= BeforeCurveChange;
            Curve.Changed -= CurveChanged;

            Dispose();

            Tools.hidden = false;
        }

        public override void OnInspectorGUI()
        {
            //adjust math if needed
            AdjustMath(BGPrivateField.GetSettings(Curve), Math);

            //styles
            BGEditorUtility.Assign(ref stickerStyle, () => new GUIStyle("Label") {fontSize = 18, alignment = TextAnchor.MiddleCenter, normal = new GUIStyleState {textColor = Color.white}});
            BGEditorUtility.Assign(ref settingsTexture, () => BGEditorUtility.LoadTexture2D(BGEditorUtility.Image.BGSettingsIcon123));

            serializedObject.Update();

            // =========== Header
            DrawLogo();

            // =========== lock view
            BGEditorUtility.Horizontal(() =>
            {
                var temp = BGCurveSettingsForEditor.LockView;
                BGCurveSettingsForEditor.LockView = BGEditorUtility.ButtonOnOff(ref temp, "Lock view", "Disable selection of other objects in the scene", LockViewActiveColor,
                    new GUIContent("Turn Off", "Click to turn this mode off"),
                    new GUIContent("Turn On", "Click to turn this mode on"));

                if (BGEditorUtility.ButtonWithIcon(settingsTexture, "Open BGCurve Editor Settings", 24, 24)) BGCurveSettingsForEditorWindow.Open();
            });

            //warning
            BGEditorUtility.HelpBox("You can not chose another objects in the scene, except points.\r\n Use rectangular selection without pressing shift", MessageType.Warning,
                BGCurveSettingsForEditor.LockView, () => GUILayout.Space(8));

            // =========== Tabs
            if (BGCurveSettingsForEditor.CurrentTab < 0 || BGCurveSettingsForEditor.CurrentTab > headers.Length - 1) BGCurveSettingsForEditor.CurrentTab = 0;
            BGCurveSettingsForEditor.CurrentTab = GUILayout.Toolbar(BGCurveSettingsForEditor.CurrentTab, headers, GUILayout.Height(ToolBarHeight));
            //do not move this method(GUILayoutUtility.GetLastRect() is used) 
            ShowStickers();
            editors[BGCurveSettingsForEditor.CurrentTab].OnInspectorGui();


            if (!GUI.changed) return; // if no change- return

            foreach (var editor in editors) editor.OnBeforeApply();

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(Curve);

            foreach (var editor in editors) editor.OnApply();

            transformMonitor.Check();

        }

        //shows error sticker if any component has error
        private void ShowStickers()
        {
            if (Event.current.type == EventType.Repaint) toolBarRect = GUILayoutUtility.GetLastRect();

            const int height = 18;
            var oneTabWidth = toolBarRect.width/editors.Length;

            for (var i = 0; i < editors.Length; i++)
            {
                var editor = editors[i];
                var error = MessageType.None;
                var message = editor.GetStickerMessage(ref error);
                if (message == null) continue;

                //show sticker
                var width = stickerStyle.CalcSize(new GUIContent(message)).x;

                var rect = new Rect(toolBarRect.x + oneTabWidth*(i + 1) - width, toolBarRect.y + 1, width, height);

                GUI.DrawTexture(rect, GetStickerTexture(error, i));
                GUI.Label(rect, message, stickerStyle);
            }
        }

        private Texture2D GetStickerTexture(MessageType error, int index)
        {
            return error == MessageType.Error
                ? stickerTextureError
                : error == MessageType.Warning
                    ? stickerTextureWarning
                    : BGCurveSettingsForEditor.CurrentTab == index
                        ? stickerTextureActive
                        : stickerTextureOk;
        }

        protected virtual void DrawLogo()
        {
            DrawLogo(headerTexture);
        }


        protected static void DrawLogo(Texture2D logo)
        {
            var rect = GUILayoutUtility.GetRect(0, 0);
            rect.width = logo.width*.5f;
            rect.height = logo.height*.5f;
            rect.y += 1;
            GUILayout.Space(rect.height + 1);
            GUI.DrawTexture(rect, logo);
        }

        public void OnSceneGUI()
        {

            var settings = BGPrivateField.GetSettings(Curve);

            AdjustMath(settings, Math);

            if (settings.HandlesSettings != null && settings.HandlesType == BGCurveSettings.HandlesTypeEnum.Configurable
                || settings.ControlHandlesSettings != null && settings.ControlHandlesType == BGCurveSettings.HandlesTypeEnum.Configurable) BGEditorUtility.ReloadSnapSettings();

            OverlayMessage.OnSceneGui();

            // process all editors
            foreach (var editor in editors) editor.OnSceneGui();

            transformMonitor.Check();

        }

        [MenuItem("GameObject/Create Other/BansheeGz/BG Curve")]
        public static void CreateCurve(MenuCommand command)
        {
            var curveObject = new GameObject("BGCurve");
            Undo.RegisterCreatedObjectUndo(curveObject, "Undo Create BGCurve");
            curveObject.AddComponent<BGCurve>();
            Selection.activeGameObject = curveObject;
        }

        [DrawGizmo(GizmoType.NotInSelectionHierarchy | GizmoType.Selected | GizmoType.InSelectionHierarchy)]
        public static void DrawGizmos(BGCurve curve, GizmoType gizmoType)
        {
            var playMode = EditorApplication.isPlaying;

            if (lastPlayMode != playMode)
            {
                lastPlayMode = playMode;

                foreach (var painterGizmo in curve2Painter) painterGizmo.Value.Math.Dispose();
                curve2Painter.Clear();
            }

            var settings = BGPrivateField.GetSettings(curve);
            if (!ComplyForDrawGizmos(curve, gizmoType, settings)) return;

            if (CurrentCurve != null && curve.GetInstanceID() == CurrentCurve.GetInstanceID())
            {
                if (CurrentGizmoPainter != null) CurrentGizmoPainter.DrawCurve();
            }
            else
            {
                //curve is not selected in hierarchy
                var painter = BGEditorUtility.Ensure(curve2Painter, curve, () => new BGCurvePainterGizmo(NewMath(curve, settings), true));
                AdjustMath(settings, painter.Math);
                painter.DrawCurve();
            }
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
            return (gizmoType & toCompare) != 0;
        }
    }
}