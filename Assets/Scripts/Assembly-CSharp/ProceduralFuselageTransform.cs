using System;
using UnityEngine;

[Serializable]
public struct ProceduralFuselageTransform
{
	public ProceduralFuselageSide side1;

	public ProceduralFuselageSide side2;

	public bool RoughlyEquals(ProceduralFuselageTransform other)
	{
		if (side1.RoughlyEquals(other.side1))
		{
			return side2.RoughlyEquals(other.side2);
		}
		return false;
	}

	public bool Equals(ProceduralFuselageTransform other, bool includeRoundness = false)
	{
		if (side1.Equals(other.side1, includeRoundness))
		{
			return side2.Equals(other.side2, includeRoundness);
		}
		return false;
	}

	public Vector3 GetBaseOrigin1(Transform fuselageTransform)
	{
		return fuselageTransform.transform.position + fuselageTransform.transform.forward * side1.lengthOffset.z + fuselageTransform.transform.right * side1.lengthOffset.x + fuselageTransform.transform.up * side1.lengthOffset.y * Mathf.Sign(fuselageTransform.localScale.y);
	}

	public Vector3 GetBaseOrigin2(Transform fuselageTransform)
	{
		return fuselageTransform.transform.position - fuselageTransform.transform.forward * side2.lengthOffset.z + fuselageTransform.transform.right * side2.lengthOffset.x + fuselageTransform.transform.up * side2.lengthOffset.y * Mathf.Sign(fuselageTransform.localScale.y);
	}

	public string Log()
	{
		return "Side1: " + side1.radius.ToString() + " " + side1.roundness.ToString() + " " + side1.lengthOffset.ToString() + "Side2: " + side2.radius.ToString() + " " + side2.roundness.ToString() + " " + side2.lengthOffset.ToString();
	}
}
