using System;
using UnityEngine;

namespace Common
{
	public static class MathHelper
	{
		public static int EvenToInt(float num)
		{
			var mod = num % 2;
			if (mod >= 1)
				return Mathf.RoundToInt(num - mod + 2);
			if (mod <= -1)
				return Mathf.RoundToInt(num - mod - 2);
			return Mathf.RoundToInt(num - mod);
		}


		public static Vector3 RoundToInt(Vector3 vec3, bool to_even = false)
		{
			if (to_even)
				return new Vector3(EvenToInt(vec3.x), EvenToInt(vec3.y), EvenToInt(vec3.z));
			Func<float, int> toIntFunc = Mathf.RoundToInt;
			return new Vector3(toIntFunc(vec3.x), toIntFunc(vec3.y), toIntFunc(vec3.z));
		}


		public static Vector2 RoundToInt(Vector2 vec2, bool to_even = false)
		{
			if (to_even)
				return new Vector2(EvenToInt(vec2.x), EvenToInt(vec2.y));
			Func<float, int> toIntFunc = Mathf.RoundToInt;
			return new Vector2(toIntFunc(vec2.x), toIntFunc(vec2.y));
		}
	}
}