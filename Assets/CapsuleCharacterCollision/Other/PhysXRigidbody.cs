using System;
using UnityEngine;

namespace CapsuleCharacterCollisionDetection
{
	//This is a way for you to not worry about whether or not the rigidbody is Physx or our PlayerRigidbody.

	[RequireComponent(typeof(Rigidbody))]
	public class PhysXRigidbody : MonoBehaviour, IRigidbody
	{
		Rigidbody myRigidbody;

		void Awake()
		{
			myRigidbody = GetComponent<Rigidbody>();
		}
		
		public Vector3 velocity {get {return myRigidbody.velocity;} set {myRigidbody.velocity = value;}}

		public void AddForce(Vector3 velocity, ForceMode forceMode = ForceMode.Force)
		{
			myRigidbody.AddForce(velocity, forceMode);
		}

		public void AddExplosionForce(float force, Vector3 position, float explosionRadius, ForceMode forceMode = ForceMode.Force)
		{
			myRigidbody.AddExplosionForce(force, position, explosionRadius, 0f, forceMode);
		}
	}
}