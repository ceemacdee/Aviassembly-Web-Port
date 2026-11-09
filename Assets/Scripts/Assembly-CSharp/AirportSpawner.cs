using System.Collections.Generic;
using UnityEngine;

public class AirportSpawner : Singleton<AirportSpawner>
{
	public List<Vector3> airportPositions = new List<Vector3>();

	public AirportData defaultData;

	public WorldAsset[] allWorldAssets;

	private ContinentManager continentManager;

	private bool initialized;

	protected override void Awake()
	{
		base.Awake();
		continentManager = Singleton<ContinentManager>.Instance;
		if (allWorldAssets.Length != 0 && string.IsNullOrWhiteSpace(Singleton<GameManager>.Instance.currentSaveFile))
		{
			WorldAsset worldAsset = null;
			for (int i = 0; i < allWorldAssets.Length; i++)
			{
				if (worldAsset == null || worldAsset.versionStartingFrom < allWorldAssets[i].versionStartingFrom)
				{
					worldAsset = allWorldAssets[i];
				}
			}
			continentManager.SetWorldAsset(worldAsset);
		}
		if (continentManager.worldAsset != null)
		{
			Singleton<AirportManager>.Instance.allCargoTypes = continentManager.worldAsset.cargoTypes.ToArray();
		}
	}

	private void Start()
	{
		if (!initialized)
		{
			Init();
		}
	}

	private void Init()
	{
		continentManager = Singleton<ContinentManager>.Instance;
		if (Singleton<GameManager>.Instance.gameModeData.disableAirports)
		{
			Airport airport = new Airport();
			airport.Init(Vector3.zero);
			airport.IsBaseAirport = true;
			airport.data = defaultData;
			airport.airportName = airport.data.airportName;
			airportPositions.Add(Vector3.zero);
			Singleton<AirportManager>.Instance.Init();
			Singleton<AirportManager>.Instance.InitFlatObjects();
			return;
		}
		Random.InitState(6);
		InitAirportPositions();
		Random.InitState(6);
		for (int i = 0; i < continentManager.continents.Count; i++)
		{
			ContinentData continentData = continentManager.continents[i];
			for (int j = 0; j < continentData.airports.Count; j++)
			{
				continentData.airports[j].cargoType = continentData.continentType.airports[j].cargoTypes;
			}
		}
		for (int k = 0; k < continentManager.continents.Count; k++)
		{
			ContinentData continentData2 = continentManager.continents[k];
			for (int l = 0; l < continentData2.airports.Count; l++)
			{
				continentData2.airports[l].SetAirportData(continentData2.continentType.airports[l], continentData2);
			}
		}
		Singleton<AirportManager>.Instance.Init();
		Singleton<AirportManager>.Instance.InitFlatObjects();
		Singleton<AirportManager>.Instance.SetInitialAirport();
		initialized = true;
	}

	private void InitAirportPositions()
	{
		airportPositions.Clear();
		for (int i = 0; i < continentManager.continents.Count; i++)
		{
			for (int j = 0; j < continentManager.continents[i].continentType.airports.Length; j++)
			{
				Vector3 airportWorldPosition = continentManager.continents[i].continentType.airports[j].GetAirportWorldPosition();
				airportPositions.Add(airportWorldPosition);
				Airport airport = new Airport();
				airport.Init(airportWorldPosition);
				continentManager.continents[i].airports.Add(airport);
			}
		}
	}

	public void Load(GameDataReader reader)
	{
		if (allWorldAssets.Length != 0)
		{
			WorldAsset worldAsset = null;
			for (int i = 0; i < allWorldAssets.Length; i++)
			{
				if (allWorldAssets[i].versionStartingFrom <= reader.version && (worldAsset == null || worldAsset.versionStartingFrom < allWorldAssets[i].versionStartingFrom))
				{
					worldAsset = allWorldAssets[i];
				}
			}
			continentManager.SetWorldAsset(worldAsset);
			if (worldAsset != null)
			{
				Singleton<AirportManager>.Instance.allCargoTypes = worldAsset.cargoTypes.ToArray();
			}
		}
		Init();
	}

	public bool ChunkContainsAirport(Vector3 position)
	{
		for (int i = 0; i < airportPositions.Count; i++)
		{
			if (Mathf.Abs(airportPositions[i].x - position.x) < 500f && Mathf.Abs(airportPositions[i].z - position.z) < 500f)
			{
				return true;
			}
		}
		return false;
	}
}
