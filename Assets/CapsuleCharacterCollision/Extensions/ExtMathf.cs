using System;
using UnityEngine;
using System.Collections;

namespace CapsuleCharacterCollisionDetection
{
	public static class ExtMathf
	{
		public static float Min(float value1, float value2, float value3)
		{
			float min = (value1 < value2) ? value1 : value2;
			return (min < value3) ? min : value3;
		}

		public static float Max(float value1, float value2, float value3)
		{
			float max = (value1 > value2) ? value1 : value2;
			return (max > value3) ? max : value3;
		}

		public static bool Approximately(float value1, float value2) {return Approximately(value1, value2, Mathf.Epsilon);}
		public static bool Approximately(float value1, float value2, float precision)
		{
			return Mathf.Abs(value1 - value2) < precision;
		}

		public static float Squared(this float value)
		{
			return value * value;
		}
		public static float Squared(this int value)
		{
			return value * value;
		}
	}
}