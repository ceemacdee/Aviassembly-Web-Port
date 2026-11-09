using UnityEngine;

public class HeightMap
{
	private float[] heights;

	private Vector3[] normals;

	private float[] collisions;

	private int gridSize;

	private Texture2D texture;

	public float MinHeight { get; private set; }

	public float MaxHeight { get; private set; }

	public HeightMap(float[] heights, Vector3[] normals)
	{
		this.heights = heights;
		this.normals = normals;
		gridSize = (int)Mathf.Sqrt(heights.Length);
		MinHeight = float.MaxValue;
		MaxHeight = float.MinValue;
		for (int i = 0; i < heights.Length; i++)
		{
			MinHeight = Mathf.Min(heights[i], MinHeight);
			MaxHeight = Mathf.Max(heights[i], MaxHeight);
		}
	}

	public float GetHeight(int index)
	{
		return heights[index];
	}

	public float GetHeight(int textureSize, Vector2 localPosition)
	{
		float num = localPosition.x / (float)(textureSize - 1) * (float)(gridSize - 1);
		float num2 = localPosition.y / (float)(textureSize - 1) * (float)(gridSize - 1);
		int num3 = (int)num;
		int num4 = (int)num2;
		float num5 = num - (float)num3;
		float num6 = num2 - (float)num4;
		int num7 = Mathf.Min(num3 + 1, gridSize - 1);
		int num8 = Mathf.Min(num4 + 1, gridSize - 1);
		float num9 = heights[num3 * gridSize + num4];
		float num10 = heights[num3 * gridSize + num8];
		float num11 = heights[num7 * gridSize + num4];
		float num12 = heights[num7 * gridSize + num8];
		float num13 = num9 + (num10 - num9) * num6;
		float num14 = num11 + (num12 - num11) * num6;
		return num13 + (num14 - num13) * num5;
	}

	public Vector3 GetNormal(float textureSize, Vector2 localPosition)
	{
		float num = ((float)gridSize - 1f) / (textureSize - 1f);
		float num2 = localPosition.x * num;
		float num3 = localPosition.y * num;
		int num4 = (int)num2;
		int num5 = (int)num3;
		float t = num2 - (float)num4;
		float t2 = num3 - (float)num5;
		int num6 = Mathf.Min(num4 + 1, gridSize - 1);
		int num7 = Mathf.Min(num5 + 1, gridSize - 1);
		Vector3 a = normals[num4 * gridSize + num5];
		Vector3 b = normals[num4 * gridSize + num7];
		Vector3 a2 = normals[num6 * gridSize + num5];
		Vector3 b2 = normals[num6 * gridSize + num7];
		Vector3 a3 = Vector3.LerpUnclamped(a, b, t2);
		Vector3 b3 = Vector3.LerpUnclamped(a2, b2, t2);
		return Vector3.LerpUnclamped(a3, b3, t);
	}

	public float GetCollision(float textureSize, Vector2 localPosition)
	{
		if (collisions == null)
		{
			return 0f;
		}
		int num = (int)Mathf.Sqrt(collisions.Length);
		float num2 = localPosition.x / (textureSize - 1f) * ((float)num - 1f);
		float num3 = localPosition.y / (textureSize - 1f) * ((float)num - 1f);
		int num4 = (int)num2;
		int num5 = (int)num3;
		return collisions[num5 * num + num4];
	}

	public void InsertCollisionsMap(Texture2D collisionMap)
	{
		Color[] pixels = collisionMap.GetPixels();
		int num = (int)Mathf.Sqrt(pixels.Length);
		collisions = new float[pixels.Length];
		int num2 = Mathf.RoundToInt((float)num * 0.1f);
		float num3 = Mathf.Sqrt(num2 * num2 + num2 * num2);
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num; j++)
			{
				if (!(pixels[i * num + j].a > 0.01f))
				{
					continue;
				}
				for (int k = -num2; k < num2; k++)
				{
					for (int l = -num2; l < num2; l++)
					{
						int num4 = j + l;
						int num5 = i + k;
						float magnitude = new Vector2(l, k).magnitude;
						if (num4 > 0 && num4 < num && num5 > 0 && num5 < num)
						{
							collisions[num5 * num + num4] = Mathf.Max(1f - magnitude / num3, collisions[num5 * num + num4]);
						}
					}
				}
			}
		}
	}

	public float NormalizeHeight(float height)
	{
		return Mathf.InverseLerp(MinHeight, MaxHeight, height);
	}

	public Texture2D GetTexture()
	{
		if (texture != null)
		{
			return texture;
		}
		Texture2D texture2D = new Texture2D(gridSize, gridSize);
		texture2D.filterMode = FilterMode.Point;
		Color[] array = new Color[gridSize * gridSize];
		for (int i = 0; i < array.Length; i++)
		{
			NormalizeHeight(heights[i]);
			array[i] = new Color(normals[i].x, normals[i].y, normals[i].z, 1f);
		}
		texture2D.SetPixels(array);
		texture2D.Apply();
		texture = texture2D;
		return texture2D;
	}
}
