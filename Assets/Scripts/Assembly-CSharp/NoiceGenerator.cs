using UnityEngine;

public class NoiceGenerator
{
	private static float GetPerlinNoise(Vector2 pos)
	{
		return Mathf.PerlinNoise(pos.x + 10000f, pos.y + 10000f);
	}

	private static float FractionalBrownianMotion(in Vector2 x, in float H, float octaves)
	{
		float num = 0f;
		for (int i = 0; (float)i < octaves; i++)
		{
			float num2 = Mathf.Pow(2f, i);
			float num3 = Mathf.Pow(num2, 0f - H);
			num += num3 * GetPerlinNoise(num2 * x);
		}
		return num;
	}

	public static float GetNoice(Vector2 p, int octaves)
	{
		Vector2 vector = new Vector2(FractionalBrownianMotion(p + Vector2.zero, 1f, octaves), FractionalBrownianMotion(p + new Vector2(5.2f, 1.3f), 1f, octaves));
		return FractionalBrownianMotion(p + 4f * vector, 1f, octaves) - 0.5f;
	}
}
