using System;
using UnityEngine;

namespace CapsuleCharacterCollisionDetection
{
	public class SpecialPhysicsMaterial : MonoBehaviour
	{
		public float friction;

		public static float GetFriction(Collider collider)
		{
			if(collider == null) return 0f;

			SpecialPhysicsMaterial specialPhysicsMaterial = collider.GetComponent<SpecialPhysicsMaterial>();
			if(specialPhysicsMaterial != null) return specialPhysicsMaterial.friction;

			return 0f;
		}
	}
}