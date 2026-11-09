using System.Collections.Generic;
using UnityEngine;

public class ContinentManager : Singleton<ContinentManager>
{
	public float continentRadius;

	public float distanceBetweenContinents;

	public GameObject continentHighlighterPrefab;

	public List<ContinentData> continents = new List<ContinentData>();

	public WorldAsset worldAsset;

	public ContinentType[] allContinentTypes;

	public ContinentType defaultContinentType;

	public bool debugMode;

	public bool Initialized { get; private set; }

	public void SetWorldAsset(WorldAsset worldAsset)
	{
		this.worldAsset = worldAsset;
		if (worldAsset != null && worldAsset.continents.Count > 0)
		{
			defaultContinentType = worldAsset.continents[0];
			List<ContinentType> list = new List<ContinentType>();
			for (int i = 1; i < worldAsset.continents.Count; i++)
			{
				list.Add(worldAsset.continents[i]);
			}
			allContinentTypes = list.ToArray();
		}
		if (!Initialized)
		{
			Init();
		}
	}

	private void Update()
	{
	}

	public void InitHighlighters()
	{
		for (int i = 1; i < continents.Count && continents[i].baseAirport != null; i++)
		{
			AiportHighlighterManager.AddHighlightInstance(new HighlightInstance(continentHighlighterPrefab, continents[i].baseAirport.position, continents[i].continentType.hintDistance, 2000f, 1000f));
		}
	}

	public void UpdateBaseAirports()
	{
		for (int i = 0; i < continents.Count; i++)
		{
			continents[i].UpdateBaseAirport();
		}
	}

	public void UpdateConvexHulls()
	{
		for (int i = 0; i < continents.Count; i++)
		{
			continents[i].UpdateConvexHull();
		}
	}

	public void Init()
	{
		if (Initialized)
		{
			return;
		}
		Vector3 vector = Quaternion.Euler(0f, allContinentTypes[0].angleFromOrigin, 0f) * Vector3.forward;
		Vector3 vector2 = Quaternion.Euler(0f, allContinentTypes[1].angleFromOrigin, 0f) * Vector3.forward;
		float num = (debugMode ? 10000f : allContinentTypes[0].distanceFromOrigin);
		float num2 = (debugMode ? 10000f : allContinentTypes[1].distanceFromOrigin);
		vector *= num + allContinentTypes[0].radius * 2f;
		_ = vector2 * (num2 + allContinentTypes[1].radius * 2f);
		continents.Add(new ContinentData(Vector3.forward * (defaultContinentType.radius - 750f), defaultContinentType));
		Random.InitState(3);
		for (int i = 0; i < allContinentTypes.Length; i++)
		{
			Vector3 zero = Vector3.zero;
			int num3 = 0;
			for (int j = 0; j < allContinentTypes[i].airports.Length; j++)
			{
				if (!allContinentTypes[i].airports[j].offshoreAirport)
				{
					zero += allContinentTypes[i].airports[j].GetAirportWorldPosition();
					num3++;
				}
			}
			Vector3 vector3 = zero / num3;
			float num4 = 0f;
			for (int k = 0; k < allContinentTypes[i].airports.Length; k++)
			{
				if (!allContinentTypes[i].airports[k].offshoreAirport)
				{
					num4 = Mathf.Max(num4, Vector3.Distance(vector3, allContinentTypes[i].airports[k].GetAirportWorldPosition()));
				}
			}
			allContinentTypes[i].radius = Mathf.Max(1000f, num4 * 1.1f);
			continents.Add(new ContinentData(vector3, allContinentTypes[i]));
		}
		Initialized = true;
	}

	public float GetContinentFalloff(Vector3 worldPos)
	{
		if (!Initialized)
		{
			Init();
		}
		ContinentData closestContinent = Singleton<ContinentManager>.Instance.GetClosestContinent(worldPos);
		return 1f - Mathf.Clamp01(closestContinent.GetDistanceFromContinent(worldPos) * 0.0003f);
	}

	public float GetTerrainHeight(Vector3 worldPos, float noiceValue)
	{
		ContinentType continentType = GetClosestContinent(worldPos).continentType;
		return Mathf.Lerp(continentType.minHeight, continentType.maxHeight, Mathf.Clamp01(noiceValue));
	}

	public ContinentData GetClosestContinent(Vector3 worldPos)
	{
		if (!Initialized)
		{
			Init();
		}
		ContinentData result = null;
		float num = float.MaxValue;
		for (int i = 0; i < continents.Count; i++)
		{
			float distanceFromContinent = continents[i].GetDistanceFromContinent(worldPos);
			if (distanceFromContinent < num)
			{
				result = continents[i];
				num = distanceFromContinent;
			}
		}
		return result;
	}

	public ContinentData GetCurrentContinent(Vector3 worldPos)
	{
		if (!Initialized)
		{
			Init();
		}
		ContinentData result = null;
		for (int i = 0; i < continents.Count; i++)
		{
			float num = Vector3.Distance(continents[i].origin, worldPos);
			if (num < continents[i].continentType.radius)
			{
				result = continents[i];
			}
		}
		return result;
	}

	public bool IsOcean(Vector3 worldPos)
	{
		ContinentData closestContinent = Singleton<ContinentManager>.Instance.GetClosestContinent(worldPos);
		return Vector3.Distance(worldPos, closestContinent.origin) > closestContinent.continentType.radius * 0.9f;
	}
}
