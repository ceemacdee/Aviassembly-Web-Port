using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MenuCamera : MonoBehaviour
{
	public float speed;

	public float chunkSize;

	public int chunkSpawnRadius;

	public GameObject terrainPrefab;

	public Transform worldParent;

	private Dictionary<Vector3, MenuTerrain> currentChunkMap = new Dictionary<Vector3, MenuTerrain>();

	private List<Vector3> targetChunkMap = new List<Vector3>();

	private Camera cam;

	private float drawDistance;

	private void Awake()
	{
		cam = GetComponent<Camera>();
	}

	private void Update()
	{
		drawDistance = Singleton<GameManager>.Instance.graphicsSettings.drawDistance;
		cam.farClipPlane = drawDistance;
		chunkSpawnRadius = Mathf.CeilToInt(drawDistance / 1000f) + 1;
		base.transform.position += base.transform.forward * Time.deltaTime * speed;
		SetTargetChunkMap();
		UpdateTerrain();
	}

	private void SetTargetChunkMap()
	{
		targetChunkMap.Clear();
		Vector3 position = base.transform.position;
		int num = Mathf.RoundToInt(position.x / chunkSize);
		int num2 = Mathf.RoundToInt(position.z / chunkSize);
		for (int i = -chunkSpawnRadius; i < chunkSpawnRadius; i++)
		{
			for (int j = -chunkSpawnRadius; j < chunkSpawnRadius; j++)
			{
				Vector3 vector = new Vector3((float)(num + i) * chunkSize, 0f, (float)(num2 + j) * chunkSize);
				if (Vector3.Distance(position, vector) < chunkSize * (float)chunkSpawnRadius)
				{
					targetChunkMap.Add(vector);
				}
			}
		}
	}

	private void UpdateTerrain()
	{
		List<Vector3> list = new List<Vector3>();
		foreach (KeyValuePair<Vector3, MenuTerrain> item in currentChunkMap)
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
		for (int i = 0; i < targetChunkMap.Count; i++)
		{
			if (!currentChunkMap.ContainsKey(targetChunkMap[i]))
			{
				CreateTerrain(targetChunkMap[i]);
			}
		}
	}

	private void CreateTerrain(Vector3 position)
	{
		GameObject gameObject = Object.Instantiate(terrainPrefab, position, Quaternion.identity, worldParent);
		currentChunkMap.Add(position, gameObject.GetComponent<MenuTerrain>());
		gameObject.GetComponent<MenuTerrain>().cam = base.transform;
	}

	private List<MenuTerrain> SortDictionary(Vector3 point)
	{
		return (from pair in currentChunkMap
			orderby Vector3.Distance(pair.Key, point)
			select pair.Value).ToList();
	}
}
