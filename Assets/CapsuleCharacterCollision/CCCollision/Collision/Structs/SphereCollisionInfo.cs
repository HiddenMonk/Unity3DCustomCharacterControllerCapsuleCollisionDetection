using System;
using UnityEngine;
using System.Collections.Generic;

namespace CapsuleCharacterCollisionDetection
{
	public struct SphereCollisionInfo
	{
		public bool hasCollided;
		public Collider collider;
		public float sphereRadius;

		Vector3 _detectionOrigin;
		public Vector3 detectionOrigin {get {return _detectionOrigin;} set {_detectionOrigin = value; SetInterpolatedNormal();}}

		Vector3 _closestPointOnSurface;
		public Vector3 closestPointOnSurface {get {return _closestPointOnSurface;} set {_closestPointOnSurface = value; SetInterpolatedNormal();}}

		Vector3 _normal;
		public Vector3 normal {get {return _normal;} set {_normal = value; SetInterpolatedNormal();}}

		//This normal will help us with corners.
		Vector3 _interpolatedNormal;
		public Vector3 interpolatedNormal { get { if(_interpolatedNormal == Vector3.zero) { SetInterpolatedNormal(); } return _interpolatedNormal; } }

		public bool isOnEdge {get; private set;}

		public SphereCollisionInfo(bool hasCollided, Collider collider, Vector3 detectionOrigin, float sphereRadius, Vector3 closestPointOnSurface, Vector3 normal)
		{
			this.hasCollided = hasCollided;
			this.collider = collider;
			this.sphereRadius = sphereRadius;
			_detectionOrigin = detectionOrigin;
			_closestPointOnSurface = closestPointOnSurface;
			_normal = normal;
			_interpolatedNormal = Vector3.zero;
			isOnEdge = false;

			//Its important to set the interpolated normal here, otherwise if we added the SphereCollisionInfo to a collection while the interpolated normal wasnt set, 
			//then every time we access the struct within the collection, it will return a copy which doesnt have the interpolated normal set, causing us to constantly set it.
			SetInterpolatedNormal();
		}

		void SetInterpolatedNormal()
		{
			_interpolatedNormal = GetInterpolatedNormal();
		}

		Vector3 GetInterpolatedNormal()
		{
			isOnEdge = false;

			Vector3 interpolatedNormal = detectionOrigin - closestPointOnSurface;
			
			//If the detection origin and closestpoint are right on eachother, we return the normal. This is important for when using a capsule.
			if(interpolatedNormal.sqrMagnitude < .001f) return normal;

			if(!ExtVector3.IsInDirection(interpolatedNormal, normal))
			{
				interpolatedNormal = -interpolatedNormal;
			}

			interpolatedNormal.Normalize();

			//We check for an angle greater than 3 as a safety since our closestPointOnSurface might have been detected slightly inaccurately,
			//which might lead to sliding when depenetrating (such as on the floor when standing still)
			//This is a custom Vector3.Angle method that assumes the vectors are already normalized for performance reasons.
			if(ExtVector3.Angle(interpolatedNormal, normal) > 3f)
			{
				isOnEdge = true;
				return interpolatedNormal;
			}

			return normal;
		}

		public Vector3 GetCollisionVelocity()
		{
			return GetCollisionVelocity(closestPointOnSurface, detectionOrigin);
		}
		public static Vector3 GetCollisionVelocity(Vector3 closestPointOnSurface, Vector3 detectionOrigin)
		{
			return detectionOrigin - closestPointOnSurface;
		}

		public float GetCollisionMagnitude()
		{
			return GetCollisionMagnitude(GetCollisionVelocity(closestPointOnSurface, detectionOrigin), interpolatedNormal, sphereRadius);
		}
		public static float GetCollisionMagnitude(Vector3 collisionVelocity, Vector3 normal, float radius)
		{
			float mag = (ExtVector3.IsInDirection(collisionVelocity, normal)) ? -collisionVelocity.magnitude : collisionVelocity.magnitude;
			mag = Mathf.Clamp(mag, -radius, radius); //We clamp by radius in case the original detection radius was larger than this radius
			return radius + mag;
		}
		//We can avoid using .magnitude as long as collisionVelocity is the same direction as the normal (and the normal is already normalized)
		public static float GetCollisionMagnitudeInDirection(Vector3 collisionVelocity, Vector3 normal, float radius)
		{
			float mag = -ExtVector3.MagnitudeInDirection(collisionVelocity, normal, false);
			mag = Mathf.Clamp(mag, -radius, radius);
			return radius + mag;
		}
		public float GetCollisionMagnitudeSqr()
		{
			Vector3 collisionVelocity = GetCollisionVelocity(closestPointOnSurface, detectionOrigin);
			float mag = (ExtVector3.IsInDirection(collisionVelocity, normal)) ? -collisionVelocity.sqrMagnitude : collisionVelocity.sqrMagnitude;
			return sphereRadius.Squared() + mag;
		}

		public Vector3 GetDepenetrationVelocity()
		{
			return GetDepenetrationVelocity(interpolatedNormal, GetCollisionMagnitude()); 
		}
		public static Vector3 GetDepenetrationVelocity(Vector3 normal, float collisionMagnitude)
		{
			return normal * collisionMagnitude;
		}

		public class SphereCollisionComparerAscend : IComparer<SphereCollisionInfo>
		{
			public static SphereCollisionComparerAscend defaultComparer = new SphereCollisionComparerAscend();

			public int Compare(SphereCollisionInfo x, SphereCollisionInfo y)
			{
				float xToYDot = Vector3.Dot(x.interpolatedNormal, y.closestPointOnSurface - x.closestPointOnSurface);
				float yToXDot = Vector3.Dot(y.interpolatedNormal, x.closestPointOnSurface - y.closestPointOnSurface);

				//If on same plane
				if(!ExtMathf.Approximately(xToYDot, 0f, .001f) && ExtMathf.Approximately(yToXDot, 0f, .001f))
				{
					if(xToYDot < 0f) return -1; //y is behind x's plane
					if(yToXDot < 0f) return 1; //x is behind y's plane
				}

				//We choose the sphere with the highest depenetration
				return y.GetCollisionMagnitudeSqr().CompareTo(x.GetCollisionMagnitudeSqr());
			}
		}

		public class SphereCollisionComparerDescend : IComparer<SphereCollisionInfo>
		{
			public static SphereCollisionComparerDescend defaultComparer = new SphereCollisionComparerDescend();

			public int Compare(SphereCollisionInfo x, SphereCollisionInfo y)
			{
				return SphereCollisionComparerAscend.defaultComparer.Compare(y, x);
			}
		}
	} 
}