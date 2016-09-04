using System;
using UnityEngine;
using System.Collections.Generic;

namespace CapsuleCharacterCollisionDetection
{
	public abstract class TestBase<T> : MonoBehaviour where T : Collider
	{
		public bool multipleContacts = true;
		public bool ignoreBehindPlane;
		public bool drawContacts = true;
		public bool drawShape = true;

		protected T myCollider;
		public List<Component> ignoreColliders = new List<Component>();
		protected List<SphereCollisionInfo> collisionPointsBuffer = new List<SphereCollisionInfo>();

		void Start()
		{
			myCollider = GetComponent<T>();
			ignoreColliders.AddRange(GetComponentsInChildren<Collider>(true));
		}

		protected abstract List<SphereCollisionInfo> GetContacts(List<SphereCollisionInfo> resultsBuffer, float checkOffset = 0f);

		protected abstract void DrawShape();
	}
}