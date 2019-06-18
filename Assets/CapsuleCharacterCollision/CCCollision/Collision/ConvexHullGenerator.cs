using System.Collections.Generic;
using UnityEngine;

namespace CapsuleCharacterCollisionDetection
{
	//Doesnt work with flat plane, but neither does unity. For flat planes unity seems to just set it to the meshes bounds.
	//For now we will just make sure to not use it for flat meshes.

	public static class ConvexHull
	{
		static ConvexHullGenerator generator = new ConvexHullGenerator();

		public static void GenerateHull(IList<Vector3> points, bool splitVerts, ref List<Vector3> vertsResults, ref List<int> trisResults, ref List<Vector3> normalsResults)
		{
			generator.GenerateHull(points, splitVerts, ref vertsResults, ref trisResults, ref normalsResults);
		}
	}

	//Taken from https://github.com/OskarSigvardsson/unity-quickhull/blob/master/Scripts/ConvexHullCalculator.cs
	//Did some changes, so some of the comments might not make sense.

	/// <summary>
	///   A not very optimized implementation of the quickhull algorithm
	///   for generating 3d convex hulls.
	///
	///   The algorithm works like this: you start with an initial
	///   "seed" hull, that is just a simple tetrahedron made up of
	///   four points in the point cloud. This seed hull is then grown
	///   until it all the points in the point cloud is inside of it,
	///   at which point it will be the convex hull for the entire
	///   set.
	///
	///   All of the points in the point cloud is divided into two
	///   parts, the "open set" and the "closed set". The open set
	///   consists of all the points outside of the tetrahedron, and
	///   the closed set is all of the points inside the tetrahedron.
	///   After each iteration of the algorithm, the closed set gets
	///   bigger and the open set get smaller. When the open set is
	///   empty, the algorithm is finished.
	///
	///   Each point in the open set is assigned to a face that it
	///   lies outside of. To grow the hull, the point in the open set
	///   which is farthest from it's face is chosen. All faces which
	///   are facing that point (I call them "lit faces" in the code,
	///   because if you imagine the point as a point light, it's the
	///   set of points which would be lit by that point light) are
	///   removed, and a "horizon" of edges is found from where the
	///   faces were removed. From this horizon, new faces are
	///   constructed in a "cone" like fashion connecting the point to
	///   the edges.
	///
	///   To keep track of the faces, I use a struct for each face
	///   which contains the three vertices of the face in CCW order,
	///   as well as the three triangles which share an edge. I was
	///   considering doing a half-edge structure to store the mesh,
	///   but it's not needed. Using a struct for each face and
	///   neighbors simplify the algorithm and makes it easy to
	///   export it as a mesh.
	///
	///   The most subtle part of the algorithm is finding the horizon.
	///   In order to properly construct the cone so that all
	///   neighbors are kept consistent, you can do a depth-first
	///   search from the first lit face. If the depth-first search
	///   always proceeeds in a counter-clockwise fashion, it
	///   guarantees that the horizon will be found in a
	///   counter-clockwise order, which makes it easy to construct
	///   the cone of new faces.
	///
	///   There are a number of things you could possibly optimize in
	///   this version of the algorithm, but this is correct and
	///   reasonably fast. Fast enough for my purposes, anyway.
	///
	///   A note: the code uses a right-handed coordinate system, where
	///   the cross-product uses the right-hand rule and the faces are
	///   in CCW order. At the end of the algorithm, the hull is
	///   exported in a Unity-friendly fashion, with a left-handed
	///   mesh.
	/// </summary>
	public class ConvexHullGenerator
	{
		/// <summary>
		///   Constant representing a point that has yet to be
		///   assigned to a face. It's only used immediately after
		///   constructing the seed hull.
		/// </summary>
		const int UNASSIGNED = -2;

		/// <summary>
		///   Constant representing a point that is inside the convex
		///   hull, and thus is behind all faces. In the openSet
		///   array, all points with INSIDE are at the end of the
		///   array, with indexes larger openSetTail.
		/// </summary>
		const int INSIDE = -1;

		/// <summary>
		///   Epsilon value.
		/// </summary>
		const float EPSILON = 0.0001f;

		/// <summary>
		///   Struct representing a single face.
		///
		///   Vertex0, Vertex1 and Vertex2 are the vertices in CCW
		///   order. They acutal points are stored in the points
		///   array, these are just indexes into that array.
		///
		///   Opposite0, Opposite1 and Opposite2 are the keys to the
		///   faces which share an edge with this face. Opposite0 is
		///   the face opposite Vertex0 (so it has an edge with
		///   Vertex2 and Vertex1), etc.
		///
		///   Normal is (unsurprisingly) the normal of the triangle.
		///
		///   TODO: SOA instead of AOS?
		/// </summary>
		struct Face
		{
			public int Vertex0;
			public int Vertex1;
			public int Vertex2;

			public int Opposite0;
			public int Opposite1;
			public int Opposite2;

			public Vector3 Normal;

			public Face(int v0, int v1, int v2, int o0, int o1, int o2, Vector3 normal)
			{
				Vertex0 = v0;
				Vertex1 = v1;
				Vertex2 = v2;
				Opposite0 = o0;
				Opposite1 = o1;
				Opposite2 = o2;
				Normal = normal;
			}

			public bool Equals(Face other)
			{
				return (this.Vertex0 == other.Vertex0)
					&& (this.Vertex1 == other.Vertex1)
					&& (this.Vertex2 == other.Vertex2)
					&& (this.Opposite0 == other.Opposite0)
					&& (this.Opposite1 == other.Opposite1)
					&& (this.Opposite2 == other.Opposite2)
					&& (this.Normal == other.Normal);
			}
		}

		/// <summary>
		///   Struct representing a mapping between a point and a
		///   face. These are used in the openSet array.
		///
		///   Point is the index of the point in the points array,
		///   Face is the key of the face in the Key dictioanry,
		///   Distance is the distance from the face to the point.
		/// </summary>
		struct PointFace
		{
			public int Point;
			public int Face;
			public float Distance;

			public PointFace(int p, int f, float d)
			{
				Point = p;
				Face = f;
				Distance = d;
			}
		}

		/// <summary>
		///   Struct representing a single edge in the horizon.
		///
		///   Edge0 and Edge1 are the vertexes of edge in CCW order,
		///   Face is the face on the other side of the horizon.
		///
		///   TODO Edge1 isn't actually needed, you can just index the
		///   next item in the horizon array.
		/// </summary>
		struct HorizonEdge
		{
			public int Face;
			public int Edge0;
			public int Edge1;
		}

		/// <summary>
		///   A dictionary storing the faces of the currently
		///   generated convex hull. The key is the id of the face,
		///   used in the Face, PointFace and HorizonEdge struct.
		///
		///   This is a Dictionary, because we need both random access
		///   to it, the ability to loop through it, and ability to
		///   quickly delete faces (in the ConstructCone method), and
		///   Dictionary is the obvious candidate that can do all of
		///   those things.
		///
		///   I'm wondering if using a Dictionary is best idea, though.
		///   It might be better to just have them in a List<Face> and
		///   mark a face as deleted by adding a field to the Face
		///   struct. The downside is that we would need an extra
		///   field in the Face struct, and when we're looping through
		///   the points in openSet, we would have to loop through all
		///   the Faces EVER created in the algorithm, and skip the
		///   ones that have been marked as deleted. However, looping
		///   through a list is fairly fast, and it might be worth it
		///   to avoid Dictionary overhead. I should probably test and
		///   profile both ways.
		///
		///   TODO test converting to a List<Face> instead.
		/// </summary>
		Dictionary<int, Face> faces;

		/// <summary>
		///   The set of points to be processed. "openSet" is a
		///   misleading name, because it's both the open set (points
		///   which are still outside the convex hull) and the closed
		///   set (points that are inside the convex hull). The first
		///   part of the array (with indexes <= openSetTail) is the
		///   openSet, the last part of the array (with indexes >
		///   openSetTail) are the closed set, with Face set to
		///   INSIDE. The closed set is largely irrelevant to the
		///   algorithm, the open set is what matters.
		///
		///   Storing the entire open set in one big list has a
		///   downside: when we're reassigning points after
		///   ConstructCone, we only need to reassign points that
		///   belong to the faces that have been removed, but storing
		///   it in one array, we have to loop through the entire
		///   list, and checking litFaces to determine which we can
		///   skip and which need to be reassigned.
		///
		///   The alternative here is to give each face in Face array
		///   it's own openSet. I don't like that solution, because
		///   then you have to juggle so many more heap-allocated
		///   List<T>'s, we'd have to use object pools and such. It
		///   would do a lot more allocation, and it would have worse
		///   locality. I should maybe test that solution, but it
		///   probably wont be faster enough (if at all) to justify
		///   the extra allocations.
		/// </summary>
		List<PointFace> openSet;

		/// <summary>
		///   Set of faces which are "lit" by the current point in the
		///   set. This is used in the FindHorizon() DFS search to
		///   keep track of which faces we've already visited, and in
		///   the ReassignPoints() method to know which points need to
		///   be reassigned.
		/// </summary>
		HashSet<int> litFaces;

		/// <summary>
		///   The current horizon. Generated by the FindHorizon() DFS
		///   search, and used in ConstructCone to construct new
		///   faces. The list of edges are in CCW order.
		/// </summary>
		List<HorizonEdge> horizon;

		/// <summary>
		///   If SplitVerts is false, this Dictionary is used to keep
		///   track of which points we've added to the final mesh.
		/// </summary>
		Dictionary<int, int> hullVerts;

		/// <summary>
		///   The "tail" of the openSet, the last index of a vertex
		///   that has been assigned to a face.
		/// </summary>
		int openSetTail = -1;

		/// <summary>
		///   When adding a new face to the faces Dictionary, use this
		///   for the key and then increment it.
		/// </summary>
		int faceCount = 0;

		/// <summary>
		///   Generate a convex hull from points in points array, and
		///   store the mesh in Unity-friendly format in verts and
		///   tris. If splitVerts is true, the the verts will be
		///   split, if false, the same vert will be used for more
		///   than one triangle.
		/// </summary>
		public void GenerateHull(IList<Vector3> points, bool splitVerts, ref List<Vector3> vertsResults, ref List<int> trisResults, ref List<Vector3> normalsResults)
		{
			Initialize(points, splitVerts);

			GenerateInitialHull(points);

			while (openSetTail >= 0)
			{
				GrowHull(points);
			}

			ExportMesh(points, splitVerts, ref vertsResults, ref trisResults, ref normalsResults);
			//VerifyMesh(points, ref verts, ref tris);
		}

		/// <summary>
		///   Make sure all the buffers and variables needed for the
		///   algorithm are initialized.
		/// </summary>
		void Initialize(IList<Vector3> points, bool splitVerts)
		{
			faceCount = 0;
			openSetTail = -1;

			if (faces == null)
			{
				faces = new Dictionary<int, Face>();
				litFaces = new HashSet<int>();
				horizon = new List<HorizonEdge>();
				openSet = new List<PointFace>(points.Count);
			}
			else
			{
				faces.Clear();
				litFaces.Clear();
				horizon.Clear();
				openSet.Clear();

				//if (openSet.Capacity < points.Count)
				//{
				//	// i wonder if this is a good idea... if you call
				//	// GenerateHull over and over with slightly
				//	// increasing points counts, it's going to
				//	// reallocate every time. Maybe i should just use
				//	// .Add(), and let the List<T> manage the
				//	// capacity, increasing it geometrically every
				//	// time we need to reallocate.

				//	// maybe do
				//	//   openSet.Capacity = Mathf.NextPowerOfTwo(points.Count)
				//	// instead?

				//	openSet.Capacity = points.Count;
				//}
			}

			if (!splitVerts)
			{
				if (hullVerts == null)
				{
					hullVerts = new Dictionary<int, int>();
				}
				else
				{
					hullVerts.Clear();
				}
			}
		}

		/// <summary>
		///   Create initial seed hull.
		///
		///   The good way to do this is probably to find the extreme
		///   points and create the seed hull from those, but I'm just
		///   using the four first points for it, for now. Obviously
		///   it can be optimized :)
		/// </summary>
		void GenerateInitialHull(IList<Vector3> points)
		{
			// TODO use extreme points to generate seed hull. I wonder
			// how much difference that actually makes, you would
			// imagine that even with a tiny seed hull, it would grow
			// pretty quickly. Anyway, the rest should be the same,
			// you only need to change how you find b0/b1/b2/b3
		//Seems to make a good difference on things like flat plane mesh.

			// TODO i'm a bit worried what happens if these points are
			// too close to each other or if the fourth point is
			// coplanar with the triangle. I should loop through the
			// point set to find suitable points instead.

			//var b0 = 0;
			//var b1 = 1;
			//var b2 = 2;
			//var b3 = 3;
			int[] initialSimplex = CreateInitialSimplex(points);
			var b0 = initialSimplex[0];
			var b1 = initialSimplex[1];
			var b2 = initialSimplex[2];
			var b3 = initialSimplex[3];

			//Debug.DrawRay(points[b0], Vector3.up, Color.white, 5f);
			//Debug.DrawRay(points[b1], Vector3.up * .75f, Color.red, 5f);
			//Debug.DrawRay(points[b2], Vector3.up * .65f, Color.blue, 5f);
			//Debug.DrawRay(points[b3], Vector3.up * .55f, Color.magenta, 5f);

			var v0 = points[b0];
			var v1 = points[b1];
			var v2 = points[b2];
			var v3 = points[b3];

			// TODO use epsilon?
			var above = Dot(v3 - v1, Cross(v1 - v0, v2 - v0)) > EPSILON;

			// Create the faces of the seed hull. You need to draw a
			// diagram here, otherwise it's impossible to know what's
			// going on :)

			// Basically: there are two different possible
			// start-tetrahedrons, depending on whether the fourth
			// point is above or below the base triangle. If you draw
			// a tetrahedron with these coordinates (in a right-handed
			// coordinate-system):

			//   b0 = (0,0,0)
			//   b1 = (1,0,0)
			//   b2 = (0,1,0)
			//   b3 = (0,0,1)

			// you can see the first case (set b3 = (0,0,-1) for the
			// second case). The faces are added with the proper
			// references to the faces opposite each vertex

			faceCount = 0;
			if (above)
			{
				faces[faceCount++] = new Face(b0, b2, b1, 3, 1, 2, Normal(points[b0], points[b2], points[b1]));
				faces[faceCount++] = new Face(b0, b1, b3, 3, 2, 0, Normal(points[b0], points[b1], points[b3]));
				faces[faceCount++] = new Face(b0, b3, b2, 3, 0, 1, Normal(points[b0], points[b3], points[b2]));
				faces[faceCount++] = new Face(b1, b2, b3, 2, 1, 0, Normal(points[b1], points[b2], points[b3]));
			}
			else
			{
				faces[faceCount++] = new Face(b0, b1, b2, 3, 2, 1, Normal(points[b0], points[b1], points[b2]));
				faces[faceCount++] = new Face(b0, b3, b1, 3, 0, 2, Normal(points[b0], points[b3], points[b1]));
				faces[faceCount++] = new Face(b0, b2, b3, 3, 1, 0, Normal(points[b0], points[b2], points[b3]));
				faces[faceCount++] = new Face(b1, b3, b2, 2, 0, 1, Normal(points[b1], points[b3], points[b2]));
			}

			//VerifyFaces(points);

			// Create the openSet. Add all points except the points of
			// the seed hull.
			for (int i = 0; i < points.Count; i++)
			{
				if (i == b0 || i == b1 || i == b2 || i == b3) continue;

				openSet.Add(new PointFace(i, UNASSIGNED, 0.0f));
			}

			// Add the seed hull verts to the tail of the list.
			openSet.Add(new PointFace(b0, INSIDE, float.NaN));
			openSet.Add(new PointFace(b1, INSIDE, float.NaN));
			openSet.Add(new PointFace(b2, INSIDE, float.NaN));
			openSet.Add(new PointFace(b3, INSIDE, float.NaN));

			// Set the openSetTail value. Last item in the array is
			// openSet.Count - 1, but four of the points (the verts of
			// the seed hull) are part of the closed set, so move
			// openSetTail to just before those.
			openSetTail = openSet.Count - 5;

			//Assert(openSet.Count == points.Count);

			// Assign all points of the open set. This does basically
			// the same thing as ReassignPoints()
			for (int i = 0; i <= openSetTail; i++)
			{
				//Assert(openSet[i].Face == UNASSIGNED);
				//Assert(openSet[openSetTail].Face == UNASSIGNED);
				//Assert(openSet[openSetTail + 1].Face == INSIDE);

				var assigned = false;
				var fp = openSet[i];

				//Assert(faces.Count == 4);
				//Assert(faces.Count == faceCount);
				for (int j = 0; j < 4; j++)
				{
					//Assert(faces.ContainsKey(j));

					var face = faces[j];

					var dist = PointFaceDistance(points[fp.Point], points[face.Vertex0], face);

					if (dist > 0)
					{
						fp.Face = j;
						fp.Distance = dist;
						openSet[i] = fp;

						assigned = true;
						break;
					}
				}

				if (!assigned)
				{
					// Point is inside
					fp.Face = INSIDE;
					fp.Distance = float.NaN;

					// Point is inside seed hull: swap point with
					// tail, and move openSetTail back. We also have
					// to decrement i, because there's a new item at
					// openSet[i], and we need to process it next iteration
					openSet[i] = openSet[openSetTail];
					openSet[openSetTail] = fp;

					openSetTail -= 1;
					i -= 1;
				}
			}

			//VerifyOpenSet(points);
		}

		//Taken from https://gist.github.com/YclepticStudios/c0d8cea56ee1e1d9714754ee9427085b
		//
		int[] extremePointsBuffer = new int[6];
		float[] extremePointValuesBuffer = new float[6];
		int[] FindExtremePoints(IList<Vector3> vertices)
		{
			for (int ii = 0; ii < extremePointsBuffer.Length - 1; ii += 2)
			{
				extremePointsBuffer[ii] = 0;
				extremePointsBuffer[ii + 1] = 0;
				extremePointValuesBuffer[ii] = float.PositiveInfinity;
				extremePointValuesBuffer[ii + 1] = float.NegativeInfinity;
			}
			// Search point cloud
			for (int ii = 0; ii < vertices.Count; ii++)
				for (int jj = 0; jj < extremePointsBuffer.Length - 1; jj += 2)
				{
					float val = vertices[ii][jj / 2];
					if (val < extremePointValuesBuffer[jj])
					{
						extremePointsBuffer[jj] = ii;
						extremePointValuesBuffer[jj] = val;
					}
					else if (val > extremePointValuesBuffer[jj + 1])
					{
						extremePointsBuffer[jj + 1] = ii;
						extremePointValuesBuffer[jj + 1] = val;
					}
				}
			return extremePointsBuffer;
		}

		int[] initialSimplexBuffer = new int[4];
		int[] CreateInitialSimplex(IList<Vector3> vertices)
		{
			return CreateInitialSimplex(FindExtremePoints(vertices), vertices);
		}
		int[] CreateInitialSimplex(int[] extremePoints, IList<Vector3> vertices)
		{
			// Find two most distent extreme points (base line of tetrahedron)
			float maxDistance = float.NegativeInfinity;
			for (int ii = 0; ii < extremePoints.Length; ii++)
				for (int jj = ii + 1; jj < extremePoints.Length; jj++)
				{
					float distance = (vertices[extremePoints[ii]] -
						vertices[extremePoints[jj]]).sqrMagnitude;
					if (distance > maxDistance)
					{
						initialSimplexBuffer[0] = extremePoints[ii];
						initialSimplexBuffer[1] = extremePoints[jj];
						maxDistance = distance;
					}
				}
			// Find the extreme point most distent from the line
			maxDistance = float.NegativeInfinity;
			Vector3 normal = vertices[initialSimplexBuffer[0]] -
				vertices[initialSimplexBuffer[1]];
			for (int ii = 0; ii < extremePoints.Length; ii++)
			{
				Vector3 v = vertices[extremePoints[ii]] -
					vertices[initialSimplexBuffer[0]];
				Vector3 rejection = Vector3.ProjectOnPlane(v, normal);
				float distance = rejection.sqrMagnitude;
				if (distance > maxDistance)
				{
					initialSimplexBuffer[2] = extremePoints[ii];
					maxDistance = distance;
				}
			}
			// Find the most distant of all the points from the plane of the
			// triangle formed from the first three "initialSimplex" points
			maxDistance = float.NegativeInfinity;
			Vector3 v1 = vertices[initialSimplexBuffer[1]] - vertices[initialSimplexBuffer[0]];
			Vector3 v2 = vertices[initialSimplexBuffer[2]] - vertices[initialSimplexBuffer[0]];
			normal = Vector3.Cross(v1, v2);
			for (int ii = 0; ii < vertices.Count; ii++)
			{
				Vector3 v = vertices[ii] - vertices[initialSimplexBuffer[0]];
				float distance = Mathf.Abs(Vector3.Dot(v, normal));
				if (distance > maxDistance)
				{
					initialSimplexBuffer[3] = ii;
					maxDistance = distance;
				}
			}
			// Swap the two first vertices if the final point is in front of the
			// triangular base plane (this makes all faces of the tetrahedron point)
			// outward
			Vector4 baseFace = Points2Plane(vertices[initialSimplexBuffer[0]],
				vertices[initialSimplexBuffer[1]],
				vertices[initialSimplexBuffer[2]]);
			if (PointAbovePlane(vertices[initialSimplexBuffer[3]], baseFace))
			{
				int t = initialSimplexBuffer[0];
				initialSimplexBuffer[0] = initialSimplexBuffer[1];
				initialSimplexBuffer[1] = t;
			}
			return initialSimplexBuffer;
		}

		Vector4 Points2Plane(Vector3 pt1, Vector3 pt2, Vector3 pt3)
		{
			Vector4 v = Vector3.Cross(pt2 - pt1, pt3 - pt1).normalized;
			v.w = -Vector3.Dot(v, pt1);
			return v;
		}
		bool PointAbovePlane(Vector3 point, Vector4 plane)
		{
			return Vector3.Dot(point, plane) + plane.w > 0.0000001f;
		}
		//

		/// <summary>
		///   Grow the hull. This method takes the current hull, and
		///   expands it to encompass the point in openSet with the
		///   point furthest away from it's face.
		/// </summary>
		void GrowHull(IList<Vector3> points)
		{
			//Assert(openSetTail >= 0);
			//Assert(openSet[0].Face != INSIDE);

			// Find farthest point and first lit face.
			var farthestPoint = 0;
			var dist = openSet[0].Distance;

			for (int i = 1; i <= openSetTail; i++)
			{
				if (openSet[i].Distance > dist)
				{
					farthestPoint = i;
					dist = openSet[i].Distance;
				}
			}

			// Use lit face to find horizon and the rest of the lit
			// faces.
			FindHorizon(
				points,
				points[openSet[farthestPoint].Point],
				openSet[farthestPoint].Face,
				faces[openSet[farthestPoint].Face]);

			//VerifyHorizon();

			// Construct new cone from horizon
			ConstructCone(points, openSet[farthestPoint].Point);

			//VerifyFaces(points);

			// Reassign points
			ReassignPoints(points);
		}

		/// <summary>
		///   Start the search for the horizon.
		///
		///   The search is a DFS search that searches neighboring
		///   triangles in a counter-clockwise fashion. When it find a
		///   neighbor which is not lit, that edge will be a line on
		///   the horizon. If the search always proceeds
		///   counter-clockwise, the edges of the horizon will be
		///   found in counter-clockwise order.
		///
		///   The heart of the search can be found in the recursive
		///   SearchHorizon() method, but the the first iteration of
		///   the search is special, because it has to visit three
		///   neighbors (all the neighbors of the initial triangle),
		///   while the rest of the search only has to visit two
		///   (because one of them has already been visited, the one
		///   you came from).
		/// </summary>
		void FindHorizon(IList<Vector3> points, Vector3 point, int fi, Face face)
		{
			// TODO should I use epsilon in the PointFaceDistance comparisons?

			litFaces.Clear();
			horizon.Clear();

			litFaces.Add(fi);

			//Assert(PointFaceDistance(point, points[face.Vertex0], face) > 0.0f);

			// For the rest of the recursive search calls, we first
			// check if the triangle has already been visited and is
			// part of litFaces. However, in this first call we can
			// skip that because we know it can't possibly have been
			// visited yet, since the only thing in litFaces is the
			// current triangle.
			{
				var oppositeFace = faces[face.Opposite0];

				var dist = PointFaceDistance(
					point,
					points[oppositeFace.Vertex0],
					oppositeFace);

				if (dist <= EPSILON)
				{
					horizon.Add(new HorizonEdge
					{
						Face = face.Opposite0,
						Edge0 = face.Vertex1,
						Edge1 = face.Vertex2,
					});
				}
				else
				{
					SearchHorizon(points, point, fi, face.Opposite0, oppositeFace);
				}
			}

			if (!litFaces.Contains(face.Opposite1))
			{
				var oppositeFace = faces[face.Opposite1];

				var dist = PointFaceDistance(
					point,
					points[oppositeFace.Vertex0],
					oppositeFace);

				if (dist <= EPSILON)
				{
					horizon.Add(new HorizonEdge
					{
						Face = face.Opposite1,
						Edge0 = face.Vertex2,
						Edge1 = face.Vertex0,
					});
				}
				else
				{
					SearchHorizon(points, point, fi, face.Opposite1, oppositeFace);
				}
			}

			if (!litFaces.Contains(face.Opposite2))
			{
				var oppositeFace = faces[face.Opposite2];

				var dist = PointFaceDistance(
					point,
					points[oppositeFace.Vertex0],
					oppositeFace);

				if (dist <= EPSILON)
				{
					horizon.Add(new HorizonEdge
					{
						Face = face.Opposite2,
						Edge0 = face.Vertex0,
						Edge1 = face.Vertex1,
					});
				}
				else
				{
					SearchHorizon(points, point, fi, face.Opposite2, oppositeFace);
				}
			}
		}

		/// <summary>
		///   Recursively search to find the horizon or lit set.
		/// </summary>
		void SearchHorizon(IList<Vector3> points, Vector3 point, int prevFaceIndex, int faceCount, Face face)
		{
			//TODO use epsilon?

			//Assert(prevFaceIndex >= 0);
			//Assert(litFaces.Contains(prevFaceIndex));
			//Assert(!litFaces.Contains(faceCount));
			//Assert(faces[faceCount].Equals(face));

			litFaces.Add(faceCount);

			// Use prevFaceIndex to determine what the next face to
			// search will be, and what edges we need to cross to get
			// there. It's important that the search proceeds in
			// counter-clockwise order from the previous face.
			int nextFaceIndex0;
			int nextFaceIndex1;
			int edge0;
			int edge1;
			int edge2;

			if (prevFaceIndex == face.Opposite0)
			{
				nextFaceIndex0 = face.Opposite1;
				nextFaceIndex1 = face.Opposite2;

				edge0 = face.Vertex2;
				edge1 = face.Vertex0;
				edge2 = face.Vertex1;
			}
			else if (prevFaceIndex == face.Opposite1)
			{
				nextFaceIndex0 = face.Opposite2;
				nextFaceIndex1 = face.Opposite0;

				edge0 = face.Vertex0;
				edge1 = face.Vertex1;
				edge2 = face.Vertex2;
			}
			else
			{
				//Assert(prevFaceIndex == face.Opposite2);

				nextFaceIndex0 = face.Opposite0;
				nextFaceIndex1 = face.Opposite1;

				edge0 = face.Vertex1;
				edge1 = face.Vertex2;
				edge2 = face.Vertex0;
			}

			if (!litFaces.Contains(nextFaceIndex0))
			{
				var oppositeFace = faces[nextFaceIndex0];

				var dist = PointFaceDistance(
					point,
					points[oppositeFace.Vertex0],
					oppositeFace);

				if (dist <= EPSILON)
				{
					horizon.Add(new HorizonEdge
					{
						Face = nextFaceIndex0,
						Edge0 = edge0,
						Edge1 = edge1,
					});
				}
				else
				{
					SearchHorizon(points, point, faceCount, nextFaceIndex0, oppositeFace);
				}
			}

			if (!litFaces.Contains(nextFaceIndex1))
			{
				var oppositeFace = faces[nextFaceIndex1];

				var dist = PointFaceDistance(
					point,
					points[oppositeFace.Vertex0],
					oppositeFace);

				if (dist <= EPSILON)
				{
					horizon.Add(new HorizonEdge
					{
						Face = nextFaceIndex1,
						Edge0 = edge1,
						Edge1 = edge2,
					});
				}
				else
				{
					SearchHorizon(points, point, faceCount, nextFaceIndex1, oppositeFace);
				}
			}
		}

		/// <summary>
		///   Remove all lit faces and construct new faces from the
		///   horizon in a "cone-like" fashion.
		///
		///   This is a relatively straight-forward procedure, given
		///   that the horizon is handed to it in already sorted
		///   counter-clockwise. The neighbors of the new faces are
		///   easy to find: they're the previous and next faces to be
		///   constructed in the cone, as well as the face on the
		///   other side of the horizon. We also have to update the
		///   face on the other side of the horizon to reflect it's
		///   new neighbor from the cone.
		/// </summary>
		void ConstructCone(IList<Vector3> points, int farthestPoint)
		{
			foreach (var fi in litFaces)
			{
				//Assert(faces.ContainsKey(fi));
				faces.Remove(fi);
			}

			var firstNewFace = faceCount;

			for (int i = 0; i < horizon.Count; i++)
			{
				// Vertices of the new face, the farthest point as
				// well as the edge on the horizon. Horizon edge is
				// CCW, so the triangle should be as well.
				var v0 = farthestPoint;
				var v1 = horizon[i].Edge0;
				var v2 = horizon[i].Edge1;

				// Opposite faces of the triangle. First, the edge on
				// the other side of the horizon, then the next/prev
				// faces on the new cone
				var o0 = horizon[i].Face;
				var o1 = (i == horizon.Count - 1) ? firstNewFace : firstNewFace + i + 1;
				var o2 = (i == 0) ? (firstNewFace + horizon.Count - 1) : firstNewFace + i - 1;

				var fi = faceCount++;

				faces[fi] = new Face(
					v0, v1, v2,
					o0, o1, o2,
					Normal(points[v0], points[v1], points[v2]));

				var horizonFace = faces[horizon[i].Face];

				if (horizonFace.Vertex0 == v1)
				{
					//Assert(v2 == horizonFace.Vertex2);
					horizonFace.Opposite1 = fi;
				}
				else if (horizonFace.Vertex1 == v1)
				{
					//Assert(v2 == horizonFace.Vertex0);
					horizonFace.Opposite2 = fi;
				}
				else
				{
					//Assert(v1 == horizonFace.Vertex2);
					//Assert(v2 == horizonFace.Vertex1);
					horizonFace.Opposite0 = fi;
				}

				faces[horizon[i].Face] = horizonFace;
			}
		}

		/// <summary>
		///   Reassign points based on the new faces added by
		///   ConstructCone().
		///
		///   Only points that were previous assigned to a removed
		///   face need to be updated, so check litFaces while looping
		///   through the open set.
		///
		///   There is a potential optimization here: there's no
		///   reason to loop through the entire openSet here. If each
		///   face had it's own openSet, we could just loop through
		///   the openSets in the removed faces. That would make the
		///   loop here shorter.
		///
		///   However, to do that, we would have to juggle A LOT more
		///   List<T>'s, and we would need an object pool to manage
		///   them all without generating a whole bunch of garbage. I
		///   don't think it's worth doing that to make this loop
		///   shorter, a straight for-loop through a list is pretty
		///   darn fast. Still, it might be worth trying
		/// </summary>
		void ReassignPoints(IList<Vector3> points)
		{
			for (int i = 0; i <= openSetTail; i++)
			{
				var fp = openSet[i];

				if (litFaces.Contains(fp.Face))
				{
					var assigned = false;
					var point = points[fp.Point];

					foreach (var kvp in faces)
					{
						var fi = kvp.Key;
						var face = kvp.Value;

						var dist = PointFaceDistance(
							point,
							points[face.Vertex0],
							face);

						if (dist > EPSILON)
						{
							assigned = true;

							fp.Face = fi;
							fp.Distance = dist;

							openSet[i] = fp;
							break;
						}
					}

					if (!assigned)
					{
						// If point hasn't been assigned, then it's
						// inside the convex hull. Swap it with
						// openSetTail, and decrement openSetTail. We
						// also have to decrement i, because there's
						// now a new thing in openSet[i], so we need i
						// to remain the same the next iteration of
						// the loop.
						fp.Face = INSIDE;
						fp.Distance = float.NaN;

						openSet[i] = openSet[openSetTail];
						openSet[openSetTail] = fp;

						i--;
						openSetTail--;
					}
				}
			}
		}

		/// <summary>
		///   Final step in algorithm, export the faces of the convex
		///   hull in a mesh-friendly format.
		///
		///   TODO normals calculation for non-split vertices. Right
		///   now it just leaves the normal array empty.
		/// </summary>
		void ExportMesh(IList<Vector3> points, bool splitVerts, ref List<Vector3> vertsResults, ref List<int> trisResults, ref List<Vector3> normalsResults)
		{
			if (vertsResults == null)
			{
				vertsResults = new List<Vector3>();
			}
			else
			{
				vertsResults.Clear();
			}

			if (trisResults == null)
			{
				trisResults = new List<int>();
			}
			else
			{
				trisResults.Clear();
			}

			if (normalsResults == null)
			{
				normalsResults = new List<Vector3>();
			}
			else
			{
				normalsResults.Clear();
			}

			foreach (var face in faces.Values)
			{
				int vi0, vi1, vi2;

				if (splitVerts)
				{
					vi0 = vertsResults.Count; vertsResults.Add(points[face.Vertex0]);
					vi1 = vertsResults.Count; vertsResults.Add(points[face.Vertex1]);
					vi2 = vertsResults.Count; vertsResults.Add(points[face.Vertex2]);

					normalsResults.Add(face.Normal);
					normalsResults.Add(face.Normal);
					normalsResults.Add(face.Normal);
				}
				else
				{
					if (!hullVerts.TryGetValue(face.Vertex0, out vi0))
					{
						vi0 = vertsResults.Count;
						hullVerts[face.Vertex0] = vi0;
						vertsResults.Add(points[face.Vertex0]);
					}

					if (!hullVerts.TryGetValue(face.Vertex1, out vi1))
					{
						vi1 = vertsResults.Count;
						hullVerts[face.Vertex1] = vi1;
						vertsResults.Add(points[face.Vertex1]);
					}

					if (!hullVerts.TryGetValue(face.Vertex2, out vi2))
					{
						vi2 = vertsResults.Count;
						hullVerts[face.Vertex2] = vi2;
						vertsResults.Add(points[face.Vertex2]);
					}
				}

				trisResults.Add(vi0);
				trisResults.Add(vi1);
				trisResults.Add(vi2);
			}
		}

		/// <summary>
		///   Signed distance from face to point (a positive number
		///   means that the point is above the face)
		/// </summary>
		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		float PointFaceDistance(Vector3 point, Vector3 pointOnFace, Face face)
		{
			return Dot(face.Normal, point - pointOnFace);
		}

		/// <summary>
		///   Calculate normal for triangle
		/// </summary>
		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		Vector3 Normal(Vector3 v0, Vector3 v1, Vector3 v2)
		{
			return Cross(v1 - v0, v2 - v0).normalized;
		}

		/// <summary>
		///   Dot product, for convenience, and testing whether
		///   aggressive inlining has any effect.
		/// </summary>
		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static float Dot(Vector3 a, Vector3 b)
		{
			return a.x * b.x + a.y * b.y + a.z * b.z;
		}

		/// <summary>
		///   Vector3.Cross i left-handed, the algorithm is
		///   right-handed. Also, i wanna test to see if using
		///   aggressive inlining makes any difference here.
		/// </summary>
		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static Vector3 Cross(Vector3 a, Vector3 b)
		{
			return new Vector3(
				a.y * b.z - a.z * b.y,
				a.z * b.x - a.x * b.z,
				a.x * b.y - a.y * b.x);
		}


		/// <summary>
		///   Method used for debugging, verifies that the openSet is
		///   in a sensible state. Conditionally compiled if
		///   DEBUG_QUICKHULL if defined.
		/// </summary>
		//[Conditional("DEBUG_QUICKHULL")]
		void VerifyOpenSet(List<Vector3> points)
		{
			for (int i = 0; i < openSet.Count; i++)
			{
				if (i > openSetTail)
				{
					Assert(openSet[i].Face == INSIDE);
				}
				else
				{
					Assert(openSet[i].Face != INSIDE);
					Assert(openSet[i].Face != UNASSIGNED);

					Assert(PointFaceDistance(
							points[openSet[i].Point],
							points[faces[openSet[i].Face].Vertex0],
							faces[openSet[i].Face]) > 0.0f);
				}
			}
		}

		/// <summary>
		///   Method used for debugging, verifies that the horizon is
		///   in a sensible state. Conditionally compiled if
		///   DEBUG_QUICKHULL if defined.
		/// </summary>
		//[Conditional("DEBUG_QUICKHULL")]
		void VerifyHorizon()
		{
			for (int i = 0; i < horizon.Count; i++)
			{
				var prev = i == 0 ? horizon.Count - 1 : i - 1;

				//Assert(horizon[prev].Edge1 == horizon[i].Edge0);
				//Assert(HasEdge(faces[horizon[i].Face], horizon[i].Edge1, horizon[i].Edge0));
			}
		}

		/// <summary>
		///   Method used for debugging, verifies that the faces array
		///   is in a sensible state. Conditionally compiled if
		///   DEBUG_QUICKHULL if defined.
		/// </summary>
		//[Conditional("DEBUG_QUICKHULL")]
		void VerifyFaces(List<Vector3> points)
		{
			foreach (var kvp in faces)
			{
				var fi = kvp.Key;
				var face = kvp.Value;

				Assert(faces.ContainsKey(face.Opposite0));
				Assert(faces.ContainsKey(face.Opposite1));
				Assert(faces.ContainsKey(face.Opposite2));

				Assert(face.Opposite0 != fi);
				Assert(face.Opposite1 != fi);
				Assert(face.Opposite2 != fi);

				Assert(face.Vertex0 != face.Vertex1);
				Assert(face.Vertex0 != face.Vertex2);
				Assert(face.Vertex1 != face.Vertex2);

				Assert(HasEdge(faces[face.Opposite0], face.Vertex2, face.Vertex1));
				Assert(HasEdge(faces[face.Opposite1], face.Vertex0, face.Vertex2));
				Assert(HasEdge(faces[face.Opposite2], face.Vertex1, face.Vertex0));

				Assert((face.Normal - Normal(
							points[face.Vertex0],
							points[face.Vertex1],
							points[face.Vertex2])).magnitude < EPSILON);
			}
		}

		/// <summary>
		///   Method used for debugging, verifies that the final mesh
		///   is actually a convex hull of all the points.
		///   Conditionally compiled if DEBUG_QUICKHULL if defined.
		/// </summary>
		//[Conditional("DEBUG_QUICKHULL")]
		void VerifyMesh(List<Vector3> points, ref List<Vector3> verts, ref List<int> tris)
		{
			Assert(tris.Count % 3 == 0);

			for (int i = 0; i < points.Count; i++)
			{
				for (int j = 0; j < tris.Count; j += 3)
				{
					var t0 = verts[tris[j]];
					var t1 = verts[tris[j + 1]];
					var t2 = verts[tris[j + 2]];

					Assert(Dot(points[i] - t0, Vector3.Cross(t1 - t0, t2 - t0)) <= EPSILON);
				}

			}
		}

		/// <summary>
		///   Does face f have a face with vertexes e0 and e1? Used
		///   only for debugging.
		/// </summary>
		bool HasEdge(Face f, int e0, int e1)
		{
			return (f.Vertex0 == e0 && f.Vertex1 == e1)
				|| (f.Vertex1 == e0 && f.Vertex2 == e1)
				|| (f.Vertex2 == e0 && f.Vertex0 == e1);
		}

		/// <summary>
		///   //Assert method, conditionally compiled with
		///   DEBUG_QUICKHULL.
		///
		///   I could just use Debug.//Assert or the //Assertions class,
		///   but I like the idea of just writing //Assert(something),
		///   and I also want it to be conditionally compiled out with
		///   the same #define as the other debug methods.
		/// </summary>
		//[Conditional("DEBUG_QUICKHULL")]
		static void Assert(bool condition)
		{
			if (!condition)
			{
				throw new UnityEngine.Assertions.AssertionException("Assertion failed", "");
			}
		}
	}
}
