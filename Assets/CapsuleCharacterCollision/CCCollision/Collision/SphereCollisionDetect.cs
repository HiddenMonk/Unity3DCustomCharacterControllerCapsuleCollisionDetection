using System;
using UnityEngine;
using System.Collections.Generic;

namespace CapsuleCharacterCollisionDetection
{
	public static class SphereCollisionDetect
	{
		static List<Collider> colliderBufferSphere = new List<Collider>();
		static List<ContactInfo> contactsBufferSphere = new List<ContactInfo>();
		public static List<SphereCollisionInfo> DetectCollisions(Vector3 origin, float radius, int mask, IList<Component> ignoreColliders, List<SphereCollisionInfo> resultBuffer, float checkOffset = 0f, bool multipleContactsPerCollider = true)
		{
			resultBuffer.Clear();
			colliderBufferSphere.Clear();

			ExtPhysics.OverlapSphere(origin, radius + checkOffset, ignoreColliders, colliderBufferSphere, mask);
			if(colliderBufferSphere.Count == 0) return resultBuffer;

			for(int i = 0; i < colliderBufferSphere.Count; i++)
			{
				contactsBufferSphere = ExtCollider.ClosestPointsOnSurface(colliderBufferSphere[i], origin, radius + checkOffset, contactsBufferSphere, multipleContactsPerCollider);

				for(int j = 0; j < contactsBufferSphere.Count; j++)
				{
					//We store just the radius, not radius + checkOffset, so that our depenetration method has the correct radius to depenetrate with.
					resultBuffer.Add(new SphereCollisionInfo(true, colliderBufferSphere[i], origin, radius, contactsBufferSphere[j].point, contactsBufferSphere[j].normal));
				}
			}

			return resultBuffer;
		}

		public static List<SphereCollisionInfo> DetectCollisions(Vector3 origin, Vector3 directionUp, float height, float radius, int mask, IList<Component> ignoreColliders, List<SphereCollisionInfo> resultBuffer, float checkOffset = 0f, bool multipleContactsPerCollider = true)
		{
			CapsuleShape points = new CapsuleShape(origin, directionUp, height, radius, checkOffset);
			return DetectCollisions(points.top, points.bottom, radius, mask, ignoreColliders, resultBuffer, checkOffset, multipleContactsPerCollider);
		}

		static List<Collider> colliderBufferCapsule = new List<Collider>();
		static List<ContactInfo> contactsBufferCapsule = new List<ContactInfo>();
		public static List<SphereCollisionInfo> DetectCollisions(Vector3 segment0, Vector3 segment1, float radius, int mask, IList<Component> ignoreColliders, List<SphereCollisionInfo> resultBuffer, float checkOffset = 0f, bool multipleContactsPerCollider = true)
		{
			resultBuffer.Clear();
			colliderBufferCapsule.Clear();

			ExtPhysics.OverlapCapsule(segment0, segment1, radius + checkOffset, ignoreColliders, colliderBufferCapsule, mask);
			if(colliderBufferCapsule.Count == 0) return resultBuffer;

			for(int i = 0; i < colliderBufferCapsule.Count; i++)
			{
				contactsBufferCapsule = ExtCollider.ClosestPointsOnSurface(colliderBufferCapsule[i], segment0, segment1, radius + checkOffset, contactsBufferCapsule, multipleContactsPerCollider);

				for(int j = 0; j < contactsBufferCapsule.Count; j++)
				{
					//We calculate sphereDetectionOriginInCapsule for our depenetration method since we need to know where the spheres detection origin would be within the capsule.
					Vector3 sphereDetectionOriginInCapsule = Vector3.zero;
					if((colliderBufferCapsule[i] is CapsuleCollider || colliderBufferCapsule[i] is SphereCollider) && !ExtVector3.IsParallel(segment1 - segment0, contactsBufferCapsule[j].normal))
					{
						sphereDetectionOriginInCapsule = Geometry.ClosestPointsOnSegmentToLine(segment0, segment1, contactsBufferCapsule[j].point, contactsBufferCapsule[j].normal).first;
					}else{
						sphereDetectionOriginInCapsule = Geometry.ClosestPointOnLineSegmentToPoint(contactsBufferCapsule[j].point, segment0, segment1);
					}

					//We store just the radius, not radius + checkOffset, so that our depenetration method has the correct radius to depenetrate with.
					resultBuffer.Add(new SphereCollisionInfo(true, colliderBufferCapsule[i], sphereDetectionOriginInCapsule, radius, contactsBufferCapsule[j].point, contactsBufferCapsule[j].normal));
				}
			}

			return resultBuffer;
		}

		public static Vector3 Depenetrate(List<SphereCollisionInfo> collisionPoints, int maxIterations = 1)
		{
			if(collisionPoints.Count > 0 && maxIterations > 0)
			{
				Vector3 depenetrationVelocity = Vector3.zero;
				Vector3 totalDepenetrationVelocity = Vector3.zero;

				//Since with each iteration we are using old collision data, higher maxIterations does not mean more accuracy. You will need to tune it to your liking.
				for(int i = 0; i < maxIterations; i++)
				{
					for(int j = 0; j < collisionPoints.Count; j++)
					{
						SphereCollisionInfo cp = collisionPoints[j];
		
						Vector3 detectOriginOffset = totalDepenetrationVelocity + depenetrationVelocity + cp.detectionOrigin;

						//We check if we are already depenetrated.
						if(ExtVector3.MagnitudeInDirection(detectOriginOffset - cp.closestPointOnSurface, cp.interpolatedNormal, false) > cp.sphereRadius) continue;
					
						//We take into account how much we already depenetrated.
						Vector3 collisionVelocityOffset = Vector3.Project(detectOriginOffset - cp.closestPointOnSurface, cp.GetCollisionVelocity());
						
						float collisionMagnitude = SphereCollisionInfo.GetCollisionMagnitudeInDirection(collisionVelocityOffset, cp.interpolatedNormal, cp.sphereRadius) + .0001f;

						depenetrationVelocity += SphereCollisionInfo.GetDepenetrationVelocity(cp.interpolatedNormal, collisionMagnitude);
					}

					if(depenetrationVelocity == Vector3.zero) break;
					
					totalDepenetrationVelocity += depenetrationVelocity;
					depenetrationVelocity = Vector3.zero;
				}

				return totalDepenetrationVelocity;
			}
			
			return Vector3.zero;
		}

		//I think this works fine with our capsule detection, but doesnt really work with spheres shaping a capsule. The reason for this is
		//when having spheres shape a capsule, its possible for a sphere to detect a hit and set the interpolated normal in a way that blocks all other hits behind it, however,
		//when this sphere depenetrates, since it isnt taking into account that we wanted to treat it like a capsule, it wont depenetrate enough for the hits behind it to be resolved.
		//However, since our capsule DetectCollisions handles placing the spheres properly to form a capsule, it should work with that.
		//This method is pretty similar to the "CleanUp" method in our meshbsptree.
		static List<MPlane> ignoreBehindPlanes = new List<MPlane>();
		public static List<SphereCollisionInfo> CleanByIgnoreBehindPlane(List<SphereCollisionInfo> collisionPoints)
		{
			if(collisionPoints.Count > 1)
			{
				ignoreBehindPlanes.Clear();
				
				//Taking advantage of C# built in QuickSort algorithm
				collisionPoints.Sort(SphereCollisionInfo.SphereCollisionComparerDescend.defaultComparer);

				for(int i = collisionPoints.Count - 1; i >= 0; i--)
				{
					if(!MPlane.IsBehindPlanes(collisionPoints[i].closestPointOnSurface, ignoreBehindPlanes, -.0001f))
					{
						ignoreBehindPlanes.Add(new MPlane(collisionPoints[i].interpolatedNormal, collisionPoints[i].closestPointOnSurface, false));
					}else{
						collisionPoints.RemoveAt(i);
					}
				}
			}

			return collisionPoints;
		}
	}
}