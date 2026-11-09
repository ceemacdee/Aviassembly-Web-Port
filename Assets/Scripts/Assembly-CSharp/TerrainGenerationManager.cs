using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class TerrainGenerationManager : Singleton<TerrainGenerationManager>
{
	private struct TerrainThreadInfo
	{
		public Action<HeightMap> callback;

		public HeightMap parameter;

		public TerrainThreadInfo(Action<HeightMap> callback, HeightMap parameter)
		{
			this.callback = callback;
			this.parameter = parameter;
		}
	}

	[Header("Mesh")]
	public int resolutionX;

	public int resolutionY;

	[Header("Height")]
	public float minHeight;

	public float maxHeight;

	[Header("Noice")]
	public float noiceScale;

	public int octaves;

	public AnimationCurve continentFalloff;

	public bool useContinents;

	[Header("Misc")]
	public float chunkSize;

	public bool menuTerrain;

	public Material defaultMaterial;

	public SurfaceDecoration[] defaultSurfaceDecorations;

	private Queue<TerrainThreadInfo> results = new Queue<TerrainThreadInfo>();

#if UNITY_WEBGL
	private struct PendingHeightmapRequest
	{
		public float xPos;

		public float zPos;

		public Action<HeightMap> callback;

		public PendingHeightmapRequest(float xPos, float zPos, Action<HeightMap> callback)
		{
			this.xPos = xPos;
			this.zPos = zPos;
			this.callback = callback;
		}
	}

	// Milliseconds of terrain generation allowed per frame on WebGL (at least one chunk is always generated).
	private const float WebGLGenerationBudgetMs = 6f;

	private Queue<PendingHeightmapRequest> pendingRequests = new Queue<PendingHeightmapRequest>();

	private readonly System.Diagnostics.Stopwatch generationStopwatch = new System.Diagnostics.Stopwatch();

	private void ProcessPendingRequests()
	{
		if (pendingRequests.Count == 0)
		{
			return;
		}
		generationStopwatch.Restart();
		do
		{
			PendingHeightmapRequest request = pendingRequests.Dequeue();
			GenerateHeightmap(request.xPos, request.zPos, request.callback, threaded: true);
		}
		while (pendingRequests.Count > 0 && generationStopwatch.Elapsed.TotalMilliseconds < WebGLGenerationBudgetMs);
	}
#endif

	private ContinentManager continentManager;

	public Dictionary<Vector2Int, TerrainMeshData> cachedTerrains = new Dictionary<Vector2Int, TerrainMeshData>();

	public List<FlatTerrainObject> flatTerrainObjects = new List<FlatTerrainObject>();

	private Dictionary<Vector2, HeightMap> heightmapCache = new Dictionary<Vector2, HeightMap>();

	private GameManager gameManager;

	public TerrainMeshData templateTerrain;

	protected override void Awake()
	{
		gameManager = Singleton<GameManager>.Instance;
		base.Awake();
		continentManager = Singleton<ContinentManager>.Instance;
		RefreshUseContinents();
		GenerateTemplateTerrain();
	}

	private void GenerateTemplateTerrain()
	{
		templateTerrain = new TerrainMeshData();
		int num = 0;
		for (int i = 0; i < resolutionX; i++)
		{
			for (int j = 0; j < resolutionY; j++)
			{
				Vector3 zero = Vector3.zero;
				zero.x = (float)i / ((float)resolutionX - 1f) - 0.5f;
				zero.z = (float)j / ((float)resolutionY - 1f) - 0.5f;
				templateTerrain.verts.Add(zero);
				if (i < resolutionX - 1 && j < resolutionY - 1)
				{
					templateTerrain.AddTriangle(num, num + resolutionY + 1, num + resolutionY);
					templateTerrain.AddTriangle(num + resolutionY + 1, num, num + 1);
				}
				num++;
				Vector2 zero2 = Vector2.zero;
				zero2.x = (float)i / ((float)resolutionX - 1f);
				zero2.y = (float)j / ((float)resolutionY - 1f);
				templateTerrain.uvs.Add(zero2);
			}
		}
	}

	public void RefreshUseContinents()
	{
		if (!menuTerrain)
		{
			useContinents = !Singleton<GameManager>.Instance.gameModeData.disableAirports;
		}
		if (gameManager.gameModeData.disableAirports && !menuTerrain)
		{
			if (gameManager.gameModeData.mapType == MapType.Forest)
			{
				defaultMaterial = gameManager.gameModeData.forest.material;
				defaultSurfaceDecorations = gameManager.gameModeData.forest.surfaceDecorations;
				maxHeight = gameManager.gameModeData.forest.maxHeight;
				minHeight = gameManager.gameModeData.forest.minHeight;
			}
			if (gameManager.gameModeData.mapType == MapType.Snow)
			{
				defaultMaterial = gameManager.gameModeData.snow.material;
				defaultSurfaceDecorations = gameManager.gameModeData.snow.surfaceDecorations;
				maxHeight = gameManager.gameModeData.snow.maxHeight;
				minHeight = gameManager.gameModeData.snow.minHeight;
			}
			if (gameManager.gameModeData.mapType == MapType.Desert)
			{
				defaultMaterial = gameManager.gameModeData.desert.material;
				defaultSurfaceDecorations = gameManager.gameModeData.desert.surfaceDecorations;
				maxHeight = gameManager.gameModeData.desert.maxHeight;
				minHeight = gameManager.gameModeData.desert.minHeight;
			}
			if (gameManager.gameModeData.mapType == MapType.Flat)
			{
				defaultMaterial = gameManager.gameModeData.flat.material;
				defaultSurfaceDecorations = gameManager.gameModeData.flat.surfaceDecorations;
				maxHeight = gameManager.gameModeData.flat.maxHeight;
				minHeight = gameManager.gameModeData.flat.minHeight;
			}
			if (gameManager.gameModeData.mapType == MapType.Hills)
			{
				defaultMaterial = gameManager.gameModeData.hills.material;
				defaultSurfaceDecorations = gameManager.gameModeData.hills.surfaceDecorations;
				maxHeight = gameManager.gameModeData.hills.maxHeight;
				minHeight = gameManager.gameModeData.hills.minHeight;
			}
			if (gameManager.gameModeData.mapType == MapType.Ocean)
			{
				defaultMaterial = gameManager.gameModeData.ocean.material;
				defaultSurfaceDecorations = gameManager.gameModeData.ocean.surfaceDecorations;
				maxHeight = gameManager.gameModeData.ocean.maxHeight;
				minHeight = gameManager.gameModeData.ocean.minHeight;
			}
		}
	}

	public void RequestHeightmap(float xPos, float zPos, Action<HeightMap> callback, bool threaded = true)
	{
		if (threaded)
		{
#if UNITY_WEBGL
			// WebGL port: browsers do not support managed threads, so "threaded" requests are queued
			// and generated on the main thread, a few per frame within a time budget (see Update).
			pendingRequests.Enqueue(new PendingHeightmapRequest(xPos, zPos, callback));
#else
			new Thread(() =>
			{
				GenerateHeightmap(xPos, zPos, callback, threaded);
			}).Start();
#endif
			return;
		}
		Vector2 key = new Vector2(xPos, zPos);
		if (heightmapCache.ContainsKey(key))
		{
			callback(heightmapCache[key]);
			return;
		}
		HeightMap value = GenerateHeightmap(xPos, zPos, callback, threaded);
		heightmapCache.Add(key, value);
	}

	public float GetFlatnessAjustedHeight(Vector3 worldPos, float baseHeight)
	{
		Vector2 worldPoint = new Vector3(worldPos.x, worldPos.z);
		float num = 1f;
		float a = baseHeight;
		for (int i = 0; i < flatTerrainObjects.Count; i++)
		{
			FlatTerrainObject flatTerrainObject = flatTerrainObjects[i];
			float num2 = Mathf.Clamp01(flatTerrainObject.rectangle2D.GetDistanceToRectangle(worldPoint) / flatTerrainObject.blendingDistance);
			if (num2 < 0.99f)
			{
				num = num2;
				a = flatTerrainObject.height;
			}
		}
		float t = Mathf.Clamp01(num - 0.25f) * 1.33333f;
		return Mathf.Lerp(a, baseHeight, t);
	}

	public float GetTerrainHeightFast(Vector3 worldPos, AnimationCurve falloffCurve, List<FlatTerrainObject> flatTerrainObjects)
	{
		Vector3 vector = new Vector3(worldPos.x / chunkSize, 0f, worldPos.z / chunkSize) * noiceScale;
		float noice = NoiceGenerator.GetNoice(new Vector2(vector.x, vector.z), octaves);
		float num = Mathf.Lerp(minHeight, maxHeight, Mathf.Clamp01(noice));
		if (useContinents)
		{
			float time = continentManager.GetContinentFalloff(worldPos);
			noice -= 1f - falloffCurve.Evaluate(time);
			num = continentManager.GetTerrainHeight(worldPos, noice);
		}
		Vector2 worldPoint = new Vector3(worldPos.x, worldPos.z);
		float num2 = 1f;
		float a = num;
		for (int i = 0; i < flatTerrainObjects.Count; i++)
		{
			FlatTerrainObject flatTerrainObject = flatTerrainObjects[i];
			float num3 = Mathf.Clamp01(flatTerrainObject.rectangle2D.GetDistanceToRectangle(worldPoint) / flatTerrainObject.blendingDistance);
			if (num3 < 0.99f)
			{
				num2 = num3;
				a = flatTerrainObject.height;
			}
		}
		float t = Mathf.Clamp01(num2 - 0.25f) * 1.33333f;
		return Mathf.Lerp(a, num, t);
	}

	public float GetTerrainHeight(Vector3 worldPos, AnimationCurve falloffCurve = null)
	{
		Vector3 zero = Vector3.zero;
		zero.x = worldPos.x / chunkSize * noiceScale;
		zero.z = worldPos.z / chunkSize * noiceScale;
		float noice = NoiceGenerator.GetNoice(new Vector2(zero.x, zero.z), octaves);
		float baseHeight = Mathf.Lerp(minHeight, maxHeight, Mathf.Clamp01(noice));
		if (useContinents)
		{
			float time = continentManager.GetContinentFalloff(worldPos);
			if (falloffCurve == null)
			{
				falloffCurve = new AnimationCurve(continentFalloff.keys);
			}
			noice -= 1f - falloffCurve.Evaluate(time);
			baseHeight = continentManager.GetTerrainHeight(worldPos, noice);
		}
		return GetFlatnessAjustedHeight(worldPos, baseHeight);
	}

	public int GetAvarageTerrainHeight(Vector3 chunkWorldPosition)
	{
		int num = 5;
		float num2 = 0f;
		float num3 = 1000f / (float)num;
		chunkWorldPosition.x = Mathf.FloorToInt(chunkWorldPosition.x / 1000f) * 1000;
		chunkWorldPosition.z = Mathf.FloorToInt(chunkWorldPosition.y / 1000f) * 1000;
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num; j++)
			{
				Vector3 worldPos = new Vector3(chunkWorldPosition.x - 500f + (float)i * num3, 0f, chunkWorldPosition.z - 500f + (float)j * num3);
				num2 += GetTerrainHeightFast(worldPos, Singleton<TerrainGenerationManager>.Instance.continentFalloff, new List<FlatTerrainObject>());
			}
		}
		return (int)Mathf.Max(Mathf.RoundToInt(num2 / (float)(num * num)), 0f);
	}

	public Vector3 GetTerrainNormal(Vector3 worldPos)
	{
		float num = 1f / (float)resolutionX * chunkSize;
		Vector3 vector = new Vector3(CustomMath.SnapToIncrement(worldPos.x, num), 0f, CustomMath.SnapToIncrement(worldPos.z, num));
		Vector3 vector2 = vector + new Vector3(num, 0f, 0f);
		Vector3 vector3 = vector + new Vector3(0f, 0f, num);
		vector.y = GetTerrainHeight(vector);
		vector2.y = GetTerrainHeight(vector2);
		vector3.y = GetTerrainHeight(vector3);
		return Vector3.Cross(vector2 - vector, vector3 - vector).normalized;
	}

	private void Update()
	{
#if UNITY_WEBGL
		ProcessPendingRequests();
#endif
		lock (results)
		{
			for (int i = 0; i < results.Count; i++)
			{
				TerrainThreadInfo terrainThreadInfo = results.Dequeue();
				terrainThreadInfo.callback(terrainThreadInfo.parameter);
			}
		}
	}

	public List<FlatTerrainObject> GetRelevantFlatTerrainObjects(Vector3 chunkPosition)
	{
		List<FlatTerrainObject> list = new List<FlatTerrainObject>(flatTerrainObjects);
		float num = chunkSize * 0.75f;
		for (int num2 = list.Count - 1; num2 >= 0; num2--)
		{
			if (list[num2].rectangle2D.GetDistanceToRectangle(chunkPosition) > num + list[num2].blendingDistance)
			{
				list.RemoveAt(num2);
			}
		}
		return list;
	}

	private HeightMap GenerateHeightmap(float xPos, float zPos, Action<HeightMap> callback, bool threaded)
	{
		AnimationCurve falloffCurve = new AnimationCurve(continentFalloff.keys);
		List<FlatTerrainObject> list = new List<FlatTerrainObject>(flatTerrainObjects);
		Vector2 worldPoint = new Vector2(xPos * chunkSize, zPos * chunkSize);
		float num = chunkSize * 0.75f;
		for (int num2 = list.Count - 1; num2 >= 0; num2--)
		{
			if (list[num2].rectangle2D.GetDistanceToRectangle(worldPoint) > num + list[num2].blendingDistance)
			{
				list.RemoveAt(num2);
			}
		}
		float[] array = new float[resolutionX * resolutionY];
		Vector3[] array2 = new Vector3[resolutionX * resolutionY];
		int num3 = 0;
		for (int i = 0; i < resolutionX; i++)
		{
			for (int j = 0; j < resolutionY; j++)
			{
				Vector3 zero = Vector3.zero;
				zero.x = (float)i / ((float)resolutionX - 1f) - 0.5f;
				zero.z = (float)j / ((float)resolutionY - 1f) - 0.5f;
				Vector3 worldPos = new Vector3((xPos + zero.x) * chunkSize, 0f, (zPos + zero.z) * chunkSize);
				zero.y = GetTerrainHeightFast(worldPos, falloffCurve, list);
				array[num3] = zero.y;
				num3++;
			}
		}
		float y = chunkSize / (float)resolutionX;
		num3 = 0;
		for (int k = 0; k < resolutionX; k++)
		{
			for (int l = 0; l < resolutionY; l++)
			{
				int num4 = Mathf.Min(k + 1, resolutionX - 1);
				int num5 = Mathf.Min(l + 1, resolutionY - 1);
				int num6 = k * resolutionY + l;
				int num7 = num4 * resolutionY + l;
				int num8 = k * resolutionY + num5;
				float num9 = array[num6];
				float num10 = array[num7];
				float num11 = array[num8];
				array2[num3] = new Vector3(num9 - num10, y, num9 - num11).normalized;
				num3++;
			}
		}
		HeightMap heightMap = new HeightMap(array, array2);
		if (threaded)
		{
			lock (results)
			{
				results.Enqueue(new TerrainThreadInfo(callback, heightMap));
			}
		}
		else
		{
			callback(heightMap);
		}
		return heightMap;
	}
}
