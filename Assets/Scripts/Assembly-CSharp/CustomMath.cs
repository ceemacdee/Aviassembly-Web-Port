using System;
using System.Collections.Generic;
using UnityEngine;

public static class CustomMath
{
	public static float SnapToIncrement(float value, float increment)
	{
		return Mathf.Round(value / increment) * increment;
	}

	public static Vector3 SnapToIncrement(Vector3 value, float increment)
	{
		return new Vector3(SnapToIncrement(value.x, increment), SnapToIncrement(value.y, increment), SnapToIncrement(value.z, increment));
	}

	public static Vector2 Pow(Vector2 value, float exponent)
	{
		return value * Mathf.Pow(value.magnitude, exponent);
	}

	public static float SignedDistanceToTransform(Ray ray, Vector3 point, bool includeDepth)
	{
		Vector3 closestPointOnLine = GetClosestPointOnLine(ray.origin, ray.direction, point);
		float num = Vector3.Distance(ray.origin, closestPointOnLine);
		return Vector3.Distance(closestPointOnLine, point) + (includeDepth ? num : 0f);
	}

	public static float DistanceToLine(Ray ray, Vector3 point)
	{
		return Vector3.Cross(ray.direction, point - ray.origin).magnitude;
	}

	public static float Round(float value, int digits)
	{
		return (float)Math.Round(value, digits);
	}

	public static Vector3 GetCenter(List<Vector3> positions)
	{
		Vector3 zero = Vector3.zero;
		for (int i = 0; i < positions.Count; i++)
		{
			zero += positions[i];
		}
		return zero / positions.Count;
	}

	public static Vector3 GetObjectCenter(GameObject obj)
	{
		MeshFilter[] componentsInChildren = obj.GetComponentsInChildren<MeshFilter>();
		if (componentsInChildren == null || componentsInChildren.Length == 0)
		{
			return obj.transform.position;
		}
		Vector3 zero = Vector3.zero;
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (componentsInChildren[i].sharedMesh == null)
			{
				return obj.transform.position;
			}
			zero += componentsInChildren[i].transform.TransformPoint(componentsInChildren[i].sharedMesh.bounds.center);
		}
		return zero / componentsInChildren.Length;
	}

	public static Vector3 ClosestPoint(Vector3 point, List<Vector3> points)
	{
		Vector3 result = points[0];
		float num = float.MaxValue;
		for (int i = 0; i < points.Count; i++)
		{
			float num2 = Vector3.Distance(points[i], point);
			if (num2 < num)
			{
				num = num2;
				result = points[i];
			}
		}
		return result;
	}

	public static int ClosestPointIndex(Vector3 point, List<Vector3> points)
	{
		int result = 0;
		float num = float.MaxValue;
		for (int i = 0; i < points.Count; i++)
		{
			float num2 = Vector3.Distance(points[i], point);
			if (num2 < num)
			{
				num = num2;
				result = i;
			}
		}
		return result;
	}

	public static Vector3 ClosestPointsOnTwoLines(Vector3 origin1, Vector3 direction1, Vector3 origin2, Vector3 direction2)
	{
		float num = Vector3.Dot(direction1, direction1);
		float num2 = Vector3.Dot(direction1, direction2);
		float num3 = Vector3.Dot(direction2, direction2);
		float num4 = num * num3 - num2 * num2;
		if (num4 != 0f)
		{
			Vector3 rhs = origin1 - origin2;
			float num5 = Vector3.Dot(direction1, rhs);
			float num6 = Vector3.Dot(direction2, rhs);
			float num7 = (num2 * num6 - num5 * num3) / num4;
			_ = (num * num6 - num5 * num2) / num4;
			return origin1 + direction1 * num7;
		}
		return origin1;
	}

	public static Vector3 GetClosestPointOnLine(Vector3 origin, Vector3 direction, Vector3 point)
	{
		direction.Normalize();
		float num = Vector3.Dot(point - origin, direction);
		return origin + direction * num;
	}

	public static float DistanceToLine(Vector2 v, Vector2 w, Vector2 p)
	{
		float sqrMagnitude = (v - w).sqrMagnitude;
		if ((double)sqrMagnitude == 0.0)
		{
			return Vector2.Distance(p, v);
		}
		float num = Mathf.Max(0f, Mathf.Min(1f, Vector2.Dot(p - v, w - v) / sqrMagnitude));
		Vector2 b = v + num * (w - v);
		return Vector2.Distance(p, b);
	}

	public static float DistanceToLineSquare(Vector2 v, Vector2 w, Vector2 p)
	{
		float sqrMagnitude = (v - w).sqrMagnitude;
		if ((double)sqrMagnitude == 0.0)
		{
			return (p - v).sqrMagnitude;
		}
		float num = Mathf.Clamp01(Vector2.Dot(p - v, w - v) / sqrMagnitude);
		return (p - (v + num * (w - v))).sqrMagnitude;
	}

	public static Vector2 GetViewportEdgeIntersection(Vector2 direction)
	{
		Vector2 vector = new Vector2(0.5f, 0.5f);
		direction.Normalize();
		float a = float.PositiveInfinity;
		float b = float.PositiveInfinity;
		if (direction.x > 0f)
		{
			a = (1f - vector.x) / direction.x;
		}
		else if (direction.x < 0f)
		{
			a = (0f - vector.x) / direction.x;
		}
		if (direction.y > 0f)
		{
			b = (1f - vector.y) / direction.y;
		}
		else if (direction.y < 0f)
		{
			b = (0f - vector.y) / direction.y;
		}
		float num = Mathf.Min(a, b);
		return vector + direction * num;
	}
}
