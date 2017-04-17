using UnityEngine;
using BansheeGz.BGSpline.Curve;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
    public class BGSceneViewOverlayPointAddSnap2D : BGSceneViewOverlayPointAdd
    {
        private static readonly BGTransition.SwayTransition rectTransition = new BGTransition.SwayTransition(1.1f, 1.3f, 2);

        public BGSceneViewOverlayPointAddSnap2D(BGSceneViewOverlay overlay) : base(overlay)
        {
        }

        public override string Name
        {
            get { return "Add point and Snap to Curve's 2D Plane"; }
        }

        protected override void Cast(Event @event, Ray ray, out Vector3 position, out string error, out Plane plane)
        {
            var curve = overlay.Editor.Curve;

            Get2DPlane(out plane, curve);

            float distance;
            if (!plane.Raycast(ray, out distance))
            {
                error = BGSceneViewOverlay.ToError("Curve is in 2D mode! \r\n Curve's plane does not intersect with the current point.") +
                        "\r\nUse Ctrl+Shift+Click to spawn a point at the distance,\r\n which is set in settings";

                position = ray.GetPoint(10);
            }
            else
            {
                error = null;
                position = ray.GetPoint(distance);
            }
        }

        public static void Get2DPlane(out Plane plane, BGCurve curve)
        {
            switch (curve.Mode2D)
            {
                case BGCurve.Mode2DEnum.XY:
                    plane = new Plane(curve.ToWorldDirection(Vector3.forward), curve.transform.position);
                    break;
                case BGCurve.Mode2DEnum.XZ:
                    plane = new Plane(curve.ToWorldDirection(Vector3.up), curve.transform.position);
                    break;
                default:
//            case BGCurve.Mode2DEnum.YZ:
                    plane = new Plane(curve.ToWorldDirection(Vector3.right), curve.transform.position);
                    break;
            }
        }

        protected override bool Comply(Event currentEvent)
        {
            return overlay.Editor.Curve.Mode2D != BGCurve.Mode2DEnum.Off;
        }

        protected override void Animate(BGTransition.SwayTransition swayTransition, Vector3 point, float distanceToCamera, Plane plane)
        {
            rectTransition.Tick();

            var verts = GetVertsByPlaneAndDistance(new Vector3(rectTransition.Value, rectTransition.Value, rectTransition.Value), point, distanceToCamera, plane);


            var color = BGCurveSettingsForEditor.I.Get<Color32>(BGCurveSettingsForEditor.HandleColorForAddAndSnap2DKey);
            Handles.DrawSolidRectangleWithOutline(verts, color, new Color32(color.r, color.g, color.b, 255));
            Handles.DrawWireDisc(point, Vector3.Cross(verts[1] - verts[0], verts[2] - verts[0]), swayTransition.Value*distanceToCamera/24);
        }
    }
}