using System;
using UnityEngine;
using System.Collections.Generic;

namespace CapsuleCharacterCollisionDetection
{
	[RequireComponent(typeof(Rigidbody))]
	public abstract class TestContactsBase<T> : TestBase<T> where T : Collider
	{
		public bool drawPhysXContacts;
		public bool drawPenetration;
		public bool reversePenetration;

		Rigidbody myRigidbody;

		void Awake()
		{
			myRigidbody = GetComponent<Rigidbody>();
			myRigidbody.constraints = RigidbodyConstraints.FreezeAll;
			myRigidbody.useGravity = false;
		}

		void Update()
		{
			if(drawShape) DrawShape();

			GetContacts(collisionPointsBuffer);
			if(ignoreBehindPlane) SphereCollisionDetect.CleanByIgnoreBehindPlane(collisionPointsBuffer);

			if(collisionPointsBuffer.Count > 0)
			{
				for(int i = 0; i < collisionPointsBuffer.Count; i++)
				{
					if(drawContacts)
					{
						ExtDebug.DrawPlane(collisionPointsBuffer[i].closestPointOnSurface, collisionPointsBuffer[i].interpolatedNormal, .25f, Color.magenta);
						Debug.DrawLine(collisionPointsBuffer[i].detectionOrigin, collisionPointsBuffer[i].closestPointOnSurface, Color.red);
						Debug.DrawRay(collisionPointsBuffer[i].closestPointOnSurface, collisionPointsBuffer[i].normal, Color.green);
					}
				}
			}
		}

		void OnCollisionStay(Collision collisionInfo)
		{
			foreach(ContactPoint contact in collisionInfo.contacts)
			{
				if(drawPhysXContacts)
				{
					//I set the draw time to Time.fixedDeltaTime since OnCollisionStay is ran every FixedUpdate, which means there would be flickering if we dont keep the draw there long enough until it runs again.
					ExtDebug.DrawPlane(contact.point, contact.normal, .5f, Color.yellow, Time.fixedDeltaTime);
				}

				if(drawPenetration)
				{
					float seperation = (reversePenetration) ? -contact.separation : contact.separation;
					ExtDebug.DrawPlane(contact.point + (contact.normal * seperation), contact.normal, .1f, Color.cyan, Time.fixedDeltaTime);
				}
			}
		}
	}
}