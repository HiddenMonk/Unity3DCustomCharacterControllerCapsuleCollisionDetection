using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace CapsuleCharacterCollisionDetection
{
	public static class ExtCollider
	{
		#region Collision Detection
		
		//Non uniformly scaled objects may return bad results.

		#region ClosestPoints Wrappers
		
		static List<ContactInfo> singleSegmentContactBuffer = new List<ContactInfo>();
		public static ContactInfo ClosestPointOnSurface(Collider collider, Vector3 segment0, Vector3 segment1, float radius)
		{
			ClosestPointsOnSurface(collider, segment0, segment1, radius, singleSegmentContactBuffer, false);
			return (singleSegmentContactBuffer.Count > 0) ? singleSegmentContactBuffer[0] : new ContactInfo();
		}

		public static List<ContactInfo> ClosestPointsOnSurface(Collider collider, Vector3 segment0, Vector3 segment1, float radius, List<ContactInfo> resultsBuffer, bool multipleContacts = true)
		{
			resultsBuffer.Clear();

			if(collider is MeshCollider)
			{
				MeshAABBTree meshTree = collider.GetComponent<MeshAABBTree>();
				if(meshTree != null)
				{
					if(multipleContacts)
					{
						meshTree.ClosestPointsOnSurface(segment0, segment1, radius, resultsBuffer);
					}else{
						ContactInfo contact = meshTree.ClosestPointOnSurface(segment0, segment1, radius);
						if(contact.point != Vector3.zero) resultsBuffer.Add(contact);
					}
				}
			}
			else if(collider is BoxCollider) resultsBuffer.Add(ClosestPointOnSurface((BoxCollider)collider, segment0, segment1));
			else if(collider is SphereCollider) resultsBuffer.Add(ClosestPointOnSurface((SphereCollider)collider, segment0, segment1));
			else if(collider is CapsuleCollider) resultsBuffer.Add(ClosestPointOnSurface((CapsuleCollider)collider, segment0, segment1));
			else if(collider is TerrainCollider) { /*Not supported*/ }

			return resultsBuffer;
		}

		static List<ContactInfo> singlePointContactBuffer = new List<ContactInfo>();
		public static ContactInfo ClosestPointOnSurface(Collider collider, Vector3 point, float radius)
		{
			ClosestPointsOnSurface(collider, point, radius, singlePointContactBuffer, false);
			return (singlePointContactBuffer.Count > 0) ? singlePointContactBuffer[0] : new ContactInfo();
		}

		public static List<ContactInfo> ClosestPointsOnSurface(Collider collider, Vector3 point, float radius, List<ContactInfo> resultsBuffer, bool multipleContacts = true)
		{
			resultsBuffer.Clear();

			if(collider is MeshCollider)
			{
				MeshAABBTree meshTree = collider.GetComponent<MeshAABBTree>();
				if(meshTree != null)
				{
					if(multipleContacts)
					{
						meshTree.ClosestPointsOnSurface(point, radius, resultsBuffer);
					}else{
						ContactInfo contact = meshTree.ClosestPointOnSurface(point, radius);
						if(contact.point != Vector3.zero) resultsBuffer.Add(contact);
					}
				}
			}
			else if(collider is BoxCollider) resultsBuffer.Add(ClosestPointOnSurface((BoxCollider)collider, point));
			else if(collider is SphereCollider) resultsBuffer.Add(ClosestPointOnSurface((SphereCollider)collider, point));
			else if(collider is CapsuleCollider) resultsBuffer.Add(ClosestPointOnSurface((CapsuleCollider)collider, point));
			else if(collider is TerrainCollider) { /*Not supported*/ }

			return resultsBuffer;
		}

		#endregion

		public static ContactInfo ClosestPointOnSurface(SphereCollider collider, Vector3 segment0, Vector3 segment1)
		{
			Vector3 localSegment0 = ToLocal(collider, collider.center, segment0);
			Vector3 localSegment1 = ToLocal(collider, collider.center, segment1);

			Vector3 closest = Geometry.ClosestPointOnLineSegmentToPoint(Vector3.zero, localSegment0, localSegment1);
		
			ContactInfo contact = new ContactInfo();
			contact.normal = closest - Vector3.zero;
			contact.normal = CheckAndSetNormal(contact.normal, Vector3.zero, (localSegment0 + localSegment1) * .5f);
			contact.point = Vector3.zero + (contact.normal * collider.radius);
			
			return ToGlobal(collider, collider.center, contact);
		}

		public static ContactInfo ClosestPointOnSurface(SphereCollider collider, Vector3 point)
		{
			Vector3 colliderCenter = collider.transform.TransformPoint(collider.center);

			ContactInfo contact = new ContactInfo();
			contact.normal = (point - colliderCenter);
			contact.normal = CheckAndSetNormal(contact.normal, collider.center, point);
			contact.point = colliderCenter + (contact.normal * (collider.radius * ExtVector3.Maximum(collider.transform.localScale)));
			
			return contact;
		}

		public static ContactInfo ClosestPointOnSurface(CapsuleCollider collider, Vector3 segment0, Vector3 segment1)
		{
			CapsuleShape points = CapsuleShape.CapsuleColliderLocalPoints(collider);

			Vector3 localSegment0 = ToLocal(collider, collider.center, segment0);
			Vector3 localSegment1 = ToLocal(collider, collider.center, segment1);

			IntersectPoints closests = Geometry.ClosestPointsOnTwoLineSegments(localSegment0, localSegment1, points.top, points.bottom);
			
			ContactInfo contact = new ContactInfo();
			contact.normal = closests.first - closests.second;
			contact.normal = CheckAndSetNormal(contact.normal, Vector3.zero, (localSegment0 + localSegment1) * .5f);
			contact.point = closests.second + (contact.normal * collider.radius);

			return ToGlobal(collider, collider.center, contact);
		}
		
		public static ContactInfo ClosestPointOnSurface(CapsuleCollider collider, Vector3 point)
		{
			CapsuleShape points = CapsuleShape.CapsuleColliderLocalPoints(collider);
			Vector3 localPoint = ToLocal(collider, collider.center, point);

			Vector3 closest = Geometry.ClosestPointOnLineSegmentToPoint(localPoint, points.top, points.bottom);
			
			ContactInfo contact = new ContactInfo();
			contact.normal = localPoint - closest;
			contact.normal = CheckAndSetNormal(contact.normal, points.center, localPoint);
			contact.point = closest + (contact.normal * collider.radius);
			
			return ToGlobal(collider, collider.center, contact);
		}

		public static ContactInfo ClosestPointOnSurface(BoxCollider collider, Vector3 segment0, Vector3 segment1)
		{
			Vector3 localSegment0 = ToLocal(collider, collider.center, segment0);
			Vector3 localSegment1 = ToLocal(collider, collider.center, segment1);
			Vector3 lineCenter = (localSegment0 + localSegment1) * .5f;
			Vector3 extents = collider.size;
			Vector3 halfExtents = extents * .5f;

			//We try to choose the best 3 faces on the box to do our rectangle tests.
		//Is it safe to use the lineCenter as the reference point?
			Vector3 xAxis = new Vector3(Mathf.Sign(lineCenter.x), 0, 0);
			Vector3 yAxis = new Vector3(0, Mathf.Sign(lineCenter.y), 0);
			Vector3 zAxis = new Vector3(0, 0, Mathf.Sign(lineCenter.z));

			Rect3D xRect = new Rect3D(Vector3.Scale(xAxis, halfExtents), Vector3.forward, Vector3.up, extents.z, extents.y);
			Rect3D yRect = new Rect3D(Vector3.Scale(yAxis, halfExtents), Vector3.right, Vector3.forward, extents.x, extents.z);
			Rect3D zRect = new Rect3D(Vector3.Scale(zAxis, halfExtents), Vector3.right, Vector3.up, extents.x, extents.y);

			IntersectPoints xIntersect = Geometry.ClosestPointOnRectangleToLine(localSegment0, localSegment1, xRect, true);
			float xDistance = (xIntersect.second - xIntersect.first).sqrMagnitude;
			IntersectPoints yIntersect = Geometry.ClosestPointOnRectangleToLine(localSegment0, localSegment1, yRect, true);
			float yDistance = (yIntersect.second - yIntersect.first).sqrMagnitude;
			IntersectPoints zIntersect = Geometry.ClosestPointOnRectangleToLine(localSegment0, localSegment1, zRect, true);
			float zDistance = (zIntersect.second - zIntersect.first).sqrMagnitude;
			
			IntersectPoints closestIntersect = new IntersectPoints();
			Vector3 closestNormal = Vector3.zero;
			if(xDistance <= yDistance && xDistance <= zDistance)
			{
				closestIntersect = xIntersect;
				closestNormal = xAxis;
			}
			else if(yDistance <= xDistance && yDistance <= zDistance)
			{
				closestIntersect = yIntersect;
				closestNormal = yAxis;
			}else{
				closestIntersect = zIntersect;
				closestNormal = zAxis;
			}

			//Two intersect distances might be the same, so we need to choose the best normal
			//Must compare with ExtMathf.Approximately since float precision can cause errors, especially when dealing with different scales.
			if(ExtMathf.Approximately(xDistance, yDistance, .0001f) || ExtMathf.Approximately(xDistance, zDistance, .0001f) || ExtMathf.Approximately(yDistance, zDistance, .0001f))
			{
				//We need to scale by the colliders scale for reasons I am not too sure of. Has to do with if the collider is scaled weird,
				//in local space it is just a uniform box which throws off our direction calculation below. Not sure if we should use local or lossy scale.
				Vector3 closestDirection = Vector3.Scale(closestIntersect.first - closestIntersect.second, collider.transform.localScale);

				float xDot = Vector3.Dot(closestDirection, xAxis);
				float yDot = Vector3.Dot(closestDirection, yAxis);
				float zDot = Vector3.Dot(closestDirection, zAxis);

				if(xDot >= yDot && xDot >= zDot)
				{
					closestNormal = xAxis;
				}
				else if(yDot >= xDot && yDot >= zDot)
				{
					closestNormal = yAxis;
				}else{
					closestNormal = zAxis;
				}
			}

			return ToGlobal(collider, collider.center, new ContactInfo(closestIntersect.second, closestNormal));
		}

		//Taken from Iron-Warrior Super Character Controller - SuperCollider class, added a normal to return
		public static ContactInfo ClosestPointOnSurface(BoxCollider collider, Vector3 point)
		{
			// Cache the collider transform
			var ct = collider.transform;

			// Firstly, transform the point into the space of the collider
			var localPoint = ct.InverseTransformPoint(point);

			// Now, shift it to be in the center of the box
			localPoint -= collider.center;

			//Pre multiply to save operations.
			var halfSize = collider.size * 0.5f;

			// Clamp the points to the collider's extents
			var localNorm = new Vector3(
					Mathf.Clamp(localPoint.x, -halfSize.x, halfSize.x),
					Mathf.Clamp(localPoint.y, -halfSize.y, halfSize.y),
					Mathf.Clamp(localPoint.z, -halfSize.z, halfSize.z)
				);

			//Calculate distances from each edge
			var dx = Mathf.Min(Mathf.Abs(halfSize.x - localNorm.x), Mathf.Abs(-halfSize.x - localNorm.x));
			var dy = Mathf.Min(Mathf.Abs(halfSize.y - localNorm.y), Mathf.Abs(-halfSize.y - localNorm.y));
			var dz = Mathf.Min(Mathf.Abs(halfSize.z - localNorm.z), Mathf.Abs(-halfSize.z - localNorm.z));

			Vector3 normal = Vector3.zero;

			// Select a face to project on
			if (dx < dy && dx < dz)
			{
				localNorm.x = Mathf.Sign(localNorm.x) * halfSize.x;
				normal = Mathf.Sign(localNorm.x) * Vector3.right;
			}
			else if (dy < dx && dy < dz)
			{
				localNorm.y = Mathf.Sign(localNorm.y) * halfSize.y;
				normal = Mathf.Sign(localNorm.y) * Vector3.up;
			}
			else if (dz < dx && dz < dy)
			{
				localNorm.z = Mathf.Sign(localNorm.z) * halfSize.z;
				normal = Mathf.Sign(localNorm.z) * Vector3.forward;
			}
			else
			{
				//On an edge somewhere. Find the best normal
				normal = ExtVector3.ClosestGeneralDirection(Vector3.Scale(localPoint - localNorm, collider.transform.localScale));
			}

			if(normal == Vector3.zero) normal = Vector3.up; //set to random normal if none found.

			return ToGlobal(collider, collider.center, new ContactInfo(localNorm, normal));
		}

		static Vector3 ToLocal(Collider collider, Vector3 colliderCenter, Vector3 point)
		{
			return collider.transform.InverseTransformPoint(point) - colliderCenter;
		}

		static ContactInfo ToGlobal(Collider collider, Vector3 colliderCenter, ContactInfo contact)
		{
			contact.point = collider.transform.TransformPoint(contact.point + colliderCenter);
			contact.normal = collider.transform.TransformDirection(contact.normal); //transform.TransformVector might be better?
			return contact;
		}

		static Vector3 CheckAndSetNormal(Vector3 normal, Vector3 colliderCenter, Vector3 pointCenter)
		{
			if(normal == Vector3.zero)
			{
				normal = pointCenter - colliderCenter; //closest points are right ontop of eachother, so we use centers
				
				if(normal == Vector3.zero)
				{
					normal = Vector3.up; //We are right ontop of eachother, set normal to anything
				}
			}

			return normal.normalized;
		}

		#endregion
	}
}
