using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using BansheeGz.BGSpline.Curve;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
    //idea.. do points paging
    [CustomEditor(typeof(BGCurve))]
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

        //selected points
        private BGCurveEditorPointsSelection editorSelection;
        private int undoGroup = -1;

        protected void OnEnable()
        {
            Curve = (BGCurve) target;

            //wth
            if (Curve == null) return;

            CurrentCurve = Curve;
            transformMonitor = BGTransformMonitor.GetMonitor(Curve);


            var settings = BGPrivateField.GetSettings(Curve);


            //painter and math
            if (curve2Painter.ContainsKey(Curve))
            {
                curve2Painter[Curve].Dispose();
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
                Curve.ImmediateChangeEvents = true;
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

            //selection
            editorSelection = new BGCurveEditorPointsSelection(Curve, this);

            // editors
            editors = new BGCurveEditorTab[]
            {
                new BGCurveEditorPoints(this, serializedObject, editorSelection), new BGCurveEditorComponents(this, serializedObject),
                new BGCurveEditorFields(this, serializedObject, editorSelection), new BGCurveEditorSettings(this, serializedObject)
            };

            headers = editors.Select(editor => editor.Header2D).ToArray();
            foreach (var editor in editors) editor.OnEnable();

            //do it every frame 
            EditorApplication.update -= OverlayMessage.Check;
            EditorApplication.update += OverlayMessage.Check;

            Undo.undoRedoPerformed -= InternalOnUndoRedo;
            Undo.undoRedoPerformed += InternalOnUndoRedo;
        }

        public static BGCurveBaseMath NewMath(BGCurve curve, BGCurveSettings settings)
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
            if (Curve == null) return;

            if (undoGroup > 0) Undo.CollapseUndoOperations(undoGroup);
            undoGroup = -1;
            EditorUtility.SetDirty(Curve);

            if (Curve.FieldsCount > 0) foreach (var field in Curve.Fields) EditorUtility.SetDirty(field);

            var pointsMode = Curve.PointsMode;
            if (Curve.PointsCount > 0 && pointsMode != BGCurve.PointsModeEnum.Inlined)
            {
                switch (pointsMode)
                {
                    case BGCurve.PointsModeEnum.Components:
                        foreach (var point in Curve.Points) EditorUtility.SetDirty((BGCurvePointComponent) point);
                        break;
                    case BGCurve.PointsModeEnum.GameObjectsNoTransform:
                    case BGCurve.PointsModeEnum.GameObjectsTransform:
                        foreach (var point in Curve.Points)
                        {
                            var curvePointGo = (BGCurvePointGO) point;
                            EditorUtility.SetDirty(curvePointGo);
                            EditorUtility.SetDirty(curvePointGo.gameObject);
                        }
                        break;
                }
            }

            foreach (var editor in editors) editor.OnCurveChanged(e);

            transformMonitor.CheckForChange();
        }

        private void BeforeCurveChange(object sender, BGCurveChangedArgs.BeforeChange e)
        {
//            Undo.IncrementCurrentGroup();
            undoGroup = Undo.GetCurrentGroup();

            var operation = e != null && e.Operation != null ? e.Operation : "Curve change";

            Undo.RecordObject(Curve, operation);


            if (Curve.FieldsCount > 0) foreach (var field in Curve.Fields) Undo.RecordObject(field, operation);

            var pointsMode = Curve.PointsMode;
            if (Curve.PointsCount > 0)
            {
                var points = Curve.Points;
                foreach (var point in points) if (point.PointTransform != null) Undo.RecordObject(point.PointTransform, operation);

                if (pointsMode != BGCurve.PointsModeEnum.Inlined)
                {
                    switch (pointsMode)
                    {
                        case BGCurve.PointsModeEnum.Components:
                            foreach (var point in points) Undo.RecordObject((BGCurvePointComponent) point, operation);
                            break;
                        case BGCurve.PointsModeEnum.GameObjectsNoTransform:
                        case BGCurve.PointsModeEnum.GameObjectsTransform:
                            foreach (var point in points)
                            {
                                var pointGo = (BGCurvePointGO) point;
                                Undo.RecordObject(pointGo, operation);
                                if (pointsMode == BGCurve.PointsModeEnum.GameObjectsTransform) Undo.RecordObject(pointGo.transform, operation);
                            }
                            break;
                    }
                }
            }
        }

        public void OnDisable()
        {
            try
            {
                EditorApplication.update -= OverlayMessage.Check;
            }
            catch (ArgumentException)
            {
                return;
            }

            if (editors != null) foreach (var editor in editors) if (editor != null) editor.OnDisable();

            if (transformMonitor != null) transformMonitor.Release();

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
            transformMonitor.CheckForChange();

            Curve.PrivateUpdateFieldsValuesIndexes();

            if (BGCurve.IsGoMode(Curve.PointsMode)) BGPrivateField.Invoke(Curve, BGCurve.MethodSetPointsNames);

            foreach (var editor in editors) editor.OnUndoRedo();

            if (Math != null) Math.Recalculate();

            Repaint();
            SceneView.RepaintAll();
        }

        public void OnDestroy()
        {
            if (editors != null) foreach (var editor in editors) if (editor != null) editor.OnDestroy();

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
                BGCurveSettingsForEditor.LockView = BGEditorUtility.ButtonOnOff(ref temp, "Lock view", "Disable selection of any object in the scene, except points", LockViewActiveColor,
                    new GUIContent("Turn Off", "Click to turn this mode off"),
                    new GUIContent("Turn On", "Click to turn this mode on"));

                if (GUILayout.Button(settingsTexture, GUILayout.MaxWidth(24), GUILayout.MaxHeight(24))) BGCurveSettingsForEditorWindow.Open(BGCurveSettingsForEditor.I);
            });

            //warning
            BGEditorUtility.HelpBox("You can not chose another objects in the scene, except points.", MessageType.Warning,
                BGCurveSettingsForEditor.LockView, () => GUILayout.Space(8));

            // =========== Tabs
            var currentTab = BGCurveSettingsForEditor.CurrentTab;
            if (currentTab < 0 || currentTab > headers.Length - 1) currentTab = 0;
            var newTab = GUILayout.Toolbar(currentTab, headers, GUILayout.Height(ToolBarHeight));
            //do not move this method(GUILayoutUtility.GetLastRect() is used) 
            ShowStickers();
            if (currentTab != newTab) GUI.FocusControl("");
            BGCurveSettingsForEditor.CurrentTab = newTab;
            editors[newTab].OnInspectorGui();

            if (!GUI.changed) return; // if no change- return

            foreach (var editor in editors) editor.OnApply();

            transformMonitor.CheckForChange();
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

            if (Curve.ForceChangedEventMode != BGCurve.ForceChangedEventModeEnum.Off) Math.Recalculate(true);


            if (settings.HandlesSettings != null && settings.HandlesType == BGCurveSettings.HandlesTypeEnum.Configurable
                || settings.ControlHandlesSettings != null && settings.ControlHandlesType == BGCurveSettings.HandlesTypeEnum.Configurable) BGEditorUtility.ReloadSnapSettings();


            OverlayMessage.OnSceneGui();

            var frustum = GeometryUtility.CalculateFrustumPlanes(SceneView.currentDrawingSceneView.camera);

            // process all editors
            foreach (var editor in editors) editor.OnSceneGui(frustum);

            editorSelection.Process(Event.current);


            transformMonitor.CheckForChange();
        }

        public static void AddPoint(BGCurve curve, BGCurvePoint point, int index)
        {
            BGPrivateField.Invoke(curve, BGCurve.MethodAddPoint, point, index, GetPointProvider(curve.PointsMode, curve));
        }

        public static void DeletePoint(BGCurve curve, int index)
        {
            BGPrivateField.Invoke(curve, BGCurve.MethodDeletePoint, new[] {typeof(int), typeof(Action<BGCurvePointI>)}, index, GetPointDestroyer(curve.PointsMode, curve));
        }

        public static void DeletePoints(BGCurve curve, BGCurvePointI[] points)
        {
            BGPrivateField.Invoke(curve, BGCurve.MethodDeletePoint, new[] {typeof(BGCurvePointI[]), typeof(Action<BGCurvePointI>)}, points, GetPointDestroyer(curve.PointsMode, curve));
        }


        public static Func<BGCurvePointI> GetPointProvider(BGCurve.PointsModeEnum pointsMode, BGCurve curve)
        {
            //init provider 
            Func<BGCurvePointI> provider = null;
            switch (pointsMode)
            {
                case BGCurve.PointsModeEnum.Components:
                    provider = () => Undo.AddComponent<BGCurvePointComponent>(curve.gameObject);
                    break;
                case BGCurve.PointsModeEnum.GameObjectsNoTransform:
                case BGCurve.PointsModeEnum.GameObjectsTransform:
                    provider = () =>
                    {
                        var pointGO = new GameObject();
                        var transform = pointGO.transform;
                        transform.parent = curve.transform;
                        transform.localRotation = Quaternion.identity;
                        transform.localPosition = Vector3.zero;
                        transform.localScale = Vector3.one;

                        Undo.RegisterCreatedObjectUndo(pointGO, "Create point");
                        var point = Undo.AddComponent<BGCurvePointGO>(pointGO);
                        return point;
                    };
                    break;
            }
            return provider;
        }

        public static Action<BGCurvePointI> GetPointDestroyer(BGCurve.PointsModeEnum pointsMode, BGCurve curve)
        {
            //init destroyer
            Action<BGCurvePointI> destroyer = null;
            switch (pointsMode)
            {
                case BGCurve.PointsModeEnum.Components:
                    destroyer = point => Undo.DestroyObjectImmediate((UnityEngine.Object) point);
                    break;
                case BGCurve.PointsModeEnum.GameObjectsNoTransform:
                case BGCurve.PointsModeEnum.GameObjectsTransform:
                    destroyer = point => Undo.DestroyObjectImmediate(((MonoBehaviour) point).gameObject);
                    break;
            }
            return destroyer;
        }


        [MenuItem("GameObject/Create Other/BansheeGz/BG Curve")]
        public static void CreateCurve(MenuCommand command)
        {
            var curveObject = new GameObject("BGCurve");
            Undo.RegisterCreatedObjectUndo(curveObject, "Undo Create BGCurve");
            curveObject.AddComponent<BGCurve>();
            Selection.activeGameObject = curveObject;
        }

//         We decided to remove this method (with settings.ShowCurveMode setting) cause it negatively affects overall SceneView navigation 
//         [DrawGizmo(GizmoType.NotInSelectionHierarchy | GizmoType.Selected | GizmoType.InSelectionHierarchy)]
        [DrawGizmo(GizmoType.Selected)]
        public static void DrawGizmos(BGCurve curve, GizmoType gizmoType)
        {
            if (curve.PointsCount == 0) return;

            var settings = curve.Settings;
            if (!settings.ShowCurve || settings.VRay) return;

/*
            var settingsShowCurveMode = settings.ShowCurveMode;
//            if (true) return;
            if (settingsShowCurveMode == BGCurveSettings.ShowCurveModeEnum.CurveOrParentSelected && (gizmoType & GizmoType.InSelectionHierarchy) == 0) return;
            if (settingsShowCurveMode == BGCurveSettings.ShowCurveModeEnum.CurveSelected && (gizmoType & GizmoType.Selected) == 0) return;

            if (BGCurvePointGOEditor.PointSelected) return;

            if (Selection.Contains(curve.gameObject) && settings.VRay) return;
*/

            var playMode = EditorApplication.isPlaying;
            if (lastPlayMode != playMode)
            {
                lastPlayMode = playMode;

                foreach (var painterGizmo in curve2Painter) painterGizmo.Value.Dispose();
                curve2Painter.Clear();
            }

            if (CurrentCurve != null && curve.GetInstanceID() == CurrentCurve.GetInstanceID())
            {
                if (CurrentGizmoPainter != null) CurrentGizmoPainter.DrawCurve();
            }
            else
            {
                //curve is not selected in hierarchy
                var painter = BGEditorUtility.Ensure(curve2Painter, curve, () => new BGCurvePainterGizmo(NewMath(curve, settings), true));
                AdjustMath(settings, painter.Math);
                if (curve.ForceChangedEventMode != BGCurve.ForceChangedEventModeEnum.Off && !Application.isPlaying) painter.Math.Recalculate();
                painter.DrawCurve();
            }
        }
    }
}