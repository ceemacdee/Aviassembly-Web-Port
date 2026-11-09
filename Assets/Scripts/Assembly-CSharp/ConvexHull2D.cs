using System.Collections.Generic;
using UnityEngine;

public class ConvexHull2D
{
	public List<Vector2> points = new List<Vector2>();

	public Vector2 distPoint;

	public List<Vector2> hullPoints = new List<Vector2>();

	public void CreateHull()
	{
		if (points.Count == 0)
		{
			return;
		}
		hullPoints.Clear();
		Vector2 vector = GetLeftPoint();
		do
		{
			hullPoints.Add(vector);
			Vector2 vector2 = points[0];
			for (int i = 0; i < points.Count; i++)
			{
				int num = Orientation(vector, vector2, points[i]);
				if (vector2 == vector || num == 1 || (num == 0 && (vector - points[i]).sqrMagnitude > (vector - vector2).sqrMagnitude))
				{
					vector2 = points[i];
				}
			}
			vector = vector2;
		}
		while (!(vector == hullPoints[0]));
	}

	public bool IsEmptyHull()
	{
		for (int i = 0; i < hullPoints.Count; i++)
		{
			for (int j = 0; j < hullPoints.Count; j++)
			{
				if ((hullPoints[i] - hullPoints[j]).sqrMagnitude > 1f)
				{
					return false;
				}
			}
		}
		return true;
	}

	private Vector2 GetLeftPoint()
	{
		Vector2 result = new Vector2(float.MaxValue, float.MaxValue);
		for (int i = 0; i < points.Count; i++)
		{
			if (points[i].x < result.x)
			{
				result = points[i];
			}
		}
		return result;
	}

	private int Orientation(Vector2 p1, Vector2 p2, Vector2 p3)
	{
		float num = (p2.y - p1.y) * (p3.x - p2.x) - (p2.x - p1.x) * (p3.y - p2.y);
		if (Mathf.Approximately(num, 0f))
		{
			return 0;
		}
		if (!(num > 0f))
		{
			return -1;
		}
		return 1;
	}

	public bool PointInHull(Vector2 point)
	{
		if (hullPoints.Count == 0)
		{
			return false;
		}
		int count = hullPoints.Count;
		double num = point.x;
		double num2 = point.y;
		bool flag = false;
		Vector2 vector = hullPoints[0];
		for (int i = 1; i <= count; i++)
		{
			Vector2 vector2 = hullPoints[i % count];
			if (num2 > (double)Mathf.Min(vector.y, vector2.y) && num2 <= (double)Mathf.Max(vector.y, vector2.y) && num <= (double)Mathf.Max(vector.x, vector2.x))
			{
				double num3 = (num2 - (double)vector.y) * (double)(vector2.x - vector.x) / (double)(vector2.y - vector.y) + (double)vector.x;
				if (vector.x == vector2.x || num <= num3)
				{
					flag = !flag;
				}
			}
			vector = vector2;
		}
		return flag;
	}

	public float GetDistanceFromHull(Vector3 point)
	{
		if (PointInHull(point))
		{
			return 0f;
		}
		float num = float.MaxValue;
		int count = hullPoints.Count;
		for (int i = 0; i < count; i++)
		{
			Vector3 vector = ((i > 0) ? hullPoints[i - 1] : hullPoints[count - 1]);
			float num2 = CustomMath.DistanceToLineSquare(w: (Vector3)hullPoints[i], v: vector, p: point);
			if (num2 < num)
			{
				num = num2;
			}
		}
		return Mathf.Sqrt(num);
	}

	public Vector2 GetHullCenter()
	{
		Vector2 zero = Vector2.zero;
		for (int i = 0; i < hullPoints.Count; i++)
		{
			zero += hullPoints[i];
		}
		return zero / hullPoints.Count;
	}

	public Vector3 GetRandomPointInHull()
	{
		if (points.Count < 2)
		{
			return Vector3.zero;
		}
		int num = Random.Range(0, points.Count);
		int num2 = Random.Range(1, points.Count - 2);
		int index = (num + num2) % points.Count;
		Vector2 a = points[num];
		Vector2 b = points[index];
		Vector2 vector = Vector2.Lerp(a, b, Random.Range(0f, 1f));
		return new Vector3(vector.x, 0f, vector.y);
	}
}
