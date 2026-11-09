using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{
	public GameObject airportPrefab;

	public bool airport;

	private float chunkSize;

	private ObjectPool surfaceDecorationPool;

	private List<Tree> trees = new List<Tree>();

	private ContinentType continentType;

	private SurfaceDecoration[] surfaceDecorations;

	private Terrain terrain;

	private Texture2D collisionMap;

	private FlatnessRectangle closestAirport;

	private Vector3 position;

	public void Init(float chunkSize, ObjectPool surfaceDecorationPool)
	{
		GameManager instance = Singleton<GameManager>.Instance;
		this.chunkSize = chunkSize;
		this.surfaceDecorationPool = surfaceDecorationPool;
		closestAirport = Singleton<AirportManager>.Instance.GetClosestAirport(base.transform.position).treeSpawningFootprint;
		position = base.transform.position;
		continentType = Singleton<ContinentManager>.Instance.GetClosestContinent(base.transform.position).continentType;
		surfaceDecorations = continentType.surfaceDecorations;
		if (instance.gameModeData.disableAirports)
		{
			surfaceDecorations = Singleton<TerrainGenerationManager>.Instance.defaultSurfaceDecorations;
		}
		if (Singleton<AirportSpawner>.Instance.ChunkContainsAirport(base.transform.position))
		{
			airport = true;
			SpawnAirport();
		}
		terrain = GetComponentInChildren<Terrain>();
		terrain.TerrainComplete += NewSurfaceDecor;
		terrain.Init(airport, continentType);
	}

	public void NewSurfaceDecor()
	{
		HeightMap heightMap = terrain.heightMap;
		if (collisionMap != null)
		{
			heightMap.InsertCollisionsMap(collisionMap);
		}
		Random.InitState((int)base.transform.position.x ^ (int)base.transform.position.y);
		for (int i = 0; i < surfaceDecorations.Length; i++)
		{
			SpawnSurfaceDecoration(surfaceDecorations[i], heightMap);
		}
	}

	private void SpawnTree(Vector2 spawnCoordinate, HeightMap heightMap, SurfaceDecoration decoration)
	{
		float maxInclusive = (float)decoration.density + decoration.randomizationOffset;
		Vector3 vector = new Vector3(spawnCoordinate.x, 0f, spawnCoordinate.y);
		vector += new Vector3(Random.Range(0f, maxInclusive), 0f, Random.Range(0f, maxInclusive));
		Vector3 worldPoint = vector + position;
		if (vector.x < -500f || vector.x > 500f || vector.z < -500f || vector.z > 500f)
		{
			return;
		}
		Vector2 localPosition = new Vector2(vector.x, vector.z) + new Vector2(500f, 500f);
		if (!(closestAirport.GetDistanceToRectangle(worldPoint) < (float)Random.Range(-10, 50)) && !(heightMap.GetCollision(1000f, localPosition) > 0.5f))
		{
			worldPoint.y = heightMap.GetHeight(1000, localPosition);
			Vector3 normal = heightMap.GetNormal(1000f, localPosition);
			float num = 1f - Vector3.Dot(normal, Vector3.up);
			if (!(worldPoint.y < -2f) && !(num < decoration.minSlope) && !(num > decoration.maxSlope))
			{
				Tree tree = surfaceDecorationPool.GetObject(decoration);
				tree.SetPosition(worldPoint);
				tree.SetRotation(Quaternion.Euler(Random.Range(15, -15), Random.Range(0, 360), Random.Range(10, -10)));
				tree.SetScale(Vector3.one * Random.Range(decoration.minSize, decoration.maxSize));
				trees.Add(tree);
			}
		}
	}

	private void SpawnSurfaceDecoration(SurfaceDecoration decoration, HeightMap heightMap)
	{
		for (int i = -500; i < 500; i += decoration.density)
		{
			for (int j = -500; j < 500; j += decoration.density)
			{
				Vector2 vector = new Vector2(i, j);
				if (decoration.clustering)
				{
					SpawnClustered(vector, heightMap, decoration);
				}
				else
				{
					SpawnTree(vector, heightMap, decoration);
				}
			}
		}
	}

	private int SpawnClustered(Vector3 origin, HeightMap heightMap, SurfaceDecoration decoration)
	{
		int result = 0;
		float num = chunkSize / 2f;
		int num2 = Random.Range(1, decoration.maxClusterSize);
		float num3 = 50f;
		for (int i = 0; i < num2; i++)
		{
			Vector2 vector = Random.insideUnitCircle * num3;
			Vector3 vector2 = new Vector3(vector.x, 0f, vector.y);
			Vector3 vector3 = origin + vector2;
			vector3.x = Mathf.Clamp(vector3.x, 0f - num, num);
			vector3.z = Mathf.Clamp(vector3.z, 0f - num, num);
			vector3.y = Singleton<TerrainGenerationManager>.Instance.GetTerrainHeight(base.transform.TransformPoint(vector3)) - 1f;
			SpawnTree(vector3, heightMap, decoration);
		}
		return result;
	}

	private void SpawnAirport()
	{
		Airport airport = Singleton<AirportManager>.Instance.GetClosestAirport(base.transform.position);
		GameObject gameObject = Object.Instantiate((airport.data == null || airport.data.airportPrefab == null) ? airportPrefab : airport.data.airportPrefab);
		gameObject.transform.SetParent(base.transform, worldPositionStays: true);
		Vector3 vector = base.transform.InverseTransformPoint(airport.position);
		float terrainHeight = Singleton<TerrainGenerationManager>.Instance.GetTerrainHeight(airport.position, Singleton<TerrainGenerationManager>.Instance.continentFalloff);
		terrainHeight = Mathf.Max(terrainHeight, -8f);
		gameObject.transform.localPosition = new Vector3(vector.x, terrainHeight, vector.z);
		gameObject.transform.rotation = Quaternion.identity;
		gameObject.transform.rotation = Singleton<AirportManager>.Instance.GetAirportRotation(airport);
		if (airport.collisionMap == null)
		{
			airport.collisionMap = Singleton<CollisionMapGenerator>.Instance.GenerateMap(gameObject);
		}
		collisionMap = (Texture2D)airport.collisionMap;
	}

	private void OnDestroy()
	{
		if (!(surfaceDecorationPool != null))
		{
			return;
		}
		for (int i = 0; i < trees.Count; i++)
		{
			if (trees[i] != null)
			{
				surfaceDecorationPool.ReleaseObject(trees[i]);
			}
		}
	}
}
