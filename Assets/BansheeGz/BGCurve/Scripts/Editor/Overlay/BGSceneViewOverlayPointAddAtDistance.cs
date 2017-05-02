using BansheeGz.BGSpline.Curve;
using UnityEngine;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
	public class BGSceneViewOverlayPointAddAtDistance : BGSceneViewOverlayPointAdd
	{
		private static readonly Color32 PointersColor = Color.white;
		private Vector3 lastPosition;

		public BGSceneViewOverlayPointAddAtDistance(BGSceneViewOverlay overlay) : base(overlay)
		{
		}

		public override string Name
		{
			get { return "Add point at Distance"; }
		}


		protected override bool Comply(Event currentEvent)
		{
			return currentEvent.shift;
		}

		protected override void Cast(Event @event, Ray ray, out Vector3 position, out string error, out Plane plane)
		{
			var settings = overlay.Editor.Settings;

			lastPosition = position = ray.GetPoint(settings.NewPointDistance);

			var curve = overlay.Editor.Curve;
			if (curve.Mode2DOn)
			{
				BGSceneViewOverlayPointAddSnap2D.Get2DPlane(out plane, curve);
				position = position - Vector3.Project(position, plane.normal.normalized);
			}
			else plane = new Plane(ray.direction.normalized, lastPosition);

			error = null;
		}

		protected override void AdditionalPreview(BGCurvePoint newPoint)
		{
			var curve = overlay.Editor.Curve;
			if (!curve.Mode2DOn) return;

			Handles.DrawLine(lastPosition, newPoint.PositionWorld);
		}

		protected override void Animation(Plane plane, Ray ray, BGTransition.SwayTransition transition)
		{
			var curve = overlay.Editor.Curve;

			if (!curve.Mode2DOn) base.Animation(plane, ray, transition);
			else
			{
				var settings = overlay.Editor.Settings;
				transition.Tick();
				Animate(transition, lastPosition, settings.NewPointDistance, plane);
			}
		}

		protected override void Animate(BGTransition.SwayTransition swayTransition, Vector3 point, float distanceToCamera, Plane plane)
		{
			var verts = GetVertsByPlaneAndDistance(new Vector3(swayTransition.Value, swayTransition.Value, swayTransition.Value), point, distanceToCamera, plane);

			var size = swayTransition.Value * ScalePreviewPoint * distanceToCamera / 5;

			BGEditorUtility.SwapHandlesColor(PointersColor, () =>
			{
				foreach (var position in verts)
				{
#if UNITY_5_6_OR_NEWER
					Handles.ConeHandleCap(0, position, Quaternion.LookRotation(point - position), size, EventType.Repaint);
#else
					Handles.ConeCap(0, position, Quaternion.LookRotation(point - position), size);
#endif
				}
			});
		}
	}
}