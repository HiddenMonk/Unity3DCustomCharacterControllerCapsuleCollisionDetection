using System;
using UnityEngine;
using System.Collections.Generic;

namespace CapsuleCharacterCollisionDetection
{
	public abstract class TestDepenetrationBase<T> : TestBase<T> where T : Collider
	{
		public int detectionIterations = 10;
		public int depenetrationInterations = 20;

		float checkOffset = .01f;

		protected Transform depenetrationShape;

		void Awake()
		{
			depenetrationShape = CreatePrimitive();
			depenetrationShape.localScale = transform.localScale;
			depenetrationShape.SetParent(transform);
		}

		void Update()
		{
			if(drawShape) DrawShape();

			depenetrationShape.position = transform.position;

			for(int i = 0; i < detectionIterations; i++)
			{
				GetContacts(collisionPointsBuffer, checkOffset);
				if(collisionPointsBuffer.Count > 0)
				{
					for(int j = 0; j < collisionPointsBuffer.Count; j++)
					{
						if(drawContacts)
						{
							ExtDebug.DrawPlane(collisionPointsBuffer[j].closestPointOnSurface, collisionPointsBuffer[j].interpolatedNormal, .5f, Color.cyan);
						}
					}
				}
				
				depenetrationShape.position += SphereCollisionDetect.Depenetrate(collisionPointsBuffer, depenetrationInterations);
			}
		}

		protected abstract Transform CreatePrimitive();
	}
}