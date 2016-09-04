using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;

namespace CapsuleCharacterCollisionDetection
{
	[RequireComponent(typeof(Collider))]
	public class TestPerformance : MonoBehaviour
	{
		public int iterations = 1;
		public bool multipleContacts = true;
		public bool debugDrawContacts;
		public bool debugLogStopWatch;
		public List<Component> ignoreColliders;

		List<SphereCollisionInfo> collisionPoints = new List<SphereCollisionInfo>();
		SphereCollider sphereCollider;
		CapsuleCollider capsuleCollider;
		Stopwatch stopWatch = new Stopwatch();

		void Awake()
		{
			sphereCollider = GetComponent<SphereCollider>();
			capsuleCollider = GetComponent<CapsuleCollider>();
			ignoreColliders.AddRange(GetComponentsInChildren<Collider>());
		}

		void Update()
		{
			stopWatch.Reset();
			stopWatch.Start();
			for(int i = 0; i < iterations; i++)
			{
				if(capsuleCollider != null) SphereCollisionDetect.DetectCollisions(transform.position, transform.up, capsuleCollider.height * transform.localScale.x, capsuleCollider.radius * transform.localScale.x, Physics.AllLayers, ignoreColliders, collisionPoints, 0, multipleContacts);
				else if(sphereCollider != null) SphereCollisionDetect.DetectCollisions(transform.position, sphereCollider.radius * transform.localScale.x, Physics.AllLayers, ignoreColliders, collisionPoints, 0, multipleContacts);
			}
			stopWatch.Stop();
			if(debugLogStopWatch) UnityEngine.Debug.Log("MeshTree ran " + iterations + " Times and took " + stopWatch.Elapsed.TotalMilliseconds + " Total Milliseconds");

			if(debugDrawContacts)
			{
				for(int i = 0; i < collisionPoints.Count; i++)
				{
					UnityEngine.Debug.DrawLine(transform.position, collisionPoints[i].closestPointOnSurface, Color.red);
				}
			}
		}
	}
}