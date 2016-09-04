using System;
using UnityEngine;

namespace CapsuleCharacterCollisionDetection
{
	public struct ContactInfo
	{
		public Vector3 point;
		public Vector3 normal;

		public ContactInfo(Vector3 point, Vector3 normal)
		{
			this.point = point;
			this.normal = normal;
		}
	}
}