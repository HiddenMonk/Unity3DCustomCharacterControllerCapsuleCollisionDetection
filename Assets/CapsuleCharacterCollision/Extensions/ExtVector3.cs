using System;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace CapsuleCharacterCollisionDetection
{
	public static class ExtVector3
	{
		public static readonly Vector3[] GeneralDirections = new Vector3[] {Vector3.right, Vector3.up, Vector3.forward, Vector3.left, Vector3.down, Vector3.back};

		public static float Maximum(this Vector3 vector)
		{
			return ExtMathf.Max(vector.x, vector.y, vector.z);
		}

		public static float Minimum(this Vector3 vector)
		{
			return ExtMathf.Min(vector.x, vector.y, vector.z);
		}

		public static bool IsParallel(Vector3 direction, Vector3 otherDirection, float precision = .000001f)
		{
			return Vector3.Cross(direction, otherDirection).sqrMagnitude < precision;
		}

		public static Vector3 ClosestDirectionTo(Vector3 direction1, Vector3 direction2, Vector3 targetDirection)
		{
			return (Vector3.Dot(direction1, targetDirection) > Vector3.Dot(direction2, targetDirection)) ? direction1 : direction2;
		}

		//from and to must be normalized
		public static float Angle(Vector3 from, Vector3 to)
		{
			return Mathf.Acos(Mathf.Clamp(Vector3.Dot(from, to), -1f, 1f)) * Mathf.Rad2Deg;
		}

		public static Vector3 Direction(Vector3 startPoint, Vector3 targetPoint)
		{
			return (targetPoint - startPoint).normalized;
		}

		public static bool IsInDirection(Vector3 direction, Vector3 otherDirection, float precision, bool normalizeParameters = true)
		{
			if(normalizeParameters)
			{
				direction.Normalize();
				otherDirection.Normalize();
			}
			return Vector3.Dot(direction, otherDirection) > 0f + precision;
		}
		public static bool IsInDirection(Vector3 direction, Vector3 otherDirection)
		{
			return Vector3.Dot(direction, otherDirection) > 0f;
		}

		public static float MagnitudeInDirection(Vector3 vector, Vector3 direction, bool normalizeParameters = true)
		{
			if(normalizeParameters) direction.Normalize();
			return Vector3.Dot(vector, direction);
		}

		public static Vector3 Abs(this Vector3 vector)
		{
			return new Vector3(Mathf.Abs(vector.x), Mathf.Abs(vector.y), Mathf.Abs(vector.z));
		}

		public static Vector3 ClosestGeneralDirection(Vector3 vector) {return ClosestGeneralDirection(vector, GeneralDirections);}
		public static Vector3 ClosestGeneralDirection(Vector3 vector, IList<Vector3> directions)
		{
			float maxDot = float.MinValue;
			int closestDirectionIndex = 0;

			for(int i = 0; i < directions.Count; i++)
			{ 
				float dot = Vector3.Dot(vector, directions[i]);
				if(dot > maxDot)
				{
					closestDirectionIndex = i;
					maxDot = dot;
				}
			}

			return directions[closestDirectionIndex];
		}
	}
}