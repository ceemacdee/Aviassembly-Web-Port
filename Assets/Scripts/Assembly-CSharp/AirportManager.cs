using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class AirportManager : Singleton<AirportManager>
{
	public CargoType[] allCargoTypes;

	public AirportSpawner airportSpawner;

	public GameObject airportHighlighterPrefab;

	public List<Airport> airports = new List<Airport>();

	public List<Quest> hoveredQuests = new List<Quest>();

	public static bool airportHover;

	private ContinentManager continentManager;

	public Vector3 CurrentBaseAirport { get; private set; }

	public Vector3 LastAirport { get; private set; }

	public float FuelAtLand { get; private set; }

	public float ElecticityAtLand { get; private set; }

	public void Init()
	{
		Random.InitState(5);
		Singleton<ContinentManager>.Instance.UpdateBaseAirports();
		Singleton<ContinentManager>.Instance.UpdateConvexHulls();
		for (int i = 0; i < airports.Count; i++)
		{
			AiportHighlighterManager.AddHighlightInstance(new HighlightInstance(airportHighlighterPrefab, airports[i].position, 2750f, 500f));
		}
		Singleton<ContinentManager>.Instance.InitHighlighters();
	}

	public Quaternion GetAirportRotation(Airport airport)
	{
		Quaternion identity = Quaternion.identity;
		if (!Singleton<AirportManager>.Instance.GetClosestAirport(airport.position).IsBaseAirport)
		{
			identity.eulerAngles = new Vector3(0f, (airport.position.x + airport.position.z - 110f) % 360f, 0f);
		}
		AirportData data = airport.data;
		if (data != null && data.rotationOverride)
		{
			identity.eulerAngles = new Vector3(0f, data.rotation, 0f);
		}
		return identity;
	}

	public void InitFlatObjects()
	{
		for (int i = 0; i < airports.Count; i++)
		{
			float rotation = (airports[i].position.x + airports[i].position.z - 110f) % 360f;
			AirportData data = airports[i].data;
			if (data != null && data.rotationOverride)
			{
				rotation = data.rotation;
			}
			if (airports[i].IsBaseAirport)
			{
				rotation = 0f;
			}
			Vector2 vector = ((data == null || data.footprint.magnitude < 10f) ? new Vector2(100f, 300f) : data.footprint);
			float blendingDistance = ((data == null || data.blendingDistance < 10f) ? 200f : data.blendingDistance);
			if (airports[i].IsBaseAirport)
			{
				blendingDistance = 1000f;
			}
			if (airports[i].position.magnitude < 200f)
			{
				blendingDistance = 200f;
			}
			FlatnessRectangle flatnessRectangle = new FlatnessRectangle(vector, new Vector2(airports[i].position.x, airports[i].position.z), rotation);
			if (data != null && !data.offshoreAirport)
			{
				Singleton<TerrainGenerationManager>.Instance.flatTerrainObjects.Add(new FlatTerrainObject(airports[i].position, flatnessRectangle, Singleton<TerrainGenerationManager>.Instance.GetAvarageTerrainHeight(airports[i].position), blendingDistance));
			}
			airports[i].flatnessObject = flatnessRectangle;
			FlatnessRectangle treeSpawningFootprint = new FlatnessRectangle((data == null || data.treeSpawningFootprint.magnitude < 10f) ? vector : data.treeSpawningFootprint, new Vector2(airports[i].position.x, airports[i].position.z), rotation);
			airports[i].treeSpawningFootprint = treeSpawningFootprint;
		}
	}

	public void SetInitialAirport()
	{
		for (int i = 0; i < airports.Count; i++)
		{
			if (airports[i].data.initialSpawnAirport)
			{
				LastAirport = airports[i].position;
			}
		}
	}

	public int CargoTypeToIndex(CargoType type)
	{
		for (int i = 0; i < allCargoTypes.Length; i++)
		{
			if (allCargoTypes[i] == type)
			{
				return i;
			}
		}
		return -1;
	}

	public CargoType CargoTypeFromName(string name)
	{
		for (int i = 0; i < allCargoTypes.Length; i++)
		{
			if (allCargoTypes[i].name == name)
			{
				return allCargoTypes[i];
			}
		}
		return null;
	}

	public void AddAirport(Airport airport)
	{
		airports.Add(airport);
	}

	public Airport GetClosestAirport(Vector3 position)
	{
		float num = float.MaxValue;
		Airport result = null;
		int count = airports.Count;
		for (int i = 0; i < count; i++)
		{
			float sqrMagnitude = (position - airports[i].position).sqrMagnitude;
			if (sqrMagnitude < num)
			{
				num = sqrMagnitude;
				result = airports[i];
			}
		}
		return result;
	}

	public Airport GetAirport(AirportData airportData)
	{
		for (int i = 0; i < airports.Count; i++)
		{
			if (airports[i].data == airportData)
			{
				return airports[i];
			}
		}
		return null;
	}

	public CargoType[] GetClosestCargoType(Vector3 position)
	{
		return GetClosestAirport(position)?.cargoType;
	}

	public Airport GetClosestAirport(Vector3 position, CargoType cargoType)
	{
		float num = float.MaxValue;
		Airport result = null;
		for (int i = 0; i < airports.Count; i++)
		{
			if (airports[i].ContainsCargoType(cargoType) || !(cargoType != null))
			{
				float sqrMagnitude = (position - airports[i].position).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					num = sqrMagnitude;
					result = airports[i];
				}
			}
		}
		return result;
	}

	public Airport GetCargoAirport(CargoType cargoType)
	{
		for (int i = 0; i < airports.Count; i++)
		{
			if (airports[i].ContainsCargoType(cargoType))
			{
				return airports[i];
			}
		}
		return null;
	}

	public void ResetAtLastAirport()
	{
		LastAirport = CurrentBaseAirport;
	}

	public int GetUnfinishedMissionCount()
	{
		int num = 0;
		for (int i = 0; i < airports.Count; i++)
		{
			for (int j = 0; j < airports[i].allQuests.Count; j++)
			{
				if (!airports[i].allQuests[j].completed)
				{
					num++;
				}
			}
		}
		return num;
	}

	private void Update()
	{
		if (!Singleton<GameManager>.Instance.Loading && GameManager.gameMode == GameMode.Flying)
		{
			PlaneContainer instance = Singleton<PlaneContainer>.Instance;
			Airport closestAirport = GetClosestAirport(instance.transform.position);
			bool flag = instance.fuel > 0.1f || closestAirport.data == null || closestAirport.data.refuelAvailable;
			if (instance.IsAtAirport() & flag)
			{
				if (Vector3.Distance(LastAirport, closestAirport.position) > 1000f)
				{
					if (instance.fuel > FuelAtLand)
					{
						FuelAtLand = instance.fuel;
					}
					if (instance.electricity > ElecticityAtLand)
					{
						ElecticityAtLand = instance.electricity;
					}
				}
				LastAirport = closestAirport.position;
			}
		}
		ContinentManager instance2 = Singleton<ContinentManager>.Instance;
		if (Singleton<PlaneContainer>.Instance.gameObject.activeInHierarchy && GameManager.gameMode == GameMode.Flying && !Singleton<GameManager>.Instance.Loading && !Singleton<GameManager>.Instance.gameModeData.disableAirports)
		{
			float num = float.MaxValue;
			ContinentData continentData = instance2.continents[0];
			for (int i = 0; i < instance2.continents.Count; i++)
			{
				if (instance2.continents[i].ContinentDiscovered() && (i != 1 || Singleton<StoryState>.Instance.desertUnlocked) && instance2.continents[i].baseAirport.data.baseAirport)
				{
					float num2 = Vector3.Distance(instance2.continents[i].origin, Singleton<PlaneContainer>.Instance.transform.position);
					if (num2 < num)
					{
						num = num2;
						continentData = instance2.continents[i];
					}
				}
			}
			CurrentBaseAirport = continentData.baseAirport.position;
		}
		for (int j = 0; j < airports.Count; j++)
		{
			if (Singleton<PlaneContainer>.Instance.IsAtAirport(airports[j]))
			{
				airports[j].PlaneLandedAtAirport();
			}
		}
	}

	public Airport GetHomeBase()
	{
		for (int i = 0; i < airports.Count; i++)
		{
			if (airports[i].position.magnitude < 200f)
			{
				return airports[i];
			}
		}
		return null;
	}

	public void Save(GameDataWriter writer)
	{
		writer.Write(CurrentBaseAirport);
		writer.Write(LastAirport);
		writer.Write(airports.Count);
		for (int i = 0; i < airports.Count; i++)
		{
			writer.Write(airports[i].id);
			Stream stream = new MemoryStream();
			GameDataWriter writer2 = new GameDataWriter(new BinaryWriter(stream));
			airports[i].Save(writer2, writer.GetPosition());
			writer.Write(stream.Length);
			writer.Write(stream);
		}
		writer.Write(FuelAtLand);
		writer.Write(ElecticityAtLand);
	}

	public void Load(GameDataReader reader)
	{
		CurrentBaseAirport = reader.ReadVector3();
		if (reader.version > 6)
		{
			LastAirport = reader.ReadVector3();
		}
		int num = reader.ReadInt();
		for (int i = 0; i < num; i++)
		{
			string id = reader.ReadString();
			Airport airport = FindAirport(id);
			long num2 = reader.ReadLong();
			if (airport == null)
			{
				reader.SetStreamPosition(reader.GetStreamPosition() + num2);
			}
			else
			{
				airport.Load(reader);
			}
		}
		if (reader.version > 21)
		{
			FuelAtLand = reader.ReadFloat();
			ElecticityAtLand = reader.ReadFloat();
		}
	}

	private Airport FindAirport(string id)
	{
		for (int i = 0; i < airports.Count; i++)
		{
			if (airports[i].id == id)
			{
				return airports[i];
			}
		}
		return null;
	}
}
