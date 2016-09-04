using System;
using UnityEngine;
using System.Collections.Generic;

namespace CapsuleCharacterCollisionDetection
{
	[RequireComponent(typeof(CapsuleCollider))]
	public class TestCapsuleDepenetration : TestDepenetrationBase<CapsuleCollider>
	{
		protected override List<SphereCollisionInfo> GetContacts(List<SphereCollisionInfo> resultsBuffer, float checkOffset = 0f)
		{
			return SphereCollisionDetect.DetectCollisions(depenetrationShape.position, transform.up, myCollider.height * transform.localScale.x, myCollider.radius * transform.localScale.x, Physics.AllLayers, ignoreColliders, resultsBuffer, checkOffset, multipleContacts);
		}

		protected override void DrawShape()
		{
			ExtDebug.DrawCapsule(transform.position, transform.up, myCollider.height * transform.localScale.x, myCollider.radius * transform.localScale.x, Color.black);
		}

		protected override Transform CreatePrimitive()
		{
			GameObject shape = GameObject.CreatePrimitive(PrimitiveType.Capsule);
			shape.AddComponent<TestCapsuleContactPoints>();
			return shape.transform;
		}
	}
}