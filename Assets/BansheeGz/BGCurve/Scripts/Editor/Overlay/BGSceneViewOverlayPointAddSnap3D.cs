using UnityEngine;
using BansheeGz.BGSpline.Curve;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
    //add point and snap to the mesh (or curve's 2D plane)
    public class BGSceneViewOverlayPointAddSnap3D : BGSceneViewOverlayPointAdd
    {
        public BGSceneViewOverlayPointAddSnap3D(BGSceneViewOverlay overlay)
            : base(overlay)
        {
        }

        public override string Name
        {
            get { return "Add point and Snap to a Mesh"; }
        }

        protected override void Animate(BGTransition.SwayTransition swayTransition, Vector3 point, float distanceToCamera, Plane plane)
        {
            //show rect
            var verts = GetVertsByPlaneAndDistance(new Vector3(swayTransition.Value, swayTransition.Value, swayTransition.Value), point, distanceToCamera, plane);
            var color = BGCurveSettingsForEditor.I.Get<Color32>(BGCurveSettingsForEditor.HandleColorForAddAndSnap3DKey);
            Handles.DrawSolidRectangleWithOutline(verts, color, new Color32(color.r, color.g, color.b, 255));
        }


        protected override void Cast(Event @event, Ray ray, out Vector3 position, out string error, out Plane plane)
        {
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                position = hit.point;
                plane = new Plane(hit.normal, hit.point);
                error = null;
            }
            else
            {
                error = BGSceneViewOverlay.ToError("No mesh (or collider) to snap a point to!") + " \r\n Use Ctrl+Shift+Click to spawn a point at the distance,\r\n which is set in settings";
                position = ray.GetPoint(10);
                plane = new Plane();
            }
        }

        protected override bool Comply(Event currentEvent)
        {
            return overlay.Editor.Curve.Mode2D == BGCurve.Mode2DEnum.Off;
        }
    }
}