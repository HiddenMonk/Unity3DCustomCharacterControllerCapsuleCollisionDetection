using UnityEngine;
using System.Diagnostics;

namespace CapsuleCharacterCollisionDetection
{
	[RequireComponent(typeof(SphereCollider), typeof(Rigidbody))]
	public class TestPhysXPerformance : MonoBehaviour
	{
		public int iterationsPerSecond = 100;
		public bool drawContacts;
		public bool debugLogStopWatch;

		int currentIterations;
		Rigidbody myRigidbody;
		Stopwatch stopWatch = new Stopwatch();

		void Awake()
		{
			myRigidbody = GetComponent<Rigidbody>();
			myRigidbody.constraints = RigidbodyConstraints.FreezeAll;
			myRigidbody.useGravity = false;
		}

		void Update()
		{
			if(iterationsPerSecond != currentIterations)
			{
				Time.fixedDeltaTime = 1f / iterationsPerSecond;
				currentIterations = iterationsPerSecond;
			}
		}

		void FixedUpdate()
		{
			stopWatch.Reset();
			stopWatch.Start();
		}

		void OnCollisionStay(Collision collisionInfo)
		{
			stopWatch.Stop();
			if(debugLogStopWatch) UnityEngine.Debug.Log("PhysX ran " + 1 + " Times and took " + stopWatch.Elapsed.TotalMilliseconds + " Total Milliseconds");
			stopWatch.Reset();

			if(drawContacts)
			{
				for(int i = 0; i < collisionInfo.contacts.Length; i++)
				{
					//I set the draw time to Time.fixedDeltaTime since OnCollisionStay is ran every FixedUpdate, which means there would be flickering if we dont keep the draw there long enough until it runs again.
					UnityEngine.Debug.DrawLine(transform.position, collisionInfo.contacts[i].point, Color.yellow, Time.fixedDeltaTime);
				}
			}
		}
	}
}