using System;
using System.Collections.Generic;
using UnityEngine;

public class ScrapSpawner : Singleton<ScrapSpawner>
{
	public GameObject scrapPrefab;

	public GameObject highlighterPrefab;

	public int scapsPerContinet;

	public Texture scrapIcon;

	public AudioDef scrapPickupSound;

	private List<GameObject> scraps = new List<GameObject>();

	private Dictionary<GameObject, HighlightInstance> scrapHighlighters = new Dictionary<GameObject, HighlightInstance>();

	private Dictionary<ContinentData, int> collectedScrapAmount = new Dictionary<ContinentData, int>();

	private bool iniatialized;

	private void Start()
	{
		base.transform.position = Vector3.zero;
	}

	public void SpawnScraps()
	{
		if (Singleton<GameManager>.Instance.gameModeData.disableAirports)
		{
			return;
		}
		UnityEngine.Random.InitState(DateTime.Now.Date.Minute + DateTime.Now.Hour + DateTime.Now.Second);
		for (int i = 0; i < Singleton<ContinentManager>.Instance.continents.Count; i++)
		{
			if (Singleton<ContinentManager>.Instance.continents[i].baseAirport != null)
			{
				SpawnScraps(Singleton<ContinentManager>.Instance.continents[i]);
			}
		}
	}

	public void CollectScrap(ContinentData continent)
	{
		if (!collectedScrapAmount.ContainsKey(continent))
		{
			collectedScrapAmount.Add(continent, 0);
		}
		collectedScrapAmount[continent]++;
	}

	private void SpawnScraps(ContinentData data)
	{
		int num = 0;
		if (collectedScrapAmount.ContainsKey(data))
		{
			num = collectedScrapAmount[data];
		}
		for (int i = 0; i < Mathf.Max(0, data.continentType.scrapAmount - num); i++)
		{
			SpawnScrap(GetRandomPosition(data));
		}
	}

	private GameObject SpawnScrap(Vector3 position)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate(scrapPrefab);
		gameObject.transform.localScale = Vector3.one;
		gameObject.transform.position = position;
		gameObject.transform.SetParent(base.transform, worldPositionStays: true);
		scraps.Add(gameObject);
		HighlightInstance highlightInstance = new HighlightInstance(highlighterPrefab, position, 600f, 0f);
		AiportHighlighterManager.AddHighlightInstance(highlightInstance);
		scrapHighlighters.Add(gameObject, highlightInstance);
		return gameObject;
	}

	private Vector3 GetRandomPosition(ContinentData data)
	{
		Vector3 vector = new Vector3(0f, -1000f, 0f);
		for (int i = 0; i < 1000; i++)
		{
			vector = data.convexHull2D.GetRandomPointInHull();
			vector.y = Singleton<TerrainGenerationManager>.Instance.GetTerrainHeight(vector);
			if (data.convexHull2D.PointInHull(new Vector2(vector.x, vector.z)) && (Singleton<AirportManager>.Instance.GetClosestAirport(vector).position - vector).magnitude > 1000f && GetClosestScrapDistance(vector) > 1500f && vector.y > 0f && vector.magnitude > 4000f)
			{
				return vector;
			}
		}
		return vector;
	}

	private void Update()
	{
		if (GameManager.gameMode == GameMode.Building || Singleton<GameManager>.Instance.Loading)
		{
			return;
		}
		for (int i = 0; i < scraps.Count; i++)
		{
			scraps[i].gameObject.SetActive(GameManager.gameMode == GameMode.Flying);
		}
		for (int num = scraps.Count - 1; num >= 0; num--)
		{
			Vector3 position = Singleton<PlaneContainer>.Instance.transform.position;
			position.y = 0f;
			Vector3 position2 = scraps[num].transform.position;
			position2.y = 0f;
			if (Vector3.Distance(position, position2) < 175f)
			{
				AiportHighlighterManager.RemoveHighlightInstance(scrapHighlighters[scraps[num]]);
				scrapHighlighters.Remove(scraps[num]);
				UnityEngine.Object.Destroy(scraps[num]);
				scraps.RemoveAt(num);
				Singleton<ResearchManager>.Instance.researchPoints += 5;
				Singleton<FlightWarningManager>.Instance.ShowWarning("5 Scrap Added", "Research new parts at base", scrapIcon, 20, 5f, warning: false, scrapPickupSound);
				CollectScrap(Singleton<ContinentManager>.Instance.GetClosestContinent(position));
			}
		}
	}

	private float GetClosestScrapDistance(Vector3 position)
	{
		float num = float.MaxValue;
		for (int i = 0; i < scraps.Count; i++)
		{
			float num2 = Vector3.Distance(position, scraps[i].transform.position);
			if (num2 < num)
			{
				num = num2;
			}
		}
		return num;
	}

	private void LateUpdate()
	{
		if (!iniatialized)
		{
			SpawnScraps();
			iniatialized = true;
		}
	}

	private ContinentData FindContinent(string continentName)
	{
		for (int i = 0; i < Singleton<ContinentManager>.Instance.continents.Count; i++)
		{
			if (Singleton<ContinentManager>.Instance.continents[i].continentType.name == continentName)
			{
				return Singleton<ContinentManager>.Instance.continents[i];
			}
		}
		return null;
	}

	public void Save(GameDataWriter writer)
	{
		writer.Write(collectedScrapAmount.Count);
		foreach (KeyValuePair<ContinentData, int> item in collectedScrapAmount)
		{
			writer.Write(item.Key.continentType.name);
			writer.Write(item.Value);
		}
	}

	public void Load(GameDataReader reader)
	{
		if (reader.version < 1)
		{
			return;
		}
		int num = reader.ReadInt();
		for (int i = 0; i < num; i++)
		{
			ContinentData continentData = FindContinent(reader.ReadString());
			int value = reader.ReadInt();
			if (continentData != null)
			{
				collectedScrapAmount.Add(continentData, value);
			}
		}
	}
}
