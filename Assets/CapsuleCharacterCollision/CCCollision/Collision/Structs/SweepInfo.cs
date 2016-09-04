using UnityEngine;

namespace CapsuleCharacterCollisionDetection
{
	public struct SweepInfo
	{
		public bool hasHit;
		public Vector3 intersectPoint;
		public Vector3 intersectCenter;
		public float distance;
	}
}