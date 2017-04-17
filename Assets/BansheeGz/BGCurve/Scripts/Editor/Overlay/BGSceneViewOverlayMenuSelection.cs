using UnityEngine;
using BansheeGz.BGSpline.Curve;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
    public class BGSceneViewOverlayMenuSelection : BGSceneViewOverlayMenu<BGSceneViewOverlayMenuSelection.SelectionMenu>
    {
        public override string Name
        {
            get { return "Selection menu"; }
        }

        public BGSceneViewOverlayMenuSelection(BGSceneViewOverlay overlay, BGCurveEditorPointsSelection editorSelection)
            : base(overlay, editorSelection)
        {
            menu = new SelectionMenu(overlay, editorSelection);
        }

        protected override bool Process(Event @event, BGCurveBaseMath math, float sceneViewHeight, ref Vector3 position, ref string message)
        {
            if (BGCurveSettingsForEditor.I.Get<bool>(BGCurveSettingsForEditor.DisableSceneViewSelectionMenuKey) || !menu.EditorSelection.HasSelected()) return false;


            var selectedPos = menu.EditorSelection.GetAveragePosition();
            if (!(DistanceTolerance > (@event.mousePosition - BGEditorUtility.GetSceneViewPosition(selectedPos, sceneViewHeight)).sqrMagnitude)) return false;


            //out params
            position = selectedPos;
            message = SuccessMessage("Selected " + menu.EditorSelection.CountSelected + " point(s).");

            //turn on the menu
            menu.On(position);

            //check if all points share the same control type
            BGCurvePoint.ControlTypeEnum singleType = BGCurvePoint.ControlTypeEnum.Absent;
            bool first = true, single=true;
            menu.EditorSelection.ForEach(point =>
            {
                if (first)
                {
                    first = false;
                    singleType = point.ControlType;
                }
                else if (singleType != point.ControlType)
                {
                    single = false;
                    return true;
                }

                return false;
            });

            if (single)
            {
                menu.Get(0).Current = singleType == BGCurvePoint.ControlTypeEnum.Absent;
                menu.Get(1).Current = singleType == BGCurvePoint.ControlTypeEnum.BezierSymmetrical;
                menu.Get(2).Current = singleType == BGCurvePoint.ControlTypeEnum.BezierIndependant; 
            }
            else menu.Get(0).Current = menu.Get(1).Current = menu.Get(2).Current = false;


            return true;
        }

        //========================================================== Selection menu
        public sealed class SelectionMenu : AbstractMenu
        {
            public SelectionMenu(BGSceneViewOverlay overlay, BGCurveEditorPointsSelection editorSelection)
                : base(overlay, editorSelection, "Selection menu")
            {
            }

            protected override void SetControl(BGCurvePoint.ControlTypeEnum type)
            {
                EditorSelection.SetControlTypeForSelected(type);
            }

            protected override void Delete()
            {
                EditorSelection.DeleteSelected();
            }

            public override string Details
            {
                get { return "Selected " + EditorSelection.CountSelected + " point(s)."; }
            }

            protected override void AdditionalMenuItems()
            {
                Add(new MenuSeparator());
                Add(new MenuItemButton(BGEditorUtility.LoadTexture2D(BGEditorUtility.Image.BGSelectionRemove123), "Remove this point from selection",
                    () =>
                    {
                        EditorSelection.Clear();
                        EditorUtility.SetDirty(Overlay.Editor.Curve);
                    }));
            }


        }

    }
}