using System;
using UnityEngine;

[Serializable]
public struct ProceduralFuselageSide
{
	public Vector2 radius;

	public Vector4 roundness;

	public Vector3 lengthOffset;

	public bool RoughlyEquals(ProceduralFuselageSide other)
	{
		if (Vector2.Distance(other.radius, radius) < 0.2f)
		{
			return Vector3.Distance(lengthOffset, other.lengthOffset) < 0.2f;
		}
		return false;
	}

	public string Log()
	{
		return "Radius " + radius.ToString() + "Roundness: " + roundness.ToString() + "Lenth offset " + lengthOffset.ToString();
	}

	public bool Equals(ProceduralFuselageSide other, bool includeRoundness)
	{
		bool num = Vector2.Distance(radius, other.radius) < 0.001f && Vector3.Distance(lengthOffset, other.lengthOffset) < 0.001f;
		bool flag = Vector4.Distance(roundness, other.roundness) < 0.001f;
		if (num)
		{
			if (!flag)
			{
				return !includeRoundness;
			}
			return true;
		}
		return false;
	}
}
