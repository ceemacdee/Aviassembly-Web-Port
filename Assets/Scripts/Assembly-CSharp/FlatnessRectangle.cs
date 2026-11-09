using System;
using UnityEngine;

public class FlatnessRectangle
{
	public Vector2 size;

	public Vector2 center;

	public float rotation;

	private Vector2 p1;

	private Vector2 p2;

	private Vector2 p3;

	private Vector2 p4;

	private Vector2 axisAlignedP1;

	private Vector2 axisAlignedP2;

	private Vector2 axisAlignedP3;

	private Vector2 axisAlignedP4;

	public Vector3 refrencePoint;

	private float cosR;

	private float sinR;

	public FlatnessRectangle(Vector2 size, Vector2 center, float rotation)
	{
		this.center = center;
		this.rotation = rotation;
		this.size = size;
		p1 = center - Vector2.up * size.y + Vector2.right * size.x;
		p2 = center + Vector2.up * size.y + Vector2.right * size.x;
		p3 = center - Vector2.up * size.y - Vector2.right * size.x;
		p4 = center + Vector2.up * size.y - Vector2.right * size.x;
		axisAlignedP1 = p1;
		axisAlignedP2 = p2;
		axisAlignedP3 = p3;
		axisAlignedP4 = p4;
		RotateRectangle(rotation);
		cosR = Mathf.Cos(rotation * (MathF.PI / 180f));
		sinR = Mathf.Sin(rotation * (MathF.PI / 180f));
	}

	private void RotateRectangle(float angle)
	{
		p1 = RotateWorldPoint(p1, angle);
		p2 = RotateWorldPoint(p2, angle);
		p3 = RotateWorldPoint(p3, angle);
		p4 = RotateWorldPoint(p4, angle);
	}

	public float GetDistanceToRectangle(Vector2 worldPoint)
	{
		float num = worldPoint.x - center.x;
		float num2 = worldPoint.y - center.y;
		float num3 = num * cosR - num2 * sinR + center.x;
		float num4 = num * sinR + num2 * cosR + center.y;
		float num5 = Mathf.Max(axisAlignedP3.x, Mathf.Min(num3, axisAlignedP1.x));
		float num6 = Mathf.Max(axisAlignedP1.y, Mathf.Min(num4, axisAlignedP2.y));
		float num7 = num3 - num5;
		float num8 = num4 - num6;
		return Mathf.Sqrt(num7 * num7 + num8 * num8);
	}

	private Vector3 RotateWorldPoint(Vector2 point, float angle)
	{
		Vector2 vector = point - center;
		vector = Quaternion.Euler(0f, 0f, 0f - angle) * vector;
		return vector + center;
	}

	public float GetDistanceToRectangle(Vector3 worldPoint)
	{
		Vector2 worldPoint2 = new Vector2(worldPoint.x, worldPoint.z);
		return GetDistanceToRectangle(worldPoint2);
	}
}
