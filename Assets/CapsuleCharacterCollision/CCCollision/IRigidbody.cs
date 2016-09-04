using System;
using UnityEngine;

namespace CapsuleCharacterCollisionDetection
{
	public interface IRigidbody
	{
		Vector3 velocity {get; set;}
		void AddForce(Vector3 velocity, ForceMode forceMode);
		void AddExplosionForce(float force, Vector3 position, float explosionRadius, ForceMode forceMode);
	}
}