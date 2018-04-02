using UnityEngine;
using System.Collections;

namespace CapsuleCharacterCollisionDetection
{
	public struct CapsuleShape
	{
		public float radius {get; private set;}
		public float height {get; private set;}
		public Vector3 top {get; private set;}
		public Vector3 bottom {get; private set;}

		public Vector3 center {get {return (top + bottom) * .5f;}}
		public float pointsDistance {get {return PointsDistance(height, radius);}}
		
		public CapsuleShape(Vector3 origin, Vector3 upDirection, float height, float radius, bool normalizeParameter = true)
		{
			if(normalizeParameter) upDirection.Normalize();

			this.radius = radius;
			this.height = height;
			this.top = origin + (upDirection * (PointsDistance(height, radius) * .5f));
			this.bottom = origin - (upDirection * (PointsDistance(height, radius) * .5f));
		}

		//We multiply height by offset * 2f since I believe for every amount you add/remove from the radius, you need to do twice as much for the height to keep the ratio the same.
		public CapsuleShape(Vector3 origin, Vector3 upDirection, float height, float radius, float offset, bool normalizeParameter = true)
			: this(origin, upDirection, height + (offset * 2f), radius + offset, normalizeParameter) {}

		public CapsuleShape(Vector3 topSegment, Vector3 bottomSegment, float radius)
		{
			this.radius = radius;
			this.height = Vector3.Distance(topSegment, bottomSegment) + (radius * 2f);
			this.top = topSegment;
			this.bottom = bottomSegment;
		}

		public static float PointsDistance(float height, float radius)
		{
			return height - (radius * 2f);
		}

		public static CapsuleShape CapsuleColliderLocalPoints(CapsuleCollider collider)
		{
			Vector3 capsuleAxis = (collider.direction == 1) ? Vector3.up : (collider.direction == 0) ? Vector3.right : Vector3.forward;
			return new CapsuleShape(Vector3.zero, capsuleAxis, collider.height, collider.radius);
		}

		public void ToLocal(Transform transform)
		{
			this = ToLocalOfUniformScale(this, transform);
		}
		public static CapsuleShape ToLocalOfUniformScale(CapsuleShape capsuleShape, Transform transform)
		{
			Vector3 localSegment0 = transform.InverseTransformPoint(capsuleShape.top);
			Vector3 localSegment1 = transform.InverseTransformPoint(capsuleShape.bottom);
			
			float difference = capsuleShape.pointsDistance / Vector3.Distance(localSegment0, localSegment1);
			
			return new CapsuleShape((localSegment0 + localSegment1) * .5f, localSegment1 - localSegment0, capsuleShape.height / difference, capsuleShape.radius / difference);
		}
	}
}
