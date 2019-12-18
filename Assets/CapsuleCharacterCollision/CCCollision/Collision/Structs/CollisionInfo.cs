using System;
using UnityEngine;

namespace CapsuleCharacterCollisionDetection
{
	public struct CollisionInfo
	{
		public Vector3 safeMoveDirection;
		public Vector3 velocity;
		public bool hasCollided;
		public bool hasFailed;
		public int attempts;
	}
}
