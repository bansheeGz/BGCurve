using System;
using UnityEngine;
using BansheeGz.BGSpline.Curve;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
    public abstract class BGSceneViewOverlayMenu<T> : BGSceneViewOverlay.SceneAction where T : BGSceneViewOverlayMenu<T>.AbstractMenu
    {
        protected const float DistanceTolerance = 100;

        private readonly Texture2D pointSelectedTexture;

        protected T menu;
        protected BGCurveEditorPointsSelection editorSelection;

        private BGTransition.SwayTransition pointIndicatorTransition;

        protected BGSceneViewOverlayMenu(BGSceneViewOverlay overlay, BGCurveEditorPointsSelection editorSelection)
            : base(overlay)
        {
            this.editorSelection = editorSelection;
            pointSelectedTexture = BGEditorUtility.LoadTexture2D(BGEditorUtility.Image.BGPointSelected123);
        }

        internal override bool Seize(Event currentEvent, ref Vector3 position, ref string message)
        {
            if (menu.Active)
            {
                menu.OnGui(currentEvent);

                BGEditorUtility.Assign(ref pointIndicatorTransition, () => new BGTransition.SwayTransition(20, 30, .4f));
                BGSceneViewOverlay.DrawHandlesGuiTexture(menu.Point2DPosition, pointIndicatorTransition, pointSelectedTexture);

                var okMessage = menu.ActiveItem != null && menu.ActiveItem.Description != null
                    ? "Release Ctrl to " + menu.ActiveItem.Description
                    : "Hover over an option and release Ctrl.";

                //out params
                position = menu.Point3DPosition;
                message = BGSceneViewOverlay.ToOk(okMessage) + "\r\n" + menu.Details;
                //============== Ok
                return true;
            }

            if (!(currentEvent.type == EventType.Repaint && currentEvent.control || currentEvent.type == EventType.MouseMove && currentEvent.control)) return false;

            if (Process(currentEvent, overlay.Editor.Editor.Math, BGEditorUtility.GetSceneViewHeight(), ref position, ref message)) return true;

            pointIndicatorTransition = null;

            //============== No luck
            return false;
        }

        protected abstract bool Process(Event @event, BGCurveBaseMath math, float sceneViewHeight, ref Vector3 position, ref string message);



        protected static string SuccessMessage(string message)
        {
            return BGSceneViewOverlay.ToOk("Hover over an option and release Ctrl.\r\n") + message;
        }


        //========================================================== Abstract menu
        public abstract class AbstractMenu : BGPopupMenu
        {
            public readonly BGCurveEditorPointsSelection EditorSelection;
            public readonly BGSceneViewOverlay Overlay;

            public abstract string Details { get; }

            protected AbstractMenu(BGSceneViewOverlay overlay, BGCurveEditorPointsSelection editorSelection, string title)
                : base(title)
            {
                EditorSelection = editorSelection;
                Overlay = overlay;

                Add(new MenuItemButton(BGEditorUtility.LoadTexture2D(BGEditorUtility.Image.BGControlAbsent123), "Convert point control to Absent",
                    () => { SetControl(BGCurvePoint.ControlTypeEnum.Absent); }));

                Add(new MenuItemButton(BGEditorUtility.LoadTexture2D(BGEditorUtility.Image.BGControlBezierSymmetrical123), "Convert point control to Bezier Symmetrical",
                    () => { SetControl(BGCurvePoint.ControlTypeEnum.BezierSymmetrical); }));

                Add(new MenuItemButton(BGEditorUtility.LoadTexture2D(BGEditorUtility.Image.BGControlBezierIndependent123), "Convert point control to Bezier Independent",
                    () => { SetControl(BGCurvePoint.ControlTypeEnum.BezierIndependant); }));


                AdditionalMenuItems();


                Add(new LockMenuItem(() =>
                {
                    BGCurveSettingsForEditor.LockView = !BGCurveSettingsForEditor.LockView;
                    EditorUtility.SetDirty(overlay.Editor.Curve);
                }));

                Add(new MenuSeparator());

                Add(new MenuItemButton(BGEditorUtility.LoadTexture2D(BGEditorUtility.Image.BGPointDelete123), "Delete a point", Delete));
            }

            protected virtual void AdditionalMenuItems()
            {
            }

            protected abstract void SetControl(BGCurvePoint.ControlTypeEnum controlType);
            protected abstract void Delete();
        }

        //========================================================== Lock menu
        private sealed class LockMenuItem : BGPopupMenu.MenuItemButton
        {
            public LockMenuItem(Action action)
                : base(null, null, action)
            {
            }

            public override string Description
            {
                get { return BGCurveSettingsForEditor.LockView ? "Switch 'Lock View' mode off" : "Switch 'Lock View' mode on"; }
            }

            public override Texture2D Icon
            {
                get { return BGEditorUtility.LoadTexture2D(BGCurveSettingsForEditor.LockView ? BGEditorUtility.Image.BGLockOn123 : BGEditorUtility.Image.BGLockOff123); }
            }
        }
    }
}