using System;
using UnityEngine;
using System.Collections.Generic;

namespace CapsuleCharacterCollisionDetection
{
	//Edited version of Iron-Warrior SuperCharacterController BSPTree
	
	[RequireComponent(typeof(MeshCollider))]
	public class MeshAABBTree : MonoBehaviour
	{
		//Instead of building a separate tree for every object with the same mesh, we build them once and reference them.
		//We can maybe take this one step further by creating and saving the trees during editing and store it to disk.
		public static Dictionary<Mesh, Dictionary<int, TriangleAABBTree[]>> trees = new Dictionary<Mesh, Dictionary<int, TriangleAABBTree[]>>();

		[Header("Max Triangles Per Node can be set only in edit mode, not while playing.")]
		public int maxTrianglesPerNode = 3;

		TriangleAABBTree tree;

		[SerializeField] TriangleAABBTree.InfoDebug infoDebug;

		void Awake()
		{
			infoDebug.transform = transform;

			MeshCollider meshCollider = GetComponent<MeshCollider>();
			Mesh mesh = meshCollider.sharedMesh;

			Dictionary<int, TriangleAABBTree[]> meshTree;
			if(!trees.TryGetValue(mesh, out meshTree))
			{
				meshTree = new Dictionary<int, TriangleAABBTree[]>();
				trees.Add(mesh, meshTree);
			}
			
			TriangleAABBTree[] aabbTrees;
			if(!meshTree.TryGetValue(maxTrianglesPerNode, out aabbTrees))
			{
				aabbTrees = new TriangleAABBTree[2];
				meshTree.Add(maxTrianglesPerNode, aabbTrees);
			}

			int index = meshCollider.convex ? 0 : 1;
			if(aabbTrees[index] == null)
			{
				aabbTrees[index] = new TriangleAABBTree(mesh, maxTrianglesPerNode, meshCollider.convex);
			}
			
			tree = aabbTrees[index];
		}

		#region Debug
#if UNITY_EDITOR
		void Update()
		{
			tree.DrawDebugs(infoDebug);
		}
#endif
		#endregion

//Problem - ClosestPoints methods only work with uniformly scaled objects.

		//-The ClosestPoints methods are very similar. Is there any way to cleanly combine them?
		//-We might not need to have a separate buffer list for each method, but I do it to be safe.

		HashSet<int> trianglesBufferMultiCaps = new HashSet<int>(IntComparerNoGarbage.defaultComparer);
		List<ClosestTrianglePoint> closestPointsBufferMultiCaps = new List<ClosestTrianglePoint>();
		public List<ContactInfo> ClosestPointsOnSurface(Vector3 segment0, Vector3 segment1, float radius, List<ContactInfo> resultsBuffer)
		{
			resultsBuffer.Clear();
			trianglesBufferMultiCaps.Clear();
			closestPointsBufferMultiCaps.Clear();

			CapsuleShape capsule = CapsuleShape.ToLocalOfUniformScale(new CapsuleShape(segment0, segment1, radius), transform);
			Vector3 localSegment0 = capsule.top;
			Vector3 localSegment1 = capsule.bottom;
			radius = capsule.radius;

			AABB aabb = AABB.CreateCapsuleAABB(localSegment0, localSegment1, radius + .001f); //We add a small radius increase due to float point precision issues that rarely happen.
			tree.FindClosestTriangles(tree.rootNode, aabb, trianglesBufferMultiCaps, infoDebug);

			Vector3 p1, p2, p3;
			IntersectPoints nearest;
			float radiusSqrd = radius * radius;
			float distance = 0;

			HashSet<int>.Enumerator enumerator = trianglesBufferMultiCaps.GetEnumerator();
			while(enumerator.MoveNext())
			{
				tree.GetTriangleVertices(enumerator.Current, out p1, out p2, out p3);

				nearest = Geometry.ClosestPointOnTriangleToLine(localSegment0, localSegment1, p1, p2, p3, true);
		
				distance = (nearest.second - nearest.first).sqrMagnitude;
				if(distance <= radiusSqrd)
				{
					closestPointsBufferMultiCaps.Add(new ClosestTrianglePoint(nearest.second, distance, enumerator.Current, nearest.first, this));
				}
			}

			CleanUp(closestPointsBufferMultiCaps, resultsBuffer);

			return resultsBuffer;
		}

		HashSet<int> trianglesBufferSingleCaps = new HashSet<int>(IntComparerNoGarbage.defaultComparer);
		public ContactInfo ClosestPointOnSurface(Vector3 segment0, Vector3 segment1, float radius)
		{
			trianglesBufferSingleCaps.Clear();

			CapsuleShape capsule = CapsuleShape.ToLocalOfUniformScale(new CapsuleShape(segment0, segment1, radius), transform);
			Vector3 localSegment0 = capsule.top;
			Vector3 localSegment1 = capsule.bottom;
			radius = capsule.radius;
		
			AABB aabb = AABB.CreateCapsuleAABB(localSegment0, localSegment1, radius + .001f);
			tree.FindClosestTriangles(tree.rootNode, aabb, trianglesBufferSingleCaps, infoDebug);

			Vector3 p1, p2, p3;
			IntersectPoints near;
			IntersectPoints nearest = new IntersectPoints();
			float radiusSqrd = radius * radius;
			float shortestDistance = float.MaxValue;
			int shortestTriangleIndex = -1;

			HashSet<int>.Enumerator enumerator = trianglesBufferSingleCaps.GetEnumerator();
			while(enumerator.MoveNext())
			{
				tree.GetTriangleVertices(enumerator.Current, out p1, out p2, out p3);
		
				near = Geometry.ClosestPointOnTriangleToLine(localSegment0, localSegment1, p1, p2, p3, true);
			
				float distance = (near.second - near.first).sqrMagnitude;
				if(PointIsBetter(distance, shortestDistance, nearest.first, nearest.second, near.first, near.second, shortestTriangleIndex, enumerator.Current, radiusSqrd))
				{
					shortestDistance = distance;
					nearest = near;
					shortestTriangleIndex = enumerator.Current;
				}
			}
			
			if(nearest.second == Vector3.zero) return new ContactInfo();
			return new ContactInfo(transform.TransformPoint(nearest.second), transform.TransformDirection(tree.GetTriangleNormal(shortestTriangleIndex)));
		}

		HashSet<int> trianglesBufferMultiSphere = new HashSet<int>(IntComparerNoGarbage.defaultComparer);
		List<ClosestTrianglePoint> closestPointsBufferMultiSphere = new List<ClosestTrianglePoint>();
		public List<ContactInfo> ClosestPointsOnSurface(Vector3 point, float radius, List<ContactInfo> resultsBuffer)
		{
			resultsBuffer.Clear();
			trianglesBufferMultiSphere.Clear();
			closestPointsBufferMultiSphere.Clear();

			Vector3 localPoint = transform.InverseTransformPoint(point);
			radius /= ExtVector3.Minimum(ExtVector3.Abs(transform.lossyScale));
			
			AABB aabb = AABB.CreateSphereAABB(localPoint, radius + .001f);
			tree.FindClosestTriangles(tree.rootNode, aabb, trianglesBufferMultiSphere, infoDebug);

			Vector3 p1, p2, p3, nearest;
			float radiusSqrd = radius * radius;
			float distance = 0;
			
			HashSet<int>.Enumerator enumerator = trianglesBufferMultiSphere.GetEnumerator();
			while(enumerator.MoveNext())
			{
				tree.GetTriangleVertices(enumerator.Current, out p1, out p2, out p3);

				nearest = Geometry.ClosestPointOnTriangleToPoint(p1, p2, p3, localPoint);

				distance = (nearest - localPoint).sqrMagnitude;
				if(distance <= radiusSqrd)
				{
					closestPointsBufferMultiSphere.Add(new ClosestTrianglePoint(nearest, distance, enumerator.Current, localPoint, this));
				}
			}
			
			CleanUp(closestPointsBufferMultiSphere, resultsBuffer);
			
			return resultsBuffer;
		}

		HashSet<int> trianglesBufferSingleSphere = new HashSet<int>(IntComparerNoGarbage.defaultComparer);
		public ContactInfo ClosestPointOnSurface(Vector3 point, float radius)
		{
			trianglesBufferSingleSphere.Clear();

			Vector3 localPoint = transform.InverseTransformPoint(point);
			radius /= ExtVector3.Minimum(ExtVector3.Abs(transform.lossyScale));

			AABB aabb = AABB.CreateSphereAABB(localPoint, radius + .001f);
			tree.FindClosestTriangles(tree.rootNode, aabb, trianglesBufferSingleSphere, infoDebug);

			Vector3 shortestPoint = Vector3.zero;
			Vector3 p1, p2, p3, nearest;
			float radiusSqrd = radius * radius;
			float shortestDistance = float.MaxValue;
			int shortestTriangleIndex = -1;

			HashSet<int>.Enumerator enumerator = trianglesBufferSingleSphere.GetEnumerator();
			while(enumerator.MoveNext())
			{
				tree.GetTriangleVertices(enumerator.Current, out p1, out p2, out p3);

				nearest = Geometry.ClosestPointOnTriangleToPoint(p1, p2, p3, localPoint);
			
				float distance = (localPoint - nearest).sqrMagnitude;
				if(PointIsBetter(distance, shortestDistance, localPoint, shortestPoint, localPoint, nearest, shortestTriangleIndex, enumerator.Current, radiusSqrd))
				{
					shortestDistance = distance;
					shortestPoint = nearest;
					shortestTriangleIndex = enumerator.Current;
				}
			}
			
			if(shortestPoint == Vector3.zero) return new ContactInfo();
			return new ContactInfo(transform.TransformPoint(shortestPoint), transform.TransformDirection(tree.GetTriangleNormal(shortestTriangleIndex)));
		}

		List<MPlane> ignoreBehindPlanes = new List<MPlane>();
		//This is very slow depending on how many contacts there are...
		void CleanUp(List<ClosestTrianglePoint> closestPoints, List<ContactInfo> resultsBuffer)
		{
			if(closestPoints.Count > 0)
			{
				ignoreBehindPlanes.Clear();

				//Taking advantage of C# built in QuickSort algorithm.
				closestPoints.Sort(ClosestTrianglePointComparerAscend.defaultComparer);
			
				//Note - If we are in a corner and our sphere origin is behind 1 wall while also large enough to touch the other wall, the first wall normal would block the second due to the .0001 offset below since the points lie on the same plane.
				//This is actually desired since the interpolatednormal of that point would have been going towards inside the other wall, which means we would be try to depenetrate into inside the wall...
	//Problem - I have noticed cases where this removes points we actually wanted, such as when we are on an edge of a single mesh touching its ceiling and floor. We would get the ceiling and floor normals as
				//well as the edges side normals. One of the side normals would end up removing 1 of the ceiling / floor normals, giving our depenetration method trouble and possibly getting us stuck.
				for(int i = 0; i < closestPoints.Count; i++)
				{
					ClosestTrianglePoint closestPoint = closestPoints[i];
					Vector3 planeNormal = tree.GetTriangleNormal(closestPoint.triangleIndex);
				
					if(!MPlane.IsBehindPlanes(closestPoint.position, ignoreBehindPlanes, -.0001f)) //thanks to the .0001 offset we avoid duplicates
					{
						resultsBuffer.Add(new ContactInfo(transform.TransformPoint(closestPoint.position), transform.TransformDirection(planeNormal)));
					}

					ignoreBehindPlanes.Add(new MPlane(planeNormal, closestPoint.position, false));
				}
			}
		}

		bool PointIsBetter(float distance, float shortestDistance, Vector3 shortestPointSphereOrigin, Vector3 shortestPoint, Vector3 currentPointSphereOrigin, Vector3 currentPoint, int shortestTriangleIndex, int currentTriangleIndex, float radiusSquared)
		{
			if(shortestTriangleIndex >= 0 && ExtMathf.Approximately(distance, shortestDistance))
			{
				if(CompareNormalTo(shortestPointSphereOrigin, shortestPoint, tree.GetTriangleNormal(shortestTriangleIndex), currentPointSphereOrigin, currentPoint, tree.GetTriangleNormal(currentTriangleIndex)))
				{
					return false;
				}
			}
			else if (distance > shortestDistance || distance > radiusSquared)
			{
				return false;
			}

			return true;
		}

		public struct ClosestTrianglePoint
		{
			public Vector3 position;
			public float distance;
			public int triangleIndex;
			public Vector3 sphereOrigin;
			public MeshAABBTree meshTree;

			public ClosestTrianglePoint(Vector3 position, float distance, int triangleIndex, Vector3 sphereOrigin, MeshAABBTree meshTree)
			{
				this.position = position;
				this.distance = distance;
				this.triangleIndex = triangleIndex;
				this.sphereOrigin = sphereOrigin;
				this.meshTree = meshTree;
			}
		}

		public class ClosestTrianglePointComparerAscend : IComparer<ClosestTrianglePoint>
		{
			public static ClosestTrianglePointComparerAscend defaultComparer = new ClosestTrianglePointComparerAscend();

			public int Compare(ClosestTrianglePoint x, ClosestTrianglePoint y)
			{
				//If two points are equal distance, we want to choose the one thats normal is facing the sphereOrigin the most.
				//It seems it is very important to only use epsilon to compare and not a low value like .0001. This seems to be because we are using 
				//the sqrMagnitude as the distance which makes the value very small so we start to get into floating point precision issues. 
				//Since our CompareNormalTo assumes the distances are the same to compare their angles, if they were slightly off then things will break.
				//A fix would be to normalize in CompareNormalTo, but that could get expensive..
				if(ExtMathf.Approximately(x.distance, y.distance))
				{
					return CompareNormalTo(x, y);
				}
				return x.distance.CompareTo(y.distance);
			}
		}

		public class ClosestTrianglePointComparerDescend : IComparer<ClosestTrianglePoint>
		{
			public static ClosestTrianglePointComparerDescend defaultComparer = new ClosestTrianglePointComparerDescend();

			public int Compare(ClosestTrianglePoint x, ClosestTrianglePoint y)
			{
				return ClosestTrianglePointComparerAscend.defaultComparer.Compare(y, x);
			}
		}

		public static int CompareNormalTo(ClosestTrianglePoint x, ClosestTrianglePoint y)
		{
			return CompareNormalTo(x.sphereOrigin, x.position, x.meshTree.tree.GetTriangleNormal(x.triangleIndex), y.sphereOrigin, y.position, y.meshTree.tree.GetTriangleNormal(y.triangleIndex)) ? -1 : 1;
		}

		//We test if the normal faces the sphereOrigin the most. This assumes both points distances to their sphereOrigin is approximately the same.
		public static bool CompareNormalTo(Vector3 point1SphereOrigin, Vector3 point1, Vector3 point1Normal, Vector3 point2SphereOrigin, Vector3 point2, Vector3 point2Normal)
		{
			return Vector3.Dot(point1SphereOrigin - point1, point1Normal) > Vector3.Dot(point2SphereOrigin - point2, point2Normal);
		}

		public class IntComparerNoGarbage : IEqualityComparer<int>
		{
			public static IntComparerNoGarbage defaultComparer = new IntComparerNoGarbage();
			const int minInt = -2147483647; //1 less than int minValue since that seems to also cause garbage? I think it might just be 0 and minInt though.

			public bool Equals(int x, int y)
			{
				return x == y;
			}

			public int GetHashCode(int value)
			{
				//Mono has a bug that hashsets doing a SlotsContainsAt with the number 0 hashcode (and others?) will cause garbage collection, so we avoid it like this.
				//Note - since we are not assiging the minInt to 0, minInt could still be added to the hashset, so dont ever add minInt to the hashset.
				if(value == 0) return minInt;

				return value;
			}
		}
	}
}
