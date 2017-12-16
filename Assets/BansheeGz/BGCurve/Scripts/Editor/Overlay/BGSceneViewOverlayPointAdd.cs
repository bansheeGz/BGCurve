using UnityEngine;
using BansheeGz.BGSpline.Curve;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
    public abstract class BGSceneViewOverlayPointAdd : BGSceneViewOverlay.SceneAction
    {
        protected const float ScalePreviewPoint = .18f;


        private static readonly BGTransition.SwayTransition swayTransition = new BGTransition.SwayTransition(.8f, 1.2f, .6);

        protected virtual bool VisualizingSections
        {
            get { return true; }
        }

        protected virtual bool ShowingDistance
        {
            get { return true; }
        }

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

#if UNITY_5_6_OR_NEWER
		Handles.SphereHandleCap(0, controlWorld, Quaternion.identity, size, EventType.Repaint);
#else
		Handles.SphereCap(0, controlWorld, Quaternion.identity, size);
#endif
            Handles.DrawLine(position, controlWorld);
        }

        protected static void DrawSection(BGCurvePointI from, BGCurvePointI to, int parts)
        {
            BGEditorUtility.Split(@from, to, parts, (fromPos, toPos) => Handles.DrawDottedLine(fromPos, toPos, 2));
        }


        // preview
        protected void Preview(Vector3 position, BGCurve curve, ref float toLast, ref float toFirst)
        {
            var settings = overlay.Editor.Settings;

            //show point 
            BGEditorUtility.SwapHandlesColor(settings.SphereColor, () =>
            {
#if UNITY_5_6_OR_NEWER
			Handles.SphereHandleCap(0, position, Quaternion.identity, BGEditorUtility.GetHandleSize(position, ScalePreviewPoint), EventType.Repaint);
#else
			Handles.SphereCap(0, position, Quaternion.identity, BGEditorUtility.GetHandleSize(position, ScalePreviewPoint));
#endif
            });

            //create a point
            var newPoint = CreatePointForPreview(position, curve, out toLast, out toFirst, settings);

            //show controls
            if (newPoint.ControlType != BGCurvePoint.ControlTypeEnum.Absent) PreviewControls(settings, position, newPoint.ControlFirstWorld, newPoint.ControlSecondWorld);

            if (curve.PointsCount == 0) return;

            if (VisualizingSections)
            {
                BGEditorUtility.SwapHandlesColor(BGCurveSettingsForEditor.I.Get<Color32>(BGCurveSettingsForEditor.ColorForNewSectionPreviewKey), () =>
                {
                    // last To new
                    DrawSection(curve[curve.PointsCount - 1], newPoint, settings.Sections);

                    AdditionalPreview(newPoint);

                    // new To zero
                    if (curve.Closed) DrawSection(newPoint, curve[0], settings.Sections);
                });
            }
        }

        protected virtual BGCurvePoint CreatePointForPreview(Vector3 position, BGCurve curve, out float toLast, out float toFirst, BGCurveSettings settings)
        {
            return BGNewPointPositionManager.CreatePoint(position, curve, settings.ControlType, settings.Sections, out toLast, out toFirst, false);
        }

        protected virtual void AdditionalPreview(BGCurvePoint newPoint)
        {
        }


        //see base class for description
        internal override bool Seize(Event currentEvent, ref Vector3 position, ref string message)
        {
            if (!Comply(currentEvent)) return false;


            Vector3 intersectionPosition;
            Plane plane;


            if (currentEvent.type == EventType.MouseDown && currentEvent.control && currentEvent.button == 0)
            {
                //Mouse down for some action
                var curve = overlay.Editor.Curve;
                var settings = overlay.Editor.Settings;

                Cast(currentEvent, HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition), out intersectionPosition, out message, out plane);

                if (message != null) BGCurveEditor.OverlayMessage.Display(message);
                else
                {
                    position = intersectionPosition;
                    AddPoint(curve, intersectionPosition, settings);
                }
                overlay.EventCanceller = new BGEditorUtility.EventCanceller();
                return true;
            }


            if (!(currentEvent.type == EventType.Repaint && currentEvent.control || currentEvent.type == EventType.MouseMove && currentEvent.control)) return false;

            var ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
            Cast(currentEvent, ray, out intersectionPosition, out message, out plane);

            position = intersectionPosition;

            if (message != null) return true;

            Animation(plane, ray, swayTransition);

            //preview
            float toLast = -1, toFirst = -1;
            Preview(intersectionPosition, overlay.Editor.Curve, ref toLast, ref toFirst);

            //distance
            message = BGSceneViewOverlay.ToOk("MouseClick to add a point\r\n")
                      + (!ShowingDistance
                          ? ""
                          :
                          //to last
                          (toLast < 0 ? "First point is ready to go!" : "Distance to last=" + toLast) +
                          //to first
                          (toFirst < 0 ? "" : ", to first=" + toFirst));
            return true;
        }


        //default implementation adds a point to the spline's end
        protected virtual void AddPoint(BGCurve curve, Vector3 intersectionPosition, BGCurveSettings settings)
        {
            BGCurveEditor.AddPoint(curve,
                BGNewPointPositionManager.CreatePoint(intersectionPosition, curve, settings.ControlType, settings.Sections, true),
                curve.PointsCount);
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