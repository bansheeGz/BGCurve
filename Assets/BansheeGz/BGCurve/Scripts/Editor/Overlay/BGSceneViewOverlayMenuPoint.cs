using UnityEngine;
using BansheeGz.BGSpline.Curve;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
    //point's menu
    public class BGSceneViewOverlayMenuPoint : BGSceneViewOverlayMenu<BGSceneViewOverlayMenuPoint.PointMenu>
    {
        public override string Name
        {
            get { return "Point menu"; }
        }

        public BGSceneViewOverlayMenuPoint(BGSceneViewOverlay overlay, BGCurveEditorPointsSelection editorSelection) : base(overlay, editorSelection)
        {
            menu = new PointMenu(overlay, editorSelection);
        }


        protected override bool Process(Event @event, BGCurveBaseMath math, float sceneViewHeight, ref Vector3 position, ref string message)
        {
            if (BGCurveSettingsForEditor.I.Get<bool>(BGCurveSettingsForEditor.DisableSceneViewPointMenuKey)) return false;

            var minDistanceToCamera = float.MaxValue;
            var mousePosition = @event.mousePosition;
            var cameraPos = SceneView.currentDrawingSceneView.camera.transform.position;

            var index = -1;
            var pointsCount = math.Curve.PointsCount;

            for (var i = 0; i < pointsCount; i++)
            {
                var pointPos = math.GetPosition(i);

                var sqrMagnitude = (mousePosition - BGEditorUtility.GetSceneViewPosition(pointPos, sceneViewHeight)).sqrMagnitude;
                if (sqrMagnitude > DistanceTolerance) continue;

                var sqrtMagnitude = Vector3.SqrMagnitude(cameraPos - pointPos);

                if (minDistanceToCamera < sqrtMagnitude) continue;

                //found a target
                minDistanceToCamera = sqrMagnitude;
                index = i;
            }

            if (index < 0) return false;


            //menu active
            var point = math.Curve[index];
            position = math.GetPosition(index);
            message = SuccessMessage("Point " + index);
            menu.On(point, index);

            //============== Ok
            return true;
        }


        //========================================================== Point menu
        public sealed class PointMenu : AbstractMenu
        {
            public int PointIndex;
            private BGCurvePointI point;
            private MenuItemButton addToSelectionItem;
            private MenuItemButton removeFromSelectionItem;

            public PointMenu(BGSceneViewOverlay overlay, BGCurveEditorPointsSelection editorSelection)
                : base(overlay, editorSelection, "Point menu")
            {
            }

            public override string Details
            {
                get { return "Point " + PointIndex; }
            }

            protected override void AdditionalMenuItems()
            {
                Add(new MenuSeparator());

                //add before
                Add(new MenuItemButton(() => BGBinaryResources.BGPointInsertBefore123, "Insert a point before this point",
                    () =>
                    {
                        var curve = point.Curve;
                        var settings = BGPrivateField.GetSettings(curve);
                        var index = curve.IndexOf(point);

                        BGCurveEditor.AddPoint(curve, BGNewPointPositionManager.InsertBefore(curve, index, settings.ControlType, settings.Sections), index);
                    }));

                //add after
                Add(new MenuItemButton(() => BGBinaryResources.BGPointInsertAfter123, "Insert a point after this point",
                    () =>
                    {
                        var curve = point.Curve;
                        var settings = BGPrivateField.GetSettings(curve);
                        var index = curve.IndexOf(point);
                        BGCurveEditor.AddPoint(curve, BGNewPointPositionManager.InsertAfter(curve, index, settings.ControlType, settings.Sections), index + 1);
                    }));


                //add remove to selection
                addToSelectionItem = new MenuItemButton(() => BGBinaryResources.BGSelectionAdd123, "Add this point to selection",
                    () => EditorSelection.Add(point));

                removeFromSelectionItem = new MenuItemButton(() => BGBinaryResources.BGSelectionRemove123, "Remove this point from selection",
                    () => EditorSelection.Remove(point));

                Add(addToSelectionItem);
                Add(removeFromSelectionItem);
            }

            protected override void SetControl(BGCurvePoint.ControlTypeEnum type)
            {
                point.ControlType = type;
            }

            public void On(BGCurvePointI point, int index)
            {
                PointIndex = index;
                this.point = point;
                On(point.PositionWorld);

                Get(0).Current = point.ControlType == BGCurvePoint.ControlTypeEnum.Absent;
                Get(1).Current = point.ControlType == BGCurvePoint.ControlTypeEnum.BezierSymmetrical;
                Get(2).Current = point.ControlType == BGCurvePoint.ControlTypeEnum.BezierIndependant;
            }

            protected override void Delete()
            {
                BGCurveEditor.DeletePoint(point.Curve, point.Curve.IndexOf(point));
            }

            public override void On(Vector3 position)
            {
                addToSelectionItem.Disabled = EditorSelection.Contains(point);
                removeFromSelectionItem.Disabled = !addToSelectionItem.Disabled;
                base.On(position);
            }
        }
    }
}