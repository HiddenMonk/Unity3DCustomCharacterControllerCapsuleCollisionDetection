using System;
using UnityEngine;
using System.Collections.Generic;

namespace CapsuleCharacterCollisionDetection
{
	public struct MPlane
	{
		public Vector3 normal {get; private set;}
		public Vector3 origin {get; private set;}

		public MPlane(Vector3 normal, Vector3 point, bool normalizeParameters = true)
		{
			if(normalizeParameters) normal.Normalize();

			this.normal = normal;
			this.origin = point;
		}

		public static float PlaneDistance(Vector3 planePoint, Vector3 planeNormal)
		{
			return -Vector3.Dot(planeNormal, planePoint);
		}

		public static float GetDistanceToPoint(Vector3 planePoint, Vector3 planeNormal, Vector3 point)
		{
			return Vector3.Dot(point - planePoint, planeNormal);
		}

		public float GetDistanceToPoint(Vector3 point)
		{
			return GetDistanceToPoint(origin, normal, point);
		}
		
		public static bool PointAbovePlane(Vector3 planePoint, Vector3 planeNormal, Vector3 point)
		{
			return GetDistanceToPoint(planePoint, planeNormal, point) > 0f;
		}

		public bool PointAbovePlane(Vector3 point)
		{
			return PointAbovePlane(origin, normal, point);
		}

		public static bool IsOnPlane(Vector3 planePoint, Vector3 planeNormal, Vector3 point, float offset = 0f)
		{
			return Mathf.Abs(GetDistanceToPoint(planePoint, planeNormal, point)) <= offset;
		}

		public bool IsOnPlane(Vector3 point, float offset = 0f)
		{
			return IsOnPlane(origin, normal, point, offset);
		}

		public bool IsBehindPlane(Vector3 point, float offset = 0f)
		{
			return !PointAbovePlane(point + (normal * offset));
		}

		public static bool IsBehindPlanes(Vector3 point, List<MPlane> planes, float offset = 0f)
		{
			for(int i = 0; i < planes.Count; i++)
			{
				if(planes[i].IsBehindPlane(point, offset)) return true;
			}
			return false;
		}
		
		public SweepInfo LinePlaneIntersect(Vector3 origin, Vector3 direction, bool normalize = true)
		{
			if(normalize) direction.Normalize();

			SweepInfo sweep = new SweepInfo();
			sweep.distance = Geometry.LinePlaneDistance(origin, direction, origin, normal);
			
			if(sweep.distance != 0f)
			{
				sweep.hasHit = true;
				sweep.intersectPoint = origin + (direction * sweep.distance);
				sweep.intersectCenter = sweep.intersectPoint;
			}

			return sweep;
		}
	}
}