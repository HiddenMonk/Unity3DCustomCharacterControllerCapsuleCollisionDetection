using System;
using UnityEngine;
using System.Collections.Generic;

namespace CapsuleCharacterCollisionDetection
{
	[RequireComponent(typeof(CapsuleCollider))]
	public class TestCapsuleContactPoints : TestContactsBase<CapsuleCollider>
	{
		protected override List<SphereCollisionInfo> GetContacts(List<SphereCollisionInfo> resultsBuffer, float checkOffset = 0f)
		{
			return SphereCollisionDetect.DetectCollisions(transform.position, transform.up, myCollider.height * transform.localScale.x, myCollider.radius * transform.localScale.x, Physics.AllLayers, ignoreColliders, resultsBuffer, checkOffset, multipleContacts);
		}

		protected override void DrawShape()
		{
			ExtDebug.DrawCapsule(transform.position, transform.up, myCollider.height * transform.localScale.x, myCollider.radius * transform.localScale.x, Color.black);
		}
	}
}