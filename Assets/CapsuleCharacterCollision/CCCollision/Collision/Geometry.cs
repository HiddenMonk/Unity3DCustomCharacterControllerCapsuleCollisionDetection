using System;
using UnityEngine;

namespace CapsuleCharacterCollisionDetection
{
	public static class Geometry
	{
		//Returns 2 points since on line 1 there will be a closest point to line 2, and on line 2 there will be a closest point to line 1.
		public static IntersectPoints ClosestPointsOnTwoLines(Vector3 point1, Vector3 point1Direction, Vector3 point2, Vector3 point2Direction)
		{
			IntersectPoints intersections = new IntersectPoints();
			
			//While not normalizing can still give "correct" results, if the lines are parallel the closest points could be randomly chosen very far away,
			//so far away that we can start to run into float point precision erros, so we normalize the directions.
			point1Direction.Normalize();
			point2Direction.Normalize();

			float a = Vector3.Dot(point1Direction, point1Direction);
			float b = Vector3.Dot(point1Direction, point2Direction);
			float e = Vector3.Dot(point2Direction, point2Direction);
 
			float d = a*e - b*b;
 
			if(d != 0f)
			{
				Vector3 r = point1 - point2;
				float c = Vector3.Dot(point1Direction, r);
				float f = Vector3.Dot(point2Direction, r);
 
				float s = (b*f - c*e) / d;
				float t = (a*f - c*b) / d;
 
				intersections.first = point1 + point1Direction * s;
				intersections.second = point2 + point2Direction * t;
			}else{
				//Lines are parallel, select any points next to eachother
				intersections.first = point1;
				intersections.second = point2 + Vector3.Project(point1 - point2, point2Direction);
			}

			return intersections;
		}

		public static IntersectPoints ClosestPointsOnSegmentToLine(Vector3 segment0, Vector3 segment1, Vector3 linePoint, Vector3 lineDirection)
		{
			IntersectPoints closests = ClosestPointsOnTwoLines(segment0, segment1 - segment0, linePoint, lineDirection);
			closests.first = ClampToSegment(closests.first, segment0, segment1);

			return closests;
		}

		public static IntersectPoints ClosestPointsOnTwoLineSegments(Vector3 segment1Point1, Vector3 segment1Point2, Vector3 segment2Point1, Vector3 segment2Point2)
		{
			Vector3 line1Direction = segment1Point2 - segment1Point1;
			Vector3 line2Direction = segment2Point2 - segment2Point1;

			IntersectPoints closests = ClosestPointsOnTwoLines(segment1Point1, line1Direction, segment2Point1, line2Direction);
			IntersectPoints clampedClosests = closests;
			clampedClosests.first = ClampToSegment(clampedClosests.first, segment1Point1, segment1Point2);
			clampedClosests.second = ClampToSegment(clampedClosests.second, segment2Point1, segment2Point2);

			//Since this is a line segment, we need to decide which line we want to clamp both closest points to. So we choose the one that is farthest from its supposed closest point.
			if((closests.first - clampedClosests.first).sqrMagnitude > (closests.second - clampedClosests.second).sqrMagnitude)
			{
				clampedClosests.second = SegmentTargetAlignToPoint(clampedClosests.first, segment2Point1, segment2Point2);
			}else{
				clampedClosests.first = SegmentTargetAlignToPoint(clampedClosests.second, segment1Point1, segment1Point2);
			}

			return clampedClosests;
		}

		public static Vector3 SegmentTargetAlignToPoint(Vector3 point, Vector3 segmentPoint1, Vector3 segmentPoint2)
		{
			//We align the second point with the first.
			Vector3 aligned = segmentPoint1 + Vector3.Project(point - segmentPoint1, segmentPoint2 - segmentPoint1);
			aligned = ClampToSegment(aligned, segmentPoint1, segmentPoint2);

			return aligned;
		}

		//Assumes the point is already on the line somewhere
		public static Vector3 ClampToSegment(Vector3 point, Vector3 linePoint1, Vector3 linePoint2)
		{
			Vector3 lineDirection = linePoint2 - linePoint1;

			if(!ExtVector3.IsInDirection(point - linePoint1, lineDirection))
			{
				point = linePoint1;
			}
			else if(ExtVector3.IsInDirection(point - linePoint2, lineDirection))
			{
				point = linePoint2;
			}

			return point;
		}

		public static Vector3 ClosestPointOnLineSegmentToPoint(Vector3 point, Vector3 linePoint1, Vector3 linePoint2)
		{
			return ClampToSegment(ClosestPointOnLineToPoint(point, linePoint1, linePoint2 - linePoint1), linePoint1, linePoint2);
		}

		public static Vector3 ClosestPointOnLineToPoint(Vector3 point, Vector3 linePoint1, Vector3 lineDirection)
		{
			return linePoint1 + Vector3.Project(point - linePoint1, lineDirection);
		}

		public static float LinePlaneDistance(Vector3 linePoint, Vector3 lineVec, Vector3 planePoint, Vector3 planeNormal)
		{
			//calculate the distance between the linePoint and the line-plane intersection point
			float dotNumerator = Vector3.Dot((planePoint - linePoint), planeNormal);
			float dotDenominator = Vector3.Dot(lineVec, planeNormal);

			//line and plane are not parallel
			if(dotDenominator != 0f)
			{
				return dotNumerator / dotDenominator;
			}

			return 0;
		}

		//Note that the line is infinite, this is not a line-segment plane intersect
		public static Vector3 LinePlaneIntersect(Vector3 linePoint, Vector3 lineVec, Vector3 planePoint, Vector3 planeNormal)
		{
			float distance = LinePlaneDistance(linePoint, lineVec, planePoint, planeNormal);

			//line and plane are not parallel
			if(distance != 0f)
			{
				return linePoint + (lineVec * distance);	
			}

			return Vector3.zero;
		}

		public static IntersectPoints ClosestPointOnTriangleToLine(Vector3 segment0, Vector3 segment1, Vector3 vertex1, Vector3 vertex2, Vector3 vertex3, bool treatAsLineSegment = false)
		{
			return ClosestPointOnTriangleToLine(segment0, segment1, vertex1, vertex2, vertex3, Vector3.zero, treatAsLineSegment, false);
		}
		//public static IntersectPoints ClosestPointOnRectangleToLine(Vector3 segment0, Vector3 segment1, Vector3 vertex1, Vector3 vertex2, Vector3 vertex3, Vector3 vertex4, bool treatAsLineSegment = false)
		//{
		//	return ClosestPointOnTriangleToLine(segment0, segment1, vertex1, vertex2, vertex3, vertex4, treatAsLineSegment, true);
		//}
		public static IntersectPoints ClosestPointOnRectangleToLine(Vector3 segment0, Vector3 segment1, Rect3D rectangle, bool treatAsLineSegment = false)
		{
			return ClosestPointOnTriangleToLine(segment0, segment1, rectangle.bottomLeft, rectangle.topLeft, rectangle.topRight, rectangle.bottomRight, treatAsLineSegment, true);
		}
		//When isRectangle is true, the vertices must be on the same plane.
		static IntersectPoints ClosestPointOnTriangleToLine(Vector3 segment0, Vector3 segment1, Vector3 vertex1, Vector3 vertex2, Vector3 vertex3, Vector3 vertex4, bool treatAsLineSegment = false, bool isRectangle = false)
		{
			Vector3 ab = vertex2 - vertex1;
			Vector3 ac = vertex3 - vertex1;
			Vector3 normal = Vector3.Cross(ab, ac);

			float s0PlaneDistance = Vector3.Dot(segment0 - vertex1, normal);
			float s1PlaneDistance = Vector3.Dot(segment1 - vertex1, normal);

			IntersectPoints closestPoints = new IntersectPoints();
			
			//If we want to treat it as a line segment then we will need to check its distance, but if the line is plane are parallel (s0PlaneDistance != s1PlaneDistance) then we will treat as line segment anyways since we need a point reference
			if(s0PlaneDistance != s1PlaneDistance && (!treatAsLineSegment || (s0PlaneDistance * s1PlaneDistance) < 0f))
			{
				closestPoints.first = segment0 + (segment1 - segment0) * (-s0PlaneDistance / (s1PlaneDistance - s0PlaneDistance));
				closestPoints.second = closestPoints.first;
			}else{
				//We get the closest segment and calculate its closest distance to the plane
				closestPoints.first = (Mathf.Abs(s0PlaneDistance) < Mathf.Abs(s1PlaneDistance)) ? segment0 : segment1;
				closestPoints.second = closestPoints.first + (normal * LinePlaneDistance(closestPoints.first, normal, vertex1, normal));
			}

			//Make sure plane intersection is within triangle bounds
			float a = Vector3.Dot(Vector3.Cross(normal, vertex2 - vertex1), closestPoints.second - vertex1);
			float b = Vector3.Dot(Vector3.Cross(normal, vertex3 - vertex2), closestPoints.second - vertex2);
			float c = float.MaxValue;
			float d = float.MaxValue;
			if(!isRectangle)
			{
				c = Vector3.Dot(Vector3.Cross(normal, vertex1 - vertex3), closestPoints.second - vertex3);
			}else{
				c = Vector3.Dot(Vector3.Cross(normal, vertex4 - vertex3), closestPoints.second - vertex3);
				d = Vector3.Dot(Vector3.Cross(normal, vertex1 - vertex4), closestPoints.second - vertex4);
			}

			if(a < 0f || b < 0f || c < 0f || d < 0f)
			{
				//We are not within the triangle, we are on an edge so find the closest
				if(a < b && a < c && a < d)
				{
					return ClosestPointsOnTwoLineSegments(segment0, segment1, vertex1, vertex2);
				}
				else if(b < a && b < c && b < d)
				{
					return ClosestPointsOnTwoLineSegments(segment0, segment1, vertex2, vertex3);
				}
				else if((c < a && c < b && c < d) || !isRectangle)
				{
					if(!isRectangle)
					{
						return ClosestPointsOnTwoLineSegments(segment0, segment1, vertex3, vertex1);
					}else{
						return ClosestPointsOnTwoLineSegments(segment0, segment1, vertex3, vertex4);
					}
				}
				else if(isRectangle)
				{
					return ClosestPointsOnTwoLineSegments(segment0, segment1, vertex4, vertex1);
				}
			}

			return closestPoints;
		}

		//We use Soh from SohCahToa
		public static SweepInfo DepenetrateSphereFromPlaneInDirection(Vector3 spherePosition, float radius, Vector3 depenetrationDirection, Vector3 planePoint, Vector3 planeNormal, bool normalizeParameter = true)
		{
			if(normalizeParameter)
			{
				depenetrationDirection.Normalize();
				planeNormal.Normalize();
			}
 
			float distanceToPlane = LinePlaneDistance(spherePosition, -planeNormal, planePoint, planeNormal);
			if(Mathf.Abs(distanceToPlane) < radius)
			{
				float depenetrationDistance = radius - distanceToPlane;
				float angle = Mathf.Abs(90f - ExtVector3.Angle(depenetrationDirection, planeNormal));
				if(angle > 0)
				{
					SweepInfo sweep = new SweepInfo();
					sweep.hasHit = true;
					sweep.distance = depenetrationDistance / Mathf.Sin(angle * Mathf.Deg2Rad);
					sweep.intersectCenter = spherePosition + (depenetrationDirection * sweep.distance);
					sweep.intersectPoint = spherePosition - (planeNormal * distanceToPlane);
					return sweep;
				}
			}
 
			return new SweepInfo();
		}

		//Doing some SohCahToa to find where the sphere would safely sit within the 2 opposing normals.
		public static SweepInfo SpherePositionBetween2Planes(float radius, Vector3 plane1Point, Vector3 plane1Normal, Vector3 plane2Point, Vector3 plane2Normal, bool normalizeParameter = true)
		{
			if(normalizeParameter)
			{
				plane1Normal.Normalize();
				plane2Normal.Normalize();
			}
 
			Vector3 averageNormal = (plane1Normal + plane2Normal).normalized;
			Vector3 p1Projected = Vector3.ProjectOnPlane(averageNormal, plane1Normal);
			float angle = Mathf.Abs(ExtVector3.Angle(averageNormal, p1Projected.normalized));
			if(angle > 0)
			{
				Vector3 p2Projected = Vector3.ProjectOnPlane(averageNormal, plane2Normal);
				Vector3 intersectedPosition = Geometry.ClosestPointsOnTwoLines(plane1Point, p1Projected, plane2Point, p2Projected).first;
				
				SweepInfo sweep = new SweepInfo();
				sweep.hasHit = true;
				sweep.distance = radius / Mathf.Sin(angle * Mathf.Deg2Rad);
				sweep.intersectCenter = intersectedPosition + (averageNormal * sweep.distance);
				sweep.intersectPoint = intersectedPosition + (averageNormal * (sweep.distance - radius));
				return sweep;
			}
 
			return new SweepInfo();
		}

		#region ClosestPointOnTriangleToPoint taken from Iron-Warrior BSPTree
		/// <summary>
		/// Determines the closest point between a point and a triangle.
		/// Borrowed from RPGMesh class of the RPGController package for Unity, by fholm
		/// The code in this method is copyrighted by the SlimDX Group under the MIT license:
		/// 
		/// Copyright (c) 2007-2010 SlimDX Group
		/// 
		/// Permission is hereby granted, free of charge, to any person obtaining a copy
		/// of this software and associated documentation files (the "Software"), to deal
		/// in the Software without restriction, including without limitation the rights
		/// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
		/// copies of the Software, and to permit persons to whom the Software is
		/// furnished to do so, subject to the following conditions:
		/// 
		/// The above copyright notice and this permission notice shall be included in
		/// all copies or substantial portions of the Software.
		/// 
		/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
		/// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
		/// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
		/// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
		/// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
		/// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
		/// THE SOFTWARE.
		/// 
		/// </summary>
		/// <param name="point">The point to test.</param>
		/// <param name="vertex1">The first vertex to test.</param>
		/// <param name="vertex2">The second vertex to test.</param>
		/// <param name="vertex3">The third vertex to test.</param>
		/// <param name="result">When the method completes, contains the closest point between the two objects.</param>
		public static Vector3 ClosestPointOnTriangleToPoint(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3, Vector3 point)
		{
			//Source: Real-Time Collision Detection by Christer Ericson
			//Reference: Page 136

			//Check if P in vertex region outside A
			Vector3 ab = vertex2 - vertex1;
			Vector3 ac = vertex3 - vertex1;
			Vector3 ap = point - vertex1;

			float d1 = Vector3.Dot(ab, ap);
			float d2 = Vector3.Dot(ac, ap);
			if (d1 <= 0.0f && d2 <= 0.0f)
			{
				return vertex1; //Barycentric coordinates (1,0,0)
			}

			//Check if P in vertex region outside B
			Vector3 bp = point - vertex2;
			float d3 = Vector3.Dot(ab, bp);
			float d4 = Vector3.Dot(ac, bp);
			if (d3 >= 0.0f && d4 <= d3)
			{
				return vertex2; // barycentric coordinates (0,1,0)
			}

			//Check if P in edge region of AB, if so return projection of P onto AB
			float vc = d1 * d4 - d3 * d2;
			if (vc <= 0.0f && d1 >= 0.0f && d3 <= 0.0f)
			{
				float v = d1 / (d1 - d3);
				return vertex1 + v * ab; //Barycentric coordinates (1-v,v,0)
			}

			//Check if P in vertex region outside C
			Vector3 cp = point - vertex3;
			float d5 = Vector3.Dot(ab, cp);
			float d6 = Vector3.Dot(ac, cp);
			if (d6 >= 0.0f && d5 <= d6)
			{
				return vertex3; //Barycentric coordinates (0,0,1)
			}

			//Check if P in edge region of AC, if so return projection of P onto AC
			float vb = d5 * d2 - d1 * d6;
			if (vb <= 0.0f && d2 >= 0.0f && d6 <= 0.0f)
			{
				float w = d2 / (d2 - d6);
				return vertex1 + w * ac; //Barycentric coordinates (1-w,0,w)
			}

			//Check if P in edge region of BC, if so return projection of P onto BC
			float va = d3 * d6 - d5 * d4;
			if (va <= 0.0f && (d4 - d3) >= 0.0f && (d5 - d6) >= 0.0f)
			{
				float w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
				return vertex2 + w * (vertex3 - vertex2); //Barycentric coordinates (0,1-w,w)
			}

			//P inside face region. Compute Q through its barycentric coordinates (u,v,w)
			float denom = 1.0f / (va + vb + vc);
			float v2 = vb * denom;
			float w2 = vc * denom;
			return vertex1 + ab * v2 + ac * w2; //= u*vertex1 + v*vertex2 + w*vertex3, u = va * denom = 1.0f - v - w
		}
		#endregion
	}
}
