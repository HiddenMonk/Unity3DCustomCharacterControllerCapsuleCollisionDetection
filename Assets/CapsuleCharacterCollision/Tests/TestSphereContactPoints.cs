using System;
using UnityEngine;
using System.Collections.Generic;

namespace CapsuleCharacterCollisionDetection
{
	[RequireComponent(typeof(SphereCollider))]
	public class TestSphereContactPoints : TestContactsBase<SphereCollider>
	{
		protected override List<SphereCollisionInfo> GetContacts(List<SphereCollisionInfo> resultsBuffer, float checkOffset = 0f)
		{
			return SphereCollisionDetect.DetectCollisions(transform.position, myCollider.radius * transform.localScale.x, Physics.AllLayers, ignoreColliders, resultsBuffer, checkOffset, multipleContacts);
		}

		protected override void DrawShape()
		{
			ExtDebug.DrawWireSphere(transform.position, myCollider.radius * transform.localScale.x, Color.black);
		}
	}
}