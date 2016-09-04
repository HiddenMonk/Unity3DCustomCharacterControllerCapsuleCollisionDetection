using System;
using UnityEngine;

namespace CapsuleCharacterCollisionDetection
{
	//Axis aligned bounding box
	public struct AABB
	{
		public Vector3 origin;
		public Vector3 halfExtents;

		public Vector3 minExtent { get { return origin - halfExtents; } }
		public Vector3 maxExtent { get { return origin + halfExtents; } }

		public AABB(Vector3 origin, Vector3 halfExtents)
		{
			this.origin = origin;
			this.halfExtents = halfExtents;
		}

		public static AABB CreateCapsuleAABB(Vector3 origin, Vector3 direction, float height, float radius, bool normalizeParameter = true)
		{
			if(normalizeParameter) direction.Normalize();

			CapsuleShape capsulePoints = new CapsuleShape(origin, direction, height, radius);
			return CreateCapsuleAABB(capsulePoints.top, capsulePoints.bottom, radius);
		}
		public static AABB CreateCapsuleAABB(Vector3 segment0, Vector3 segment1, float radius)
		{
			for(int i = 0; i < 3; i++)
			{
				if(segment0[i] < segment1[i])
				{
					segment0[i] -= radius;
					segment1[i] += radius;
				}else{
					segment0[i] += radius;
					segment1[i] -= radius;
				}
			}

			return new AABB((segment0 + segment1) * .5f, ExtVector3.Abs(segment1 - segment0) * .5f); //It seems the extents must be positives for our overlap tests to work...
		}

		public static AABB CreateSphereAABB(Vector3 origin, float radius)
		{
			return new AABB(origin, Vector3.one * radius);
		}

		public static bool AABBOverlapsAABB(AABB a, AABB b)
		{
			if(Mathf.Abs(a.origin.x - b.origin.x) > (a.halfExtents.x + b.halfExtents.x)) return false;
			if(Mathf.Abs(a.origin.y - b.origin.y) > (a.halfExtents.y + b.halfExtents.y)) return false;
			if(Mathf.Abs(a.origin.z - b.origin.z) > (a.halfExtents.z + b.halfExtents.z)) return false;
			return true;
		}

		public static bool SphereOverlapsAABB(AABB aabb, Vector3 sphereOrigin, float radius)
		{
			float distSquared = radius * radius;
			Vector3 minExtents = aabb.minExtent;
			Vector3 maxExtents = aabb.maxExtent;

			if (sphereOrigin.x < minExtents.x) distSquared -= ExtMathf.Squared(sphereOrigin.x - minExtents.x);
			else if (sphereOrigin.x > maxExtents.x) distSquared -= ExtMathf.Squared(sphereOrigin.x - maxExtents.x);
			if (sphereOrigin.y < minExtents.y) distSquared -= ExtMathf.Squared(sphereOrigin.y - minExtents.y);
			else if (sphereOrigin.y > maxExtents.y) distSquared -= ExtMathf.Squared(sphereOrigin.y - maxExtents.y);
			if (sphereOrigin.z < minExtents.z) distSquared -= ExtMathf.Squared(sphereOrigin.z - minExtents.z);
			else if (sphereOrigin.z > maxExtents.z) distSquared -= ExtMathf.Squared(sphereOrigin.z - maxExtents.z);

			return distSquared >= 0;
		}

		//Taken from http://gamedev.stackexchange.com/questions/18436/most-efficient-aabb-vs-ray-collision-algorithms
		public static IntersectPoints LineAABBIntersection(Vector3 lineOrigin, Vector3 lineDirection, AABB box)
		{
			Vector3 T1 = Vector3.zero;
			Vector3 T2 = Vector3.zero; 
			float nearHit = float.MinValue;
			float farHit = float.MaxValue;
			Vector3 boxMin = box.minExtent;
			Vector3 boxMax = box.maxExtent;

			for(int i = 0; i < 3; i++) //We test slabs in every direction
			{
				if(lineDirection[i] == 0) //Ray parallel to planes in this direction
				{
					if((lineOrigin[i] < boxMin[i]) || (lineOrigin[i] > boxMax[i]))
					{
						return new IntersectPoints(); //Parallel AND outside box : no intersection possible
					}
				} 
				else 
				{ 
					//Ray not parallel to planes in this direction
					T1[i] = (boxMin[i] - lineOrigin[i]) / lineDirection[i];
					T2[i] = (boxMax[i] - lineOrigin[i]) / lineDirection[i];

					if(T1[i] > T2[i])
					{
						//We want T_1 to hold values for intersection with near plane
						Vector3 t1 = T1;
						T1 = T2;
						T2 = t1;
					}
					if(T1[i] > nearHit)
					{
						nearHit = T1[i];
					}
					if(T2[i] < farHit)
					{
						farHit = T2[i];
					}
					if((nearHit > farHit) || (farHit < 0))
					{
						return new IntersectPoints();
					}
				}
			}

			return new IntersectPoints(lineOrigin + (lineDirection * nearHit), lineOrigin + (lineDirection * farHit));
		}
	}
}