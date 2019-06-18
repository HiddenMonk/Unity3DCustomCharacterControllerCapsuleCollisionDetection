using System;
using UnityEngine;
using System.Collections.Generic;

namespace CapsuleCharacterCollisionDetection
{
	public class TriangleAABBTree
	{
		public Node rootNode;

		int maxTrianglesPerNode;

		int triangleCount;
		//int vertexCount;
		Vector3[] vertices;
		int[] tris;
		Vector3[] triangleNormals;

		public TriangleAABBTree(Mesh mesh, int maxTrianglesPerNode = 3, bool generateConvex = false)
		{
			this.maxTrianglesPerNode = maxTrianglesPerNode;

			if(generateConvex)
			{
				List<Vector3> hullVerts = new List<Vector3>();
				List<int> hullTris = new List<int>();
				List<Vector3> hullNormals = new List<Vector3>();
				ConvexHull.GenerateHull(mesh.vertices, true, ref hullVerts, ref hullTris, ref hullNormals);
				tris = hullTris.ToArray();
				vertices = hullVerts.ToArray();
			}else{
				tris = mesh.triangles;
				vertices = mesh.vertices;
			}

			//vertexCount = mesh.vertices.Length;
			triangleCount = mesh.triangles.Length / 3;

			triangleNormals = new Vector3[triangleCount];

			for(int i = 0; i < tris.Length; i += 3)
			{
				triangleNormals[i / 3] = CalculateTriangleNormal(i);
			}

			BuildTriangleTree();
		}

		void BuildTriangleTree()
		{
			List<int> rootTriangles = new List<int>();

			for (int i = 0; i < tris.Length; i += 3)
			{
				rootTriangles.Add(i);
			}

			rootNode = new Node();

			RecursivePartition(rootTriangles, 0, rootNode);
		}

		void RecursivePartition(List<int> triangles, int depth, Node parent)
		{
			TrianglesExtents extents = GetTrianglesExtents(triangles);
			Vector3 extentsMagnitude = extents.HalfExtent();
			Vector3 partitionNormal = Vector3.zero;

			if(extentsMagnitude.x >= extentsMagnitude.y && extentsMagnitude.x >= extentsMagnitude.z)
				partitionNormal = Vector3.right;
			else if(extentsMagnitude.y >= extentsMagnitude.x && extentsMagnitude.y >= extentsMagnitude.z)
				partitionNormal = Vector3.up;
			else
				partitionNormal = Vector3.forward;

			List<int> positiveTriangles;
			List<int> negativeTriangles;
			Split(triangles, extents.trianglesCenter, partitionNormal, out positiveTriangles, out negativeTriangles);

			parent.Set(extents);
			parent.positiveChild = new Node();
			parent.negativeChild = new Node();

			if(positiveTriangles.Count < triangles.Count && positiveTriangles.Count > maxTrianglesPerNode)
			{
				RecursivePartition(positiveTriangles, depth + 1, parent.positiveChild);
			}else{
				parent.positiveChild.triangles = positiveTriangles.ToArray();
				parent.positiveChild.Set(GetTrianglesExtents(positiveTriangles));
			}

			if(negativeTriangles.Count < triangles.Count && negativeTriangles.Count > maxTrianglesPerNode)
			{
				RecursivePartition(negativeTriangles, depth + 1, parent.negativeChild);
			}else{
				parent.negativeChild.triangles = negativeTriangles.ToArray();
				parent.negativeChild.Set(GetTrianglesExtents(negativeTriangles));
			}

		}

		void Split(List<int> triangles, Vector3 partitionPoint, Vector3 partitionNormal, out List<int> positiveTriangles, out List<int> negativeTriangles)
		{
			positiveTriangles = new List<int>();
			negativeTriangles = new List<int>();

			for(int i = 0; i < triangles.Count; i++)
			{
				int triangle = triangles[i];

				Vector3 triangleCenter = (vertices[tris[triangle]] + vertices[tris[triangle + 1]] + vertices[tris[triangle + 2]]) / 3f;
				bool pointAbove = MPlane.PointAbovePlane(partitionPoint, partitionNormal, triangleCenter);

				if(pointAbove)
				{
					positiveTriangles.Add(triangle);
				}else{
					negativeTriangles.Add(triangle);
				}
			}
		}

		public void FindClosestTriangles(Node node, AABB aabb, HashSet<int> triangles, InfoDebug infoDebug = null)
		{
			if(node.triangles == null)
			{
				if(AABB.AABBOverlapsAABB(node.aabb, aabb))
				{
					FindClosestTriangles(node.positiveChild, aabb, triangles, infoDebug);
					FindClosestTriangles(node.negativeChild, aabb, triangles, infoDebug);
				}
			}
			else
			{
				if(AABB.AABBOverlapsAABB(node.aabb, aabb))
				{
					#region Debug
#if UNITY_EDITOR
					if(infoDebug != null && (infoDebug.drawAABBOnContact || infoDebug.drawTrianglesOnContact) && CanDraw(infoDebug))
					{
						if(infoDebug.drawAABBOnContact) DrawAABBOnContact(node, infoDebug.transform);
						if(infoDebug.drawTrianglesOnContact) DrawTrianglesOnContact(node, infoDebug.transform);
					}
#endif
					#endregion

					//I originally used a hashset as a way to avoid duplicate triangles, however with new changes to
					//the tree there should be no chance of duplicate triangles, so a hashset might not be needed anymore.
					for(int i = 0; i < node.triangles.Length; i++)
					{
						triangles.Add(node.triangles[i]);
					}
				}
			}
		}

		public Vector3 GetTriangleNormal(int triangleIndex)
		{
			return triangleNormals[triangleIndex / 3];
		}

		public void GetTriangleVertices(int triangleIndex, out Vector3 p1, out Vector3 p2, out Vector3 p3)
		{
			p1 = vertices[tris[triangleIndex]];
			p2 = vertices[tris[triangleIndex + 1]];
			p3 = vertices[tris[triangleIndex + 2]];
		}

		Vector3 CalculateTriangleNormal(int triangleIndex)
		{
			if(triangleIndex < 0) return Vector3.zero;
			return TriangleNormal(vertices[tris[triangleIndex]], vertices[tris[triangleIndex + 1]], vertices[tris[triangleIndex + 2]]);
		}

		public static Vector3 TriangleNormal(Vector3 p1, Vector3 p2, Vector3 p3)
		{
			return Vector3.Cross(p2 - p1, p3 - p1).normalized;
		}

		TrianglesExtents GetTrianglesExtents(IList<int> triangles)
		{
			TrianglesExtents extents = TrianglesExtents.max;

			for(int i = 0; i < triangles.Count; i++)
			{
				int triangle = triangles[i];

				extents.trianglesCenter += (vertices[tris[triangle]] + vertices[tris[triangle + 1]] + vertices[tris[triangle + 2]]);

				extents.minExtent.x = Mathf.Min(extents.minExtent.x, vertices[tris[triangle]].x, vertices[tris[triangle + 1]].x, vertices[tris[triangle + 2]].x);
				extents.minExtent.y = Mathf.Min(extents.minExtent.y, vertices[tris[triangle]].y, vertices[tris[triangle + 1]].y, vertices[tris[triangle + 2]].y);
				extents.minExtent.z = Mathf.Min(extents.minExtent.z, vertices[tris[triangle]].z, vertices[tris[triangle + 1]].z, vertices[tris[triangle + 2]].z);
				
				extents.maxExtent.x = Mathf.Max(extents.maxExtent.x, vertices[tris[triangle]].x, vertices[tris[triangle + 1]].x, vertices[tris[triangle + 2]].x);
				extents.maxExtent.y = Mathf.Max(extents.maxExtent.y, vertices[tris[triangle]].y, vertices[tris[triangle + 1]].y, vertices[tris[triangle + 2]].y);
				extents.maxExtent.z = Mathf.Max(extents.maxExtent.z, vertices[tris[triangle]].z, vertices[tris[triangle + 1]].z, vertices[tris[triangle + 2]].z);
			}

			extents.trianglesCenter /= (triangles.Count * 3f);

			return extents;
		}

		public class Node
		{
			public AABB aabb;

			public Node positiveChild;
			public Node negativeChild;

			public int[] triangles;

			public void Set(TrianglesExtents extents) {Set(extents.Center(), extents.HalfExtent());}
			public void Set(Vector3 origin, Vector3 halfExtent)
			{
				this.aabb = new AABB(origin, halfExtent);
			}
		}

		public struct TrianglesExtents
		{
			public static TrianglesExtents max = new TrianglesExtents(new Vector3(float.MaxValue, float.MaxValue, float.MaxValue), new Vector3(float.MinValue, float.MinValue, float.MinValue), Vector3.zero);

			public Vector3 minExtent;
			public Vector3 maxExtent;
			public Vector3 trianglesCenter;

			public TrianglesExtents(Vector3 min, Vector3 max, Vector3 trianglesCenter)
			{
				minExtent = min;
				maxExtent = max;
				this.trianglesCenter = trianglesCenter;
			}

			public Vector3 HalfExtent()
			{
				return ExtVector3.Abs((maxExtent - minExtent) * .5f);
			}

			public Vector3 Center()
			{
				return (minExtent + maxExtent) * .5f;
			}
		}

				#region Debug
#if UNITY_EDITOR
		public void DrawDebugs(InfoDebug infoDebug)
		{
			if(infoDebug.drawAABBTree || infoDebug.drawTriangleTree)
			{
				if(CanDraw(infoDebug))
				{
					if(infoDebug.drawAABBTree) TraverseNodes(rootNode, DrawAABBTree, DrawAABBTree, infoDebug.transform);
					if(infoDebug.drawTriangleTree) TraverseNodes(rootNode, null, DrawTriangleTree, infoDebug.transform);
				}
			}
		}

		void DrawAABBTree(Node node, Transform transform)
		{
			//We add a small offset to make it easier to see overlapping AABBs.
			ExtDebug.DrawTransformedBox(node.aabb.origin + (Vector3.one * UnityEngine.Random.Range(0f, .001f)), node.aabb.halfExtents * 2f, transform.localToWorldMatrix, ExtDebug.RandomColor());
		}

		void DrawTriangleTree(Node node, Transform transform)
		{
			DrawTriangleSet(node.triangles, ExtDebug.RandomColor(), transform);
		}

		void TraverseNodes(Node node, Action<Node, Transform> ifNotEnd, Action<Node, Transform> ifEnd, Transform transform)
		{
			if(node.triangles == null)
			{
				if(ifNotEnd != null) ifNotEnd(node, transform);

				TraverseNodes(node.positiveChild, ifNotEnd, ifEnd, transform);
				TraverseNodes(node.negativeChild, ifNotEnd, ifEnd, transform);
			}else{
				if(ifEnd != null) ifEnd(node, transform);
			}
		}

		void DrawAABBOnContact(Node node, Transform transform)
		{
			ExtDebug.DrawTransformedBox(node.aabb.origin, node.aabb.halfExtents * 2f, transform.localToWorldMatrix, ExtDebug.RandomColor());
		}

		void DrawTrianglesOnContact(Node node, Transform transform)
		{
			DrawTriangleSet(node.triangles, ExtDebug.RandomColor(), transform);
		}

		void DrawTriangleSet(IList<int> triangles, Color color, Transform transform)
		{
			for(int i = 0; i < triangles.Count; i++)
			{
				int triangle = triangles[i];
				ExtDebug.DrawTriangle(vertices[tris[triangle]], vertices[tris[triangle + 1]], vertices[tris[triangle + 2]], color, transform);
			}
		}

		int gaveWarningOnFrame = -1; //Just to avoid lag when debugging.
		bool CanDraw(InfoDebug infoDebug)
		{
			if(triangleCount <= InfoDebug.drawSafetyMaxTriangleCount || infoDebug.overrideDrawSafety)
			{
				return true;
			}
			else if(gaveWarningOnFrame != Time.frameCount)
			{
				Debug.LogWarning("The mesh of GameObject " + infoDebug.transform.name + " has " + triangleCount + " triangles. Drawing the mesh tree might cause lots of lag. Enable the overrideDrawSafety to draw anyways.", infoDebug.transform.gameObject);
				gaveWarningOnFrame = Time.frameCount;
			}

			return false;
		}

#endif
		#endregion

		[Serializable]
		public class InfoDebug
		{
			public bool drawAABBOnContact;
			public bool drawTrianglesOnContact;
			public bool drawAABBTree;
			public bool drawTriangleTree;
			public bool overrideDrawSafety; //Drawing the trees could cause a ton of lag, so I put a warning and dont draw if your mesh is very large and this will override it.
			public static int drawSafetyMaxTriangleCount = 15000;
			[HideInInspector] public Transform transform;
		}
	}
}
