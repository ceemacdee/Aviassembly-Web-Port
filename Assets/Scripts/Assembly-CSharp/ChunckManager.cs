using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ChunckManager : Singleton<ChunckManager>
{
	public GameObject chunkPrefab;

	public Transform cameraTransform;

	public float chunkSize;

	public int chunkSpawnRadius;

	private Dictionary<Vector3, Chunk> currentChunkMap = new Dictionary<Vector3, Chunk>();

	private List<Vector3> targetChunkMap = new List<Vector3>();

	private ObjectPool surfaceDecorationPool;

	protected override void Awake()
	{
		base.Awake();
		base.transform.position = Vector3.zero;
	}

	private void Start()
	{
		GameManager.buildModeLoaded += FlightModeEnd;
		surfaceDecorationPool = Singleton<ObjectPool>.Instance;
		SetTargetChunkMap();
	}

	private void FlightModeEnd()
	{
		if (surfaceDecorationPool != null)
		{
			surfaceDecorationPool.DisableAll();
		}
	}

	private void Update()
	{
		chunkSpawnRadius = Mathf.CeilToInt(Singleton<GameManager>.Instance.graphicsSettings.drawDistance / 1000f) + 1;
		SetTargetChunkMap();
		UpdateTerrain();
	}

	private void SetTargetChunkMap()
	{
		Vector3 cameraPosition = cameraTransform.position;
		targetChunkMap.Clear();
		Vector3 forward = Singleton<PlaneContainer>.Instance.transform.forward;
		forward.y = 0f;
		forward.Normalize();
		Vector3 a = cameraTransform.position + forward;
		int num = Mathf.RoundToInt(a.x / chunkSize);
		int num2 = Mathf.RoundToInt(a.z / chunkSize);
		for (int i = -chunkSpawnRadius; i < chunkSpawnRadius; i++)
		{
			for (int j = -chunkSpawnRadius; j < chunkSpawnRadius; j++)
			{
				Vector3 vector = new Vector3((float)(num + i) * chunkSize, 0f, (float)(num2 + j) * chunkSize);
				if (Vector3.Distance(a, vector) < chunkSize * (float)chunkSpawnRadius)
				{
					targetChunkMap.Add(vector);
				}
			}
		}
		targetChunkMap.Sort((Vector3 vector2, Vector3 b) => (vector2 - cameraPosition).sqrMagnitude.CompareTo((b - cameraPosition).sqrMagnitude));
	}

	private void UpdateTerrain()
	{
		List<Vector3> list = new List<Vector3>();
		foreach (KeyValuePair<Vector3, Chunk> item in currentChunkMap)
		{
			if (!targetChunkMap.Contains(item.Key))
			{
				Object.Destroy(item.Value.gameObject);
				list.Add(item.Key);
			}
		}
		foreach (Vector3 item2 in list)
		{
			currentChunkMap.Remove(item2);
		}
		int num = 0;
		for (int i = 0; i < targetChunkMap.Count; i++)
		{
			if (!currentChunkMap.ContainsKey(targetChunkMap[i]))
			{
				CreateChunk(targetChunkMap[i]);
				num++;
				if (num > 5)
				{
					break;
				}
			}
		}
	}

	private void CreateChunk(Vector3 position)
	{
		Chunk component = Object.Instantiate(chunkPrefab, position, Quaternion.identity, base.transform).GetComponent<Chunk>();
		currentChunkMap.Add(position, component);
		component.Init(chunkSize, surfaceDecorationPool);
	}

	private List<Chunk> SortDictionary(Vector3 point)
	{
		return (from pair in currentChunkMap
			orderby Vector3.Distance(pair.Key, point)
			select pair.Value).ToList();
	}
}
