using System.Collections.Generic;
using UnityEngine;

public class ContinentData
{
	public Vector3 origin;

	public ContinentType continentType;

	public List<Airport> airports = new List<Airport>();

	public Airport baseAirport;

	public ConvexHull2D convexHull2D = new ConvexHull2D();

	public Color[] mapColors;

	public ContinentData(Vector3 origin, ContinentType continentType)
	{
		this.origin = origin;
		this.continentType = continentType;
	}

	private void SetMapColors()
	{
		int num = 128;
		float radius = continentType.radius;
		Vector3 vector = origin - new Vector3(radius, 0f, radius);
		TerrainGenerationManager instance = Singleton<TerrainGenerationManager>.Instance;
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num; j++)
			{
				Vector3 worldPos = vector;
				worldPos.x = Mathf.Lerp(vector.x, vector.x + 2f * radius, (float)j / (float)num);
				worldPos.z = Mathf.Lerp(vector.z, vector.z + 2f * radius, (float)i / (float)num);
				float terrainHeight = instance.GetTerrainHeight(worldPos);
				Color color = Color.Lerp(Color.black, Color.white, (terrainHeight + 8f) / 200f);
				mapColors[i * num + j] = color;
			}
		}
	}

	public bool ContinentDiscovered()
	{
		bool result = false;
		for (int i = 0; i < airports.Count; i++)
		{
			if (Singleton<FogOfWar>.Instance.LocationDiscovered(airports[i].position))
			{
				result = true;
				break;
			}
		}
		return result;
	}

	public void UpdateBaseAirport()
	{
		if (airports.Count == 0)
		{
			return;
		}
		Airport airport = null;
		float num = float.MaxValue;
		for (int i = 0; i < airports.Count; i++)
		{
			float magnitude = airports[i].position.magnitude;
			if (magnitude < num)
			{
				num = magnitude;
				airport = airports[i];
			}
		}
		airport.IsBaseAirport = true;
		airport.cargoType = null;
		baseAirport = airport;
		airport.IsBaseAirport = true;
	}

	public void UpdateTreasureAirport()
	{
		List<Airport> list = new List<Airport>(airports);
		list.Remove(baseAirport);
		float num = 0f;
		for (int i = 0; i < list.Count; i++)
		{
			float magnitude = list[i].position.magnitude;
			if (magnitude > num)
			{
				num = magnitude;
				_ = list[i];
			}
		}
	}

	public void UpdateConvexHull()
	{
		convexHull2D.points.Clear();
		for (int i = 0; i < airports.Count; i++)
		{
			if (!(airports[i].data != null) || !airports[i].data.offshoreAirport)
			{
				convexHull2D.points.Add(new Vector2(airports[i].position.x, airports[i].position.z));
			}
		}
		convexHull2D.CreateHull();
	}

	public float GetDistanceFromContinent(Vector3 point)
	{
		if (convexHull2D.IsEmptyHull())
		{
			return Vector3.Distance(origin, point);
		}
		return convexHull2D.GetDistanceFromHull(new Vector2(point.x, point.z));
	}
}
