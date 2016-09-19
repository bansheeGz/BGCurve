using UnityEngine;
using BansheeGz.BGSpline.Curve;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
    public abstract class BGSceneViewOverlayPointAdd : BGSceneViewOverlay.SceneAction
    {
        protected const float ScalePreviewPoint = .18f;


        private static readonly BGTransition.SwayTransition swayTransition = new BGTransition.SwayTransition(.8f, 1.2f, .6);


        protected BGSceneViewOverlayPointAdd(BGSceneViewOverlay overlay) : base(overlay)
        {
        }

        protected static void PreviewControls(BGCurveSettings settings, Vector3 position, Vector3 control1World, Vector3 control2World)
        {
            BGEditorUtility.SwapHandlesColor(settings.ControlHandlesColor, () =>
            {
                PreviewControl(settings, position, control1World);
                PreviewControl(settings, position, control2World);
            });
        }

        protected static void PreviewControl(BGCurveSettings settings, Vector3 position, Vector3 controlWorld)
        {
            var size = BGEditorUtility.GetHandleSize(position, ScalePreviewPoint*.8f);

            Handles.SphereCap(0, controlWorld, Quaternion.identity, size);
            Handles.DrawLine(position, controlWorld);
        }

        protected static void DrawSection(BGCurvePoint from, BGCurvePoint to, int parts)
        {
            BGEditorUtility.Split(@from, to, parts, (fromPos, toPos) => Handles.DrawDottedLine(fromPos, toPos, 2));
        }


        // preview
        protected void Preview(Vector3 position, BGCurve curve, ref float toLast, ref float toFirst)
        {
            var settings = overlay.Editor.Settings;

            //show point 
            BGEditorUtility.SwapHandlesColor(settings.SphereColor, () => Handles.SphereCap(0, position, Quaternion.identity, BGEditorUtility.GetHandleSize(position, ScalePreviewPoint)));

            //create a point
            var newPoint = BGNewPointPositionManager.CreatePoint(position, curve, settings.ControlType, settings.Sections, out toLast, out toFirst, false);

            //show controls
            if (newPoint.ControlType != BGCurvePoint.ControlTypeEnum.Absent) PreviewControls(settings, position, newPoint.ControlFirstWorld, newPoint.ControlSecondWorld);

            if (curve.PointsCount == 0) return;

            // last To new
            DrawSection(curve[curve.PointsCount - 1], newPoint, settings.Sections);

            AdditionalPreview(newPoint);

            if (!curve.Closed) return;

            // new To zero
            DrawSection(newPoint, curve[0], settings.Sections);
        }

        protected virtual void AdditionalPreview(BGCurvePoint newPoint)
        {
        }


        //see base class for description
        internal override bool Seize(Event currentEvent, ref Vector3 position, ref string message)
        {
            if (!Comply(currentEvent)) return false;

            var ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);

            Vector3 intersectionPosition;
            Plane plane;


            if (currentEvent.type == EventType.mouseDown && currentEvent.control && currentEvent.button == 0)
            {
                //Mouse down for some action
                var curve = overlay.Editor.Curve;
                var settings = overlay.Editor.Settings;

                Cast(currentEvent, ray, out intersectionPosition, out message, out plane);

                if (message != null) BGCurveEditor.OverlayMessage.Display(message);
                else
                {
                    position = intersectionPosition;
                    curve.AddPoint(BGNewPointPositionManager.CreatePoint(intersectionPosition, curve, settings.ControlType, settings.Sections, true));
                    EditorUtility.SetDirty(curve);
                }
                overlay.EventCanceller = new BGEditorUtility.EventCanceller();
                return true;
            }


            if (!(currentEvent.type == EventType.Repaint && currentEvent.control || currentEvent.type == EventType.MouseMove && currentEvent.control)) return false;

            Cast(currentEvent, ray, out intersectionPosition, out message, out plane);

            position = intersectionPosition;

            if (message != null) return true;

            Animation(plane, ray, swayTransition);

            //preview
            float toLast = -1, toFirst = -1;
            Preview(intersectionPosition, overlay.Editor.Curve, ref toLast, ref toFirst);

            //distance
            message = BGSceneViewOverlay.ToOk("MouseClick to add a point\r\n") +
                      //to last
                      (toLast < 0 ? "First point is ready to go!" : "Distance to last=" + toLast) +
                      //to first
                      (toFirst < 0 ? "" : ", to first=" + toFirst);
            return true;
        }

        protected virtual void Animation(Plane plane, Ray ray, BGTransition.SwayTransition transition)
        {
            float enter;
            if (plane.Raycast(ray, out enter))
            {
                swayTransition.Tick();

                Animate(transition, ray.GetPoint(enter), enter, plane);
            }
        }


        protected virtual bool Comply(Event currentEvent)
        {
            return true;
        }

        protected static Vector3[] GetVertsByPlaneAndDistance(Vector3 scale, Vector3 point, float distanceToCamera, Plane plane)
        {
            var m = Matrix4x4.TRS(point, Quaternion.LookRotation(plane.normal), scale);
            var verts = new[]
            {GetRectVector(-1, -1, distanceToCamera, m), GetRectVector(-1, 1, distanceToCamera, m), GetRectVector(1, 1, distanceToCamera, m), GetRectVector(1, -1, distanceToCamera, m)};
            return verts;
        }

        private static Vector3 GetRectVector(float x, float y, float distance, Matrix4x4 matrix)
        {
            return matrix.MultiplyPoint(new Vector3(x, y)*distance/18);
        }


        //abstract
        protected abstract void Animate(BGTransition.SwayTransition swayTransition, Vector3 point, float distanceToCamera, Plane plane);

        protected abstract void Cast(Event @event, Ray ray, out Vector3 position, out string error, out Plane plane);
    }
}